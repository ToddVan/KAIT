using GalaSoft.MvvmLight;
using KinectKiosk.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inception.Common.Interfaces;

namespace KinectKiosk.ViewModel
{
    public class ItemInteractionViewModel :ViewModelBase
    {
        string _interactionState;
        public string InteractionState
        {
            get { return _interactionState; }
            set
            {
                if (_interactionState == value)
                    return;
                _interactionState = value;
                RaisePropertyChanged("InteractionState");
            }
        }
    }
}
