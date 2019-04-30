// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GitHub.UI.Helpers
{
    /// <summary>
    /// Contains attached dependency properties to correct the scoping of access keys within the WPF framework.
    /// </summary>
    /// <remarks>Code comes from the following blog post: http://coderelief.net/2012/07/29/wpf-access-keys-scoping/</remarks>
    public static class AccessKeysManagerScoping
    {
        /// <summary>
        /// Attached dependency property to enable or disable scoping of access keys.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty
            = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool),
                typeof(AccessKeysManagerScoping), new PropertyMetadata(false, OnIsEnabledChanged));

        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsEnabled(DependencyObject d)
        {
            return (bool)d.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject d, bool value)
        {
            d.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null)
                return;

            if ((bool)e.NewValue)
                AccessKeyManager.AddAccessKeyPressedHandler(d, HandleAccessKeyPressed);
            else
                AccessKeyManager.RemoveAccessKeyPressedHandler(d, HandleAccessKeyPressed);
        }

        /// <summary>
        /// Fixes access key scoping bug within the WPF framework.
        /// </summary>
        /// <param name="sender">Potential target of the current access keys.</param>
        /// <param name="e">Info object for the current access keys and proxy to effect it's confirmation.</param>
        /// <remarks>
        /// The problem is that all access key presses are scoped to the active window, regardless of
        /// what properties, handlers, scope etc. you may have set. Targets are objects that have
        /// potential to be the target of the access keys in effect.
        ///
        /// If you happen to have a current object focused and you press the access keys of one of
        /// it's child's targets it will execute the child target. But, if you also have a ancestor
        /// target, the ancestor target will be executed instead. That goes against intuition and
        /// standard Windows behavior. The root of this logic (bug) is within the
        /// HwndSource.OnMnemonicCore method. If the scope is set to anything but the active window's
        /// HwndSource, the target will not be executed and the handler for the next target in the
        /// chain will be called. This handler gets called for every target within the scope, which
        /// because of the bug is always at the window level of the active window. If you set
        /// e.Handled to true, no further handlers in the chain will be executed. However because
        /// setting the scope to anything other than active window's HwndSource causes the target not
        /// to be acted on, we can use it to not act on the target while not canceling the chain
        /// either, thereby allowing us to skip to the next target's handler. Note that if a handler
        /// does act on the target it will inheritably break the chain because the menu will lose
        /// focus and the next handlers won't apply anymore; because a target has already been
        /// confirmed. We will use this knowledge to resolve the issue. We will set the scope to
        /// something other than the active window's HwndSource, if we find that the incorrect
        /// element is being targeted for the access keys (because the target is out of scope). This
        /// will cause the target to be skipped and the next target's handler will be called. If we
        /// detect the target is correct, we'll just leave everything alone so the target will be confirmed.
        ///
        /// NOTE: Do not call AccessKeyManager.IsKeyRegistered as it will cause a
        ///       <see cref="T:System.StackOverflowException"/> to be thrown. The key is registered
        /// otherwise this handler wouldn't be called for it, therefore there is no need to call it.
        /// </remarks>
        private static void HandleAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
        {
            var focusedElement = Keyboard.FocusedElement as FrameworkElement;
            if (focusedElement == null)
                return; // No focused element.

            if (Equals(sender, focusedElement))
                return; // This is the correct target.

            // Look through descendants tree to see if this target is a descendant of the focused
            // element. We will stop looking at either the end of the tree or if a object with
            // multiple children is encountered that this target isn't a descendant of.

            // If no valid target is found, we'll set the scope to the sender which results in
            // skipping to the next target handler in the chain (due to the bug).

            DependencyObject obj = focusedElement;
            while (obj != null)
            {
                int childCount = VisualTreeHelper.GetChildrenCount(obj);
                for (int i = 0; i < childCount; i++)
                {
                    if (VisualTreeHelper.GetChild(obj, i) == sender)
                        return; // Found correct target; let it execute.
                }

                if (childCount > 1)
                {
                    // This target isn't a direct descendant and there are multiple direct
                    // descendants; skip this target.
                    e.Scope = sender;
                    return;
                }

                if (childCount == 1)
                {
                    // This target isn't a direct descendant, but we'll keep looking down the
                    // descendants chain to see if it's a descendant of the direct descendant.
                    obj = VisualTreeHelper.GetChild(obj, 0);
                }
                else
                {
                    // End of the line; skip this target.
                    e.Scope = sender;
                    return;
                }
            }
        }
    }
}
