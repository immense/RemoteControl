import * as UI from "./UI.js";
import { DtoType } from "./Enums/BaseDtoType.js";
import { ViewerApp } from "./App.js";
import { Sound } from "./Sound.js";
import { ReceiveFile } from "./FileTransferService.js";
import { HandleCaptureReceived } from "./CaptureProcessor.js";
const Chunks = {};
export class DtoMessageHandler {
    constructor() {
        this.MessagePack = window['msgpack5']();
    }
    ParseBinaryMessage(data) {
        var wrapper = this.MessagePack.decode(data);
        switch (wrapper.DtoType) {
            case DtoType.AudioSample:
                this.HandleAudioSample(wrapper);
                break;
            case DtoType.CaptureFrame:
                this.HandleCaptureFrame(wrapper);
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
        let audioSample = this.TryComplete(wrapper);
        if (!audioSample) {
            return;
        }
        Sound.Play(audioSample.Buffer);
    }
    HandleCaptureFrame(wrapper) {
        let captureFrame = this.TryComplete(wrapper);
        if (!captureFrame) {
            return;
        }
        HandleCaptureReceived(captureFrame);
    }
    HandleClipboardText(wrapper) {
        let clipboardText = this.TryComplete(wrapper);
        if (!clipboardText) {
            return;
        }
        ViewerApp.ClipboardWatcher.SetClipboardText(clipboardText.ClipboardText);
    }
    HandleCursorChange(wrapper) {
        let cursorChange = this.TryComplete(wrapper);
        if (!cursorChange) {
            return;
        }
        UI.UpdateCursor(cursorChange.ImageBytes, cursorChange.HotSpotX, cursorChange.HotSpotY, cursorChange.CssOverride);
    }
    HandleFile(wrapper) {
        let file = this.TryComplete(wrapper);
        if (!file) {
            return;
        }
        ReceiveFile(file);
    }
    HandleScreenData(wrapper) {
        let screenDataDto = this.TryComplete(wrapper);
        if (!screenDataDto) {
            return;
        }
        document.title = `${screenDataDto.MachineName} - Remotely Session`;
        UI.ToggleConnectUI(false);
        UI.SetScreenSize(screenDataDto.ScreenWidth, screenDataDto.ScreenHeight);
        UI.UpdateDisplays(screenDataDto.SelectedDisplay, screenDataDto.DisplayNames);
    }
    HandleScreenSize(wrapper) {
        let screenSizeDto = this.TryComplete(wrapper);
        if (!screenSizeDto) {
            return;
        }
        UI.SetScreenSize(screenSizeDto.Width, screenSizeDto.Height);
    }
    HandleWindowsSessions(wrapper) {
        let windowsSessionsDto = this.TryComplete(wrapper);
        if (!windowsSessionsDto) {
            return;
        }
        UI.UpdateWindowsSessions(windowsSessionsDto.WindowsSessions);
    }
    TryComplete(wrapper) {
        if (!Chunks[wrapper.InstanceId]) {
            Chunks[wrapper.InstanceId] = [];
        }
        Chunks[wrapper.InstanceId].push(wrapper);
        if (!wrapper.IsLastChunk) {
            return;
        }
        const buffers = Chunks[wrapper.InstanceId]
            .sort((a, b) => a.SequenceId - b.SequenceId)
            .map(x => x.DtoChunk)
            .reduce(x => x);
        delete Chunks[wrapper.InstanceId];
        return this.MessagePack.decode(buffers);
    }
}
//# sourceMappingURL=DtoMessageHandler.js.map