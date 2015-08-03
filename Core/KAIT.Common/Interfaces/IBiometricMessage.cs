
namespace KAIT.Common.Services.Messages
{
    public interface IBiometricMessage<T>  
    {
        string DataType { get; set; }
        T Data { get; set; }     
    }
}
