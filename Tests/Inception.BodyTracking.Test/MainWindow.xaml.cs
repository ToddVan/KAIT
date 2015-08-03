//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace KAIT.BodyTracking.Test
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using KAIT.Common.Interfaces;
    using KAIT.Kinect.Service;
    using System.Configuration;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        ////KAIT Body Tracking
        private BodyTrackingService _bodyTrackingService = new BodyTrackingService();
        private IList<JointType> _bodyJointTypes;
        
        //Configuration Settings
        public float MissingBodyLeftEdgeBoundary 
        { 
            get { return _bodyTrackingService.MissingBodyLeftEdgeBoundary; } 
            set
            {
                if (_bodyTrackingService.MissingBodyLeftEdgeBoundary != value)
                {
                    _bodyTrackingService.MissingBodyLeftEdgeBoundary = value;
                    OnPropertyChanged("MissingBodyLeftEdgeBoundary");
                }
            }
        }

        
        public float MissingBodyRightEdgeBoundary
        {
            get { return _bodyTrackingService.MissingBodyRightEdgeBoundary; }
            set
            {
                if (_bodyTrackingService.MissingBodyRightEdgeBoundary != value)
                {
                    _bodyTrackingService.MissingBodyRightEdgeBoundary = value;
                    OnPropertyChanged("MissingBodyRightEdgeBoundary");
                }
            }
        }
        
        public float MissingBodyBackDepthBoundary
        {
            get { return _bodyTrackingService.MissingBodyBackDepthBoundary; }
            set
            {
                if (_bodyTrackingService.MissingBodyBackDepthBoundary != value)
                {
                    _bodyTrackingService.MissingBodyBackDepthBoundary = value;
                    OnPropertyChanged("MissingBodyBackDepthBoundary");
                }
            }
        }
        
        public int MissingBodyExpiredTimeLimit
        {
            get { return _bodyTrackingService.MissingBodyExpiredTimeLimit; }
            set
            {
                if (_bodyTrackingService.MissingBodyExpiredTimeLimit != value)
                {
                    _bodyTrackingService.MissingBodyExpiredTimeLimit = value;
                    OnPropertyChanged("MissingBodyExpiredTimeLimit");
                }
            }
        }
        
        public float NewBodyLeftEdgeBoundary
        {
            get { return _bodyTrackingService.NewBodyLeftEdgeBoundary; }
            set
            {
                if (_bodyTrackingService.NewBodyLeftEdgeBoundary != value)
                {
                    _bodyTrackingService.NewBodyLeftEdgeBoundary = value;
                    OnPropertyChanged("NewBodyLeftEdgeBoundary;");
                }
            }
        }        
        public float NewBodyRightEdgeBoundary
        {
            get { return _bodyTrackingService.NewBodyRightEdgeBoundary; }
            set
            {
                if (_bodyTrackingService.NewBodyRightEdgeBoundary != value)
                {
                    _bodyTrackingService.NewBodyRightEdgeBoundary = value;
                    OnPropertyChanged("NewBodyRightEdgeBoundary");
                }
            }
        }

        public float NewBodyBackDepthBoundary
        {
            get { return _bodyTrackingService.NewBodyBackDepthBoundary; }
            set
            {
                if (_bodyTrackingService.NewBodyBackDepthBoundary != value)
                {
                    _bodyTrackingService.NewBodyBackDepthBoundary = value;
                    OnPropertyChanged("NewBodyBackDepthBoundary");
                }
            }
        }

        public float HijackedBodyJumpTolerance
        {
            get { return _bodyTrackingService.HijackedBodyJumpTolerance; }
            set
            {
                if (_bodyTrackingService.HijackedBodyJumpTolerance != value)
                {
                    _bodyTrackingService.HijackedBodyJumpTolerance = value;
                    OnPropertyChanged("BodyJumpTolerance");
                }
            }
        }

        public float HighVelocityPlayerJumpThreshold
        {
            get { return _bodyTrackingService.HighVelocityPlayerJumpThreshold; }
            set
            {
                if (_bodyTrackingService.HighVelocityPlayerJumpThreshold != value)
                {
                    _bodyTrackingService.HighVelocityPlayerJumpThreshold = value;
                    OnPropertyChanged("HighVelocityPlayerJumpThreshold");
                }
            }
        }

        public float HighVelocityPlayerJumpMultiplier
        {
            get { return _bodyTrackingService.HighVelocityPlayerJumpMultiplier; }
            set
            {
                if (_bodyTrackingService.HighVelocityPlayerJumpMultiplier != value)
                {
                    _bodyTrackingService.HighVelocityPlayerJumpMultiplier = value;
                    OnPropertyChanged("HighVelocityPlayerJumpFactor");
                }
            }
        }

        public float SwitchActiveBodyZPositionVariance
        {
            get { return _bodyTrackingService.SwitchActiveBodyZPositionVariance; }
            set
            {
                if (_bodyTrackingService.SwitchActiveBodyZPositionVariance != value)
                {
                    _bodyTrackingService.SwitchActiveBodyZPositionVariance = value;
                    OnPropertyChanged("SwitchActiveBodyZPositionVariance");
                }
            }
        }
        public float LeftBodySelectionTrackLimit
        {
            get { return _bodyTrackingService.LeftBodySelectionTrackLimit; }
            set
            {
                if (_bodyTrackingService.LeftBodySelectionTrackLimit != value)
                {
                    _bodyTrackingService.LeftBodySelectionTrackLimit = value;
                    OnPropertyChanged("LeftBodySelectionTrackLimit");
                }
            }
        }
        public float RightBodySelectionTrackLimit
        {
            get { return _bodyTrackingService.RightBodySelectionTrackLimit; }
            set
            {
                if (_bodyTrackingService.RightBodySelectionTrackLimit != value)
                {
                    _bodyTrackingService.RightBodySelectionTrackLimit = value;
                    OnPropertyChanged("RightBodySelectionTrackLimit");
                }
            }
        }

        public int TrackedKinectBodyCount
        {
            get { return _bodyTrackingService.TrackedKinectBodyCount; }
            set
            {
                if (_bodyTrackingService.TrackedKinectBodyCount != value)
                {
                    _bodyTrackingService.TrackedKinectBodyCount = value;
                    OnPropertyChanged("TrackedKinectBodyCount");
                }
            }
        }

        public int RequiredJointsTrackedCount
        {
            get { return _bodyTrackingService.RequiredJointsTrackedCount; }
            set
            {
                if (_bodyTrackingService.RequiredJointsTrackedCount != value)
                {
                    _bodyTrackingService.RequiredJointsTrackedCount = value;
                    OnPropertyChanged("RequiredJointsTrackedCount");
                }
            }
        }

        public int ActiveBodyTrackedJointsCount
        {
            get { return _bodyTrackingService.ActiveBodyTrackedJointsCount; }
        }

        public IList<JointType> BodyJointTypes 
        { 
            get 
            {
                return _bodyJointTypes;
            } 
        }

        public JointType BodyPositionJoint
        {
            get { return _bodyTrackingService.BodyPositionJoint; }
            set
            {
                if (_bodyTrackingService.BodyPositionJoint != value)
                {
                    _bodyTrackingService.BodyPositionJoint = value;
                    OnPropertyChanged("BodyPositionJoint");
                }
            }
        }

        public int MissingBodyCount { get { return _bodyTrackingService.MissingBodyCount; } }

        public ulong MissingBodyId { get { return _bodyTrackingService.MissingBodyId; } }

        public int HijackedBodyCount { get { return _bodyTrackingService.HijackedBodyCount; } }
        public ulong HijackedBodyId { get { return _bodyTrackingService.HijackedBodyId; } }

        public ulong ActiveBodyId
        {
            get { return _bodyTrackingService.ActiveBodyId; }
        }

        public ulong ActiveBodyCorrelationId
        {
            get { return _bodyTrackingService.ActiveBodyCorrelationId; }
        }

        private void RefreshBindings()
        {
            OnPropertyChanged("ActiveBodyId");
            OnPropertyChanged("ActiveBodyCorrelationId");
            OnPropertyChanged("MissingBodyCount");
            OnPropertyChanged("HijackedBodyCount");
            OnPropertyChanged("MissingBodyId");
            OnPropertyChanged("HijackedBodyId");
            OnPropertyChanged("TrackedKinectBodyCount");
            OnPropertyChanged("ActiveBodyTrackedJointsCount");
        }

        private void Button_LoadConfigurationClick(object sender, RoutedEventArgs e)
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
                    _bodyTrackingService.LoadConfiguration(filename);
                    RefreshBindings();
                }
                catch
                {
                    LoadConfiguraiontDefaults();
                }
            }
        }
        private void Button_SaveConfigurationClick(object sender, RoutedEventArgs e)
        {
            _bodyTrackingService.SaveConfiguration();
        }

        public void LoadConfiguraiontDefaults()
        {
            _bodyTrackingService.LoadConfigurationDefaults();
            RefreshBindings();
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();
            //this.kinectSensor = new KAIT.Kinect.Service.KinectSensorService();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            _bodyJointTypes = new List<JointType>();
            _bodyJointTypes.Add(JointType.Head);
            _bodyJointTypes.Add(JointType.Neck);
            _bodyJointTypes.Add(JointType.SpineBase);
            _bodyJointTypes.Add(JointType.SpineMid);
            _bodyJointTypes.Add(JointType.SpineShoulder);


            // initialize the components (controls) of the window
            this.InitializeComponent();

            //_bodyTrackingService.PropertyChanged += _bodyTrackingService_PropertyChanged;
            MissingBodyLeftEdgeBoundary = _bodyTrackingService.MissingBodyLeftEdgeBoundary;
            MissingBodyRightEdgeBoundary = _bodyTrackingService.MissingBodyRightEdgeBoundary;
            MissingBodyBackDepthBoundary = _bodyTrackingService.MissingBodyBackDepthBoundary;
            MissingBodyExpiredTimeLimit = _bodyTrackingService.MissingBodyExpiredTimeLimit;
            NewBodyLeftEdgeBoundary = _bodyTrackingService.NewBodyLeftEdgeBoundary;
            NewBodyRightEdgeBoundary = _bodyTrackingService.NewBodyRightEdgeBoundary;
            NewBodyBackDepthBoundary = _bodyTrackingService.NewBodyBackDepthBoundary;
            HijackedBodyJumpTolerance = _bodyTrackingService.HijackedBodyJumpTolerance;
            HighVelocityPlayerJumpMultiplier = _bodyTrackingService.HighVelocityPlayerJumpMultiplier;
            HighVelocityPlayerJumpThreshold = _bodyTrackingService.HighVelocityPlayerJumpThreshold;
            RequiredJointsTrackedCount = _bodyTrackingService.RequiredJointsTrackedCount;            
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

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
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;

                    #region this code calls the KAIT Advanced Body Tracking

                    _bodyTrackingService.TrackBodies(this.bodies);

                    //if (!_bodyTrackingService.IsActivePlayerStillTracked(this.bodies))
                    //{
                    //    if (!_bodyTrackingService.IsActivePlayerStillTracked(this.bodies))
                    //    {
                    //        _bodyTrackingService.GetNextPlayer(this.bodies);                            
                    //    }
                    //}

                    var activeBodyId = _bodyTrackingService.SetActivePlayer(this.bodies);                    

                    RefreshBindings();
                    #endregion
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }        
    }
}

