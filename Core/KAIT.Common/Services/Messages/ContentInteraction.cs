using System;

namespace KAIT.Common.Services.Messages
{
    public enum ContentAction
    {
        Enter,
        Exit
    }
    
    public class ContentInteraction
    {       
        public ulong TrackingId { get; set; }

        public ulong CorrelatedTrackingID { get; set; }

        public string KioskState { get; set; }

        public ContentAction Action { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public string DeviceSelection { get; set; }

        public DeviceSelectionState DeviceSelectionState { get; set; }

        public LocationCoordinates Location { get; set; }
        public string KinectDeviceId { get; set; }

        public string InteractionZone { get; set; }

        public ContentInteraction()
        {
            this.TimeStamp = DateTimeOffset.UtcNow;
        }
    }
}
