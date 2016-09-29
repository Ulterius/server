#region

using System;
using System.IO;
using System.Security.Cryptography;
using UlteriusServer.Utilities.Security.Streams;

#endregion

namespace UlteriusServer.Utilities.Security
{
    public class UlteriusAes
    {
        public enum EncryptionType
        {
            OFB,
            CBC
        }

        public static void EncryptFile(byte[] passwordBytes, string inputFile, string outputFile)
        {
            var cryptFile = outputFile;

            using (var fsCrypt = new FileStream(cryptFile, FileMode.Create))
            using (var aes = new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Key = passwordBytes,
                IV = passwordBytes,
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC
            })
            using (var cs = new CryptoStream(fsCrypt,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write))
            using (var fsIn = new FileStream(inputFile, FileMode.Open))
            {
                int data;
                while ((data = fsIn.ReadByte()) != -1)
                    cs.WriteByte((byte) data);
            }
        }

        public static byte[] DecryptFile(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes;

            using (var ms = new MemoryStream())
            {
                using (var aes = new RijndaelManaged())
                {
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (
                        var cs = new CryptoStream(ms, aes.CreateDecryptor(passwordBytes, passwordBytes),
                            CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }




        public static byte[] EncryptFrame(byte[] bytesToBeEncrypted, byte[] keyBytes, byte[] ivBytes)
        {
            byte[] encrypted;
            // Create a RijndaelManaged object  
            // with the specified key and IV.  
            using (var aes = new RijndaelManaged())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                using (var vectorStream = new MemoryStream(bytesToBeEncrypted))
                {
                    using (var ofbStream = new OfbStream(vectorStream, aes, CryptoStreamMode.Read))
                    {
                        using (var cipherStream = new MemoryStream())
                        {
                            ofbStream.CopyTo(cipherStream);
                            encrypted = cipherStream.ToArray();
                        }
                    }
                }
            }
            // Return the encrypted bytes from the memory stream.  
            return encrypted;
        }

        public static byte[] Encrypt(string plainText, byte[] key, byte[] iv)
        {
            // Check arguments.  
            if (plainText == null || plainText.Length <= 0)
            {
                throw new ArgumentNullException("plainText");
            }
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            byte[] encrypted;
            // Create a RijndaelManaged object  
            // with the specified key and IV.  
            using (var rijAlg = new RijndaelManaged())
            {
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.Key = key;
                rijAlg.IV = iv;

                // Create a decrytor to perform the stream transform.  
                var encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.  
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.  
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            // Return the encrypted bytes from the memory stream.  
            return encrypted;
        }

        public static string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments.  
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException("cipherText");
            }
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }

            // Declare the string used to hold  
            // the decrypted text.  
            string plaintext = null;

            // Create an RijndaelManaged object  
            // with the specified key and IV.  
            using (var rijAlg = new RijndaelManaged())
            {
                //Settings  
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.Key = key;
                rijAlg.IV = iv;

                // Create a decrytor to perform the stream transform.  
                var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                try
                {
                    // Create the streams used for decryption.  
                    using (var msDecrypt = new MemoryStream(cipherText))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                // Read the decrypted bytes from the decrypting stream  
                                // and place them in a string.  
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return plaintext;
        }
    }
}