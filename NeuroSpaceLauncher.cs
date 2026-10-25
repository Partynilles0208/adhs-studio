using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;

namespace NeuroSpaceLauncher
{
    public class HotkeyWatcherForm : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int GWL_STYLE = -16;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CHILD = 0x40000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private const int MOD_ALT = 0x0001;
        private const int VK_B = 0x42;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9920;

        private Process browserProcess;
        private System.Windows.Forms.Timer embedTimer;
        private IntPtr cachedEdgeHwnd = IntPtr.Zero;

        public HotkeyWatcherForm(Process proc)
        {
            this.browserProcess = proc;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new System.Drawing.Size(0, 0);

            // Register system-wide Alt+B hotkey for instant minimize to desktop
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, VK_B);

            // In-App Roblox seamless window embedder timer
            embedTimer = new System.Windows.Forms.Timer();
            embedTimer.Interval = 150;
            embedTimer.Tick += OnEmbedTimerTick;
            embedTimer.Start();

            if (proc != null)
            {
                proc.EnableRaisingEvents = true;
                proc.Exited += delegate
                {
                    try
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            embedTimer.Stop();
                            UnregisterHotKey(this.Handle, HOTKEY_ID);
                            Application.Exit();
                        });
                    }
                    catch { }
                };
            }
        }

        private void OnEmbedTimerTick(object sender, EventArgs e)
        {
            try
            {
                // Keep external desktop shortcuts clean
                CleanRobloxDesktopShortcuts();

                // Find or verify Edge window
                if (cachedEdgeHwnd == IntPtr.Zero || !IsWindow(cachedEdgeHwnd))
                {
                    cachedEdgeHwnd = FindEdgeWindow();
                }

                if (cachedEdgeHwnd == IntPtr.Zero) return;

                // Find Roblox window (Class: WINDOWCLIENT, Title: Roblox)
                IntPtr rbxHwnd = FindWindow("WINDOWSCLIENT", "Roblox");
                if (rbxHwnd == IntPtr.Zero) return;

                RECT edgeRect;
                GetClientRect(cachedEdgeHwnd, out edgeRect);
                int w = edgeRect.Right - edgeRect.Left;
                int h = edgeRect.Bottom - edgeRect.Top;
                if (w <= 0 || h <= 0) return;

                // If not yet reparented, embed Roblox directly inside NeuroSpace window!
                if (GetParent(rbxHwnd) != cachedEdgeHwnd)
                {
                    SetParent(rbxHwnd, cachedEdgeHwnd);
                    int oldStyle = GetWindowLong(rbxHwnd, GWL_STYLE);
                    int newStyle = (oldStyle & ~WS_POPUP & ~WS_CAPTION & ~WS_THICKFRAME) | WS_CHILD | WS_VISIBLE;
                    SetWindowLong(rbxHwnd, GWL_STYLE, newStyle);
                }

                // Keep Roblox perfectly positioned inside NeuroSpace under top nav bar
                MoveWindow(rbxHwnd, 0, 48, w, h - 48, true);
                SetWindowPos(rbxHwnd, IntPtr.Zero, 0, 48, w, h - 48, SWP_SHOWWINDOW);
            }
            catch { }
        }

        private IntPtr FindEdgeWindow()
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder sb = new StringBuilder(512);
                    GetWindowText(hWnd, sb, 512);
                    string title = sb.ToString();
                    if (title.IndexOf("NeuroSpace", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found = hWnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                MinimizeAllToDesktop();
            }
            base.WndProc(ref m);
        }

        private static void MinimizeAllToDesktop()
        {
            try
            {
                CleanRobloxDesktopShortcuts();
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType != null)
                {
                    object shell = Activator.CreateInstance(shellType);
                    shellType.InvokeMember("MinimizeAll", BindingFlags.InvokeMethod, null, shell, null);
                }
            }
            catch { }
        }

        public static void CleanRobloxDesktopShortcuts()
        {
            try
            {
                string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string commonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                foreach (string d in new string[] { userDesktop, commonDesktop })
                {
                    if (Directory.Exists(d))
                    {
                        foreach (string lnk in Directory.GetFiles(d, "*Roblox*.lnk"))
                        {
                            try { File.Delete(lnk); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (embedTimer != null) embedTimer.Stop();
                UnregisterHotKey(this.Handle, HOTKEY_ID);
            }
            catch { }
            base.OnFormClosing(e);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                HotkeyWatcherForm.CleanRobloxDesktopShortcuts();
                string targetHtml = @"C:\NeuroSpace\index.html";
                if (!File.Exists(targetHtml))
                {
                    string curHtml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.html");
                    if (File.Exists(curHtml)) targetHtml = curHtml;
                }

                string htmlUri = "file:///" + targetHtml.Replace('\\', '/') + "?app=desktop";

                string edgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe");
                if (!File.Exists(edgePath))
                {
                    edgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe");
                }

                string chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Google\Chrome\Application\chrome.exe");
                if (!File.Exists(chromePath))
                {
                    chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Google\Chrome\Application\chrome.exe");
                }

                string browserExe = null;
                if (File.Exists(edgePath)) browserExe = edgePath;
                else if (File.Exists(chromePath)) browserExe = chromePath;

                string flags = string.Format(
                    "--app=\"{0}\" --user-data-dir=\"C:\\NeuroSpace\\EdgeData\" --autoplay-policy=no-user-gesture-required --disable-features=PreloadMediaEngagementData,AutoplayIgnoreWebAudio --no-first-run --no-default-browser-check",
                    htmlUri
                );

                Process browserProc = null;
                if (browserExe != null)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = browserExe;
                    psi.Arguments = flags;
                    psi.UseShellExecute = false;
                    browserProc = Process.Start(psi);
                }
                else
                {
                    browserProc = Process.Start(new ProcessStartInfo
                    {
                        FileName = targetHtml,
                        UseShellExecute = true
                    });
                }

                // Run in-app embedder & hotkey watcher form
                Application.Run(new HotkeyWatcherForm(browserProc));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Starten von NeuroSpace: " + ex.Message, "NeuroSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
