using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Threading;
using KAIT.Kiosk.ViewModel;
using KAIT.Kiosk.Kiosk;
using KAIT.Kiosk.Services;
using KAIT.Kiosk.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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


namespace KAIT.Kiosk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// asd

    public partial class MainWindow : Window
    {
        //t

      
        //KinectServices _kinect;
        public MainWindow()
        {
         
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            this.KeyDown += new KeyEventHandler(Window_KeyDown);
            DispatcherHelper.Initialize();
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
            var viewmodel = this.DataContext as MainViewModel;
            viewmodel.RowWidth = this.ActualWidth;
            
        }

      

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as MainViewModel;

           // vm.KioskState = "ActiveEngage";
           vm.StartCal();
            //switch (count)
            //{
            //    case 0:
            //        vm.KioskState = "PassiveAttract";
          //  VisualStateManager.GoToElementState(mainGrid, "ActiveEngage", true);
            //        break;
            //    case 1:
            //        //VisualStateManager.GoToElementState(mainGrid, "ActiveAttract", true);
            //        break;
            //    case 2:
            //        //VisualStateManager.GoToElementState(mainGrid, "PassiveEngage", true);
            //        break;
            //    case 3:
            //        //VisualStateManager.GoToElementState(mainGrid, "PassiveAttract", true);
            //        break;
            //    default:
            //        break;
            //}
            //count++;
        }

        private void button_Copy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //var service = SimpleIoc.Default.GetInstance<IDemographicsService>();
            //service.Listen(null);
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var vm = this.DataContext as MainViewModel;

            //Window target = (Window)sender;
            //target.Close();

            // Launch the console app
            if (e.Key == Key.Escape)
            {
                vm.StartCal();
                //ProcessStartInfo pInfo = new ProcessStartInfo("KAIT.ConfigSettingsConsole.exe");
                //Process.Start(pInfo);

                //var mainWindow = Application.Current.Windows.Cast<Window>().SingleOrDefault(w => w.Name == "KAIT.Kiosk2MainWindow");
                //if (mainWindow == null)
                //    mainWindow = new KAIT.Kiosk2.MainWindow();

                //mainWindow.Show();
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var contentConfigWindow = Application.Current.Windows.Cast<Window>().SingleOrDefault(w => w.Name == "MediaContentConfiguration");
                if (contentConfigWindow == null)
                    contentConfigWindow = new KAIT.Kiosk.MediaContentConfiguration();

                var mediaContentConfigurationViewModel = new MediaContentConfigurationViewModel(vm.ConfigurationProvider);
                contentConfigWindow.DataContext = mediaContentConfigurationViewModel;
                contentConfigWindow.ShowDialog();
            }
        }

       
    }
}
