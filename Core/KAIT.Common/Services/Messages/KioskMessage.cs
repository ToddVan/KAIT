using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inception.Common.Services.Messages
{
    public class KioskMessage<T>
    {
        public string DataType { get; set; }
        public T Data { get; set; }     // This should really be named 'result' if we wanted to kind of follow convention

        public KioskMessage(T data)
        {
            this.DataType = typeof(T).ToString();
            this.Data = data;
        }
    }
}
