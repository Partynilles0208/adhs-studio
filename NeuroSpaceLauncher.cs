using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;

namespace NeuroSpaceLauncher
{
    public class HotkeyWatcherForm : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int MOD_ALT = 0x0001;
        private const int VK_B = 0x42;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9920;

        private Process browserProcess;

        public HotkeyWatcherForm(Process proc)
        {
            this.browserProcess = proc;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new System.Drawing.Size(0, 0);

            // Register system-wide Alt+B hotkey for instant minimize to desktop
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, VK_B);

            if (proc != null)
            {
                proc.EnableRaisingEvents = true;
                proc.Exited += delegate
                {
                    try
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            UnregisterHotKey(this.Handle, HOTKEY_ID);
                            Application.Exit();
                        });
                    }
                    catch { }
                };
            }
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
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType != null)
                {
                    object shell = Activator.CreateInstance(shellType);
                    shellType.InvokeMember("MinimizeAll", BindingFlags.InvokeMethod, null, shell, null);
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { UnregisterHotKey(this.Handle, HOTKEY_ID); } catch { }
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
                    "--app=\"{0}\" --autoplay-policy=no-user-gesture-required --disable-features=PreloadMediaEngagementData,AutoplayIgnoreWebAudio --no-first-run --no-default-browser-check",
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

                // Run lightweight background watcher for Alt+B Boss-Key Minimize
                Application.Run(new HotkeyWatcherForm(browserProc));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Starten von NeuroSpace: " + ex.Message, "NeuroSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
