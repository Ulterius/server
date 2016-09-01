#region

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using UlteriusServer.WebSocketAPI;

#endregion

namespace UlteriusServer.Utilities.Security
{
    public class Rsa
    {
        public SecureString PrivateKey;
        public SecureString PublicKey;

        public static AsymmetricCipherKeyPair GetKeyPair()
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var secureRandom = new SecureRandom(randomGenerator);
            var keyGenerationParameters = new KeyGenerationParameters(secureRandom, 2048);

            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            return keyPairGenerator.GenerateKeyPair();
        }

        public static SecureString StringToSecureString(string input)
        {
            var output = new SecureString();
            var l = input.Length;
            var s = input.ToCharArray(0, l);
            foreach (var c in s)
            {
                output.AppendChar(c);
            }
            return output;
        }


        public static string SecureStringToString(SecureString value)
        {
            var valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }


        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static SecureString Decryption(SecureString clientPrivateKey, string encryptedData)
        {
            var data = Convert.FromBase64String(SecureStringToString(clientPrivateKey));
            var decodedPrivateKey = Encoding.UTF8.GetString(data);
            var bytesToDecrypt = Convert.FromBase64String(encryptedData);
            var decryptEngine = new Pkcs1Encoding(new RsaEngine());

            using (var txtreader = new StringReader(decodedPrivateKey))
            {
                var keyPair = (AsymmetricCipherKeyPair) new PemReader(txtreader).ReadObject();

                decryptEngine.Init(false, keyPair.Private);
            }
            return
                StringToSecureString(
                    Encoding.UTF8.GetString(decryptEngine.ProcessBlock(bytesToDecrypt, 0, bytesToDecrypt.Length)));
        }


        public void GenerateKeyPairs()
        {
            var keyPair = GetKeyPair();
            var publicKey = (RsaKeyParameters) keyPair.Public;
            var privateKey = (RsaKeyParameters) keyPair.Private;


            var publicWriter = new StringWriter();
            var pemWriter = new PemWriter(publicWriter);
            pemWriter.WriteObject(publicKey);
            pemWriter.Writer.Flush();
            PublicKey = StringToSecureString(Base64Encode(publicWriter.ToString()));
            publicWriter.Close();

            var privateWriter = new StringWriter();
            var pemWriterP = new PemWriter(privateWriter);
            pemWriterP.WriteObject(privateKey);
            pemWriterP.Writer.Flush();
            PrivateKey = StringToSecureString(Base64Encode(privateWriter.ToString()));
            privateWriter.Close();
        }
    }
}