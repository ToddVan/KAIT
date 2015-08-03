using AutoMapper;
using Inception.Common.Services.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Forms;
//using GalaSoft.MvvmLight;

namespace Inception.ConfigSettingsConsole.ViewModel
{
    
    public class ConfigurationViewModel : ObservableObject
    {
        private string _rootContentDirectory;
        public string RootContentDirectory
        {
            get { return _rootContentDirectory; }
            set
            {
                _rootContentDirectory = value;
                RaisePropertyChangedEvent("RootContentDirectory");
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
                RaisePropertyChangedEvent("Status");
            }
        }

        private string _zoneName;
        public string ZoneName 
        {
            get { return _zoneName; }
            set
            {
                _zoneName = value;
                RaisePropertyChangedEvent("ZoneName");
            }
        }

        private string _zoneRange;
        public string ZoneRange 
        {
            get { return _zoneRange; }
            set
            {
                _zoneRange = value;
                RaisePropertyChangedEvent("ZoneRange");
            }
        }

        private ObservableCollection<ZoneDefinition> _zones;
        public ObservableCollection<ZoneDefinition> ZoneDefinitions
        {
            get { return _zones; }
            set
            {
                _zones = value;
                RaisePropertyChangedEvent("Zones");
            }
        }

        public ZoneDefinition SelectedZone { get; set; }


        public ConfigurationViewModel()
        {
            Mapper.CreateMap<ConfigurationViewModel, ConfigSettings>();

            ConfigurationProvider cp = new ConfigurationProvider();
            var cs = cp.Load();

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

        public ICommand SaveSettingsCommand
        {
            get { return new DelegateCommand(SaveText); }
        }

        private void SaveText()
        {
            ConfigSettings cs = Mapper.Map<ConfigurationViewModel, ConfigSettings>(this);

            if (string.IsNullOrEmpty(cs.RootContentDirectory) || !Directory.Exists(cs.RootContentDirectory))
            {
                Status = "Invalid Root Directory.  File not saved.";
                return;
            }

            if (cs.ZoneDefinitions.Count() == 0)
            {
                Status = "Must create at least one zone.  File not saved.";
                return;
            }

            ConfigurationProvider cp = new ConfigurationProvider();
            Status = cp.WriteFile(cs);
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
}
