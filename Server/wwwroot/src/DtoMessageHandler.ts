import * as UI from "./UI.js";
import { DtoType } from "./Enums/DtoType.js";
import { ViewerApp } from "./App.js";
import { ShowMessage } from "./UI.js";
import { Sound } from "./Sound.js";
import {
    AudioSampleDto,
    ScreenCaptureDto,
    ClipboardTextDto,
    CursorChangeDto,
    ScreenDataDto,
    ScreenSizeDto,
    FileDto,
    WindowsSessionsDto,
    DtoWrapper
} from "./Interfaces/Dtos.js";
import { ReceiveFile } from "./FileTransferService.js";
import { HandleCaptureReceived } from "./CaptureProcessor.js";
import { TryComplete } from "./DtoChunker.js";

export class DtoMessageHandler {

    MessagePack: any = window['msgpack5']();

    ParseBinaryMessage(data: ArrayBuffer) {
        var wrapper = this.MessagePack.decode(data) as DtoWrapper;
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
                this.HandleScreenSize(wrapper)
                break;
            case DtoType.WindowsSessions:
                this.HandleWindowsSessions(wrapper)
                break;
            case DtoType.File:
                this.HandleFile(wrapper);
            default:
                break;
        }
    }

    HandleAudioSample(wrapper: DtoWrapper) {
        let audioSample = TryComplete<AudioSampleDto>(wrapper);

        if (!audioSample) {
            return;
        }

        Sound.Play(audioSample.Buffer);
    }

    HandleScreenCapture(wrapper: DtoWrapper) {
        let screenCapture = TryComplete<ScreenCaptureDto>(wrapper);

        if (!screenCapture) {
            return;
        }

        HandleCaptureReceived(screenCapture);
    }

    HandleClipboardText(wrapper: DtoWrapper) {
        let clipboardText = TryComplete<ClipboardTextDto>(wrapper);

        if (!clipboardText) {
            return;
        }

        ViewerApp.ClipboardWatcher.SetClipboardText(clipboardText.ClipboardText);
    }
    HandleCursorChange(wrapper: DtoWrapper) {
        let cursorChange = TryComplete<CursorChangeDto>(wrapper);

        if (!cursorChange) {
            return;
        }

        UI.UpdateCursor(cursorChange.ImageBytes, cursorChange.HotSpotX, cursorChange.HotSpotY, cursorChange.CssOverride);
    }
    HandleFile(wrapper: DtoWrapper) {
        let file = TryComplete<FileDto>(wrapper);

        if (!file) {
            return;
        }

        ReceiveFile(file);
    }
    HandleScreenData(wrapper: DtoWrapper) {
        let screenDataDto = TryComplete<ScreenDataDto>(wrapper);

        if (!screenDataDto) {
            return;
        }

        document.title = `${screenDataDto.MachineName} - Remotely Session`;
        UI.ToggleConnectUI(false);
        UI.SetScreenSize(screenDataDto.ScreenWidth, screenDataDto.ScreenHeight);
        UI.UpdateDisplays(screenDataDto.SelectedDisplay, screenDataDto.DisplayNames);
    }

    HandleScreenSize(wrapper: DtoWrapper) {
        let screenSizeDto = TryComplete<ScreenSizeDto>(wrapper);

        if (!screenSizeDto) {
            return;
        }

        UI.SetScreenSize(screenSizeDto.Width, screenSizeDto.Height);
    }

    HandleWindowsSessions(wrapper: DtoWrapper) {
        let windowsSessionsDto = TryComplete<WindowsSessionsDto>(wrapper);

        if (!windowsSessionsDto) {
            return;
        }

        UI.UpdateWindowsSessions(windowsSessionsDto.WindowsSessions);
    }
}