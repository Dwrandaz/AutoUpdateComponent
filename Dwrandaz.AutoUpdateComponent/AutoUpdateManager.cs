using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Dwrandaz.AutoUpdateComponent
{
    public sealed class AutoUpdateManager
    {
        private static readonly HttpClient _client;

        static AutoUpdateManager()
        {
            // Always download the latest version, don't cache anything
            var RootFilter = new HttpBaseProtocolFilter();
            RootFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            RootFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

            _client = new HttpClient(RootFilter);
        }

        /// <summary>
        /// Try to start the update process if an update is available. If an update is available, the app will be restarted.
        /// <param name="packageFamilyName">The family name of the package to be updated. Can be obtained through <see cref="Windows.ApplicationModel.Package.Current.Id.FamilyName"/></param>
        /// <param name="updatePackageLocation">The full path of the .appxbundle file on the server.</param>
        /// <returns></returns>
        public static IAsyncOperation<UpdateResult> TryToUpdateAsync(UpdateInfo updateInfo)
        {
            return TryToUpdateAsync(updateInfo, Constants.DwrandazUpdaterAppService);
        }

        /// <summary>
        /// Try to start the update process if an update is available. If an update is available, the app will be restarted.
        /// <param name="packageFamilyName">The family name of the package to be updated. Can be obtained through <see cref="Windows.ApplicationModel.Package.Current.Id.FamilyName"/></param>
        /// <param name="updatePackageLocation">The full path of the .appxbundle file on the server.</param>
        /// <param name="updaterServiceName">The name of the background service that's going to perform the update process. This parameter is optional.</param>
        /// <returns></returns>
        public static IAsyncOperation<UpdateResult> TryToUpdateAsync(
            UpdateInfo updateInfo,
            string updaterServiceName
            )
        {
            return AsyncInfo.Run(async token =>
                await TryToUpdateAsyncImpl(token, updateInfo, updaterServiceName));
        }

        // This method is here because of some limitations regarding to WinRT components.
        // See: https://docs.microsoft.com/en-us/windows/uwp/winrt-components//creating-windows-runtime-components-in-csharp-and-visual-basic#asynchronous-operations
        private static async Task<UpdateResult> TryToUpdateAsyncImpl(
            CancellationToken token,
            UpdateInfo updateInfo,
            string updaterServiceName
            )
        {
            try
            {
                // Make sure the update is newer than the current version
                if (!updateInfo.ShouldUpdate)
                {
                    throw new ArgumentException("The app is already up-to-date.");
                }

                // Set up a connection to the BackgroundTask
                var updaterService = new AppServiceConnection
                {
                    PackageFamilyName = Package.Current.Id.FamilyName,
                    AppServiceName = updaterServiceName
                };

                token.ThrowIfCancellationRequested();

                var status = await updaterService.OpenAsync();
                if (status != AppServiceConnectionStatus.Success)
                {
                    return new UpdateResult($"Couldn't communicate with the Updater Service. Status: {Enum.GetName(typeof(AppServiceConnectionStatus), status)}");
                }

                token.ThrowIfCancellationRequested();

                var message = new ValueSet
                {
                    { Constants.UpdatePackageLocation, updateInfo.MainBundleUrl },
                };

                AppServiceResponse response = await updaterService.SendMessageAsync(message);

                if (response.Status == AppServiceResponseStatus.Success)
                {
                    if (response.Message.TryGetValue(Constants.ErrorMessage, out var error))
                    {
                        return new UpdateResult(error as string);
                    }
                    else
                    {
                        return new UpdateResult();
                    }
                }
                else if (response.Status == AppServiceResponseStatus.Failure)
                {
                    // If the app gets updated, the status of the response will be a failure
                    return new UpdateResult();
                }
                else
                {
                    return new UpdateResult($"Could not communicate with the Updater Service. Status: {Enum.GetName(typeof(AppServiceResponseStatus), response.Status)}");
                }
            }
            catch (Exception ex)
            {
                return new UpdateResult(ex.Message);
            }
        }

        /// <summary>
        /// Checks the server for updates and returns information about any possible updates.
        /// </summary>
        /// <param name="appinstallerUrl">The url for the .appinstaller file on the server</param>
        /// <returns></returns>
        public static IAsyncOperation<UpdateInfo> CheckForUpdatesAsync(string appinstallerUrl)
        {
            return AsyncInfo.Run(async _ => await CheckForUpdatesAsyncImpl(appinstallerUrl));
        }

        // This method is here because of some limitations regarding to WinRT components.
        // See: https://docs.microsoft.com/en-us/windows/uwp/winrt-components//creating-windows-runtime-components-in-csharp-and-visual-basic#asynchronous-operations
        private static async Task<UpdateInfo> CheckForUpdatesAsyncImpl(string appinstallerUrl)
        {
            try
            {
                using (var response = await _client.GetAsync(new Uri(appinstallerUrl)))
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        return new UpdateInfo($"Couldn't download the appinstaller file: {Enum.GetName(typeof(HttpStatusCode), response.StatusCode)}.");
                    }

                    // For more details about the .appinstaller xml schema:
                    // https://docs.microsoft.com/en-us/uwp/schemas/appinstallerschema/schema-root
                    var document = new XmlDocument();
                    document.LoadXml(content);

                    var root = document.DocumentElement;
                    var installerUri = root.GetAttribute("Uri");
                    var version = root.GetAttribute("Version");

                    var mainBundle = root["MainBundle"];
                    var bundleName = mainBundle.GetAttribute("Name");
                    var bundleVersion = mainBundle.GetAttribute("Version");
                    var bundleUri = mainBundle.GetAttribute("Uri");

                    // Make sure the update is for the current package
                    if (bundleName != Package.Current.Id.Name)
                        return new UpdateInfo("Found an update, but it's not for the current package.");

                    var shouldUpdate = IsNewer(GetCurrentVersion(), bundleVersion);

                    return new UpdateInfo(bundleName, bundleUri, bundleVersion, shouldUpdate);
                }
            }
            catch (ArgumentNullException)
            {
                return new UpdateInfo("Invalid appinstaller package.");
            }
            catch (NullReferenceException)
            {
                return new UpdateInfo("Invalid appinstaller package.");
            }
            catch (Exception e)
            {
                return new UpdateInfo(e.Message);
            }
        }

        private static bool IsNewer(string currentVersion, string onlineVersion)
        {
            return new Version(currentVersion) < new Version(onlineVersion.Trim());
        }

        private static string GetCurrentVersion()
        {
            PackageVersion version = Package.Current.Id.Version;

            return string.Format($"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
        }
    }
}