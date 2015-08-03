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
using KAIT.ObjectDetection.ViewModel;

namespace KAIT.ObjectDetection.UI
{
    /// <summary>
    /// Interaction logic for Calibration.xaml
    /// </summary>
    public partial class Calibration : Window
    {
        ConfigurationViewModel _viewModel;

        public Calibration()
        {
            InitializeComponent();

            this.Loaded += (s, e) => {
                _viewModel = this.DataContext as ConfigurationViewModel;
                //_viewModel.Closed += (sender, args) =>
                //{
                //    var parent = this.Parent as FrameworkElement;
                //    VisualStateManager.GoToElementState(parent, "HideCalibration", true);
                //};

                //if (_viewModel != null)
                //    this.Closing += _viewModel.OnWindowClosing;
            };

            this.Closed += Calibration_Closed;
        }

        void Calibration_Closed(object sender, EventArgs e)
        {
            //in case the use closes the view without using the Close button
            _viewModel.IsCalibrating = false;
            _viewModel.IsWindowVisible = false;
        }

        //public void Start()
        //{
        //    _viewModel.Start();
        //}
    }
}
