import { ViewerApp } from "./App.js";
import {
    CtrlAltDelDto,
    KeyDownDto,
    KeyPressDto,
    KeyUpDto,
    MouseDownDto,
    MouseMoveDto,
    MouseUpDto,
    MouseWheelDto,
    SelectScreenDto,
    TapDto,
    ToggleAudioDto,
    ToggleBlockInputDto,
    ClipboardTransferDto,
    FileDto,
    WindowsSessionsDto,
    DtoWrapper,
    EmptyDto,
    FrameReceivedDto
} from "./Interfaces/Dtos.js";
import { CreateGUID, When } from "./Utilities.js";
import { FileTransferProgress } from "./UI.js";
import { DtoType } from "./Enums/DtoType.js";
import { RemoteControlMode } from "./Enums/RemoteControlMode.js";

export class MessageSender {
    GetWindowsSessions() {
        if (ViewerApp.Mode == RemoteControlMode.Unattended) {
            var dto = new WindowsSessionsDto();
            ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.WindowsSessions);
        }
    }
    ChangeWindowsSession(sessionId: number) {
        ViewerApp.ViewerHubConnection.ChangeWindowsSession(sessionId);
    }
    SendFrameReceived(timestamp: number) {
        var dto = new FrameReceivedDto(timestamp);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.FrameReceived);
    }
    SendSelectScreen(displayName: string) {
        var dto = new SelectScreenDto(displayName);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.SelectScreen);
    }
    SendMouseMove(percentX: number, percentY: number) {
        var dto = new MouseMoveDto(percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.MouseMove);
    }
    SendMouseDown(button: number, percentX: number, percentY: number) {
        var dto = new MouseDownDto(button, percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.MouseDown);
    }
    SendMouseUp(button: number, percentX: number, percentY: number) {
        var dto = new MouseUpDto(button, percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.MouseUp);
    }
    SendTap(percentX: number, percentY: number) {
        var dto = new TapDto(percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.Tap);
    }
    SendMouseWheel(deltaX: number, deltaY: number) {
        var dto = new MouseWheelDto(deltaX, deltaY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.MouseWheel);
    }
    SendKeyDown(key: string) {
        var dto = new KeyDownDto(key);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.KeyDown);
    }
    SendKeyUp(key: string) {
        var dto = new KeyUpDto(key);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.KeyUp);
    }
    SendKeyPress(key: string) {
        var dto = new KeyPressDto(key);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.KeyPress);
    }
    SendSetKeyStatesUp() {
        var dto = new EmptyDto();
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.SetKeyStatesUp);
    }
    SendCtrlAltDel() {
        var dto = new CtrlAltDelDto();
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.CtrlAltDel);
        ViewerApp.ViewerHubConnection.InvokeCtrlAltDel();
    }

    SendOpenFileTransferWindow() {
        var dto = new EmptyDto();
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.OpenFileTransferWindow);
    }

    async SendFile(buffer: Uint8Array, fileName: string) {
        var messageId = CreateGUID();
        let dto = new FileDto(null, fileName, messageId, false, true);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.File);
            

        for (var i = 0; i < buffer.byteLength; i += 50_000) {

            let dto = new FileDto(buffer.slice(i, i + 50_000), fileName, messageId, false, false);

            await ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.File);

            if (i > 0) {
                FileTransferProgress.value = i / buffer.byteLength;
            }
        }

        dto = new FileDto(null, fileName, messageId, true, false);

        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.File);
            
    }

    SendToggleAudio(toggleOn: boolean) {
        var dto = new ToggleAudioDto(toggleOn);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.ToggleAudio);
            
    };
    SendToggleBlockInput(toggleOn: boolean) {
        var dto = new ToggleBlockInputDto(toggleOn);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.ToggleBlockInput);
            
    }

    SendClipboardTransfer(text: string, typeText: boolean) {
        var dto = new ClipboardTransferDto(text, typeText);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.ClipboardTransfer);
            
    }
}