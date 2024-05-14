using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SqliteDataReader.Controls
{
    /// <summary>
    /// Interaction logic for Counter.xaml
    /// </summary>
    public partial class Counter : UserControl
    {
        public static DependencyProperty CountProp;
        public int Count
        {
            get => (int)base.GetValue(CountProp);
            set => base.SetValue(CountProp, value);
        }

        public Counter()
        {
            InitializeComponent();
        }

        static Counter()
        {
            CountProp = DependencyProperty.Register("Count", typeof(int), typeof(Counter), new PropertyMetadata(100));
        }

        private void UpBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DownBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
