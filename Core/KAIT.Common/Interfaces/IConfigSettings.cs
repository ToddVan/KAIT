using System.Collections.Generic;

namespace KAIT.Common.Services.Messages
{
    public interface IConfigSettings
    {
        string RootContentDirectory { get; }
        bool ShowJpgFiles { get; }
        bool ShowPngFiles { get; }
        bool ShowGifFiles { get; }
        bool ShowWmvFiles { get; }
        bool ShowMp4Files { get; }
        List<ZoneDefinition> ZoneDefinitions { get; }
        bool EnableTouchScreen { get; }
        bool EnableDiagnostics { get; } 
    }
}
