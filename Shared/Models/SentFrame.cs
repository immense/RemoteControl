using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Shared.Models
{
    public struct SentFrame
    {
        public SentFrame(int frameSize, DateTimeOffset timestamp)
        {
            FrameSize = frameSize;
            Timestamp = timestamp;
        }

        public DateTimeOffset Timestamp { get; }
        public int FrameSize { get; }
    }
}
