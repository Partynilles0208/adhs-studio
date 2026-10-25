using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace NeuroSpaceInstaller
{
    public class InstallerForm : Form
    {
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblTitle;
        private Label lblSub;
        private Label lblDetails;
        private Button btnAction;
        private Button btnClose;

        public const string TARGET_DIR = @"C:\NeuroSpace";

        public InstallerForm()
        {
            this.Text = "NeuroSpace Studio — Herbst-Installationsassistent";
            this.Size = new Size(640, 420);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 12, 8);
            this.ForeColor = Color.FromArgb(254, 243, 199);
            this.DoubleBuffered = true;

            try
            {
                if (File.Exists("neurospace.ico"))
                {
                    this.Icon = new Icon("neurospace.ico");
                }
            }
            catch { }

            InitUI();
            StartInstallation();
        }

        private void InitUI()
        {
            btnClose = new Button();
            btnClose.Text = "✕";
            btnClose.Size = new Size(36, 32);
            btnClose.Location = new Point(this.Width - 44, 10);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.ForeColor = Color.FromArgb(245, 158, 11);
            btnClose.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += delegate(object s, EventArgs e) { this.Close(); };
            this.Controls.Add(btnClose);

            lblTitle = new Label();
            lblTitle.Text = "🍁 NeuroSpace Studio";
            lblTitle.Location = new Point(40, 34);
            lblTitle.Size = new Size(560, 40);
            lblTitle.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(251, 191, 36);
            this.Controls.Add(lblTitle);

            lblSub = new Label();
            lblSub.Text = "Herbst-Edition • Vollwertige 100% Offline-Desktop-App";
            lblSub.Location = new Point(42, 76);
            lblSub.Size = new Size(550, 24);
            lblSub.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            lblSub.ForeColor = Color.FromArgb(214, 211, 209);
            this.Controls.Add(lblSub);

            Panel pnlInfo = new Panel();
            pnlInfo.Location = new Point(40, 112);
            pnlInfo.Size = new Size(560, 110);
            pnlInfo.BackColor = Color.FromArgb(28, 18, 12);
            pnlInfo.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(245, 158, 11), 1.2f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlInfo.Width - 1, pnlInfo.Height - 1);
                }
            };

            lblDetails = new Label();
            lblDetails.Location = new Point(16, 12);
            lblDetails.Size = new Size(528, 86);
            lblDetails.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblDetails.ForeColor = Color.FromArgb(254, 243, 199);
            lblDetails.Text = "📂 Zielverzeichnis: " + TARGET_DIR + "\n" +
                              "🖥️ Desktop-Verknüpfung: 'NeuroSpace Studio' mit individuellem Herbst-Icon\n" +
                              "⚡ 30 Sensorische Apps: Komplett lokal & offline ohne Internet nutzbar\n" +
                              "📡 Intelligenter Online-Detektor: Automatische Prüfung bei Online-Spielen";
            pnlInfo.Controls.Add(lblDetails);
            this.Controls.Add(pnlInfo);

            lblStatus = new Label();
            lblStatus.Text = "Vorbereitung der Installation...";
            lblStatus.Location = new Point(40, 238);
            lblStatus.Size = new Size(560, 22);
            lblStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(245, 158, 11);
            this.Controls.Add(lblStatus);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(40, 266);
            progressBar.Size = new Size(560, 24);
            progressBar.Style = ProgressBarStyle.Continuous;
            this.Controls.Add(progressBar);

            btnAction = new Button();
            btnAction.Text = "Installieren...";
            btnAction.Location = new Point(40, 316);
            btnAction.Size = new Size(560, 48);
            btnAction.FlatStyle = FlatStyle.Flat;
            btnAction.FlatAppearance.BorderColor = Color.FromArgb(245, 158, 11);
            btnAction.FlatAppearance.BorderSize = 1;
            btnAction.BackColor = Color.FromArgb(180, 83, 9);
            btnAction.ForeColor = Color.FromArgb(255, 255, 255);
            btnAction.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnAction.Cursor = Cursors.Hand;
            btnAction.Enabled = false;
            btnAction.Click += BtnAction_Click;
            this.Controls.Add(btnAction);

            this.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    this.Capture = false;
                    Message msg = Message.Create(this.Handle, 0xa1, new IntPtr(2), IntPtr.Zero);
                    this.WndProc(ref msg);
                }
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(Color.FromArgb(245, 158, 11), 2.5f))
            {
                e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 3, this.Height - 3);
            }
        }

        private void StartInstallation()
        {
            Thread t = new Thread(DoInstallWork);
            t.IsBackground = true;
            t.Start();
        }

        public static void PerformCoreInstall(Action<int, string> reportProgress)
        {
            if (reportProgress != null) reportProgress(15, "1/5: Prüfe Installationspfad " + TARGET_DIR + "...");
            Thread.Sleep(200);

            if (!Directory.Exists(TARGET_DIR))
            {
                Directory.CreateDirectory(TARGET_DIR);
            }

            if (reportProgress != null) reportProgress(35, "2/5: Kopiere sensorische Apps & Web-Kern...");
            Thread.Sleep(200);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;

            string targetHtml = Path.Combine(TARGET_DIR, "index.html");
            if (File.Exists(Path.Combine(curDir, "index.html")))
            {
                File.Copy(Path.Combine(curDir, "index.html"), targetHtml, true);
            }
            else
            {
                ExtractEmbeddedFileStatic("index.html", targetHtml);
            }

            string[] mediaFiles = new string[] {
                "background.mp3",
                "backround.mp4",
                "neurospace.ico",
                "neurospace_icon.png",
                "nutzung.jpg",
                "epilepsie warnung.jpg"
            };

            foreach (string mf in mediaFiles)
            {
                string src = Path.Combine(curDir, mf);
                string dst = Path.Combine(TARGET_DIR, mf);
                if (File.Exists(src))
                {
                    try { File.Copy(src, dst, true); } catch { }
                }
                else
                {
                    ExtractEmbeddedFileStatic(mf, dst);
                }
            }

            if (reportProgress != null) reportProgress(65, "3/5: Erstelle Desktop-App-Starter...");
            Thread.Sleep(200);

            string batPath = Path.Combine(TARGET_DIR, "NeuroSpace.bat");
            string batContent = "@echo off\r\n" +
                                "set \"EDGE=%ProgramFiles(x86)%\\Microsoft\\Edge\\Application\\msedge.exe\"\r\n" +
                                "if not exist \"%EDGE%\" set \"EDGE=%ProgramFiles%\\Microsoft\\Edge\\Application\\msedge.exe\"\r\n" +
                                "if exist \"%EDGE%\" (\r\n" +
                                "    start \"\" \"%EDGE%\" --app=\"file:///C:/NeuroSpace/index.html\"\r\n" +
                                ") else (\r\n" +
                                "    start \"\" \"C:\\NeuroSpace\\index.html\"\r\n" +
                                ")\r\n" +
                                "exit\r\n";
            File.WriteAllText(batPath, batContent);

            string vbsPath = Path.Combine(TARGET_DIR, "NeuroSpace.vbs");
            string vbsContent = "Set WshShell = CreateObject(\"WScript.Shell\")\r\n" +
                                "WshShell.Run chr(34) & \"" + batPath.Replace("\\", "\\\\") + "\" & chr(34), 0\r\n" +
                                "Set WshShell = Nothing\r\n";
            File.WriteAllText(vbsPath, vbsContent);

            if (reportProgress != null) reportProgress(85, "4/5: Erstelle Desktop-Verknüpfung 'NeuroSpace Studio'...");
            Thread.Sleep(200);

            CreateDesktopShortcutStatic(vbsPath);

            if (reportProgress != null) reportProgress(100, "5/5: Installation erfolgreich abgeschlossen!");
            Thread.Sleep(100);
        }

        private void DoInstallWork()
        {
            try
            {
                PerformCoreInstall((val, msg) => UpdateProgress(val, msg));

                this.Invoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "✨ Fertig! NeuroSpace ist installiert und auf deinem Desktop bereit.";
                    lblStatus.ForeColor = Color.FromArgb(74, 222, 128);
                    btnAction.Text = "🍁 NeuroSpace Studio jetzt starten";
                    btnAction.BackColor = Color.FromArgb(22, 163, 74);
                    btnAction.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "Hinweis: " + ex.Message;
                    btnAction.Text = "Trotzdem starten";
                    btnAction.Enabled = true;
                });
            }
        }

        private static void ExtractEmbeddedFileStatic(string resName, string outPath)
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string[] names = asm.GetManifestResourceNames();
                foreach (string n in names)
                {
                    if (n.EndsWith(resName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (Stream s = asm.GetManifestResourceStream(n))
                        {
                            if (s != null)
                            {
                                using (FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
                                {
                                    byte[] buf = new byte[8192];
                                    int read;
                                    while ((read = s.Read(buf, 0, buf.Length)) > 0)
                                    {
                                        fs.Write(buf, 0, read);
                                    }
                                }
                                return;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void CreateDesktopShortcutStatic(string targetPath)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutLocation = Path.Combine(desktopPath, "NeuroSpace Studio.lnk");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    object shell = Activator.CreateInstance(shellType);
                    object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutLocation });

                    Type shortcutType = shortcut.GetType();
                    shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
                    shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { TARGET_DIR });
                    shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "NeuroSpace Studio - Offline Sensorik & Autismus Hub" });

                    string iconPath = Path.Combine(TARGET_DIR, "neurospace.ico");
                    if (File.Exists(iconPath))
                    {
                        shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconPath + ",0" });
                    }

                    shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                }
            }
            catch { }
        }

        private void UpdateProgress(int val, string status)
        {
            this.Invoke((MethodInvoker)delegate
            {
                progressBar.Value = Math.Min(100, Math.Max(0, val));
                lblStatus.Text = status;
            });
        }

        private void BtnAction_Click(object sender, EventArgs e)
        {
            try
            {
                string vbs = Path.Combine(TARGET_DIR, "NeuroSpace.vbs");
                if (File.Exists(vbs))
                {
                    Process.Start("wscript.exe", "\"" + vbs + "\"");
                }
                else
                {
                    Process.Start(Path.Combine(TARGET_DIR, "index.html"));
                }
            }
            catch { }
            this.Close();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0 && args[0].ToLower().Contains("silent"))
            {
                PerformCoreInstall(null);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}
