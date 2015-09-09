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

using KAIT.Common.Services.Messages;
using System;

namespace KAIT.Common
{
   
    public class KioskStateEventArgs :EventArgs
    {
        public ulong TrackingID { get; set; }
        public string KioskState { get; set; }
        public string CurrentZone { get; set; }
        public ManipulationStates ItemState { get; set; }

        public BiometricData Demographics { get; set; }

        public string ItemSelected { get; set; }

        public ContentAction ContentAction { get; set; }
    }
}
