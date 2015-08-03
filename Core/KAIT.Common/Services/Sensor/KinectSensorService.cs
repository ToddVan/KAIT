using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inception.Common.Sensor
{
    public class KinectSensorService : ISensorService<KinectSensor>
    {

        public event EventHandler<SensorStatusEventArgs> StatusChanged;
        public KinectSensor Sensor { get; private set; }

        public KinectSensorService()
        {
            this.Sensor = KinectSensor.GetDefault();
            this.Sensor.IsAvailableChanged += Sensor_IsAvailableChanged;
        }

        void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            var status = (e.IsAvailable) ? SensorStatus.Ready : SensorStatus.Closed;
            RaiseStatusChanged(status);
        }

        private void RaiseStatusChanged(SensorStatus e)
        {
            var handler = this.StatusChanged;
            if (handler != null)
            {
                handler(this, new SensorStatusEventArgs() { Status = e });
            }
        }


        public void Open()
        {
            if (this.Sensor == null)
                RaiseStatusChanged(SensorStatus.Error);
            this.Sensor.Open();

        }

        public void Close()
        {

            if (this.Sensor != null)
            {
                if (this.Sensor.IsOpen)
                    this.Sensor.Close();
                this.Sensor = null;
            }
        }

    }
}
