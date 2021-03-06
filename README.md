# Dwrandaz.AutoUpdateComponent

﻿This is an auto-update mechanism for UWP apps, it uses a background task to update the package.

## How to use
1. Install the nuget package: [`Dwrandaz.AutoUpdateComponent`](http://nuget.org/packages/Dwrandaz.AutoUpdateComponent)
2. Set minimum version of the app to `1803`
3. Open the package manifest `.appmanifest` file of the main app and declare an app service:
   - Name: The default values is `Dwrandaz.AutoUpdate`. However, you can change it to any name you like but you should note that this name is important and it should be passed to `AutoUpdateManager.TryToUpdateAsync` if you don't use the default name.
   - Entry point: `Dwrandaz.AutoUpdateComponent.UpdateTask`
4. Right click on the package manifest `.appmanifest` file and click on `View Code`.
5. Add this namespace declaration: `xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"`
6. Add `rescap` to the `IgnorableNamespaces`, for example: `IgnorableNamespaces="uap mp rescap"`
7. Inside the `Package` tag, make sure these elements exist:

```xml
<Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="packageManagement" />
</Capabilities>
```
For more information, take a look at the [sample apps manifest file](https://github.com/Dwrandaz/AutoUpdateComponent/blob/master/Sample/SampleApp/Package.appxmanifest).

8. Example usage:
```csharp
var path = "http://localhost:5000/install/AwesomeApp.appinstaller";
var info = await AutoUpdateManager.CheckForUpdatesAsync(path);
if (!info.Succeeded)
{
    // There was an error in getting the update information from the server
    // use info.ErrorMessage to get the error message
    return;
}

if (!info.ShouldUpdate)
{
    // The app is already up-to-date :)
    return;
}

// You can use info.MainBundleVersion to get the update version

var result = await AutoUpdateManager.TryToUpdateAsync(info);
if (!result.Succeeded)
{
    // There was an error in updating the app
    // use result.ErrorMessage to get the error message
    return;
}

// Success! The app was updated, it will restart soon!
```

See the [Sample app](https://github.com/Dwrandaz/AutoUpdateComponent/blob/master/Sample/SampleApp/MainPage.xaml.cs#L35) for a concrete example.

## Creating update packages

1. Make sure you select the `Release` configuration
2. Right click on the main app project and click `Store` > `Create App Packages...`
3. Select `I want to create packages for sideloading.`And check the `Enable automatic updates` checkbox
4. Click on `Next`
5. Check the `Automatically Incremenent` checkbox under `version`.
6. Select `Always` under `Generate App bundle`
7. Click on `Next`
8. Write the update location path and Select `Check every 1 Week` or more so that the native auto-update mechanism doesn't mess with our auto-update mechanism
9. Click on `Create`

## Configuring the IIS server

- https://docs.microsoft.com/en-us/windows/uwp/packaging/web-install-iis

## Troubleshooting
 - Before using the library, make sure your web app (that's serving the update package) works correctly. To do this, try to manually update the app by going to your web app and clicking on install and then clicking on update. This will help you avoid a lot of head ache.
 - Configuring your web app can take quite some time. Instead use a free [Azure Web App](https://docs.microsoft.com/en-us/azure/app-service-web/) and upload your package via FTP. Azure Blob Storage works great too. It's also much faster than Azure App Service.
 - Kiosk Mode Problems: take a look at #2.
If you have any other tips, let me know or better yet send a pull request :)

## More information
- https://mazeez.dev/posts/update-sideloaded-uwp
- https://mazeez.dev/posts/uwp-devops
- https://mazeez.dev/posts/update-sideloaded-uwp-2

- https://matthijs.hoekstraonline.net/2016/09/27/auto-updater-for-my-side-loaded-uwp-apps/
- http://blog.infernored.com/how-to-push-updates-to-raspberry-pi-uwp-apps-in-prod
- https://channel9.msdn.com/Shows/Inside-Windows-Platform/Exposing-and-Calling-App-Services-from-your-UWP-app
- https://github.com/AutomatedArchitecture/sirenofshame-uwp/blob/develop/SirenOfShame.Uwp.Maintenance/Services/BundleService.cs
