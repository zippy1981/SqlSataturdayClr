using System;
using System.Data.SqlTypes;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.SqlServer.Server;
using Microsoft.Win32.SafeHandles;

/// <remarks>Adapted from Wintellects code.</remarks>
/// <seealso href="https://github.com/Wintellect/ProcMonDebugOutput/blob/master/Source/Sysinternals.Debug/NativeMethods.cs"/>
public partial class StoredProcedures
{
    const uint GENERIC_WRITE = 0x40000000;
    const uint OPEN_EXISTING = 3;
    const uint FILE_WRITE_ACCESS = 0x0002;
    const uint FILE_SHARE_WRITE = 0x00000002;
    const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    const uint METHOD_BUFFERED = 0;
    const uint FILE_DEVICE_PROCMON_LOG = 0x00009535;
    const string PROCMON_DEBUGGER_HANDLER = "\\\\.\\Global\\ProcmonDebugLogger";

    static uint IOCTL_EXTERNAL_LOG_DEBUGOUT { get { return CTL_CODE(); } }

    private static uint CTL_CODE(
        uint DeviceType = FILE_DEVICE_PROCMON_LOG,
        uint Function = 0x81,
        uint Method = METHOD_BUFFERED,
        uint Access = FILE_WRITE_ACCESS)
    {
        return ((DeviceType << 16) | (Access << 14) | (Function << 2) | Method);
    }


    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern SafeFileHandle CreateFile(
        string lpFileName = PROCMON_DEBUGGER_HANDLER,
        uint dwDesiredAccess = GENERIC_WRITE,
        uint dwShareMode = FILE_SHARE_WRITE,
        IntPtr lpSecurityAttributes = default(IntPtr),
        uint dwCreationDisposition = OPEN_EXISTING,
        uint dwFlagsAndAttributes = FILE_ATTRIBUTE_NORMAL,
        IntPtr hTemplateFile = default(IntPtr));

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool DeviceIoControl(
        SafeFileHandle hDevice, uint dwIoControlCode,
        StringBuilder lpInBuffer, uint nInBufferSize,
        IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [SqlProcedure]
    public static void ProcMonDebugOutput (SqlString msg)
    {
        if (msg.IsNull) throw new ArgumentNullException("msg");
        uint outLen;
        var message = new StringBuilder(msg.Value);
        var hProcMon = CreateFile();
        DeviceIoControl(
            hProcMon, IOCTL_EXTERNAL_LOG_DEBUGOUT,
            message, (uint)(msg.Value.Length * sizeof(char)),
            IntPtr.Zero, 0, out outLen, IntPtr.Zero);
    }
}
