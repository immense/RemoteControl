import * as UI from "./UI.js";
import { ViewerApp } from "./App.js";
import { CursorInfo } from "./Models/CursorInfo.js";
import { RemoteControlMode } from "./Enums/RemoteControlMode.js";
import { ShowToast } from "./UI.js";
import { WindowsSession } from "./Models/WindowsSession.js";
import { DtoType } from "./Enums/DtoType.js";
import { HubConnection } from "./Models/HubConnection.js";
import { ChunkDto } from "./DtoChunker.js";
import { MessagePack } from "./Interfaces/MessagePack.js";
import { ProcessStream } from "./CaptureProcessor.js";
import { HubConnectionState } from "./Enums/HubConnectionState.js";
import { StreamingState } from "./Models/StreamingState.js";
import { Result } from "./Models/Result.js";

const MsgPack: MessagePack = window["MessagePack"];

var signalR = window["signalR"];

export class ViewerHubConnection {
    Connection: HubConnection;
    PartialCaptureFrames: Uint8Array[] = [];


    Connect() {
        if (this.Connection) {
            this.Connection.stop();
        }

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

    async SendDtoToClient<T>(dto: T, type: DtoType): Promise<any> {

        if (this.Connection?.state != HubConnectionState.Connected) {
            return;
        }

        let chunks = ChunkDto(dto, type);

        for (var i = 0; i < chunks.length; i++) {
            const chunk = MsgPack.encode(chunks[i]);
            await this.Connection.invoke("SendDtoToClient", chunk);
        }
    }


    async SendScreenCastRequestToDevice() {
        const result = await this.Connection.invoke<Result>("SendScreenCastRequestToDevice", ViewerApp.SessionId, ViewerApp.AccessKey, ViewerApp.RequesterName);
        if (!result.IsSuccess) {
            this.Connection.stop();
            UI.SetStatusMessage(result.Reason);
            return;
        }

        const streamingState = new StreamingState();
        ProcessStream(streamingState);

        this.Connection.stream("GetDesktopStream")
            .subscribe({
                next: (chunk: Uint8Array) => {
                    streamingState.ReceivedChunks.push(chunk);
                },
                complete: () => {
                    streamingState.StreamEnded = true;
                    UI.ToggleConnectUI(true);
                },
                error: (err) => {
                    console.warn(err);
                    streamingState.StreamEnded = true;
                    UI.ToggleConnectUI(true);
                },
            });

    }


    private ApplyMessageHandlers(hubConnection) {
        hubConnection.on("SendDtoToViewer", async (dto: ArrayBuffer) => {
            await ViewerApp.DtoMessageHandler.ParseBinaryMessage(dto);
        });

        hubConnection.on("ConnectionFailed", () => {
            UI.ConnectButton.removeAttribute("disabled");
            UI.ConnectButton.innerText = "Connect";
            UI.SetStatusMessage("Connection failed or was denied.");
            ShowToast("Connection failed.  Please reconnect.");
            this.Connection.stop();
        });
        hubConnection.on("ReconnectFailed", () => {
            UI.ConnectButton.removeAttribute("disabled");
            UI.ConnectButton.innerText = "Connect";
            UI.SetStatusMessage("Unable to reconnect.");
            ShowToast("Unable to reconnect.");
            this.Connection.stop();
        });
        hubConnection.on("ConnectionRequestDenied", () => {
            UI.ConnectButton.innerText = "Connect";
            this.Connection.stop();
            UI.SetStatusMessage("Connection request denied.");
            ShowToast("Connection request denied.");
        });
        hubConnection.on("ViewerRemoved", () => {
            UI.ConnectButton.removeAttribute("disabled");
            UI.ConnectButton.innerText = "Connect";
            UI.SetStatusMessage("The session was stopped by your partner.");
            ShowToast("Session ended");
            this.Connection.stop();
        });
        hubConnection.on("ScreenCasterDisconnected", () => {
            UI.SetStatusMessage("The host has disconnected.");
            this.Connection.stop();
        });
        hubConnection.on("RelaunchedScreenCasterReady", (newSessionId: string, newAccessKey: string) => {
            const newUrl =
                `${location.origin}${location.pathname}` +
                `?mode=Unattended&sessionId=${newSessionId}&accessKey=${newAccessKey}&viewOnly=${ViewerApp.ViewOnlyMode}`;
            location.assign(newUrl);
        });

        hubConnection.on("Reconnecting", () => {
            UI.SetStatusMessage("Reconnecting");
            ShowToast("Reconnecting");
        });

        hubConnection.on("CursorChange", (cursor: CursorInfo) => {
            UI.UpdateCursor(cursor.ImageBytes, cursor.HotSpot.X, cursor.HotSpot.Y, cursor.CssOverride);
        });

        hubConnection.on("ShowMessage", (message: string) => {
            ShowToast(message);
            UI.SetStatusMessage(message);
        });
        hubConnection.on("WindowsSessions", (windowsSessions: Array<WindowsSession>) => {
            UI.UpdateWindowsSessions(windowsSessions);
        });
        hubConnection.on("PingViewer", () => "Pong");
    }
}
