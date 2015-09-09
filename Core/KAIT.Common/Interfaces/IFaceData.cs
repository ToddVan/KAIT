//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

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
