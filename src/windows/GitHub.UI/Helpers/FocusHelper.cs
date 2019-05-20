// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GitHub.UI.Helpers
{
    public static class FocusHelper
    {
        /// <summary>
        /// Attempts to move focus to an element within the provided container waiting for the
        /// element to be loaded if necessary (waits max 1 second to protect against confusing focus
        /// shifts if the element gets loaded much later).
        /// </summary>
        /// <param name="element">The element to move focus from.</param>
        /// <param name="direction">The direction to give focus.</param>
        public static Task<bool> TryMoveFocus(this FrameworkElement element, FocusNavigationDirection direction)
        {
            return TryFocusImpl(element, e => e.MoveFocus(new TraversalRequest(direction)));
        }

        /// <summary>
        /// Attempts to move focus to the element, waiting for the element to be loaded if necessary
        /// (waits max 1 second to protect against confusing focus shifts if the element gets loaded
        /// much later).
        /// </summary>
        /// <param name="element">The element to give focus to.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "There's a Take(1) in there it'll be fine")]
        public static Task<bool> TryFocus(this FrameworkElement element)
        {
            return TryFocusImpl(element, e => e.Focus());
        }

        private static async Task<bool> TryFocusImpl(FrameworkElement element, Func<FrameworkElement, bool> focusAction)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return false;
            }

            var loadedElement = await WaitForElementLoaded(element);

            if (focusAction?.Invoke(element) ?? false)
                return true;

            // TODO: MoveFocus almost always requires its descendant elements to be fully loaded, we
            // have no way of knowing if they are so we should try again before bailing out.
            return false;
        }

        private static Task<FrameworkElement> WaitForElementLoaded(FrameworkElement element)
        {
            if (element.IsLoaded) return Task.FromResult(element);
            var taskCompletionSource = new TaskCompletionSource<FrameworkElement>();
            element.Loaded += (s, e) => taskCompletionSource.SetResult(element);
            return taskCompletionSource.Task;
        }
    }
}
