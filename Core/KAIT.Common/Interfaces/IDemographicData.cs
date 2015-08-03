using Newtonsoft.Json;

namespace KAIT.Common.Services.Messages
{
    [JsonConverter(typeof(InterfaceToConcreteConverter<IDemographicData, DemographicData>))]
    public interface IDemographicData : ITrackingData
    {       
        Gender Gender { get; set; }
        int Age { get; set; }
        double GenderConfidence { get; set; }
    }
}
