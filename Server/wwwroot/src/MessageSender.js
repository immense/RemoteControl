import { ViewerApp } from "./App.js";
import { CtrlAltDelDto, KeyDownDto, KeyPressDto, KeyUpDto, MouseDownDto, MouseMoveDto, MouseUpDto, MouseWheelDto, SelectScreenDto, TapDto, ToggleAudioDto, ToggleBlockInputDto, ClipboardTransferDto, FileDto, WindowsSessionsDto, GenericDto } from "./Interfaces/Dtos.js";
import { CreateGUID } from "./Utilities.js";
import { FileTransferProgress } from "./UI.js";
import { DtoType } from "./Enums/BaseDtoType.js";
import { RemoteControlMode } from "./Enums/RemoteControlMode.js";
export class MessageSender {
    GetWindowsSessions() {
        if (ViewerApp.Mode == RemoteControlMode.Unattended) {
            var dto = new WindowsSessionsDto();
            ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
        }
    }
    ChangeWindowsSession(sessionId) {
        ViewerApp.ViewerHubConnection.ChangeWindowsSession(sessionId);
    }
    SendFrameReceived() {
        var dto = new GenericDto(DtoType.FrameReceived);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendSelectScreen(displayName) {
        var dto = new SelectScreenDto(displayName);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendMouseMove(percentX, percentY) {
        var dto = new MouseMoveDto(percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendMouseDown(button, percentX, percentY) {
        var dto = new MouseDownDto(button, percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendMouseUp(button, percentX, percentY) {
        var dto = new MouseUpDto(button, percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendTap(percentX, percentY) {
        var dto = new TapDto(percentX, percentY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendMouseWheel(deltaX, deltaY) {
        var dto = new MouseWheelDto(deltaX, deltaY);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendKeyDown(key) {
        var dto = new KeyDownDto(key);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendKeyUp(key) {
        var dto = new KeyUpDto(key);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendKeyPress(key) {
        var dto = new KeyPressDto(key);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendSetKeyStatesUp() {
        var dto = new GenericDto(DtoType.SetKeyStatesUp);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendCtrlAltDel() {
        var dto = new CtrlAltDelDto();
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendOpenFileTransferWindow() {
        var dto = new GenericDto(DtoType.OpenFileTransferWindow);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    async SendFile(buffer, fileName) {
        var messageId = CreateGUID();
        let dto = new FileDto(null, fileName, messageId, false, true);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
        for (var i = 0; i < buffer.byteLength; i += 50000) {
            let dto = new FileDto(buffer.slice(i, i + 50000), fileName, messageId, false, false);
            await ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
            if (i > 0) {
                FileTransferProgress.value = i / buffer.byteLength;
            }
        }
        dto = new FileDto(null, fileName, messageId, true, false);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendToggleAudio(toggleOn) {
        var dto = new ToggleAudioDto(toggleOn);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    ;
    SendToggleBlockInput(toggleOn) {
        var dto = new ToggleBlockInputDto(toggleOn);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
    SendClipboardTransfer(text, typeText) {
        var dto = new ClipboardTransferDto(text, typeText);
        ViewerApp.ViewerHubConnection.SendDtoToClient(dto);
    }
}
//# sourceMappingURL=MessageSender.js.map