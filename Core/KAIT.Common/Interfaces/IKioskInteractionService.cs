using System;

namespace KAIT.Common.Interfaces
{    
    /// <summary>
    /// Notifies client app of changes in customer state
    /// </summary>
    public interface IKioskInteractionService
    {
        event EventHandler<KioskStateEventArgs> KioskStateChanged;
        event EventHandler<BodyTrackEventArgs> BodyTrackUpdate;

        //event EventHandler<Body> TrackingUpdate;

        /// <summary>
        /// Current state of kiosk with 0 representing default state
        /// </summary>
        string KioskState { get; }

        string CurrentZone { get; }
    }
}
