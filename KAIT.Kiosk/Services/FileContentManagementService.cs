using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using KAIT.Common.Services.Messages;
using System.Diagnostics;

namespace KAIT.ContentMetaData
{
    /// <summary>
    /// This service is used to retrieve media content for the Kiosk
    /// 
    /// Folder structure should be set up as follows
    ///     root/contentTypeName/demographicData1/demographicData1
    ///     
    ///     root = defined in app.config or json file (created from a console app)
    ///     content Type Name = zone or item tracked 
    ///     demographic data = is gender or age (folders can be set up as gender/age or age/gender, just not both ways)
    ///     
    ///     example:
    ///     c:\KioskRoot\PassiveEngage\Male\30
    ///     c:\KioskRoot\ActiveAttract\Female\50
    ///     c:\KioskRoot\Item1
    ///     c:\KioskRoot\Item2
    ///     c:\KioskRoot\Item3
    ///     c:\KioskRoot\Item4
    ///     
    /// Default content is found at the "folder content" level, this is because the filter passed in needs both gender and age 
    /// to find content - if one or both folders are missing, default to the ContentTypeName level and load all files found.
    /// 
    /// </summary>
    public class FileContentManagementService : IContentManagement<ZoneFileMetaData>
    {
        int _index;
        string _root;
        List<string> _fileExtensions;
        IConfigurationProvider _configurationProvider;

        public IEnumerable<IFileMetaData> _filteredContentList { get; set; }


        public IList<ZoneFileMetaData> _allZoneContentList { get; set; }
        public IList<ZoneFileMetaData> _defaultZoneContentList { get; set; }
        public IList<ItemFileMetaData> _allItemContentList { get; set; }
        public IList<ItemFileMetaData> _defaultItemContentList { get; set; }

        public IFileMetaData CurrentContentItem
        {
            get
            {
                if (_filteredContentList.ToList().Count == 0)
                    return null;
                else
                {
                    var content = _filteredContentList.ToList()[_index++];
                    return content;
                }
            }
        }



        public FileContentManagementService(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
            _configurationProvider.ConfigurationSettingsChanged += _configurationProvider_ConfigurationSettingsChanged;
            IConfigSettings cs = _configurationProvider.Load();

            ConfigureContent(cs);

            // Load up entire directory structure
            LoadZoneFolderContent();
            LoadItemFolderContent();
            
        }

        private void ConfigureContent(IConfigSettings cs)
        {
            _root = @cs.RootContentDirectory;
            _fileExtensions = new List<string>();

            if (cs.ShowJpgFiles)
            {
                _fileExtensions.Add(".jpg");
                _fileExtensions.Add(".jpeg");
            }

            if (cs.ShowPngFiles) { _fileExtensions.Add(".png"); }
            if (cs.ShowGifFiles) { _fileExtensions.Add(".gif"); }
            if (cs.ShowWmvFiles) { _fileExtensions.Add(".wmv"); }
            if (cs.ShowMp4Files) { _fileExtensions.Add(".mp4"); }
        }


        void _configurationProvider_ConfigurationSettingsChanged(object sender, Common.KioskConfigSettingsEventArgs e)
        {
            ConfigureContent(e.ConfigSettings);
        }

        private void LoadZoneFolderContent()
        {
            //Zone
            if (!Directory.Exists(_root))
                return;

            var rootDirectory = new DirectoryInfo(_root);
            var metaList = new List<ZoneFileMetaData>();
            var defaultList = new List<ZoneFileMetaData>();

            // Loop thru the content folders....
            foreach (var contentFolder in rootDirectory.GetDirectories().Where(x => !x.Name.ToLower().Contains("item")))
            { 
                // looping thru 1st demographic folders
                foreach (var d1 in contentFolder.GetDirectories())
                {
                    // 2nd demographic
                    foreach (var d2 in d1.GetDirectories())
                    {
                        // getting content
                        foreach (var f in d2.GetFiles())
                        {
                            string ext = Path.GetExtension(f.Name);
                            if (_fileExtensions.Contains(ext))
                            { 
                                int age = 0;
                                var content = new ZoneFileMetaData() { ContentPath = f.FullName };
                                if (int.TryParse(d2.Name, out age))
                                {
                                    content.Age = age;
                                    content.Gender = d1.Name;
                                }
                                else
                                {
                                    if (int.TryParse(d1.Name, out age))
                                        content.Age = age;
                                    content.Gender = d2.Name;
                                }
                                content.ContentFolder = contentFolder.Name;
                                metaList.Add(content);
                            }
                        }
                    }
                }

                // default data will be here
                foreach (var f in contentFolder.GetFiles())
                {
                    string ext = Path.GetExtension(f.Name);
                    if (_fileExtensions.Contains(ext))
                    {
                        var content = new ZoneFileMetaData() { ContentPath = f.FullName, ContentFolder = contentFolder.Name };
                        defaultList.Add(content);
                    }
                }

            }   // root folder

            // populate max age
            foreach (var ml in metaList)
            {
                var filteredMetaList = metaList.Where(m => m.Age > ml.Age && m.Gender == ml.Gender && m.ContentFolder == ml.ContentFolder).OrderBy(m => m.Age).FirstOrDefault();
                if (filteredMetaList != null)
                    ml.MaxAge = filteredMetaList.Age - 1;
                else
                    ml.MaxAge = 1000;
            }

            _allZoneContentList = metaList;
            _defaultZoneContentList = defaultList;

        }

        private void LoadItemFolderContent()
        { 
            if (!Directory.Exists(_root))
                return;

            var rootDirectory = new DirectoryInfo(_root);
            var metaList = new List<ItemFileMetaData>();
            var defaultMetaList = new List<ItemFileMetaData>();

            // Loop thru the item content folders....
            foreach (var contentFolder in rootDirectory.GetDirectories().Where(x => x.Name.ToLower().Contains("item")))
            {
                // if there are files at the item level, it is default content
                foreach (var f in contentFolder.GetFiles())
                {
                    var content = new ItemFileMetaData() { ContentFolder = contentFolder.Name, ContentPath = f.FullName, ItemState = contentFolder.Name };
                    defaultMetaList.Add(content);
                }
                

                if (contentFolder.GetDirectories().Count() != 0)    // get content for manipulation state
                {
                    // looping thru the manipulation state folders
                    foreach (var d1 in contentFolder.GetDirectories())
                    {
                        foreach (var f in d1.GetFiles())
                        {
                            var content = new ItemFileMetaData() { ContentFolder = contentFolder.Name, ContentPath = f.FullName, ItemState = d1.Name };
                            metaList.Add(content);
                        }
                    }
                }
            }

            _allItemContentList = metaList;
            _defaultItemContentList = defaultMetaList;
        }

        // contentFolder is either the zone or object
        public bool LoadContents(string contentFolder, Func<ZoneFileMetaData, bool> filter = null)
        {
            _index = 0;
            
            _filteredContentList = GetContent(contentFolder, filter);
            var filteredContentList = _filteredContentList.ToList();
            if (filteredContentList.Count == 0)
            {
                Debug.Print("@@@@@@@@@@@@@@@@@@@@@@@ content is empty.");
            }

            if (filteredContentList != null && filteredContentList.Count > 0)
                return true;        // has content
            else
                return false;
        }

        public bool LoadItemContents(string item, ManipulationStates itemState)
        { 

            _filteredContentList = null;
            _filteredContentList = _allItemContentList.Where(x => x.ContentFolder.ToLower() == item.ToLower() && x.ItemState.ToLower() == itemState.ToString().ToLower());

            // if _filteredContentList is null, return default content
            if (_filteredContentList == null || _filteredContentList.Count() == 0)
            {
                _filteredContentList = _defaultItemContentList.Where(x => x.ContentFolder.ToLower() == item.ToLower());
            }

            if (_filteredContentList != null && _filteredContentList.Count() > 0)
                return true;        // has content
            else
                return false;

        }

        // return all files for the zone
        private IEnumerable<IFileMetaData> GetContent(string contentFolder, Func<ZoneFileMetaData, bool> filter = null)
        {
            IEnumerable<IFileMetaData> fileMetaList = new List<ZoneFileMetaData>();

            if (filter != null)
            {
                IEnumerable<ZoneFileMetaData> metaList = _allZoneContentList.Where(x => x.ContentFolder.ToLower() == contentFolder.ToLower());
                //Get the targeted content
                fileMetaList = metaList.Where(filter);

                //get the default content
                var defaultContent = _defaultZoneContentList.Where(x => x.ContentFolder.ToLower() == contentFolder.ToLower());
                
                //Here we add the default content to the targeted content.
                fileMetaList = fileMetaList.Concat(defaultContent);
            }

            if (fileMetaList.ToList().Count == 0)
            {
                return _defaultZoneContentList.Where(x => x.ContentFolder.ToLower() == contentFolder.ToLower());
            }

           // _filteredContentList = metaList;
            return fileMetaList;
        }

        public IFileMetaData MoveNext()
        {
            var filteredContentList = _filteredContentList.ToList();

            if (_index >= filteredContentList.Count)
                _index = 0;

            if (filteredContentList.Count == 0)
                return null;

            var content = filteredContentList[_index++];
            return content;
        }

    }
}
