using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Web.Http;
using System.Text.RegularExpressions;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.System;
using Windows.UI.Popups;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ChatsApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public int _NewConversationCount = 0;

        public MainPage()
        {
            this.InitializeComponent();
            MyWebview.RegisterPropertyChangedCallback(WebView.DocumentTitleProperty, OnDocTitleChanged);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up interface
            AboutText.Text = this.ApplicationVersion;
            NotificationToggle.IsOn = this.EnableNotifications;
            DisplayTitleBar(CoreApplication.GetCurrentView().TitleBar, null);
            Window.Current.SetTitleBar(MainTitleBar);

            // Bind Events
            Window.Current.Activated += Current_Activated;
            CoreApplication.GetCurrentView().TitleBar.IsVisibleChanged += DisplayTitleBar;
            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += DisplayTitleBar;

            // Make WhatsApp request
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, new Uri("https://web.whatsapp.com"));
            req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Safari/537.36");
            this.MyWebview.NavigateWithHttpRequestMessage(req);

            // Display warning for Continuum
            await ShowContinuumWarning();

        }

        private static async System.Threading.Tasks.Task ShowContinuumWarning()
        {
            var mobileWarned = Windows.Storage.ApplicationData.Current.LocalSettings.Values["MobileWarned"];
            if (App.IsMobile && mobileWarned == null)
            {
                var message = "Hey, it looks like you're running this on Windows 10 Mobile! That's cool, just be aware this is meant for running on a " +
                                "second monitor with Continuum. If you don't have a second screen (or a second device with the official WhatsApp app), you're not going to " +
                                "be able to scan this QR code." +
                                "\r\n\r\n" +
                                "I won't bug you about this again.";
                var dialog = new MessageDialog(message);
                dialog.Commands.Add(new UICommand("Got it!"));
                dialog.CancelCommandIndex = 0;
                await dialog.ShowAsync();
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["MobileWarned"] = true;
            }
        }

        private void MyWebview_PermissionRequested(WebView sender, WebViewPermissionRequestedEventArgs args)
        {
            if (args.PermissionRequest.PermissionType == WebViewPermissionType.Media &&
                    args.PermissionRequest.Uri.Host == "web.whatsapp.com")
            {
                args.PermissionRequest.Allow();
            }
        }
        
        private void MyWebview_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Nav completed");
        }

        private void OnDocTitleChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (MyWebview.DocumentTitle.Trim().Length > 0)
            {
                int conversationCountFromTitle = ConvertToInt(MyWebview.DocumentTitle);
                if (_NewConversationCount != conversationCountFromTitle)
                {
                    System.Diagnostics.Debug.WriteLine("New conversation count: " + MyWebview.DocumentTitle);
                    if (_NewConversationCount == 0 && DisplayNotifications)
                    {
                        Notify("You have new messages.");
                    }
                    _NewConversationCount = conversationCountFromTitle;
                }
            }
        }


        private void Notify(string notificationText)
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastImageAndText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(notificationText));

            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public static int ConvertToInt(String input)
        {
            // Strip the number from a string
            String inputCleaned = Regex.Replace(input, "[^0-9]", "");
            int value = 0;
            if (int.TryParse(inputCleaned, out value))
            {
                return value;
            }

            return 0; 
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentSplitView.IsPaneOpen = !ContentSplitView.IsPaneOpen;
        }

        bool DisplayNotifications
        {
            get
            {
                return (!this.IsInFocus && this.EnableNotifications);
            }
        }

        string ApplicationVersion
        {
            get
            {
                var theApp = Windows.ApplicationModel.Package.Current;
                var ver = theApp.Id.Version;
                return theApp.DisplayName + " " + ver.Major.ToString() + "." + ver.Minor.ToString() + "." + ver.Build.ToString() + "." + ver.Revision.ToString();
            }
        }

        bool EnableNotifications
        {
            get
            {
                bool? notificationsEnabled = Windows.Storage.ApplicationData.Current.LocalSettings.Values["NotificationsEnabled"] as bool?;
                if(notificationsEnabled == null)
                {
                    Windows.Storage.ApplicationData.Current.LocalSettings.Values["NotificationsEnabled"] = true;
                    return true;
                }
                else if (notificationsEnabled == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["NotificationsEnabled"] = value;
            }
        }

        bool IsInFocus
        {
            get
            {
                return (Windows.Storage.ApplicationData.Current.LocalSettings.Values["IsWindowInFocus"] as bool? == true);
            }
        }

        private void NotificationToggle_Toggled(object sender, RoutedEventArgs e)
        {
            this.EnableNotifications = NotificationToggle.IsOn;
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState != CoreWindowActivationState.Deactivated)
            {
                TitleBar.Background = new SolidColorBrush(Color.FromArgb(255, 0, 150, 136));
                TitleText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }
            else
            {
                TitleBar.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                TitleText.Foreground = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
            }

            SettingsButton.Foreground = TitleText.Foreground;
        }

        private void DisplayTitleBar(CoreApplicationViewTitleBar coreTitleBar, object args)
        {
            coreTitleBar.ExtendViewIntoTitleBar = coreTitleBar.IsVisible;
            if (coreTitleBar.IsVisible) { TitleBar.Height = coreTitleBar.Height; }
        }
    }
}
