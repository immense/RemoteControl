using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Models
{
    public class CaptureFrame
    {
        public byte[] EncodedImageBytes { get; init; } = Array.Empty<byte>();
        public Guid Id { get; } = Guid.NewGuid();
        public int Top { get; init; }
        public int Left { get; init; }
        public int Height { get; init; }
        public int Width { get; init; }
        public long Sequence { get; init; }
    }
}
