using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Native.Windows;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static Immense.RemoteControl.Desktop.Shared.Native.Windows.User32;

namespace Immense.RemoteControl.Desktop.Windows.Services;

[SupportedOSPlatform("windows")]
public class KeyboardMouseInputWin : IKeyboardMouseInput
{
    private readonly IUiDispatcher _dispatcher;
    private readonly ConcurrentQueue<Action> _inputActions = new();
    private readonly ILogger<KeyboardMouseInputWin> _logger;
    private volatile bool _inputBlocked;
    private Thread? _inputProcessingThread;

    public KeyboardMouseInputWin(
        IUiDispatcher dispatcher,
        ILogger<KeyboardMouseInputWin> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    [Flags]
    private enum ShiftState : byte
    {
        None = 0,
        ShiftPressed = 1 << 0,
        CtrlPressed = 1 << 1,
        AltPressed = 1 << 2,
        HankakuPressed = 1 << 3,
        Reserved1 = 1 << 4,
        Reserved2 = 1 << 5,
    }

    public Tuple<double, double> GetAbsolutePercentFromRelativePercent(double percentX, double percentY, IScreenCapturer capturer)
    {
        var absoluteX = capturer.CurrentScreenBounds.Width * percentX + capturer.CurrentScreenBounds.Left - capturer.GetVirtualScreenBounds().Left;
        var absoluteY = capturer.CurrentScreenBounds.Height * percentY + capturer.CurrentScreenBounds.Top - capturer.GetVirtualScreenBounds().Top;
        return new Tuple<double, double>(absoluteX / capturer.GetVirtualScreenBounds().Width, absoluteY / capturer.GetVirtualScreenBounds().Height);
    }

    public Tuple<double, double> GetAbsolutePointFromRelativePercent(double percentX, double percentY, IScreenCapturer capturer)
    {
        var absoluteX = capturer.CurrentScreenBounds.Width * percentX + capturer.CurrentScreenBounds.Left;
        var absoluteY = capturer.CurrentScreenBounds.Height * percentY + capturer.CurrentScreenBounds.Top;
        return new Tuple<double, double>(absoluteX, absoluteY);
    }

    public void Init()
    {
        StartInputProcessingThread();
    }

    public void SendKeyDown(string key)
    {
        TryOnInputDesktop(() =>
        {
            try
            {
                if (key.Length == 1)
                {
                    var character = Convert.ToChar(key);

                    // If a modifier key is pressed, we need to send the virtual key
                    // so the command will execute.  For example, without this,
                    // Ctrl+A would result in simply typing "a".
                    if (IsModKeyPressed())
                    {
                        var vkey = (VirtualKey)VkKeyScan(character);
                        var input = CreateKeyboardInput(vkey);
                        _ = SendInput(1, new INPUT[] { input }, INPUT.Size);
                    }
                    else
                    {
                        var keyCode = Convert.ToUInt16(character);
                        var inputEx = CreateKeyboardInput(keyCode, KEYEVENTF.UNICODE);
                        _ = SendInput(1, new InputEx[] { inputEx }, InputEx.Size);
                    }
                }
                else if (ConvertJavaScriptKeyToVirtualKey(key, out var keyCode))
                {
                    var input = CreateKeyboardInput(keyCode.Value, KEYEVENTF.EXTENDEDKEY);
                    _ = SendInput(1, new INPUT[] { input }, INPUT.Size);
                }
                else
                {
                    _logger.LogWarning("Unable to simulate key input {key}.", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending key down.");
            }
        });
    }

    public void SendKeyUp(string key)
    {
        TryOnInputDesktop(() =>
        {
            try
            {
                if (key.Length == 1)
                {
                    var character = Convert.ToChar(key);

                    if (IsModKeyPressed())
                    {
                        var vkey = (VirtualKey)VkKeyScan(character);
                        var input = CreateKeyboardInput(vkey, KEYEVENTF.KEYUP);
                        _ = SendInput(1, new INPUT[] { input }, INPUT.Size);
                    }
                    else
                    {
                        var keyCode = Convert.ToUInt16(character);
                        var inputEx = CreateKeyboardInput(keyCode, KEYEVENTF.UNICODE | KEYEVENTF.KEYUP);
                        _ = SendInput(1, new InputEx[] { inputEx }, InputEx.Size);
                    }

                }
                else if (ConvertJavaScriptKeyToVirtualKey(key, out var keyCode))
                {
                    var input = CreateKeyboardInput(keyCode.Value, KEYEVENTF.KEYUP | KEYEVENTF.EXTENDEDKEY);
                    _ = SendInput(1, new INPUT[] { input }, INPUT.Size);
                }
                else
                {
                    _logger.LogWarning("Unable to simulate key input {key}.", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending key up.");
            }

        });
    }

    public void SendMouseButtonAction(int button, ButtonAction buttonAction, double percentX, double percentY, IViewer viewer)
    {
        TryOnInputDesktop(() =>
        {
            try
            {
                MOUSEEVENTF mouseEvent;
                switch (button)
                {
                    case 0:
                        switch (buttonAction)
                        {
                            case ButtonAction.Down:
                                mouseEvent = MOUSEEVENTF.LEFTDOWN;
                                break;
                            case ButtonAction.Up:
                                mouseEvent = MOUSEEVENTF.LEFTUP;
                                break;
                            default:
                                return;
                        }
                        break;
                    case 1:
                        switch (buttonAction)
                        {
                            case ButtonAction.Down:
                                mouseEvent = MOUSEEVENTF.MIDDLEDOWN;
                                break;
                            case ButtonAction.Up:
                                mouseEvent = MOUSEEVENTF.MIDDLEUP;
                                break;
                            default:
                                return;
                        }
                        break;
                    case 2:
                        switch (buttonAction)
                        {
                            case ButtonAction.Down:
                                mouseEvent = MOUSEEVENTF.RIGHTDOWN;
                                break;
                            case ButtonAction.Up:
                                mouseEvent = MOUSEEVENTF.RIGHTUP;
                                break;
                            default:
                                return;
                        }
                        break;
                    default:
                        return;
                }
                var xyPercent = GetAbsolutePercentFromRelativePercent(percentX, percentY, viewer.Capturer);
                // Coordinates must be normalized.  The bottom-right coordinate is mapped to 65535.
                var normalizedX = xyPercent.Item1 * 65535D;
                var normalizedY = xyPercent.Item2 * 65535D;
                var union = new InputUnion() { mi = new MOUSEINPUT() { dwFlags = MOUSEEVENTF.ABSOLUTE | mouseEvent | MOUSEEVENTF.VIRTUALDESK, dx = (int)normalizedX, dy = (int)normalizedY, time = 0, mouseData = 0, dwExtraInfo = GetMessageExtraInfo() } };
                var input = new INPUT() { type = InputType.MOUSE, U = union };
                _ = SendInput(1, new INPUT[] { input }, INPUT.Size);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending mouse button.");
            }
        });
    }

    public void SendMouseMove(double percentX, double percentY, IViewer viewer)
    {
        TryOnInputDesktop(() =>
        {
            try
            {
                var xyPercent = GetAbsolutePercentFromRelativePercent(percentX, percentY, viewer.Capturer);
                // Coordinates must be normalized.  The bottom-right coordinate is mapped to 65535.
                var normalizedX = xyPercent.Item1 * 65535D;
                var normalizedY = xyPercent.Item2 * 65535D;
                var union = new InputUnion() { mi = new MOUSEINPUT() { dwFlags = MOUSEEVENTF.ABSOLUTE | MOUSEEVENTF.MOVE | MOUSEEVENTF.VIRTUALDESK, dx = (int)normalizedX, dy = (int)normalizedY, time = 0, mouseData = 0, dwExtraInfo = GetMessageExtraInfo() } };
                var input = new INPUT() { type = InputType.MOUSE, U = union };
                _ = SendInput(1, new INPUT[] { input }, INPUT.Size);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending mouse move.");
            }
        });
    }

    public void SendMouseWheel(int deltaY)
    {
        TryOnInputDesktop(() =>
        {
            try
            {
                if (deltaY < 0)
                {
                    deltaY = -120;
                }
                else if (deltaY > 0)
                {
                    deltaY = 120;
                }
                var union = new InputUnion() { mi = new MOUSEINPUT() { dwFlags = MOUSEEVENTF.WHEEL, dx = 0, dy = 0, time = 0, mouseData = deltaY, dwExtraInfo = GetMessageExtraInfo() } };
                var input = new INPUT() { type = InputType.MOUSE, U = union };
                _ = SendInput(1, new INPUT[] { input }, INPUT.Size);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending mouse wheel.");
            }
        });
    }

    public void SendText(string transferText)
    {
        TryOnInputDesktop(() =>
        {
            try
            {

                var inputs = new List<InputEx>();

                foreach (var character in transferText)
                {
                    var keyCode = Convert.ToUInt16(character);
                    var keyDown = CreateKeyboardInput(keyCode, KEYEVENTF.UNICODE);
                    var keyUp = CreateKeyboardInput(keyCode, KEYEVENTF.UNICODE | KEYEVENTF.KEYUP);
                    inputs.Add(keyDown);
                    inputs.Add(keyUp);
                }

                var result = SendInput((uint)inputs.Count, inputs.ToArray(), InputEx.Size);
                Debug.Assert(result == inputs.Count);

                if (result != inputs.Count)
                {
                    _logger.LogWarning(
                        "Input simulation failed.  Expected inputs: {count}.  " +
                        "Actual inputs sent: {result}.",
                        inputs.Count,
                        result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending text.");
            }
        });
    }

    public void SetKeyStatesUp()
    {
        TryOnInputDesktop(() =>
        {
            var thread = new Thread(() =>
            {
                foreach (VirtualKey key in Enum.GetValues(typeof(VirtualKey)))
                {
                    try
                    {
                        var state = GetKeyState(key);
                        if (state == -127)
                        {
                            var input = CreateKeyboardInput(key, KEYEVENTF.KEYUP);
                            _ = SendInput(1, new INPUT[] { input }, INPUT.Size);
                        }
                    }
                    catch { }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        });
    }

    public void ToggleBlockInput(bool toggleOn)
    {
        _inputActions.Enqueue(() =>
        {
            _inputBlocked = toggleOn;
            var result = BlockInput(toggleOn);
            _logger.LogInformation("Result of ToggleBlockInput set to {toggleOn}: {result}", toggleOn, result);
        });
    }

    private void AddShiftInput(List<INPUT> inputs, ShiftState shiftState, KEYEVENTF keyEvent = default)
    {
        if (shiftState.HasFlag(ShiftState.ShiftPressed))
        {
            inputs.Add(CreateKeyboardInput(VirtualKey.SHIFT, keyEvent));
        }

        if (shiftState.HasFlag(ShiftState.CtrlPressed))
        {
            inputs.Add(CreateKeyboardInput(VirtualKey.CONTROL, keyEvent));
        }

        if (shiftState.HasFlag(ShiftState.AltPressed))
        {
            inputs.Add(CreateKeyboardInput(VirtualKey.MENU, keyEvent));
        }
    }

    private void CheckQueue(CancellationToken cancelToken)
    {
        while (!cancelToken.IsCancellationRequested)
        {
            try
            {
                if (_inputActions.TryDequeue(out var action))
                {
                    action();
                }
            }
            finally
            {
                Thread.Sleep(1);
            }
        }

        _logger.LogInformation("Stopping input processing on thread {threadId}.", Environment.CurrentManagedThreadId);
    }

    private bool ConvertJavaScriptKeyToVirtualKey(string key, [NotNullWhen(true)] out VirtualKey? result)
    {
        result = key switch
        {
            "Down" or "ArrowDown" => VirtualKey.DOWN,
            "Up" or "ArrowUp" => VirtualKey.UP,
            "Left" or "ArrowLeft" => VirtualKey.LEFT,
            "Right" or "ArrowRight" => VirtualKey.RIGHT,
            "Enter" => VirtualKey.RETURN,
            "Esc" or "Escape" => VirtualKey.ESCAPE,
            "Alt" => VirtualKey.MENU,
            "Control" => VirtualKey.CONTROL,
            "Shift" => VirtualKey.SHIFT,
            "PAUSE" => VirtualKey.PAUSE,
            "BREAK" => VirtualKey.PAUSE,
            "Backspace" => VirtualKey.BACK,
            "Tab" => VirtualKey.TAB,
            "CapsLock" => VirtualKey.CAPITAL,
            "Delete" => VirtualKey.DELETE,
            "Home" => VirtualKey.HOME,
            "End" => VirtualKey.END,
            "PageUp" => VirtualKey.PRIOR,
            "PageDown" => VirtualKey.NEXT,
            "NumLock" => VirtualKey.NUMLOCK,
            "Insert" => VirtualKey.INSERT,
            "ScrollLock" => VirtualKey.SCROLL,
            "F1" => VirtualKey.F1,
            "F2" => VirtualKey.F2,
            "F3" => VirtualKey.F3,
            "F4" => VirtualKey.F4,
            "F5" => VirtualKey.F5,
            "F6" => VirtualKey.F6,
            "F7" => VirtualKey.F7,
            "F8" => VirtualKey.F8,
            "F9" => VirtualKey.F9,
            "F10" => VirtualKey.F10,
            "F11" => VirtualKey.F11,
            "F12" => VirtualKey.F12,
            "Meta" => VirtualKey.LWIN,
            "ContextMenu" => VirtualKey.MENU,
            _ => key.Length == 1 ?
                    (VirtualKey)VkKeyScan(Convert.ToChar(key)) :
                    null
        };

        if (result is null)
        {
            _logger.LogWarning("Unable to parse key input: {key}.", key);
            return false;
        }
        return true;
    }

    private INPUT CreateKeyboardInput(
        VirtualKey virtualKey,
        KEYEVENTF keyEvent = default)
    {
        return new INPUT()
        {
            type = InputType.KEYBOARD,
            U = new InputUnion()
            {
                ki = new KEYBDINPUT()
                {
                    wVk = virtualKey,
                    wScan = (ScanCodeShort)MapVirtualKeyEx((uint)virtualKey, VkMapType.MAPVK_VSC_TO_VK_EX, GetKeyboardLayout((uint)Environment.CurrentManagedThreadId)),
                    dwFlags = keyEvent,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };
    }

    private InputEx CreateKeyboardInput(
      ushort unicodeKey,
      KEYEVENTF keyEvent = KEYEVENTF.UNICODE)
    {
        return new InputEx()
        {
            type = InputType.KEYBOARD,
            U = new InputUnionEx()
            {
                ki = new KeybdInputEx()
                {
                    wVk = 0,
                    wScan = unicodeKey,
                    dwFlags = keyEvent,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };
    }

    private (bool Pressed, bool Toggled) GetKeyPressState(VirtualKey vkey)
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeystate#return-value
        var state = GetKeyState(vkey);
        var pressed = state < 0;
        var toggled = (state & 1) != 0;
        return (pressed, toggled);
    }

    private bool IsModKeyPressed()
    {
        var (ctrlPressed, _) = GetKeyPressState(VirtualKey.CONTROL);
        var (altPressed, _) = GetKeyPressState(VirtualKey.MENU);

        // I'm not sure we'll be able to get these to work with a browser front-end.
        //var (lwinPressed, _) = GetKeyPressState(VirtualKey.LWIN);
        //var (rwinPressed, _) = GetKeyPressState(VirtualKey.RWIN);

        return ctrlPressed || altPressed;
    }
    private void StartInputProcessingThread()
    {
        // After BlockInput is enabled, only simulated input coming from the same thread
        // will work.  So we have to start a new thread that runs continuously and
        // processes a queue of input events.
        _inputProcessingThread = new Thread(() =>
        {
            _logger.LogInformation("New input processing thread started on thread {threadId}.", Environment.CurrentManagedThreadId);

            if (_inputBlocked)
            {
                ToggleBlockInput(true);
            }
            CheckQueue(_dispatcher.ApplicationExitingToken);
        });

        _inputProcessingThread.SetApartmentState(ApartmentState.STA);
        _inputProcessingThread.Start();
    }

    private void TryOnInputDesktop(Action inputAction)
    {
        _inputActions.Enqueue(() =>
        {
            try
            {
                if (!Win32Interop.SwitchToInputDesktop())
                {
                    _logger.LogWarning("Desktop switch failed during input processing.");

                    // Thread likely has hooks in current desktop.  SendKeys will create one with no way to unhook it.
                    // Start a new thread for processing input.
                    StartInputProcessingThread();
                    return;
                }
                inputAction();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during input queue processing.");
            }
        });
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ShortHelper
    {
        public ShortHelper(short value)
        {
            Value = value;
        }

        [FieldOffset(0)]
        public short Value;
        [FieldOffset(0)]
        public byte Low;
        [FieldOffset(1)]
        public byte High;
    }
}