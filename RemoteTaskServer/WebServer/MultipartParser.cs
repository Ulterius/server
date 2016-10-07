#region

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace UlteriusServer.WebServer
{
    /// <summary>
    /// MultipartParser http://multipartparser.codeplex.com
    /// Reads a multipart form data stream and returns the filename, content type and contents as a stream.
    /// 2009 Anthony Super http://antscode.blogspot.com
    /// </summary>
    namespace RemoteTaskServer.WebServer
    {
        public class MultipartParser
        {
            public MultipartParser(Stream stream)
            {
                Parse(stream, Encoding.UTF8);
            }

            public MultipartParser(Stream stream, Encoding encoding)
            {
                Parse(stream, encoding);
            }

            public bool Success { get; private set; }

            public string ContentType { get; private set; }


            public string Filename { get; private set; }

            public byte[] FileContents { get; private set; }

            private void Parse(Stream stream, Encoding encoding)
            {
                Success = false;

                // Read the stream into a byte array
                var data = ToByteArray(stream);

                // Copy to a string for header parsing
                var content = encoding.GetString(data);
                // The first line should contain the delimiter
                var delimiterEndIndex = content.IndexOf("\r\n", StringComparison.Ordinal);


                if (delimiterEndIndex <= -1) return;
                var delimiter = content.Substring(0, content.IndexOf("\r\n", StringComparison.Ordinal));


                // Look for Content-Type
                var re = new Regex(@"(?<=Content\-Type:)(.*?)(?=\r\n\r\n)");
                var contentTypeMatch = re.Match(content);

                // Look for filename
                re = new Regex(@"(?<=filename\=\"")(.*?)(?=\"")");
                var filenameMatch = re.Match(content);


                // Did we find the required values?
                if (!contentTypeMatch.Success || !filenameMatch.Success) return;
                // Set properties
                ContentType = contentTypeMatch.Value.Trim();
                Filename = filenameMatch.Value.Trim();


                // Get the start & end indexes of the file contents
                var startIndex = contentTypeMatch.Index + contentTypeMatch.Length + "\r\n\r\n".Length;

                var delimiterBytes = encoding.GetBytes("\r\n" + delimiter);
                var endIndex = IndexOf(data, delimiterBytes, startIndex);

                var contentLength = endIndex - startIndex;

                // Extract the file contents from the byte array
                var fileData = new byte[contentLength];

                Buffer.BlockCopy(data, startIndex, fileData, 0, contentLength);

                FileContents = fileData;
                Success = true;
            }

            private int IndexOf(byte[] searchWithin, byte[] serachFor, int startIndex)
            {
                var index = 0;
                var startPos = Array.IndexOf(searchWithin, serachFor[0], startIndex);

                if (startPos != -1)
                {
                    while (startPos + index < searchWithin.Length)
                    {
                        if (searchWithin[startPos + index] == serachFor[index])
                        {
                            index++;
                            if (index == serachFor.Length)
                            {
                                return startPos;
                            }
                        }
                        else
                        {
                            startPos = Array.IndexOf(searchWithin, serachFor[0], startPos + index);
                            if (startPos == -1)
                            {
                                return -1;
                            }
                            index = 0;
                        }
                    }
                }

                return -1;
            }

            private byte[] ToByteArray(Stream stream)
            {
                var buffer = new byte[32768];
                using (var ms = new MemoryStream())
                {
                    while (true)
                    {
                        var read = stream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                            return ms.ToArray();
                        ms.Write(buffer, 0, read);
                    }
                }
            }
        }
    }
}