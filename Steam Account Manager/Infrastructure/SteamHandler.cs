using Steam_Account_Manager.Infrastructure.Models.AccountModel;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;

namespace Steam_Account_Manager.Infrastructure
{
    internal class SteamHandler
    {
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(HandleRef hWnd, int nCmdShow);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "BlockInput")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public const int SW_RESTORE = 9;

        private enum WM : uint
        {
            KEYDOWN = 0x0100,
            KEYUP = 0x0101,
            CHAR = 0x0102
        }
        private enum VK : uint
        {
            RETURN = 0x0D,
            TAB = 0x09,
            CONTROL = 0X11,
            SPACE = 0x20
        }

        #region WinAPI Helper

        private static void SendVirtualKey(IntPtr HWND, VK key)
        {
            PostMessage(HWND, (int)WM.KEYDOWN, (IntPtr)key, IntPtr.Zero);
            PostMessage(HWND, (int)WM.KEYUP, (IntPtr)key, IntPtr.Zero);
        }

        public static void ForceWindowToForeground(IntPtr HWND)
        {
            const int SW_SHOW = 5;
            AttachedThreadInputAction(
                () =>
                {
                    BringWindowToTop(HWND);
                    ShowWindow(HWND, SW_SHOW);
                });
        }

        private static void AttachedThreadInputAction(Action action)
        {
            var foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            var appThread = GetCurrentThreadId();
            bool threadsAttached = false;
            try
            {
                threadsAttached =
                    foreThread == appThread ||
                    AttachThreadInput(foreThread, appThread, true);
                if (threadsAttached) action();
                else throw new Exception("Attached steam thread failed");
            }
            finally
            {
                if (threadsAttached)
                    AttachThreadInput(foreThread, appThread, false);
            }
        }

        #endregion


        public static void VirtualSteamLogger(Account account, bool savePassword = false, bool paste2fa = false)
        {
            Automation.RemoveAllEventHandlers();
            if (Utilities.GetSteamRegistryLanguage() != account.Login)
            {
                Utilities.SetSteamRegistryRememberUser(String.Empty);
            }

            Utilities.KillSteamAndConnect(Config.Properties.SteamDirection);
            byte SteamAwaiter = 0;

            Automation.AddAutomationEventHandler(
                WindowPattern.WindowOpenedEvent,
                AutomationElement.RootElement,
                TreeScope.Children,
                (sender, e) =>
                {
                    var element = sender as AutomationElement;

                    if (element.Current.Name.Equals("Steam"))
                    {
                        if (++SteamAwaiter >= 2)
                        {
                            Automation.RemoveAllEventHandlers();
                        }
                    }
                    if (element.Current.Name.Contains("Steam") && element.Current.Name.Length > 5)
                    {
                        Thread.Sleep(500);
                        ForceWindowToForeground((IntPtr)element.Current.NativeWindowHandle);
                        SetForegroundWindow((IntPtr)element.Current.NativeWindowHandle);
                        Thread.Sleep(50);

                        if (Utilities.GetSteamRegistryActiveUser() == 0)
                        {
                            BlockInput(true);
                            if (String.IsNullOrEmpty(Utilities.GetSteamRegistryRememberUser()))
                            {
                                foreach (char c in account.Login)
                                {
                                    SetForegroundWindow((IntPtr)element.Current.NativeWindowHandle);
                                    PostMessage((IntPtr)element.Current.NativeWindowHandle, (int)WM.CHAR, (IntPtr)c, IntPtr.Zero);
                                }
                                Thread.Sleep(100);
                                SendVirtualKey((IntPtr)element.Current.NativeWindowHandle, VK.TAB);
                                Thread.Sleep(100);
                            }

                            foreach (char c in account.Password)
                            {
                                SetForegroundWindow((IntPtr)element.Current.NativeWindowHandle);
                                PostMessage((IntPtr)element.Current.NativeWindowHandle, (int)WM.CHAR, (IntPtr)c, IntPtr.Zero);
                            }

                            if (savePassword)
                            {
                                SetForegroundWindow((IntPtr)element.Current.NativeWindowHandle);
                                Thread.Sleep(100);
                                SendVirtualKey((IntPtr)element.Current.NativeWindowHandle, VK.TAB);
                                Thread.Sleep(100);
                                SendVirtualKey((IntPtr)element.Current.NativeWindowHandle, VK.SPACE);
                            }

                            SetForegroundWindow((IntPtr)element.Current.NativeWindowHandle);
                            Thread.Sleep(100);
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                            SetForegroundWindow((IntPtr)element.Current.NativeWindowHandle);
                            if (paste2fa)
                            {
                                string code = "";
                                App.Current.Dispatcher.Invoke(() => { code = Clipboard.GetText(TextDataFormat.Text); });
                                Thread.Sleep(2700);
                                if(Config.Properties.Input2FaMethod == Models.Input2faMethod.Manually)
                                {
                                    foreach (char c in code)
                                    {
                                        SetForegroundWindow((IntPtr)element.Current.NativeWindowHandle);
                                        PostMessage((IntPtr)element.Current.NativeWindowHandle, (int)WM.CHAR, (IntPtr)c, IntPtr.Zero);
                                    }
                                }
                                else
                                {
                                    keybd_event(0x11, 0, 0x00, 0);
                                    keybd_event(0x56, 0, 0x00, 0);
                                    Thread.Sleep(50);
                                    keybd_event(0x11, 0, 0x02, 0);
                                    keybd_event(0x56, 0, 0x02, 0);
                                }

                                
                                Thread.Sleep(200);
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                            }
                            Automation.RemoveAllEventHandlers();
                            BlockInput(false);



                            if (Config.Properties.ActionAfterLogin == Models.LoggedAction.Close)
                                App.Current.Dispatcher.InvokeShutdown();
                        }

                    }

                });
        }
    }
}
