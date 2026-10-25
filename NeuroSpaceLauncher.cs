using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using Microsoft.Win32;
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

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        public static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            return SetWindowLong32(hWnd, nIndex, dwNewLong);
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

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
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

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
        private const int GWL_EXSTYLE = -20;
        private const int GWLP_HWNDPARENT = -8;

        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CHILD = 0x40000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_SYSMENU = 0x00080000;

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOCOPYBITS = 0x0100;
        private const uint SWP_ASYNCWINDOWPOS = 0x4000;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private const int MOD_ALT = 0x0001;
        private const int VK_B = 0x42;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9920;

        private System.Windows.Forms.Timer embedTimer;
        private IntPtr cachedEdgeHwnd = IntPtr.Zero;
        private bool edgeEverFound = false;
        private bool isRobloxFullscreen = false;
        private bool prevF11Down = false;
        private string lastObservedTitle = "";

        private IntPtr lastOwnerSet = IntPtr.Zero;
        private int lastAppliedX = -99999;
        private int lastAppliedY = -99999;
        private int lastAppliedW = -99999;
        private int lastAppliedH = -99999;
        private bool lastAppliedVisible = false;

        public HotkeyWatcherForm()
        {
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new System.Drawing.Size(0, 0);

            // Register system-wide Alt+B hotkey for instant minimize to desktop
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, VK_B);

            // Smooth flicker-free embedder timer (100ms interval)
            embedTimer = new System.Windows.Forms.Timer();
            embedTimer.Interval = 100;
            embedTimer.Tick += OnEmbedTimerTick;
            embedTimer.Start();
        }

        private void OnEmbedTimerTick(object sender, EventArgs e)
        {
            try
            {
                // Keep external desktop shortcuts and autostart clean
                CleanRobloxDesktopShortcuts();
                RemoveRobloxAutostart();

                // Find or verify Edge window
                if (cachedEdgeHwnd == IntPtr.Zero || !IsWindow(cachedEdgeHwnd))
                {
                    cachedEdgeHwnd = FindEdgeWindow();
                    if (cachedEdgeHwnd != IntPtr.Zero)
                    {
                        edgeEverFound = true;
                    }
                }

                // If Edge was previously open and now closed, kill Roblox and exit launcher
                if (edgeEverFound && (cachedEdgeHwnd == IntPtr.Zero || !IsWindow(cachedEdgeHwnd)))
                {
                    KillRobloxProcess();
                    Application.Exit();
                    return;
                }

                if (cachedEdgeHwnd == IntPtr.Zero) return;

                // Check title to determine if user is in Roblox Active mode or requested closing
                StringBuilder sb = new StringBuilder(512);
                GetWindowText(cachedEdgeHwnd, sb, 512);
                string title = sb.ToString();

                if (title.IndexOf("Close Roblox", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    title.IndexOf("Exit App", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    KillRobloxProcess();
                    lastAppliedVisible = false;
                    lastAppliedX = -99999;
                    lastAppliedY = -99999;
                    lastAppliedW = -99999;
                    lastAppliedH = -99999;
                    lastOwnerSet = IntPtr.Zero;
                    return;
                }

                // Find normal Roblox window (Class: WINDOWCLIENT)
                IntPtr rbxHwnd = FindWindow("WINDOWSCLIENT", null);
                if (rbxHwnd == IntPtr.Zero || !IsWindow(rbxHwnd)) return;

                bool isRobloxActive = title.IndexOf("Roblox Active", StringComparison.OrdinalIgnoreCase) >= 0;

                // 1. Detach from Edge if previously reparented as WS_CHILD
                if (GetParent(rbxHwnd) != IntPtr.Zero)
                {
                    SetParent(rbxHwnd, IntPtr.Zero);
                }

                // 2. Set Edge as OWNER ONCE per window instance (never call in an unconditional loop!)
                if (lastOwnerSet != cachedEdgeHwnd)
                {
                    SetWindowLongPtr(rbxHwnd, GWLP_HWNDPARENT, cachedEdgeHwnd);
                    lastOwnerSet = cachedEdgeHwnd;
                }

                // 3. Ensure borderless popup window style (WS_POPUP, NOT WS_CHILD) only if styles differ
                int curStyle = GetWindowLong(rbxHwnd, GWL_STYLE);
                int targetStyle = (curStyle & ~WS_CHILD & ~WS_CAPTION & ~WS_THICKFRAME & ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX & ~WS_SYSMENU) | WS_POPUP | WS_VISIBLE;
                if (curStyle != targetStyle)
                {
                    SetWindowLong(rbxHwnd, GWL_STYLE, targetStyle);
                }

                // 4. Hide from taskbar only if styles differ
                int curExStyle = GetWindowLong(rbxHwnd, GWL_EXSTYLE);
                int targetExStyle = (curExStyle & ~WS_EX_APPWINDOW) | WS_EX_TOOLWINDOW;
                if (curExStyle != targetExStyle)
                {
                    SetWindowLong(rbxHwnd, GWL_EXSTYLE, targetExStyle);
                }

                RECT edgeRect;
                GetWindowRect(cachedEdgeHwnd, out edgeRect);
                int w = edgeRect.Right - edgeRect.Left;
                int h = edgeRect.Bottom - edgeRect.Top;
                if (w <= 0 || h <= 0) return;

                bool isToolsOpen = title.IndexOf("Tools Open", StringComparison.OrdinalIgnoreCase) >= 0;

                // Only sync from title if the title actually changed in the browser (e.g. user clicked UI button)
                if (title != lastObservedTitle)
                {
                    lastObservedTitle = title;
                    if (title.IndexOf("Fullscreen", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isRobloxFullscreen = true;
                    }
                    else if (title.IndexOf("Windowed", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isRobloxFullscreen = false;
                    }
                }

                // F11 global toggle: detects key transition reliably anywhere on the system
                bool f11Down = (GetAsyncKeyState(0x7A) & 0x8000) != 0; // VK_F11
                if (f11Down && !prevF11Down)
                {
                    isRobloxFullscreen = !isRobloxFullscreen;
                    lastObservedTitle = isRobloxFullscreen
                        ? "NeuroSpace Studio — Roblox Active — Fullscreen"
                        : (isToolsOpen ? "NeuroSpace Studio — Roblox Active — Tools Open — Windowed" : "NeuroSpace Studio — Roblox Active — Windowed");
                }
                prevF11Down = f11Down;

                if (isRobloxActive)
                {
                    int targetX, targetY, targetWidth, targetHeight;

                    if (isRobloxFullscreen)
                    {
                        // VOLLBILD: Roblox covers entire screen
                        targetX = edgeRect.Left;
                        targetY = edgeRect.Top;
                        targetWidth = w;
                        targetHeight = h;
                    }
                    else
                    {
                        // MINIMIERT / STUDIO-MODUS: Roblox docked below NeuroSpace options bar (48px) and above bottom bar (26px)
                        int targetTop = 48;
                        int targetBottom = 26;
                        int leftOffset = isToolsOpen ? 340 : 0;

                        targetX = edgeRect.Left + leftOffset;
                        targetY = edgeRect.Top + targetTop;
                        targetWidth = Math.Max(100, w - leftOffset);
                        targetHeight = Math.Max(100, h - targetTop - targetBottom);
                    }

                    // ZERO-FLICKER: Only touch window position if dimensions or visibility changed!
                    if (targetX != lastAppliedX || targetY != lastAppliedY ||
                        targetWidth != lastAppliedW || targetHeight != lastAppliedH ||
                        !lastAppliedVisible)
                    {
                        lastAppliedX = targetX;
                        lastAppliedY = targetY;
                        lastAppliedW = targetWidth;
                        lastAppliedH = targetHeight;
                        lastAppliedVisible = true;

                        ShowWindow(rbxHwnd, SW_SHOW);
                        SetWindowPos(rbxHwnd, (IntPtr)0, targetX, targetY, targetWidth, targetHeight,
                            SWP_NOACTIVATE | SWP_NOCOPYBITS | SWP_ASYNCWINDOWPOS | SWP_SHOWWINDOW);
                    }
                }
                else
                {
                    if (lastAppliedVisible)
                    {
                        lastAppliedVisible = false;
                        lastAppliedX = -99999;
                        lastAppliedY = -99999;
                        lastAppliedW = -99999;
                        lastAppliedH = -99999;
                        ShowWindow(rbxHwnd, SW_HIDE);
                    }
                }
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
                    StringBuilder cls = new StringBuilder(256);
                    GetClassName(hWnd, cls, 256);
                    if (cls.ToString() == "Chrome_WidgetWin_1")
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

                string[] programDirs = new string[] {
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)
                };
                foreach (string pd in programDirs)
                {
                    if (Directory.Exists(pd))
                    {
                        string rbxProg = Path.Combine(pd, "Roblox");
                        if (Directory.Exists(rbxProg))
                        {
                            try { Directory.Delete(rbxProg, true); } catch { }
                        }
                        foreach (string lnk in Directory.GetFiles(pd, "*Roblox*.lnk", SearchOption.AllDirectories))
                        {
                            try { File.Delete(lnk); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        public static void RemoveRobloxAutostart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        foreach (string valName in key.GetValueNames())
                        {
                            if (valName.IndexOf("roblox", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                try { key.DeleteValue(valName); } catch { }
                            }
                            else
                            {
                                object v = key.GetValue(valName);
                                if (v != null && v.ToString().IndexOf("roblox", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    try { key.DeleteValue(valName); } catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                string[] startupDirs = new string[] {
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
                };
                foreach (string sd in startupDirs)
                {
                    if (Directory.Exists(sd))
                    {
                        foreach (string f in Directory.GetFiles(sd, "*roblox*.*"))
                        {
                            try { File.Delete(f); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        public static void KillRobloxProcess()
        {
            try
            {
                string[] processNames = new string[] {
                    "RobloxPlayerBeta",
                    "RobloxCrashHandler",
                    "RobloxPlayerLauncher",
                    "RobloxPlayerInstaller"
                };

                foreach (string procName in processNames)
                {
                    try
                    {
                        Process[] procs = Process.GetProcessesByName(procName);
                        foreach (Process p in procs)
                        {
                            try
                            {
                                if (!p.HasExited)
                                {
                                    p.Kill();
                                    p.WaitForExit(500);
                                }
                            }
                            catch { }
                            finally
                            {
                                try { p.Dispose(); } catch { }
                            }
                        }
                    }
                    catch { }
                }

                // Extra safety taskkill fallback to ensure zero lingering background instances
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "cmd.exe";
                    psi.Arguments = "/c taskkill /F /IM RobloxPlayerBeta.exe /IM RobloxCrashHandler.exe /IM RobloxPlayerLauncher.exe /T >nul 2>&1";
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    using (Process tk = Process.Start(psi))
                    {
                        if (tk != null) tk.WaitForExit(800);
                    }
                }
                catch { }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                KillRobloxProcess();
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
                AppDomain.CurrentDomain.ProcessExit += (s, ev) => { HotkeyWatcherForm.KillRobloxProcess(); };
                Application.ApplicationExit += (s, ev) => { HotkeyWatcherForm.KillRobloxProcess(); };

                HotkeyWatcherForm.KillRobloxProcess();
                HotkeyWatcherForm.CleanRobloxDesktopShortcuts();
                HotkeyWatcherForm.RemoveRobloxAutostart();
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
                    "--app=\"{0}\" --start-fullscreen --start-maximized --user-data-dir=\"C:\\NeuroSpace\\EdgeData\" --autoplay-policy=no-user-gesture-required --disable-features=PreloadMediaEngagementData,AutoplayIgnoreWebAudio --no-first-run --no-default-browser-check",
                    htmlUri
                );

                if (browserExe != null)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = browserExe;
                    psi.Arguments = flags;
                    psi.UseShellExecute = false;
                    Process.Start(psi);
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetHtml,
                        UseShellExecute = true
                    });
                }

                // Run in-app embedder & hotkey watcher form
                Application.Run(new HotkeyWatcherForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Starten von NeuroSpace: " + ex.Message, "NeuroSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
