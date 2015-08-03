using KAIT.Common.Services.Messages;
using System;

namespace KAIT.Common
{
   
    public class KioskStateEventArgs :EventArgs
    {
        public ulong TrackingID { get; set; }
        public string KioskState { get; set; }
        public string CurrentZone { get; set; }
        public ManipulationStates ItemState { get; set; }

        public BiometricData Demographics { get; set; }

        public string ItemSelected { get; set; }

        public ContentAction ContentAction { get; set; }
    }
}
