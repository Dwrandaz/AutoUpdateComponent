using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Dwrandaz.AutoUpdateComponent
{
    internal enum DeployOperation
    {
        Install,
        Update
    }

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


        public static IAsyncOperation<DeployResult> RemoveApp(string fullName)
        {
            return AsyncInfo.Run(token => RemoveAppAsync(token, fullName, Constants.DwrandazUpdaterAppService));
        }

        private static async Task<DeployResult> RemoveAppAsync(
     CancellationToken token,
     string fullName,
     string updaterServiceName
     )
        {
            try
            {
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
                    return new DeployResult($"Couldn't communicate with the Updater Service. Status: {Enum.GetName(typeof(AppServiceConnectionStatus), status)}");
                }

                token.ThrowIfCancellationRequested();

                var message = new ValueSet
                {
                    { Constants.Verb, Constants.RemoveVerb },
                    { Constants.PackageId, fullName },
                };

                AppServiceResponse response = await updaterService.SendMessageAsync(message);

                if (response.Status == AppServiceResponseStatus.Success)
                {
                    if (response.Message.TryGetValue(Constants.ErrorMessage, out var error))
                    {
                        return new DeployResult(error as string);
                    }
                    else
                    {
                        return new DeployResult();
                    }
                }
                else if (response.Status == AppServiceResponseStatus.Failure)
                {
                    // If the app gets updated, the status of the response will be a failure
                    return new DeployResult();
                }
                else
                {
                    return new DeployResult($"Could not communicate with the Updater Service. Status: {Enum.GetName(typeof(AppServiceResponseStatus), response.Status)}");
                }
            }
            catch (Exception ex)
            {
                return new DeployResult(ex.Message);
            }
        }

        /// <summary>
        /// Tries to install an app.
        /// </summary>
        /// <param name="updateInfo">The package information, can be obtained by calling <see cref="GetPackageInfoAsync(string)"/></param>
        /// <returns></returns>
        public static IAsyncOperationWithProgress<DeployResult, uint> TryToInstall(PackageInfo updateInfo)
        {
            return TryToInstall(updateInfo, Constants.DwrandazUpdaterAppService);
        }

        /// <summary>
        /// ries to install an app.
        /// </summary>
        /// <param name="updateInfo">The package information,, can be obtained by calling <see cref="GetPackageInfoAsync(string)"/></param>
        /// <param name="updaterServiceName">The name of the background service that's going to perform the install process. This parameter is optional.</param>
        /// <returns></returns>
        public static IAsyncOperationWithProgress<DeployResult, uint> TryToInstall(
            PackageInfo updateInfo,
            string updaterServiceName
            )
        {
            return AsyncInfo.Run<DeployResult, uint>((ct, progress) =>
                DeployAsync(ct, updateInfo, updaterServiceName, DeployOperation.Install, progress));
        }


        /// <summary>
        /// Tries to update and then restart the app. Should only be called if <see cref="PackageInfo.ShouldUpdate"/> is true.
        /// </summary>
        /// <param name="updateInfo">Next update information, can be obtained by calling <see cref="GetPackageInfoAsync(string)"/></param>
        /// <returns></returns>
        public static IAsyncOperationWithProgress<DeployResult, uint> TryToUpdateAsync(UpdatePackageInfo updateInfo)
        {
            return TryToUpdateAsync(updateInfo, Constants.DwrandazUpdaterAppService);
        }

        /// <summary>
        /// Tries to update and then restart the app. Should only be called if <see cref="PackageInfo.ShouldUpdate"/> is true.
        /// </summary>
        /// <param name="updateInfo">Next update information, can be obtained by calling <see cref="GetPackageInfoAsync(string)"/></param>
        /// <param name="updaterServiceName">The name of the background service that's going to perform the update process. This parameter is optional.</param>
        /// <returns></returns>
        public static IAsyncOperationWithProgress<DeployResult, uint> TryToUpdateAsync(
            UpdatePackageInfo updateInfo,
            string updaterServiceName
            )
        {
            return AsyncInfo.Run<DeployResult, uint>((ct, progress) =>
                DeployAsync(ct, updateInfo, updaterServiceName, DeployOperation.Update, progress));
        }

        private static async Task<DeployResult> DeployAsync(
            CancellationToken token,
            PackageInfo updateInfo,
            string updaterServiceName,
            DeployOperation operation,
            IProgress<uint> progress = default
            )
        {
            AppServiceConnection updaterService = null;

            try
            {
                // Make sure the update is newer than the current version
                if (operation == DeployOperation.Update && (updateInfo as UpdatePackageInfo).ShouldUpdate == false)
                {
                    throw new ArgumentException("The app is already up-to-date.");
                }

                // Set up a connection to the BackgroundTask
                updaterService = new AppServiceConnection
                {
                    PackageFamilyName = Package.Current.Id.FamilyName,
                    AppServiceName = updaterServiceName
                };

                token.ThrowIfCancellationRequested();

                var status = await updaterService.OpenAsync();
                if (status != AppServiceConnectionStatus.Success)
                {
                    return new DeployResult($"Couldn't communicate with the Updater Service. Status: {Enum.GetName(typeof(AppServiceConnectionStatus), status)}");
                }

                token.ThrowIfCancellationRequested();

                var message = new ValueSet
                {
                    { Constants.Verb, operation == DeployOperation.Update ? Constants.UpdateVerb : Constants.InstallVerb },
                    { Constants.PackageLocation, updateInfo.MainBundleUrl },
                };

                updaterService.RequestReceived += ReportProgress;

                AppServiceResponse response = await updaterService.SendMessageAsync(message);

                if (response.Status == AppServiceResponseStatus.Success)
                {
                    if (response.Message.TryGetValue(Constants.ErrorMessage, out var error))
                    {
                        return new DeployResult(error as string);
                    }
                    else
                    {
                        return new DeployResult();
                    }
                }
                else if (response.Status == AppServiceResponseStatus.Failure)
                {
                    // If the app gets updated, the status of the response will be a failure
                    return new DeployResult();
                }
                else
                {
                    return new DeployResult($"Could not communicate with the Updater Service. Status: {Enum.GetName(typeof(AppServiceResponseStatus), response.Status)}");
                }
            }
            catch (Exception ex)
            {
                return new DeployResult(ex.Message);
            }
            finally
            {
                if (updaterService != null)
                    updaterService.RequestReceived -= ReportProgress;
            }

            void ReportProgress(AppServiceConnection s, AppServiceRequestReceivedEventArgs e)
            {
                {
                    if (e.Request.Message.TryGetValue(Constants.DeploymentProgress, out var p))
                    {
                        progress.Report((uint)p);
                    }
                };
            }
        }

        /// <summary>
        /// Checks the server for updates and returns information about any possible updates.
        /// </summary>
        /// <param name="appinstallerUrl">The url for the .appinstaller file on the server</param>
        /// <returns></returns>
        public static IAsyncOperation<UpdatePackageInfo> CheckForUpdatesAsync(string appinstallerUrl)
        {
            return AsyncInfo.Run(_ => CheckForUpdatesAsyncImpl(appinstallerUrl));
        }

        /// <summary>
        /// Checks the server for updates and returns information about any possible updates.
        /// </summary>
        /// <param name="appinstallerUrl">The url for the .appinstaller file on the server</param>
        /// <returns></returns>
        public static IAsyncOperation<PackageInfo> GetPackageInfoAsync(string appinstallerUrl)
        {
            return AsyncInfo.Run(_ => GetPackageInfoAsyncImpl(appinstallerUrl));
        }

        public static string GetCurrentPackageFullName()
        {
            return Package.Current.Id.FullName;
        }

        private static async Task<UpdatePackageInfo> CheckForUpdatesAsyncImpl(string appinstallerUrl)
        {
            var packageInfo = await CheckForUpdatesAsyncImpl(appinstallerUrl);

            // Make sure the update is for the current package
            if (packageInfo.MainBundleName != Package.Current.Id.Name)
                return new UpdatePackageInfo("Found an update, but it's not for the current package.");

            var shouldUpdate = IsNewer(GetCurrentVersion(), packageInfo.MainBundleVersion);

            return new UpdatePackageInfo(
                packageInfo.MainBundleName,
                packageInfo.MainBundleUrl,
                packageInfo.MainBundleVersion,
                packageInfo.ShouldUpdate);
        }

        // This method is here because of some limitations regarding to WinRT components.
        // See: https://docs.microsoft.com/en-us/windows/uwp/winrt-components//creating-windows-runtime-components-in-csharp-and-visual-basic#asynchronous-operations
        private static async Task<PackageInfo> GetPackageInfoAsyncImpl(string appinstallerUrl)
        {
            try
            {
                using (var response = await _client.GetAsync(new Uri(appinstallerUrl)))
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        return new PackageInfo($"Couldn't download the appinstaller file: {Enum.GetName(typeof(HttpStatusCode), response.StatusCode)}.");
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

                    return new PackageInfo(bundleName, bundleUri, bundleVersion);
                }
            }
            catch (ArgumentNullException)
            {
                return new PackageInfo("Invalid appinstaller package.");
            }
            catch (NullReferenceException)
            {
                return new PackageInfo("Invalid appinstaller package.");
            }
            catch (Exception e)
            {
                return new PackageInfo(e.Message);
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