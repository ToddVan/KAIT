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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Configuration;

namespace KAIT.Common.Services.Messages
{

    public class ConfigurationProvider : IConfigurationProvider
    {
        const string FILENAME = @"config.json";

        public event EventHandler<KioskConfigSettingsEventArgs> ConfigurationSettingsChanged;

        public IConfigSettings Load()
        {
            string configRootDirectory = ConfigurationManager.AppSettings["ConfigRootDirectory"];
            string filename = ConfigurationManager.AppSettings["ConfigFilename"];

            if (string.IsNullOrEmpty(configRootDirectory))
                configRootDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            if (string.IsNullOrEmpty(filename))
                filename = FILENAME;

            // If file not found, return some defaults
            if (!string.IsNullOrWhiteSpace(filename) && !File.Exists(filename))
            {
                // put in some defaults...
                ConfigSettings cs = new ConfigSettings();
                return cs;
            }

            using (StreamReader r = new StreamReader(filename))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<ConfigSettings>(json);
            }
        }

        public string WriteFile(IConfigSettings configSettings)
        {
            string configRootDirectory = ConfigurationManager.AppSettings["ConfigRootDirectory"];
            string filename = ConfigurationManager.AppSettings["ConfigFilename"];

            if (string.IsNullOrEmpty(configRootDirectory))
                configRootDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            if (string.IsNullOrEmpty(filename))
                filename = FILENAME;


            try
            {
                var json = JsonConvert.SerializeObject(configSettings, Formatting.Indented);
                File.WriteAllText(filename, json);
            }
            catch (Exception ex)
            {
                return string.Format("Error writing configuration file: {0}", ex.Message);
            }


            var handler = this.ConfigurationSettingsChanged;
            if (handler != null)
            {
                var kioskSettingsChangedEvent = new KioskConfigSettingsEventArgs();
                kioskSettingsChangedEvent.ConfigSettings = configSettings;
                handler(this, kioskSettingsChangedEvent);
            }

            return "File written successfully.";
        }

    }
}
