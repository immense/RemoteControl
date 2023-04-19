import { ViewerApp } from "./App.js";
import { ScreenCaptureDto } from "./Interfaces/Dtos.js";
import { Screen2DContext } from "./UI.js";

const Partials: Record<string, Array<Uint8Array>> = {};

export async function HandleCaptureReceived(screenCapture: ScreenCaptureDto) {

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

    let bitmap = await createImageBitmap(imageBlob);

    Screen2DContext.drawImage(bitmap,
        screenCapture.Left,
        screenCapture.Top,
        screenCapture.Width,
        screenCapture.Height);

    bitmap.close();

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

