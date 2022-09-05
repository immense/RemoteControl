import { ViewerApp } from "./App.js";
import { ScreenCaptureDto } from "./Interfaces/Dtos.js";
import { CompletedFrame } from "./Models/CompletedFrame.js";
import { Screen2DContext } from "./UI.js";

const Partials: Record<string, Array<Uint8Array>> = {};

export function HandleCaptureReceived(screenCapture: ScreenCaptureDto) {

    if (!Partials[screenCapture.InstanceId]) {
        Partials[screenCapture.InstanceId] = [];
    }

    Partials[screenCapture.InstanceId].push(screenCapture.ImageBytes);

    if (!screenCapture.IsLastChunk) {
        return;
    }

    let imageBlob = new Blob(Partials[screenCapture.InstanceId]);
    delete Partials[screenCapture.InstanceId];
    ViewerApp.MessageSender.SendFrameReceived();

    createImageBitmap(imageBlob).then(bitmap => {
        Screen2DContext.drawImage(bitmap,
            screenCapture.Left,
            screenCapture.Top,
            screenCapture.Width,
            screenCapture.Height);

        bitmap.close();
    });

    //let url = window.URL.createObjectURL(imageBlob);
    //let img = new Image(screenCapture.Width, screenCapture.Height);
    //img.onload = () => {
    //    Screen2DContext.drawImage(img,
    //        screenCapture.Left,
    //        screenCapture.Top,
    //        screenCapture.Width,
    //        screenCapture.Height);
    //    window.URL.revokeObjectURL(url);
    //};
    //img.src = url;
}

