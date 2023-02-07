﻿using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Native.Linux;
using Immense.RemoteControl.Desktop.Shared.Services;

using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.Linux.Services;

public class KeyboardMouseInputLinux : IKeyboardMouseInput
{
    private readonly ILogger<KeyboardMouseInputLinux> _logger;

    private IntPtr Display { get; set; }

    public KeyboardMouseInputLinux(ILogger<KeyboardMouseInputLinux> logger)
    {
        _logger = logger;
    }
    public void Init()
    {
        // Nothing to do here.  The Windows implementation needs to start
        // a processing queue to keep all input simulation on the same
        // thread.  Linux doesn't.
    }

    public void SendKeyDown(string key)
    {
        try
        {
            InitDisplay();
            key = ConvertJavaScriptKeyToX11Key(key);
            var keySim = LibX11.XStringToKeysym(key);
            if (keySim == IntPtr.Zero)
            {
                _logger.LogError("Key not mapped: {key}", key);
                return;
            }

            var keyCode = LibX11.XKeysymToKeycode(Display, keySim);
            LibXtst.XTestFakeKeyEvent(Display, keyCode, true, 0);
            LibX11.XSync(Display, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending key down.");
        }
    }

    public void SendKeyUp(string key)
    {
        try
        {
            InitDisplay();
            key = ConvertJavaScriptKeyToX11Key(key);
            var keySim = LibX11.XStringToKeysym(key);
            if (keySim == IntPtr.Zero)
            {
                _logger.LogError("Key not mapped: {key}", key);
                return;
            }

            var keyCode = LibX11.XKeysymToKeycode(Display, keySim);
            LibXtst.XTestFakeKeyEvent(Display, keyCode, false, 0);
            LibX11.XSync(Display, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending key up.");
        }

    }


    public void SendMouseButtonAction(int button, ButtonAction buttonAction, double percentX, double percentY, IViewer viewer)
    {
        try
        {
            var isPressed = buttonAction == ButtonAction.Down;
            // Browser buttons start at 0.  XTest starts at 1.
            var mouseButton = (uint)(button + 1);

            InitDisplay();
            SendMouseMove(percentX, percentY, viewer);
            LibXtst.XTestFakeButtonEvent(Display, mouseButton, isPressed, 0);
            LibX11.XSync(Display, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending mouse button action.");
        }
    }

    public void SendMouseMove(double percentX, double percentY, IViewer viewer)
    {
        try
        {
            InitDisplay();

            var screenBounds = viewer.Capturer.CurrentScreenBounds;
            LibXtst.XTestFakeMotionEvent(Display,
                LibX11.XDefaultScreen(Display),
                screenBounds.X + (int)(screenBounds.Width * percentX),
                screenBounds.Y + (int)(screenBounds.Height * percentY),
                0);
            LibX11.XSync(Display, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending mouse move.");
        }
    }

    public void SendMouseWheel(int deltaY)
    {
        try
        {
            InitDisplay();
            if (deltaY > 0)
            {
                LibXtst.XTestFakeButtonEvent(Display, 4, true, 0);
                LibXtst.XTestFakeButtonEvent(Display, 4, false, 0);
            }
            else
            {
                LibXtst.XTestFakeButtonEvent(Display, 5, true, 0);
                LibXtst.XTestFakeButtonEvent(Display, 5, false, 0);
            }
            LibX11.XSync(Display, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending mouse wheel.");
        }
    }

    public void SendRightMouseDown(double percentX, double percentY, IViewer viewer)
    {
        try
        {
            InitDisplay();
            SendMouseMove(percentX, percentY, viewer);
            LibXtst.XTestFakeButtonEvent(Display, 3, true, 0);
            LibX11.XSync(Display, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending mouse right down.");
        }
    }

    public void SendRightMouseUp(double percentX, double percentY, IViewer viewer)
    {
        try
        {
            InitDisplay();
            SendMouseMove(percentX, percentY, viewer);
            LibXtst.XTestFakeButtonEvent(Display, 3, false, 0);
            LibX11.XSync(Display, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending mouse right up.");
        }
    }
    public void SendText(string transferText)
    {
        foreach (var key in transferText)
        {
            SendKeyDown(key.ToString());
            SendKeyUp(key.ToString());
        }
    }

    public void SetKeyStatesUp()
    {
        // Not implemented.
    }

    public void ToggleBlockInput(bool toggleOn)
    {
        // Not implemented.
    }

    private string ConvertJavaScriptKeyToX11Key(string key)
    {
        string keySym = key switch
        {
            "ArrowDown" => "Down",
            "ArrowUp" => "Up",
            "ArrowLeft" => "Left",
            "ArrowRight" => "Right",
            "Enter" => "Return",
            "Esc" => "Escape",
            "Alt" => "Alt_L",
            "Control" => "Control_L",
            "Shift" => "Shift_L",
            "PAUSE" => "Pause",
            "BREAK" => "Break",
            "Backspace" => "BackSpace",
            "Tab" => "Tab",
            "CapsLock" => "Caps_Lock",
            "Delete" => "Delete",
            "PageUp" => "Page_Up",
            "PageDown" => "Page_Down",
            "NumLock" => "Num_Lock",
            "ScrollLock" => "Scroll_Lock",
            "ContextMenu" => "Menu",
            " " => "space",
            "!" => "exclam",
            "\"" => "quotedbl",
            "#" => "numbersign",
            "$" => "dollar",
            "%" => "percent",
            "&" => "ampersand",
            "'" => "apostrophe",
            "(" => "parenleft",
            ")" => "parenright",
            "*" => "asterisk",
            "+" => "plus",
            "," => "comma",
            "-" => "minus",
            "." => "period",
            "/" => "slash",
            ":" => "colon",
            ";" => "semicolon",
            "<" => "less",
            "=" => "equal",
            ">" => "greater",
            "?" => "question",
            "@" => "at",
            "[" => "bracketleft",
            "\\" => "backslash",
            "]" => "bracketright",
            "_" => "underscore",
            "`" => "grave",
            "{" => "braceleft",
            "|" => "bar",
            "}" => "braceright",
            "~" => "asciitilde",
            _ => key,
        };
        return keySym;
    }
    private void InitDisplay()
    {
        try
        {
            if (Display == IntPtr.Zero)
            {
                Display = LibX11.XOpenDisplay(string.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while initializing display.");
        }
    }

}
