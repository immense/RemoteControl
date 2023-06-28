import {
    AudioButton,
    ChangeScreenButton,
    PopupMenus,
    ScreenSelectMenu,
    ClipboardTransferButton,
    ClipboardTransferMenu,
    TypeClipboardButton,
    ConnectButton,
    CtrlAltDelButton,
    DisconnectButton,
    FileTransferButton,
    FileTransferInput,
    FitToScreenButton,
    ScreenViewer,
    BlockInputButton,
    InviteButton,
    KeyboardButton,
    TouchKeyboardTextArea,
    MenuFrame,
    MenuButton,
    ScreenViewerWrapper,
    WindowsSessionSelect,
    FileTransferMenu,
    FileUploadButtton,
    FileDownloadButton,
    ViewOnlyButton,
    FullScreenButton,
    RequesterNameInput,
    SessionIDInput,
    ConnectForm,
    CloseAllPopupMenus,
    ExtrasMenu,
    ExtrasMenuButton,
    WindowsSessionMenuButton,
    WindowsSessionMenu,
    MetricsButton,
    MetricsFrame,
    SetStatusMessage,
    BetaPillPullDown
} from "./UI.js";
import { Sound } from "./Sound.js";
import { ViewerApp } from "./App.js";
import { Point } from "./Models/Point.js";
import { UploadFiles } from "./FileTransferService.js";
import { RemoteControlMode } from "./Enums/RemoteControlMode.js";
import { GetDistanceBetween } from "./Utilities.js";
import { ShowToast } from "./UI.js";

var isDragging: boolean;
var currentPointerDevice: string;
var currentTouchCount: number;
var cancelNextViewerClick: boolean;
var isPinchZooming: boolean;
var startPinchPoint1: Point;
var startPinchPoint2: Point;
var lastPinchDistance: number;
var longPressStarted: boolean;
var longPressStartOffsetX: number;
var longPressStartOffsetY: number;
var lastPinchCenterX: number;
var lastPinchCenterY: number;
var isScrolling: boolean;
var lastScrollTime: number;
var lastScrollTouchY1: number;
var lastScrollTouchY2: number;

export function ApplyInputHandlers() {
    AudioButton.addEventListener("click", (ev) => {
        AudioButton.classList.toggle("toggled");
        var toggleOn = AudioButton.classList.contains("toggled");
        if (toggleOn) {
            Sound.Init();
        }
        ViewerApp.MessageSender.SendToggleAudio(toggleOn);
    });
    ChangeScreenButton.addEventListener("click", (ev) => {
        ev.stopPropagation();

        CloseAllPopupMenus(ScreenSelectMenu.id);

        // This could be put into a re-usable "openPopup" function that takes
        // "target element" and "placement" as inputs, but all this is
        // temporary, so I don't think it's worth the time.
        const x = ChangeScreenButton.getBoundingClientRect().left;
        const left = `${x.toFixed(0)}px`;
        const y = ChangeScreenButton.getBoundingClientRect().bottom;
        const top = `${y.toFixed(0)}px`;

        ScreenSelectMenu.style.left = left;
        ScreenSelectMenu.style.top = top;
        ScreenSelectMenu.classList.toggle("open");

        window.addEventListener("click", () => {
            CloseAllPopupMenus(null);
        }, { once: true });
    });
    ClipboardTransferButton.addEventListener("click", (ev) => {
        ev.stopPropagation();

        CloseAllPopupMenus(ClipboardTransferMenu.id);

        const x = ClipboardTransferButton.getBoundingClientRect().left;
        const left = `${x.toFixed(0)}px`;
        const y = ClipboardTransferButton.getBoundingClientRect().bottom;
        const top = `${y.toFixed(0)}px`;

        ClipboardTransferMenu.style.left = left;
        ClipboardTransferMenu.style.top = top;
        ClipboardTransferMenu.classList.toggle("open");

        window.addEventListener("click", () => {
            CloseAllPopupMenus(null);
        }, { once: true });
    });
    ViewOnlyButton.addEventListener("click", () => {
        ViewOnlyButton.classList.toggle("toggled");
        ViewerApp.ViewOnlyMode = ViewOnlyButton.classList.contains("toggled");
    });
    TypeClipboardButton.addEventListener("click", (ev) => {
        if (!location.protocol.includes("https") &&
            !location.origin.includes("localhost")) {
            alert("Clipboard API only works in a secure context (i.e. HTTPS or localhost).");
            return;
        }

        if (!navigator.clipboard?.readText) {
            alert("Clipboard access isn't supported on this browser.");
            return;
        }

        if (ViewerApp.ViewOnlyMode) {
            alert("View-only mode is enabled.");
            return;
        }

        navigator.clipboard.readText().then(text => {
            ViewerApp.MessageSender.SendClipboardTransfer(text, true);
            ShowToast("Clipboard sent!");
        }, reason => {
            alert("Unable to read clipboard.  Please check your permissions.");
            console.log("Unable to read clipboard.  Reason: " + reason);
        });
    });
    ConnectButton.addEventListener("click", () => {
        if (!ConnectForm.checkValidity()) {
            return;
        }
        ViewerApp.ConnectToClient();
    });
    CtrlAltDelButton.addEventListener("click", () => {
        if (ViewerApp.ViewOnlyMode) {
            alert("View-only mode is enabled.");
            return;
        }

        CloseAllPopupMenus(null);
        ViewerApp.MessageSender.SendCtrlAltDel();
    });
    DisconnectButton.addEventListener("click", (ev) => {
        ConnectButton.removeAttribute("disabled");
        ConnectButton.innerText = "Connect";
        SetStatusMessage("Connection closed.");
        ViewerApp.ViewerHubConnection.Connection.stop();
        if (location.search.includes("fromApi=true")) {
            window.close();
        }
    });

    [SessionIDInput, RequesterNameInput].forEach(x => {
        x.addEventListener("keypress", (ev: KeyboardEvent) => {
            if (!SessionIDInput.value || !RequesterNameInput.value) {
                return;
            }

            if (ev.key.toLowerCase() == "enter") {
                ViewerApp.ConnectToClient();
            }
        })

        x.addEventListener("input", () => {
            if (!SessionIDInput.value || !RequesterNameInput.value) {
                ConnectButton.setAttribute("disabled", "disabled");
            }
            else {
                ConnectButton.removeAttribute("disabled");
            }
        });
    });
    ExtrasMenuButton.addEventListener("click", (ev) => {
        ev.stopPropagation();

        CloseAllPopupMenus(ExtrasMenu.id);

        const x = document.body.clientWidth - ExtrasMenuButton.getBoundingClientRect().right;
        const right = `${x.toFixed(0)}px`;
        const y = ExtrasMenuButton.getBoundingClientRect().bottom;
        const top = `${y.toFixed(0)}px`;

        ExtrasMenu.style.right = right;
        ExtrasMenu.style.top = top;
        ExtrasMenu.classList.toggle("open");

        window.addEventListener("click", () => {
            CloseAllPopupMenus(null);
        }, { once: true });
    });
    FileTransferButton.addEventListener("click", (ev) => {
        ev.stopPropagation();

        const x = document.body.clientWidth - FileTransferButton.getBoundingClientRect().right;
        const right = `${x.toFixed(0)}px`;
        const y = FileTransferButton.getBoundingClientRect().bottom;
        const top = `${y.toFixed(0)}px`;

        FileTransferMenu.style.right = right;
        FileTransferMenu.style.top = top;
        FileTransferMenu.classList.toggle("open");
        const buttonZindex = Number.parseInt(getComputedStyle(FileTransferButton.parentElement).zIndex);
        FileTransferMenu.style.zIndex = `${buttonZindex + 1}`;

        window.addEventListener("click", () => {
            CloseAllPopupMenus(null);
        }, { once: true });
    });
    FileUploadButtton.addEventListener("click", (ev) => {
        FileTransferInput.click();
    });
    FileDownloadButton.addEventListener("click", (ev) => {
        if (ViewerApp.ViewOnlyMode) {
            alert("View-only mode is enabled.");
            return;
        }

        ViewerApp.MessageSender.SendOpenFileTransferWindow();
    });
    FileTransferInput.addEventListener("change", (ev) => {
        UploadFiles(FileTransferInput.files);
    });
    FitToScreenButton.addEventListener("click", (ev) => {
        FitToScreenButton.classList.toggle("toggled");
        if (FitToScreenButton.classList.contains("toggled")) {
            ScreenViewer.classList.add("fit");
        }
        else {
            ScreenViewer.classList.remove("fit");
        }
    });
    FullScreenButton.addEventListener("click", () => {
        FullScreenButton.classList.toggle("toggled");

        if (FullScreenButton.classList.contains("toggled")) {
            document.body.requestFullscreen();
        }
        else {
            document.exitFullscreen();
        }
    })
    BlockInputButton.addEventListener("click", (ev) => {
        if (ViewerApp.ViewOnlyMode) {
            alert("View-only mode is enabled.");
            return;
        }
        BlockInputButton.classList.toggle("toggled");
        if (BlockInputButton.classList.contains("toggled")) {
            ViewerApp.MessageSender.SendToggleBlockInput(true);
        }
        else {
            ViewerApp.MessageSender.SendToggleBlockInput(false);
        }
    });
    InviteButton.addEventListener("click", (ev) => {
        var url = "";
        if (ViewerApp.Mode == RemoteControlMode.Attended) {
            url = `${location.origin}${location.pathname}?sessionId=${ViewerApp.SessionId}`;
        }
        else {
            url = `${location.origin}${location.pathname}?mode=Unattended&sessionId=${ViewerApp.SessionId}&accessKey=${ViewerApp.AccessKey}`;
        }
        ViewerApp.ClipboardWatcher.SetClipboardText(url);
        ShowToast("Link copied to clipboard.");
    });
    KeyboardButton.addEventListener("click", (ev) => {
        CloseAllPopupMenus(null);
        TouchKeyboardTextArea.focus();
        TouchKeyboardTextArea.setSelectionRange(TouchKeyboardTextArea.value.length, TouchKeyboardTextArea.value.length);
    });
    MenuButton.addEventListener("click", (ev) => {
        MenuFrame.classList.toggle("open");
        MenuButton.classList.toggle("open");

        if (MenuFrame.classList.contains("open")) {
            BetaPillPullDown.classList.add("d-none");
        }
        else {
            BetaPillPullDown.classList.remove("d-none");
        }
        CloseAllPopupMenus(null);
    });

    MetricsButton.addEventListener("click", () => {
        MetricsFrame.classList.toggle("d-none");
    });
    ScreenViewer.addEventListener("pointermove", function (e: PointerEvent) {
        currentPointerDevice = e.pointerType;
    });

    ScreenViewer.addEventListener("pointerdown", function (e: PointerEvent) {
        currentPointerDevice = e.pointerType;
    });

    ScreenViewer.addEventListener("pointerenter", function (e: PointerEvent) {
        currentPointerDevice = e.pointerType;
    });

    ScreenViewer.addEventListener("mousemove", function (e: MouseEvent) {
        e.preventDefault();

        if (ViewerApp.ViewOnlyMode) {
            return;
        }

        var percentX = e.offsetX / ScreenViewer.clientWidth;
        var percentY = e.offsetY / ScreenViewer.clientHeight;
        ViewerApp.MessageSender.SendMouseMove(percentX, percentY);
    });


    ScreenViewer.addEventListener("mousedown", function (e: MouseEvent) {
        if (currentPointerDevice == "touch") {
            return;
        }

        e.preventDefault();

        if (ViewerApp.ViewOnlyMode) {
            return;
        }

        var percentX = e.offsetX / ScreenViewer.clientWidth;
        var percentY = e.offsetY / ScreenViewer.clientHeight;
        ViewerApp.MessageSender.SendMouseDown(e.button, percentX, percentY);
    });

    ScreenViewer.addEventListener("mouseup", function (e: MouseEvent) {
        if (currentPointerDevice == "touch") {
            return;
        }

        e.preventDefault();

        if (ViewerApp.ViewOnlyMode) {
            return;
        }

        var percentX = e.offsetX / ScreenViewer.clientWidth;
        var percentY = e.offsetY / ScreenViewer.clientHeight;
        ViewerApp.MessageSender.SendMouseUp(e.button, percentX, percentY);
    });

    ScreenViewer.addEventListener("click", function (e: MouseEvent) {
        if (cancelNextViewerClick) {
            cancelNextViewerClick = false;
            return;
        }
        if (currentPointerDevice == "mouse") {
            e.preventDefault();
            e.stopPropagation();
        }
        else if (currentPointerDevice == "touch" && currentTouchCount == 0) {
            if (ViewerApp.ViewOnlyMode) {
                return;
            }

            var percentX = e.offsetX / ScreenViewer.clientWidth;
            var percentY = e.offsetY / ScreenViewer.clientHeight;
            ViewerApp.MessageSender.SendTap(percentX, percentY);
        }
    });

    ScreenViewer.addEventListener("touchstart", function (e: TouchEvent) {
        currentTouchCount = e.touches.length;

        if (currentTouchCount > 1) {
            cancelNextViewerClick = true;
        }
        if (currentTouchCount == 2) {
            lastScrollTouchY1 = e.touches[0].pageY;
            lastScrollTouchY2 = e.touches[1].pageY;
            startPinchPoint1 = { X: e.touches[0].pageX, Y: e.touches[0].pageY, IsEmpty: false };
            startPinchPoint2 = { X: e.touches[1].pageX, Y: e.touches[1].pageY, IsEmpty: false };
            lastPinchDistance = GetDistanceBetween(startPinchPoint1.X,
                startPinchPoint1.Y,
                startPinchPoint2.X,
                startPinchPoint2.Y);
            lastPinchCenterX = (startPinchPoint1.X + startPinchPoint2.X) / 2;
            lastPinchCenterY = (startPinchPoint1.Y + startPinchPoint2.Y) / 2;
        }
        isDragging = false;
        var focusedInput = document.querySelector("input:focus") as HTMLInputElement;
        if (focusedInput) {
            focusedInput.blur();
        }
    });


    ScreenViewer.addEventListener("touchmove", function (e: TouchEvent) {
        currentTouchCount = e.touches.length;

        if (e.touches.length == 1 && longPressStarted && !isDragging) {
            e.preventDefault();
            e.stopPropagation();

            if (ViewerApp.ViewOnlyMode) {
                return;
            }

            const rect = ScreenViewer.getBoundingClientRect();
            const offsetX = e.touches[0].pageX - rect.left;
            const offsetY = e.touches[0].pageY - rect.top;

            const moveDistance = GetDistanceBetween(
                longPressStartOffsetX,
                longPressStartOffsetY,
                offsetX,
                offsetY);

            if (moveDistance > 5) {
                isDragging = true;
                const percentX = (e.touches[0].pageX - ScreenViewer.getBoundingClientRect().left) / ScreenViewer.clientWidth;
                const percentY = (e.touches[0].pageY - ScreenViewer.getBoundingClientRect().top) / ScreenViewer.clientHeight;
                ViewerApp.MessageSender.SendMouseMove(percentX, percentY);
                ViewerApp.MessageSender.SendMouseDown(0, percentX, percentY);
                return;
            }
        }

        if (e.touches.length == 2) {
            let touchMove1 = lastScrollTouchY1 - e.touches[0].pageY;
            let touchMove2 = lastScrollTouchY2 - e.touches[1].pageY;

            if (!isPinchZooming && (isScrolling || touchMove1 * touchMove2 > 0)) {
                // Both touch points are moving in the same direction.  We're doing a scroll.

                if (!isScrolling) {
                    // If this is the start of scrolling, move the mouse to our touch point so
                    // the scroll wheel action will target the intended element on screen.
                    var screenViewerLeft = ScreenViewer.getBoundingClientRect().left;
                    var screenViewerTop = ScreenViewer.getBoundingClientRect().top;
                    var pagePercentX = (e.touches[0].pageX - screenViewerLeft) / ScreenViewer.clientWidth;
                    var pagePercentY = (e.touches[0].pageY - screenViewerTop) / ScreenViewer.clientHeight;
                    ViewerApp.MessageSender.SendMouseMove(pagePercentX, pagePercentY);
                }

                isScrolling = true;
                if (Date.now() - lastScrollTime < 100) {
                    return;
                }
                lastScrollTime = Date.now();
                let yMove = Math.max(-1, Math.min(touchMove1, 1));
                ViewerApp.MessageSender.SendMouseWheel(0, yMove);
                lastScrollTouchY1 = e.touches[0].pageY;
                return;
            }


            var pinchPoint1 = {
                X: e.touches[0].pageX,
                Y: e.touches[0].pageY,
                IsEmpty: false
            };
            var pinchPoint2 = {
                X: e.touches[1].pageX,
                Y: e.touches[1].pageY,
                IsEmpty: false
            };
            var pinchDistance = GetDistanceBetween(pinchPoint1.X,
                pinchPoint1.Y,
                pinchPoint2.X,
                pinchPoint2.Y);


            var pinchCenterX = (pinchPoint1.X + pinchPoint2.X) / 2;
            var pinchCenterY = (pinchPoint1.Y + pinchPoint2.Y) / 2;

            ScreenViewerWrapper.scrollBy(lastPinchCenterX - pinchCenterX,
                lastPinchCenterY - pinchCenterY);

            lastPinchCenterX = pinchCenterX;
            lastPinchCenterY = pinchCenterY;

            if (Math.abs(pinchDistance - lastPinchDistance) > 5) {
                isPinchZooming = true;
                if (FitToScreenButton.classList.contains("toggled")) {
                    FitToScreenButton.click();
                }

                var currentWidth = ScreenViewer.clientWidth;
                var currentHeight = ScreenViewer.clientHeight;

                var clientAdjustedScrollLeftPercent = (ScreenViewerWrapper.scrollLeft + (ScreenViewerWrapper.clientWidth * .5)) / ScreenViewerWrapper.scrollWidth;
                var clientAdjustedScrollTopPercent = (ScreenViewerWrapper.scrollTop + (ScreenViewerWrapper.clientHeight * .5)) / ScreenViewerWrapper.scrollHeight;

                var currentWidthPercent = Number(ScreenViewer.style.width.slice(0, -1));
                var newWidthPercent = Math.max(100, (currentWidthPercent + (pinchDistance - lastPinchDistance) * (currentWidthPercent / 100)));
                newWidthPercent = Math.min(5000, newWidthPercent);
                ScreenViewer.style.width = String(newWidthPercent) + "%";

                var heightChange = ScreenViewer.clientHeight - currentHeight;
                var widthChange = ScreenViewer.clientWidth - currentWidth;

                var pinchAdjustX = pinchCenterX / window.innerWidth - .5;
                var pinchAdjustY = pinchCenterY / window.innerHeight - .5;

                var scrollByX = widthChange * (clientAdjustedScrollLeftPercent + (pinchAdjustX * ScreenViewerWrapper.clientWidth / ScreenViewerWrapper.scrollWidth));
                var scrollByY = heightChange * (clientAdjustedScrollTopPercent + (pinchAdjustY * ScreenViewerWrapper.clientHeight / ScreenViewerWrapper.scrollHeight));

                ScreenViewerWrapper.scrollBy(scrollByX, scrollByY);

                lastPinchDistance = pinchDistance;
            }
            return;
        }
        else if (isDragging) {
            e.preventDefault();
            e.stopPropagation();

            if (ViewerApp.ViewOnlyMode) {
                return;
            }

            var screenViewerLeft = ScreenViewer.getBoundingClientRect().left;
            var screenViewerTop = ScreenViewer.getBoundingClientRect().top;
            var pagePercentX = (e.touches[0].pageX - screenViewerLeft) / ScreenViewer.clientWidth;
            var pagePercentY = (e.touches[0].pageY - screenViewerTop) / ScreenViewer.clientHeight;
            ViewerApp.MessageSender.SendMouseMove(pagePercentX, pagePercentY);
        }
    });

    ScreenViewer.addEventListener("touchend", function (e: TouchEvent) {
        currentTouchCount = e.touches.length;

        if (currentTouchCount == 0) {
            cancelNextViewerClick = false;
            isPinchZooming = false;
            isScrolling = false;
            lastScrollTouchY1 = null;
            lastScrollTouchY2 = null;
            startPinchPoint1 = null;
            startPinchPoint2 = null;
        }

        var percentX = (e.changedTouches[0].pageX - ScreenViewer.getBoundingClientRect().left) / ScreenViewer.clientWidth;
        var percentY = (e.changedTouches[0].pageY - ScreenViewer.getBoundingClientRect().top) / ScreenViewer.clientHeight;

        if (longPressStarted && !isDragging && !ViewerApp.ViewOnlyMode) {
            ViewerApp.MessageSender.SendMouseDown(2, percentX, percentY);
            ViewerApp.MessageSender.SendMouseUp(2, percentX, percentY);
        }


        if (isDragging && !ViewerApp.ViewOnlyMode) {
            ViewerApp.MessageSender.SendMouseUp(0, percentX, percentY);
        }

        longPressStarted = false;
        isDragging = false;
    });


    ScreenViewer.addEventListener("contextmenu", (ev) => {
        ev.preventDefault();

        if (ViewerApp.ViewOnlyMode) {
            return;
        }

        if (currentPointerDevice == "touch") {
            // We're either starting a right-click or left-button drag.
            // Either way, we'll move the cursor to the initial touch point.
            const percentX = (ev.pageX - ScreenViewer.getBoundingClientRect().left) / ScreenViewer.clientWidth;
            const percentY = (ev.pageY - ScreenViewer.getBoundingClientRect().top) / ScreenViewer.clientHeight;
            ViewerApp.MessageSender.SendMouseMove(percentX, percentY);

            longPressStarted = true;
            longPressStartOffsetX = ev.offsetX;
            longPressStartOffsetY = ev.offsetY;
        }
    });

    ScreenViewer.addEventListener("wheel", function (e: WheelEvent) {
        e.preventDefault();
        if (ViewerApp.ViewOnlyMode) {
            return;
        }
        ViewerApp.MessageSender.SendMouseWheel(e.deltaX, e.deltaY);
    });


    TouchKeyboardTextArea.addEventListener("input", (ev) => {
        if (ViewerApp.ViewOnlyMode) {
            return;
        }

        if (TouchKeyboardTextArea.value.length == 1) {
            ViewerApp.MessageSender.SendKeyPress("Backspace");
        }
        else if (TouchKeyboardTextArea.value.endsWith("\n")) {
            ViewerApp.MessageSender.SendKeyPress("Enter");
        }
        else if (TouchKeyboardTextArea.value.endsWith(" ")) {
            ViewerApp.MessageSender.SendKeyPress(" ");
        }
        else {
            var input = TouchKeyboardTextArea.value.trim().substring(1);
            for (var i = 0; i < input.length; i++) {
                var character = input.charAt(i);
                var sendShift = character.match(/[A-Z~!@#$%^&*()_+{}|<>?]/);
                if (sendShift) {
                    ViewerApp.MessageSender.SendKeyDown("Shift");
                }

                ViewerApp.MessageSender.SendKeyPress(character);

                if (sendShift) {
                    ViewerApp.MessageSender.SendKeyUp("Shift");
                }
            }
        }

        window.setTimeout(() => {
            TouchKeyboardTextArea.value = " #";
            TouchKeyboardTextArea.setSelectionRange(TouchKeyboardTextArea.value.length, TouchKeyboardTextArea.value.length);
        });
    });
    WindowsSessionMenuButton.addEventListener("click", (ev) => {
        ev.stopPropagation();

        CloseAllPopupMenus(WindowsSessionMenu.id);

        const x = document.body.clientWidth - WindowsSessionMenuButton.getBoundingClientRect().right;
        const right = `${x.toFixed(0)}px`;
        const y = WindowsSessionMenuButton.getBoundingClientRect().bottom;
        const top = `${y.toFixed(0)}px`;

        WindowsSessionMenu.style.right = right;
        WindowsSessionMenu.style.top = top;
        WindowsSessionMenu.classList.toggle("open");

        window.addEventListener("click", () => {
            CloseAllPopupMenus(null);
        }, { once: true });
    });
    WindowsSessionSelect.addEventListener("click", ev => {
        ev.stopPropagation();
    });
    WindowsSessionSelect.addEventListener("focus", () => {
        ViewerApp.MessageSender.GetWindowsSessions();
    });
    WindowsSessionSelect.addEventListener("change", () => {
        SetStatusMessage("Switching sessions");
        ShowToast("Switching sessions");
        ViewerApp.MessageSender.ChangeWindowsSession(Number(WindowsSessionSelect.selectedOptions[0].value));
    });

    window.addEventListener("keydown", function (e) {
        if (document.querySelector("input:focus") || document.querySelector("textarea:focus")) {
            return;
        }
        if (ViewerApp.ViewOnlyMode) {
            return;
        }
        if (!e.ctrlKey || !e.shiftKey || e.key.toLowerCase() != "i") {
            e.preventDefault();
        }
        ViewerApp.MessageSender.SendKeyDown(e.key);
    });
    window.addEventListener("keyup", function (e) {
        if (document.querySelector("input:focus") || document.querySelector("textarea:focus")) {
            return;
        }
        e.preventDefault();
        if (ViewerApp.ViewOnlyMode) {
            return;
        }
        ViewerApp.MessageSender.SendKeyUp(e.key);
    });

    window.addEventListener("blur", () => {
        if (ViewerApp.ViewOnlyMode) {
            return;
        }
        ViewerApp.MessageSender.SendSetKeyStatesUp();
    });

    window.addEventListener("touchstart", () => {
        KeyboardButton.classList.remove("d-none");
    });

    window.ondragover = function (e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = "copy";
    };
    window.ondrop = function (e) {
        e.preventDefault();
        if (e.dataTransfer.files.length < 1) {
            return;
        }
        UploadFiles(e.dataTransfer.files);
    };
}