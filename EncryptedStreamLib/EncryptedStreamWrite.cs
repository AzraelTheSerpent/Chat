using System.Security.Cryptography;
using System.Text;

namespace EncryptedStreamLib;

public partial class EncryptedStream
{
    public async Task WriteAsync(byte[] data)
    {
        var dataLength = data.Length;
        var lengthBytes = BitConverter.GetBytes(dataLength);
        
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        
        await stream.WriteAsync(lengthBytes.AsMemory(0, 4));
        await stream.WriteAsync(data.AsMemory(0, 0 + dataLength));
    }
    
    public async Task EncryptedWriteAsync(string data, string publicKey)
    {
        var dataBytes = Encrypt(data, publicKey);
        var dataLength = dataBytes.Length;
        var lengthBytes = BitConverter.GetBytes(dataLength);
        
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        
        await stream.WriteAsync(lengthBytes.AsMemory(0, 4));
        await stream.WriteAsync(dataBytes.AsMemory(0, 0 + dataLength));
    }

    private byte[] Encrypt(string? data, string key)
    {
        using RSACryptoServiceProvider rsa = new();
        rsa.FromXmlString(key);
        var dataBytes = Encoding.UTF8.GetBytes(data!);
        return rsa.Encrypt(dataBytes, rsaEncryptionPadding).ToArray();
    }
}