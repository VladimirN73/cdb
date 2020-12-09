using System;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MsSqlCloneDb.Lib.Common
{
    public interface IEncryptionService
    {
        string Encrypt(string value);
        Result<string> Encrypt(string value, string originValue, bool isMasked);
        string Decrypt(string value);
        string DecryptIgnoreError(string value);
        string DecryptOrMaskIgnoreError(string value, bool shouldMask);
        string DecryptConnectionString(string connectionString);
    }

    /// <summary>
    /// Diese Factory sollte NICHT in Online verwendet werden
    /// Sie existiert für die Stellen Classic oder anderen Tools in denen keine Dependency Injection möglich ist
    /// </summary>
    public static class EncryptionServiceFactory
    {
        public static IEncryptionService Create()
        {
            return new EncryptionService();
        }
    }
    
    internal class EncryptionService : IEncryptionService
    {
        private const char PasswordMaskSymbol = '¥';
        private const string PasswordMask = "¥¥¥¥¥¥¥¥";

        public Result<string> Encrypt(string value, string originValue, bool isMasked)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Result<string>.Success(value);
            }

            var unmasked = value;

            if (isMasked)
            {
                var decryptedMaskResult = DecryptPasswordMask(value, originValue);

                if (decryptedMaskResult.Failed)
                {
                    return decryptedMaskResult;
                }

                unmasked = decryptedMaskResult.Value;
            }

            return Result<string>.Success(Encrypt(unmasked));
        }

        public string Encrypt(string value)
        {
            return AesEncryption.Encrypt(value);
        }

        public string Decrypt(string value)
        {
            return AesEncryption.Decrypt(value);
        }

        public string DecryptIgnoreError(string value)
        {
            return AesEncryption.DecryptIgnoreError(value);
        }

        public string DecryptOrMaskIgnoreError(string value, bool shouldMask)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return shouldMask
                ? EncryptPasswordMask(value)
                : DecryptIgnoreError(value);
        }

        public string DecryptConnectionString(string connectionString)
        {
            // decrypt full string
            var ret = DecryptIgnoreError(connectionString);

            // decrypt password
            var temp = new SqlConnectionStringBuilder(ret);
            temp.Password = DecryptIgnoreError(temp.Password);
            ret = temp.ConnectionString;

            return ret;
        }

        private Result<string> DecryptPasswordMask(string value, string original)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Result<string>.Success(value);
            }

            if (value.Equals(PasswordMask) || value.All(v => v.Equals(PasswordMaskSymbol)))
            {
                return Result<string>.Success(original);
            }

            if (value.Contains(PasswordMaskSymbol))
            {
                return Result<string>.Failure(null, "Passwort_enthaelt_ungueltige_Symbole");
            }

            return Result<string>.Success(value);
        }

        private string EncryptPasswordMask(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return PasswordMask;
        }
    }

    // we still need a static version of the class/methods, due to it is used in generated-codes
    public class AesEncryption
    {
        private static readonly byte[] Key = { 137, 163, 112, 96, 0, 147, 112, 236, 238, 199, 131, 63, 160, 131, 130, 176, 196, 56, 161, 224, 60, 232, 127, 156, 32, 230, 172, 37, 251, 55, 207, 123 };
        private static readonly byte[] Iv = { 194, 138, 125, 174, 80, 140, 210, 170, 17, 21, 108, 194, 155, 230, 211, 30 };

        protected internal static string Encrypt(string value)
        {
            if (value.IsNullOrEmpty()) return null;

            using (var aes = Aes.Create())
            {
                using (var encryptor = aes?.CreateEncryptor(Key, Iv))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (var streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(value);
                            }
                            return Convert.ToBase64String(memoryStream.ToArray());
                        }
                    }
                }
            }
        }

        protected internal static string Decrypt(string value)
        {
            if (value.IsNullOrEmpty()) return null;

            // to avoid unnecessary exceptions in Visual Studio, if the connection string is not encrypted
            if (value.StartsWith("Data Source", true, CultureInfo.InvariantCulture))
            {
                return value;
            }

            using (var aes = Aes.Create())
            {
                using (var decryptor = aes?.CreateDecryptor(Key, Iv))
                {
                    using (var memoryStream = new MemoryStream(Convert.FromBase64String(value)))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (var streamReader = new StreamReader(cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        protected internal static string DecryptIgnoreError(string value)
        {
            var ret = value;
            try
            {
                ret = Decrypt(value);
            }
            catch (Exception)
            {
                // ignore
            }

            return ret;
        }

    }
}