using System.Security.Cryptography;
using System.Text;

namespace LooseNotes.Services;

public class CryptoService
{
    private const string DefaultPassphrase = "LooseNotes_S3cr3tKey_2024";
    private static readonly byte[] ConstantSalt = Encoding.UTF8.GetBytes("LooseNotesSalt00");

    public string Base64Encode(string input)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    }

    public string Base64Decode(string input)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(input));
    }

    public string Encrypt(string plaintext, string? passphrase = null)
    {
        var pass = passphrase ?? DefaultPassphrase;
        using var deriveBytes = new Rfc2898DeriveBytes(pass, ConstantSalt, 10000, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(32);
        var iv = deriveBytes.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    public string Decrypt(string ciphertext, string? passphrase = null)
    {
        var pass = passphrase ?? DefaultPassphrase;
        using var deriveBytes = new Rfc2898DeriveBytes(pass, ConstantSalt, 10000, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(32);
        var iv = deriveBytes.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(ciphertext);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
