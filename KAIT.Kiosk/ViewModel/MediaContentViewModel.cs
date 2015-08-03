using GalaSoft.MvvmLight;
using KAIT.Common;
using KAIT.Common.Interfaces;
using KAIT.Common.Services.Messages;
using KAIT.ContentMetaData;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace KAIT.Kiosk.ViewModel
{
    public class MediaContentViewModel : ViewModelBase
    {
        IKioskInteractionService _interactionService;
        IDemographicsService _demographicsService;
        IContentManagement<ZoneFileMetaData> _contentManagement;
        IItemInteractionService _itemInteractionService;

        bool _missingDemographicData;
        ulong _missingUsersTrackingID;
        const string NOTRACK = "NoTrack";
        string _currentZone = NOTRACK;
        BiometricData _demographics;

        public event EventHandler Activated;
        public event EventHandler Deactivated;

        string _mediaSource;
        public string MediaSource
        {
            get { return _mediaSource; }
            set
            {
                _mediaSource = value;
                RaisePropertyChanged("MediaSource");
            }
        }

        public ManipulationStates ItemState { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>The issue was the content was jumping from image to image when the user was moving quickly between zoned.
        ///          This property is used so the content only changes at the end of it's viewing so the content transition is smoother.
        /// </remarks>
        public bool IsVideoPlaying { get; set; }

        string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set 
            { 
                _errorMessage = value;
                RaisePropertyChanged("ErrorMessage");
            }
        }

        string _displayZoneName;
        public string DisplayZoneName
        {
            get { return _displayZoneName; }
            set
            {
                _displayZoneName = value;
                RaisePropertyChanged("DisplayZoneName");
            }
        }

        public bool EnableDiagnostics { get; set; }

        //public string ClosestZone { get; set; }
        public static bool IsDebug
        {
            get
            {
                if (Debugger.IsAttached)
                    return true;
                else
                    return false;
            }
        }


        
        public MediaContentViewModel(IKioskInteractionService interactionService, 
                                     IDemographicsService demographicSrv, 
                                     IContentManagement<ZoneFileMetaData> contentManagement, 
                                     IItemInteractionService itemInteractionService)
        {
            ItemState = ManipulationStates.NoTrack;
            _interactionService = interactionService;
            _interactionService.KioskStateChanged += _interactionService_KioskStateChanged;

            _demographicsService = demographicSrv;
            _demographicsService.DemographicsReceived += _demographicsService_DemographicsReceived;
            _demographicsService.DemographicsProcessingFailure += _demographicsService_DemographicsProcessingFailure;

            _contentManagement = contentManagement;

            _itemInteractionService = itemInteractionService;
            _itemInteractionService.ItemInteraction += _itemInteractionService_ItemStateChanged;

            ConfigurationProvider cp = new ConfigurationProvider();
            IConfigSettings cs = cp.Load();
            EnableDiagnostics = cs.EnableDiagnostics;
            //ClosestZone = cs.ZoneDefinitions.Where(x => x.MaximumRange == cs.ZoneDefinitions.Min(o => o.MaximumRange)).First().Name;
        }

        void _demographicsService_DemographicsProcessingFailure(object sender, string e)
        {
            MessageBoxResult result = MessageBox.Show(String.Format("NEC Biometric Configuration Issue: {0}", e));
        }

        void _demographicsService_DemographicsReceived(object sender, BiometricData e)
        {
            if (_missingDemographicData)
            {
                if (e.TrackingId == _missingUsersTrackingID)
                {
                    SelectContentBasedOnDemographics(e);  
                }
            }
        }


        public void MoveNext()
        {
            IFileMetaData metaData = _contentManagement.MoveNext();
            if (metaData != null)
            {
                MediaSource = metaData.ContentPath;
                ErrorMessage = String.Empty;
            }
            else
            {
                ConfigurationProvider cp = new ConfigurationProvider();
                IConfigSettings cs = cp.Load();
                if (_demographics != null)
                    ErrorMessage = String.Format(@"Media Content is missing. Root Directory setting: {0}\{1}", cs.RootContentDirectory, _currentZone);
                else
                    ErrorMessage = String.Format(@"Media Content is missing in the following directory: {0}\{1} for {2}yo {3}", cs.RootContentDirectory, _currentZone, _demographics.Age.ToString(), _demographics.Gender.ToString());
            }
            
        }

        void _interactionService_KioskStateChanged(object sender, KioskStateEventArgs e)
        {
            if (e.KioskState == "Tracking")
            {
                Debug.Print("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ Media Content Kiosk State Change " + e.CurrentZone);
                if (_currentZone != e.CurrentZone)
                {
                    if (_currentZone == NOTRACK) IsVideoPlaying = false;
                    _currentZone = e.CurrentZone;
                    DisplayZoneName = _currentZone;
                    if (e.Demographics == null)
                    {
                        _missingDemographicData = true;
                        _missingUsersTrackingID = e.TrackingID;
                    }
                    else
                    {
                        _missingDemographicData = false;
                    }

                    if (e.Demographics != null)
                        Debug.Print("Demographics during Kiosk State Change" + e.Demographics.Gender.ToString());

                    _demographics = e.Demographics;
                    SelectContentBasedOnDemographics(e.Demographics);
                    OnActivated();
                }
            }
            else if (e.KioskState == NOTRACK)
            {
                _currentZone = NOTRACK;
                DisplayZoneName = _currentZone;
                SelectContentBasedOnDemographics(null);
                OnActivated();
            }
            else
                OnDeactivated();
        }

        void _itemInteractionService_ItemStateChanged(object sender, KioskStateEventArgs e)
        {
            if (e.ItemState != ManipulationStates.NoTrack && e.ItemState != ItemState)
            {
                    Debug.Print("@@@ITEM STATE: " + e.ItemState.ToString());
                    DisplayZoneName = _currentZone + ", " + e.ItemState.ToString();
                    ItemState = e.ItemState;

                    switch (e.ItemState)
	                {
                        case ManipulationStates.NoTrack:
                                break;

                        case ManipulationStates.Touched:
                            IsVideoPlaying = false;     // when an item is touched, change to the item content immediately
                            SelectContentBasedOnItem(e.ItemSelected, ManipulationStates.Touched);
                            break;

                        case ManipulationStates.Removed:
                            // Show something different
                            //break;

		                default:    // released/replaced
                            SelectContentBasedOnDemographics(_demographics);
                            //SelectContentBasedOnItem(e.ItemSelected, ManipulationStates.Released);
                            break;
	                }

                    OnActivated();
            }

        }

        private void SelectContentBasedOnItem(string itemSelected, ManipulationStates state)
        {
            bool hasContent = _contentManagement.LoadItemContents(itemSelected, state);

            if (hasContent)
            {
                if (!IsVideoPlaying)
                    MediaSource = _contentManagement.MoveNext().ContentPath;
            }
            else
            {
                ConfigurationProvider cp = new ConfigurationProvider();
                IConfigSettings cs = cp.Load();
                ErrorMessage = String.Format(@"Media Content is missing in the following directory: {0}\{1}, for interaction {2}", cs.RootContentDirectory, itemSelected, state.ToString());
            }
        }

        private void SelectContentBasedOnDemographics(BiometricData currentUser, string itemSelected = null) 
        {
            bool hasContent = false;

            if (string.IsNullOrEmpty(itemSelected))
            {
                if (currentUser == null)
                    hasContent = _contentManagement.LoadContents(_currentZone);
                else
                    hasContent = _contentManagement.LoadContents(_currentZone, (m) => { return (currentUser.Age >= m.Age && currentUser.Age < m.MaxAge) && m.Gender.ToString().ToLower() == currentUser.Gender.ToString().ToLower(); });
            }
            else
            {
                if (currentUser == null)
                    hasContent = _contentManagement.LoadContents(itemSelected);
                else
                    hasContent = _contentManagement.LoadContents(itemSelected, (m) => { return (currentUser.Age >= m.Age && currentUser.Age < m.MaxAge) && m.Gender.ToString().ToLower() == currentUser.Gender.ToString().ToLower(); });
            }

            if (hasContent)
            {
                if (!IsVideoPlaying)
                    MediaSource = _contentManagement.MoveNext().ContentPath;
            }
            else
            {
                ConfigurationProvider cp = new ConfigurationProvider();
                IConfigSettings cs = cp.Load();
                if (string.IsNullOrEmpty(itemSelected))
                {
                    if (currentUser == null)
                        ErrorMessage = String.Format(@"Media Content is missing in the following directory: {0}\{1}", cs.RootContentDirectory, _currentZone);
                    else
                        ErrorMessage = String.Format(@"Media Content is missing in the following directory: {0}\{1} for {2}yo {3}", cs.RootContentDirectory, _currentZone, currentUser.Age.ToString(), currentUser.Gender.ToString());
                }
                else
                {
                    if (currentUser == null)
                        ErrorMessage = String.Format(@"Media Content is missing in the following directory: {0}\{1}", cs.RootContentDirectory, itemSelected);
                    else
                        ErrorMessage = String.Format(@"Media Content is missing in the following directory: {0}\{1} for {2}yo {3}", cs.RootContentDirectory, itemSelected, currentUser.Age.ToString(), currentUser.Gender.ToString());
                }
            }
        }

        private void OnDeactivated()
        {
            var handler = this.Deactivated;
            if (handler != null)
                handler(this, null);
        }

        private void OnActivated()
        {
            var handler = this.Activated;
            if (handler != null)
                handler(this, null);
        }


    }
}
