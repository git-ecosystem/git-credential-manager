// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace GitHub.UI.ViewModels.Validation
{
    public class PropertyValidationResult
    {
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

        public static PropertyValidationResult Success { get; } = new PropertyValidationResult(ValidationStatus.Valid);

        public static PropertyValidationResult Unvalidated { get; } = new PropertyValidationResult();

        public PropertyValidationResult() : this(ValidationStatus.Unvalidated, "")
        {
        }

        public PropertyValidationResult(ValidationStatus validationStatus) : this(validationStatus, "")
        {
        }

        public PropertyValidationResult(ValidationStatus validationStatus, string message)
        {
            Status = validationStatus;
            IsValid = validationStatus == ValidationStatus.Valid;
            DisplayValidationError = validationStatus == ValidationStatus.Invalid;
            Message = message;
        }
    }
}
