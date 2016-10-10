namespace Sensus.Service.Tools
{
    public interface IEncryption
    {
        byte[] Encrypt(string value);
        string Decrypt(byte[] value);
    }
}
