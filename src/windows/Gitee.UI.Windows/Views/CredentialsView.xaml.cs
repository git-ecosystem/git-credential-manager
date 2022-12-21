using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Gitee.UI.ViewModels;
using GitCredentialManager.UI.Controls;

namespace Gitee.UI.Views
{
    public partial class CredentialsView : UserControl, IFocusable
    {
        private readonly BrushConverter _brushConverter = new BrushConverter();

        private DrawingImage _GiteeLogoImage;

        public CredentialsView()
        {
            InitializeComponent();
        }

        // Set focus on a UIElement the next time it becomes visible
        private static void OnIsVisibleChangedOneTime(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element)
            {
                // Unsubscribe to prevent re-triggering
                element.IsVisibleChanged -= OnIsVisibleChangedOneTime;

                // Set logical focus
                element.Focus();

                // Set keyboard focus
                Keyboard.Focus(element);
            }
        }

        public void SetFocus()
        {
            if (!(DataContext is CredentialsViewModel vm))
            {
                return;
            }

            //
            // Select the best available authentication mechanism that is visible
            // and make the textbox/button focused when it next made visible.
            //
            // In WPF the controls in a TabItem are not part of the visual tree until
            // the TabControl has been switched to that tab, so we must delay focusing
            // on the textbox/button until it becomes visible.
            //
            // This means as the user first moves through the tabs, the "correct" control
            // will be given focus in that tab.
            //
            void SetFocusOnNextVisible(UIElement element)
            {
                element.IsVisibleChanged += OnIsVisibleChangedOneTime;
            }

            // Set up focus events on all controls
            SetFocusOnNextVisible(
                string.IsNullOrWhiteSpace(vm.UserName)
                    ? userNameTextBox
                    : passwordTextBox);
            SetFocusOnNextVisible(tokenTextBox);
            SetFocusOnNextVisible(browserButton);

            // Switch to the preferred tab
            if (vm.ShowBrowserLogin)
            {
                tabControl.SelectedIndex = 0;
            }
            else if (vm.ShowTokenLogin)
            {
                tabControl.SelectedIndex = 1;
            }
            else if (vm.ShowBasicLogin)
            {
                tabControl.SelectedIndex = 2;
            }
        }

        public DrawingImage GiteeLogoImage
        {
            get
            {
                if (_GiteeLogoImage is null)
                {
                    SolidColorBrush brush1 = (SolidColorBrush)_brushConverter.ConvertFrom("#C71D23");
                    SolidColorBrush brush2 = (SolidColorBrush)_brushConverter.ConvertFrom("#000000");
                    var geometry1 = Geometry.Parse(
                         "M27.9094271,0 C43.323378,0 55.8188541,12.4954761 55.8188541,27.9094271 C55.8188541,43.323378 43.323378,55.8188541 27.9094271,55.8188541 " +
                        "C12.4954761,55.8188541 0,43.323378 0,27.9094271 C0,12.4954761 12.4954761,0 27.9094271,0 Z M42.0372834,12.40419 C42.0369985,12.4041899 " +
                        "42.0367135,12.4041898 42.0364286,12.4050443 L22.7409577,12.4050443 C17.0321184,12.4050443 12.4041898,17.0329729 12.4041898,22.7418122 " +
                        "L12.4041898,42.0364286 C12.4041898,42.7976072 13.021247,43.4146643 13.7824255,43.4146643 L34.1117768,43.4146643 C39.2496197,43.4146643 " +
                        "43.4146643,39.2496197 43.4146643,34.1117768 L43.4146643,26.187125 C43.4146643,25.4259464 42.7976072,24.8088893 42.0364286,24.8088893 " +
                        "L26.1867179,24.8088893 C25.4256803,24.8092503 24.8086815,25.4260875 24.8081213,26.187125 L24.807219,29.6325761 C24.8066809,30.3489793 " +
                        "25.3531034,30.9378612 26.0519898,31.0048287 L26.1847213,31.0111726 C26.1848455,31.0111726 26.1849696,31.0111726 26.1850937,31.0108003 " +
                        "L35.8343679,31.0107199 C36.5507712,31.0107031 37.1395145,31.5572949 37.2063048,32.2561994 L37.2126036,32.3889441 L37.2126036,32.3889441 " +
                        "L37.2126036,33.0780271 C37.2126036,35.3615628 35.3614322,37.2127342 33.0778965,37.2127342 L19.9836314,37.2127342 C19.2225621,37.2126963 " +
                        "18.6055876,36.5957388 18.6055287,35.8346695 L18.6051677,22.7416739 C18.6049962,20.5273363 20.3456221,18.7195081 22.5332835,18.6119187 " +
                        "L22.7397609,18.6069668 L22.7397609,18.6069668 L42.0325819,18.6069668 C42.7934267,18.6061117 43.4103455,17.9895754 43.4116725,17.2287314 " +
                        "L43.4138095,13.7832798 C43.4151362,13.0221019 42.7984619,12.4046621 42.0372834,12.40419 Z"
                    );
                    var geometry2 = Geometry.Parse(
                         "M80.6897474,49.4117647 C84.8848423,49.4117647 88.2339265,48.5934807 90.7369998,46.9569127 C93.2400731,45.3203447 " +
                        "94.4916098,43.201199 94.4916098,40.5994755 C94.4916098,36.4031472 91.3592722,34.3049831 85.0945971,34.3049831 " +
                        "L85.0945971,34.3049831 L81.2351097,34.3049831 C79.9486139,34.3049831 79.025693,34.1790933 78.466347,33.9273136 " +
                        "C77.907001,33.6755339 77.627328,33.2559011 77.627328,32.6684151 C77.627328,32.0809292 77.8091155,31.5913576 " +
                        "78.1726904,31.1997003 C78.3125269,31.0318471 78.4943143,30.9758961 78.7180527,31.0318471 C79.6130063,31.2836268 " +
                        "80.466009,31.4095167 81.2770607,31.4095167 C84.1576925,31.4095167 86.4999539,30.7171225 88.3038447,29.3323342 " +
                        "C90.1077356,27.9475459 91.009681,25.898339 91.009681,23.1847134 C91.009681,22.1775946 90.8278935,21.2823779 " +
                        "90.4643186,20.4990633 C90.4363513,20.4431123 90.4433432,20.3801674 90.4852941,20.3102286 C90.5272451,20.2402897 " +
                        "90.5901715,20.2053203 90.6740734,20.2053203 L90.6740734,20.2053203 L91.6389452,20.2053203 C92.3381277,20.2053203 " +
                        "92.925441,19.9675284 93.4008851,19.4919445 C93.8763292,19.0163607 94.1140513,18.4288747 94.1140513,17.7294867 " +
                        "L94.1140513,17.7294867 L94.1140513,17.1420007 C94.1140513,16.4705882 93.8763292,15.8900962 93.4008851,15.4005245 " +
                        "C92.925441,14.9109529 92.3381277,14.6661671 91.6389452,14.6661671 L91.6389452,14.6661671 L85.4721556,14.6661671 " +
                        "C85.2763845,14.6661671 85.0666298,14.6381916 84.8428914,14.5822405 C83.7521667,14.2465343 82.5635565,14.0786812 " +
                        "81.2770607,14.0786812 C79.4591862,14.0786812 77.7951318,14.3934058 76.2848977,15.022855 C74.7746635,15.6523042 " +
                        "73.5231268,16.659423 72.5302877,18.0442113 C71.5374485,19.4289996 71.041029,21.0725615 71.041029,22.974897 " +
                        "C71.041029,24.2897465 71.3486692,25.4926939 71.9639498,26.5837392 C72.5792304,27.6747846 73.3483312,28.5560135 " +
                        "74.2712521,29.227426 C74.3271867,29.2554015 74.355154,29.2973648 74.355154,29.3533158 C74.355154,29.4092669 " +
                        "74.3271867,29.4652179 74.2712521,29.521169 C73.4881677,30.0806794 72.8589034,30.7590858 72.3834593,31.5563882 " +
                        "C71.9080152,32.3536905 71.6702932,33.1719745 71.6702932,34.0112402 C71.6702932,35.8016735 72.3834593,37.1864618 " +
                        "73.8097916,38.1656051 C73.8657262,38.1935806 73.8936935,38.2425378 73.8936935,38.3124766 C73.8936935,38.3824154 " +
                        "73.8657262,38.4453603 73.8097916,38.5013114 C72.6910996,39.0608218 71.8450888,39.7462221 71.2717592,40.5575122 " +
                        "C70.6984295,41.3688023 70.4117647,42.264019 70.4117647,43.2431622 C70.4117647,44.6419383 70.8872088,45.8169102 " +
                        "71.838097,46.7680779 C72.7889852,47.7192457 74.0125546,48.3976521 75.5088051,48.8032971 C77.0050556,49.2089422 " +
                        "78.7320364,49.4117647 80.6897474,49.4117647 Z M81.2770607,26.7935556 C80.3541398,26.7935556 79.6060145,26.4648433 " +
                        "79.0326849,25.8074185 C78.4593552,25.1499938 78.1726904,24.2058199 78.1726904,22.974897 C78.1726904,21.7719495 " +
                        "78.4593552,20.8417635 79.0326849,20.1843387 C79.6060145,19.526914 80.3541398,19.1982016 81.2770607,19.1982016 " +
                        "C82.1999816,19.1982016 82.9481068,19.5199201 83.5214365,20.1633571 C84.0947661,20.8067941 84.3814309,21.743974 " +
                        "84.3814309,22.974897 C84.3814309,24.2058199 84.0947661,25.1499938 83.5214365,25.8074185 C82.9481068,26.4648433 " +
                        "82.1999816,26.7935556 81.2770607,26.7935556 Z M81.864374,44.6279505 C80.2702379,44.6279505 79.0047176,44.4111403 " +
                        "78.067813,43.9775197 C77.1309085,43.5438991 76.6624562,42.8934682 76.6624562,42.0262271 C76.6624562,41.326839 " +
                        "76.9840802,40.6694143 77.627328,40.0539528 C77.7112299,39.9700262 77.8370828,39.9280629 78.0048866,39.9280629 " +
                        "L78.0048866,39.9280629 L78.1307394,39.9280629 C78.8299219,40.0679406 79.8926793,40.1378794 81.3190116,40.1378794 " +
                        "L81.3190116,40.1378794 L83.6263138,40.1378794 C84.856875,40.1378794 85.7588205,40.2637692 86.3321501,40.5155489 " +
                        "C86.9054797,40.7673286 87.1921446,41.2289247 87.1921446,41.9003372 C87.1921446,42.6836518 86.695725,43.3340827 " +
                        "85.7028859,43.8516298 C84.7100467,44.369177 83.4305428,44.6279505 81.864374,44.6279505 Z M102.168634,11.1412514 " +
                        "C103.427162,11.1412514 104.461952,10.7775696 105.273004,10.0502061 C106.084056,9.32284251 106.489581,8.38566255 106.489581," +
                        "7.23866617 C106.489581,6.09166979 106.084056,5.14749594 105.273004,4.40614462 C104.461952,3.66479331 103.427162,3.29411765 " +
                        "102.168634,3.29411765 C100.882138,3.29411765 99.8263722,3.66479331 99.0013369,4.40614462 C98.1763016,5.14749594 97.7637839," +
                        "6.09166979 97.7637839,7.23866617 C97.7637839,8.38566255 98.1763016,9.32284251 99.0013369,10.0502061 C99.8263722,10.7775696 " +
                        "100.882138,11.1412514 102.168634,11.1412514 Z M103.469113,39.0048707 C104.140328,39.0048707 104.72065,38.7600849 105.210077," +
                        "38.2705133 C105.699505,37.7809417 105.944219,37.2004496 105.944219,36.5290371 L105.944219,36.5290371 L105.944219,17.0161109 " +
                        "C105.944219,16.3446984 105.699505,15.7642063 105.210077,15.2746347 C104.72065,14.7850631 104.140328,14.5402773 103.469113," +
                        "14.5402773 L103.469113,14.5402773 L100.826203,14.5402773 C100.127021,14.5402773 99.5397074,14.7850631 99.0642633,15.2746347 " +
                        "C98.5888192,15.7642063 98.3510972,16.3446984 98.3510972,17.0161109 L98.3510972,17.0161109 L98.3510972,36.5290371 C98.3510972," +
                        "37.2004496 98.5888192,37.7809417 99.0642633,38.2705133 C99.5397074,38.7600849 100.127021,39.0048707 100.826203,39.0048707 " +
                        "L100.826203,39.0048707 L103.469113,39.0048707 Z M121.717776,39.5923567 C122.780534,39.5923567 123.829307,39.4944424 124.864097," +
                        "39.2986137 C125.535313,39.1867116 126.052708,38.8230298 126.416283,38.2075684 C126.667988,37.7879356 126.793841,37.354315 126.793841," +
                        "36.9067066 C126.793841,36.710878 126.779857,36.5010616 126.75189,36.2772574 L126.75189,36.2772574 L126.584086,35.6058449 C126.44425," +
                        "35.0183589 126.094659,34.549769 125.535313,34.2000749 C124.975967,33.8503809 124.346702,33.6615461 123.64752,33.6335706 C121.717776," +
                        "33.5496441 120.752904,32.2907456 120.752904,29.8568752 L120.752904,29.8568752 L120.752904,20.8347696 C120.752904,20.6389409 120.85079," +
                        "20.5410266 121.046561,20.5410266 L121.046561,20.5410266 L124.025078,20.5410266 C124.696294,20.5410266 125.276615,20.2962408 125.766043," +
                        "19.8066692 C126.255471,19.3170975 126.500184,18.7366055 126.500184,18.065193 L126.500184,18.065193 L126.500184,17.0161109 C126.500184," +
                        "16.3446984 126.255471,15.7642063 125.766043,15.2746347 C125.276615,14.7850631 124.696294,14.5402773 124.025078,14.5402773 L124.025078," +
                        "14.5402773 L121.046561,14.5402773 C120.85079,14.5402773 120.752904,14.4423629 120.752904,14.2465343 L120.752904,14.2465343 L120.752904," +
                        "10.5957287 C120.752904,9.8963407 120.50819,9.30885475 120.018763,8.83327089 C119.529335,8.35768702 118.949013,8.11989509 118.277798," +
                        "8.11989509 L118.277798,8.11989509 L116.851466,8.11989509 C116.152283,8.11989509 115.537003,8.35069314 115.005624,8.81228925 C114.474245," +
                        "9.27388535 114.152622,9.85437742 114.040752,10.5537655 L114.040752,10.5537655 L113.579292,14.2465343 C113.551325,14.4423629 113.425472," +
                        "14.554265 113.201733,14.5822405 L113.201733,14.5822405 L112.236862,14.6661671 C111.537679,14.7221181 110.950366,15.0088672 110.474922," +
                        "15.5264144 C109.999478,16.0439615 109.761755,16.6524291 109.761755,17.3518172 L109.761755,17.3518172 L109.761755,18.065193 C109.761755," +
                        "18.7366055 110.006469,19.3170975 110.495897,19.8066692 C110.985325,20.2962408 111.565646,20.5410266 112.236862,20.5410266 L112.236862," +
                        "20.5410266 L112.782224,20.5410266 C112.977995,20.5410266 113.075881,20.6389409 113.075881,20.8347696 L113.075881,20.8347696 L113.075881," +
                        "29.9408018 C113.075881,32.9901336 113.775063,35.3610591 115.173428,37.0535781 C116.571793,38.7460972 118.753242,39.5923567 " +
                        "121.717776,39.5923567 Z M142.231791,39.5923567 C144.469175,39.5923567 146.678591,39.0748095 148.860041,38.0397152 C149.475321," +
                        "37.75996 149.866863,37.2703884 150.034667,36.5710004 C150.118569,36.3471962 150.16052,36.123392 150.16052,35.8995879 C150.16052," +
                        "35.479955 150.034667,35.0603222 149.782961,34.6406894 L149.782961,34.6406894 L149.657109,34.3889097 C149.321501,33.8014238 " +
                        "148.832073,33.4097665 148.188825,33.2139378 C147.881185,33.1300112 147.573545,33.088048 147.265904,33.088048 C146.90233,33.088048 " +
                        "146.552738,33.143999 146.217131,33.2559011 C145.266243,33.5916073 144.287387,33.7594605 143.280564,33.7594605 C139.980423,33.7594605 " +
                        "137.980761,32.2907456 137.281578,29.3533158 C137.253611,29.2693893 137.267595,29.1924566 137.323529,29.1225178 C137.379464,29.052579 " +
                        "137.449382,29.0176096 137.533284,29.0176096 L137.533284,29.0176096 L149.069795,29.0176096 C149.796945,29.0176096 150.440193,28.7868115 " +
                        "150.999539,28.3252154 C151.558885,27.8636193 151.852542,27.2831273 151.880509,26.5837392 L151.880509,26.5837392 L151.880509,25.7864369 " +
                        "C151.880509,22.2615212 150.985555,19.408018 149.195648,17.2259273 C147.405741,15.0438366 144.804782,13.9527913 141.392772,13.9527913 " +
                        "C139.910505,13.9527913 138.477181,14.2535282 137.092799,14.8550019 C135.708418,15.4564756 134.477857,16.2957412 133.401116,17.3727988 " +
                        "C132.324375,18.4498564 131.457388,19.8066692 130.800157,21.4432372 C130.142925,23.0798052 129.814309,24.8632447 129.814309,26.7935556 " +
                        "C129.814309,30.7381042 130.981944,33.8573748 133.317214,36.1513676 C135.652483,38.4453603 138.624009,39.5923567 142.231791,39.5923567 " +
                        "Z M145.084455,24.0659423 L137.407431,24.0659423 C137.21166,24.0659423 137.113775,23.9960035 137.113775,23.8561259 C137.113775,22.7091295 " +
                        "137.659137,21.688023 138.749862,20.7928063 C139.532946,20.1213938 140.469851,19.7856875 141.560575,19.7856875 C142.819104,19.7856875 " +
                        "143.763,20.1423754 144.392264,20.8557512 C145.021529,21.569127 145.364128,22.5272886 145.420063,23.730236 L145.420063,23.730236 L145.420063," +
                        "23.7721993 C145.420063,23.968028 145.308193,24.0659423 145.084455,24.0659423 L145.084455,24.0659423 Z M167.82187,39.5923567 C170.059254," +
                        "39.5923567 172.26867,39.0748095 174.45012,38.0397152 C175.0654,37.75996 175.456943,37.2703884 175.624746,36.5710004 C175.708648,36.3471962 " +
                        "175.750599,36.123392 175.750599,35.8995879 C175.750599,35.479955 175.624746,35.0603222 175.373041,34.6406894 L175.373041,34.6406894 L175.247188," +
                        "34.3889097 C174.91158,33.8014238 174.422153,33.4097665 173.778905,33.2139378 C173.471264,33.1300112 173.163624,33.088048 172.855984,33.088048 " +
                        "C172.492409,33.088048 172.142818,33.143999 171.80721,33.2559011 C170.856322,33.5916073 169.877466,33.7594605 168.870644,33.7594605 C165.570502," +
                        "33.7594605 163.57084,32.2907456 162.871658,29.3533158 C162.84369,29.2693893 162.857674,29.1924566 162.913609,29.1225178 C162.969543,29.052579 " +
                        "163.039462,29.0176096 163.123363,29.0176096 L163.123363,29.0176096 L174.659875,29.0176096 C175.387024,29.0176096 176.030272,28.7868115 176.589618," +
                        "28.3252154 C177.148964,27.8636193 177.442621,27.2831273 177.470588,26.5837392 L177.470588,26.5837392 L177.470588,25.7864369 C177.470588,22.2615212 " +
                        "176.575635,19.408018 174.785727,17.2259273 C172.99582,15.0438366 170.394861,13.9527913 166.982851,13.9527913 C165.500584,13.9527913 164.06726," +
                        "14.2535282 162.682878,14.8550019 C161.298497,15.4564756 160.067936,16.2957412 158.991195,17.3727988 C157.914454,18.4498564 157.047468,19.8066692 " +
                        "156.390236,21.4432372 C155.733004,23.0798052 155.404389,24.8632447 155.404389,26.7935556 C155.404389,30.7381042 156.572023,33.8573748 158.907293," +
                        "36.1513676 C161.242563,38.4453603 164.214088,39.5923567 167.82187,39.5923567 Z M170.674534,24.0659423 L162.997511,24.0659423 C162.80174,24.0659423 " +
                        "162.703854,23.9960035 162.703854,23.8561259 C162.703854,22.7091295 163.249216,21.688023 164.339941,20.7928063 C165.123025,20.1213938 166.05993," +
                        "19.7856875 167.150655,19.7856875 C168.409183,19.7856875 169.353079,20.1423754 169.982344,20.8557512 C170.611608,21.569127 170.954207,22.5272886 " +
                        "171.010142,23.730236 L171.010142,23.730236 L171.010142,23.7721993 C171.010142,23.968028 170.898273,24.0659423 170.674534,24.0659423 L170.674534,24.0659423 Z"
                    );
                    _GiteeLogoImage = new DrawingImage
                    {
                        Drawing = new DrawingGroup
                        {
                            Children =
                            {
                                new GeometryDrawing { Geometry = geometry1, Brush = brush1 },
                                new GeometryDrawing { Geometry = geometry2, Brush = brush2 },
                            }
                        }
                    };
                }

                return _GiteeLogoImage;
            }
        }

         
    }
}
