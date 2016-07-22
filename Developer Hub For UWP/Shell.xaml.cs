﻿using Intense.Presentation;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Developer_Hub_For_UWP.Pages;
using Core.Network;
using Core.DataModel;
using Core.ViewModel;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.System;
using System.Net.Http;
using Windows.UI.Notifications;
using System;
using Windows.Data.Xml.Dom;
using Newtonsoft.Json;
using Windows.Networking.Connectivity;
using Windows.Storage;
using System.IO;

namespace Developer_Hub_For_UWP
{
    public sealed partial class Shell : UserControl
    {
        private bool isAltKeyPressed;
        private bool isControlKeyPressed;
        private ApplicationDataContainer _localSettings;

        public Shell()
        {
            _localSettings =ApplicationData.Current.LocalSettings;
            this.InitializeComponent();

            var applicationView = ApplicationView.GetForCurrentView();
            var titleBar = applicationView.TitleBar;
            titleBar.ButtonInactiveBackgroundColor = Colors.Black;
            titleBar.ButtonInactiveForegroundColor = Colors.White;
            titleBar.ButtonBackgroundColor = Colors.Black;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.InactiveBackgroundColor = Colors.Black;
            titleBar.InactiveForegroundColor = Colors.White;
            titleBar.BackgroundColor = Colors.Black;
            titleBar.ForegroundColor = Colors.White;

            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            var vm = new ShellViewModel();

            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = loader.GetString("Home"), PageType = typeof(Home) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = loader.GetString("UL"), PageType = typeof(Page2) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = loader.GetString("SMI"), PageType = typeof(Page1) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = loader.GetString("AG"), PageType = typeof(Page3) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = loader.GetString("LBI"), PageType = typeof(Page4) });

            vm.BottomItems.Add(new NavigationItem { Icon = "", DisplayName = loader.GetString("Settings"), PageType = typeof(SettingsPage) });
            vm.BottomItems.Add(new NavigationItem { Icon = "", DisplayName = loader.GetString("About"), PageType = typeof(AboutPage) });
            

            // select the first top item
            vm.SelectedItem = vm.TopItems.First();
            this.ViewModel = vm;
            //if new download
            if (!_localSettings.Containers.ContainsKey("Settings"))
            {
                ApplicationDataContainer container = _localSettings.CreateContainer("Settings", ApplicationDataCreateDisposition.Always);
                _localSettings.Containers["Settings"].Values["IsUpdatePopupDisabled"] = false;
                _localSettings.Containers["Settings"].Values["IsFonticonExtraFileDownloaded"] = false;
                _localSettings.Containers["Settings"].Values["Version"] = "020200";

                DelLegacyHistory();
                TransferToStorage();
            }
            //if update
            else if(_localSettings.Containers["Settings"].Values["Version"] != "020200")
            {
                _localSettings.Containers["Settings"].Values["Version"] = "020200";
                _localSettings.Containers["Settings"].Values["IsUpdatePopupDisabled"] = false;
            }
            // whether the internet is connected.
            var conetvty = NetworkInformation.GetInternetConnectionProfile().GetNetworkConnectivityLevel();
            bool PopIgnored = Convert.ToBoolean(_localSettings.Containers["Settings"].Values["IsUpdatePopupIgnored"]);
            bool PopupDisabled = Convert.ToBoolean(_localSettings.Containers["Settings"].Values["IsUpdatePopupDisabled"]);
            if (conetvty == NetworkConnectivityLevel.InternetAccess)
            {
                if (!PopupDisabled) CheckUpdate();
                UpdateInsidetenApi();
            }           

            this.Loaded += delegate { this.Focus(Windows.UI.Xaml.FocusState.Programmatic); };
        }

        private async void DelLegacyHistory()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await localFolder.CreateFileAsync("history_icon.log", CreationCollisionOption.OpenIfExists);
            await sampleFile.DeleteAsync();
        }
        public async void TransferToStorage()
        {
            // Cant await inside catch, but this works anyway
            StorageFile stopfile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Data/api.json"));
            await stopfile.CopyAsync(ApplicationData.Current.LocalFolder);
        }
        private async void UpdateInsidetenApi()
        {
            await UrlPhraser.ReachFile("http://insideten.xyz/api.json", Response.DownloadToLocalFolder);
        }
        private async void CheckUpdate()
        {
            string url = "http://ap.westudio.ml/sources/json/wecode-update.json";
#if DEBUG
            url = "http://ap.westudio.ml/sources/json/wecode-update-test.json";
#endif
            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(new Uri(url));

            var jsonString = await response.Content.ReadAsStringAsync();
            Update.RootObject data = JsonConvert.DeserializeObject<Update.RootObject>(jsonString);
            string version = data.version;
            string[] versionnum = version.Split('.');
            int versioncount = Convert.ToInt32(versionnum[0]) * 10000 + Convert.ToInt32(versionnum[1]) * 100 + Convert.ToInt32(versionnum[2]);
            if (versioncount > 20200)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                HttpResponseMessage detailstring;
                switch (loader.GetString("nr_lan"))
                {
                    case "en":
                        detailstring = await client.GetAsync(new Uri(data.detail.en));
                        break;
                    case "zh-hans":
                         detailstring = await client.GetAsync(new Uri(data.detail.zh_hans));
                        break;
                    case "zh-hant":
                         detailstring = await client.GetAsync(new Uri(data.detail.zh_hant));
                        break;
                    default:
                        detailstring = await client.GetAsync(new Uri(data.detail.en));
                        break;
                }

                string detailstringin = await detailstring.Content.ReadAsStringAsync();
                string xmlContent = string.Empty;
                XmlDocument xdoc = new XmlDocument();
                xmlContent = string.Format(
                    "<toast>" +
                        "<visual>" +
                            "<binding template = 'ToastGeneric'>" +
                                   "<image placement = 'appLogoOverride' src = '' />" +
                                   "<text> {0} {1} {2}</text>" +
                                    "<text>{3}</text>" +
                                    "<image  placement = 'hero' src = 'Assets/new-ver.png' />" +
                            "</binding>" +
                        "</visual>" +
                        "<actions>" +
                            "<action" +
                             " content = '{4}'" +
                             " activationType='protocol'" +
                             " arguments = 'ms-windows-store://pdp/?ProductId=9nblggh5p90f' />" +
                             "<action" +
                             " content = '{5}'" +
                             " arguments = 'action=disableNoti' />" +
                             "<action" +
                             " content = '{6}'" +
                             " activationType='system'" +
                             " arguments = 'dismiss' />" +
                         "</actions>" +
                    "</toast>",
                     loader.GetString("nr_1"), version, loader.GetString("nr_2"), detailstringin, loader.GetString("nr_3"), loader.GetString("nr_5"), loader.GetString("nr_4")
                );
                xdoc.LoadXml(xmlContent);
                ToastNotification toast1 = new ToastNotification(xdoc);
                ToastNotificationManager.CreateToastNotifier().Show(toast1);
            }
        }

        public ShellViewModel ViewModel { get; private set; }

        public Frame RootFrame
        {
            get
            {
                return this.Frame;
            }
        }

        private void Grid_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Menu) isAltKeyPressed = true;
            else if (isAltKeyPressed)
            {
                switch (e.Key)
                {
                    case VirtualKey.H: Frame.Navigate(typeof(Home), Frame); break;
                    case VirtualKey.U: Frame.Navigate(typeof(Page2), Frame); break;
                    case VirtualKey.F: Frame.Navigate(typeof(Page1), Frame); break;
                    case VirtualKey.A: Frame.Navigate(typeof(Page3), Frame); break;
                    case VirtualKey.B: Frame.Navigate(typeof(Page4), Frame); break;      
                }
            }
            if (e.Key == VirtualKey.Control) isControlKeyPressed = true;
            else if (isControlKeyPressed)
            {
                switch (e.Key)
                {
                    case VirtualKey.A: Frame.Navigate(typeof(AboutPage), Frame); break;
                    case VirtualKey.S: Frame.Navigate(typeof(SettingsPage), Frame); break;
                }
            }
        }
    }
}
