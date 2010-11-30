using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Phone.Controls;

namespace UniUlmApp
{
    public partial class MainPage : PhoneApplicationPage
    {

        WelcomeWiFi welcome = new WelcomeWiFi();

        // Konstruktor
        public MainPage()
        {
            InitializeComponent();
        }

        // Daten für die ViewModel-Elemente laden
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.progress.IsIndeterminate = true;
            this.loadingPopup.IsOpen = true;
            // only check for the welcome network when we use WiFi
            if (Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType ==
                Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.Wireless80211)
            {
                welcome.finishedLogin += new Action(hasConnection);
                welcome.needsLogin += new Action(needsLogin);
                welcome.loginError += new Action<string>(welcome_loginError);
                welcome.checkConnection();
            }
            else
                hasConnection();
        }

        void needsLogin()
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                this.loadingPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.loginPanel.Visibility = System.Windows.Visibility.Visible;
            });
        }

        private void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            welcome.login(this.usernameTB.Text, this.passwordTB.Password);
            this.popupTitle.Text = "Logging in";
            this.loginPanel.Visibility = System.Windows.Visibility.Collapsed;
            this.loadingPanel.Visibility = System.Windows.Visibility.Visible;
        }

        void welcome_loginError(string msg)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                this.loginPanel.Visibility = System.Windows.Visibility.Visible;
                this.loadingPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.loginMsgTB.Text = msg;
                this.loginMsgTB.Foreground = new SolidColorBrush(Colors.Red);
            });
        }

        void hasConnection()
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                this.loadingPopup.IsOpen = false;
                var isf = IsolatedStorageFile.GetUserStoreForApplication();
                var xmlSaveFile = "plan.xml";
                var cacheStream = isf.OpenFile(xmlSaveFile, System.IO.FileMode.OpenOrCreate);
                var mp = new Mensaplan("http://www.uni-ulm.de/mensaplan/mensaplan.xml", cacheStream);
                mp.Loaded += new Action<Mensaplan>(mensaplan_Loaded);
            });
        }

        void mensaplan_Loaded(Mensaplan mp)
        {
            this.DayPivot.ItemsSource = mp.Tage;
            var heuteItem = mp.Tage.Where(tag => tag.DateName == "Heute").FirstOrDefault();
            if (heuteItem != null)
            {
                //WP7 bug (?) Header isn't updated for selected item change
                this.DayPivot.Loaded += (_, __) =>
                        this.DayPivot.SelectedItem = heuteItem;
            }

            if (mp.HasErrors)
            {
                this.popupTitle.Text = "Error... :(";
                this.popupTitle.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                this.loadingPopup.IsOpen = false;
            }

            this.progress.IsIndeterminate = false;
        }
    }

    public class DebugConverter : IValueConverter
    {

        public Object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
        {

            //set a breakpoint here
            return value;
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
        {

            //set a breakpoint here
            return value;
        }
    }
}