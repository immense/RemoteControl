import { ViewerApp } from "./App.js";
import { StreamingSessionState } from "./Models/StreamingSessionState.js";
import { Screen2DContext } from "./UI.js";

// Five 32-bit values make up the header.
const FrameHeaderSize = 20;

export async function HandleCaptureReceived(chunk: Uint8Array, streamState: StreamingSessionState) {
    try {
        const newBuffer = new Uint8Array(streamState.Chunks.length + chunk.length);
        newBuffer.set(streamState.Chunks);
        newBuffer.set(chunk, streamState.Chunks.length);
        streamState.Chunks = newBuffer;

        if (!streamState.MetadataSet && streamState.Chunks.length >= FrameHeaderSize) {
            streamState.MetadataSet = true;

            const dataView = new DataView(streamState.Chunks.buffer);
            streamState.ImageSize = dataView.getInt32(0, true);
            streamState.X = dataView.getFloat32(4, true);
            streamState.Y = dataView.getFloat32(8, true);
            streamState.Width = dataView.getFloat32(12, true);
            streamState.Height = dataView.getFloat32(16, true);
        }

        if (streamState.MetadataSet &&
            streamState.Chunks.length - FrameHeaderSize >= streamState.ImageSize) {

            streamState.MetadataSet = false;
            ViewerApp.MessageSender.SendFrameReceived();

            const imageBlob = new Blob([streamState.Chunks.slice(FrameHeaderSize)]);

            const bitmap = await createImageBitmap(imageBlob);

            Screen2DContext.drawImage(bitmap,
                streamState.X,
                streamState.Y,
                streamState.Width,
                streamState.Height);

            bitmap.close();

            streamState.Chunks = new Uint8Array();
        }
    }
    catch (ex) {
        console.error("Capture processing error.  Resetting streaming state.", ex);
        streamState.MetadataSet = false;
        streamState.Chunks = new Uint8Array();
    }
}
