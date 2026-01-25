#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Lavender.Systems
{
	public static class StartExternalProcess
	{
	    //for launching the executables
	    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	    static extern IntPtr ShellExecuteW(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);
	    const int SW_HIDE = 0;
	    const int SW_SHOW = 5;


	    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	    static extern bool CreateProcessW(
	        string lpApplicationName,
	        string lpCommandLine,
	        IntPtr lpProcessAttributes,
	        IntPtr lpThreadAttributes,
	        bool bInheritHandles,
	        uint dwCreationFlags,
	        IntPtr lpEnvironment,
	        string lpCurrentDirectory,
	        [In] ref STARTUPINFO lpStartupInfo,
	        out PROCESS_INFORMATION lpProcessInformation);


	    [DllImport("kernel32.dll", SetLastError = true)]
	    [return: MarshalAs(UnmanagedType.Bool)]
	    static extern bool CloseHandle(IntPtr hObject);


	    [DllImport("kernel32.dll", SetLastError = true)]
	    [return: MarshalAs(UnmanagedType.Bool)]
	    static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);


	    [DllImport("kernel32.dll", SetLastError = true)]
	    static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);


	    [DllImport("kernel32.dll", SetLastError = true)]
	    static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);


	    [DllImport("kernel32.dll", SetLastError = true)]
	    [return: MarshalAs(UnmanagedType.Bool)]
	    static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);


	    [DllImport("kernel32.dll", SetLastError = true)]
	    static extern bool AttachConsole(uint dwProcessId);


	    [DllImport("kernel32.dll", SetLastError = true)]
	    static extern bool FreeConsole();


	    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	    struct STARTUPINFO{
	        public int cb;
	        public string lpReserved;
	        public string lpDesktop;
	        public string lpTitle;
	        public uint dwX;
	        public uint dwY;
	        public uint dwXSize;
	        public uint dwYSize;
	        public uint dwXCountChars;
	        public uint dwYCountChars;
	        public uint dwFillAttribute;
	        public uint dwFlags;
	        public short wShowWindow;
	        public short cbReserved2;
	        public IntPtr lpReserved2;
	        public IntPtr hStdInput;
	        public IntPtr hStdOutput;
	        public IntPtr hStdError;
	    }

	    [StructLayout(LayoutKind.Sequential)]
	    struct PROCESS_INFORMATION{
	        public IntPtr hProcess;
	        public IntPtr hThread;
	        public uint dwProcessId;
	        public uint dwThreadId;
	    }

	    const uint NORMAL_PRIORITY_CLASS = 0x0020;
	    const uint CREATE_NO_WINDOW = 0x08000000;
	    const uint INFINITE = 0xFFFFFFFF;
	    const uint PROCESS_ALL_ACCESS = 0x001F0FFF;


	    public static bool Run_Exe(string filepath, string workingDir, bool hidden=false){
	        if(Path.GetExtension(filepath).ToLowerInvariant() != ".exe"){ return false; }
	        // Use ShellExecute for .exe files + request elevation (administrator rights)
	        IntPtr result = ShellExecuteW( IntPtr.Zero, "runas", filepath, null, workingDir, 
	                                        hidden? SW_HIDE : SW_SHOW );

	        if(result.ToInt64() <= 32){// ShellExecute returns a value <= 32 on error
	            int error = Marshal.GetLastWin32Error();
	            Debug.LogError($"Failed to launch exe with elevation. Error code: {error}, Error message: {new Win32Exception(error).Message}");
	            return false;
	        }
	        // Note: We can't get the process ID when using ShellExecute
	        // Return a boolean
	        Debug.Log($"Executable launched successfully with elevation: {filepath}");
	        return true; 
	    }


	    public static uint Run_Bat_or_Shortcut_or_Command( string filepath_or_command,  bool isJustFile,  string workingDir, 
	                                                        bool keepWindow=false,  bool hidden = false ){
	        string fileToLaunch = "C:\\Windows\\System32\\cmd.exe";
	        string arguments = "";

	        if (isJustFile){
	            var extension = Path.GetExtension(filepath_or_command).ToLowerInvariant();
	            if (extension == ".lnk"){
	                arguments = $"/C start \"\" \"{filepath_or_command}\"";
	            }else{
	                fileToLaunch = filepath_or_command;
	            }
	        }else{// For complex commands, we'll use cmd.exe with /C to execute the command
	            // The  /C will close the window, while /K would keep it open.
	            // To remain open your command should specify  pause  inside it.
	            string prefix = keepWindow? "/K " : "/C ";
	            arguments = $"{prefix}\"{filepath_or_command}\"";
	        }
	        uint creationFlags  = NORMAL_PRIORITY_CLASS;
	                creationFlags |= hidden? CREATE_NO_WINDOW : 0;

	        string commandLine = $"{fileToLaunch} {arguments}";
	        Debug.Log($"Attempting to execute: {commandLine}");
	        Debug.Log($"Working directory: {workingDir}");

	        // Detach from the current console (so we can attach soon).
	        // Attaching to console is important for bat files, which otherwise don't spawn console no matter what.
	        FreeConsole();

	        STARTUPINFO si = new STARTUPINFO();
	        PROCESS_INFORMATION pi;
	        si.cb = Marshal.SizeOf(si);
	        si.wShowWindow = 1; // SW_SHOWNORMAL

	        bool success = CreateProcessW(  null,  commandLine,  IntPtr.Zero,  IntPtr.Zero,  false,
	                                        creationFlags,  IntPtr.Zero,  workingDir,  ref si,  out pi );
	        if (!success){
	            int error = Marshal.GetLastWin32Error();
	            Debug.LogError($"Failed to start process. Error code: {error}, Error message: {new System.ComponentModel.Win32Exception(error).Message}");
	            return 0;
	        }
	        Debug.Log($"Process started successfully. Process ID: {pi.dwProcessId}");

	        CloseHandle(pi.hProcess);
	        CloseHandle(pi.hThread);

	        AttachConsole(pi.dwProcessId);
	        return pi.dwProcessId;
	    }


	    public static bool KillProcess(uint processId){
	        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
	        if (hProcess == IntPtr.Zero)
	            return false;

	        bool result = TerminateProcess(hProcess, 0);
	        CloseHandle(hProcess);
	        return result;
	    }

	    public static bool IsProcessRunning(uint processId){
	        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
	        if (hProcess == IntPtr.Zero)
	            return false;

	        uint exitCode;
	        bool result = GetExitCodeProcess(hProcess, out exitCode);
	        CloseHandle(hProcess);

	        return result && exitCode == 259; // STILL_ACTIVE = 259
	    }

	    public static bool WaitForProcessExit(uint processId, int timeoutMs = -1){
	        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
	        if (hProcess == IntPtr.Zero)
	            return true; // Process doesn't exist, so it's not running

	        uint waitResult = WaitForSingleObject(hProcess, timeoutMs < 0 ? INFINITE : (uint)timeoutMs);
	        CloseHandle(hProcess);

	        return waitResult != 0x00000102; // WAIT_TIMEOUT = 0x00000102
	    }
	}
}
#endif