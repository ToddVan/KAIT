
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

namespace KAIT.Common.Interfaces
{    
    /// <summary>
    /// Notifies client app of changes in customer state
    /// </summary>
    public interface IKioskInteractionService
    {
        event EventHandler<KioskStateEventArgs> KioskStateChanged;
        event EventHandler<BodyTrackEventArgs> BodyTrackUpdate;


        //event EventHandler<Body> TrackingUpdate;

        /// <summary>
        /// Current state of kiosk with 0 representing default state
        /// </summary>
        string KioskState { get; }

        string CurrentZone { get; }
    }
}
