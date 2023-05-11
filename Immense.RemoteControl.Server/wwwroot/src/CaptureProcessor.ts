import { ViewerApp } from "./App.js";
import { StreamingState } from "./Models/StreamingState.js";
import { Screen2DContext } from "./UI.js";

// Five 32-bit values make up the header.
const FrameHeaderSize = 20;

export async function ProcessFrameChunk(chunk: Uint8Array, streamingState: StreamingState) {
    streamingState.ReceivedChunks.push(chunk);

    if (streamingState.IsProcessing) {
        return;
    }

    streamingState.IsProcessing = true;
    await processBuffer(streamingState);
}

async function processBuffer(streamingState: StreamingState): Promise<void> {
    while (true) {
        try {
            const chunks = streamingState.ReceivedChunks.splice(0);
            streamingState.Buffer = new Blob([streamingState.Buffer, ...chunks]);

            const bufferSize = streamingState.Buffer.size;

            if (bufferSize < FrameHeaderSize) {
                break;
            }

            const headerBlob = streamingState.Buffer.slice(0, FrameHeaderSize);
            const buffer = await headerBlob.arrayBuffer();

            const dataView = new DataView(buffer);
            const imageSize = dataView.getInt32(0, true);

            if (bufferSize - FrameHeaderSize < imageSize) {
                break;;
            }

            ViewerApp.MessageSender.SendFrameReceived();

            const imageX = dataView.getFloat32(4, true);
            const imageY = dataView.getFloat32(8, true);
            const imageWidth = dataView.getFloat32(12, true);
            const imageHeight = dataView.getFloat32(16, true);

            const imageBlob = streamingState.Buffer.slice(FrameHeaderSize, FrameHeaderSize + imageSize);

            const bitmap = await createImageBitmap(imageBlob);

            Screen2DContext.drawImage(bitmap,
                imageX,
                imageY,
                imageWidth,
                imageHeight);

            bitmap.close();

            streamingState.Buffer = streamingState.Buffer.slice(FrameHeaderSize + imageSize);
        }
        catch (ex) {
            console.error("Capture processing error.  Resetting stream buffer.", ex);
            streamingState.Buffer = new Blob();
            break;
        }
    }
    streamingState.IsProcessing = false;
}