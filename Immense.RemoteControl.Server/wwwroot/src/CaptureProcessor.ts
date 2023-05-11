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
        streamState.IsAtFrameStart = false;
        streamState.Buffer = [];
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
    streamState.Buffer.push(...chunks);
    const bufferSize = streamState.Buffer.reduce((acc, cur) => acc + cur.length, 0);

    if (streamState.IsAtFrameStart && bufferSize >= FrameHeaderSize) {
        streamState.IsAtFrameStart = false;

        const bufferBlob = new Blob(streamState.Buffer);
        const buffer = await bufferBlob.arrayBuffer();

        const dataView = new DataView(buffer);
        streamState.ImageSize = dataView.getInt32(0, true);
        streamState.X = dataView.getFloat32(4, true);
        streamState.Y = dataView.getFloat32(8, true);
        streamState.Width = dataView.getFloat32(12, true);
        streamState.Height = dataView.getFloat32(16, true);
    }

    if (!streamState.IsAtFrameStart &&
        bufferSize - FrameHeaderSize >= streamState.ImageSize) {

        streamState.IsAtFrameStart = true;
        ViewerApp.MessageSender.SendFrameReceived();

        const imageBlob = new Blob(streamState.Buffer).slice(FrameHeaderSize, FrameHeaderSize + streamState.ImageSize);

        const bitmap = await createImageBitmap(imageBlob);

        Screen2DContext.drawImage(bitmap,
            streamState.X,
            streamState.Y,
            streamState.Width,
            streamState.Height);

        bitmap.close();

        streamState.Buffer = [];
    }

    await processReceivedChunks(streamState);
}