using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Dwrandaz.AutoUpdateComponent;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            var version = Package.Current.Id.Version;
            versionTextBlock.Text = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            statusTextBlock.Text = "Getting the latest update information...";

            var path = "http://localhost:5000/install/SampleApp.appinstaller";
            var info = await AutoUpdateManager.CheckForUpdatesAsync(path);
            if (!info.Succeeded)
            {
                statusTextBlock.Text = info.ErrorMessage;
                return;
            }

            if (!info.ShouldUpdate)
            {
                statusTextBlock.Text = "This app is already up-to-date :)";
                return;
            }

            // You can use info.MainBundleVersion to get the update version
            statusTextBlock.Text = $"New version: {info.MainBundleVersion}";

            var result = await AutoUpdateManager.TryToUpdateAsync(info);
            if (!result.Succeeded)
            {
                statusTextBlock.Text = result.ErrorMessage;
                return;
            }

            statusTextBlock.Text = $"Success! The app will be restarted soon, see you later!";
        }
    }
}
