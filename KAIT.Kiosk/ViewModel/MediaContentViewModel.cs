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
using KAIT.Common;
using KAIT.Common.Interfaces;
using KAIT.Common.Services.Messages;
using KAIT.ContentMetaData;
using System;
using System.Diagnostics;
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
        ulong _currentUserTrackingID = 0;

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

        string _Prod1Content;
        public string Prod1ComparisonContent
        {
            get { return _Prod1Content; }
            set
            {
                _Prod1Content = value;
                RaisePropertyChanged("Prod1ComparisonContent");
            }
        }

        string _Prod2Content;
        public string Prod2ComparisonContent
        {
            get { return _Prod2Content; }
            set
            {
                _Prod2Content = value;
                RaisePropertyChanged("Prod2ComparisonContent");
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

        private string _MediaContentState = "Normal";
        public string MediaContentState
        {
            get { return _MediaContentState; }
            set
            {
                if (_MediaContentState == value)
                    return;
                // _lastKioskState = _kioskState;
                _MediaContentState = value;
                RaisePropertyChanged("MediaContentState");
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

        string tmp = "";
        public void MoveNext()
        {
            IFileMetaData metaData = _contentManagement.MoveNext();
            if (metaData != null)
            {
                if (tmp == metaData.ContentPath)
                {
                    
                }

                tmp = metaData.ContentPath;
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
             
                if (_currentZone != e.CurrentZone || _currentUserTrackingID != e.TrackingID)
                {
                   
                    //remember the last state we had before selecting content.
                    MediaContentState = "Normal";
                    _currentZone = e.CurrentZone;
                    _currentUserTrackingID = e.TrackingID;

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
                        Debug.Print("Demographics during Kiosk State Change " + e.Demographics.Gender.ToString());

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

        int _activeItems = 0;
        string lastItemSelected = "";

        void _itemInteractionService_ItemStateChanged(object sender, KioskStateEventArgs e)
        {
            if (e.ItemState != ManipulationStates.NoTrack /*&& e.ItemState != ItemState*/)
            {
                Debug.Print(e.ItemSelected + " " + e.ItemState + " " + _activeItems.ToString());
                DisplayZoneName = _currentZone + ", " + e.ItemState.ToString();
                    ItemState = e.ItemState;

                switch (e.ItemState)
                {
                    case ManipulationStates.NoTrack:
                        break;

                    case ManipulationStates.Touched:
                        _activeItems++;
                        if (_activeItems == 1)
                        { 
                            IsVideoPlaying = false;     // when an item is touched, change to the item content immediately
                            SelectContentBasedOnItem(e.ItemSelected, ManipulationStates.Touched);
                            lastItemSelected = e.ItemSelected;
                        }
                        else if(_activeItems == 2)
                        {
                            //Display comparisons
                            SelectContentForComarison(lastItemSelected, e.ItemSelected);
                        }

                        break;

                        case ManipulationStates.Removed:
                            // Show something different
                            //break;

		                default:    // released/replaced
                            if(_activeItems == 2) //If we are coming back to just one item selected from a compare we need to force the system to reload the content and not auto cycle as the content may still be for the comparison
                                IsVideoPlaying = false;

                        _activeItems--;

                             if (MediaContentState == "Compare")
                            {
                                SelectContentBasedOnItem(lastItemSelected, ManipulationStates.Touched);
                            }
                                                         
                            
                           
                            MediaContentState = "Normal";
                        break;
	                }

                    OnActivated();
            }

        }

        private void SelectContentForComarison(string item1, string item2)
        {
            bool item1hasContent = _contentManagement.LoadItemContents(item1, ManipulationStates.Compare);

            if (item1hasContent)
            {
                Prod1ComparisonContent = _contentManagement.MoveNext().ContentPath;
            }

            bool item2hasContent = _contentManagement.LoadItemContents(item2, ManipulationStates.Compare);

            if (item2hasContent)
            {
                Prod2ComparisonContent = _contentManagement.MoveNext().ContentPath;
            }

            if(item1hasContent && item2hasContent) // Turn on comparison view
            {
                MediaContentState = "Compare";
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
