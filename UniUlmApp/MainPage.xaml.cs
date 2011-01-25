using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;

namespace UniUlmApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        const string wlanloginFile = "wlanlogin.xml";
        const string cachedMensaplanFile = "plan.xml";

        WelcomeWiFi welcome = new WelcomeWiFi();
        static IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication();
        Mensaplan mensaplan;

        //animations that couldn't be created (because of bindings) in xaml
        Storyboard openPopupAnimation, closePopupAnimation;

        // Konstruktor
        public MainPage()
        {
            InitializeComponent();
        }

        // Daten für die ViewModel-Elemente laden
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            initializeAnimations();
            this.progress.IsIndeterminate = true;
            this.loadingPopup.IsOpen = true;
            // only check for the welcome network when we use WiFi
            var network = Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType;
            if (network == Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.Wireless80211
             || network == Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.Ethernet)
            {
                welcome.finishedLogin += new Action(hasConnection);
                welcome.needsLogin += new Action(needsLogin);
                welcome.loginError += new Action<string>(welcome_loginError);
                welcome.checkConnection();
            }
            else
            {
                this.progress.IsIndeterminate = false;
                this.loadingPopup.IsOpen = false;
                hasConnection();
            }
        }

        void needsLogin()
        {
            if (isf.FileExists(wlanloginFile))
            {
                var xml = System.Xml.Linq.XDocument.Load(isf.OpenFile(wlanloginFile, System.IO.FileMode.Open));
                welcome.login(xml.Root.Attribute("user").Value, xml.Root.Attribute("pass").Value);
            }
            else this.Dispatcher.BeginInvoke(() =>
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
                if (isf.FileExists(cachedMensaplanFile) == false)
                {
                    this.progress.IsIndeterminate = true;
                    this.loadingPopup.IsOpen = true;
                }
                var cacheStream = isf.OpenFile(cachedMensaplanFile, System.IO.FileMode.OpenOrCreate);
                var mp = new Mensaplan("http://www.uni-ulm.de/mensaplan/mensaplan.xml", cacheStream);
                mp.Loaded += new Action<Mensaplan>(mensaplan_Loaded);
                mp.OnError += new Action<Mensaplan>(mp_OnError);
            });
        }

        void mp_OnError(Mensaplan obj)
        {
            MessageBox.Show("Es gab ein Problem. Versuch es später nochmal!");
        }

        void mensaplan_Loaded(Mensaplan mp)
        {
            this.DayPivot.ItemsSource = mp.Tage;
            this.mensaplan = mp;
            var todayItem = mp.Tage.Where(tag => tag.Date == DateTime.Today).FirstOrDefault();
            if (todayItem != null)
            {
                //WP7 bug (?) Header isn't updated for selected item change
                this.DayPivot.Loaded += (_, __) =>
                        this.DayPivot.SelectedItem = todayItem;
            }

            if (mp.HasErrors)
            {
                this.popupTitle.Text = "Error... :(";
                this.popupTitle.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                this.loadingPopup.IsOpen = false;
                this.optionsBtn.IsEnabled = true;

                //on successfull load save login data
                if (string.IsNullOrEmpty(this.usernameTB.Text) == false
                 && string.IsNullOrEmpty(this.passwordTB.Password) == false
                 && (this.saveLoginCB.IsChecked ?? false))
                {
                    var xml = System.Xml.XmlWriter.Create(isf.CreateFile(wlanloginFile));
                    xml.WriteStartDocument(true);
                    xml.WriteStartElement("WLAN-Login");
                    xml.WriteAttributeString("user", this.usernameTB.Text);
                    xml.WriteAttributeString("pass", this.passwordTB.Password);
                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                    xml.Flush();
                    xml.Close();
                }
            }

            this.progress.IsIndeterminate = false;
        }

        private void optionsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.optionsPopup.IsOpen = true;
            this.openPopupAnimation.Begin();
        }

        private void clearWifiLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Gespeicherten WLAN-Login wirklich löschen?", "Wirklich löschen?", MessageBoxButton.OKCancel)
                == MessageBoxResult.OK)
            {
                try
                {
                    isf.DeleteFile(wlanloginFile);
                }
                catch
                {
                    //may happen for various reason, but error handling is not needed here
                }
            }
            this.closePopupAnimation.Begin();
        }

        private void clearCacheBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Der Mensaplan wird beim nächsten mal neu geladen.");
            try
            {
                isf.DeleteFile(cachedMensaplanFile);
            }
            catch
            {
                //may happen for various reason, but error handling is not needed here
            }
            this.closePopupAnimation.Begin();
        }

        private void closeOptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.closePopupAnimation.Begin();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            // enable the back button to close the popup
            if (this.optionsPopup.IsOpen)
            {
                e.Cancel = true;
                this.closePopupAnimation.Begin();
            }
        }

        private void initializeAnimations()
        {
            //open animation
            this.openPopupAnimation = new Storyboard();
            this.openPopupAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            var openAnim = new DoubleAnimation();
            this.openPopupAnimation.Children.Add(openAnim);
            openAnim.From = this.LayoutRoot.ActualWidth;
            openAnim.To = 0;
            openAnim.EasingFunction = new QuadraticEase();
            openAnim.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            Storyboard.SetTarget(openAnim, this.optionsPopup);
            Storyboard.SetTargetProperty(openAnim, new PropertyPath("HorizontalOffset"));

            //close animation
            this.closePopupAnimation = new Storyboard();
            this.closePopupAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            var closeAnim = new DoubleAnimation();
            this.closePopupAnimation.Children.Add(closeAnim);
            closeAnim.From = 0;
            closeAnim.To = this.LayoutRoot.ActualWidth;
            closeAnim.EasingFunction = new QuadraticEase();
            closeAnim.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            Storyboard.SetTarget(closeAnim, this.optionsPopup);
            Storyboard.SetTargetProperty(closeAnim, new PropertyPath("HorizontalOffset"));
            this.closePopupAnimation.Completed += (_, __) => this.optionsPopup.IsOpen = false;
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