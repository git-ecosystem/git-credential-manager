using System.Windows;
using System.Windows.Controls;

namespace GitHub.UI.Controls
{
    public partial class GitHubActionLink : Button
    {
        public static readonly DependencyProperty HasDropDownProperty = DependencyProperty.Register(
            "HasDropDown", typeof(bool), typeof(GitHubActionLink));

        public static readonly DependencyProperty TextTrimmingProperty =
            TextBlock.TextTrimmingProperty.AddOwner(typeof(GitHubActionLink));

        public static readonly DependencyProperty TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner(typeof(GitHubActionLink));

        public bool HasDropDown
        {
            get { return (bool)GetValue(HasDropDownProperty); }
            set { SetValue(HasDropDownProperty, value); }
        }

        public TextTrimming TextTrimming
        {
            get { return (TextTrimming)GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        public GitHubActionLink()
        {
        }
    }
}
