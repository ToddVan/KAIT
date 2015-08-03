using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAIT.Common.Services.Messages
{
    public interface IConfigurationProvider
    {
        IConfigSettings Load();
        string WriteFile(IConfigSettings configSettings);

        event EventHandler<KioskConfigSettingsEventArgs> ConfigurationSettingsChanged;
    }
}
