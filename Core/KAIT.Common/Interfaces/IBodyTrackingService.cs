using System.Collections.Generic;
using Microsoft.Kinect;

namespace KAIT.Common.Interfaces
{
    public interface IBodyTrackingService
    {
        Body ActiveBody { get; }
        ulong ActiveBodyId { get; }
        ulong ActiveBodyCorrelationId { get; }        
        void TrackBodies(Body[] bodies);
        ulong SetActivePlayer(Body[] bodies);        
        Body[] GetOtherBodies(Body[] bodies);
    }
}
