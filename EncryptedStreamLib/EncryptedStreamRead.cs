using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace EncryptedStreamLib;

public partial class EncryptedStream(NetworkStream stream, RSAEncryptionPadding rsaEncryptionPadding) : IDisposable
{
    public void Dispose() => stream.Dispose();

    public async Task<byte[]> ReadAsync()
    {
        var lengthBuffer = new byte[4];
        await stream.ReadExactlyAsync(lengthBuffer, 0, 4);

        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBuffer);

        var messageLength = BitConverter.ToInt32(lengthBuffer, 0);

        var responseData = new byte[messageLength];
        var bytesRead = 0;
        while (bytesRead < messageLength)
            bytesRead += await stream.ReadAsync(responseData.AsMemory(
                bytesRead,
                messageLength - bytesRead)
            );

        return responseData;
    }

    public async Task<string> DecryptedReadAsync(string privateKey)
        => Decrypt(await ReadAsync(), privateKey);


    private string Decrypt(byte[] data, string key)
    {
        using RSACryptoServiceProvider rsa = new();
        rsa.FromXmlString(key);
        var decryptedData = rsa.Decrypt(data, rsaEncryptionPadding);
        return Encoding.UTF8.GetString(decryptedData);
    }
}