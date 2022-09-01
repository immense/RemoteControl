import { ViewerApp } from "./App.js";
import { CtrlAltDelDto, KeyDownDto, KeyPressDto, KeyUpDto, MouseDownDto, MouseMoveDto, MouseUpDto, MouseWheelDto, SelectScreenDto, TapDto, ToggleAudioDto, ToggleBlockInputDto, ClipboardTransferDto, FileDto, WindowsSessionsDto, EmptyDto } from "./Interfaces/Dtos.js";
import { CreateGUID } from "./Utilities.js";
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
    ChangeWindowsSession(sessionId) {
        ViewerApp.ViewerHubConnection.ChangeWindowsSession(sessionId);
    }
    SendFrameReceived() {
        var dto = new EmptyDto();
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.FrameReceived);
    }
    SendSelectScreen(displayName) {
        var dto = new SelectScreenDto(displayName);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.SelectScreen);
    }
    SendMouseMove(percentX, percentY) {
        var dto = new MouseMoveDto(percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.MouseMove);
    }
    SendMouseDown(button, percentX, percentY) {
        var dto = new MouseDownDto(button, percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.MouseDown);
    }
    SendMouseUp(button, percentX, percentY) {
        var dto = new MouseUpDto(button, percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.MouseUp);
    }
    SendTap(percentX, percentY) {
        var dto = new TapDto(percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.Tap);
    }
    SendMouseWheel(deltaX, deltaY) {
        var dto = new MouseWheelDto(deltaX, deltaY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.MouseWheel);
    }
    SendKeyDown(key) {
        var dto = new KeyDownDto(key);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.KeyDown);
    }
    SendKeyUp(key) {
        var dto = new KeyUpDto(key);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.KeyUp);
    }
    SendKeyPress(key) {
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
    }
    SendOpenFileTransferWindow() {
        var dto = new EmptyDto();
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.OpenFileTransferWindow);
    }
    async SendFile(buffer, fileName) {
        var messageId = CreateGUID();
        let dto = new FileDto(null, fileName, messageId, false, true);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.File);
        for (var i = 0; i < buffer.byteLength; i += 50000) {
            let dto = new FileDto(buffer.slice(i, i + 50000), fileName, messageId, false, false);
            await ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.File);
            if (i > 0) {
                FileTransferProgress.value = i / buffer.byteLength;
            }
        }
        dto = new FileDto(null, fileName, messageId, true, false);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.File);
    }
    SendToggleAudio(toggleOn) {
        var dto = new ToggleAudioDto(toggleOn);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.ToggleAudio);
    }
    ;
    SendToggleBlockInput(toggleOn) {
        var dto = new ToggleBlockInputDto(toggleOn);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.ToggleBlockInput);
    }
    SendClipboardTransfer(text, typeText) {
        var dto = new ClipboardTransferDto(text, typeText);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto, DtoType.ClipboardTransfer);
    }
}
//# sourceMappingURL=MessageSender.js.map