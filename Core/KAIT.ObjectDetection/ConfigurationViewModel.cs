using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using KAIT.Common.Interfaces;
using System.Windows.Data;
using System.Globalization;


namespace KAIT.ObjectDetection.ViewModel
{    
    public class ConfigurationViewModel : ViewModelBase
    {
        ObjectDetectionService _manipulationService;        

        public event EventHandler Closed;

        public WriteableBitmap DepthBitmap {
            get { 
                return _manipulationService.DepthBitmap; }
        }

        public WriteableBitmap TopViewDepthBitmap
        {
            get { return _manipulationService.TopViewDepthBitmap; }
        }

        public ObservableCollection<string> Interactions
        {
            get { return _manipulationService.Interactions; }            
        }
        public int StartColSamplingIndex
        {
            get { return _manipulationService.StartColSamplingIndex; }
            set { _manipulationService.StartColSamplingIndex = value; }
        }
        public int EndColSamplingIndex
        {
            get { return _manipulationService.EndColSamplingIndex; }
            set { _manipulationService.EndColSamplingIndex = value; }
        }

        public int SamplingRow
        {
            get { return _manipulationService.SamplingRow; }
            set { _manipulationService.SamplingRow = value; }
        }
        //How big in width must an object be before we consider it a target
        public int ObjectSizeThreshold
        {
            get { return _manipulationService.ObjectSizeThreshold; }
            set { _manipulationService.ObjectSizeThreshold = value; }
        }

        public ushort ObjectDetectionDepthThreshold
        {
            get { return _manipulationService.ObjectDetectionDepthThreshold; }
            set { _manipulationService.ObjectDetectionDepthThreshold = value; }
        }

        public int ObjectDepthTolerance
        {
            get { return _manipulationService.ObjectDepthTolerance; }
            set { _manipulationService.ObjectDepthTolerance = value; }
        }

        public int ObjectParimeterBuffer
        {
            get { return _manipulationService.ObjectParimeterBuffer; }
            set { _manipulationService.ObjectParimeterBuffer = value; }
        }
        //How far much padding do we allow for when we are detecting that someone is touching the object
        //for example you can set this so we "see the touch coming" by looking well ahead of the item
        public int InteractionFrontBuffer
        {
            get { return _manipulationService.InteractionFrontBuffer; }
            set { _manipulationService.InteractionFrontBuffer = value; }
        }
        public int InteractionBackBuffer
        {
            get { return _manipulationService.InteractionBackBuffer; }
            set { _manipulationService.InteractionBackBuffer = value; }
        }

        public int MissingDataTolerance
        {
            get { return _manipulationService.MissingDataTolerance; }
            set { _manipulationService.MissingDataTolerance = value; }
        }

        public bool IsZeroDepth
        {
            get { return _manipulationService.IsZeroDepth; }
            set
            {
                _manipulationService.IsZeroDepth = value;
                RaisePropertyChanged("IsZeroDepth");
            }
        }

        public bool IsCalibrating
        {
            get { return _manipulationService.IsCalibrating; }
            set 
            { 
                _manipulationService.IsCalibrating = value;                
                RaisePropertyChanged("IsCalibrating");
            }
        }

        public bool IsWindowVisible
        {
            get { return _manipulationService.IsWindowVisible; }
            set
            {
                _manipulationService.IsWindowVisible = value;
                RaisePropertyChanged("IsWindowVisible");
            }
        }

        public int ObjectCount
        {
            get 
            {
                if (_manipulationService == null)
                    return -1;

                return _manipulationService.ObjectCount; 
            }
        }

        public string ObjectDetectionServiceState
        {
            get
            {
                if (_manipulationService == null || string.IsNullOrEmpty(_manipulationService.ServiceState))
                    return KAIT.Common.ServiceStates.NotReady.ToString();                

                return _manipulationService.ServiceState;
            }
        }

        public RelayCommand StartService { get; private set; }
        public RelayCommand StopService { get; private set; }        
        public RelayCommand SaveCalibration { get; private set; }
        public RelayCommand LoadCalibration { get; private set; }
        public RelayCommand<Window> CloseCommand { get; private set; }
        public ConfigurationViewModel(IItemInteractionService manipulationService)
        {
            Messenger.Default.Register<string>(this, (message) =>
            {
                //Debug.WriteLine("CVM msg recieved: " + message);
                switch (message.ToUpper())
                {
                    case "STARTCALIBRATION":
                        this.Start();
                        break;
                    case "STOPCALIBRATION":
                        this.Stop();
                        break;
                }
            });
            _manipulationService = manipulationService as ObjectDetectionService;

            _manipulationService.IsWindowVisible = true; 

            this.SaveCalibration = new RelayCommand(() =>
            {
                if (_manipulationService.IsCalibrating || _manipulationService.ObjectCount == 0)
                    MessageBox.Show("Please complete calibration and ensure that the 'Calibrate' setting is unchecked and at least one Object is Detected before Saving Calibration.");
                else
                    MessageBox.Show(_manipulationService.SaveCalibration());
            });

            this.LoadCalibration = new RelayCommand(() =>
            {
                _manipulationService.LoadCalibration();
            });

            this.CloseCommand = new RelayCommand<Window>(
                (window) => 
                    {
                        _manipulationService.IsWindowVisible = false;
                        _manipulationService.IsCalibrating = false;
                        window.Close();
                        var mainWindow = Application.Current.Windows.Cast<Window>().SingleOrDefault(w => w.Name == "MainWindow");
                        if (mainWindow != null)
                        {
                            mainWindow.Show();
                        }
                    },
                (window) =>  window != null
                );

            this.StartService = new RelayCommand(() => { _manipulationService.Start(); });
            this.StopService = new RelayCommand(() => { _manipulationService.Stop(); });

            this._manipulationService.PropertyChanged += _manipulationService_PropertyChanged;

        }
        
        void _manipulationService_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("ServiceState"))
            {
                RaisePropertyChanged("ObjectDetectionServiceState");
            }
            else if (e.PropertyName.Equals("ServiceConfiguration"))
            {
                //refreshbindings                
                RaisePropertyChanged("StartColSamplingIndex");
                RaisePropertyChanged("EndColSamplingIndex");
                RaisePropertyChanged("SamplingRow");
                RaisePropertyChanged("ObjectSizeThreshold");
                RaisePropertyChanged("ObjectDetectionDepthThreshold");
                RaisePropertyChanged("ObjectDepthTolerance");
                RaisePropertyChanged("ObjectParimeterBuffer");
                RaisePropertyChanged("InteractionFrontBuffer");
                RaisePropertyChanged("InteractionBackBuffer");
                RaisePropertyChanged("MissingDataTolerance");
                RaisePropertyChanged("IsZeroDepth");

                if (!IsCalibrating)
                    IsCalibrating = true;
            }
            else
                RaisePropertyChanged(e.PropertyName);
        }

        public void Start()
        {
            _manipulationService.Start();
        }

        public void Stop()
        {
            _manipulationService.Stop();
        }      
    }

    public class SaveCalibrationViewModel : ViewModelBase
    {
        private string _message;

        public RelayCommand OkCommand { get; private set; }
        public SaveCalibrationViewModel(string saveCalibrationMessage)
        {
            _message = saveCalibrationMessage;

            this.OkCommand = new RelayCommand(() =>
            {
                var saveCalibrationWindow = Application.Current.Windows.Cast<Window>().SingleOrDefault(w => w.Name == "SaveCalibrationWindow");
                if (saveCalibrationWindow != null)
                    saveCalibrationWindow.Close();
            });
        }
        public string SaveCalibrationMessage
        {
            get { return _message; }
            set
            {
                if (_message != value)
                {
                    _message = value;
                }
            }
        }
    }
}


namespace KAIT.ObjectDetection.Converters
{
    public class BooleanInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
