using Inception.Common.Services.Messages;
using System;

namespace Inception.Common.Services.Communication
{
    public interface ILocalCommunicationClient
    {
        event EventHandler<ITrackingData> TrackingDataReceived;
        event EventHandler<IBiometricData> BiometricsReceived;
        event EventHandler<IFaceData> FaceReceived;
        event EventHandler<IDemographicData> DemographicsReceived;
        void Listen(string source);
    }
}
