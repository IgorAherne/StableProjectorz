using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace spz {

	//can help open system files, when compiled on IL2CPP
	public class ShellExecuteMGR : MonoBehaviour{
	    [DllImport("shell32.dll", SetLastError = true)]
	    static extern IntPtr ShellExecute(
	        IntPtr hwnd,
	        string lpOperation,
	        string lpFile,
	        string lpParameters,
	        string lpDirectory,
	        int nShowCmd);

	    private const int SW_HIDE = 0;
	    private const int SW_SHOW = 5;

	    public static bool Open(string fileName){
	        IntPtr result = ShellExecute(IntPtr.Zero, "open", fileName, null, null, SW_SHOW);
	        return result.ToInt64() > 32; // If the function succeeds, it returns a value greater than 32.
	    }

	    public static bool RunCommand(string command, string arguments, string workingDirectory, bool showWindow = false){
	        int showCmd = showWindow ? SW_SHOW : SW_HIDE;
	        IntPtr result = ShellExecute(IntPtr.Zero, "open", command, arguments, workingDirectory, showCmd);
	        return result.ToInt64() > 32;
	    }

	}
}//end namespace
