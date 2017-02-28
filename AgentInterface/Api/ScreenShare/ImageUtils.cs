using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AgentInterface.Api.ScreenShare
{
    class ImageUtils

    {
        // Stores known information
        private Hashtable m_knownColors = new Hashtable((int)Math.Pow(2, 20), 1.0f);

        public Bitmap ConvertTo8bppFormat(Bitmap bmpSource)
        {
            int imageWidth = bmpSource.Width;
            int imageHeight = bmpSource.Height;

            Bitmap bmpDest = null;
            BitmapData bmpDataDest = null;
            BitmapData bmpDataSource = null;

            try
            {

                // Create new image with 8BPP format
                bmpDest = new Bitmap(
                    imageWidth,
                    imageHeight,
                    PixelFormat.Format8bppIndexed
                    );

                // Lock bitmap in memory
                bmpDataDest = bmpDest.LockBits(
                    new Rectangle(0, 0, imageWidth, imageHeight),
                    ImageLockMode.ReadWrite,
                    bmpDest.PixelFormat
                    );

                bmpDataSource = bmpSource.LockBits(
                    new Rectangle(0, 0, imageWidth, imageHeight),
                    ImageLockMode.ReadOnly,
                    bmpSource.PixelFormat
                );

                int pixelSize = GetPixelInfoSize(bmpDataSource.PixelFormat);
                byte[] buffer = new byte[imageWidth * imageHeight * pixelSize];
                byte[] destBuffer = new byte[imageWidth * imageHeight];

                // Read all data to buffer
                ReadBmpData(bmpDataSource, buffer, pixelSize, imageWidth, imageHeight);

                // Get color indexes
                MatchColors(buffer, destBuffer, pixelSize, bmpDest.Palette);

                // Copy all colors to destination bitmaps
                WriteBmpData(bmpDataDest, destBuffer, imageWidth, imageHeight);

                return bmpDest;
            }
            finally
            {
                if (bmpDest != null) bmpDest.UnlockBits(bmpDataDest);
                if (bmpSource != null) bmpSource.UnlockBits(bmpDataSource);
            }
        }

        private void MatchColors(
            byte[] buffer,
            byte[] destBuffer,
            int pixelSize,
            ColorPalette pallete)
        {
            int length = destBuffer.Length;
            byte[] temp = new byte[pixelSize];

            int palleteSize = pallete.Entries.Length;

            int mult_1 = 256;
            int mult_2 = 256 * 256;

            int currentKey = 0;

            for (int i = 0; i < length; i++)
            {
                Array.Copy(buffer, i * pixelSize, temp, 0, pixelSize);

                currentKey = temp[0] + temp[1] * mult_1 + temp[2] * mult_2;

                if (!m_knownColors.ContainsKey(currentKey))
                {
                    destBuffer[i] = GetSimilarColor(pallete, temp, palleteSize);
                    m_knownColors.Add(currentKey, destBuffer[i]);
                }
                else
                {
                    destBuffer[i] = (byte)m_knownColors[currentKey];
                }
            }// for
        }



        private int GetPixelInfoSize(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Format24bppRgb:
                    {
                        return 3;
                    }
                default:
                    {
                        throw new ApplicationException("Only 24bit colors supported now");
                    }
            }

        }


        private void ReadBmpData(
            BitmapData bmpDataSource,
            byte[] buffer,
            int pixelSize,
            int width,
            int height)
        {
            int addrStart = bmpDataSource.Scan0.ToInt32();

            for (int i = 0; i < height; i++)
            {
                IntPtr realByteAddr = new IntPtr(addrStart +
                    Convert.ToInt32(i * bmpDataSource.Stride)
                    );

                Marshal.Copy(
                    realByteAddr,
                    buffer,
                    (int)(i * width * pixelSize),
                    (int)(width * pixelSize)
                );
            }
        }

        private void WriteBmpData(
            BitmapData bmpDataDest,
            byte[] destBuffer,
            int imageWidth,
            int imageHeight)
        {
            int addrStart = bmpDataDest.Scan0.ToInt32();

            for (int i = 0; i < imageHeight; i++)
            {
                IntPtr realByteAddr = new IntPtr(addrStart +
                   Convert.ToInt32(i * bmpDataDest.Stride)
                    );

                Marshal.Copy(
                    destBuffer,
                    i * imageWidth,
                    realByteAddr,
                    imageWidth
                );
            }
        }

        /// <summary>
        /// Returns Similar color 
        /// </summary>
        /// <param name="palette"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private byte GetSimilarColor(ColorPalette palette, byte[] color, int palleteSize)
        {

            byte minDiff = byte.MaxValue;
            byte index = 0;

            if (color.Length == 3)
            {
                for (int i = 0; i < palleteSize - 1; i++)
                {

                    byte currentDiff = GetMaxDiff(color, palette.Entries[i]);

                    if (currentDiff < minDiff)
                    {
                        minDiff = currentDiff;
                        index = (byte)i;
                    }
                }// for
            }
            else
            {
                throw new ApplicationException("Only 24bit colors supported now");
            }

            return index;
        }

        /// <summary>
        /// Return similar color
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static byte GetMaxDiff(byte[] a, Color b)
        {
            byte bDiff = a[0] > b.B ? (byte)(a[0] - b.B) : (byte)(b.B - a[0]);

            byte gDiff = a[1] > b.G ? (byte)(a[1] - b.G) : (byte)(b.G - a[1]);

            byte rDiff = a[2] > b.R ? (byte)(a[2] - b.R) : (byte)(b.R - a[2]);

            byte max = bDiff > gDiff ? bDiff : gDiff;

            max = max > rDiff ? max : rDiff;

            return max;
        }

    }
}

