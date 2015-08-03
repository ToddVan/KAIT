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
