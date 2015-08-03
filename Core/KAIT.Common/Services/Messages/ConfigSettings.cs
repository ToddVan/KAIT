using System.Collections.Generic;

namespace KAIT.Common.Services.Messages
{
    public class ConfigSettings : IConfigSettings
    {
        public string RootContentDirectory { get; set; }
        public bool ShowJpgFiles { get; set; }
        public bool ShowPngFiles { get; set; }
        public bool ShowGifFiles { get; set; }
        public bool ShowWmvFiles { get; set; }
        public bool ShowMp4Files { get; set; }
        public List<ZoneDefinition> ZoneDefinitions { get; set; }
        public bool EnableTouchScreen { get; set; }
        public bool EnableDiagnostics { get; set; }

    }

    public class ZoneDefinition
    {
        public string Name { get; set; }
        public double MaximumRange { get; set; }
    }

}
