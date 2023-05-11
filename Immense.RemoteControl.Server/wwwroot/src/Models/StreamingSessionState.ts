export class StreamingSessionState {
    constructor() {
        this.IsAtFrameStart = true;
        this.Buffer = [];
        this.ReceivedChunks = [];
        this.ImageSize = 0;
        this.IsProcessing = false;
        this.X = 0;
        this.Y = 0;
        this.Width = 0;
        this.Height = 0;
    }

    IsAtFrameStart: boolean;
    Buffer: Uint8Array[];
    ReceivedChunks: Uint8Array[];
    ImageSize: number;
    IsProcessing: boolean;
    X: number;
    Y: number;
    Width: number;
    Height: number;
}