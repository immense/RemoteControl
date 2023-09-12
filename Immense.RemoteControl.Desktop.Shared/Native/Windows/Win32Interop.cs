using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Extensions;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using static Immense.RemoteControl.Desktop.Shared.Native.Windows.ADVAPI32;
using static Immense.RemoteControl.Desktop.Shared.Native.Windows.User32;

namespace Immense.RemoteControl.Desktop.Shared.Native.Windows;

// TODO: Use https://github.com/microsoft/CsWin32 for all p/invokes.
[SupportedOSPlatform("windows")]
public class Win32Interop
{
    private static nint _lastInputDesktop;


    public static bool CreateInteractiveSystemProcess(
    string commandLine,
    int targetSessionId,
    bool forceConsoleSession,
    string desktopName,
    bool hiddenWindow,
    out PROCESS_INFORMATION procInfo)
    {
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

        var dupResult = TryDuplicateProcessToken(
            "winlogon",
            dwSessionId,
            out var sa,
            out var sourceToken,
            out var processHandle,
            out var duplicateToken);

        if (!dupResult)
        {
            Kernel32.CloseHandle(processHandle);
            Kernel32.CloseHandle(sourceToken);
            procInfo = default;
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
            duplicateToken,
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
        Kernel32.CloseHandle(processHandle);
        Kernel32.CloseHandle(sourceToken);
        Kernel32.CloseHandle(duplicateToken);

        return result;
    }

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

    public static bool GetCurrentDesktopName([NotNullWhen(true)] out string? desktopName)
    {
        desktopName = null;
        var threadId = Kernel32.GetCurrentThreadId();
        var desktop = User32.GetThreadDesktop(threadId);
        if (desktop == nint.Zero)
        {
            return false;
        }

        return GetDesktopName(desktop, out desktopName);
    }

    public static bool GetInputDesktopName([NotNullWhen(true)] out string? desktopName)
    {
        var inputDesktop = OpenInputDesktop();
        try
        {
            return GetDesktopName(inputDesktop, out desktopName);
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

    public static Result<BackstageSession> StartProcessInBackstage<T>(
        string commandLine,
        string windowStationName,
        string desktopName,
        ILogger<T> logger,
        out PROCESS_INFORMATION procInfo)
    {
        using var logScope = logger.BeginScope(nameof(StartProcessInBackstage));

        procInfo = new();

        var winstaEnumResult = EnumWindowStations(
            (string windowStation, nint lParam) =>
            {
                logger.LogInformation("Found window station {windowStation}.", windowStation);
                return true;
            },
            nint.Zero);

        if (!winstaEnumResult)
        {
            var err = Marshal.GetLastWin32Error();
            logger.LogError("Enum winsta failed. Last Error: {err}", err);
        }

        var sa = new SECURITY_ATTRIBUTES()
        {
            bInheritHandle = false,
        };
        sa.Length = Marshal.SizeOf(sa);
        var saPtr = Marshal.AllocHGlobal(sa.Length);
        Marshal.StructureToPtr(sa, saPtr, false);

        var createWinstaResult = CreateWindowStation(
            windowStationName,
            0,
            ACCESS_MASK.GENERIC_ALL | ACCESS_MASK.MAXIMUM_ALLOWED,
            saPtr);

        if (createWinstaResult == nint.Zero)
        {
            return LogWin32Result(
                Result.Fail<BackstageSession>("Create winsta failed."),
                logger);
        }

        // When calling CreateDesktop, the calling process must be associated with
        // the target Window station.
        var setProcessWinstaResult = SetProcessWindowStation(createWinstaResult);

        var enumDesktopsResult = EnumDesktopsA(createWinstaResult,
            (string desktop, nint lParam) =>
            {
                logger.LogInformation("Found desktop {desktop}.", desktop);
                return true;
            },
            nint.Zero);

        if (!enumDesktopsResult)
        {
            var err = Marshal.GetLastWin32Error();
            logger.LogError("Enum desktops failed. Last Error: {err}", err);
        }

        if (!setProcessWinstaResult)
        {
            var backstageResult = Result.Fail<BackstageSession>("Set process winsta failed.");
            logger.LogResult(backstageResult);
            return backstageResult;
        }

        var createDesktopResult = CreateDesktop(
            desktopName,
            null,
            null,
            0,
            ACCESS_MASK.GENERIC_ALL | ACCESS_MASK.MAXIMUM_ALLOWED,
            saPtr);

        if (createDesktopResult == nint.Zero)
        {
            return LogWin32Result(
                Result.Fail<BackstageSession>("Create desktop failed."),
                logger);
        }

        // This fails if I try to create a new window station/desktop.  Probably missing
        // some attributes I need to set on it or something.
        // See remarks: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-switchdesktop#remarks
        if (!SwitchDesktop(createDesktopResult))
        {
            return LogWin32Result(
                Result.Fail<BackstageSession>("Switch desktop failed."),
                logger);
        }

        if (!SetThreadDesktop(createDesktopResult))
        {
            return LogWin32Result(
                Result.Fail<BackstageSession>("Set thread desktop failed."),
                logger);
        }

        var si = new STARTUPINFO
        {
            lpDesktop = @$"{windowStationName}\{desktopName}"
        };
        si.cb = Marshal.SizeOf(si);

        var createProcessResult = Kernel32.CreateProcess(
                null,
                commandLine,
                saPtr,
                saPtr,
                true,
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

    private static bool GetDesktopName(nint desktopHandle, [NotNullWhen(true)] out string? desktopName)
    {
        byte[] deskBytes = new byte[256];
        if (!GetUserObjectInformationW(desktopHandle, UOI_NAME, deskBytes, 256, out uint lenNeeded))
        {
            desktopName = string.Empty;
            return false;
        }

        desktopName = Encoding.Unicode.GetString(deskBytes.Take((int)lenNeeded).ToArray()).Replace("\0", "");
        return true;
    }

    private static Result<ResultT> LogWin32Result<ResultT, LoggerT>(
        Result<ResultT> result,
        ILogger<LoggerT> logger)
    {
        // This must be here.  It will return 0 if called after logging.
        var lastError = Marshal.GetLastWin32Error();

        logger.LogResult(result);

        if (!result.IsSuccess)
        {
            logger.LogError("Last Win32 error: {lastError}", lastError);
        }

        return result;
    }

    private static bool TryDuplicateProcessToken(
        string processName,
        uint targetSessionId,
        out SECURITY_ATTRIBUTES sa,
        out nint sourceToken,
        out nint processHandle,
        out nint dupToken)
    {
        sourceToken = nint.Zero;
        processHandle = nint.Zero;
        dupToken = nint.Zero;

        // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser.
        sa = new SECURITY_ATTRIBUTES();
        sa.Length = Marshal.SizeOf(sa);

        var processId = Process
            .GetProcessesByName(processName)
            .FirstOrDefault(x => x.SessionId == targetSessionId)
            ?.Id;

        if (!processId.HasValue)
        {
            return false;
        }

        // Obtain a handle to the winlogon process.
        processHandle = Kernel32.OpenProcess(MAXIMUM_ALLOWED, false, (uint)processId);

        // Obtain a handle to the access token of the winlogon process.
        if (!OpenProcessToken(processHandle, TOKEN_DUPLICATE, ref sourceToken))
        {
            Kernel32.CloseHandle(processHandle);
            return false;
        }

        // Copy the access token of the winlogon process; the newly created token will be a primary token.
        return DuplicateTokenEx(
            sourceToken,
            MAXIMUM_ALLOWED,
            ref sa,
            SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
            TOKEN_TYPE.TokenPrimary,
            out dupToken);
    }
}
