using BSolutions.Buttonboard.Services.Attributes;

namespace BSolutions.Buttonboard.Services.Enumerations
{
    /// <summary>
    /// Defines the set of supported remote control commands
    /// that can be sent to a VLC player instance.
    /// </summary>
    /// <remarks>
    /// Each member is decorated with a <see cref="VlcPlayerCommandAttribute"/>,
    /// which specifies the exact query string value required by VLC’s HTTP interface.
    /// Example: <c>requests/status.xml?command=pl_pause</c>.
    /// </remarks>
    public enum VlcPlayerCommand
    {
        /// <summary>
        /// Pauses playback if currently playing, or resumes playback if paused.
        /// Corresponds to the VLC command string <c>pl_pause</c>.
        /// </summary>
        [VlcPlayerCommand("pl_pause")]
        PAUSE,

        /// <summary>
        /// Stops playback and clears the current playback state.
        /// Corresponds to the VLC command string <c>pl_stop</c>.
        /// </summary>
        [VlcPlayerCommand("pl_stop")]
        STOP,

        /// <summary>
        /// Skips to the next item in the current playlist.
        /// Corresponds to the VLC command string <c>pl_next</c>.
        /// </summary>
        [VlcPlayerCommand("pl_next")]
        NEXT,

        /// <summary>
        /// Returns to the previous item in the current playlist.
        /// Corresponds to the VLC command string <c>pl_previous</c>.
        /// </summary>
        [VlcPlayerCommand("pl_previous")]
        PREVIOUS
    }
}
