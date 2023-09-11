using System.Runtime.InteropServices;
using static Immense.RemoteControl.Desktop.Shared.Native.Windows.ADVAPI32;

namespace Immense.RemoteControl.Desktop.Shared.Native.Windows;

public static class Kernel32
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(nint hSnapshot);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool CreateProcess(
       string? lpApplicationName,
       string lpCommandLine,
       nint lpProcessAttributes, // Pointer to SECURITY_ATTRIBUTES structure
       nint lpThreadAttributes,  // Pointer to SECURITY_ATTRIBUTES structure
       bool bInheritHandles,
       uint dwCreationFlags,
       IntPtr lpEnvironment,
       string? lpCurrentDirectory,
       [In] ref STARTUPINFO lpStartupInfo,
       out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern nint GetCommandLine();

    [DllImport("kernel32.dll")]
    public static extern nint GetConsoleWindow();

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll")]
    public static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")]
    public static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

    [DllImport("kernel32.dll")]
    public static extern uint WTSGetActiveConsoleSessionId();

    /// <summary>
    /// contains information about the current state of both physical and virtual memory, including extended memory
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        /// <summary>
        /// Size of the structure, in bytes. You must set this member before calling GlobalMemoryStatusEx.
        /// </summary>
        public uint dwLength;

        /// <summary>
        /// Number between 0 and 100 that specifies the approximate percentage of physical memory that is in use (0 indicates no memory use and 100 indicates full memory use).
        /// </summary>
        public uint dwMemoryLoad;

        /// <summary>
        /// Total size of physical memory, in bytes.
        /// </summary>
        public ulong ullTotalPhys;

        /// <summary>
        /// Size of physical memory available, in bytes.
        /// </summary>
        public ulong ullAvailPhys;

        /// <summary>
        /// Size of the committed memory limit, in bytes. This is physical memory plus the size of the page file, minus a small overhead.
        /// </summary>
        public ulong ullTotalPageFile;

        /// <summary>
        /// Size of available memory to commit, in bytes. The limit is ullTotalPageFile.
        /// </summary>
        public ulong ullAvailPageFile;

        /// <summary>
        /// Total size of the user mode portion of the virtual address space of the calling process, in bytes.
        /// </summary>
        public ulong ullTotalVirtual;

        /// <summary>
        /// Size of unreserved and uncommitted memory in the user mode portion of the virtual address space of the calling process, in bytes.
        /// </summary>
        public ulong ullAvailVirtual;

        /// <summary>
        /// Size of unreserved and uncommitted memory in the extended portion of the virtual address space of the calling process, in bytes.
        /// </summary>
        public ulong ullAvailExtendedVirtual;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:MEMORYSTATUSEX"/> class.
        /// </summary>
        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }
}
