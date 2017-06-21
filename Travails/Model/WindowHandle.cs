using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Travails.Model
{
    public class WindowHandleInfo
    {
        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        protected delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam); 
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        protected static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        protected static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        protected static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")]
        protected static extern bool IsWindowVisible(IntPtr hWnd); 

        private IntPtr _MainHandle;

        public WindowHandleInfo(IntPtr handle)
        {
            this._MainHandle = handle;
        }

        public List<Tuple<IntPtr, string>> GetAllChildHandles()
        {
            List<Tuple<IntPtr, string>> childHandles = new List<Tuple<IntPtr, string>>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(this._MainHandle, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            //https://code.msdn.microsoft.com/windowsapps/Enumerate-top-level-9aa9d7c1
            int size = GetWindowTextLength(hWnd);
            string windowName = string.Empty;
            if (size++ > 0 && IsWindowVisible(hWnd))
            {
                StringBuilder sb = new StringBuilder(size);
                GetWindowText(hWnd, sb, size);
                windowName = sb.ToString();
                //Console.WriteLine(sb.ToString());
            } 
            List<Tuple<IntPtr, string>> childHandles = gcChildhandlesList.Target as List<Tuple<IntPtr, string>>;
            childHandles.Add(new Tuple<IntPtr, string>(hWnd, windowName));

            return true;
        }
    }
}
