using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace B83.Win32
{
    public enum HookType : int
    {
        WH_GETMESSAGE = 3,
        // Other hook types can be added if needed
    }

    public enum WM : uint
    {
        DROPFILES = 0x0233,
        // Other window messages can be added if needed
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;

        public POINT(int aX, int aY)
        {
            x = aX;
            y = aY;
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
        public uint lPrivate;
    }

    public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
    public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

    public static class Window
    {
        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        public static string GetClassName(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            int count = GetClassName(hWnd, sb, 256);
            return sb.ToString(0, count);
        }
    }

    public static class WinAPI
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("shell32.dll")]
        public static extern void DragAcceptFiles(IntPtr hwnd, bool fAccept);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);
        [DllImport("shell32.dll")]
        public static extern void DragFinish(IntPtr hDrop);
        [DllImport("shell32.dll")]
        public static extern bool DragQueryPoint(IntPtr hDrop, out POINT lppt);
    }
#endif

    public static class UnityDragAndDropHook
    {
        public delegate void DroppedFilesEvent(List<string> aPathNames, POINT aDropPoint);
        public static event DroppedFilesEvent OnDroppedFiles;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN

        private static uint threadId;
        private static IntPtr mainWindow = IntPtr.Zero;
        private static IntPtr m_Hook;
        private static string m_ClassName = "UnityWndClass";

        // Attribute required for IL2CPP, also has to be a static method
        [AOT.MonoPInvokeCallback(typeof(EnumThreadDelegate))]
        private static bool EnumCallback(IntPtr hWnd, IntPtr lParam)
        {
            if (Window.IsWindowVisible(hWnd) && (mainWindow == IntPtr.Zero || (m_ClassName != null && Window.GetClassName(hWnd) == m_ClassName)))
            {
                mainWindow = hWnd;
            }
            return true;
        }

        public static void InstallHook()
        {
            threadId = WinAPI.GetCurrentThreadId();
            if (threadId > 0)
                Window.EnumThreadWindows(threadId, EnumCallback, IntPtr.Zero);

            var hModule = WinAPI.GetModuleHandle(null);
            m_Hook = WinAPI.SetWindowsHookEx(HookType.WH_GETMESSAGE, Callback, hModule, threadId);

            // Allow dragging of files onto the main window. Generates the WM_DROPFILES message
            WinAPI.DragAcceptFiles(mainWindow, true);
        }

        public static void UninstallHook()
        {
            if (m_Hook != IntPtr.Zero)
            {
                WinAPI.UnhookWindowsHookEx(m_Hook);
                m_Hook = IntPtr.Zero;
            }
            if (mainWindow != IntPtr.Zero)
            {
                WinAPI.DragAcceptFiles(mainWindow, false);
                mainWindow = IntPtr.Zero;
            }
        }

        // Attribute required for IL2CPP, also has to be a static method
        [AOT.MonoPInvokeCallback(typeof(HookProc))]
        private static IntPtr Callback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                // Marshal the MSG structure from lParam
                MSG msg = Marshal.PtrToStructure<MSG>(lParam);

                if (msg.message == (uint)WM.DROPFILES)
                {
                    HandleDropFiles(msg.wParam, msg.pt);
                }
            }
            return WinAPI.CallNextHookEx(m_Hook, code, wParam, lParam);
        }

        private static void HandleDropFiles(IntPtr hDrop, POINT pt)
        {
            // Get the drop point in client coordinates
            POINT pos;
            WinAPI.DragQueryPoint(hDrop, out pos);

            // Get the number of files dropped
            uint fileCount = WinAPI.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
            var files = new List<string>();

            // Buffer for file paths
            var fileName = new StringBuilder(260); // MAX_PATH

            // Retrieve each file path
            for (uint i = 0; i < fileCount; i++)
            {
                int length = (int)WinAPI.DragQueryFile(hDrop, i, fileName, (uint)fileName.Capacity);
                files.Add(fileName.ToString(0, length));
                fileName.Clear();
            }

            // Finish the drag operation
            WinAPI.DragFinish(hDrop);

            // Invoke the event
            OnDroppedFiles?.Invoke(files, pos);
        }
#else
        public static void InstallHook()
        {
        }
        public static void UninstallHook()
        {
        }
#endif
    }
}
