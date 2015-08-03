
namespace KAIT.Common.Services.Messages
{
    public class FaceData : IFaceData
    {
        public ulong TrackingId { get; set; }
        public string FaceID { get; set; }

        public bool FaceMatch { get; set; }

        public float FaceConfidence { get; set; }

        public float FaceScore { get; set; }

        public float FrontalFaceScore { get; set; }

        public float HeadConfidence { get; set; }
        
        public System.Drawing.Bitmap FaceImage { get; set; }
       
    }
}
