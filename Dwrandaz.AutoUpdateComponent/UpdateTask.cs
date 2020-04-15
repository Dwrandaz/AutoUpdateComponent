using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;

namespace Dwrandaz.AutoUpdateComponent
{
    // Related resources: 
    // - https://matthijs.hoekstraonline.net/2016/09/27/auto-updater-for-my-side-loaded-uwp-apps/
    // - http://blog.infernored.com/how-to-push-updates-to-raspberry-pi-uwp-apps-in-prod
    // - https://channel9.msdn.com/Shows/Inside-Windows-Platform/Exposing-and-Calling-App-Services-from-your-UWP-app
    // - https://github.com/AutomatedArchitecture/sirenofshame-uwp/blob/develop/SirenOfShame.Uwp.Maintenance/Services/BundleService.cs

    public sealed class UpdateTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _serviceDeferral;
        private AppServiceTriggerDetails _details;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Take a service deferral so the service isn't terminated
            _serviceDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnTaskCanceled;

            // Listen for incoming app service requests
            _details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            _details.AppServiceConnection.RequestReceived += OnRequestReceived;
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (_serviceDeferral != null)
            {
                // Complete the service deferral
                _serviceDeferral.Complete();
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral so we can use an awaitable API to respond to the message
            var messageDeferral = args.GetDeferral();

            ValueSet message = args.Request.Message;

            try
            {
                var verb = message[Constants.Verb] as string;
                PackageManager manager = new PackageManager();

                if (verb == Constants.UpdateVerb)
                {
                    string packageLocation = message[Constants.PackageLocation] as string;
                    await DeployApp(args, packageLocation, manager, DeployOperation.Update);
                }
                else if (verb == Constants.InstallVerb)
                {
                    string packageLocation = message[Constants.PackageLocation] as string;
                    await DeployApp(args, packageLocation, manager, DeployOperation.Install);
                }
                else if (verb == Constants.RemoveVerb)
                {
                    string removePackageId = message[Constants.PackageId] as string;
                    await RemoveApp(args, removePackageId, manager);
                }
            }
            catch (Exception e)
            {
                await args.Request.SendResponseAsync(new ValueSet { { Constants.ErrorMessage, e.Message } });
            }
            finally
            {
                // Complete the message deferral so the platform knows we're done responding
                messageDeferral.Complete();
            }
        }

        private async Task RemoveApp(AppServiceRequestReceivedEventArgs args, string removePackageId, PackageManager manager)
        {
            var result = await manager.RemovePackageAsync(removePackageId);
            if (!string.IsNullOrEmpty(result.ErrorText))
            {
                await args.Request.SendResponseAsync(new ValueSet { { Constants.ErrorMessage, result.ErrorText } });
            }
            else
            {
                await args.Request.SendResponseAsync(new ValueSet { { Constants.Success, true } });
            }
        }

        private async Task DeployApp(AppServiceRequestReceivedEventArgs args, string packageLocation, PackageManager manager, DeployOperation operation)
        {
            var deploymentOpations = DeploymentOptions.ForceApplicationShutdown;
            var uri = new Uri(packageLocation);

            var volume = manager.FindPackageVolumes()
                .FirstOrDefault(v => v.IsAppxInstallSupported && v.IsFullTrustPackageSupported);

            if (volume is null)
            {
                throw new InvalidOperationException("Could not find a volume to install the package on.");
            }

            // https://github.com/colinkiama/UWP-Package-Installer/blob/master/installTask/install.cs
            var deploymentOperation = operation == DeployOperation.Install ?
                manager.AddPackageAsync(uri, new List<Uri>(), DeploymentOptions.ForceApplicationShutdown) :
                manager.UpdatePackageAsync(uri, null, deploymentOpations);

            var progress = new Progress<DeploymentProgress>();
            progress.ProgressChanged += async (s, e) =>
            {
                await _details.AppServiceConnection.SendMessageAsync(new ValueSet
                {
                    { Constants.DeploymentProgress, e.percentage },
                });
            };

            var result = await deploymentOperation.AsTask(progress);

            if (!string.IsNullOrEmpty(result.ErrorText))
            {
                await args.Request.SendResponseAsync(new ValueSet { { Constants.ErrorMessage, result.ErrorText } });
            }
            else
            {
                await args.Request.SendResponseAsync(new ValueSet { { Constants.Success, true } });
            }
        }
    }
}