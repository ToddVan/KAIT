using Inception.Common.Services.Messages;
using Inception.ConfigSettingsConsole.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Inception.ConfigSettingsConsole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ConfigurationViewModel();
        }

    }
}
