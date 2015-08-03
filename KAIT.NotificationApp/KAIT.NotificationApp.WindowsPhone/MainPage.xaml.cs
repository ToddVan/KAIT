using Microsoft.Band;
using Microsoft.Band.Notifications;
using Microsoft.Band.Tiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace KAIT.NotificationApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [System.Runtime.InteropServices.GuidAttribute("E076E166-36C4-4ACC-A2C0-41C6925BFF35")]
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        CoreDispatcher dispatcher;
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            Current = this;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }



        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            App.Channel.PushNotificationReceived += Channel_PushNotificationReceived;

            var json = e.Parameter as string;
            UpdateDeviceText(json);

            CheckForBand();
        }

        private async void CheckForBand()
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Paired") && 
                ((bool) ApplicationData.Current.LocalSettings.Values["Paired"])) 
                    return;

            var pairedBands = await BandClientManager.Instance.GetBandsAsync();
            if (pairedBands.Length > 0)
                bandRegistration.Visibility = Windows.UI.Xaml.Visibility.Visible;
           
        }

        private void UpdateDeviceText(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<PushNotificationMessage>(json);
                    textItemName.Text = msg.item;
                }
                catch (Exception ex)
                {
                    textItemName.Text = "Error: " + json;
                }

            }
            else
                textItemName.Text = "Uknown Request";
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.Channel.PushNotificationReceived -= Channel_PushNotificationReceived;
            base.OnNavigatedFrom(e);
        }

        async void Channel_PushNotificationReceived(Windows.Networking.PushNotifications.PushNotificationChannel sender, Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs args)
        {
            if (args.NotificationType == Windows.Networking.PushNotifications.PushNotificationType.Toast)
            {
                var root = args.ToastNotification.Content.DocumentElement;
                var launch = root.GetAttribute("launch");

                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateDeviceText(launch);
                });
            }
        }

        private async void bandRegistration_Click(object sender, RoutedEventArgs e)
        {
            var pairedBands = await BandClientManager.Instance.GetBandsAsync();
            using (IBandClient bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
            {
                // Create a Tile.
                Guid myTileId = new Guid("9DE3027F-287B-43AB-B0AA-84D80116FC7E");
                BandTile myTile = new BandTile(myTileId)
                {
                    Name = "KAIT",
                    TileIcon = await LoadIcon("ms-appx:///Assets/BandTileIconLarge.png"),
                    SmallIcon = await LoadIcon("ms-appx:///Assets/BandTileIconSmall.png")
                };
                await bandClient.TileManager.AddTileAsync(myTile);

                // Send a notification.
                await bandClient.NotificationManager.SendMessageAsync(myTileId, "KAIT", "Connected To Phone", DateTimeOffset.Now, MessageFlags.ShowDialog);
                ApplicationData.Current.LocalSettings.Values["Paired"] = true;
                bandRegistration.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private async Task<BandIcon> LoadIcon(string uri)
        {
            StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

            using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                await bitmap.SetSourceAsync(fileStream);
                return bitmap.ToBandIcon();
            }
        }

        private void bandRegistration_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
