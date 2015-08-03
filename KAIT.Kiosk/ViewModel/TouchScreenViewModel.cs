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
