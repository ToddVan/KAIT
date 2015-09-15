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


using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using KAIT.Common;
using KAIT.Common.Interfaces;
using System.Linq;
using GalaSoft.MvvmLight.CommandWpf;
using KAIT.Common.Services.Messages;
using KAIT.Common.Sensor;

namespace KAIT.Kiosk.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        readonly double DEPTH_WIDTH = 640;
        IKioskInteractionService _kioskInteractionService;
        IItemInteractionService _itemInteractionService;
        ISpeechService _speechService;        
        DateTimeOffset _lastStateChange = DateTimeOffset.Now;
        string _speechText;
        string _lastKioskState;
        string _kioskState;
        IConfigurationProvider _configurationProvider;

        public IConfigurationProvider ConfigurationProvider
        {
            get { return _configurationProvider; }
            set
            {
                if (_configurationProvider == value)
                    return;
                _configurationProvider = value;
                RaisePropertyChanged("ConfigurationProvider");
            }
        }

        public string KioskState
        {
            get { return _kioskState; }
            set
            {
                if (_kioskState == value)
                    return;
                _lastKioskState = _kioskState;
                _kioskState = value;  
                RaisePropertyChanged("KioskState");
            }
        }

        string _itemState = "None";
        public string ItemState
        {
            get { return _itemState; }
            set
            {
                if (_itemState == value)
                    return;
                _itemState = value;
                RaisePropertyChanged("ItemState");
            }
        }

        ObservableCollection<double> _bodyTrack;
        public ObservableCollection<double> BodyTrack
        {
            get { return _bodyTrack; }
            set
            {
                if (_bodyTrack == value)
                    return;
                _bodyTrack = value;
                RaisePropertyChanged("BodyTrack");
            }
        }


        public WriteableBitmap ColorBitmap
        {
            get {
                if (_kioskInteractionService is KinectSensorService)
                    return (_kioskInteractionService as KinectSensorService).colorBitmap;

                return null;
            }
           
        }
       
        public double RowWidth { get; set; }

      

        private bool _enableDiagnostics;
        public bool EnableDiagnostics
        {
            get { return _enableDiagnostics; }
            set
            {
                if (_enableDiagnostics == value)
                    return;
                _enableDiagnostics = value;
                RaisePropertyChanged("EnableDiagnostics");
            }
        }

        public RelayCommand OpenObjectDetection { get; private set; }




        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IKioskInteractionService interactionService,
                             ISpeechService speechService,
                             IItemInteractionService manipulationService,
                             IConfigurationProvider configurationProvider)
        {
            this.RowWidth = DEPTH_WIDTH;  
        
            _kioskInteractionService = interactionService;
            _kioskInteractionService.KioskStateChanged += _kioskInteractionService_KioskStateChanged;
            _kioskInteractionService.BodyTrackUpdate += _kioskInteractionService_BodyTrackUpdate;
            

            BodyTrack = new ObservableCollection<double>() { -75, -75, -75, -75, -75, -75 };            
            
            _itemInteractionService = manipulationService;
            _itemInteractionService.ServiceStateChanged += _itemInteractionService_ServiceStateChanged;
            _itemInteractionService.ItemInteraction += _itemInteractionService_ItemInteraction;
            _itemInteractionService.PropertyChanged += _itemInteractionService_PropertyChanged;
            
            _speechService = speechService;
            _speechService.SpeechRecognized += _speechService_SpeechRecognized;
            _speechService.SpeechRejected += _speechService_SpeechRejected;
            _speechService.StartListening();
            _speechService.Speak("Kiosk Ready");

            Messenger.Default.Register<string>(this, (msg) => {

                if (msg == "SPECIALSTOP")
                {
                    Debug.WriteLine("MVM SPECIALSTOP " + _lastKioskState);
                    this.KioskState = "CloseSpecial";
                }                    
            });

            this.OpenObjectDetection = new RelayCommand(() =>
            {
                ShowObjectDetectionWindow(true);
            });

            _configurationProvider = configurationProvider;
            _configurationProvider.ConfigurationSettingsChanged += _configurationProvider_ConfigurationSettingsChanged;
            GetConfig();

            // Show Object Detection Window on start up
            ShowObjectDetectionWindow();
        }

        void _configurationProvider_ConfigurationSettingsChanged(object sender, KioskConfigSettingsEventArgs e)
        {
            GetConfig(e.ConfigSettings);
        }

        private void GetConfig(IConfigSettings configSettings = null)
        {
            EnableDiagnostics = configSettings == null ? _configurationProvider.Load().EnableDiagnostics : configSettings.EnableDiagnostics;            
        }

        void _itemInteractionService_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //raise property changed to refresh bindings on this ViewModel
            if (e.PropertyName.Equals("ServiceState"))
            {
                RaisePropertyChanged("ObjectDetectionServiceState");
            }
            else
                RaisePropertyChanged(e.PropertyName);
        }

        private void ShowObjectDetectionWindow(bool ForceCalibrationWindow = false)
        {
            var calibrationWindow = Application.Current.Windows.Cast<Window>().SingleOrDefault(w => w.Name == "CalibrationConfigurationWindow");
            if (calibrationWindow != null)
            {
                var interactionServiceConfig = calibrationWindow.DataContext as KAIT.ObjectDetection.ViewModel.ConfigurationViewModel;
                interactionServiceConfig.IsWindowVisible = true;
                interactionServiceConfig.IsCalibrating = true;
                calibrationWindow.ShowDialog();
            }
            else // Create and show new window on start up
            {
                //Only show calibration window we don't have tracked objects or we are focing calibration
                if (_itemInteractionService.ObjectCount == 0 || ForceCalibrationWindow)
                {
                    calibrationWindow = new KAIT.ObjectDetection.UI.Calibration();                    
                    var interactionServiceConfig = calibrationWindow.DataContext as KAIT.ObjectDetection.ViewModel.ConfigurationViewModel;
                    interactionServiceConfig.IsWindowVisible = true;
                    interactionServiceConfig.IsCalibrating = true;
                    calibrationWindow.ShowDialog();
                }                
            }
        }

       
        void _kioskInteractionService_BodyTrackUpdate(object sender, BodyTrackEventArgs e)
        {
            // initialize them all to inactive to "remove dead bodies"
            for (int ii = 0; ii < this.BodyTrack.Count; ii++)
            {
                this.BodyTrack[ii] = -75;
            }

            for (int i = 0; i < e.BodyTrack.Length; i++)
            {
                if(i <= this.BodyTrack.Count())
                     this.BodyTrack[i] = (e.BodyTrack[i] * this.RowWidth / DEPTH_WIDTH) - 37.5f;
            }
        }

        void _itemInteractionService_ItemInteraction(object sender, KioskStateEventArgs e)
        {
            ItemState = e.ItemSelected;
        }

        void _itemInteractionService_ServiceStateChanged(object sender, ServiceStateEventArgs e)
        {
            Debug.WriteLine("manipluaiton service state change:" + e.State);
            switch (e.State)
            {
                case ServiceStates.Error:
                    this.KioskState = "ManipulationError";
                    break;
                case ServiceStates.NotReady:
                    StartCalibration();
                    break;
                default:
                    break;
            }           
        }

        void _speechService_SpeechRejected(object sender, Microsoft.Speech.Recognition.SpeechRecognitionRejectedEventArgs e)
        {
        }

        void _speechService_SpeechRecognized(object sender, Microsoft.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            const double ConfidenceThreshold = 0.7;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                _speechText = e.Result.Semantics.Value.ToString();
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "START CALIBRATION":
                        _speechService.Speak("Working on it Boss. Might I say myself that this is a great presentation!");
                        StartCalibration();
                        break;
              
                    case "CALIBRATION COMPLETE":                        
                        //since the calibration window is a modal dialog, this won't get called until the calibration window is closed, which is to late therefore,
                        //don't use speech to close this calibration window as we don't want to allow user to keep calibration open during active sessions
                        //StopCalibration();
                        break;
                }
            }
        }

        private void StopCalibration()
        {
            Messenger.Default.Send<string>("StopCalibration");
            KioskState = "HideCalibration";
            _speechService.Speak("Confirming Calibration Process Completed");
        }

        public void StartCal()
        {
            StartCalibration();
        }
        private void StartCalibration()
        {
            ShowObjectDetectionWindow(true);

            Debug.WriteLine("STARTCALIBRATION");
            _speechService.Speak("Calibration Process Started");
            KioskState = "ShowCalibration";
            Messenger.Default.Send<string>("StartCalibration");
        }



        void _kioskInteractionService_KioskStateChanged(object sender, KioskStateEventArgs e)
        {
            Debug.WriteLine("MainViewModel kiosk state changed: " + e.KioskState);
            switch (e.KioskState)
            {
                case "Special":
                    Messenger.Default.Send<string>("SPECIALSTART");
                    this.KioskState = "Special";
                    break;
                case "Ready":
                    this.KioskState = "SensorReady";
                    break;
                default:
                    this.KioskState = e.KioskState;
                    break;
            }
        }
    }

}