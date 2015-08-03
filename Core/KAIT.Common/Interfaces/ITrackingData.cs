using Newtonsoft.Json;

namespace KAIT.Common.Services.Messages
{
    [JsonConverter(typeof(InterfaceToConcreteConverter<ITrackingData,TrackingData>))]
    public interface ITrackingData
    {
        ulong TrackingId { get; set; }
    }
}
