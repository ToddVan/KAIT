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

using System;

namespace KAIT.Common.Services.Messages
{
    public enum ContentAction
    {
        Enter,
        Exit
    }
    
    public class ContentInteraction
    {       
        public ulong TrackingId { get; set; }

        public ulong CorrelatedTrackingID { get; set; }

        public string KioskState { get; set; }

        public ContentAction Action { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public string DeviceSelection { get; set; }

        public DeviceSelectionState DeviceSelectionState { get; set; }

        public LocationCoordinates Location { get; set; }
        public string KinectDeviceId { get; set; }

        public string InteractionZone { get; set; }

        public ContentInteraction()
        {
            this.TimeStamp = DateTimeOffset.UtcNow;
        }
    }
}
