using System;

namespace Dwrandaz.AutoUpdateComponent
{
    public class UpdatePackageInfo : PackageInfo
    {
        internal UpdatePackageInfo(string errorMessage) : base(errorMessage)
        {
        }

        internal UpdatePackageInfo(string name, string url, string version, bool shouldUpdate) : base(name, url, version)
        {
            ShouldUpdate = shouldUpdate;
        }

        /// <summary>
        /// Gets whether you should call <see cref="AutoUpdateManager.TryToUpdateAsync"/>
        /// </summary>
        public bool ShouldUpdate { get; set; }
    }

    /// <summary>
    /// Represents the result of an update check.
    /// </summary>
    public class PackageInfo
    {
        internal PackageInfo(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        internal PackageInfo(string name, string url, string version)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(version))
                throw new ArgumentNullException();

            Succeeded = true;
            MainBundleUrl = url;
            MainBundleVersion = version;
            MainBundleName = name;
        }

        /// <summary>
        /// Gets whether the update check succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// If the update check failed, returns the error message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the url for the mainbundle package.
        /// </summary>
        public string MainBundleUrl { get; }

        /// <summary>
        /// Gets the main package version.
        /// </summary>
        public string MainBundleVersion { get; }

        /// <summary>
        /// Gets the main package name.
        /// </summary>
        public string MainBundleName { get; set; }

    }
}