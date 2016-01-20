#region

using System;
using System.Text;
using MiscUtil.Conversion;

#endregion

namespace UlteriusServer.WebSocketAPI
{
    internal class WebSocketFunctions
    {
        /// <summary>
        ///     Method to decode the message after its sent to the server
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string DecodeMessage(byte[] buffer, int length)
        {
            var b = buffer[1];
            var dataLength = 0;
            var totalLength = 0;
            var keyIndex = 0;

            if (b - 128 <= 125)
            {
                dataLength = b - 128;
                keyIndex = 2;
                totalLength = dataLength + 6;
            }

            if (b - 128 == 126)
            {
                dataLength = BitConverter.ToInt16(new[] {buffer[3], buffer[2]}, 0);
                keyIndex = 4;
                totalLength = dataLength + 8;
            }

            if (b - 128 == 127)
            {
                dataLength =
                    (int)
                        BitConverter.ToInt64(
                            new[]
                            {buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2]}, 0);
                keyIndex = 10;
                totalLength = dataLength + 14;
            }

            if (totalLength > length)
                throw new Exception("The buffer length is small than the data length");

            byte[] key = {buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3]};

            var dataIndex = keyIndex + 4;
            var count = 0;
            for (var i = dataIndex; i < totalLength; i++)
            {
                buffer[i] = (byte) (buffer[i] ^ key[count%4]);
                count++;
            }

            return Encoding.ASCII.GetString(buffer, dataIndex, dataLength);
        }

        /// <summary>
        ///     Method to encode the message before sending to the Client.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static byte[] EncodeMessageToSend(string message)
        {
            byte[] response;
            var bytesRaw = Encoding.UTF8.GetBytes(message);
            var frame = new byte[10];

            var indexStartRawData = -1;
            var length = bytesRaw.Length;

            frame[0] = 129;
            if (length <= 125)
            {
                frame[1] = (byte) length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = 126;
                frame[2] = (byte) ((length >> 8) & 255);
                frame[3] = (byte) (length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = 127;
                var bytes = EndianBitConverter.Big.GetBytes((ulong) length);
                Buffer.BlockCopy(bytes, 0, frame, 2, 8);

                indexStartRawData = 10;
            }

            response = new byte[indexStartRawData + length];

            int i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }

            return response;
        }
    }
}