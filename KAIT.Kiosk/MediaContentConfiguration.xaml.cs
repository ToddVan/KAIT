using KAIT.Common.Services.Messages;
using KAIT.Kiosk.ViewModel;
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
using System.Windows.Shapes;

namespace KAIT.Kiosk
{
    /// <summary>
    /// Interaction logic for MediaContentConfiguration.xaml
    /// </summary>
    public partial class MediaContentConfiguration : Window
    {
        public MediaContentConfiguration()
        {
            InitializeComponent();
            //DataContext = new MediaContentConfigurationViewModel(new ConfigurationProvider());
        }
    }
}
