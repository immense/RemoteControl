import * as Utilities from "./Utilities.js";
import * as UI from "./UI.js";
import { RemoteControlMode } from "./Enums/RemoteControlMode.js";
import { ClipboardWatcher } from "./ClipboardWatcher.js";
import { DtoMessageHandler } from "./DtoMessageHandler.js";
import { MessageSender } from "./MessageSender.js";
import { SessionRecorder } from "./SessionRecorder.js";
import { ApplyInputHandlers } from "./InputEventHandlers.js";
import { ViewerHubConnection } from "./ViewerHubConnection.js";
import { GetSettings, SetSettings } from "./SettingsService.js";


var queryString = Utilities.ParseSearchString();

export const ViewerApp = {
    ClipboardWatcher: new ClipboardWatcher(),
    MessageSender: new MessageSender(),
    ViewerHubConnection: new ViewerHubConnection(),
    DtoMessageHandler: new DtoMessageHandler(),
    SessionRecorder: new SessionRecorder(),
    SessionId: queryString["sessionId"] ? decodeURIComponent(queryString["sessionId"]) : "",
    AccessKey: queryString["accessKey"] ? decodeURIComponent(queryString["accessKey"]) : "",
    RequesterName: queryString["requesterName"] ? decodeURIComponent(queryString["requesterName"]) : "",
    ViewOnlyMode: queryString["viewonly"] ?
        decodeURIComponent(queryString["viewonly"]).toLowerCase() == "true" :
        false,
    Mode: RemoteControlMode.Unknown,
    Settings: GetSettings(),

    Init: () => {
        ViewerApp.Mode = queryString["mode"] ?
            RemoteControlMode[decodeURIComponent(queryString["mode"])] :
            RemoteControlMode.Attended;

        if (ViewerApp.ViewOnlyMode) {
            UI.ViewOnlyButton.classList.add("toggled");
        }

        ApplyInputHandlers();

        if (UI.RequesterNameInput.value) {
            ViewerApp.RequesterName = UI.RequesterNameInput.value;
        }
        else if (ViewerApp.Settings.displayName) {
            UI.RequesterNameInput.value = ViewerApp.Settings.displayName;
            ViewerApp.RequesterName = ViewerApp.Settings.displayName;
        }

        if (ViewerApp.Mode == RemoteControlMode.Unattended) {
            ViewerApp.ViewerHubConnection.Connect();
            UI.StatusMessage.innerHTML = "Connecting to remote device...";
        }
        else {
            UI.ConnectBox.style.removeProperty("display");
            UI.SessionIDInput.value = ViewerApp.SessionId;
            UI.RequesterNameInput.value = ViewerApp.RequesterName;
        }
    },
    ConnectToClient: () => {
        UI.ConnectButton.disabled = true;
        ViewerApp.SessionId = UI.SessionIDInput.value.split(" ").join("");
        ViewerApp.RequesterName = UI.RequesterNameInput.value;
        ViewerApp.Mode = RemoteControlMode.Attended;
        ViewerApp.ViewerHubConnection.Connect();
        UI.StatusMessage.innerHTML = "Requesting access on remote device...";

        ViewerApp.Settings.displayName = ViewerApp.RequesterName;
        SetSettings(ViewerApp.Settings);
    }
}

window["ViewerApp"] = ViewerApp;