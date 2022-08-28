import * as UI from "./UI.js";
import { DtoType } from "./Enums/BaseDtoType.js";
import { ViewerApp } from "./App.js";
import { ShowMessage } from "./UI.js";
import { Sound } from "./Sound.js";
import {
    AudioSampleDto,
    CaptureFrameDto,
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

const Chunks: Record<string, DtoWrapper[]> = {};

export class DtoMessageHandler {

    MessagePack: any = window['msgpack5']();

    ParseBinaryMessage(data: ArrayBuffer) {
        var wrapper = this.MessagePack.decode(data) as DtoWrapper;
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
        let audioSample = this.TryComplete<AudioSampleDto>(wrapper);

        if (!audioSample) {
            return;
        }

        Sound.Play(audioSample.Buffer);
    }

    HandleCaptureFrame(wrapper: DtoWrapper) {
        let captureFrame = this.TryComplete<CaptureFrameDto>(wrapper);

        if (!captureFrame) {
            return;
        }

        HandleCaptureReceived(captureFrame);
    }

    HandleClipboardText(wrapper: DtoWrapper) {
        let clipboardText = this.TryComplete<ClipboardTextDto>(wrapper);

        if (!clipboardText) {
            return;
        }

        ViewerApp.ClipboardWatcher.SetClipboardText(clipboardText.ClipboardText);
    }
    HandleCursorChange(wrapper: DtoWrapper) {
        let cursorChange = this.TryComplete<CursorChangeDto>(wrapper);

        if (!cursorChange) {
            return;
        }

        UI.UpdateCursor(cursorChange.ImageBytes, cursorChange.HotSpotX, cursorChange.HotSpotY, cursorChange.CssOverride);
    }
    HandleFile(wrapper: DtoWrapper) {
        let file = this.TryComplete<FileDto>(wrapper);

        if (!file) {
            return;
        }

        ReceiveFile(file);
    }
    HandleScreenData(wrapper: DtoWrapper) {
        let screenDataDto = this.TryComplete<ScreenDataDto>(wrapper);

        if (!screenDataDto) {
            return;
        }

        document.title = `${screenDataDto.MachineName} - Remotely Session`;
        UI.ToggleConnectUI(false);
        UI.SetScreenSize(screenDataDto.ScreenWidth, screenDataDto.ScreenHeight);
        UI.UpdateDisplays(screenDataDto.SelectedDisplay, screenDataDto.DisplayNames);
    }

    HandleScreenSize(wrapper: DtoWrapper) {
        let screenSizeDto = this.TryComplete<ScreenSizeDto>(wrapper);

        if (!screenSizeDto) {
            return;
        }

        UI.SetScreenSize(screenSizeDto.Width, screenSizeDto.Height);
    }

    HandleWindowsSessions(wrapper: DtoWrapper) {
        let windowsSessionsDto = this.TryComplete<WindowsSessionsDto>(wrapper);

        if (!windowsSessionsDto) {
            return;
        }

        UI.UpdateWindowsSessions(windowsSessionsDto.WindowsSessions);
    }

    private TryComplete<T>(wrapper: DtoWrapper) : T {
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

        return this.MessagePack.decode(buffers) as T;
    }
}