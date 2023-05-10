export class StreamingSessionState {
    constructor() {
        this.MetadataSet = false;
        this.Chunks = new Uint8Array();
        this.ImageSize = 0;
        this.X = 0;
        this.Y = 0;
        this.Width = 0;
        this.Height = 0;
    }

    MetadataSet: boolean;
    Chunks: Uint8Array;
    ImageSize: number;
    X: number;
    Y: number;
    Width: number;
    Height: number;
}