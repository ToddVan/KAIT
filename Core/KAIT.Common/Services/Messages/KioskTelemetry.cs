using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inception.Common.Services.Messages
{
    public enum InteractionType
    {
        Enter,
        Exit
    }
    public class KioskInteraction
    {
        public ulong TrackingId { get; set; }
        public string  KioskState { get; set; }
        public InteractionType Action { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTimeOffset TimeStamp { get; set; }

        public string ItemSelection { get; set; }

        public ManipulationStates ItemState { get; set; }

        public KioskInteraction()
        {
            this.TimeStamp = DateTimeOffset.UtcNow;
        }
    }
}
