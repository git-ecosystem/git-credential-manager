// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Linq;

namespace GitHub.UI.ViewModels.Validation
{
    /// <summary>
    /// A validator that represents the validation state of a model. It's true if all the supplied
    /// property validators are true.
    /// </summary>
    public class ModelValidator : ViewModel
    {
        public ModelValidator(params PropertyValidator[] propertyValidators)
        {
            if (propertyValidators == null) throw new ArgumentNullException(nameof(propertyValidators));

            // Protect against mutations of the supplied array.
            var validators = propertyValidators.ToList();

            // This would be a lot cleaner with ReactiveUI but here we are.
            foreach (var validator in validators)
            {
                validator.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName != nameof(validator.ValidationResult)) return;

                    IsValid = validators.All(v => v.ValidationResult.IsValid);
                };
            }
        }

        private bool _isValid;

        public bool IsValid
        {
            get { return _isValid; }
            set
            {
                _isValid = value;
                RaisePropertyChangedEvent(nameof(IsValid));
            }
        }
    }
}
