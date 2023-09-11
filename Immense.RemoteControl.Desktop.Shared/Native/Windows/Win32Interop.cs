using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static Immense.RemoteControl.Desktop.Shared.Native.Windows.ADVAPI32;
using static Immense.RemoteControl.Desktop.Shared.Native.Windows.User32;

namespace Immense.RemoteControl.Desktop.Shared.Native.Windows;

// TODO: Use https://github.com/microsoft/CsWin32 for all p/invokes.
public class Win32Interop
{
    private static nint _lastInputDesktop;

    public static List<WindowsSession> GetActiveSessions()
    {
        var sessions = new List<WindowsSession>();
        var consoleSessionId = Kernel32.WTSGetActiveConsoleSessionId();
        sessions.Add(new WindowsSession()
        {
            Id = consoleSessionId,
            Type = WindowsSessionType.Console,
            Name = "Console",
            Username = GetUsernameFromSessionId(consoleSessionId)
        });

        nint ppSessionInfo = nint.Zero;
        var count = 0;
        var enumSessionResult = WTSAPI32.WTSEnumerateSessions(WTSAPI32.WTS_CURRENT_SERVER_HANDLE, 0, 1, ref ppSessionInfo, ref count);
        var dataSize = Marshal.SizeOf(typeof(WTSAPI32.WTS_SESSION_INFO));
        var current = ppSessionInfo;

        if (enumSessionResult != 0)
        {
            for (int i = 0; i < count; i++)
            {
                var wtsInfo = Marshal.PtrToStructure(current, typeof(WTSAPI32.WTS_SESSION_INFO));
                if (wtsInfo is null)
                {
                    continue;
                }
                var sessionInfo = (WTSAPI32.WTS_SESSION_INFO)wtsInfo;
                current += dataSize;
                if (sessionInfo.State == WTSAPI32.WTS_CONNECTSTATE_CLASS.WTSActive && sessionInfo.SessionID != consoleSessionId)
                {

                    sessions.Add(new WindowsSession()
                    {
                        Id = sessionInfo.SessionID,
                        Name = sessionInfo.pWinStationName,
                        Type = WindowsSessionType.RDP,
                        Username = GetUsernameFromSessionId(sessionInfo.SessionID)
                    });
                }
            }
        }

        return sessions;
    }

    public static string GetCommandLine()
    {
        var commandLinePtr = Kernel32.GetCommandLine();
        return Marshal.PtrToStringAuto(commandLinePtr) ?? string.Empty;
    }

    public static bool GetCurrentDesktop(out string desktopName)
    {
        var inputDesktop = OpenInputDesktop();
        try
        {
            byte[] deskBytes = new byte[256];
            if (!GetUserObjectInformationW(inputDesktop, UOI_NAME, deskBytes, 256, out uint lenNeeded))
            {
                desktopName = string.Empty;
                return false;
            }

            desktopName = Encoding.Unicode.GetString(deskBytes.Take((int)lenNeeded).ToArray()).Replace("\0", "");
            return true;
        }
        finally
        {
            CloseDesktop(inputDesktop);
        }
    }

    public static string GetUsernameFromSessionId(uint sessionId)
    {
        var username = string.Empty;

        if (WTSAPI32.WTSQuerySessionInformation(nint.Zero, sessionId, WTSAPI32.WTS_INFO_CLASS.WTSUserName, out var buffer, out var strLen) && strLen > 1)
        {
            username = Marshal.PtrToStringAnsi(buffer);
            WTSAPI32.WTSFreeMemory(buffer);
        }

        return username ?? string.Empty;
    }

    public static nint OpenInputDesktop()
    {
        return User32.OpenInputDesktop(0, true, ACCESS_MASK.GENERIC_ALL);
    }

    public static bool CreateInteractiveSystemProcess(
        string commandLine,
         int targetSessionId,
         bool forceConsoleSession,
         string desktopName,
         bool hiddenWindow,
         out PROCESS_INFORMATION procInfo)
    {
        uint winlogonPid = 0;
        var hUserTokenDup = nint.Zero;
        var hPToken = nint.Zero;
        var hProcess = nint.Zero;

        procInfo = new PROCESS_INFORMATION();

        // If not force console, find target session.  If not present,
        // use last active session.
        var dwSessionId = Kernel32.WTSGetActiveConsoleSessionId();
        if (!forceConsoleSession)
        {
            var activeSessions = GetActiveSessions();
            if (activeSessions.Any(x => x.Id == targetSessionId))
            {
                dwSessionId = (uint)targetSessionId;
            }
            else
            {
                dwSessionId = activeSessions.Last().Id;
            }
        }

        // Obtain the process ID of the winlogon process that is running within the currently active session.
        var processes = Process.GetProcessesByName("winlogon");
        foreach (Process p in processes)
        {
            if ((uint)p.SessionId == dwSessionId)
            {
                winlogonPid = (uint)p.Id;
            }
        }

        // Obtain a handle to the winlogon process.
        hProcess = Kernel32.OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);

        // Obtain a handle to the access token of the winlogon process.
        if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE, ref hPToken))
        {
            Kernel32.CloseHandle(hProcess);
            return false;
        }

        // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser.
        var sa = new SECURITY_ATTRIBUTES();
        sa.Length = Marshal.SizeOf(sa);

        // Copy the access token of the winlogon process; the newly created token will be a primary token.
        if (!DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup))
        {
            Kernel32.CloseHandle(hProcess);
            Kernel32.CloseHandle(hPToken);
            return false;
        }

        // By default, CreateProcessAsUser creates a process on a non-interactive window station, meaning
        // the window station has a desktop that is invisible and the process is incapable of receiving
        // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
        // interaction with the new process.
        var si = new STARTUPINFO();
        si.cb = Marshal.SizeOf(si);
        si.lpDesktop = @"winsta0\" + desktopName;

        // Flags that specify the priority and creation method of the process.
        uint dwCreationFlags;
        if (hiddenWindow)
        {
            dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_UNICODE_ENVIRONMENT | CREATE_NO_WINDOW;
            si.dwFlags = STARTF_USESHOWWINDOW;
            si.wShowWindow = 0;
        }
        else
        {
            dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_UNICODE_ENVIRONMENT | CREATE_NEW_CONSOLE;
        }

        // Create a new process in the current user's logon session.
        var result = CreateProcessAsUser(
            hUserTokenDup,
            null,
            commandLine,
            ref sa,
            ref sa,
            false,
            dwCreationFlags,
            nint.Zero,
            null,
            ref si,
            out procInfo);

        // Invalidate the handles.
        Kernel32.CloseHandle(hProcess);
        Kernel32.CloseHandle(hPToken);
        Kernel32.CloseHandle(hUserTokenDup);

        return result;
    }

    public static void SetMonitorState(MonitorState state)
    {
        SendMessage(0xFFFF, 0x112, 0xF170, (int)state);
    }

    public static MessageBoxResult ShowMessageBox(nint owner,
        string message,
        string caption,
        MessageBoxType messageBoxType)
    {
        return (MessageBoxResult)MessageBox(owner, message, caption, (long)messageBoxType);
    }

    public static bool SwitchToInputDesktop()
    {
        try
        {
            CloseDesktop(_lastInputDesktop);
            var inputDesktop = OpenInputDesktop();

            if (inputDesktop == nint.Zero)
            {
                return false;
            }

            var result = SetThreadDesktop(inputDesktop) && SwitchDesktop(inputDesktop);
            _lastInputDesktop = inputDesktop;
            return result;
        }
        catch
        {
            return false;
        }
    }

    public static void SetConsoleWindowVisibility(bool isVisible)
    {
        var handle = Kernel32.GetConsoleWindow();

        if (isVisible)
        {
            ShowWindow(handle, (int)SW.SW_SHOW);
        }
        else
        {
            ShowWindow(handle, (int)SW.SW_HIDE);
        }

        Kernel32.CloseHandle(handle);
    }

    public static Result<BackstageSession> StartProcessInBackstage<T>(
        string commandLine,
        string windowStationName,
        ILogger<T> logger,
        out PROCESS_INFORMATION procInfo)
    {
        using var logScope = logger.BeginScope(nameof(StartProcessInBackstage));

        procInfo = new();

        var winstaEnumResult = User32.EnumWindowStations((string windowStation, nint lParam) =>
        {
            logger.LogInformation("Found window station {windowStation}.", windowStation);
            return true;
        },
        nint.Zero);

        if (!winstaEnumResult)
        {
            logger.LogError("Enum winsta failed.");
        }

        var createWinstaResult = CreateWindowStation(windowStationName, 0, ACCESS_MASK.MAXIMUM_ALLOWED, nint.Zero);

        if (createWinstaResult == nint.Zero)
        {
            logger.LogError("Create winsta failed.");
            return Result.Fail<BackstageSession>("Create winsta failed.");
        }

        // When calling CreateDesktop, the calling process must be associated with
        // the target Window station.
        var setProcessWinstaResult = SetProcessWindowStation(createWinstaResult);

        if (!setProcessWinstaResult)
        {
            logger.LogError("Set process winsta failed.");
            return Result.Fail<BackstageSession>("Set process winsta failed.");
        }

        var createDesktopResult = CreateDesktop(
            "default",
            null,
            null,
            0,
            ACCESS_MASK.MAXIMUM_ALLOWED | ACCESS_MASK.DESKTOP_CREATEWINDOW,
            nint.Zero);

        if (createDesktopResult == nint.Zero)
        {
            logger.LogError("Create desktop failed.");
            return Result.Fail<BackstageSession>("Create desktop failed.");
        }

        var si = new STARTUPINFO();
        si.cb = Marshal.SizeOf(si);
        si.lpDesktop = @$"{windowStationName}\default";


        var createProcessResult = Kernel32.CreateProcess(
                null,
                commandLine,
                nint.Zero,
                nint.Zero,
                false,
                NORMAL_PRIORITY_CLASS | CREATE_UNICODE_ENVIRONMENT | CREATE_NEW_CONSOLE,
                nint.Zero,
                null,
                ref si,
                out procInfo);

        if (!createProcessResult)
        {
            logger.LogError("Create process failed.");
            return Result.Fail<BackstageSession>("Create process failed.");
        }

        var session = new BackstageSession(createWinstaResult, createDesktopResult, procInfo);
        return Result.Ok(session);
    }

    private static bool EnumWinstaFunc()
    {

        return true;
    }
}
