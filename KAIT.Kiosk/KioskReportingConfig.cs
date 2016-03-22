using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAIT.Kiosk
{
    public class KioskReportingConfig
    {
        public string location { get; set; }
        public string sublocation { get; set; }
        public string kinectID { get; set; }
        public string applicationID { get; set; }
        public float applicationVersion { get; set; }
        public string[] applicationProducts { get; set; }
    }

}

