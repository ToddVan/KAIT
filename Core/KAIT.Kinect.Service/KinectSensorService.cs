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

using KAIT.Biometric.Services;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using Extensions;
using System.Windows.Media;
using KAIT.Common.Interfaces;
using System.ComponentModel;

namespace KAIT.Common.Sensor
{
    public class KinectSensorService : ISensorService<KinectSensor>, INotifyPropertyChanged
    {

        public event EventHandler<SensorStatusEventArgs> StatusChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public KinectSensor Sensor { get; private set; }

        /// <summary>
        /// Reader for body frames
        /// </summary>        
        private MultiSourceFrameReader multiSourceFrameReader = null;

        /// <summary>
        /// Face rotation display angle increment in degrees
        /// </summary>
        private const double FaceRotationIncrementInDegrees = 5.0;

        /// <summary>
        /// Array to store bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Number of bodies tracked
        /// </summary>
        private int bodyCount;

        /// <summary>
        /// Face frame sourcesp
        /// </summary>
        private FaceFrameSource[] faceFrameSources = null;

        /// <summary>
        /// Face frame readers
        /// </summary>
        private FaceFrameReader[] faceFrameReaders = null;

        /// <summary>
        /// Storage for face frame results
        /// </summary>
        private FaceFrameResult[] faceFrameResults = null;

        /// <summary>
        /// Width of display (color space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (color space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// Display rectangle
        /// </summary>
        private Rect displayRect;

        private Double demographicsSamplingRange;



        WriteableBitmap _colorBitmap;
        public WriteableBitmap colorBitmap
        {
            get { return _colorBitmap; }
            set
            {
                if (_colorBitmap == value)
                    return;
                _colorBitmap = value;
                RaisePropertyChanged("colorBitmap");
            }
        }

       

        BlockingCollection<ProcessFaceData> _BiometricProcessingQueue;

        BlockingCollection<Body> _SkeletonTrackingProcessingQueue;

        SkeletalTelemetryService _skeletalTelemetry;

        BiometricTelemetryService _biometricTelemetry;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;
        public KinectSensorService(IDemographicsService biometricTelemetryService)
        {

            this.Sensor = KinectSensor.GetDefault();

            _biometricTelemetry = (BiometricTelemetryService) biometricTelemetryService;
            _biometricTelemetry.IsPrimaryRoleBiometricID = false;
            _biometricTelemetry.EventHubConnectionString = ConfigurationManager.AppSettings["Azure.Hub.Biometric"];

            _biometricTelemetry.DebugImages = false;

            _skeletalTelemetry = new SkeletalTelemetryService(ConfigurationManager.AppSettings["Azure.Hub.SkeletalHub"]);

            _BiometricProcessingQueue = new BlockingCollection<ProcessFaceData>();
            {
                IObservable<ProcessFaceData> ob = _BiometricProcessingQueue.
                  GetConsumingEnumerable().
                  ToObservable(TaskPoolScheduler.Default);

                ob.Subscribe(p =>
                {
                    // This handler will get called whenever 
                    // anything appears on myQueue in the future.
                    _biometricTelemetry.ProcessFace(p);
                });
            }

            // get FrameDescription from InfraredFrameSource
            _SkeletonTrackingProcessingQueue = new BlockingCollection<Body>();
            {
                IObservable<Body> ob = _SkeletonTrackingProcessingQueue.
                  GetConsumingEnumerable().
                  ToObservable(TaskPoolScheduler.Default);

                ob.Subscribe(p =>
                {
                    // This handler will get called whenever 
                    // anything appears on myQueue in the future.
                    _skeletalTelemetry.TrackSkeleton(p, this.Sensor.UniqueKinectId);
                });
            }

            demographicsSamplingRange = System.Convert.ToDouble(ConfigurationManager.AppSettings["Demographics.Sampling.Range"]);

            this.multiSourceFrameReader = this.Sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color | FrameSourceTypes.Infrared);

            //set up event processing to only process telemetry frames ever 200 milliseonds to optimize app performance and avoid saturating the network
            TimeSpan ts = new TimeSpan(0,0,0,0,200);
           
            // AllFramesReadyEventArgs will all be timestamped.
            // Subscription is set to run on ThreadPool but observer
            // will be notified using UI thread dispatcher
            var frames = Observable.FromEventPattern<MultiSourceFrameArrivedEventArgs>(
                ev => this.multiSourceFrameReader.MultiSourceFrameArrived += ev,
                ev => this.multiSourceFrameReader.MultiSourceFrameArrived -= ev)
            .Sample(ts);
    
            var obsBodies = frames
                .Select(frame => {

                    if (frame.EventArgs.FrameReference != null)
                    {
                        try
                        {
                            var multiSourceFrame = frame.EventArgs.FrameReference.AcquireFrame();

                            if (multiSourceFrame != null)
                                return multiSourceFrame;
                            else
                                return null;
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    else
                        return null;
                    

                }
             )
            .ObserveOnDispatcher()
            .Subscribe(x =>
            {
                OnMultipleFramesArrivedHandler(x);
            });

    
            // set the maximum number of bodies that would be tracked by Kinect
            this.bodyCount = this.Sensor.BodyFrameSource.BodyCount;

            // allocate storage to store body objects
            this.bodies = new Body[this.bodyCount];

            // specify the required face frame results
            FaceFrameFeatures faceFrameFeatures =
                FaceFrameFeatures.BoundingBoxInColorSpace
                | FaceFrameFeatures.PointsInColorSpace
                | FaceFrameFeatures.RotationOrientation
                | FaceFrameFeatures.FaceEngagement
                | FaceFrameFeatures.Glasses
                | FaceFrameFeatures.Happy
                | FaceFrameFeatures.LeftEyeClosed
                | FaceFrameFeatures.RightEyeClosed
                | FaceFrameFeatures.LookingAway
                | FaceFrameFeatures.MouthMoved
                | FaceFrameFeatures.MouthOpen
                | FaceFrameFeatures.BoundingBoxInInfraredSpace;

            // create a face frame source + reader to track each face in the FOV
            this.faceFrameSources = new FaceFrameSource[this.bodyCount];
            this.faceFrameReaders = new FaceFrameReader[this.bodyCount];

            for (int i = 0; i < this.bodyCount; i++)
            {
                // create the face frame source with the required face frame features and an initial tracking Id of 0
                this.faceFrameSources[i] = new FaceFrameSource(this.Sensor, 0, faceFrameFeatures);

                // open the corresponding reader
                this.faceFrameReaders[i] = this.faceFrameSources[i].OpenReader();
            }

            // allocate storage to store face frame results for each face in the FOV
            this.faceFrameResults = new FaceFrameResult[this.bodyCount];

            FrameDescription colorFrameDescription = this.Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            
            // get the color frame details
            FrameDescription frameDescription = this.Sensor.ColorFrameSource.FrameDescription;

            // set the display specifics
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;
            this.displayRect = new Rect(0.0, 0.0, this.displayWidth, this.displayHeight);
            
            this.Sensor.IsAvailableChanged += Sensor_IsAvailableChanged;
        }


        private async void OnMultipleFramesArrivedHandler(MultiSourceFrame e)
        {
            BodyFrame bodyFrame = null;
            ColorFrame colorFrame = null;
            InfraredFrame infraredFrame = null;

            
            if (e == null) return;

            try
            {
                bodyFrame = e.BodyFrameReference.AcquireFrame();
                colorFrame = e.ColorFrameReference.AcquireFrame();
                
                infraredFrame = e.InfraredFrameReference.AcquireFrame();

                if ((bodyFrame == null) || (colorFrame == null) || (infraredFrame == null))
                {
                    return;
                }

               
                 //ColorFrame 
                using (colorFrame)
                {
                    //BodyFrame
                    await ProcessBodyFrame(bodyFrame, colorFrame);
                }

                // InfraredFrame
                //if (infraredFrame != null)
                //{
                //    // the fastest way to process the infrared frame data is to directly access 
                //    // the underlying buffer
                //    using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                //    {
                //        // verify data and write the new infrared frame data to the display bitmap
                //        if (((this.infraredFrameDescription.Width * this.infraredFrameDescription.Height) == (infraredBuffer.Size / this.infraredFrameDescription.BytesPerPixel)) &&
                //            (this.infraredFrameDescription.Width == this.infraredBitmap.PixelWidth) && (this.infraredFrameDescription.Height == this.infraredBitmap.PixelHeight))
                //        {
                //            this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                //        }
                //    }
                //}
            }
            finally
            {
                if (infraredFrame != null)
                {
                    infraredFrame.Dispose();
                }

                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                }

                if (bodyFrame != null)
                {
                    bodyFrame.Dispose();
                }
            }

        }
        int _frameCount = 0;

        //Flag to only render the color frame if we need it to extract faces...
        bool _isColorFrameRenderedForPass = false;
        private async Task ProcessBodyFrame(BodyFrame bodyFrame, ColorFrame colorFrame)
        {
            using (bodyFrame)
            {
                if (bodyFrame != null)
                {
                    // update body data
                    bodyFrame.GetAndRefreshBodyData(this.bodies);


                    // iterate through each face source
                    for (int i = 0; i < this.bodyCount; i++)
                    {
                        var body = this.bodies[i];
                       
                        _SkeletonTrackingProcessingQueue.Add(body);

                         // check if a valid face is tracked in this face source
                        if (this.faceFrameSources[i].IsTrackingIdValid)
                        {
                            // check if we have valid face frame results
                            if (this.faceFrameResults[i] != null)
                            {
                                try
                                {
                                    if (this.bodies[i].Joints[JointType.Neck].Position.Z <= demographicsSamplingRange && this.faceFrameResults[i].FaceProperties[FaceProperty.Engaged] == DetectionResult.Yes
                                         && _biometricTelemetry.IsNewPlayer(this.bodies[i].TrackingId)
                                        )
                                    {
                                        //Determine if need to render the color frame to extract the faces for this pass this is an optimization since we may have more than one player
                                        //present in this image so we don't want to process the same image up to 6 times per pass.
                                        if (!_isColorFrameRenderedForPass)
                                        {
                                            RenderColorFrame(colorFrame);
                                            _isColorFrameRenderedForPass = true;
                                        }
                                        //
                                        //var convertedImage = BitmapFactory.ConvertToPbgra32Format(this.infraredBitmap);
                                        //int imageWidth = (this.faceFrameResults[i].FaceBoundingBoxInInfraredSpace.Left + this.faceFrameResults[i].FaceBoundingBoxInInfraredSpace.Width() + 40) > this.infraredFrameDescription.Width ? this.infraredFrameDescription.Width : (int)this.faceFrameResults[i].FaceBoundingBoxInInfraredSpace.Width() + 40;

                                        //var faceImage = convertedImage.Crop(new Rect(
                                        //                                            this.faceFrameResults[i].FaceBoundingBoxInInfraredSpace.Left - 20,
                                        //                                            this.faceFrameResults[i].FaceBoundingBoxInInfraredSpace.Top - 20,
                                        //                                             this.faceFrameResults[i].FaceBoundingBoxInInfraredSpace.Width() + 40,
                                        //                                            this.faceFrameResults[i].FaceBoundingBoxInInfraredSpace.Height() + 40));

                                        var convertedImage = BitmapFactory.ConvertToPbgra32Format(this.colorBitmap);
                                        var faceImage = convertedImage.Crop(new Rect(
                                                                                    this.faceFrameResults[i].FaceBoundingBoxInColorSpace.Left - 60,
                                                                                    this.faceFrameResults[i].FaceBoundingBoxInColorSpace.Top - 80,
                                                                                    this.faceFrameResults[i].FaceBoundingBoxInColorSpace.Width() + 100,
                                                                                    this.faceFrameResults[i].FaceBoundingBoxInColorSpace.Height() + 140));

                                      

                                       
                                        var sample = new ProcessFaceData() { TrackingID = this.faceFrameResults[i].TrackingId, PlayersFace = CreateBitmap(faceImage.Clone()) };

                                        _BiometricProcessingQueue.Add(sample);

                                    }
                                }
                                catch (Exception ex)
                                {
                                    var s = ex;
                                }
                            }
                        }
                        else
                        {
                            // check if the corresponding body is tracked 
                            if (this.bodies[i].IsTracked)
                            {
                                // update the face frame source to track this body
                                this.faceFrameSources[i].TrackingId = this.bodies[i].TrackingId;
                            }
                        }
                    }

                    //Reset the state so we know we need to render the frame again on the next face sample.
                    _isColorFrameRenderedForPass = false;
                }
            }
        }

        public static Bitmap CreateBitmap(WriteableBitmap bitmap)
        {
            System.Drawing.Bitmap bmp;

            using (MemoryStream outStream = new MemoryStream())
            {
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)bitmap));
                enc.Save(outStream);
                outStream.Seek(0, SeekOrigin.Begin);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            return bmp;
        }

        private void RenderColorFrame(ColorFrame colorFrame)
        {
            if (colorFrame != null)
            {
                FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                {
                    this.colorBitmap.Lock();

                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                    {
                        colorFrame.CopyConvertedFrameDataToIntPtr(
                            this.colorBitmap.BackBuffer,
                            (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                            ColorImageFormat.Bgra);

                        this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                    }

                    this.colorBitmap.Unlock();
                }
            }
        }

        /// <summary>
        /// Converts rotation quaternion to Euler angles 
        /// And then maps them to a specified range of values to control the refresh rate
        /// </summary>
        /// <param name="rotQuaternion">face rotation quaternion</param>
        /// <param name="pitch">rotation about the X-axis</param>
        /// <param name="yaw">rotation about the Y-axis</param>
        /// <param name="roll">rotation about the Z-axis</param>
        private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // convert face rotation quaternion to Euler angles in degrees
            double yawD, pitchD, rollD;
            pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

            // clamp the values to a multiple of the specified increment to control the refresh rate
            double increment = FaceRotationIncrementInDegrees;
            pitch = (int)((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * (int)increment;
            yaw = (int)((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * (int)increment;
            roll = (int)((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * (int)increment;
        }

        /// <summary>
        /// Validates face bounding box and face points to be within screen space
        /// </summary>
        /// <param name="faceResult">the face frame result containing face box and points</param>
        /// <returns>success or failure</returns>
        private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
        {
            bool isFaceValid = faceResult != null;

            if (isFaceValid)
            {
                var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                if (faceBox != null)
                {
                    // check if we have a valid rectangle within the bounds of the screen space
                    isFaceValid = faceBox.Width() > 0 &&
                                  faceBox.Height() > 0 &&
                                  faceBox.Right <= this.displayWidth &&
                                  faceBox.Bottom <= this.displayHeight;

                    if (isFaceValid)
                    {
                        var facePoints = faceResult.FacePointsInColorSpace;
                        if (facePoints != null)
                        {
                            foreach (Microsoft.Kinect.PointF pointF in facePoints.Values)
                            {
                                // check if we have a valid face point within the bounds of the screen space
                                bool isFacePointValid = pointF.X > 0.0f &&
                                                        pointF.Y > 0.0f &&
                                                        pointF.X < this.displayWidth &&
                                                        pointF.Y < this.displayHeight;

                                if (!isFacePointValid)
                                {
                                    isFaceValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return isFaceValid;
        }

        /// <summary>
        /// Handles the face frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    // get the index of the face source from the face source array
                    int index = this.GetFaceSourceIndex(faceFrame.FaceFrameSource);

                    // check if this face frame has valid face frame results
                    if (this.ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                    {
                        // store this face frame result to draw later
                        this.faceFrameResults[index] = faceFrame.FaceFrameResult;
                    }
                    else
                    {
                        // indicates that the latest face frame result from this reader is invalid
                        this.faceFrameResults[index] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the index of the face frame source
        /// </summary>
        /// <param name="faceFrameSource">the face frame source</param>
        /// <returns>the index of the face source in the face source array</returns>
        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            int index = -1;

            for (int i = 0; i < this.bodyCount; i++)
            {
                if (this.faceFrameSources[i] == faceFrameSource)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }
        void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            var status = (e.IsAvailable) ? SensorStatus.Ready : SensorStatus.Closed;
            RaiseStatusChanged(status);
        }

        private void RaiseStatusChanged(SensorStatus e)
        {
            var handler = this.StatusChanged;
            if (handler != null)
            {
                handler(this, new SensorStatusEventArgs() { Status = e });
            }
        }


        public void Open()
        {
            if (this.Sensor == null)
                RaiseStatusChanged(SensorStatus.Error);
          
             this.Sensor.Open();

            for (int i = 0; i < this.bodyCount; i++)
            {
                if (this.faceFrameReaders[i] != null)
                {
                    // wire handler for face frame arrival
                    this.faceFrameReaders[i].FrameArrived += this.Reader_FaceFrameArrived;
                }
            }
        }

        public void Close()
        {
            if (this.Sensor != null)
            {
                if (this.Sensor.IsOpen)
                {
                    
                    for (int i = 0; i < this.bodyCount; i++)
                    {
                        if (this.faceFrameReaders[i] != null)
                        {
                            // FaceFrameReader is IDisposable
                            this.faceFrameReaders[i].Dispose();
                            this.faceFrameReaders[i] = null;
                        }

                        if (this.faceFrameSources[i] != null)
                        {
                            // FaceFrameSource is IDisposable
                            this.faceFrameSources[i].Dispose();
                            this.faceFrameSources[i] = null;
                        }
                    }
                    this.Sensor.IsAvailableChanged -= Sensor_IsAvailableChanged;
                    _biometricTelemetry.Shutdown();

                    this.Sensor.Close();
                }
                    
                this.Sensor = null;
            }
        }

        private void RaisePropertyChanged(string propName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propName));
        }

    }
}
