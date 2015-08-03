using Microsoft.Kinect;
using System;
using KAIT.Common;
using KAIT.Common.Interfaces;

namespace KAIT.Kiosk.Services
{
    public class TestInteractionService : IKioskInteractionService
    {
        public event EventHandler<KioskStateEventArgs> KioskStateChanged;
        public event EventHandler<KioskStateEventArgs> ZoneChanged;
        public event EventHandler<BodyTrackEventArgs> BodyTrackUpdate;

        public event EventHandler<Body> TrackingUpdate;

        string _state;
        public string KioskState
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state == value)
                    return;
                _state = value;
                RaiseStateChanged();
            }
        }

        private void RaiseStateChanged()
        {
            var handler = this.KioskStateChanged;
            if (handler != null)
                handler(this, new KioskStateEventArgs() { KioskState = this.KioskState });
        }

        string _zone;
        public string CurrentZone { get; set; }
    }
}
