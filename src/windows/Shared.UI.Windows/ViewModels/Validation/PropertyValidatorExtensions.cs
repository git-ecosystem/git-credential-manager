using System;
using System.Security;

namespace Microsoft.Git.CredentialManager.UI.ViewModels.Validation
{
    public static class PropertyValidatorExtensions
    {
        public static PropertyValidator<string> Required(this PropertyValidator<string> validator, string errorMessage)
        {
            return validator.ValidIfTrue(value => !string.IsNullOrEmpty(value), errorMessage);
        }

        public static PropertyValidator<SecureString> Required(this PropertyValidator<SecureString> validator, string errorMessage)
        {
            return validator.ValidIfTrue(value => value is null ? (bool?) null : value.Length > 0, errorMessage);
        }

        public static PropertyValidator<TProperty> ValidIfTrue<TProperty>(
            this PropertyValidator<TProperty> validator,
            Func<TProperty, bool?> predicate,
            string errorMessage)
        {
            validator.AddRule(value =>
            {
                bool? result = predicate(value);
                if (result.HasValue)
                {
                    return result.Value
                        ? PropertyValidationResult.Success
                        : new PropertyValidationResult(ValidationStatus.Invalid, errorMessage);
                }

                return PropertyValidationResult.Unvalidated;
            });

            return validator;
        }
    }
}
