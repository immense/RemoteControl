using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Native.Win32;
using Immense.RemoteControl.Shared.Models;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;

namespace Immense.RemoteControl.Desktop.Windows.Services;

/// <summary>
/// A class that can be used to watch for cursor icon changes.
/// </summary>
public class CursorIconWatcherWin : ICursorIconWatcher
{
    private readonly System.Timers.Timer _changeTimer;

    private User32.CursorInfo _cursorInfo;

    private string _previousCursorHandle = string.Empty;

    public CursorIconWatcherWin()
    {
        _changeTimer = new System.Timers.Timer(25);
        _changeTimer.Elapsed += ChangeTimer_Elapsed;
        _changeTimer.Start();
    }

    public event EventHandler<CursorInfo>? OnChange;

    public CursorInfo GetCurrentCursor()
    {
        try
        {
            var ci = new User32.CursorInfo();
            ci.cbSize = Marshal.SizeOf(ci);
            User32.GetCursorInfo(out ci);
            if (ci.flags == User32.CURSOR_SHOWING)
            {
                if (ci.hCursor.ToString() == Cursors.IBeam.Handle.ToString())
                {
                    return new CursorInfo(Array.Empty<byte>(), Point.Empty, "text");
                }

                using var icon = Icon.FromHandle(ci.hCursor);
                using var ms = new MemoryStream();
                using var cursor = new Cursor(ci.hCursor);
                var hotspot = cursor.HotSpot;
                icon.ToBitmap().Save(ms, ImageFormat.Png);
                return new CursorInfo(ms.ToArray(), hotspot);
            }
            else
            {
                return new CursorInfo(Array.Empty<byte>(), Point.Empty, "default");
            }
        }
        catch
        {
            return new CursorInfo(Array.Empty<byte>(), Point.Empty, "default");
        }
    }

    private void ChangeTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (OnChange == null)
        {
            return;
        }
        try
        {
            _cursorInfo = new User32.CursorInfo();
            _cursorInfo.cbSize = Marshal.SizeOf(_cursorInfo);
            User32.GetCursorInfo(out _cursorInfo);
            if (_cursorInfo.flags == User32.CURSOR_SHOWING)
            {
                var currentCursor = _cursorInfo.hCursor.ToString();
                if (currentCursor != _previousCursorHandle)
                {
                    if (currentCursor == Cursors.IBeam.Handle.ToString())
                    {
                        OnChange?.Invoke(this, new CursorInfo(Array.Empty<byte>(), Point.Empty, "text"));
                    }
                    else
                    {
                        using var icon = Icon.FromHandle(_cursorInfo.hCursor);
                        using var ms = new MemoryStream();
                        using var cursor = new Cursor(_cursorInfo.hCursor);
                        var hotspot = cursor.HotSpot;
                        icon.ToBitmap().Save(ms, ImageFormat.Png);
                        OnChange?.Invoke(this, new CursorInfo(ms.ToArray(), hotspot));
                    }
                    _previousCursorHandle = currentCursor;
                }
            }
            else if (_previousCursorHandle != "0")
            {
                _previousCursorHandle = "0";
                OnChange?.Invoke(this, new CursorInfo(Array.Empty<byte>(), Point.Empty, "default"));
            }
        }
        catch
        {
            OnChange?.Invoke(this, new CursorInfo(Array.Empty<byte>(), Point.Empty, "default"));
        }
    }

}
