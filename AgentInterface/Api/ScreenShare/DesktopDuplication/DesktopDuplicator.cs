#region

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Point = System.Drawing.Point;
using Rectangle = SharpDX.Rectangle;
using Resource = SharpDX.DXGI.Resource;
using ResultCode = SharpDX.DXGI.ResultCode;

#endregion

namespace AgentInterface.Api.ScreenShare.DesktopDuplication
{
    /// <summary>
    ///     Provides access to frame-by-frame updates of a particular desktop (i.e. one monitor), with image and cursor
    ///     information.
    /// </summary>
    public class DesktopDuplicator
    {
        private readonly OutputDuplication _mDeskDupl;
        private readonly Device _mDevice;
        private readonly Texture2DDescription _mTextureDesc;
        private readonly int _mWhichOutputDevice;
        private Texture2D _desktopImageTexture;

        private Bitmap _finalImage1, _finalImage2;
        private OutputDuplicateFrameInformation _frameInfo;
        private bool _isFinalImage1;
        private OutputDescription _mOutputDesc;

        /// <summary>
        ///     Duplicates the output of the specified monitor.
        /// </summary>
        /// <param name="whichMonitor">
        ///     The output device to duplicate (i.e. monitor). Begins with zero, which seems to correspond
        ///     to the primary monitor.
        /// </param>
        public DesktopDuplicator(int whichMonitor)
            : this(0, whichMonitor)
        {
        }

        /// <summary>
        ///     Duplicates the output of the specified monitor on the specified graphics adapter.
        /// </summary>
        /// <param name="whichGraphicsCardAdapter">The adapter which contains the desired outputs.</param>
        /// <param name="whichOutputDevice">
        ///     The output device to duplicate (i.e. monitor). Begins with zero, which seems to
        ///     correspond to the primary monitor.
        /// </param>
        public DesktopDuplicator(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            _mWhichOutputDevice = whichOutputDevice;
            Adapter1 adapter;
            try
            {
                adapter = new Factory1().GetAdapter1(whichGraphicsCardAdapter);
            }
            catch (SharpDXException)
            {
                throw new DesktopDuplicationException("Could not find the specified graphics card adapter.");
            }
            _mDevice = new Device(adapter);
            Output output;
            try
            {
                output = adapter.GetOutput(whichOutputDevice);
            }
            catch (SharpDXException)
            {
                throw new DesktopDuplicationException("Could not find the specified output device.");
            }
            var output1 = output.QueryInterface<Output1>();
            _mOutputDesc = output.Description;
            _mTextureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = ((Rectangle) _mOutputDesc.DesktopBounds).Width,
                Height = ((Rectangle) _mOutputDesc.DesktopBounds).Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = {Count = 1, Quality = 0},
                Usage = ResourceUsage.Staging
            };

            try
            {
                _mDeskDupl = output1.DuplicateOutput(_mDevice);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == ResultCode.NotCurrentlyAvailable.Result.Code)
                {
                    throw new DesktopDuplicationException(
                        "There is already the maximum number of applications using the Desktop Duplication API running, please close one of the applications and try again.");
                }
            }
        }

        private Bitmap FinalImage
        {
            get { return _isFinalImage1 ? _finalImage1 : _finalImage2; }
            set
            {
                if (_isFinalImage1)
                {
                    _finalImage2 = value;
                    _finalImage1?.Dispose();
                }
                else
                {
                    _finalImage1 = value;
                    _finalImage2?.Dispose();
                }
                _isFinalImage1 = !_isFinalImage1;
            }
        }

        /// <summary>
        ///     Retrieves the latest desktop image and associated metadata.
        /// </summary>
        public DesktopFrame GetLatestFrame()
        {
            var frame = new DesktopFrame();
            // Try to get the latest frame; this may timeout
            var retrievalTimedOut = RetrieveFrame();
            if (retrievalTimedOut)
                return null;
            try
            {
                RetrieveFrameMetadata(frame);
                //we don't need cursor info
                //RetrieveCursorMetadata(frame);
                //we dont need a full frame
                //ProcessFrame(frame);
            }
            catch
            {
                ReleaseFrame();
            }
            try
            {
                ReleaseFrame();
            }
            catch
            {
                //    throw new DesktopDuplicationException("Couldn't release frame.");  
            }
            return frame;
        }

        private bool RetrieveFrame()
        {
            if (_desktopImageTexture == null)
                _desktopImageTexture = new Texture2D(_mDevice, _mTextureDesc);
            Resource desktopResource = null;
            _frameInfo = new OutputDuplicateFrameInformation();
            try
            {
                _mDeskDupl.AcquireNextFrame(5000, out _frameInfo, out desktopResource);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == ResultCode.WaitTimeout.Result.Code)
                {
                    return true;
                }
                if (ex.ResultCode.Failure)
                {
                    //return true;
                    desktopResource?.Dispose();
                    throw new DesktopDuplicationException("Failed to acquire next frame.");
                }
            }
            using (var tempTexture = desktopResource?.QueryInterface<Texture2D>())
                _mDevice.ImmediateContext.CopyResource(tempTexture, _desktopImageTexture);
            desktopResource?.Dispose();
            return false;
        }
        private void RetrieveFrameMetadata(DesktopFrame frame)
        {
            if (_frameInfo.TotalMetadataBufferSize > 0)
            {
                // Get moved regions
                var movedRegionsLength = 0;
                var movedRectangles = new OutputDuplicateMoveRectangle[_frameInfo.TotalMetadataBufferSize];
                _mDeskDupl.GetFrameMoveRects(movedRectangles.Length, movedRectangles, out movedRegionsLength);
                frame.MovedRegions =
                    new MovedRegion[movedRegionsLength / Marshal.SizeOf(typeof(OutputDuplicateMoveRectangle))];
                for (var i = 0; i < frame.MovedRegions.Length; i++)
                {
                    var destRect = (Rectangle)movedRectangles[i].DestinationRect;
                    frame.MovedRegions[i] = new MovedRegion
                    {
                        Source = new Point(movedRectangles[i].SourcePoint.X, movedRectangles[i].SourcePoint.Y),
                        Destination = new global::System.Drawing.Rectangle(destRect.X, destRect.Y, destRect.Width, destRect.Height)
                    };
                }

                // Get dirty regions
                var dirtyRegionsLength = 0;
                var dirtyRectangles = new RawRectangle[_frameInfo.TotalMetadataBufferSize];
                _mDeskDupl.GetFrameDirtyRects(dirtyRectangles.Length, dirtyRectangles, out dirtyRegionsLength);
                frame.UpdatedRegions = new global::System.Drawing.Rectangle[dirtyRegionsLength / Marshal.SizeOf(typeof(Rectangle))];
                frame.FinishedRegions = new FinishedRegions[frame.UpdatedRegions.Length];
                for (var i = 0; i < frame.UpdatedRegions.Length; i++)
                {
                    var dirtyRect = (Rectangle)dirtyRectangles[i];
                    var rect = new global::System.Drawing.Rectangle(dirtyRect.X, dirtyRect.Y, dirtyRect.Width,
                        dirtyRect.Height);
    

                    frame.FinishedRegions[i] = new FinishedRegions
                    {
                        Destination = rect,
                        Frame = ExtractRect(rect.X, rect.Y, rect.Width, rect.Height)
                    };
                }
            }
            else
            {
                frame.MovedRegions = new MovedRegion[0];
                frame.UpdatedRegions = new global::System.Drawing.Rectangle[0];
            }
        }
      

        private Bitmap ExtractRect(int originX, int originY, int width, int height)
        {
            // Get the desktop capture screenTexture
            DataBox mapSource = _mDevice.ImmediateContext.MapSubresource(_desktopImageTexture, 0, MapMode.Read,
                MapFlags.None);

            // Create Drawing.Bitmap

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb); //不能是ARGB
            var boundsRect = new global::System.Drawing.Rectangle(0, 0, width, height);

            // Copy pixels from screen capture Texture to GDI bitmap
            BitmapData mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            IntPtr sourcePtr = mapSource.DataPointer;
            IntPtr destPtr = mapDest.Scan0;

            sourcePtr = IntPtr.Add(sourcePtr, originY * mapSource.RowPitch + originX * 4);
            for (int y = 0; y < height; y++)
            {
                // Copy a single line 

                Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                // Advance pointers
                if (y != height - 1)
                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }

            // Release source and dest locks
            bitmap.UnlockBits(mapDest);
            _mDevice.ImmediateContext.UnmapSubresource(_desktopImageTexture, 0);
            return bitmap;
        }

        private void ProcessFrame(DesktopFrame frame)
        {
            // Get the desktop capture texture
            var mapSource = _mDevice.ImmediateContext.MapSubresource(_desktopImageTexture, 0, MapMode.Read,
                MapFlags.None);
            var bounds = (Rectangle) _mOutputDesc.DesktopBounds;
            FinalImage = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppRgb);
            var boundsRect = new global::System.Drawing.Rectangle(0, 0, bounds.Width, bounds.Height);
            // Copy pixels from screen capture Texture to GDI bitmap
            var mapDest = FinalImage.LockBits(boundsRect, ImageLockMode.WriteOnly, FinalImage.PixelFormat);
            var sourcePtr = mapSource.DataPointer;
            var destPtr = mapDest.Scan0;
            for (var y = 0; y < bounds.Height; y++)
            {
                // Copy a single line 
                Utilities.CopyMemory(destPtr, sourcePtr, bounds.Width*4);

                // Advance pointers
                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }

            // Release source and dest locks
            FinalImage.UnlockBits(mapDest);
            _mDevice.ImmediateContext.UnmapSubresource(_desktopImageTexture, 0);
            frame.DesktopImage = (Bitmap) FinalImage.Clone();
        }

        private void ReleaseFrame()
        {
            try
            {
                _mDeskDupl.ReleaseFrame();
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Failure)
                {
                    throw new DesktopDuplicationException("Failed to release frame.");
                }
            }
        }
    }
}