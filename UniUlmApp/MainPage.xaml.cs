using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.Phone.Controls;

namespace UniUlmApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Konstruktor
        public MainPage()
        {
            InitializeComponent();
        }

        // Daten für die ViewModel-Elemente laden
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var mp = new Mensaplan("http://www.uni-ulm.de/mensaplan/mensaplan.xml");
            mp.Loaded += new Action<Mensaplan>(mensaplan_Loaded);
        }

        void mensaplan_Loaded(Mensaplan mp)
        {
            this.DayPivot.ItemsSource = mp.Tage;
            var heuteItem = mp.Tage.Where(tag => tag.DateName == "Heute").FirstOrDefault();
            if (heuteItem != null)
                this.DayPivot.SelectedItem = heuteItem;
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