using System.Security.Cryptography;
using System.Text;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AmiyaDbaasManager.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:Key"] ?? "AmiyaDbaasManagerEncryptionKey2026!"; // Should be 32 chars
        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        byte[] iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(iv, 0, iv.Length); 

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);

            using Aes aes = Aes.Create();
            aes.Key = _key;

            int ivLength = aes.BlockSize / 8;
            if (fullCipher.Length < ivLength)
                throw new InvalidOperationException("Invalid cipher text");

            byte[] iv = new byte[ivLength];
            byte[] cipher = new byte[fullCipher.Length - ivLength];

            Array.Copy(fullCipher, 0, iv, 0, ivLength);
            Array.Copy(fullCipher, ivLength, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {

            return cipherText;
        }
    }
}
