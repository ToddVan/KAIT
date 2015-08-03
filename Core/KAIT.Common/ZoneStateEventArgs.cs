using Inception.Common.Services.Messages;

namespace Inception.Common
{
    public class ZoneStateEventArgs
    {
        public ulong TrackingID { get; set; }
        public string KioskState { get; set; }
        public string CurrentZone { get; set; }
        public ManipulationStates ItemState { get; set; }

        public BiometricData Demographics { get; set; }

        public string ItemSelected { get; set; }

    }
}
