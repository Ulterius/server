#region

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UlteriusServer.Utilities.Security.Streams;

#endregion

namespace UlteriusServer.Utilities.Security
{
    public class UlteriusAes
    {
        public enum EncryptionType
        {
            OFB,
            CBC,
            NAN
        }


        private const int PAGE_EXECUTE_READWRITE = 0x40;

        private byte[] _codeX64 =
        {
            0x53, /* push rbx */
            0x48, 0xc7, 0xc0, 0x01, 0x00, 0x00, 0x00, /* mov rax, 0x1 */
            0x0f, 0xa2, /* cpuid */
            0x41, 0x89, 0x00, /* mov [r8], eax */
            0x41, 0x89, 0x50, 0x04, /* mov [r8+0x4], ebx !!! changed */
            0x41, 0x89, 0x50, 0x08, /* mov [r8+0x8], ecx !!! added */
            0x41, 0x89, 0x50, 0x0C, /* mov [r8+0xC], edx !!! added*/
            0x5b, /* pop rbx */
            0xc3 /* ret */
        };

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
                    cs.WriteByte((byte)data);
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


        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            byte[] encryptedData = null;
            using (var aesProvider = new AesCryptoServiceProvider())
            {
                aesProvider.BlockSize = 128;
                aesProvider.Mode = CipherMode.CBC;
                aesProvider.Padding = PaddingMode.PKCS7;
                aesProvider.Key = key;
                aesProvider.IV = iv;
                using (var encryptor = aesProvider.CreateEncryptor(aesProvider.Key, aesProvider.IV))
                {
                    encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
            return encryptedData;
        }


        public static string Decrypt(byte[] encryptedData, byte[] key, byte[] iv)
        {
            byte[] decryptedData = null;
            using (var aesProvider = new AesCryptoServiceProvider())
            {
                aesProvider.BlockSize = 128;
                aesProvider.Mode = CipherMode.CBC;
                aesProvider.Padding = PaddingMode.PKCS7;
                aesProvider.Key = key;
                aesProvider.IV = iv;
                using (var decryptor = aesProvider.CreateDecryptor(aesProvider.Key, aesProvider.IV))
                {
                    decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                }
            }
            return decryptedData != null ? Encoding.UTF8.GetString(decryptedData) : null;
        }


        private static bool IsAesniPresent()
        {
            var sn = new byte[16]; // !!! Here were 8 bytes

            if (!ExecuteCode(ref sn))
                return false;

            var ecx = BitConverter.ToUInt32(sn, 8);
            return (ecx & (1 << 25)) != 0;
        }


        private static bool ExecuteCode(ref byte[] result)
        {
            int num;

            /* The opcodes below implement a C function with the signature:
             * __stdcall CpuIdWindowProc(hWnd, Msg, wParam, lParam);
             * with wParam interpreted as a pointer pointing to an 8 byte unsigned character buffer.
             * */

            byte[] codeX86 =
            {
                0x55, /* push ebp */
                0x89, 0xe5, /* mov  ebp, esp */
                0x57, /* push edi */
                0x8b, 0x7d, 0x10, /* mov  edi, [ebp+0x10] */
                0x6a, 0x01, /* push 0x1 */
                0x58, /* pop  eax */
                0x53, /* push ebx */
                0x0f, 0xa2, /* cpuid    */
                0x89, 0x07, /* mov  [edi], eax */
                0x89, 0x57, 0x04, /* mov  [edi+0x4], edx */
                0x5b, /* pop  ebx */
                0x5f, /* pop  edi */
                0x89, 0xec, /* mov  esp, ebp */
                0x5d, /* pop  ebp */
                0xc2, 0x10, 0x00 /* ret  0x10 */
            };
            byte[] codeX64 =
            {
                0x53, /* push rbx */
                0x48, 0xc7, 0xc0, 0x01, 0x00, 0x00, 0x00, /* mov rax, 0x1 */
                0x0f, 0xa2, /* cpuid */
                0x41, 0x89, 0x00, /* mov [r8], eax */
                0x41, 0x89, 0x50, 0x04, /* mov [r8+0x4], ebx !!! changed */
                0x41, 0x89, 0x50, 0x08, /* mov [r8+0x8], ecx !!! added */
                0x41, 0x89, 0x50, 0x0C, /* mov [r8+0xC], edx !!! added*/
                0x5b, /* pop rbx */
                0xc3 /* ret */
            };

            var code = IsX64Process() ? codeX64 : codeX86;
            var ptr = new IntPtr(code.Length);

            if (!VirtualProtect(code, ptr, PAGE_EXECUTE_READWRITE, out num))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            ptr = new IntPtr(result.Length);

            return CallWindowProcW(code, IntPtr.Zero, 0, result, ptr) != IntPtr.Zero;
        }

        private static bool IsX64Process()
        {
            return IntPtr.Size == 8;
        }

        [DllImport("user32", EntryPoint = "CallWindowProcW", CharSet = CharSet.Unicode, SetLastError = true,
            ExactSpelling = true)]
        private static extern IntPtr CallWindowProcW([In] byte[] bytes, IntPtr hWnd, int msg, [In, Out] byte[] wParam,
            IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool VirtualProtect([In] byte[] bytes, IntPtr size, int newProtect, out int oldProtect);
    }
}