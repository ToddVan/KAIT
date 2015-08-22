using Microsoft.Kinect;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using KAIT.Common;
using KAIT.Common.Interfaces;
using KAIT.Common.Sensor;
using KAIT.Common.Services.Messages;
using KAIT.EventHub.Messaging;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using KAIT.Kinect.Service;

namespace KAIT.Kiosk.Services
{    
    public class KioskInteractionService : IKioskInteractionService
    {
        public enum KioskStates
        {
            Ready,
            NoTrack,
            Tracking,
                // note: tracking includes moving through zones
            Touch,
            Special,
            Help,
            NoSensor,
            Error

        }

        const int SPECIAL_GESTURE_DELAY = 30;  // frames processed before activation of gesture

        ISensorService<KinectSensor> _sensorService;
        IDemographicsService _demographicService;
        IItemInteractionService _itemInteractionService;
        IConfigurationProvider _configurationProvider;
        IBodyTrackingService _bodyTrackingService { get; set; }
        
        BlockingCollection<KioskStateEventArgs> _interactionProcessingQueue;
        private ContentInteraction _lastContentInteraction = new ContentInteraction();
        
        CoordinateMapper _coordinateMapper;
        private EventHubMessageSender _eventHub;
        BodyFrameReader _bodyFrameReader;

        Body[] _bodies;
        DateTimeOffset _lastStateChange;

        //DispatcherTimer _timer;
        TimeSpan _timerInterval = TimeSpan.FromSeconds(1);

        public event EventHandler<KioskStateEventArgs> KioskStateChanged;
        public event EventHandler<BodyTrackEventArgs> BodyTrackUpdate;

        KioskStates _currentState;
        KioskStates CurrentState {
            get { return _currentState; }
            set {
                if (_currentState == value)       
                    return;
                _lastStateChange = DateTimeOffset.UtcNow;

                _currentState = value;
                OnKioskStateChanged(value);
            }
        }
        public string KioskState
        {
            get { return _currentState.ToString(); }
        }

        public IConfigSettings ConfigurationSettings { get; set; }

        private string _currentZone;
        public string CurrentZone 
        {
            get
            { return _currentZone; }
            set
            {
                if (_currentZone == value)
                    return;
                _lastStateChange = DateTimeOffset.UtcNow;

                _currentZone = value;
                OnKioskStateChanged(CurrentState);
            }
        }        

        public KioskInteractionService(ISensorService<KinectSensor> sensorService,
            IDemographicsService demographicsService,
            IItemInteractionService itemInteractionService,
            IBodyTrackingService bodyTrackingService,
            IConfigurationProvider configurationProvider)
        {
            _currentZone = "NoTrack";
            _demographicService = demographicsService;

            _eventHub = new EventHubMessageSender(ConfigurationManager.AppSettings["Azure.Hub.Kiosk"]);
            
            _sensorService = sensorService;
          //  _telemetryService = telemetryService;

            _itemInteractionService = itemInteractionService;
            _itemInteractionService.ItemInteraction += _itemInteractionService_ItemInteraction;
            _coordinateMapper = _sensorService.Sensor.CoordinateMapper;

            _configurationProvider = configurationProvider;
            _configurationProvider.ConfigurationSettingsChanged += _configurationProvider_ConfigurationSettingsChanged;
            GetConfig();

            _sensorService.StatusChanged += _sensorService_StatusChanged;
            _bodyFrameReader = _sensorService.Sensor.BodyFrameSource.OpenReader();
            if (_bodyFrameReader != null)
                _bodyFrameReader.FrameArrived += _bodyFrameReader_FrameArrived;
          
            _sensorService.Open();
            
            _interactionProcessingQueue = new BlockingCollection<KioskStateEventArgs>();
            {
                IObservable<KioskStateEventArgs> ob = _interactionProcessingQueue.
                  GetConsumingEnumerable().
                  ToObservable(TaskPoolScheduler.Default);

                ob.Subscribe(p =>
                {
                    //var temp = Thread.CurrentThread.ManagedThreadId;
                    // This handler will get called whenever 
                    // anything appears on myQueue in the future.
                    this.SendIteraction(p);
                    //Debug.Write("Consuming: {0}\n", p);
                });
            }

            _bodyTrackingService = bodyTrackingService;

            CurrentState = KioskStates.NoTrack;
        }

        void _configurationProvider_ConfigurationSettingsChanged(object sender, KioskConfigSettingsEventArgs e)
        {
            GetConfig();
        }


        void _itemInteractionService_ItemInteraction(object sender, KioskStateEventArgs e)
        {
            if (e.ItemState != ManipulationStates.NoTrack)
            {
                Debug.Print("Sending Object Interaction Event: " + e.ItemSelected);
                var kioskEvent = new KioskStateEventArgs() { TrackingID = _bodyTrackingService.ActiveBodyId, KioskState = CurrentState.ToString(), CurrentZone = CurrentZone };
                kioskEvent.ItemState = e.ItemState;
                kioskEvent.ItemSelected = e.ItemSelected;
                _interactionProcessingQueue.Add(kioskEvent);
            }
        }


        private void GetConfig()
        {
            ConfigurationSettings = _configurationProvider.Load();         
        }
        void _sensorService_StatusChanged(object sender, SensorStatusEventArgs e)
        {
            switch (e.Status)
            {
                case SensorStatus.Ready:
                    //CurrentState = KioskStates.Ready;
                    CurrentState = KioskStates.Tracking;
                    break;
                case SensorStatus.NoSensor:
                    CurrentState = KioskStates.NoSensor;
                    break;
                case SensorStatus.Closed:
                    CurrentState = KioskStates.NoTrack;
                    break;
                case SensorStatus.Error:
                    CurrentState = KioskStates.Error;
                    break;
            }
        }


        void _bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                    return;

                if (this._bodies == null)
                    this._bodies = new Body[bodyFrame.BodyCount];
                               
                bodyFrame.GetAndRefreshBodyData(this._bodies);                

                #region this code calls the Advanced Body Tracking Code
                _bodyTrackingService.TrackBodies(this._bodies);
                
                //System.Diagnostics.Debug.Print("Body Tracking Active Body TrackingId:{0}, CorrelationId:{1}", _bodyTrackingService.ActiveBodyId, _bodyTrackingService.ActiveBodyCorrelationId);                               
                var activeBodyId = _bodyTrackingService.SetActivePlayer(this._bodies);
                if (activeBodyId != _itemInteractionService.ActivePlayerId)
                {
                    _itemInteractionService.ActivePlayerId = activeBodyId;
                    _itemInteractionService.CorrelationPlayerId = activeBodyId;
                }
                #endregion

                SetKioskState();
                UpdateBodyTrack(); //required for body tracking ellipse on main view
            }
        }
        
        private void UpdateBodyTrack()
        {
            var handler = this.BodyTrackUpdate;
            if (handler == null)
                return;

            var others = _bodyTrackingService.GetOtherBodies(this._bodies);

            var bodyTrack = new float[others.Count() + 1];
            bodyTrack[0] = (_bodyTrackingService.ActiveBody != null) ? _coordinateMapper.MapCameraPointToDepthSpace(_bodyTrackingService.ActiveBody.Joints[JointType.Neck].Position).X : 0;

            for (int i = 0; i < others.Count(); i++)
            {

                bodyTrack[i + 1] = _coordinateMapper.MapCameraPointToDepthSpace(others[i].Joints[JointType.Neck].Position).X;
            }

            handler(this, new BodyTrackEventArgs() { BodyTrack = bodyTrack });
        }


        private void OnKioskStateChanged(KioskStates newKioskState)
        {
            Debug.WriteLine("NewKioskState: " + newKioskState.ToString());
            var kioskEvent = new KioskStateEventArgs() { TrackingID = _bodyTrackingService.ActiveBodyId, KioskState = CurrentState.ToString(), CurrentZone = CurrentZone };

            //make sure pass the correct demographics for this player
            var activeUserDemographics = (from user in _demographicService.UserExperiences where user.TrackingId == _bodyTrackingService.ActiveBodyId select user).FirstOrDefault();

            kioskEvent.Demographics = activeUserDemographics;
            kioskEvent.ContentAction = ContentAction.Enter;

            _interactionProcessingQueue.Add(kioskEvent);

            var handler = this.KioskStateChanged;
            if (handler != null)
                handler(this, kioskEvent);
        }

        
        private void SendIteraction(KioskStateEventArgs args)
        {
          //  Debug.Print("Send Interactions........");
          //  return;

            var contentInteraction = new ContentInteraction();

            if (args.ContentAction == ContentAction.Exit)
            {
                var duration = contentInteraction.TimeStamp - _lastStateChange;
                contentInteraction.Duration = duration;
            }
            contentInteraction.Action = args.ContentAction;
            contentInteraction.KioskState = args.KioskState;
            contentInteraction.TrackingId = args.TrackingID;
            
            contentInteraction.CorrelatedTrackingID = args.TrackingID;
          
            contentInteraction.DeviceSelection = args.ItemSelected ?? string.Empty;
            contentInteraction.InteractionZone = CurrentZone ?? string.Empty;
            contentInteraction.KinectDeviceId = this._sensorService.Sensor.UniqueKinectId;
            contentInteraction.Location = new LocationCoordinates
            {
                Latitude = float.Parse(ConfigurationManager.AppSettings["LocationLatitude"]),
                Longitude = float.Parse(ConfigurationManager.AppSettings["LocationLongitutde"])
            };


            switch (args.ItemState)
            {
                case ManipulationStates.NoTrack:
                    contentInteraction.DeviceSelectionState = DeviceSelectionState.NoTrack;
                    break;
                case ManipulationStates.Released:
                    contentInteraction.DeviceSelectionState = DeviceSelectionState.Released;
                    break;
                case ManipulationStates.Removed:
                    contentInteraction.DeviceSelectionState = DeviceSelectionState.DeviceRemoved;
                    break;
                case ManipulationStates.Replaced:
                    contentInteraction.DeviceSelectionState = DeviceSelectionState.DeviceReplaced;
                    break;
                case ManipulationStates.Touched:
                    contentInteraction.DeviceSelectionState = DeviceSelectionState.Touched;
                    break;
            }

            contentInteraction.TimeStamp = DateTime.Now;

            if (_lastContentInteraction.InteractionZone != contentInteraction.InteractionZone || _lastContentInteraction.DeviceSelectionState != contentInteraction.DeviceSelectionState)
            {
                _lastContentInteraction = contentInteraction;
                System.Diagnostics.Debug.Print("**** SendInteraction ****, TrackingId{0}, Zone{1}, SelectionState{2}, Timestamp{3}", contentInteraction.TrackingId, contentInteraction.InteractionZone, contentInteraction.DeviceSelectionState, contentInteraction.TimeStamp);
                _eventHub.SendMessageToEventHub(contentInteraction);
            }
        }

        void SetKioskState()
        {
            if (ConfigurationSettings.ZoneDefinitions.Count == 0)  // we don't have interaction ranges so will default to no track right now
            {
                _currentZone = "NoTrack";      
                CurrentState = KioskStates.NoTrack;
                return;
            }

            var activePlayer = (from body in _bodies where body.TrackingId == _bodyTrackingService.ActiveBodyId select body).FirstOrDefault();
            KioskStates newState;

            if (activePlayer != null && activePlayer.IsTracked && activePlayer.TrackingId != 0)
            {
                newState = KioskStates.Tracking;
                int count = 0;
                foreach (var z in ConfigurationSettings.ZoneDefinitions.OrderByDescending(x => x.MaximumRange))
                {
                    count++;
                    if (activePlayer.Joints[JointType.SpineMid].Position.Z >= z.MaximumRange)
                    {
                        CurrentZone = z.Name;
                        if (ConfigurationSettings.ZoneDefinitions.Count() == count && ConfigurationSettings.EnableTouchScreen)
                            newState = KioskStates.Touch;
                        break;
                    }
                }
            }
            else 
            {
                newState = KioskStates.NoTrack;
                _currentZone = "NoTrack";
            }

            CurrentState = newState;
        }

    }
}
