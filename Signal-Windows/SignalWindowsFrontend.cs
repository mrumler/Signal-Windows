using libsignalservice;
using Microsoft.Extensions.Logging;
using Signal_Windows.Lib;
using Signal_Windows.Models;
using Signal_Windows.ViewModels;
using Signal_Windows.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Signal_Windows
{
    public class SignalWindowsFrontend : ISignalFrontend
    {
        private readonly ILogger Logger = LibsignalLogging.CreateLogger<SignalWindowsFrontend>();
        public CoreApplicationView View { get; set; }
        public ViewModelLocator Locator { get; set; }
        public int ViewId { get; set; }
        public ActivationViewSwitcher Switcher { get; set; }
        public SignalWindowsFrontend(CoreApplicationView view, ViewModelLocator locator, int viewId, ActivationViewSwitcher switcher)
        {
            View = view;
            Locator = locator;
            ViewId = ViewId;
            Switcher = switcher;
        }
        public void AddOrUpdateConversation(SignalConversation conversation, SignalMessage updateMessage)
        {
            Locator.MainPageInstance.AddOrUpdateConversation(conversation, updateMessage);
        }

        public void HandleIdentitykeyChange(LinkedList<SignalMessage> messages)
        {
            Locator.MainPageInstance.HandleIdentitykeyChange(messages);
        }

        public void HandleMessage(SignalMessage message, SignalConversation conversation)
        {
            Locator.MainPageInstance.HandleMessage(message, conversation);
        }

        public void HandleMessageUpdate(SignalMessage updatedMessage)
        {
            Locator.MainPageInstance.HandleMessageUpdate(updatedMessage);
        }

        public void ReplaceConversationList(List<SignalConversation> conversations)
        {
            Locator.MainPageInstance.ReplaceConversationList(conversations);
        }

        public void HandleAuthFailure()
        {
            Logger.LogInformation("HandleAuthFailure() {0}", View);
            if (View.IsMain)
            {
                Frame f = (Frame) Window.Current.Content;
                f.Navigate(typeof(StartPage));
                View.CoreWindow.Activate();
                ApplicationViewSwitcher.TryShowAsStandaloneAsync(App.MainViewId);
            }
            else
            {
                // kill this window!
                View.CoreWindow.Close();
            }
        }
    }
}
