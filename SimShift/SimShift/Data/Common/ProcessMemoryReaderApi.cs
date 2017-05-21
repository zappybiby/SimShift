using System;
using System.Runtime.InteropServices;

namespace SimShift.Data.Common
{
    /// <summary>
    ///     ProcessMemoryReader is a class that enables direct reading a process memory
    /// </summary>
    class ProcessMemoryReaderApi
    {
        // BOOL CloseHandle(
        // HANDLE hObject // handle to object
        // );
        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);

        // constants information can be found in <winnt.h>

        // function declarations are found in the MSDN and in <winbase.h>

        // HANDLE OpenProcess(
        // DWORD dwDesiredAccess, // access flag
        // BOOL bInheritHandle, // handle inheritance option
        // DWORD dwProcessId // process identifier
        // );
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

        // BOOL ReadProcessMemory(
        // HANDLE hProcess, // handle to the process
        // LPCVOID lpBaseAddress, // base of memory area
        // LPVOID lpBuffer, // data buffer
        // SIZE_T nSize, // number of bytes to read
        // SIZE_T * lpNumberOfBytesRead // number of bytes read
        // );
        [DllImport("kernel32.dll")]
        public static extern int ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, uint size, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, out int lpNumberOfBytesWritten);
    }
}