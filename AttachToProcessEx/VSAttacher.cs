using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace AttachToProcessEx
{
    enum ATPEres
    {
        SUCCESS,
        CANT_FIND_ATTACH_TO_PROCESS_WINDOW,
        CANT_FIND_FORM,
        FORM_EMPTY,
        OPEN_PROCESS_FAILED,
        ALLOC_FALSE,
        CANT_FIND_TARGET_PROCESS,
        CANT_FIND_ATTACH_BUTTON
    }

    /// <summary>
    /// Open visual studio attach to process window and attach
    /// </summary>
    class VSAttacher
    {
        // the reference
        private const uint LVM_FIRST = 0x1000;
        private const uint LVM_GETITEMCOUNT = LVM_FIRST + 4;
        private const uint LVM_GETITEMTEXT = LVM_FIRST + 115;
        private const uint LVM_SETITEMSTATE = LVM_FIRST + 43;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_READWRITE = 4;
        private const uint LVIS_FOCUSED = 0x0001;
        private const uint LVIS_SELECTED = 0x0002;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;

        [DllImport("user32.dll")] private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hwnd, uint Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern int PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint dwProcessId);
        [DllImport("kernel32.dll")] private static extern IntPtr OpenProcess(uint dwDesireAccess, bool bInheritHandle, uint dwProcessId);
        [DllImport("kernel32.dll")] private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll")] private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwfreeType);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll")] private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead);
        [DllImport("kernel32.dll")] private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead);

        public struct LVITEM
        {
            public int mask;
            public int iItem;
            public int iSubItem;
            public int state;
            public int stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public int cColumns;
            public IntPtr puColumns;
        }

        // only support Chinese and English
        private const string attachtoprocesswndnamezh_cn = "附加到进程";
        private const string attachtoprocesswndnameen_us = "Attach to Process";
        private const string attachbtnnamezh_cn = "附加(&A)";
        private const string attachbtnnameen_us = "&Attach";

        private const int timeout = 5;
        private const int buffersize = 256;

        public void Attach(string pid)
        {
            // it must be async because the window will block the main thread
            Thread thread = new Thread(new ParameterizedThreadStart(this.AttachToProcess));
            thread.Start(pid);
            System.Windows.Forms.SendKeys.SendWait("^%p");
        }

        private void AttachToProcess(object pid)
        {
            string pidstr = Convert.ToString(pid);
            ATPEres res = SendMsgToATPWnd(pidstr);
            string tips = null;
            switch (res)
            {
                case ATPEres.SUCCESS:
                    break;
                case ATPEres.CANT_FIND_ATTACH_TO_PROCESS_WINDOW:
                    tips = "Can't find Attach to Process Windows"; break;
                case ATPEres.CANT_FIND_FORM:
                    tips = "Can't find Form"; break;
                case ATPEres.FORM_EMPTY:
                    tips = "Form is empty"; break;
                case ATPEres.OPEN_PROCESS_FAILED:
                    tips = "Can't open process, you can check the last error"; break;
                case ATPEres.ALLOC_FALSE:
                    tips = "Can't allocate memory, you can check the last error"; break;
                case ATPEres.CANT_FIND_TARGET_PROCESS:
                    tips = "Can't find the process you selected"; break;
                case ATPEres.CANT_FIND_ATTACH_BUTTON:
                    tips = "Can't find the attach button"; break;
                default:
                    tips = "Unknown Error"; break;
            }
            if(tips != null)
                MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "{0}", tips),
                "ATPEWindow");
        }

        private ATPEres SendMsgToATPWnd(string pid)
        {
            IntPtr mainhwnd = IntPtr.Zero;
            TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
            TimeSpan end = new TimeSpan(DateTime.Now.Ticks);
            while (mainhwnd == IntPtr.Zero && end.TotalSeconds - start.TotalSeconds < timeout)
            {
                mainhwnd = FindWindow("#32770", attachtoprocesswndnameen_us);
                if (mainhwnd == IntPtr.Zero)
                {
                    mainhwnd = FindWindow("#32770", attachtoprocesswndnamezh_cn);
                }
                end = new TimeSpan(DateTime.Now.Ticks);
            }
            if (mainhwnd == IntPtr.Zero)
                return ATPEres.CANT_FIND_ATTACH_TO_PROCESS_WINDOW;

            IntPtr formhwnd = IntPtr.Zero;
            start = new TimeSpan(DateTime.Now.Ticks);
            end = new TimeSpan(DateTime.Now.Ticks);
            while (formhwnd == IntPtr.Zero && end.TotalSeconds - start.TotalSeconds < timeout)
            {
                formhwnd = FindWindowEx(mainhwnd, IntPtr.Zero, "SysListView32", null);
                end = new TimeSpan(DateTime.Now.Ticks);
            }
            if (formhwnd == IntPtr.Zero)
                return ATPEres.CANT_FIND_FORM;

            // todo clean the search
            int itemCount = SendMessage(formhwnd, LVM_GETITEMCOUNT, 0, 0);
            start = new TimeSpan(DateTime.Now.Ticks);
            end = new TimeSpan(DateTime.Now.Ticks);
            while (itemCount == 0 && end.TotalSeconds - start.TotalSeconds < timeout)
            {
                itemCount = SendMessage(formhwnd, LVM_GETITEMCOUNT, 0, 0);
                end = new TimeSpan(DateTime.Now.Ticks);
            }
            if (itemCount == 0)
                return ATPEres.FORM_EMPTY;

            uint processid;
            GetWindowThreadProcessId(formhwnd, out processid);
            IntPtr hProcess = OpenProcess(PROCESS_VM_OPERATION |
                PROCESS_VM_READ | PROCESS_VM_WRITE, false, processid);
            if (hProcess == IntPtr.Zero)
                return ATPEres.OPEN_PROCESS_FAILED;

            IntPtr plvitem = VirtualAllocEx(hProcess, IntPtr.Zero,
                (uint)Marshal.SizeOf(typeof(LVITEM)), MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
            IntPtr pitem = VirtualAllocEx(hProcess, IntPtr.Zero, buffersize,
                MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
            if (plvitem == IntPtr.Zero || pitem == IntPtr.Zero)
            {
                VirtualFreeEx(hProcess, plvitem, 0, MEM_RELEASE);
                VirtualFreeEx(hProcess, pitem, 0, MEM_RELEASE);
                return ATPEres.ALLOC_FALSE;
            }

            bool b_find = false;
            for (int i = 0; i < itemCount; i++)
            {
                byte[] buffer = new byte[buffersize];
                LVITEM[] item = new LVITEM[1];
                item[0].iItem = i;
                item[0].iSubItem = 1; // absolute 1 because the window will reset
                item[0].cchTextMax = buffer.Length;
                item[0].pszText = pitem;
                uint numberOfByteRead = 0;
                WriteProcessMemory(hProcess, plvitem,
                    Marshal.UnsafeAddrOfPinnedArrayElement(item, 0),
                    Marshal.SizeOf(typeof(LVITEM)), ref numberOfByteRead);
                SendMessage(formhwnd, LVM_GETITEMTEXT, i, plvitem.ToInt32());
                ReadProcessMemory(hProcess, pitem,
                    Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0),
                    buffer.Length, ref numberOfByteRead);
                string curpid = Marshal.PtrToStringUni(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0));

                if (curpid == pid)
                {
                    b_find = true;
                    // Form's default selected item is the first item
                    if (i != 0)
                    {
                        item[0].state = (int)LVIS_SELECTED;
                        item[0].stateMask = (int)LVIS_SELECTED;
                        WriteProcessMemory(hProcess, plvitem, Marshal.UnsafeAddrOfPinnedArrayElement(item, 0), Marshal.SizeOf(typeof(LVITEM)), ref numberOfByteRead);
                        SendMessage(formhwnd, LVM_SETITEMSTATE, i, plvitem.ToInt32());
                        item[0].state = 0; // cancel the first selected
                        item[0].stateMask = (int)LVIS_SELECTED;
                        WriteProcessMemory(hProcess, plvitem, Marshal.UnsafeAddrOfPinnedArrayElement(item, 0), Marshal.SizeOf(typeof(LVITEM)), ref numberOfByteRead);
                        SendMessage(formhwnd, LVM_SETITEMSTATE, 0, plvitem.ToInt32());
                    }
                    break;
                }
            }
            if (!b_find)
            {
                VirtualFreeEx(hProcess, plvitem, 0, MEM_RELEASE);
                VirtualFreeEx(hProcess, pitem, 0, MEM_RELEASE);
                return ATPEres.CANT_FIND_TARGET_PROCESS;
            }

            IntPtr btnhwnd = IntPtr.Zero;
            btnhwnd = FindWindowEx(mainhwnd, IntPtr.Zero, "Button", attachbtnnameen_us);
            if (btnhwnd == IntPtr.Zero)
                btnhwnd = FindWindowEx(mainhwnd, IntPtr.Zero, "Button", attachbtnnamezh_cn);
            if(btnhwnd == IntPtr.Zero)
            {
                VirtualFreeEx(hProcess, plvitem, 0, MEM_RELEASE);
                VirtualFreeEx(hProcess, pitem, 0, MEM_RELEASE);
                return ATPEres.CANT_FIND_ATTACH_BUTTON;
            }

            PostMessage(btnhwnd, WM_LBUTTONDOWN, 0, 0);
            PostMessage(btnhwnd, WM_LBUTTONUP, 0, 0);

            VirtualFreeEx(hProcess, plvitem, 0, MEM_RELEASE);
            VirtualFreeEx(hProcess, pitem, 0, MEM_RELEASE);
            return ATPEres.SUCCESS;
        }
    }
}
