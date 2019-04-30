// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace GitHub.UI.ViewModels.Validation
{
    public static class PropertyValidatorExtensions
    {
        public static PropertyValidator<string> Required(this PropertyValidator<string> validator, string errorMessage)
        {
            return validator.ValidIfTrue(value => !string.IsNullOrEmpty(value), errorMessage);
        }

        public static PropertyValidator<TProperty> ValidIfTrue<TProperty>(
            this PropertyValidator<TProperty> validator,
            Func<TProperty, bool> predicate,
            string errorMessage)
        {
            return new PropertyValidator<TProperty>(validator, value => predicate(value)
                ? PropertyValidationResult.Success
                : new PropertyValidationResult(ValidationStatus.Invalid, errorMessage));
        }
    }
}
