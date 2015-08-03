
namespace KAIT.Common.Services.Messages
{
    public class BiometricMessage<TData> : IBiometricMessage<TData> 
    {
        public string DataType { get; set; }
        public TData Data { get; set; }     // This should really be named 'result' to follow convention

        public BiometricMessage(TData data)
        {
            this.DataType = typeof(TData).ToString();
            this.Data = data;
        }
    }
}
