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
        => await WriteAsync(Encrypt(data, publicKey));


    private byte[] Encrypt(string? data, string key)
    {
        using RSACryptoServiceProvider rsa = new();
        rsa.FromXmlString(key);
        var dataBytes = Encoding.UTF8.GetBytes(data!);
        return rsa.Encrypt(dataBytes, rsaEncryptionPadding).ToArray();
    }
}