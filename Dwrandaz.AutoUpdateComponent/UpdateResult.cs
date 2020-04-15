namespace Dwrandaz.AutoUpdateComponent
{
    /// <summary>
    /// The result of an update operation.
    /// </summary>
    public sealed class DeployResult
    {
        public DeployResult()
        {
            Succeeded = true;
        }

        public DeployResult(string error)
        {
            Succeeded = false;
            ErrorMessage = error;
        }

        /// <summary>
        /// Gets whether the update operation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// If the update operation failed, returns the error message.
        /// </summary>
        public string ErrorMessage { get; }
    }
}