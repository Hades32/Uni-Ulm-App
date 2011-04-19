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

            if (needsUpdate)
            {
                this.progress.IsIndeterminate = true;
                this.loadingPopup.IsOpen = true;
            }

            initializeAnimations();

            System.Threading.ThreadPool.QueueUserWorkItem((x) =>
                {
                    // only check for the welcome network when we use WiFi
                    var network = Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType;
                    if (network == Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.Wireless80211
                     || network == Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.Ethernet)
                    {
                        this.Dispatcher.BeginInvoke(() =>
                            this.progress.IsIndeterminate = true);

                        welcome.finishedLogin += new Action(hasConnection);
                        welcome.needsLogin += new Action(needsLogin);
                        welcome.loginError += new Action<string>(welcome_loginError);
                        welcome.checkConnection();
                    }
                    else if (network == Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.None)
                    {
                        if (this.needsUpdate)
                            this.welcome_loginError("No network available");
                    }
                    else
                    {
                        hasConnection();
                    }
                });
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
            this.DayPivot.ItemsSource = mp.Tage;
            this.mensaplan = mp;

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
            Mensaplan.Tag todayItem;
            todayItem = mp.Tage.Where(tag => tag.Date == searchday.Date).FirstOrDefault();


            if (todayItem != null)
            {
                //WP7 bug (?) Header isn't updated for selected item change
                this.DayPivot.Loaded += (_, __) =>
                        this.DayPivot.SelectedItem = todayItem;
            }

            if (mp.isLoaded == false)
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
            this.hasConnection();
        }

        private void closeOptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.closePopupAnimation.Begin();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            // enable the back button to close the popup
            if (this.optionsPopup.IsOpen)
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