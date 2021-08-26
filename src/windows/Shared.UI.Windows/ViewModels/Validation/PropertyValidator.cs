using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Git.CredentialManager.UI.ViewModels.Validation
{
    public abstract class PropertyValidator
    {
        public event EventHandler ValidationResultChanged;

        /// <summary>
        /// Creates a validator for a property. This validator is the starting point to attach other
        /// validations to the property. This method itself doesn't apply any validations.
        /// </summary>
        /// <typeparam name="TObject">Type of the object with the property to validate.</typeparam>
        /// <typeparam name="TProperty">The type of the property to validate.</typeparam>
        /// <param name="source">The object with the property to validate.</param>
        /// <param name="property">An expression for the property to validate</param>
        /// <returns>A property validator</returns>
        public static PropertyValidator<TObject, TProperty> For<TObject, TProperty>(TObject source, Expression<Func<TObject, TProperty>> property)
            where TObject : INotifyPropertyChanged
        {
            return new PropertyValidator<TObject, TProperty>(source, property);
        }

        private PropertyValidationResult _validationResult = PropertyValidationResult.Unvalidated;

        /// <summary>
        /// The current validation result for this validator.
        /// </summary>
        public PropertyValidationResult ValidationResult
        {
            get => _validationResult;
            protected set
            {
                _validationResult = value;
                ValidationResultChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class PropertyValidator<TProperty> : PropertyValidator
    {
        private readonly IList<Func<TProperty, PropertyValidationResult>> _rules =
            new List<Func<TProperty, PropertyValidationResult>>();

        public void AddRule(Func<TProperty, PropertyValidationResult> predicate)
        {
            _rules.Add(predicate);
        }

        protected void Evaluate(TProperty property)
        {
            PropertyValidationResult result = PropertyValidationResult.Unvalidated;

            foreach (Func<TProperty, PropertyValidationResult> rule in _rules)
            {
                result = rule(property);

                // An invalid validation rule means we stop evaluating more rules
                // and return the current result.
                if (result.Status == ValidationStatus.Invalid)
                {
                    break;
                }
            }

            ValidationResult = result;
        }
    }

    /// <summary>
    /// This validator watches the target property for changes and then propagates that change up the chain.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    public class PropertyValidator<TObject, TProperty> : PropertyValidator<TProperty> where TObject : INotifyPropertyChanged
    {
        internal PropertyValidator(TObject source, Expression<Func<TObject, TProperty>> propertyExpression)
        {
            EnsureArgument.NotNull(source, nameof(source));
            EnsureArgument.NotNull(propertyExpression, nameof(propertyExpression));

            Func<TObject, TProperty> compiledProperty = propertyExpression.Compile();
            PropertyInfo propertyInfo = GetPropertyInfo(propertyExpression);

            // Start watching for changes to this property and propagate those changes to the chained validators.
            source.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == propertyInfo.Name)
                {
                    Evaluate(compiledProperty(source));
                }
            };
        }

        private static PropertyInfo GetPropertyInfo(Expression<Func<TObject, TProperty>> propertyExpression)
        {
            var member = propertyExpression.Body as MemberExpression;
            Debug.Assert(member != null, "Property expression doesn't refer to a member.");

            var propertyInfo = member.Member as PropertyInfo;
            Debug.Assert(propertyInfo != null, "Property expression does not refer to a property.");

            var propertyType = typeof(TObject);
            Debug.Assert(propertyInfo.ReflectedType != null
                         && (propertyType == propertyInfo.ReflectedType ||
                             propertyType.IsSubclassOf(propertyInfo.ReflectedType)),
                "Property expression is not of the specified type");

            return propertyInfo;
        }
    }
}
