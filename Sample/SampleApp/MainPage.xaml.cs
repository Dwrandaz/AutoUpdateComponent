using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Dwrandaz.AutoUpdateComponent;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;
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

            var path = "https://installgreatapp.azurewebsites.net/SampleApp.appinstaller";
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

            var progress = new Progress<uint>();
            progress.ProgressChanged += (s, args) =>
            {
                Debug.WriteLine(args);
                logTextBox.Text += args + "\n";
            };

            var result = await AutoUpdateManager.TryToUpdateAsync(info).AsTask(progress);
            if (!result.Succeeded)
            {
                statusTextBlock.Text = result.ErrorMessage;
                return;
            }

            statusTextBlock.Text = $"Success! The app will be restarted soon, see you later!";
        }

        private async void removeButton_Click(object sender, RoutedEventArgs e)
        {
            var fullName = AutoUpdateManager.GetCurrentPackageFullName();
            await AutoUpdateManager.RemoveApp(fullName);
        }

        private async void installButton_Click(object sender, RoutedEventArgs e)
        {
            var path = "https://installgreatapp.azurewebsites.net/SampleApp2.appinstaller";
            var info = await AutoUpdateManager.GetPackageInfoAsync(path);
            if (!info.Succeeded)
            {
                statusTextBlock.Text = info.ErrorMessage;
                return;
            }

            // You can use info.MainBundleVersion to get the update version
            statusTextBlock.Text = $"Package: {info.MainBundleName} {info.MainBundleVersion}";

            var progress = new Progress<uint>();
            progress.ProgressChanged += (s, args) =>
            {
                Debug.WriteLine(args);
                logTextBox.Text += args + "\n";
            };

            var result = await AutoUpdateManager.TryToInstall(info).AsTask(progress);
            if (!result.Succeeded)
            {
                statusTextBlock.Text = result.ErrorMessage;
                return;
            }

            statusTextBlock.Text = $"Success!";
        }
    }
}
