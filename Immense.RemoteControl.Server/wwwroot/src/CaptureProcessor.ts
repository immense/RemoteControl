import { ViewerApp } from "./App.js";
import { StreamingSessionState } from "./Models/StreamingSessionState.js";
import { Screen2DContext } from "./UI.js";

// Five 32-bit values make up the header.
const FrameHeaderSize = 20;

export async function ProcessFrameChunk(chunk: Uint8Array, streamState: StreamingSessionState) {
    streamState.ReceivedChunks.push(chunk);

    if (streamState.IsProcessing) {
        // Already processing.
        return;
    }

    streamState.IsProcessing = true;

    try {
        await processReceivedChunks(streamState);
    }
    catch (ex) {
        console.error("Capture processing error.  Resetting streaming state.", ex);
        streamState.Buffer = new Blob();
    }
    finally {
        streamState.IsProcessing = false;
    }
}

async function processReceivedChunks(streamState: StreamingSessionState): Promise<void> {
    if (streamState.ReceivedChunks.length == 0) {
        return;
    }

    const chunks = streamState.ReceivedChunks.splice(0);
    streamState.Buffer = new Blob([streamState.Buffer, ...chunks]);

    while (true) {
        const bufferSize = streamState.Buffer.size;

        if (bufferSize < FrameHeaderSize) {
            break;
        }

        const headerBlob = streamState.Buffer.slice(0, FrameHeaderSize);
        const buffer = await headerBlob.arrayBuffer();

        const dataView = new DataView(buffer);
        const imageSize = dataView.getInt32(0, true);

        if (bufferSize - FrameHeaderSize < imageSize) {
            break;
        }

        ViewerApp.MessageSender.SendFrameReceived();

        const imageX = dataView.getFloat32(4, true);
        const imageY = dataView.getFloat32(8, true);
        const imageWidth = dataView.getFloat32(12, true);
        const imageHeight = dataView.getFloat32(16, true);

        const imageBlob = streamState.Buffer.slice(FrameHeaderSize, FrameHeaderSize + imageSize);

        const bitmap = await createImageBitmap(imageBlob);

        Screen2DContext.drawImage(bitmap,
            imageX,
            imageY,
            imageWidth,
            imageHeight);

        bitmap.close();

        streamState.Buffer = streamState.Buffer.slice(FrameHeaderSize + imageSize);
    }
    
}