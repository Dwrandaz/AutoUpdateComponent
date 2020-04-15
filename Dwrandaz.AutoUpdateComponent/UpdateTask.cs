using System;
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

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Take a service deferral so the service isn't terminated
            _serviceDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnTaskCanceled;

            // Listen for incoming app service requests
            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            details.AppServiceConnection.RequestReceived += OnRequestReceived;
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
                string packageLocation = message[Constants.UpdatePackageLocation] as string;

                // Try to update the app
                PackageManager manager = new PackageManager();
                var result = await manager.UpdatePackageAsync(new Uri(packageLocation), null, DeploymentOptions.ForceApplicationShutdown);

                if (!string.IsNullOrEmpty(result.ErrorText))
                {
                    await args.Request.SendResponseAsync(new ValueSet { { Constants.ErrorMessage, result.ErrorText } });
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
    }
}