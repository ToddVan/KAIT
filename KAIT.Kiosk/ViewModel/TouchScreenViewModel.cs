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

using GalaSoft.MvvmLight;
using KAIT.Common;
using KAIT.Common.Interfaces;
using System;

namespace KAIT.Kiosk.ViewModel
{
    public class TouchScreenViewModel : ViewModelBase
    {
        IKioskInteractionService _kioskInteractionService;

        public event EventHandler Activated;
        public event EventHandler Deactivated;

        string _itemState = "None";
        public string ItemState
        {
            get { return _itemState; }
            set
            {
                if (_itemState == value)
                    return;
                _itemState = value;
                RaisePropertyChanged("ItemState");
            }
        }

        public TouchScreenViewModel(IKioskInteractionService kioskInteractionService)
        {
            _kioskInteractionService = kioskInteractionService;
            _kioskInteractionService.KioskStateChanged += _kioskInteractionService_KioskStateChanged;

        }


        void _kioskInteractionService_KioskStateChanged(object sender, KioskStateEventArgs e)
        {
            if (e.KioskState == "Touch")
                OnActivated();
            else
                OnDeactivated();
        }

        private void OnDeactivated()
        {
            var handler = this.Deactivated;
            if (handler != null)
                handler(this, null);
        }

        private void OnActivated()
        {
            var handler = this.Activated;
            if (handler != null)
                handler(this, null);
        }


    }
}
