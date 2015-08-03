using System;
using System.Collections.Generic;
using Microsoft.Kinect;

namespace KAIT.Common.Interfaces
{
    public interface ITrackedBody
    {
        List<ulong> PlayerTrackingIds { get; set; }
        ulong CurrentPlayerTrackingId { get; }
        ulong CorrelationPlayerId { get; }        
        DateTime? TimeWentMissing { get; set; }
        CameraSpacePoint BodyPosition { get; set; }
        bool DetectBodyHijack(CameraSpacePoint newBodyPosition);
        float MinBodyPositionX { get; set; }
        float MaxBodyPositionX { get; set; }
        float MinBodyPositionY { get; set; }
        float MaxBodyPositionY { get; set; }
        float MinBodyPositionZ { get; set; }
        float MaxBodyPositionZ { get; set; }
    }
}
