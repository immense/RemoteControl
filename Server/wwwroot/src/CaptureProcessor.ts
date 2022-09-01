import { ViewerApp } from "./App.js";
import { ScreenCaptureDto } from "./Interfaces/Dtos.js";
import { CompletedFrame } from "./Models/CompletedFrame.js";
import { Screen2DContext } from "./UI.js";


export function HandleCaptureReceived(screenCapture: ScreenCaptureDto) {

    let imageBlob = new Blob([screenCapture.ImageBytes]);

    createImageBitmap(imageBlob).then(bitmap => {
        Screen2DContext.drawImage(bitmap,
            screenCapture.Left,
            screenCapture.Top,
            screenCapture.Width,
            screenCapture.Height);

        bitmap.close();

        ViewerApp.MessageSender.SendFrameReceived();
    });

    //let url = window.URL.createObjectURL(imageBlob);
    //let img = new Image(captureFrame.Width, captureFrame.Height);
    //img.onload = () => {
    //    UI.Screen2DContext.drawImage(img,
    //        captureFrame.Left,
    //        captureFrame.Top,
    //        captureFrame.Width,
    //        captureFrame.Height);
    //    window.URL.revokeObjectURL(url);
    //};
}

