using Gma.UserActivityMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;

namespace ShowDesktopPerMonitor
{
    public partial class Main : Form
    {
        private static InputSimulator inputSimulator = new InputSimulator();
        private List<IntPtr> hiddenWindows = new List<IntPtr>();

        private bool winKeyDown;
        private bool winKeyDownSimulated;
        private int winAndDKeysDown;
        private bool passWinKeyDown;
        private bool windowsHidden;

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            HookManager.KeyDown += HookManager_KeyDown;
            HookManager.KeyUp += HookManager_KeyUp;
        }

        private void HookManager_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LWin)
            {
                e.Handled = !passWinKeyDown;
                passWinKeyDown = false;

                if (!winKeyDown)
                {
                    winKeyDown = true;
                    winKeyDownSimulated = false;
                }
            }
            else if (e.KeyCode == Keys.D)
            {
                if (winKeyDown)
                {
                    winAndDKeysDown = 2;
                    e.Handled = true;

                    ShowDesktop();
                }
            }
            else
            {
                if(winKeyDown && !winKeyDownSimulated)
                {
                    winKeyDownSimulated = true;
                    passWinKeyDown = true;
                    inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LWIN);
                }
            }
        }

        private void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.LWin && winKeyDown)
            {
                if (!winKeyDownSimulated && winAndDKeysDown == 0)
                {
                    winKeyDownSimulated = true;
                    passWinKeyDown = true;
                    inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LWIN);
                }

                winKeyDown = false;
                winKeyDownSimulated = false;

                if (winAndDKeysDown > 0)
                {
                    winAndDKeysDown--;
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.D && winAndDKeysDown > 0)
            {
                winAndDKeysDown--;
                e.Handled = true;
            }
        }

        private void ShowDesktop()
        {
            var windows = new List<IntPtr>();
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (!NativeMethods.IsWindowVisible(hWnd))
                    return true;

                var caption = GetWindowCaption(hWnd);
                if (string.IsNullOrWhiteSpace(caption) || caption.Equals("Program Manager"))
                    return true;

                var screen = Screen.FromHandle(hWnd);
                if (screen.Bounds != Screen.PrimaryScreen.Bounds)
                    return true;

                windows.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            Task.Factory.StartNew(() => 
            {
                if (!windowsHidden)
                {
                    foreach (var hWnd in windows)
                    {
                        NativeMethods.ShowWindow(hWnd, NativeMethods.ShowWindowCmd.SW_HIDE);
                        hiddenWindows.Add(hWnd);
                    }
                }
                else
                {
                    foreach(var hWnd in hiddenWindows)
                        NativeMethods.ShowWindow(hWnd, NativeMethods.ShowWindowCmd.SW_SHOW);

                    hiddenWindows.Clear();
                }

                windowsHidden = !windowsHidden;
            });
        }

        private string GetWindowCaption(IntPtr hWnd)
        {
            int len = NativeMethods.GetWindowTextLength(hWnd) + 1;
            StringBuilder sb = new StringBuilder(len);
            len = NativeMethods.GetWindowText(hWnd, sb, len);
            return sb.ToString(0, len);
        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
            }
        }
    }
}
