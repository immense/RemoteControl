import { ViewerApp } from "./App.js";
import { StreamingState } from "./Models/StreamingState.js";
import { Screen2DContext } from "./UI.js";
import { GetUint64 } from "./Utilities.js";

const FrameHeaderSize = 28;

export async function ProcessFrameChunk(chunk: Uint8Array, streamingState: StreamingState) {
    streamingState.ReceivedChunks.push(chunk);

    if (streamingState.IsProcessing) {
        return;
    }

    streamingState.IsProcessing = true;
    await processBuffer(streamingState);
}

async function processBuffer(streamingState: StreamingState): Promise<void> {
    try {
        const chunks = streamingState.ReceivedChunks.splice(0);
        streamingState.Buffer = new Blob([streamingState.Buffer, ...chunks]);

        const bufferSize = streamingState.Buffer.size;

        if (bufferSize < FrameHeaderSize) {
            streamingState.IsProcessing = false;
            return;
        }

        const headerBlob = streamingState.Buffer.slice(0, FrameHeaderSize);
        const buffer = await headerBlob.arrayBuffer();

        const dataView = new DataView(buffer);
        const imageSize = dataView.getInt32(0, true);

        if (bufferSize - FrameHeaderSize < imageSize) {
            streamingState.IsProcessing = false;
            return;;
        }

        const imageX = dataView.getFloat32(4, true);
        const imageY = dataView.getFloat32(8, true);
        const imageWidth = dataView.getFloat32(12, true);
        const imageHeight = dataView.getFloat32(16, true);
        const timestamp = GetUint64(dataView, 20, true);

        const imageBlob = streamingState.Buffer.slice(FrameHeaderSize, FrameHeaderSize + imageSize);

        const bitmap = await createImageBitmap(imageBlob);

        Screen2DContext.drawImage(bitmap,
            imageX,
            imageY,
            imageWidth,
            imageHeight);

        bitmap.close();

        streamingState.Buffer = streamingState.Buffer.slice(FrameHeaderSize + imageSize);

        ViewerApp.MessageSender.SendFrameReceived(timestamp);

        requestAnimationFrame(() => {
            processBuffer(streamingState);
        });
    }
    catch (ex) {
        console.error("Capture processing error.  Resetting stream buffer.", ex);
        streamingState.Buffer = new Blob();
    }
}