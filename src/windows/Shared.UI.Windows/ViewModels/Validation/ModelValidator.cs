// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager.UI.ViewModels.Validation
{
    /// <summary>
    /// A validator that binds to the validation state of a <see cref="ViewModel"/>.
    /// It is valid if all child <see cref="PropertyValidator"/>s are valid.
    /// </summary>
    public class ModelValidator
    {
        private readonly IList<PropertyValidator> _validators = new List<PropertyValidator>();
        public event EventHandler IsValidChanged;

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            private set
            {
                if (_isValid != value)
                {
                    _isValid = value;
                    IsValidChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Add(PropertyValidator validator)
        {
            EnsureArgument.NotNull(validator, nameof(validator));

            _validators.Add(validator);
            validator.ValidationResultChanged += (s, e) => Evaluate();
        }

        private void Evaluate()
        {
            IsValid = _validators.All(x => x.ValidationResult.IsValid);
        }
    }
}
