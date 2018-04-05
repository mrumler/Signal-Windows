﻿using libsignalservice;
using libsignalservice.push;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Signal_Windows.Lib
{
    public static class DispatcherTaskExtensions
    {
        // Taken from https://github.com/Microsoft/Windows-task-snippets/blob/master/tasks/UI-thread-task-await-from-background-thread.md
        public static async Task<T> RunTaskAsync<T>(this CoreDispatcher dispatcher,
            Func<Task<T>> func, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            await dispatcher.RunAsync(priority, async () =>
            {
                try
                {
                    taskCompletionSource.SetResult(await func());
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });
            return await taskCompletionSource.Task;
        }

        // There is no TaskCompletionSource<void> so we use a bool that we throw away.
        public static async Task RunTaskAsync(this CoreDispatcher dispatcher,
            Func<Task> func, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal) =>
            await RunTaskAsync(dispatcher, async () => { await func(); return false; }, priority);

        // We want to await
        public static async Task<bool> RunTaskAsync(this CoreDispatcher dispatcher,
            Action func, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            await dispatcher.RunAsync(priority, () =>
            {
                try
                {
                    func();
                    taskCompletionSource.SetResult(false);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });
            return await taskCompletionSource.Task;
        }
    }

    public class LibUtils
    {
        private static readonly ILogger Logger = LibsignalLogging.CreateLogger<LibUtils>();
        public const string GlobalSemaphoreName = "SignalWindowsPrivateMessenger_Mutex";
        public static string URL = "https://textsecure-service.whispersystems.org";
        public static SignalServiceUrl[] ServiceUrls = new SignalServiceUrl[] { new SignalServiceUrl(URL, null) };
        public static bool MainPageActive = false;
        public static string USER_AGENT = "Signal-Windows";
        public static uint PREKEY_BATCH_SIZE = 100;
        public static bool WindowActive = false;
        public static Mutex GlobalLock;
        private static SynchronizationContext GlobalLockContext;

        internal static void Lock()
        {
            Logger.LogTrace("System lock locking, sync context = {0}", SynchronizationContext.Current);
            GlobalLock = new Mutex(false, GlobalSemaphoreName, out bool createdNew);
            GlobalLockContext = SynchronizationContext.Current;
            try
            {
                GlobalLock.WaitOne();
            }
            catch (AbandonedMutexException e)
            {
                Logger.LogWarning("System lock was abandoned! {0}", e.Message);
            }
            Logger.LogTrace("System lock locked");
        }

        public static bool Lock(int timeout)
        {
            GlobalLock = new Mutex(false, GlobalSemaphoreName, out bool createdNew);
            GlobalLockContext = SynchronizationContext.Current;
            Logger.LogTrace("System lock locking with timeout, sync context = {0}", SynchronizationContext.Current);
            bool success = false;
            try
            {
                success = GlobalLock.WaitOne(timeout);
            }
            catch(AbandonedMutexException e)
            {
                Logger.LogWarning("System lock was abandoned! {0}", e.Message);
                success = true;
            }
            Logger.LogTrace("System lock locked = {}", success);
            return success;
        }

        public static void Unlock()
        {
            Logger.LogTrace("System lock releasing, sync context = {0}", SynchronizationContext.Current);
            try
            {
                if(GlobalLockContext != null)
                {
                    GlobalLockContext.Post((a) =>
                    {
                        GlobalLock.ReleaseMutex();
                    }, null);
                }
                else
                {
                    GlobalLock.ReleaseMutex();
                }
            }
            catch(Exception e)
            {
                Logger.LogWarning("System lock failed to unlock! {0}\n{1}", e.Message, e.StackTrace);
            }
            Logger.LogTrace("System lock released");
        }
    }
}
