using Newtonsoft.Json;
using System.Drawing;

namespace KAIT.Common.Services.Messages
{
    [JsonConverter(typeof(InterfaceToConcreteConverter<IFaceData, FaceData>))]
    public interface IFaceData : ITrackingData
    {      
        string FaceID { get; set; }
        bool FaceMatch { get; set; }
        float FaceConfidence { get; set; }
        float FaceScore { get; set; }
        float FrontalFaceScore { get; set; }
        float HeadConfidence { get; set; }
        Bitmap FaceImage { get; set; }
    }
}
