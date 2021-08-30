
namespace Microsoft.Git.CredentialManager.UI.ViewModels.Validation
{
    public class PropertyValidationResult
    {
        public static readonly PropertyValidationResult Success = new PropertyValidationResult(ValidationStatus.Valid);
        public static readonly PropertyValidationResult Unvalidated = new PropertyValidationResult(ValidationStatus.Unvalidated);

        /// <summary>
        /// Describes if the property passes validation
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Describes which state we are in - Valid, Not Validated, or Invalid
        /// </summary>
        public ValidationStatus Status { get; }

        /// <summary>
        /// An error message to display
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Describes if we should show this error in the UI We only show errors which have been
        /// marked specifically as Invalid and we do not show errors for inputs which have not yet
        /// been validated.
        /// </summary>
        public bool DisplayValidationError { get; }

        public PropertyValidationResult(ValidationStatus validationStatus, string message = null)
        {
            Status = validationStatus;
            IsValid = validationStatus == ValidationStatus.Valid;
            DisplayValidationError = validationStatus == ValidationStatus.Invalid;
            Message = message ?? string.Empty;
        }
    }
}
