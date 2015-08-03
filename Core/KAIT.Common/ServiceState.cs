using System;

namespace KAIT.Common
{
    public enum ServiceStates
    {
        Open,
        Closed,
        Error,
        NotReady
        
    }
    public class ServiceStateEventArgs : EventArgs {
        public ServiceStates State { get;  set; }
    }
}
