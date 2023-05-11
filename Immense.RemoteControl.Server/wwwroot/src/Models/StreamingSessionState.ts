export class StreamingSessionState {
    constructor() {
        this.Buffer = new Blob();
        this.ReceivedChunks = [];
        this.IsProcessing = false;
    }

    Buffer: Blob;
    ReceivedChunks: Uint8Array[];
    IsProcessing: boolean;
}