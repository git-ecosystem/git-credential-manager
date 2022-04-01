using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GitLab.UI.ViewModels;
using GitCredentialManager.UI.Controls;

namespace GitLab.UI.Views
{
    public class CredentialsView : UserControl, IFocusable
    {
        private TabControl _tabControl;
        private Button _browserButton;
        private TextBox _tokenTextBox;
        private TextBox _patUserNameTextBox;
        private TextBox _userNameTextBox;
        private TextBox _passwordTextBox;
        private DrawingImage _gitLabLogoImage;
        private DrawingImage _gitLabLogoTypeImage;

        public CredentialsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _tabControl = this.FindControl<TabControl>("authModesTabControl");
            _browserButton = this.FindControl<Button>("signInBrowserButton");
            _patUserNameTextBox = this.FindControl<TextBox>("patUserNameTextBox");
            _tokenTextBox = this.FindControl<TextBox>("tokenTextBox");
            _userNameTextBox = this.FindControl<TextBox>("userNameTextBox");
            _passwordTextBox = this.FindControl<TextBox>("passwordTextBox");
        }

        public void SetFocus()
        {
            if (!(DataContext is CredentialsViewModel vm))
            {
                return;
            }

            // Select the best available authentication mechanism that is visible
            // and focus on the button/text box
            if (vm.ShowBrowserLogin)
            {
                _tabControl.SelectedIndex = 0;
                _browserButton.Focus();
            }
            else if (vm.ShowTokenLogin)
            {
                _tabControl.SelectedIndex = 1;
                _tokenTextBox.Focus();
            }
            else if (vm.ShowBasicLogin)
            {
                _tabControl.SelectedIndex = 2;
                if (string.IsNullOrWhiteSpace(vm.UserName))
                {
                    _userNameTextBox.Focus();
                }
                else
                {
                    _passwordTextBox.Focus();
                }
            }
        }

        public IImage GitLabLogoImage
        {
            get
            {
                if (_gitLabLogoImage is null)
                {
                    var brush1 = SolidColorBrush.Parse("#E24329");
                    var brush2 = SolidColorBrush.Parse("#FCA326");
                    var brush3 = SolidColorBrush.Parse("#FC6D26");

                    var geometry1 = PathGeometry.Parse(
                        "M442.097,243.57h-87.12l37.425-115.224c1.919-5.895,10.282-5.895,12.27,0L442.097,243.57L442.097,243.57z" +
                        "M292.778,434.892L292.778,434.892l62.199-191.322H230.669L292.778,434.892L292.778,434.892z" +
                        "M143.549,243.57h87.12l-37.494-115.224c-1.919-5.895-10.282-5.895-12.27,0L143.549,243.57L143.549,243.57z"
                    );
                    var geometry2 = PathGeometry.Parse(
                        "M442.097,243.57L442.097,243.57l18.873,58.126c1.714,5.278-0.137,11.104-4.661,14.394L292.778,434.892L442.097,243.57L442.097,243.57z" +
                        "M143.549,243.57L143.549,243.57l-18.941,58.126c-1.714,5.278,0.137,11.104,4.661,14.394l163.509,118.801L143.549,243.57L143.549,243.57z"
                    );
                    var poly1 = new PolylineGeometry
                    {
                        Points =
                        {
                            new Point(292.778,434.892),
                            new Point(354.977,243.57),
                            new Point(442.097,243.57),
                        }
                    };
                    var poly2 = new PolylineGeometry
                    {
                        Points =
                        {
                            new Point(292.778,434.892),
                            new Point(143.549,243.57),
                            new Point(230.669,243.57),
                        }
                    };

                    _gitLabLogoImage = new DrawingImage
                    {
                        Drawing = new DrawingGroup
                        {
                            Children =
                            {
                                new GeometryDrawing { Geometry = geometry1, Brush = brush1 },
                                new GeometryDrawing { Geometry = geometry2, Brush = brush2 },
                                new GeometryDrawing { Geometry = poly1, Brush = brush3 },
                                new GeometryDrawing { Geometry = poly2, Brush = brush3 },
                            }
                        }
                    };
                }

                return _gitLabLogoImage;
            }
        }

        public IImage GitLabLogoTypeImage
        {
            get
            {
                if (_gitLabLogoTypeImage is null)
                {
                    var brush = SolidColorBrush.Parse("#8C929D");
                    var geometry1 = PathGeometry.Parse(
                        "M13,188.892c-5.5,5.7-14.6,11.4-27,11.4c-16.6,0-23.3-8.2-23.3-18.9" +
                        "c0-16.1,11.2-23.8,35-23.8c4.5,0,11.7,0.5,15.4,1.2v30.1H13z M-9.6,90.392c-17.6,0-33.8,6.2-46.4,16.7l7.7,13.4" +
                        "c8.9-5.2,19.8-10.4,35.5-10.4c17.9,0,25.8,9.2,25.8,24.6v7.9c-3.5-0.7-10.7-1.2-15.1-1.2c-38.2,0-57.6,13.4-57.6,41.4" +
                        "c0,25.1,15.4,37.7,38.7,37.7c15.7,0,30.8-7.2,36-18.9l4,15.9h15.4v-83.2C34.3,107.992,22.9,90.392-9.6,90.392L-9.6,90.392z"
                    );
                    var geometry2 = PathGeometry.Parse(
                        "M-17.7,201.192c-8.2,0-15.4-1-20.8-3.5v-67.3v-7.8c7.4-6.2,16.6-10.7,28.3-10.7" +
                        "c21.1,0,29.2,14.9,29.2,39C19,185.092,5.9,201.192-17.7,201.192 M-8.5,90.592c-19.5,0-30,13.3-30,13.3v-21l-0.1-27.8h-9.8h-11.5" +
                        "l0.1,158.5c10.7,4.5,25.3,6.9,41.2,6.9c40.7,0,60.3-26,60.3-70.9C41.6,114.092,23.5,90.592-8.5,90.592"
                    );
                    var geometry3 = PathGeometry.Parse(
                        "M18.3,72.192c19.3,0,31.8,6.4,39.9,12.9l9.4-16.3c-12.7-11.2-29.9-17.2-48.3-17.2" +
                        "c-46.4,0-78.9,28.3-78.9,85.4c0,59.8,35.1,83.1,75.2,83.1c20.1,0,37.2-4.7,48.4-9.4l-0.5-63.9v-7.5v-12.6H4v20.1h38l0.5,48.5" +
                        "c-5,2.5-13.6,4.5-25.3,4.5c-32.2,0-53.8-20.3-53.8-63C-36.7,93.292-14.4,72.192,18.3,72.192"
                    );
                    var geometry4 = PathGeometry.Parse(
                        "M-37.7,55.592H-59l0.1,27.3v11.2v6.5v11.4v65v0.2c0,26.3,11.4,43.9,43.9,43.9" +
                        "c4.5,0,8.9-0.4,13.1-1.2v-19.1c-3.1,0.5-6.4,0.7-9.9,0.7c-17.9,0-25.8-9.2-25.8-24.6v-65h35.7v-17.8h-35.7L-37.7,55.592" +
                        "L-37.7,55.592z"
                    );
                    var geometry5 = PathGeometry.Parse(
                        "M839.7,198.192h-21.8l0.1,162.5h88.3v-20.1h-66.5L839.7,198.192L839.7,198.192z" +
                        "M680.4,360.692h21.3v-124h-21.3V360.692L680.4,360.692z" +
                        "M680.4,219.592h21.3v-21.3h-21.3V219.592L680.4,219.592z"
                    );

                    _gitLabLogoTypeImage = new DrawingImage
                    {
                        Drawing = new DrawingGroup
                        {
                            Children =
                            {
                                new DrawingGroup
                                {
                                    Transform = new TranslateTransform(977.327440, 143.286396),
                                    Children = { new GeometryDrawing { Geometry = geometry1, Brush = brush } }
                                },
                                new DrawingGroup
                                {
                                    Transform = new TranslateTransform(1099.766904, 143.128930),
                                    Children = { new GeometryDrawing { Geometry = geometry2, Brush = brush } }
                                },
                                new DrawingGroup
                                {
                                    Transform = new TranslateTransform(584.042117, 143.630796),
                                    Children = { new GeometryDrawing { Geometry = geometry3, Brush = brush } }
                                },
                                new DrawingGroup
                                {
                                    Transform = new TranslateTransform(793.569045, 142.577463),
                                    Children = { new GeometryDrawing { Geometry = geometry4, Brush = brush } }
                                },
                                new GeometryDrawing { Geometry = geometry5, Brush = brush },
                            }
                        }
                    };
                }

                return _gitLabLogoTypeImage;
            }
        }
    }
}
