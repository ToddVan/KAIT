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
using System.Windows.Media.Imaging;

namespace KAIT.Common.Interfaces
{
    
    public interface IItemInteractionService
    {
        WriteableBitmap DepthBitmap { get;  }
        event EventHandler<ServiceStateEventArgs> ServiceStateChanged;
        event EventHandler<KioskStateEventArgs> ItemInteraction;
        void Start();
        void Stop();
        string ServiceState { get; }        
        event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        int ObjectCount { get; }
        ulong ActivePlayerId { get; set; }
        ulong CorrelationPlayerId { get; set; }
    }
}
