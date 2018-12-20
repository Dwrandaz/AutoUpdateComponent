namespace Dwrandaz.AutoUpdateComponent
{
    /// <summary>
    /// The result of an update operation.
    /// </summary>
    public sealed class UpdateResult
    {
        public UpdateResult()
        {
            Succeeded = true;
        }

        public UpdateResult(string error)
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