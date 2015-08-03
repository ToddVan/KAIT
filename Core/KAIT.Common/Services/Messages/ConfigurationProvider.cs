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
