#region

using System.Drawing;
using System.Runtime.Serialization;

#endregion

namespace AgentInterface.Api.ScreenShare.DesktopDuplication
{

    /// <summary>
    /// Provides image data, cursor data, and image metadata about the retrieved desktop frame.
    /// </summary>
    [DataContract]
    public class DesktopFrame
    {
        /// <summary>
        ///     Gets the bitmap representing the last retrieved desktop frame. This image spans the entire bounds of the specified
        ///     monitor.
        /// </summary>
        [DataMember]
        public Bitmap DesktopImage { get; set; }


        /// <summary>
        ///     Gets a list of the rectangles of pixels in the desktop image that the operating system moved to another location
        ///     within the same image.
        /// </summary>
        /// <remarks>
        ///     To produce a visually accurate copy of the desktop, an application must first process all moved regions before it
        ///     processes updated regions.
        /// </remarks>
        [DataMember]
        public MovedRegion[] MovedRegions { get; set; }


        /// <summary>
        /// Returns the list of non-overlapping rectangles that indicate the areas of the desktop image that the operating system updated since the last retrieved frame.
        /// </summary>
        /// <remarks>
        /// To produce a visually accurate copy of the desktop, an application must first process all moved regions before it processes updated regions.
        /// </remarks>
        [DataMember]
        public Rectangle[] UpdatedRegions { get; set; }


        /// <summary>
        ///     The number of frames that the operating system accumulated in the desktop image surface since the last retrieved
        ///     frame.
        /// </summary>
        [DataMember]
        public int AccumulatedFrames { get; set; }

        /// <summary>
        ///     Gets the location of the top-left-hand corner of the cursor. This is not necessarily the same position as the
        ///     cursor's hot spot, which is the location in the cursor that interacts with other elements on the screen.
        /// </summary>
        [DataMember]
        public Point CursorLocation { get; set; }

        /// <summary>
        ///     Gets whether the cursor on the last retrieved desktop image was visible.
        /// </summary>
        [DataMember]
        public bool CursorVisible { get; set; }

        /// <summary>
        ///     Gets whether the desktop image contains protected content that was already blacked out in the desktop image.
        /// </summary>
        [DataMember]
        public bool ProtectedContentMaskedOut { get; set; }

        /// <summary>
        ///     Gets whether the operating system accumulated updates by coalescing updated regions. If so, the updated regions
        ///     might contain unmodified pixels.
        /// </summary>
        [DataMember]
        public bool RectanglesCoalesced { get; set; }

        [DataMember]
        public FinishedRegions[] FinishedRegions { get; set; }
    }
}