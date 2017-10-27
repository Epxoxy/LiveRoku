using System;
using System.Runtime.InteropServices;

namespace LiveRoku.Notifications.helpers {
    public class Win32API {
        [DllImport ("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern long GetWindowLong (IntPtr hwnd, int nIndex);
        [DllImport ("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern long SetWindowLong (IntPtr hwnd, int nIndex, long dwNewLong);
        [DllImport ("user32", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern int SetLayeredWindowAttributes (IntPtr Handle, int crKey, byte bAlpha, int dwFlags);
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int LWA_ALPHA = 0x2;
        public const int LWA_COLORKEY = 0x1;

        [DllImport ("user32.dll")]
        public static extern bool SetWindowPos (IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public static readonly IntPtr HWND_TOPMOST = new IntPtr (-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr (-2);
        public static readonly IntPtr HWND_TOP = new IntPtr (0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr (1);

        public const UInt32 SWP_NOSIZE = 0x0001;
        public const UInt32 SWP_NOMOVE = 0x0002;
        public const UInt32 SWP_NOZORDER = 0x0004;
        public const UInt32 SWP_NOREDRAW = 0x0008;
        public const UInt32 SWP_NOACTIVATE = 0x0010;

        public const UInt32 SWP_FRAMECHANGED = 0x0020; /* The frame changed: send WM_NCCALCSIZE */
        public const UInt32 SWP_SHOWWINDOW = 0x0040;
        public const UInt32 SWP_HIDEWINDOW = 0x0080;
        public const UInt32 SWP_NOCOPYBITS = 0x0100;
        public const UInt32 SWP_NOOWNERZORDER = 0x0200; /* Don't do owner Z ordering */
        public const UInt32 SWP_NOSENDCHANGING = 0x0400; /* Don't send WM_WINDOWPOSCHANGING */

        public const UInt32 TOPMOST_FLAGS =
            SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSIZE | SWP_NOMOVE | SWP_NOREDRAW | SWP_NOSENDCHANGING;

        #region Drag support

        [DllImport ("user32.dll")]
        public static extern IntPtr WindowFromPoint (POINT Point);

        [DllImport ("user32.dll")]
        [
            return :MarshalAs (UnmanagedType.Bool)
        ]
        public static extern bool GetCursorPos (out POINT lpPoint);

        [DllImportAttribute ("user32.dll")]
        public static extern IntPtr SendMessage (IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImportAttribute ("user32.dll")]
        public static extern bool ReleaseCapture ();

        [StructLayout (LayoutKind.Sequential)]
        public struct POINT {
            public int X;
            public int Y;
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        #endregion

        [DllImport ("user32.dll")]
        [
            return :MarshalAs (UnmanagedType.Bool)
        ]
        public static extern bool GetWindowRect (IntPtr hWnd, out RECT lpRect);

        [StructLayout (LayoutKind.Sequential)]
        public struct RECT {
            public int Left; // X coordinate of topleft point
            public int Top; // Y coordinate of topleft point
            public int Right; // X coordinate of bottomright point
            public int Bottom; // Y coordinate of bottomright point
        }
    }
}