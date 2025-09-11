using BSolutions.Buttonboard.Services.Attributes;

namespace BSolutions.Buttonboard.Services.Enumerations
{
    public enum VlcPlayerCommand
    {
        [VlcPlayerCommand("pl_pause")]
        PAUSE,

        [VlcPlayerCommand("pl_stop")]
        STOP,

        [VlcPlayerCommand("pl_next")]
        NEXT,

        [VlcPlayerCommand("pl_previous")]
        PREVIOUS
    }
}
