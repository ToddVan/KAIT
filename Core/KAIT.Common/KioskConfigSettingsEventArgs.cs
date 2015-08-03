using KAIT.Common.Services.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAIT.Common
{
    public class KioskConfigSettingsEventArgs : EventArgs
    {
        public IConfigSettings ConfigSettings { get; set; }
    }
}
