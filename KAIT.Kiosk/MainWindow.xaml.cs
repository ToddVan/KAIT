//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.


using GalaSoft.MvvmLight.Threading;
using KAIT.Kiosk.ViewModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;


namespace KAIT.Kiosk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// asd

    public partial class MainWindow : Window
    {
        
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

        
           vm.StartCal();
       
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var vm = this.DataContext as MainViewModel;

            // Launch the console app
            if (e.Key == Key.Escape)
            {
                vm.StartCal();
               
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
