using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
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
        bool needsUpdate = false;
        bool optionsPopupOpen = false, wifiPopupOpen = false;

        string user = null, pass = null;
        bool saveLogin = false;

        //animations that couldn't be created (because of bindings) in xaml
        Storyboard openPopupAnimation, closePopupAnimation;
        Storyboard openWifiPopupAnimation, closeWifiPopupAnimation;

        // Konstruktor
        public MainPage()
        {
            InitializeComponent();
        }

        // Daten für die ViewModel-Elemente laden
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //handle loosing events
            welcome.finishedLogin -= new Action<bool>(welcome_finishedLogin);
            welcome.needsLogin -= new Action(needsLogin);
            welcome.loginError -= new Action<string>(welcome_loginError);
            //see above
            welcome.finishedLogin += new Action<bool>(welcome_finishedLogin);
            welcome.needsLogin += new Action(needsLogin);
            welcome.loginError += new Action<string>(welcome_loginError);

            this.needsUpdate = false;
            if (isf.FileExists(cachedMensaplanFile) == false)
            {
                this.needsUpdate = true;
            }
            else
            {
                var mp = loadCachedMensaplan();
                if (mp.isCurrent == false)
                {
                    this.needsUpdate = true;
                }
                else
                {
                    this.mensaplan_Loaded(mp);
                }
            }

            initializeAnimations();

            checkNetworkAndStartLoginAndDownload();
        }

        private void checkNetworkAndStartLoginAndDownload()
        {
            if (this.needsUpdate)
            {
                this.progress.IsIndeterminate = true;
                this.loadingPopup.IsOpen = true;
            }

            System.Threading.ThreadPool.QueueUserWorkItem((x) =>
            {
                // only check for the welcome network when we use WiFi
                var network = Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType;
                if (network == Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.Wireless80211
                 || network == Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.Ethernet)
                {
                    this.Dispatcher.BeginInvoke(() =>
                        this.progress.IsIndeterminate = true);

                    welcome.checkConnection();
                }
                else if (network == Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.None)
                {
                    if (this.needsUpdate)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            this.progress.IsIndeterminate = false;
                            this.loginPanel.Visibility = System.Windows.Visibility.Visible;
                            this.loadingPanel.Visibility = System.Windows.Visibility.Collapsed;
                            this.loginPanelPart.Visibility = System.Windows.Visibility.Collapsed;
                            this.loginMsgTB.Text = "Kein Netzwerk verfügbar! Ohne eine Netzwerkverbindung kann kein Mensaplan heruntergeladen werden.";
                            this.loginMsgTB.Foreground = new SolidColorBrush(Colors.Red);
                            if (this.optionsPopupOpen)
                                this.closePopupAnimation.Begin();
                        });
                    }
                }
                else
                {
                    hasConnection();
                }
            });
        }

        void welcome_finishedLogin(bool loginNeeded)
        {
            if (loginNeeded)
            {
                this.Dispatcher.BeginInvoke(() =>
                        this.openWifiPopupAnimation.Begin());

                //on successfull load save login data
                if (string.IsNullOrEmpty(this.user) == false
                 && string.IsNullOrEmpty(this.pass) == false
                 && this.saveLogin == true)
                {
                    var xml = System.Xml.XmlWriter.Create(isf.CreateFile(wlanloginFile));
                    xml.WriteStartDocument(true);
                    xml.WriteStartElement("WLAN-Login");
                    xml.WriteAttributeString("user", this.user);
                    xml.WriteAttributeString("pass", this.pass);
                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                    xml.Flush();
                    xml.Close();
                }
            }
            hasConnection();
        }

        private static Mensaplan loadCachedMensaplan()
        {
            var cacheStream = isf.OpenFile(cachedMensaplanFile, System.IO.FileMode.OpenOrCreate);
            var mp = new Mensaplan(cacheStream);
            cacheStream.Close();
            return mp;
        }

        void needsLogin()
        {
            if (isf.FileExists(wlanloginFile))
            {
                var file = isf.OpenFile(wlanloginFile, System.IO.FileMode.Open);
                try
                {

                    var xml = System.Xml.Linq.XDocument.Load(file);
                    file.Close();
                    this.user = xml.Root.Attribute("user").Value;
                    this.pass = xml.Root.Attribute("pass").Value;
                    welcome.login(this.user, this.pass);
                    return;
                }
                catch
                {
                    file.Close();
                    isf.DeleteFile(wlanloginFile);
                }
            }
            // did not return - problem with stored login. redo login
            this.Dispatcher.BeginInvoke(() =>
            {
                this.loadingPopup.IsOpen = true;
                this.loadingPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.loginPanel.Visibility = System.Windows.Visibility.Visible;
            });
        }

        private void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            this.popupTitle.Text = "Logging in";
            this.loginPanel.Visibility = System.Windows.Visibility.Collapsed;
            this.loadingPanel.Visibility = System.Windows.Visibility.Visible;

            this.pass = this.passwordTB.Password;
            this.user = this.usernameTB.Text;
            this.saveLogin = this.saveLoginCB.IsChecked ?? false;

            welcome.login(this.usernameTB.Text, this.passwordTB.Password);
        }

        void welcome_loginError(string msg)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                this.progress.IsIndeterminate = false; ;
                this.loginPanel.Visibility = System.Windows.Visibility.Visible;
                this.loadingPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.loginMsgTB.Text = msg;
                this.loginMsgTB.Foreground = new SolidColorBrush(Colors.Red);
            });
        }

        void hasConnection()
        {
            if (this.needsUpdate)
            {
                var wc = new WebClient();
                wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
                wc.DownloadStringAsync(new Uri("http://www.uni-ulm.de/mensaplan/mensaplan.xml"), wc);
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                    {
                        this.progress.IsIndeterminate = false;
                        this.loadingPopup.IsOpen = false;
                    });
            }
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var cacheStream = isf.OpenFile(cachedMensaplanFile, System.IO.FileMode.Create);
            var buf = System.Text.Encoding.UTF8.GetBytes(e.Result);
            cacheStream.Write(buf, 0, buf.Length);
            cacheStream.Flush();
            cacheStream.Close();

            var mp = loadCachedMensaplan();
            this.Dispatcher.BeginInvoke(() =>
                    {
                        this.progress.IsIndeterminate = false;
                        this.loadingPopup.IsOpen = false;
                        this.mensaplan_Loaded(mp);
                    });
        }

        void mp_OnError(Mensaplan obj)
        {
            this.showMessage("Es gab ein Problem. Versuch es später nochmal!");
        }

        private MessageBoxResult showMessage(string msg, string caption = "", MessageBoxButton btn = MessageBoxButton.OK)
        {
            var res = MessageBox.Show(msg, caption, btn);
            return res;
        }

        void mensaplan_Loaded(Mensaplan mp)
        {
            this.mensaplan = mp;
            this.DayPivot.ItemsSource = mp.Tage;

            if (mp.isLoaded == false)
            {
                this.popupTitle.Text = "Error... :(";
                this.popupTitle.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                this.loadingPopup.IsOpen = false;
                this.optionsBtn.IsEnabled = true;
                trySelectCurrentDay();
            }

            this.progress.IsIndeterminate = false;
        }

        private void optionsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.optionsPopupOpen = true;
            this.openPopupAnimation.Begin();
        }

        private void clearWifiLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.showMessage("Gespeicherten WLAN-Login wirklich löschen?", "Wirklich löschen?", MessageBoxButton.OKCancel)
                == MessageBoxResult.OK)
            {
                this.closePopupAnimation.Begin();
                try
                {
                    isf.DeleteFile(wlanloginFile);
                }
                catch
                {
                    //may happen for various reason, but error handling is not needed here
                }
            }
        }

        private void clearCacheBtn_Click(object sender, RoutedEventArgs e)
        {
            this.closePopupAnimation.Begin();
            this.progress.IsIndeterminate = true;
            try
            {
                isf.DeleteFile(cachedMensaplanFile);
            }
            catch {/*may happen for various reason, but error handling is not needed here*/}
            this.needsUpdate = true;
            this.checkNetworkAndStartLoginAndDownload();
        }

        private void closeOptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.closePopupAnimation.Begin();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            // enable the back button to close the popup
            if (this.optionsPopupOpen)
            {
                e.Cancel = true;
                this.closePopupAnimation.Begin();
            }
            base.OnBackKeyPress(e);
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
            Storyboard.SetTarget(openAnim, this.optionsPopupTransform);
            Storyboard.SetTargetProperty(openAnim, new PropertyPath("X"));
            this.openPopupAnimation.Completed += (_, __) => this.optionsPopupOpen = true;

            //close animation
            this.closePopupAnimation = new Storyboard();
            this.closePopupAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            var closeAnim = new DoubleAnimation();
            this.closePopupAnimation.Children.Add(closeAnim);
            closeAnim.From = 0;
            closeAnim.To = this.LayoutRoot.ActualWidth;
            closeAnim.EasingFunction = new QuadraticEase();
            closeAnim.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            Storyboard.SetTarget(closeAnim, this.optionsPopupTransform);
            Storyboard.SetTargetProperty(closeAnim, new PropertyPath("X"));
            this.closePopupAnimation.Completed += (_, __) => this.optionsPopupOpen = false;


            //open animation
            this.openWifiPopupAnimation = new Storyboard();
            this.openWifiPopupAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            var openWifiAnim = new DoubleAnimation();
            this.openWifiPopupAnimation.Children.Add(openWifiAnim);
            openWifiAnim.From = -this.wifiPopupGrid.ActualHeight;
            openWifiAnim.To = 0;
            openWifiAnim.EasingFunction = new QuadraticEase();
            openWifiAnim.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            Storyboard.SetTarget(openWifiAnim, this.wifiPopupTransform);
            Storyboard.SetTargetProperty(openWifiAnim, new PropertyPath("Y"));
            this.openWifiPopupAnimation.Completed +=
                (_, __) =>
                {
                    this.wifiPopupOpen = true;
                    UIHelper.SetTimeout(3500, () =>
                    {
                        if (this.wifiPopupOpen)
                        {
                            this.wifiPopupOpen = false;
                            this.Dispatcher.BeginInvoke(() =>
                                this.closeWifiPopupAnimation.Begin());
                        }
                    });
                };

            //close animation
            this.closeWifiPopupAnimation = new Storyboard();
            this.closeWifiPopupAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            var closeWifiAnim = new DoubleAnimation();
            this.closeWifiPopupAnimation.Children.Add(closeWifiAnim);
            closeWifiAnim.From = 0;
            closeWifiAnim.To = -this.wifiPopupGrid.ActualHeight;
            closeWifiAnim.EasingFunction = new QuadraticEase();
            closeWifiAnim.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            Storyboard.SetTarget(closeWifiAnim, this.wifiPopupTransform);
            Storyboard.SetTargetProperty(closeWifiAnim, new PropertyPath("Y"));
            this.closeWifiPopupAnimation.Completed += (_, __) => this.wifiPopupOpen = false;
        }

        private void wifiPopupGrid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.wifiPopupOpen = false;
            this.closeWifiPopupAnimation.Begin();
        }

        private void DayPivot_Loaded(object sender, RoutedEventArgs e)
        {
            trySelectCurrentDay();
        }

        private void DayPivot_LoadedPivotItem(object sender, PivotItemEventArgs e)
        {
            //trySelectCurrentDay();
        }

        private void trySelectCurrentDay()
        {
            if (this.mensaplan == null)
                return;

            DateTime searchday = DateTime.Now;
            if (searchday.Hour > 14)
            {
                searchday = searchday.AddDays(1);
            }
            if (searchday.DayOfWeek == DayOfWeek.Saturday)
            {
                searchday = searchday.AddDays(2);
            }
            else if (searchday.DayOfWeek == DayOfWeek.Sunday)
            {
                searchday = searchday.AddDays(1);
            }

            Mensaplan.Tag todayItem = this.mensaplan.Tage.Where(tag => tag.Date == searchday.Date).FirstOrDefault();

            if (todayItem == null)
                return;

            // we are already on the UI thread, but the Pivot control is very buggy...
            this.Dispatcher.BeginInvoke(() =>
                this.DayPivot.SelectedItem = todayItem);
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