import * as UI from "./UI.js";
import { DtoType } from "./Enums/DtoType.js";
import { ViewerApp } from "./App.js";
import { Sound } from "./Sound.js";
import { ReceiveFile } from "./FileTransferService.js";
import { HandleCaptureReceived } from "./CaptureProcessor.js";
import { TryComplete } from "./DtoChunker.js";
const MsgPack = window["MessagePack"];
export class DtoMessageHandler {
    ParseBinaryMessage(data) {
        var wrapper = MsgPack.decode(data);
        switch (wrapper.DtoType) {
            case DtoType.AudioSample:
                this.HandleAudioSample(wrapper);
                break;
            case DtoType.ScreenCapture:
                this.HandleScreenCapture(wrapper);
                break;
            case DtoType.ClipboardText:
                this.HandleClipboardText(wrapper);
                break;
            case DtoType.CursorChange:
                this.HandleCursorChange(wrapper);
                break;
            case DtoType.ScreenData:
                this.HandleScreenData(wrapper);
                break;
            case DtoType.ScreenSize:
                this.HandleScreenSize(wrapper);
                break;
            case DtoType.WindowsSessions:
                this.HandleWindowsSessions(wrapper);
                break;
            case DtoType.File:
                this.HandleFile(wrapper);
            default:
                break;
        }
    }
    HandleAudioSample(wrapper) {
        let audioSample = TryComplete(wrapper);
        if (!audioSample) {
            return;
        }
        Sound.Play(audioSample.Buffer);
    }
    HandleScreenCapture(wrapper) {
        let screenCapture = TryComplete(wrapper);
        if (!screenCapture) {
            return;
        }
        HandleCaptureReceived(screenCapture);
    }
    HandleClipboardText(wrapper) {
        let clipboardText = TryComplete(wrapper);
        if (!clipboardText) {
            return;
        }
        ViewerApp.ClipboardWatcher.SetClipboardText(clipboardText.ClipboardText);
    }
    HandleCursorChange(wrapper) {
        let cursorChange = TryComplete(wrapper);
        if (!cursorChange) {
            return;
        }
        UI.UpdateCursor(cursorChange.ImageBytes, cursorChange.HotSpotX, cursorChange.HotSpotY, cursorChange.CssOverride);
    }
    HandleFile(wrapper) {
        let file = TryComplete(wrapper);
        if (!file) {
            return;
        }
        ReceiveFile(file);
    }
    HandleScreenData(wrapper) {
        let screenDataDto = TryComplete(wrapper);
        if (!screenDataDto) {
            return;
        }
        document.title = `${screenDataDto.MachineName} - Remotely Session`;
        UI.ToggleConnectUI(false);
        UI.SetScreenSize(screenDataDto.ScreenWidth, screenDataDto.ScreenHeight);
        UI.UpdateDisplays(screenDataDto.SelectedDisplay, screenDataDto.DisplayNames);
    }
    HandleScreenSize(wrapper) {
        let screenSizeDto = TryComplete(wrapper);
        if (!screenSizeDto) {
            return;
        }
        UI.SetScreenSize(screenSizeDto.Width, screenSizeDto.Height);
    }
    HandleWindowsSessions(wrapper) {
        let windowsSessionsDto = TryComplete(wrapper);
        if (!windowsSessionsDto) {
            return;
        }
        UI.UpdateWindowsSessions(windowsSessionsDto.WindowsSessions);
    }
}
//# sourceMappingURL=DtoMessageHandler.js.map