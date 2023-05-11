export class StreamingState {
    constructor() {
        this.Buffer = new Blob();
        this.IsProcessing = false;
        this.ReceivedChunks = [];
    }

    Buffer: Blob;
    IsProcessing: boolean;
    ReceivedChunks: Uint8Array[];
}