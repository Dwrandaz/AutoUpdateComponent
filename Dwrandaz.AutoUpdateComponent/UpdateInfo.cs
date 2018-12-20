using System;

namespace Dwrandaz.AutoUpdateComponent
{
    /// <summary>
    /// Represents the result of an update check.
    /// </summary>
    public sealed class UpdateInfo
    {
        internal UpdateInfo(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        internal UpdateInfo(string name, string url, string version, bool shouldUpdate)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(version))
                throw new ArgumentNullException();

            Succeeded = true;
            MainBundleUrl = url;
            MainBundleVersion = version;
            MainBundleName = name;
            ShouldUpdate = shouldUpdate;
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

        /// <summary>
        /// Gets whether you should call <see cref="AutoUpdateManager.TryToUpdateAsync"/>
        /// </summary>
        public bool ShouldUpdate { get; set; }
    }
}