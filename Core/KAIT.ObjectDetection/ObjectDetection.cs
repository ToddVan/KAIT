
using Microsoft.Kinect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections.ObjectModel;
using KAIT.Common;
using KAIT.Common.Interfaces;
using KAIT.Common.Sensor;
using KAIT.Common.Services.Messages;

namespace KAIT.ObjectDetection
{
    public class ItemState
    {
        public bool Touched = false;
        public bool DeviceRemoved = false;
        public bool DeviceReplaced = false;
        public int NosiePixelCount = 0;

    }



    //public enum ManipulationState
    //{
    //    NoTrack,
    //    Touched,
    //    Released,
    //    DeviceRemoved,
    //    DeviceReplaced

    //}

    public class TrackedItem
    {
        public int DepthArrayPointer;
        public int Width;
        public int Height;
        public int Row;
        public int Col;
        public BitmapSource Image;
        public ItemState DS;
        public string ObjectID;
        public List<TrackedPixel> TrackedPixels;
        public int TouchSampleCount;
        public int NoTouchSampleCount;

    }
    public class TrackedPixel
    {
        public ushort OrginalDepthValue;
        public int PixelIndexInDepthFrame;

    }

    public class ObjectDetectionConfiguration
    {
        public int SamplingRow { get; set; }        
        public int StartColSamplingIndex { get; set; }        

        public int EndColSamplingIndex { get; set; }        

        public bool IsCalibrating { get; set; }        

        public int ObjectDepthTolerance { get; set; }        

        public ushort ObjectDetectionDepthThreshold { get; set; }

        public int InteractionBackBuffer { get; set; }

        public int InteractionFrontBuffer { get; set; }        

        public int ObjectSizeThreshold { get; set; }        

        public int ObjectParimeterBuffer { get; set; }
        public int MissingDataTolerance { get; set; }

        public bool IsZeroDepth { get; set; }
        
        public ObservableCollection<TrackedItem> Objects { get; set; }        

    }

    

    public class ObjectDetectionService : INotifyPropertyChanged, IItemInteractionService
    {
        ISensorService<KinectSensor> _sensorService;
        DepthFrameSource _depthFrameSource;
        DepthFrameReader _depthFrameReader;        
        ObjectDetectionConfiguration _serviceConfiguration = new ObjectDetectionConfiguration();

        private const int _topViewBitsPerPixelConversionValue = 3; //4; 4 is for PixelFormats.Bgr32
        private const int _maxTopViewDepthIndex = 512 * 424 * _topViewBitsPerPixelConversionValue; //651264 512 * 424 * 3  //868352; //512 * 424 * 4
        private const int _bytesPerRowTopView = 512 * _topViewBitsPerPixelConversionValue; //1563 512 * 3 // 2048; //512 * 4;        
        private const decimal _millimetersPerRow = 4000 / 424;            

        bool _isRunning;

        string _serviceState;
        public string ServiceState
        {
            get { return _serviceState; }
            set
            {
                if (_serviceState == value)
                    return;
                _serviceState = value;
                OnPropertyChanged("ServiceState");
            }
        }
        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<ServiceStateEventArgs> ServiceStateChanged;

        public event EventHandler<KioskStateEventArgs> ItemInteraction;

        /// <summary>
        /// Description of the data contained in the depth frame
        /// </summary>
        /// 
        private FrameDescription _depthFrameDescription;
        public FrameDescription DepthFrameDescription
        {
            get { return _depthFrameDescription; }
            set
            {
                _depthFrameDescription = value;
                // create the bitmap to display
                this.DepthBitmap = new WriteableBitmap(DepthFrameDescription.Width, DepthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

                // allocate space to put the pixels being received and converted
                _depthPixels = new byte[_depthFrameDescription.Width * _depthFrameDescription.Height];

                //TopView                
                this.TopViewDepthBitmap = new WriteableBitmap(DepthFrameDescription.Width, DepthFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr24, null);
                _topViewDepthPixels = new byte[_depthFrameDescription.Width * _depthFrameDescription.Height * _topViewBitsPerPixelConversionValue];
            }
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>

        bool _isCalibrating = false;
        public bool IsCalibrating
        {
            get { return _isCalibrating; }
            set
            {
                _isCalibrating = value;                
                OnPropertyChanged("IsCalibrating");
            }
        }

        bool _isWindowVisible = false;
        public bool IsWindowVisible
        {
            get { return _isWindowVisible; }
            set
            {
                _isWindowVisible = value;
                OnPropertyChanged("IsWindowVisible");
            }
        }
        public string SaveCalibration(bool SaveAs = false)
        {
            if (!_isCalibrating)
            {
                string filename = "";

                if (objects.Count > 0)
                {
                    if (SaveAs)
                    {
                        Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                        //dlg.InitialDirectory = @"C:\";
                        dlg.FileName = "ObjectDetectionConfig";
                        dlg.DefaultExt = ".txt";
                        dlg.Filter = "Text documents (.txt)|*.txt";

                        // Show save file dialog box
                        Nullable<bool> result = dlg.ShowDialog();

                        // Process save file dialog box results
                        if (result == true)
                        {
                            filename = dlg.FileName;
                        }

                    }
                    else
                    {
                        filename = "ObjectDetectionConfig.txt";
                    }

                    // Process save file dialog box results
                    if (filename != "")
                    {
                        try
                        {
                            // Save document
                            using (FileStream fs = new FileStream(filename, FileMode.Create))
                            {
                                using (StreamWriter sw = new StreamWriter(fs))
                                {
                                    var configStream = JsonConvert.SerializeObject(ServiceConfiguration, Formatting.Indented);
                                    sw.Write(configStream);
                                    sw.Flush();
                                    fs.Flush();
                                    //fs.Close(); 
                                }
                            }                            
                            return "Calibration Saved.";
                        }
                        catch (Exception)
                        {                            
                            return "An Error occurred saving calibration settings.";
                        }
                    }
                    else
                        return "Please select a file path to save settings.";
                }
                else
                    return "At least one object must be detected before saving calibration settings.";
            }
            else
                return "Complete Calibrating before saving settings.";
        }

        public void LoadCalibration()
        {
            
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();            
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                string filename = dlg.FileName;                    
                try
                {
                    string config = string.Empty;
                    using (FileStream fs = new FileStream("ObjectDetectionConfig.txt", FileMode.Open))
                    {
                        StreamReader sr = new StreamReader(fs);
                        config = sr.ReadToEnd();
                        fs.Close();    
                    }
                    ServiceConfiguration = (ObjectDetectionConfiguration)JsonConvert.DeserializeObject(config, typeof(ObjectDetectionConfiguration));
                    objects = ServiceConfiguration.Objects;

                    OnPropertyChanged("ServiceConfiguration");
                }
                catch
                {
                    ResetConfiguraiontDefaults();
                    this.IsCalibrating = true;
                }
            }
        }

        public ulong ActivePlayerId { get; set; }        

        public ulong CorrelationPlayerId { get; set; }


        ObservableCollection<string> _interactions = new ObservableCollection<string>();

        public ObservableCollection<string> Interactions
        {
            get { return _interactions; }
            set { _interactions = value; }
        }

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private byte[] _depthPixels = null;

        /// Bitmap to display
        /// </summary>
        WriteableBitmap _depthBitmap;
        public WriteableBitmap DepthBitmap
        {
            get { return _depthBitmap; }
            private set
            {
                _depthBitmap = value;
                OnPropertyChanged("DepthBitmap");
            }
        }
        
        private byte[] _topViewDepthPixels = null;

        WriteableBitmap _topViewDepthBitmap;
        public WriteableBitmap TopViewDepthBitmap
        {
            get { return _topViewDepthBitmap; }
            private set
            {
                _topViewDepthBitmap = value;
                OnPropertyChanged("TopViewDepthBitmap");
            }
        }

        public ObjectDetectionConfiguration ServiceConfiguration
        {
            get { return _serviceConfiguration; }
            set { _serviceConfiguration = value; }
        }

        //These are being used to draw vertical lines on the screen for targeting purposes
        public int StartColSamplingIndex
        {
            get { return ServiceConfiguration.StartColSamplingIndex; }
            set { if(value < EndColSamplingIndex) ServiceConfiguration.StartColSamplingIndex = value; }
        }
        public int EndColSamplingIndex
        {
            get { return ServiceConfiguration.EndColSamplingIndex; }
            set { if (value > StartColSamplingIndex && value <= 512) ServiceConfiguration.EndColSamplingIndex = value; }
        }

        public int SamplingRow
        {
            get { return ServiceConfiguration.SamplingRow; }
            set { if (value <= 424) ServiceConfiguration.SamplingRow = value; }
        }

        //How big in width must an object be before we consider it a target
        public int ObjectSizeThreshold
        {
            get { return ServiceConfiguration.ObjectSizeThreshold; }
            set { ServiceConfiguration.ObjectSizeThreshold = value; }
        }

        //How much variance do we allow in depth data to determine if we are still looking at the same object
        //poor mans edge detection
        public int ObjectDepthTolerance
        {
            get { return ServiceConfiguration.ObjectDepthTolerance; }
            set { ServiceConfiguration.ObjectDepthTolerance = value; }
        }

        public int ObjectParimeterBuffer
        {
            get { return ServiceConfiguration.ObjectParimeterBuffer; }
            set { if (value > 0) ServiceConfiguration.ObjectParimeterBuffer = value; }
        }
        //How far much padding do we allow for when we are detecting that someone is touching the object
        //for example you can set this so we "see the touch coming" by looking well ahead of the item
        //Front/Back is from the Perspective of the customer
        public int InteractionFrontBuffer
        {
            get { return ServiceConfiguration.InteractionFrontBuffer; }
            set { ServiceConfiguration.InteractionFrontBuffer = value; }
        }
        public int InteractionBackBuffer
        {
            get { return ServiceConfiguration.InteractionBackBuffer; }
            set { ServiceConfiguration.InteractionBackBuffer = value; }
        }

        //How far should objects be from the sensor before we consider them a target
        public ushort ObjectDetectionDepthThreshold
        {
            get { return ServiceConfiguration.ObjectDetectionDepthThreshold; }
            set { ServiceConfiguration.ObjectDetectionDepthThreshold = value; }
        }

        public int MissingDataTolerance
        {
            get { return ServiceConfiguration.MissingDataTolerance; }
            set { ServiceConfiguration.MissingDataTolerance = value; }
        }

        public bool IsZeroDepth
        {
            get { return ServiceConfiguration.IsZeroDepth; }
            set 
            {
                if (value == true)
                {
                    ServiceConfiguration.ObjectDetectionDepthThreshold = 0;
                    ServiceConfiguration.ObjectDepthTolerance = 0;
                    ServiceConfiguration.InteractionBackBuffer = 0;

                    OnPropertyChanged("ObjectDetectionDepthThreshold");
                    OnPropertyChanged("ObjectDepthTolerance");
                    OnPropertyChanged("InteractionBackBuffer");
                }
                ServiceConfiguration.IsZeroDepth = value;
            }
        }

        public int ObjectCount
        {
            get { return objects.Count; }
        }
        //Objects we're tracking
        public ObservableCollection<TrackedItem> objects;

        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        public int threshold;

        public ObjectDetectionService(ISensorService<KinectSensor> sensorService)
        {
            _sensorService = sensorService;

            string config = string.Empty;
            try
            {
                using (FileStream fs = new FileStream("ObjectDetectionConfig.txt", FileMode.OpenOrCreate))
                {
                    StreamReader sr = new StreamReader(fs);
                    config = sr.ReadToEnd();
                    fs.Flush();
                }

                if (config == "")
                {
                    ResetConfiguraiontDefaults();
                    this.IsCalibrating = false;
                }
                else
                {
                    ServiceConfiguration = (ObjectDetectionConfiguration)JsonConvert.DeserializeObject(config, typeof(ObjectDetectionConfiguration));
                    objects = ServiceConfiguration.Objects;
                }
            }
            catch 
            {
                ResetConfiguraiontDefaults();
                this.IsCalibrating = true;
            }

            objects.CollectionChanged += objects_CollectionChanged;            

            _sensorService.Open();
            Start();

        }

        void objects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("ObjectCount");            
        }

        private void ResetConfiguraiontDefaults()
        {
            this.SamplingRow = 300;
            this.StartColSamplingIndex = 100;
            this.EndColSamplingIndex = 400;            
            this.ObjectDepthTolerance = 150;
            this.ObjectDetectionDepthThreshold = 2000;
            this.InteractionBackBuffer = 25;
            this.InteractionFrontBuffer = 50;
            this.ObjectSizeThreshold = 300;
            this.MissingDataTolerance = 5;

            ServiceConfiguration.Objects = new ObservableCollection<TrackedItem>();
            objects = ServiceConfiguration.Objects;
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;

            _depthFrameSource = _sensorService.Sensor.DepthFrameSource;
            this.DepthFrameDescription = _depthFrameSource.FrameDescription;
            _depthFrameReader = _depthFrameSource.OpenReader();
            _depthFrameReader.FrameArrived += _depthFrameReader_FrameArrived;

            
            if (IsCalibrating)
            {
                this.ServiceState = ServiceStates.NotReady.ToString();
                OnServiceStateChanged(ServiceStates.NotReady);
            }
            else
            {
                this.ServiceState = ServiceStates.Open.ToString();
                OnServiceStateChanged(ServiceStates.Open);
            }
        }

        public void Stop()
        {
            _isRunning = false;
            this.ServiceState = ServiceStates.Closed.ToString();
            if (_depthFrameReader != null)
                _depthFrameReader.FrameArrived -= _depthFrameReader_FrameArrived;
                
            OnServiceStateChanged(ServiceStates.Closed);

            //_isRunning = false;
            //if (_depthFrameReader != null && this.ServiceState != ServiceStates.Closed.ToString())
            //{
            //    _depthFrameReader.FrameArrived -= _depthFrameReader_FrameArrived;
            //    OnServiceStateChanged(ServiceStates.Closed);
            //}
        }


        private void _depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((_depthFrameDescription.Width * _depthFrameDescription.Height) == (depthBuffer.Size / _depthFrameDescription.BytesPerPixel)) &&
                            (_depthFrameDescription.Width == this.DepthBitmap.PixelWidth) && (_depthFrameDescription.Height == this.DepthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            var depthData = new ushort[_depthFrameDescription.Width * _depthFrameDescription.Height];

                            depthFrame.CopyFrameDataToArray(depthData);

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance                            
                            ProcessDepthFrameData(depthData, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);                            
                        }
                    }
                }
            }
        }


        private void RenderDepthPixels()
        {

            this.DepthBitmap.WritePixels(
                new Int32Rect(0, 0, this.DepthBitmap.PixelWidth, this.DepthBitmap.PixelHeight),
                _depthPixels,
                this.DepthBitmap.PixelWidth,
                0);

            this.TopViewDepthBitmap.WritePixels(
                new Int32Rect(0, 0, this.TopViewDepthBitmap.PixelWidth, this.TopViewDepthBitmap.PixelHeight),
                _topViewDepthPixels,
                this.TopViewDepthBitmap.PixelWidth * _topViewBitsPerPixelConversionValue,
                0);
        }        
        public void ProcessDepthFrameData(ushort[] frameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {            
            int x = 512 * (int)SamplingRow;
            int column = StartColSamplingIndex;
            int startSampleColIndex = x + StartColSamplingIndex;
            int endSampleColIndex = x + EndColSamplingIndex;

            //These are being used to draw vertical lines on the screen for targeting purposes
            int startColDisplayIndex = StartColSamplingIndex;
            int endColDisplayIndex = EndColSamplingIndex;
            ushort minDepthTolerance = (ushort)(ObjectDetectionDepthThreshold - ObjectDepthTolerance < 0 ? 0 : ObjectDetectionDepthThreshold - ObjectDepthTolerance);                       

            //TopView variables
            var startDepthSampleIndexTopView = (int)((ObjectDetectionDepthThreshold - ObjectDepthTolerance) / _millimetersPerRow) * _bytesPerRowTopView;
            var endDepthSampleIndexTopView = (int)((ObjectDetectionDepthThreshold + ObjectDepthTolerance) / _millimetersPerRow) * _bytesPerRowTopView;
            var startDepthLineIndexTopView = (int)(ObjectDetectionDepthThreshold / _millimetersPerRow) * _bytesPerRowTopView;
            var endDepthLineIndexTopView = (int)(ObjectDetectionDepthThreshold / _millimetersPerRow) * _bytesPerRowTopView + _bytesPerRowTopView;
            var startColDisplayIndexTopView = StartColSamplingIndex;
            var endColDisplayIndexTopView = EndColSamplingIndex;

            if (IsCalibrating)
                objects.Clear();

            if (IsWindowVisible)
            {
                // convert depth to a visual representation
                for (int i = 0; i < (int)(depthFrameDataSize / DepthFrameDescription.BytesPerPixel); ++i)
                {

                    #region TopView draw out Monitoring Area
                    int j = i * _topViewBitsPerPixelConversionValue;

                    _topViewDepthPixels[j] = 0;
                    _topViewDepthPixels[j + 1] = 0;
                    _topViewDepthPixels[j + 2] = 0;

                    //depth Threshold line
                    if (j >= startDepthLineIndexTopView && j <= endDepthLineIndexTopView)
                    {
                        _topViewDepthPixels[j] = 255;
                        _topViewDepthPixels[j + 1] = 128;
                        _topViewDepthPixels[j + 2] = 0;

                    }
                    else if (j >= startDepthSampleIndexTopView && j <= (startDepthSampleIndexTopView + _bytesPerRowTopView))
                    {
                        ////Depth Thresold - Start Tolerance Line                            
                        _topViewDepthPixels[j] = 0;
                        _topViewDepthPixels[j + 1] = 0;
                        _topViewDepthPixels[j + 2] = 255;
                    }
                    else if (j >= (endDepthSampleIndexTopView - _bytesPerRowTopView) && j <= endDepthSampleIndexTopView)
                    {
                        ////Depth Thresold - End Tolerance Line                            
                        _topViewDepthPixels[j] = 0;
                        _topViewDepthPixels[j + 1] = 0;
                        _topViewDepthPixels[j + 2] = 255;
                    }                        

                    //Columns lines
                    if ((j / _topViewBitsPerPixelConversionValue) == startColDisplayIndexTopView) //left line
                    {
                        _topViewDepthPixels[j] = 255;
                        _topViewDepthPixels[j + 1] = 0;
                        _topViewDepthPixels[j + 2] = 0;

                        startColDisplayIndexTopView += 512;
                    }
                    else if ((j / _topViewBitsPerPixelConversionValue) == endColDisplayIndexTopView) //right line
                    {
                        _topViewDepthPixels[j] = 255;
                        _topViewDepthPixels[j + 1] = 0;
                        _topViewDepthPixels[j + 2] = 0;

                        endColDisplayIndexTopView += 512;
                    }
                    #endregion TopView draw out Monitoring Area

                    // Get the depth for this pixel
                    ushort depth = frameData[i];

                    if (startSampleColIndex <= i && i <= endSampleColIndex && IsCalibrating)
                    {
                        column++;
                        startSampleColIndex++;
                        if (depth <= ObjectDetectionDepthThreshold && depth >= minDepthTolerance)
                        {
                            var temp = GetTargetWidth(i, endSampleColIndex, MissingDataTolerance, ObjectDetectionDepthThreshold, minDepthTolerance, frameData);

                            if (temp > ObjectSizeThreshold)
                            {
                                var NewObject = new TrackedItem() { DepthArrayPointer = i, Width = temp, Col = column, Row = SamplingRow };

                                //TopView use this Depth value which represents the actual top left depth of the object(FindTopOfObject will change this index to be the top left of the buffer boundary)
                                var objectActualTopLeftDepthPointer = NewObject.DepthArrayPointer;                                

                                //System.Diagnostics.Debug.Print("ObjectDepth:{0} = frameData[objectActualTopLeftDepthPointer{1}]", frameData[objectActualTopLeftDepthPointer], objectActualTopLeftDepthPointer);

                                NewObject.DepthArrayPointer = FindTopOfObject(NewObject.DepthArrayPointer, NewObject.Width, ref NewObject.Height, MissingDataTolerance, ObjectDetectionDepthThreshold, minDepthTolerance, frameData);

                                UpdateTrackedObjectCollection(NewObject);


                                ////Top View - Draw points representing the object buffer
                                ////Yellow box represents the Buffer boundaries around the object
                                ////Green line represents the front of the Object being detected                                
                                decimal objectTopLeftDepthValue = frameData[objectActualTopLeftDepthPointer];
                                int topViewBytlesPerPixelOverDepthView = 2; // was 3 for PixelFormats.Bgr32 in cnt + 3

                                if (objectTopLeftDepthValue <= ObjectDetectionDepthThreshold && objectTopLeftDepthValue >= minDepthTolerance)
                                {
                                    //Draw the line representing the object...                                
                                    decimal lineValuePercentOfOverallDepth = Math.Round((objectTopLeftDepthValue / 4000m), 2);
                                    var depthRowLocation = (int)(lineValuePercentOfOverallDepth * 424);
                                    var objectTopLeftIndex = (depthRowLocation * _bytesPerRowTopView) + (NewObject.Col * _topViewBitsPerPixelConversionValue);
                                    var objectBufferTopLeftIndex = (depthRowLocation * _bytesPerRowTopView) + ((ObjectParimeterBuffer * _topViewBitsPerPixelConversionValue) + NewObject.Col * _topViewBitsPerPixelConversionValue);

                                    ////object top line
                                    for (int cnt = 0; cnt < (NewObject.Width * _topViewBitsPerPixelConversionValue); cnt += _topViewBitsPerPixelConversionValue) //width of object
                                    {
                                        var bufferOffset = (ObjectParimeterBuffer * _topViewBitsPerPixelConversionValue) / 2;

                                        if (objectTopLeftIndex > 0 && objectTopLeftIndex + bufferOffset + cnt + topViewBytlesPerPixelOverDepthView < _maxTopViewDepthIndex)
                                        {
                                            //green
                                            _topViewDepthPixels[objectTopLeftIndex + bufferOffset + cnt] = 0;
                                            _topViewDepthPixels[objectTopLeftIndex + bufferOffset + cnt + 1] = 255;
                                            _topViewDepthPixels[objectTopLeftIndex + bufferOffset + cnt + 2] = 0;
                                        }
                                    }

                                    ////object buffer box
                                    for (int cnt = 0; cnt < (NewObject.Width * _topViewBitsPerPixelConversionValue) + (ObjectParimeterBuffer * _topViewBitsPerPixelConversionValue); cnt += _topViewBitsPerPixelConversionValue) //width of object + Object buffer
                                    {
                                        //object buffer boundaries                                                                                                              
                                        var objectTopLeftFrontBufferIndex = objectTopLeftIndex - (InteractionFrontBuffer * _bytesPerRowTopView) / 10;

                                        if (objectTopLeftFrontBufferIndex > 0 && objectTopLeftFrontBufferIndex + cnt + topViewBytlesPerPixelOverDepthView < _maxTopViewDepthIndex)
                                        {
                                            //Yellow
                                            _topViewDepthPixels[objectTopLeftFrontBufferIndex + cnt] = 0;
                                            _topViewDepthPixels[objectTopLeftFrontBufferIndex + cnt + 1] = 255;
                                            _topViewDepthPixels[objectTopLeftFrontBufferIndex + cnt + 2] = 255;
                                        }

                                        var objectTopLeftBackBufferIndex = objectTopLeftIndex + (InteractionBackBuffer * _bytesPerRowTopView) / 10;

                                        if (objectTopLeftBackBufferIndex > 0 && objectTopLeftBackBufferIndex + cnt + topViewBytlesPerPixelOverDepthView < _maxTopViewDepthIndex)
                                        {
                                            //Yellow
                                            _topViewDepthPixels[objectTopLeftBackBufferIndex + cnt] = 0;
                                            _topViewDepthPixels[objectTopLeftBackBufferIndex + cnt + 1] = 255;
                                            _topViewDepthPixels[objectTopLeftBackBufferIndex + cnt + 2] = 255;
                                        }

                                        //////side lines of object buffer
                                        if (cnt == 0 || cnt == ((NewObject.Width * _topViewBitsPerPixelConversionValue) + (ObjectParimeterBuffer * _topViewBitsPerPixelConversionValue) - _topViewBitsPerPixelConversionValue))
                                        {
                                            var sidePixelIndex = objectTopLeftFrontBufferIndex + _bytesPerRowTopView;
                                            do
                                            {
                                                if (sidePixelIndex > 0 && sidePixelIndex + cnt + topViewBytlesPerPixelOverDepthView < _maxTopViewDepthIndex)
                                                {
                                                    //Yellow
                                                    _topViewDepthPixels[sidePixelIndex + cnt] = 0;
                                                    _topViewDepthPixels[sidePixelIndex + cnt + 1] = 255;
                                                    _topViewDepthPixels[sidePixelIndex + cnt + 2] = 255;
                                                }

                                                sidePixelIndex = sidePixelIndex + _bytesPerRowTopView;
                                            } while (sidePixelIndex <= objectTopLeftBackBufferIndex);
                                        }
                                    }
                                }                                
                                else
                                {
                                    var lineValueDepthOutsideThreshold = depth;
                                    decimal lineValueDepthPercentOfOverallDepthOutsideThreshold = Math.Round((lineValueDepthOutsideThreshold / 4000m), 2);
                                    var depthValueRowLocationOutsideThreshold = (int)(lineValueDepthPercentOfOverallDepthOutsideThreshold * 424);
                                    var depthValueColumnOutsideThreshold = i % 512;
                                    var depthValueTopLeftIndexOutsideThreshold = (depthValueRowLocationOutsideThreshold * _bytesPerRowTopView) + (depthValueColumnOutsideThreshold * _topViewBitsPerPixelConversionValue);

                                    if (depthValueTopLeftIndexOutsideThreshold > 0 && depthValueTopLeftIndexOutsideThreshold + _topViewBitsPerPixelConversionValue < _maxTopViewDepthIndex)
                                    {
                                        _topViewDepthPixels[depthValueTopLeftIndexOutsideThreshold] = 255;
                                        _topViewDepthPixels[depthValueTopLeftIndexOutsideThreshold + 1] = 255;
                                        _topViewDepthPixels[depthValueTopLeftIndexOutsideThreshold + 2] = 255;
                                    }
                                }

                                //Jump ahead to the end of the detected object
                                column += temp;
                                i += temp;
                            }

                            _depthPixels[i] = (byte)((4000 / MapDepthToByte)); //turn it white


                            //TopView
                            var lineValueDepth = depth;
                            decimal lineValueDepthPercentOfOverallDepth = Math.Round((lineValueDepth / 4000m), 2);
                            var depthValueRowLocation = (int)(lineValueDepthPercentOfOverallDepth * 424);
                            var depthValueColumn = i % 512;
                            var depthValueTopLeftIndex = (depthValueRowLocation * _bytesPerRowTopView) + (depthValueColumn * _topViewBitsPerPixelConversionValue);

                            if (depthValueTopLeftIndex > 0 && depthValueTopLeftIndex + _topViewBitsPerPixelConversionValue < _maxTopViewDepthIndex)
                            {
                                _topViewDepthPixels[depthValueTopLeftIndex] = 255;
                                _topViewDepthPixels[depthValueTopLeftIndex + 1] = 255;
                                _topViewDepthPixels[depthValueTopLeftIndex + 2] = 255;
                            }
                        }
                        else
                        {
                            _depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0); //convert depth to gray scale value

                            //TopView
                            var lineValueDepth = depth;
                            decimal lineValueDepthPercentOfOverallDepth = Math.Round((lineValueDepth / 4000m), 2);
                            var depthValueRowLocation = (int)(lineValueDepthPercentOfOverallDepth * 424);
                            var depthValueColumn = i % 512;
                            var depthValueTopLeftIndex = (depthValueRowLocation * _bytesPerRowTopView) + (depthValueColumn * _topViewBitsPerPixelConversionValue);
                            if (depthValueTopLeftIndex > 0 && depthValueTopLeftIndex + _topViewBitsPerPixelConversionValue < _maxTopViewDepthIndex)
                            {
                                _topViewDepthPixels[depthValueTopLeftIndex] = 128;
                                _topViewDepthPixels[depthValueTopLeftIndex + 1] = 128;
                                _topViewDepthPixels[depthValueTopLeftIndex + 2] = 128;
                            }

                        }                        
                    }
                    else if (i == startColDisplayIndex) //left line
                    {
                        _depthPixels[i] = (byte)((4000 / MapDepthToByte));
                        //Move pointer to next row
                        startColDisplayIndex += 512;
                    }
                    else if (i == endColDisplayIndex) //right line
                    {
                        _depthPixels[i] = (byte)((4000 / MapDepthToByte));
                        //Move pointer to next row
                        endColDisplayIndex += 512;
                    }
                    else
                    {                        
                        // To convert to a byte, we're mapping the depth value to the byte range.
                        // Values outside the reliable depth range are mapped to 0 (black).

                        //int defaultColor = 0;
                        //if(depth >= minDepth && depth <= maxDepth)
                        //    defaultColor = (depth / MapDepthToByte);


                        //_depthPixels[i] = (byte)defaultColor; //(byte)(depth >= minDepth && depth <= maxDepth ? defaultColor : 0); //middle line                    
                        //if (defaultColor > maxDefaultColor)
                        //    maxDefaultColor = defaultColor;


                        // To convert to a byte, we're mapping the depth value to the byte range.
                        // Values outside the reliable depth range are mapped to 0 (black).                    
                        _depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0); //middle line
                    }
                }
            }

            foreach (TrackedItem trackedObj in objects)
            {

                var st = DrawAndMonitorBoundingBox(
                        trackedObj,
                        (byte)(4000 / MapDepthToByte),
                        ObjectDetectionDepthThreshold + InteractionFrontBuffer,
                        ObjectDetectionDepthThreshold - InteractionBackBuffer,
                        frameData,
                        _depthPixels);

                if (st != ManipulationStates.NoTrack)
                {
                    Debug.Print(st.ToString());

                    OnItemInteraction(trackedObj, st);

                    if (!IsCalibrating)
                        _interactions.Insert(0, trackedObj.ObjectID + " " + st.ToString());
                }
                else
                {
                    OnItemInteraction(trackedObj, st);
                }
            }

            RenderDepthPixels();
        }

        private void OnItemInteraction(TrackedItem item, ManipulationStates st)
        {
            var handler = this.ItemInteraction;
            if (handler != null)
            {
                var kioskEvent = new KioskStateEventArgs();
                kioskEvent.ItemSelected = item.ObjectID;
                kioskEvent.ItemState = st;
                kioskEvent.TrackingID = this.CorrelationPlayerId;
                handler(this, kioskEvent);
            }
        }
        public ManipulationStates DrawAndMonitorBoundingBox(TrackedItem trackedObject, ushort color, int MaxValue, int MinValue, ushort[] frameData, byte[] depthPixels)
        {
            //check to see if we're at the beginning of a row before backing out 5px
            int BoundBoxStart = trackedObject.Col <= this.ObjectParimeterBuffer ? trackedObject.DepthArrayPointer : trackedObject.DepthArrayPointer - this.ObjectParimeterBuffer;
            int BoundBoxWidth = (trackedObject.Col + trackedObject.Width + (this.ObjectParimeterBuffer * 2)) >= 512 ? trackedObject.Width : trackedObject.Width + (this.ObjectParimeterBuffer * 2);
            int closePixelCount = 0;

            if (trackedObject.TrackedPixels == null)
                trackedObject.TrackedPixels = new List<TrackedPixel>();
                        
            var objectBoundaryCenterIndex = BoundBoxStart + ((trackedObject.Height / 2) * 512)  + (BoundBoxWidth / 2);
            var objectTopBoundaryCenterDepth = frameData[objectBoundaryCenterIndex];
            var objectCenterIndex = BoundBoxStart + (BoundBoxWidth / 2);
            var objectCenterDepth = frameData[objectCenterIndex];

            //bool writeOutput = false;            
            //if(writeOutput)
            //    System.Diagnostics.Debug.Print("********** objectTopBoundaryCenterDepth:{0} **********", objectTopBoundaryCenterDepth);

            if (!IsCalibrating && trackedObject.TrackedPixels.Count > 0)
            {                
                foreach (TrackedPixel tp in trackedObject.TrackedPixels)
                {
                    var test = frameData[tp.PixelIndexInDepthFrame];

                    //if (writeOutput)
                    //    System.Diagnostics.Debug.Print("**TrackedPixel OrginalDepthValue:{0}, PixelIndexInDepthFrame:{1} Depth:{2} - ", tp.OrginalDepthValue, tp.PixelIndexInDepthFrame, test);

                    if (IsZeroDepth)
                    {
                        //var zeroDepthMaxValue = MaxValue * 4;

                        if (frameData[tp.PixelIndexInDepthFrame] > 0 && frameData[tp.PixelIndexInDepthFrame] < MaxValue * 4 && tp.OrginalDepthValue > MaxValue * 4)
                        {
                            depthPixels[tp.PixelIndexInDepthFrame] = System.Convert.ToByte(4000 / MapDepthToByte);
                            closePixelCount++;
                        }
                        else if (tp.OrginalDepthValue > MaxValue * 4 && frameData[tp.PixelIndexInDepthFrame] == 0)
                        {
                            depthPixels[tp.PixelIndexInDepthFrame] = System.Convert.ToByte(4000 / MapDepthToByte);
                            closePixelCount++;
                        }
                        else if ((tp.OrginalDepthValue == 0 && frameData[tp.PixelIndexInDepthFrame] > 0) && frameData[tp.PixelIndexInDepthFrame] < MaxValue)
                        {
                            //handle when there is a zero value depth reading that is causing Touched/Released to not be recognized                        
                            if ((frameData[tp.PixelIndexInDepthFrame] > objectTopBoundaryCenterDepth - this.ObjectParimeterBuffer - 50 && frameData[tp.PixelIndexInDepthFrame] < objectTopBoundaryCenterDepth + this.ObjectParimeterBuffer + 50))
                            {
                                depthPixels[tp.PixelIndexInDepthFrame] = System.Convert.ToByte(4000 / MapDepthToByte);
                                closePixelCount++;
                            }
                        }
                        else
                        {
                            depthPixels[tp.PixelIndexInDepthFrame] = System.Convert.ToByte(color / MapDepthToByte);
                        }
                    }
                    else
                    {

                        if (tp.OrginalDepthValue > MaxValue &&
                            frameData[tp.PixelIndexInDepthFrame] < MaxValue && frameData[tp.PixelIndexInDepthFrame] >= MinValue)
                        {
                            depthPixels[tp.PixelIndexInDepthFrame] = System.Convert.ToByte(4000 / MapDepthToByte);
                            closePixelCount++;
                        }
                        else if (tp.OrginalDepthValue < MaxValue && frameData[tp.PixelIndexInDepthFrame] + 10 < tp.OrginalDepthValue && frameData[tp.PixelIndexInDepthFrame] > MinValue)
                        {
                            depthPixels[tp.PixelIndexInDepthFrame] = System.Convert.ToByte(4000 / MapDepthToByte);
                            closePixelCount++;
                        }
                        else if (tp.OrginalDepthValue == 0 && frameData[tp.PixelIndexInDepthFrame] > 0
                        && (frameData[tp.PixelIndexInDepthFrame] < MaxValue && frameData[tp.PixelIndexInDepthFrame] >= MinValue))
                        {
                            //handle when there is a zero value depth reading that is causing Touched/Released to not be recognized                        
                            if ((frameData[tp.PixelIndexInDepthFrame] > objectTopBoundaryCenterDepth - this.ObjectParimeterBuffer - 50 && frameData[tp.PixelIndexInDepthFrame] < objectTopBoundaryCenterDepth + this.ObjectParimeterBuffer + 50))
                            {
                                depthPixels[tp.PixelIndexInDepthFrame] = System.Convert.ToByte(4000 / MapDepthToByte);
                                closePixelCount++;
                            }
                        }
                        else
                        {
                            depthPixels[tp.PixelIndexInDepthFrame] = System.Convert.ToByte(color / MapDepthToByte);
                        }
                    }
                }

            }
            else
            {
                if (IsZeroDepth)
                {
                    //Check to see if the top is being interacted with
                    for (int i = BoundBoxStart; i < BoundBoxStart + BoundBoxWidth; i++)
                    {
                        depthPixels[i] = System.Convert.ToByte(color / MapDepthToByte);
                        trackedObject.TrackedPixels.Add(new TrackedPixel() { PixelIndexInDepthFrame = i, OrginalDepthValue = frameData[i] });

                        if (frameData[i] > 0 && frameData[i] <= MaxValue * 4)
                        {
                            depthPixels[i] = System.Convert.ToByte(4000 / MapDepthToByte);
                            closePixelCount++; //reached in close to pixel in object
                        }
                    }
                }
                else
                {
                    //Check to see if the top is being interacted with
                    for (int i = BoundBoxStart; i < BoundBoxStart + BoundBoxWidth; i++)
                    {
                        depthPixels[i] = System.Convert.ToByte(color / MapDepthToByte);
                        trackedObject.TrackedPixels.Add(new TrackedPixel() { PixelIndexInDepthFrame = i, OrginalDepthValue = frameData[i] });

                        if (frameData[i] <= MaxValue && frameData[i] >= MinValue)
                        {
                            depthPixels[i] = System.Convert.ToByte(4000 / MapDepthToByte);
                            closePixelCount++; //reached in close to pixel in object
                        }
                    }
                }

                var adjustFactorForCameraAngle = 0;// (MaxValue - MinValue) / (trackedObject.Height / 4);
                //Check to see if the sides of the object have been interacted with
                for (int i = 1; i < trackedObject.Height / 2; i++)
                {
                    int tmp = frameData[BoundBoxStart + (i * 512)];

                    if (trackedObject.Col > 5) //Don't draw or monitor the left edge if we're past the edge of the feed
                    {
                        trackedObject.TrackedPixels.Add(new TrackedPixel() { PixelIndexInDepthFrame = BoundBoxStart + (i * 512), OrginalDepthValue = frameData[BoundBoxStart + (i * 512)] });

                        depthPixels[BoundBoxStart + (i * 512)] = System.Convert.ToByte(color / MapDepthToByte);

                        if (IsZeroDepth)
                        {
                            if (frameData[BoundBoxStart + (i * 512)] > 0 && frameData[BoundBoxStart + (i * 512)] <= MaxValue - (adjustFactorForCameraAngle * i) * 4)
                            {
                                depthPixels[BoundBoxStart + (i * 512)] = System.Convert.ToByte(4000 / MapDepthToByte);
                                closePixelCount++;
                            }
                        }
                        else
                        {
                            if (frameData[BoundBoxStart + (i * 512)] <= MaxValue - (adjustFactorForCameraAngle * i) && frameData[BoundBoxStart + (i * 512)] >= MinValue)
                            {
                                depthPixels[BoundBoxStart + (i * 512)] = System.Convert.ToByte(4000 / MapDepthToByte);
                                closePixelCount++;
                            }
                        }
                    }

                    if ((trackedObject.Col + trackedObject.Width) < 500) //Don't draw or monitors the objects right edge if we're past the feeds edge
                    {
                        depthPixels[BoundBoxStart + (i * 512) + BoundBoxWidth] = System.Convert.ToByte(color / MapDepthToByte);
                        trackedObject.TrackedPixels.Add(new TrackedPixel() { PixelIndexInDepthFrame = BoundBoxStart + (i * 512) + BoundBoxWidth, OrginalDepthValue = frameData[BoundBoxStart + (i * 512) + BoundBoxWidth] });

                        if (IsZeroDepth)
                        {
                            if (frameData[BoundBoxStart + (i * 512) + BoundBoxWidth] > 0 && frameData[BoundBoxStart + (i * 512) + BoundBoxWidth] <= MaxValue - (adjustFactorForCameraAngle * i) * 4)
                            {
                                depthPixels[BoundBoxStart + (i * 512) + BoundBoxWidth] = System.Convert.ToByte(4000 / MapDepthToByte);
                                closePixelCount++;
                            }
                        }
                        else
                        {
                            if (frameData[BoundBoxStart + (i * 512) + BoundBoxWidth] <= MaxValue - (adjustFactorForCameraAngle * i) && frameData[BoundBoxStart + (i * 512) + BoundBoxWidth] >= MinValue)
                            {
                                depthPixels[BoundBoxStart + (i * 512) + BoundBoxWidth] = System.Convert.ToByte(4000 / MapDepthToByte);
                                closePixelCount++;
                            }
                        }
                    }
                }

            }
            if (IsCalibrating)
            {
                //Sometimes we can have pixels that get into our activte detection range because of sensor noise or object angle.
                //here we try to learn about that noise so when we actuatly try to detect a touch we have a much more accurate theshold
                if (trackedObject.DS.NosiePixelCount == 0)
                    trackedObject.DS.NosiePixelCount = 0;//closePixelCount;
                else
                {
                    trackedObject.DS.NosiePixelCount = 0;// closePixelCount + trackedObject.DS.NosiePixelCount / 2;
                }
                return ManipulationStates.NoTrack;
            }

            int w = (trackedObject.DepthArrayPointer + (512 * 10));
            int currentIndex = (trackedObject.DepthArrayPointer + (512 * 10));
            int numberOfPixelsPerObject = (trackedObject.Height / 4) * trackedObject.Width;
            int existReadCnt = 0;
            int notExistsReadCnt = 0;

            
            //if (false)
            //{
            //    System.Diagnostics.Debug.Print("w{0} = (trackedObject.DepthArrayPointer{1} + (512 * 10))", w, trackedObject.DepthArrayPointer);
            //    System.Diagnostics.Debug.Print("********** depth maxValue:{0} **********", MaxValue);                
            //    System.Diagnostics.Debug.Print("********** objectCenterDepth:{0} **********", objectCenterDepth);
            //}

            //Scan the object space looking for any signs the object is still there...
            for (int Y = 0; Y < trackedObject.Height / 4; Y++)
            {
                for (int X = 0; X < trackedObject.Width; X++)
                {
                    ushort depth = frameData[currentIndex + X];

                    //if(writeOutput)
                    //    System.Diagnostics.Debug.Print("********** Object Space Depth frameData[{0}]:{1} **********",currentIndex + X, depth);

                    if (IsZeroDepth)
                    {
                        if (depth == 0)
                        {
                            depthPixels[currentIndex + X] = System.Convert.ToByte(4000 / MapDepthToByte);
                            existReadCnt++;
                        }
                        else
                            notExistsReadCnt++;
                    }
                    else
                    {

                        //if (writeOutput)
                        //    System.Diagnostics.Debug.Print("********** Object Space Depth frameData[{0}]:{1}, MaxValue:{2} **********", currentIndex + X, depth, MaxValue);

                        if (depth > 0 && depth <= MaxValue)
                        {
                            depthPixels[currentIndex + X] = System.Convert.ToByte(4000 / MapDepthToByte);
                            existReadCnt++;
                        }
                        //Removed - Replaced to handle when object is on the edge which causes many zero value readings
                        else if ((depth == 0 && objectCenterDepth == 0) && depth <= MaxValue)
                        {
                            depthPixels[currentIndex + X] = System.Convert.ToByte(4000 / MapDepthToByte);
                            existReadCnt++;
                        }
                        else
                        {
                            notExistsReadCnt++;
                        }
                    }

                    //System.Diagnostics.Debug.Print("Object Depth: frameData[currentIndex{0} + X{1}]: {2}, existsReadCnt{3}, notExistsReadCnt{4}", currentIndex, X, depth, existReadCnt, notExistsReadCnt);
                }
                currentIndex = w + (512 * Y);                
            }

            float res = (float)existReadCnt / (float)numberOfPixelsPerObject;
            var DeviceIsPresent = true;
            if (res > .10)
            {

            }
            else
            {
                DeviceIsPresent = false;
            }

            //System.Diagnostics.Debug.Print("DeviceIsPresent: {0}, res: {1}, numberOfPixelsPerObject {2}", DeviceIsPresent, res, numberOfPixelsPerObject);

            //Try to confirm action by evaluating how many times we've seen this state
            if (IsZeroDepth)
            {
                if (closePixelCount > trackedObject.DS.NosiePixelCount + 5)
                {
                    trackedObject.NoTouchSampleCount = 0;
                    trackedObject.TouchSampleCount++;
                }
                else
                {
                    trackedObject.NoTouchSampleCount++;
                    trackedObject.TouchSampleCount = 0;
                }
            }
            else
            {
                if (closePixelCount > trackedObject.DS.NosiePixelCount + 5)
                {
                    trackedObject.NoTouchSampleCount = 0;
                    trackedObject.TouchSampleCount++;
                }
                else
                {
                    trackedObject.NoTouchSampleCount++;
                    trackedObject.TouchSampleCount = 0;
                }
            }

            if (closePixelCount > trackedObject.DS.NosiePixelCount + MissingDataTolerance && !IsCalibrating && trackedObject.TouchSampleCount > 10)
            {
                //reset the state
                trackedObject.TouchSampleCount = 0;

                //Find out if they are putting the camera back in place
                if (DeviceIsPresent && !trackedObject.DS.DeviceRemoved && !trackedObject.DS.DeviceReplaced && !trackedObject.DS.Touched)
                {
                    trackedObject.DS.Touched = true;
                    Debug.WriteLine(trackedObject.ObjectID + " " + ManipulationStates.Touched);
                    return ManipulationStates.Touched;
                }
                else if (DeviceIsPresent && trackedObject.DS.DeviceRemoved)
                {
                    trackedObject.DS.DeviceRemoved = false;
                    trackedObject.DS.DeviceReplaced = true;
                    Debug.WriteLine(trackedObject.ObjectID + " " + ManipulationStates.Replaced);
                    return ManipulationStates.Replaced;
                }
            }
            else if (trackedObject.NoTouchSampleCount > 10)
            {
                trackedObject.NoTouchSampleCount = 0;
                //Find out if they are putting the camera back in place
                if (DeviceIsPresent && trackedObject.DS.Touched)
                {
                    trackedObject.DS.Touched = false;
                    Debug.WriteLine(trackedObject.ObjectID + " " + ManipulationStates.Released);
                    return ManipulationStates.Released;
                }
                else if (!DeviceIsPresent && !trackedObject.DS.DeviceRemoved)
                {
                    trackedObject.DS.DeviceRemoved = true;
                    Debug.WriteLine(trackedObject.ObjectID + " " + ManipulationStates.Removed);
                    return ManipulationStates.Removed;
                }
                else if (DeviceIsPresent && trackedObject.DS.DeviceReplaced)
                {
                    trackedObject.DS.Touched = false;
                    trackedObject.DS.DeviceReplaced = false;
                    return ManipulationStates.Released;
                }
            }

            return ManipulationStates.NoTrack;
        }


        public bool IsTargetInPlace(TrackedItem trackedObject, int Tolerance, int MaxValue, int MinValue, ushort[] frameData, byte[] depthPixels)
        {
            int currentIndex = trackedObject.DepthArrayPointer;
            int existReadCnt = 0;
            int notExistsReadCnt = 0;

            //Scan the object space looking for any signs the object is still there...
            for (int Y = 0; Y < trackedObject.Height / 2; Y++)
            {
                for (int X = 0; X < trackedObject.Width; X++)
                {
                    ushort depth = frameData[currentIndex + X];
                    Debug.Print(X.ToString() + " " + depth.ToString());
                    if (depth <= MaxValue)
                    {
                        existReadCnt++;
                    }
                    else
                    {
                        notExistsReadCnt++;
                    }

                }
                currentIndex = trackedObject.DepthArrayPointer + (512 * Y);

            }

            if (existReadCnt > 10)
                return true;
            else
                return false;

        }
        public int GetTargetWidth(int Start, int Reach, int MissingDataTolerance, ushort MaxValue, ushort MinValue, ushort[] frameData)
        {
            int StreamLength = 0;
            int missingDataCnt = 0;

            //bool writeOutput = false;

            if (IsZeroDepth)
            {

                //if (writeOutput)
                //    System.Diagnostics.Debug.Print("GetTargetWidth Start:{0}, Reach:{1}", Start, Reach);

                for (int i = Start; i < Reach; i++)
                {
                    //if (writeOutput)
                    //{
                    //    var depth = frameData[i];
                    //    System.Diagnostics.Debug.Print("GetTargetWidth frameData[{0}]:{1}", i, frameData[i]);
                    //}

                    StreamLength++;
                    if (frameData[i] == 0)
                    {
                        missingDataCnt = 0;
                    }
                    else
                    {
                        missingDataCnt++;

                        if (missingDataCnt > MissingDataTolerance)
                            break;
                    }
                }
            }
            else
            {
                //if (writeOutput)
                //    System.Diagnostics.Debug.Print("GetTargetWidth Start:{0}, Reach:{1}", Start, Reach);

                for (int i = Start; i < Reach; i++)
                {

                    //if (writeOutput)
                    //{
                    //    var depth = frameData[i];
                    //    System.Diagnostics.Debug.Print("GetTargetWidth frameData[{0}]:{1}", i, frameData[i]);
                    //}

                    StreamLength++;
                    if (frameData[i] <= MaxValue && frameData[i] >= MinValue)
                    {
                        missingDataCnt = 0;
                    }
                    else
                    {
                        missingDataCnt++;

                        if (missingDataCnt > MissingDataTolerance)
                            break;
                    }
                }
            }

            return StreamLength - missingDataCnt;

        }        
        public int FindTopOfObject(int TargetArrayIndex, int TargetWidth, ref int TargetHeight, int MissingDataTolerance, ushort MaxValue, ushort MinValue, ushort[] frameData)
        {

            int midPoint = TargetWidth / 2;
            int topOfObject = 0;
            int bottomOfObject = 0;
            int missingDataCnt = 0;
            ushort lastSample = frameData[TargetArrayIndex + midPoint];
            bool topOfObjectBasedOnDepthVarianceFound = false;
            int topOfObjectBasedOnDepthVariance = 0;

            //Look for the top
            for (int i = TargetArrayIndex + midPoint; i > 0; i -= _depthFrameDescription.Width)
            {
                topOfObject++;
                if (frameData[i] <= MaxValue && frameData[i] >= MinValue || (Math.Abs(lastSample - frameData[i])) < 100)
                {
                    //this is used during Angled View to help Kinect find the top of the object
                    if (!topOfObjectBasedOnDepthVarianceFound && frameData[i] - lastSample >= 20)
                    {
                        topOfObjectBasedOnDepthVarianceFound = true;
                        topOfObjectBasedOnDepthVariance = topOfObject;
                    }

                    missingDataCnt = 0;
                    lastSample = frameData[i];
                }
                else
                {
                    missingDataCnt++;

                    if (missingDataCnt > MissingDataTolerance)
                    {
                        if (topOfObjectBasedOnDepthVarianceFound)
                            topOfObject = topOfObjectBasedOnDepthVariance + 6;

                        break;
                    }
                }

                //Are we at the top of the image
                if (SamplingRow - topOfObject == 0)
                {
                    if (topOfObjectBasedOnDepthVarianceFound)
                        topOfObject = topOfObjectBasedOnDepthVariance + 6;

                    break;
                }
            }

            missingDataCnt = 0;
            lastSample = frameData[TargetArrayIndex + midPoint];

            //look for the bottom
            for (int i = TargetArrayIndex + midPoint; i > 0; i += _depthFrameDescription.Width)
            {
                bottomOfObject++;
                if (frameData[i] <= MaxValue && frameData[i] >= MinValue)
                {
                    missingDataCnt = 0;
                    lastSample = frameData[i];
                }
                else
                {
                    missingDataCnt++;

                    if (missingDataCnt > MissingDataTolerance)
                        break;
                }

                if (SamplingRow + bottomOfObject == _depthFrameDescription.Height)
                    break;
            }

            TargetHeight = topOfObject + bottomOfObject;

            return TargetArrayIndex -= (topOfObject * 512);
        }

        private void UpdateTrackedObjectCollection(TrackedItem potentialTarget)
        {
            var existingTarget = (from targets in objects
                                  where ((targets.Col - 5) <= potentialTarget.Col && potentialTarget.Col < (targets.Col + targets.Width)) //DEALING WITH OVER LAP issues below
                                      && (potentialTarget.Width < targets.Width + 5 && potentialTarget.Width > targets.Width - 5) || ((potentialTarget.Col + potentialTarget.Width >= targets.Col) && (potentialTarget.Col + potentialTarget.Width) <= (targets.Col + targets.Width))
                                  select targets).FirstOrDefault();


            if (existingTarget == null)
            {
                potentialTarget.Row = potentialTarget.DepthArrayPointer / 512;
                potentialTarget.DS = new ItemState();

                potentialTarget.ObjectID = "Item" + (objects.Count + 1).ToString();
                objects.Add(potentialTarget);

                // notify any bound elements that the text has changed
                OnPropertyChanged("ObjectCount");
            }
            else
            {
                existingTarget.Col = potentialTarget.Col;
                existingTarget.Width = potentialTarget.Width;
                existingTarget.DepthArrayPointer = potentialTarget.DepthArrayPointer;
                existingTarget.Row = potentialTarget.DepthArrayPointer / 512;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (propertyName.Equals("IsCalibrating"))
            {
                if (this.IsCalibrating == false && !string.IsNullOrEmpty(ServiceState) && ServiceState.Equals(ServiceStates.NotReady.ToString()))
                    ServiceState = ServiceStates.Open.ToString();
                if (this.IsCalibrating == true && !string.IsNullOrEmpty(ServiceState) && ServiceState.Equals(ServiceStates.Open.ToString()))
                    ServiceState = ServiceStates.NotReady.ToString();
            }

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnServiceStateChanged(ServiceStates state)
        {
            if (this.ServiceStateChanged != null)
            {
                Debug.WriteLine("ObjectDetectionStateChange: " + this.ServiceState);
                this.ServiceStateChanged(this, new ServiceStateEventArgs() { State = state });
            }
        }

    }
}

