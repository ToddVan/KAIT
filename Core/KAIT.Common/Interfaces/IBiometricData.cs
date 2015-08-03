using Newtonsoft.Json;

namespace KAIT.Common.Services.Messages
{
    public enum BiometricTrackingState
    {
        TrackingStarted,
        TrackingLost
    }
    [JsonConverter(typeof(InterfaceToConcreteConverter<IBiometricData, BiometricData>))]
    public interface IBiometricData : IFaceData, IDemographicData
    {
    }
}
