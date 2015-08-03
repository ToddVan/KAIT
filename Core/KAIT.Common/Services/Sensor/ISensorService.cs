using System;

namespace KAIT.Common.Sensor
{
    public enum SensorStatus
    {
        Closed,
        Ready,
        Error,
        NoSensor
    }

    public class SensorStatusEventArgs : EventArgs {
        public SensorStatus Status { get; set; }
    }

    public interface ISensorService<T>
    {
        event EventHandler<SensorStatusEventArgs> StatusChanged;

        T Sensor { get;  }
        void Open();
        void Close();
    }
}
