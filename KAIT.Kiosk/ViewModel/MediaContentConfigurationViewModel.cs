using KAIT.Common.Services.Messages;
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

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight;

namespace KAIT.Kiosk.ViewModel
{    
    public class MediaContentConfigurationViewModel : ViewModelBase
    {
        private string _rootContentDirectory;
        private IConfigurationProvider _configurationProvider;
        public string RootContentDirectory
        {
            get { return _rootContentDirectory; }
            set
            {
                _rootContentDirectory = value;
                RaisePropertyChanged("RootContentDirectory");
            }
        }

        private bool _showJpgFiles;
        public bool ShowJpgFiles
        {
            get { return _showJpgFiles; }
            set { _showJpgFiles = value; }
        }

        private bool _showPngFiles;
        public bool ShowPngFiles
        {
            get { return _showPngFiles; }
            set { _showPngFiles = value; }
        }

        private bool _showGifFiles;
        public bool ShowGifFiles
        {
            get { return _showGifFiles; }
            set { _showGifFiles = value; }
        }

        private bool _showWmvFiles;
        public bool ShowWmvFiles
        {
            get { return _showWmvFiles; }
            set { _showWmvFiles = value; }
        }

        private bool _showMp4Files;
        public bool ShowMp4Files
        {
            get { return _showMp4Files; }
            set { _showMp4Files = value; }
        }

        private bool _isTouchEnabled;
        public bool EnableTouchScreen
        {
            get { return _isTouchEnabled; }
            set { _isTouchEnabled = value; }
        }

        private bool _isDiagnosticsEnabled;
        public bool EnableDiagnostics
        {
            get { return _isDiagnosticsEnabled; }
            set { _isDiagnosticsEnabled = value; }
        }

        private bool _useDefaultContent;
        public bool UseDefaultContent
        {
            get { return _useDefaultContent; }
            set { _useDefaultContent = value; }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set 
            { 
                _status = value;
                RaisePropertyChanged("Status");
            }
        }

        private string _zoneName;
        public string ZoneName 
        {
            get { return _zoneName; }
            set
            {
                _zoneName = value;
                RaisePropertyChanged("ZoneName");
            }
        }

        private string _zoneRange;
        public string ZoneRange 
        {
            get { return _zoneRange; }
            set
            {
                _zoneRange = value;
                RaisePropertyChanged("ZoneRange");
            }
        }

        private ObservableCollection<ZoneDefinition> _zones;
        public ObservableCollection<ZoneDefinition> ZoneDefinitions
        {
            get { return _zones; }
            set
            {
                _zones = value;
                RaisePropertyChanged("Zones");
            }
        }

        public ZoneDefinition SelectedZone { get; set; }


        public MediaContentConfigurationViewModel(IConfigurationProvider configurationProvider)
        {            
            _configurationProvider = configurationProvider;
           
            var cs = _configurationProvider.Load();
            ConfigureContent(cs);

        }

        private void ConfigureContent(IConfigSettings cs)
        {
            if (cs != null)
            {
                _rootContentDirectory = cs.RootContentDirectory;
                _showGifFiles = cs.ShowGifFiles;
                _showJpgFiles = cs.ShowJpgFiles;
                _showMp4Files = cs.ShowMp4Files;
                _showPngFiles = cs.ShowPngFiles;
                _showWmvFiles = cs.ShowWmvFiles;
                _isTouchEnabled = cs.EnableTouchScreen;
                _isDiagnosticsEnabled = cs.EnableDiagnostics;

                _zones = new ObservableCollection<ZoneDefinition>();
                if (cs.ZoneDefinitions != null)
                {
                    foreach (var z in cs.ZoneDefinitions)
                    {
                        _zones.Add(z);
                    }
                }
            }
        }

        void _configurationProvider_ConfigurationSettingsChanged(object sender, Common.KioskConfigSettingsEventArgs e)
        {
            ConfigureContent(e.ConfigSettings);
        }

        public ICommand SaveSettingsCommand
        {
            get { return new DelegateCommand(SaveText); }
        }

        private void SaveText()
        {
            if (string.IsNullOrEmpty(this.RootContentDirectory) || !Directory.Exists(this.RootContentDirectory))
            {
                Status = "Invalid Root Directory.  File not saved.";
                return;
            }

            if (this.ZoneDefinitions.Count() == 0)
            {
                Status = "Must create at least one zone.  File not saved.";
                return;
            }

            ConfigSettings cs = new ConfigSettings()
            {
                EnableDiagnostics = this.EnableDiagnostics,
                RootContentDirectory = this.RootContentDirectory,
                EnableTouchScreen = this.EnableTouchScreen,
                ShowGifFiles = this.ShowGifFiles,
                ShowJpgFiles = this.ShowJpgFiles,
                ShowMp4Files = this.ShowMp4Files,
                ShowPngFiles = this.ShowPngFiles,
                ShowWmvFiles = this.ShowWmvFiles,
                ZoneDefinitions = this.ZoneDefinitions.ToList(),
            };

            Status = _configurationProvider.WriteFile(cs);
        }

        public ICommand BrowsePathCommand
        {
            get { return new DelegateCommand(BrowsePath); }
        }

        private void BrowsePath()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            RootContentDirectory = dialog.SelectedPath;
        }

        public ICommand AddZoneCommand
        {
            get { return new DelegateCommand(AddZone); }
        }
        
        private void AddZone()
        {
            double range = 0;
            if (double.TryParse(ZoneRange, out range))
            {
                if (range > 8)
                {
                    Status = "The maximum a zone range can be is 8 meters.  Zone not added.";
                    return;
                }
            }
            else
            {
                Status = "The maximum a zone range must be a number.  Zone not added.";
                return;
            }

            ZoneDefinitions.Add(new ZoneDefinition() { Name = ZoneName, MaximumRange = range });
            ZoneName = string.Empty;
            ZoneRange = string.Empty;
            Status = string.Empty;
        }

        public ICommand RemoveZoneCommand
        {
            get { return new DelegateCommand(RemoveZone); }
        }

        private void RemoveZone()
        {
            ZoneDefinitions.Remove(SelectedZone);
        }
    }

    public class DelegateCommand : ICommand
    {
        private readonly Action _action;

        public DelegateCommand(Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67
    }

}

