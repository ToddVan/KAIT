//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Kinect.Biometric
{
    using Extensions;
    using KAIT.Biometric.Services;
    using KAIT.Common.Interfaces;
    using KAIT.Common.Sensor;
    using KAIT.Common.Services.Messages;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Face;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Xml.Linq;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        
        IDemographicsService _demographicsService;
        
        ISensorService<KinectSensor> _sensorService;
        

        Dictionary<ulong, DateTime> _activeUsers = new Dictionary<ulong, DateTime>();

        /// <summary>
        /// Maximum value (as a float) that can be returned by the InfraredFrame
        /// </summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        /// <summary>
        /// The value by which the infrared source data will be scaled
        /// </summary>
        private const float InfraredSourceScale = 0.75f;


        /// <summary>
        /// Smallest value to display when the infrared data is normalized
        /// </summary
        private const float InfraredOutputValueMinimum = 0.01f;


        /// <summary>
        /// Largest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        private WriteableBitmap colorBitmap = null;
    
        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        private Bitmap _currentPlayImage;

        
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            Init();

        
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        void _biometricSampling_PlayerIdentified(object sender, BiometricData e)
        {
            Dispatcher.Invoke(() =>
            {
                var output = string.Format("\r\n" + "{6} @ " + DateTime.Now.ToLongTimeString() + " > Gender: {0} - Age: {1} - Confidence: {2} - Id: {3} Match: {4} FaceID: {5}",
                                            e.Gender.ToString(),
                                            e.Age,
                                            e.GenderConfidence,
                                            e.TrackingId,
                                            e.FaceMatch,
                                            e.FaceID,
                                            e.TrackingState);
                textLog.Text += output;

                if(e.FaceImage != null)
                {
                    _currentPlayImage = e.FaceImage;
                    face.Source = ToBitmapImage(e.FaceImage);
                }
                
            });
        }

        private async void Init()
        {

            _demographicsService = new BiometricTelemetryService(ConfigurationManager.AppSettings["Azure.Hub.Biometric"]);
            _demographicsService.DemographicsReceived += _demographicsService_DemographicsReceived;

            _demographicsService.DemographicsProcessingFailure += _demographicsService_DemographicsProcessingFailure;
            
            _sensorService = new KinectSensorService(_demographicsService);
            _sensorService.Open();     



        }

        void _demographicsService_DemographicsProcessingFailure(object sender, string e)
        {
            throw new NotImplementedException();
        }

        void _demographicsService_DemographicsReceived(object sender, BiometricData e)
        {
            Dispatcher.Invoke(() =>
            {
                var output = string.Format("\r\n" + "{6} @ " + DateTime.Now.ToLongTimeString() + " > Gender: {0} - Age: {1} - Confidence: {2} - Id: {3} Match: {4} FaceID: {5}",
                                            e.Gender.ToString(),
                                            e.Age,
                                            e.GenderConfidence,
                                            e.TrackingId,
                                            e.FaceMatch,
                                            e.FaceID,
                                            e.TrackingState);
                textLog.Text += output;


                if (e.FaceImage != null)
                {
                    _currentPlayImage = e.FaceImage;
                    face.Source = ToBitmapImage(e.FaceImage);
                }
            });
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;



        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }



        

        
        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
        
            if (_sensorService != null)
                _sensorService.Close();

            
        }

        
   

        public Bitmap CreateBitmap( WriteableBitmap bitmap)
        {
            System.Drawing.Bitmap bmp;

            using (MemoryStream outStream = new MemoryStream())
            {
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)bitmap));
                enc.Save(outStream);
                outStream.Seek(0, SeekOrigin.Begin);
                bmp = new System.Drawing.Bitmap(outStream);
                //  bmp = await EnhanceImage(bmp);

                //  bmp =  ResizeImage((Image)bmp,(int)(bmp.Width * 2.0),(int)( bmp.Height * 2.0));
            }


            try
            {
                //if (Properties.Settings.Default.DebugImages)
                //    bmp.Save("C:\\TEMP\\" + trackingId + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                var s = ex;
            }
            return bmp;
        }

        private  BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
  
      

   
   
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(UserID.Text))
            {
                _demographicsService.EnrollFace(UserID.Text, _currentPlayImage);
            }
            
        }
    }
}
