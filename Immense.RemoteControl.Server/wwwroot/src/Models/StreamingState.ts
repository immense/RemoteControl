export class StreamingState {
    constructor() {
        this.Buffer = new Blob();
        this.IsProcessing = false;
        this.ReceivedChunks = [];
        this.StreamEnded = false;
    }

    Buffer: Blob;
    IsProcessing: boolean;
    ReceivedChunks: Uint8Array[];
    StreamEnded: boolean;
}