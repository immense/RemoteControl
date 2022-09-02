import * as UI from "./UI.js";
import { ViewerApp } from "./App.js";
import { CursorInfo } from "./Models/CursorInfo.js";
import { RemoteControlMode } from "./Enums/RemoteControlMode.js";
import { ShowMessage } from "./UI.js";
import { WindowsSession } from "./Models/WindowsSession.js";
import { DtoType } from "./Enums/DtoType.js";
import { HubConnection } from "./Models/HubConnection.js";
import { ChunkDto, TryComplete } from "./DtoChunker.js";
import { MessagePack } from "./Interfaces/MessagePack.js";
import { DtoWrapper, ScreenCaptureDto } from "./Interfaces/Dtos.js";
import { HandleCaptureReceived } from "./CaptureProcessor.js";
import { HubConnectionState } from "./Enums/HubConnectionState.js";

var signalR = window["signalR"];

export class ViewerHubConnection {
    Connection: HubConnection;
    MessagePack: MessagePack = window['msgpack5']();
    PartialCaptureFrames: Uint8Array[] = [];

 
    Connect() {
        this.Connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/viewer")
            .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.ApplyMessageHandlers(this.Connection);

        this.Connection.start().then(() => {
            this.SendScreenCastRequestToDevice();
        }).catch(err => {
            console.error(err.toString());
            console.log("Connection closed.");
            UI.StatusMessage.innerHTML = `Connection error: ${err.message}`;
            UI.ToggleConnectUI(true);
        });

        this.Connection.onclose(() => {
            UI.ToggleConnectUI(true);
        });

        ViewerApp.ClipboardWatcher.WatchClipboard();
    }

    ChangeWindowsSession(sessionID: number) {
        if (ViewerApp.Mode == RemoteControlMode.Unattended) {
            this.Connection.invoke("ChangeWindowsSession", sessionID);
        }
    }

    InvokeCtrlAltDel() {
        if (this.Connection?.state != HubConnectionState.Connected) {
            return;
        }

        this.Connection.invoke("InvokeCtrlAltDel");
    }

    SendDtoToClient<T>(dto: T, type: DtoType): Promise<any> {

        if (this.Connection?.state != HubConnectionState.Connected) {
            return;
        }

        let chunks = ChunkDto(dto, type);

        for (var i = 0; i < chunks.length; i++) {
            const chunk = this.MessagePack.encode(chunks[i]);
            this.Connection.invoke("SendDtoToClient", chunk);
        }
    }


    async SendScreenCastRequestToDevice() {
        await this.Connection.invoke("SendScreenCastRequestToDevice", ViewerApp.SessionId, ViewerApp.AccessKey, ViewerApp.RequesterName);

    }


    private ApplyMessageHandlers(hubConnection) {
        hubConnection.on("SendDtoToViewer", (dto: ArrayBuffer) => {
            ViewerApp.DtoMessageHandler.ParseBinaryMessage(dto);
        });

        hubConnection.on("ConnectionFailed", () => {
            UI.ConnectButton.removeAttribute("disabled");
            UI.StatusMessage.innerHTML = "Connection failed or was denied.";
            ShowMessage("Connection failed.  Please reconnect.");
            this.Connection.stop();
        });
        hubConnection.on("ConnectionRequestDenied", () => {
            this.Connection.stop();
            UI.StatusMessage.innerHTML = "Connection request denied.";
            ShowMessage("Connection request denied.");
        });
        hubConnection.on("Unauthorized", () => {
            UI.ConnectButton.removeAttribute("disabled");
            UI.StatusMessage.innerHTML = "Authorization failed.";
            ShowMessage("Authorization failed.");
            this.Connection.stop();
        });
        hubConnection.on("ViewerRemoved", () => {
            UI.ConnectButton.removeAttribute("disabled");
            UI.StatusMessage.innerHTML = "The session was stopped by your partner.";
            ShowMessage("Session ended.");
            this.Connection.stop();
        });
        hubConnection.on("SessionIDNotFound", () => {
            UI.ConnectButton.removeAttribute("disabled");
            UI.StatusMessage.innerHTML = "Session ID not found.";
            this.Connection.stop();
        });
        hubConnection.on("ScreenCasterDisconnected", () => {
            UI.StatusMessage.innerHTML = "The host has disconnected.";
            this.Connection.stop();
        });
        hubConnection.on("RelaunchedScreenCasterReady", (newSessionId: string, newAccessKey: string) => {
            ViewerApp.SessionId = newSessionId;
            ViewerApp.AccessKey = newAccessKey;
            this.Connection.stop();
            this.Connect();
        });
      
        hubConnection.on("Reconnecting", () => {
            ShowMessage("Reconnecting...");
        });

        hubConnection.on("CursorChange", (cursor: CursorInfo) => {
            UI.UpdateCursor(cursor.ImageBytes, cursor.HotSpot.X, cursor.HotSpot.Y, cursor.CssOverride);
        });

        hubConnection.on("RequestingScreenCast", () => {
            ShowMessage("Requesting remote control...");
        });

        hubConnection.on("SendStreamReady", () => {
            this.Connection.stream("GetDesktopStream")
                .subscribe({
                    next: (item: Uint8Array) => {
                        let wrapper = this.MessagePack.decode<DtoWrapper>(item);
                        let result = TryComplete<ScreenCaptureDto>(wrapper);

                        if (!result) {
                            return;
                        }

                        HandleCaptureReceived(result);
                    },
                    complete: () => {
                        ShowMessage("Desktop stream ended");
                    },
                    error: (err) => {
                        console.error(err);
                        ShowMessage("Desktop stream error");
                    },
                });
        });
        hubConnection.on("ShowMessage", (message: string) => {
            ShowMessage(message);
        });
        hubConnection.on("WindowsSessions", (windowsSessions: Array<WindowsSession>) => {
            UI.UpdateWindowsSessions(windowsSessions);
        });
    }
}