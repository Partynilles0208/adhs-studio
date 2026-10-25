using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using Microsoft.Win32;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace NeuroSpaceInstaller
{
    public class InstallerWizardForm : Form
    {
        private Panel pnlHeader;
        private Panel pnlContent;
        private Panel pnlFooter;
        private Label lblHeaderTitle;
        private Label lblHeaderSub;
        private Button btnNext;
        private Button btnBack;
        private Button btnClose;

        // Step Panels
        private Panel stepWelcome;
        private Panel stepTerms;
        private Panel stepOptions;
        private Panel stepInstall;
        private Panel stepFinish;

        // Terms controls
        private CheckBox chkAcceptTerms;
        private TextBox txtTerms;

        // Options controls
        private TextBox txtTargetDir;
        private CheckBox chkDesktopShortcut;
        private CheckBox chkAppMode;

        // Install controls
        private ProgressBar progressBar;
        private Label lblInstallStatus;

        private int currentStep = 1;
        public const string TARGET_DIR = @"C:\NeuroSpace";

        public InstallerWizardForm()
        {
            this.Text = "NeuroSpace Studio — Professioneller Installationsassistent";
            this.Size = new Size(680, 500);
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

            BuildLayout();
            ShowStep(1);
        }

        private void BuildLayout()
        {
            // HEADER PANEL
            pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 74;
            pnlHeader.BackColor = Color.FromArgb(28, 18, 12);
            pnlHeader.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(245, 158, 11), 1.5f))
                {
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            lblHeaderTitle = new Label();
            lblHeaderTitle.Text = "🍁 NeuroSpace Studio — Herbst-Edition";
            lblHeaderTitle.Location = new Point(24, 14);
            lblHeaderTitle.Size = new Size(540, 26);
            lblHeaderTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblHeaderTitle.ForeColor = Color.FromArgb(251, 191, 36);
            pnlHeader.Controls.Add(lblHeaderTitle);

            lblHeaderSub = new Label();
            lblHeaderSub.Text = "Professioneller Offline-Installationsassistent für Windows";
            lblHeaderSub.Location = new Point(26, 42);
            lblHeaderSub.Size = new Size(540, 20);
            lblHeaderSub.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblHeaderSub.ForeColor = Color.FromArgb(214, 211, 209);
            pnlHeader.Controls.Add(lblHeaderSub);

            btnClose = new Button();
            btnClose.Text = "✕";
            btnClose.Size = new Size(36, 32);
            btnClose.Location = new Point(this.Width - 46, 12);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.ForeColor = Color.FromArgb(245, 158, 11);
            btnClose.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += delegate(object s, EventArgs e) { this.Close(); };
            pnlHeader.Controls.Add(btnClose);

            // Drag form anywhere via header
            pnlHeader.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    this.Capture = false;
                    Message msg = Message.Create(this.Handle, 0xa1, new IntPtr(2), IntPtr.Zero);
                    this.WndProc(ref msg);
                }
            };
            this.Controls.Add(pnlHeader);

            // FOOTER PANEL
            pnlFooter = new Panel();
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Height = 64;
            pnlFooter.BackColor = Color.FromArgb(24, 16, 10);
            pnlFooter.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(245, 158, 11), 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
                }
            };

            btnBack = new Button();
            btnBack.Text = "← Zurück";
            btnBack.Size = new Size(110, 38);
            btnBack.Location = new Point(24, 13);
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(245, 158, 11);
            btnBack.BackColor = Color.FromArgb(38, 26, 18);
            btnBack.ForeColor = Color.FromArgb(254, 243, 199);
            btnBack.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnBack.Cursor = Cursors.Hand;
            btnBack.Click += delegate(object s, EventArgs e) { Navigate(-1); };
            pnlFooter.Controls.Add(btnBack);

            btnNext = new Button();
            btnNext.Text = "Weiter →";
            btnNext.Size = new Size(140, 38);
            btnNext.Location = new Point(this.Width - 164, 13);
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.FlatAppearance.BorderColor = Color.FromArgb(245, 158, 11);
            btnNext.BackColor = Color.FromArgb(180, 83, 9);
            btnNext.ForeColor = Color.White;
            btnNext.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnNext.Cursor = Cursors.Hand;
            btnNext.Click += delegate(object s, EventArgs e) { Navigate(1); };
            pnlFooter.Controls.Add(btnNext);

            this.Controls.Add(pnlFooter);

            // CONTENT PANEL
            pnlContent = new Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = Color.FromArgb(18, 12, 8);
            this.Controls.Add(pnlContent);

            BuildSteps();
        }

        private void BuildSteps()
        {
            // === STEP 1: WELCOME ===
            stepWelcome = new Panel();
            stepWelcome.Dock = DockStyle.Fill;

            Label lblWTitle = new Label();
            lblWTitle.Text = "Willkommen bei NeuroSpace Studio";
            lblWTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblWTitle.ForeColor = Color.FromArgb(251, 191, 36);
            lblWTitle.Location = new Point(30, 24);
            lblWTitle.Size = new Size(600, 32);
            stepWelcome.Controls.Add(lblWTitle);

            Label lblWDesc = new Label();
            lblWDesc.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblWDesc.ForeColor = Color.FromArgb(226, 232, 240);
            lblWDesc.Location = new Point(32, 65);
            lblWDesc.Size = new Size(610, 80);
            lblWDesc.Text = "Dieser Assistent installiert NeuroSpace Studio auf Ihrem Computer.\n\n" +
                           "NeuroSpace ist eine sensorische Ruheoase mit 30 interaktiven Simulationen, " +
                           "ADHS- & Autismus-Tools, Kamin-Atmosphäre und einer integrierten Arcade.\n" +
                           "Die App läuft zu 100% lokal und benötigt keine dauerhafte Internetverbindung.";
            stepWelcome.Controls.Add(lblWDesc);

            Panel pnlHighlights = new Panel();
            pnlHighlights.Location = new Point(32, 160);
            pnlHighlights.Size = new Size(610, 160);
            pnlHighlights.BackColor = Color.FromArgb(28, 18, 12);
            pnlHighlights.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(245, 158, 11), 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlHighlights.Width - 1, pnlHighlights.Height - 1);
                }
            };

            Label lblHiText = new Label();
            lblHiText.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblHiText.ForeColor = Color.FromArgb(254, 243, 199);
            lblHiText.Location = new Point(16, 14);
            lblHiText.Size = new Size(575, 130);
            lblHiText.Text = "🍁 Highlights dieser Version:\n" +
                            "• 30 Sensorische Apps: Pixel-Gravitation, Sand-Kaskade, Buckyballs 3D, uvm.\n" +
                            "• Autoklicker & Live-FPS Werkzeuge: Anpassbar auf der linken Bildschirmseite\n" +
                            "• Normales Roblox nahtlos im Studio: Direkte Server-Verbindung & beliebte Erlebnisse ohne separates Fenster\n" +
                            "• Autoplay ohne Klicks: Musik und Videos starten automatisch ohne Browser-Restriktionen\n" +
                            "• Privatsphäre garantiert: Daten verbleiben lokal in C:\\NeuroSpace";
            pnlHighlights.Controls.Add(lblHiText);
            stepWelcome.Controls.Add(pnlHighlights);

            pnlContent.Controls.Add(stepWelcome);

            // === STEP 2: TERMS & PRIVACY ===
            stepTerms = new Panel();
            stepTerms.Dock = DockStyle.Fill;

            Label lblTTitle = new Label();
            lblTTitle.Text = "Datenschutzbestimmungen & Nutzungsbedingungen";
            lblTTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTTitle.ForeColor = Color.FromArgb(251, 191, 36);
            lblTTitle.Location = new Point(30, 18);
            lblTTitle.Size = new Size(600, 26);
            stepTerms.Controls.Add(lblTTitle);

            txtTerms = new TextBox();
            txtTerms.Multiline = true;
            txtTerms.ReadOnly = true;
            txtTerms.ScrollBars = ScrollBars.Vertical;
            txtTerms.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            txtTerms.BackColor = Color.FromArgb(24, 16, 10);
            txtTerms.ForeColor = Color.FromArgb(226, 232, 240);
            txtTerms.Location = new Point(32, 52);
            txtTerms.Size = new Size(610, 240);
            txtTerms.Text =
                "1. DATENSCHUTZBESTIMMUNGEN (PRIVACY POLICY)\r\n" +
                "===========================================\r\n" +
                "• NeuroSpace Studio respektiert Ihre Privatsphäre zu 100%.\r\n" +
                "• Es werden KEINE personenbezogenen Daten, Surf-Gewohnheiten oder Passwörter erfasst oder an Server übermittelt.\r\n" +
                "• Alle Konfigurationen, Profile und Spielstände verbleiben ausschließlich lokal auf Ihrem Rechner (unter C:\\NeuroSpace).\r\n" +
                "• 100% Offline-Garantie, DSGVO-konform, keine Werbe-Tracker, keine Weitergabe an Dritte.\r\n\r\n" +
                "2. WICHTIGE NUTZUNGSBEDINGUNGEN & ÄNDERUNGSVORBEHALT\r\n" +
                "====================================================\r\n" +
                "• Die Software wird für den privaten, therapeutischen und pädagogischen Gebrauch zur Verfügung gestellt.\r\n" +
                "• Das Studio dient der Entspannung, Reizminderung und dem Fokus bei ADHS und Autismus-Spektrum-Bedingungen.\r\n" +
                "• WICHTIGER ÄNDERUNGSVORBEHALT: Sämtliche Bestimmungen, Sicherheitsrichtlinien sowie Spiele und Features können sich durch fortlaufende Updates jederzeit ändern.\r\n\r\n" +
                "3. GESUNDHEITSSCHUTZ & HERZ-KREISLAUF-SICHERHEIT\r\n" +
                "================================================\r\n" +
                "• EPILEPSIE: Dynamische 3D-Simulationen enthalten Licht- und Kontrastwechsel. Personen mit photosensibler Epilepsie sollten Pausen einlegen und bei Unwohlsein die Nutzung sofort stoppen.\r\n" +
                "• HERZRHYTHMUSSTÖRUNGEN: Bei bekannten Herzrhythmusstörungen oder kardiovaskulären Vorerkrankungen ist das Studio mit besonderer Achtsamkeit zu nutzen. Schnelle Reize und Gaming-Szenen (z. B. Roblox) können Puls und Kreislauf belasten.\r\n" +
                "• REALE GEFAHR BEI NOCH UNENTDECKTEN HERZKRANKHEITEN: Auch bei Personen ohne diagnostizierte Herzerkrankung können unentdeckte oder symptomlose Vorerkrankungen vorliegen. Bei Schreckmomenten oder Reizüberflutung besteht eine reale Gefahr für plötzliche Adrenalinausschüttung und Herzrasen. Bei Herzklopfen, Schwindel, Engegefühl oder Unwohlsein Nutzung sofort stoppen und ärztlich abklären!";
            stepTerms.Controls.Add(txtTerms);

            chkAcceptTerms = new CheckBox();
            chkAcceptTerms.Text = "Ich habe die Datenschutzbestimmungen und Nutzungsbedingungen gelesen und akzeptiere sie.";
            chkAcceptTerms.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkAcceptTerms.ForeColor = Color.FromArgb(254, 243, 199);
            chkAcceptTerms.Location = new Point(32, 304);
            chkAcceptTerms.Size = new Size(610, 28);
            chkAcceptTerms.Cursor = Cursors.Hand;
            chkAcceptTerms.CheckedChanged += delegate(object s, EventArgs e)
            {
                btnNext.Enabled = chkAcceptTerms.Checked;
            };
            stepTerms.Controls.Add(chkAcceptTerms);

            pnlContent.Controls.Add(stepTerms);

            // === STEP 3: OPTIONS ===
            stepOptions = new Panel();
            stepOptions.Dock = DockStyle.Fill;

            Label lblOTitle = new Label();
            lblOTitle.Text = "Installationspfad & Desktop-Optionen";
            lblOTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblOTitle.ForeColor = Color.FromArgb(251, 191, 36);
            lblOTitle.Location = new Point(30, 20);
            lblOTitle.Size = new Size(600, 26);
            stepOptions.Controls.Add(lblOTitle);

            Label lblDirLabel = new Label();
            lblDirLabel.Text = "Zielordner auf Ihrem Computer:";
            lblDirLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblDirLabel.ForeColor = Color.FromArgb(214, 211, 209);
            lblDirLabel.Location = new Point(32, 60);
            lblDirLabel.Size = new Size(600, 20);
            stepOptions.Controls.Add(lblDirLabel);

            txtTargetDir = new TextBox();
            txtTargetDir.Text = TARGET_DIR;
            txtTargetDir.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            txtTargetDir.BackColor = Color.FromArgb(28, 18, 12);
            txtTargetDir.ForeColor = Color.FromArgb(251, 191, 36);
            txtTargetDir.Location = new Point(32, 85);
            txtTargetDir.Size = new Size(610, 28);
            txtTargetDir.ReadOnly = true;
            stepOptions.Controls.Add(txtTargetDir);

            Panel pnlOptBox = new Panel();
            pnlOptBox.Location = new Point(32, 135);
            pnlOptBox.Size = new Size(610, 180);
            pnlOptBox.BackColor = Color.FromArgb(28, 18, 12);
            pnlOptBox.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(245, 158, 11), 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlOptBox.Width - 1, pnlOptBox.Height - 1);
                }
            };

            chkDesktopShortcut = new CheckBox();
            chkDesktopShortcut.Checked = true;
            chkDesktopShortcut.Text = "Desktop-Verknüpfung 'NeuroSpace Studio' mit individuellem Herbst-Icon anlegen";
            chkDesktopShortcut.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkDesktopShortcut.ForeColor = Color.FromArgb(254, 243, 199);
            chkDesktopShortcut.Location = new Point(16, 20);
            chkDesktopShortcut.Size = new Size(575, 26);
            chkDesktopShortcut.Cursor = Cursors.Hand;
            pnlOptBox.Controls.Add(chkDesktopShortcut);

            chkAppMode = new CheckBox();
            chkAppMode.Checked = true;
            chkAppMode.Text = "Eigener App-Modus ohne Browser-Leiste (Startet sofort mit Ton & ohne Klickzwang)";
            chkAppMode.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkAppMode.ForeColor = Color.FromArgb(254, 243, 199);
            chkAppMode.Location = new Point(16, 60);
            chkAppMode.Size = new Size(575, 26);
            chkAppMode.Cursor = Cursors.Hand;
            pnlOptBox.Controls.Add(chkAppMode);

            Label lblOptDesc = new Label();
            lblOptDesc.Text = "⚡ Durch den App-Modus startet NeuroSpace in einem randlosen Anwendungsfenster. " +
                             "Alle Audio- und Videoeffekte starten ohne störende Browser-Blockaden.";
            lblOptDesc.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblOptDesc.ForeColor = Color.FromArgb(214, 211, 209);
            lblOptDesc.Location = new Point(38, 95);
            lblOptDesc.Size = new Size(545, 65);
            pnlOptBox.Controls.Add(lblOptDesc);

            stepOptions.Controls.Add(pnlOptBox);
            pnlContent.Controls.Add(stepOptions);

            // === STEP 4: INSTALLING ===
            stepInstall = new Panel();
            stepInstall.Dock = DockStyle.Fill;

            Label lblITitle = new Label();
            lblITitle.Text = "Installation läuft...";
            lblITitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblITitle.ForeColor = Color.FromArgb(251, 191, 36);
            lblITitle.Location = new Point(30, 40);
            lblITitle.Size = new Size(600, 26);
            stepInstall.Controls.Add(lblITitle);

            lblInstallStatus = new Label();
            lblInstallStatus.Text = "Dateien werden übertragen nach C:\\NeuroSpace...";
            lblInstallStatus.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblInstallStatus.ForeColor = Color.FromArgb(245, 158, 11);
            lblInstallStatus.Location = new Point(32, 90);
            lblInstallStatus.Size = new Size(610, 24);
            stepInstall.Controls.Add(lblInstallStatus);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(32, 125);
            progressBar.Size = new Size(610, 28);
            progressBar.Style = ProgressBarStyle.Continuous;
            stepInstall.Controls.Add(progressBar);

            pnlContent.Controls.Add(stepInstall);

            // === STEP 5: FINISH ===
            stepFinish = new Panel();
            stepFinish.Dock = DockStyle.Fill;

            Label lblFTitle = new Label();
            lblFTitle.Text = "✨ Installation erfolgreich abgeschlossen!";
            lblFTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblFTitle.ForeColor = Color.FromArgb(74, 222, 128); // Green
            lblFTitle.Location = new Point(30, 40);
            lblFTitle.Size = new Size(600, 32);
            stepFinish.Controls.Add(lblFTitle);

            Label lblFDesc = new Label();
            lblFDesc.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            lblFDesc.ForeColor = Color.FromArgb(226, 232, 240);
            lblFDesc.Location = new Point(32, 90);
            lblFDesc.Size = new Size(610, 100);
            lblFDesc.Text = "NeuroSpace Studio wurde erfolgreich auf Ihrem Computer eingerichtet.\r\n\r\n" +
                           "• Zielordner: C:\\NeuroSpace\r\n" +
                           "• Desktop-Verknüpfung: 'NeuroSpace Studio' wurde auf dem Desktop angelegt.\r\n" +
                           "• Sie können das Studio nun jederzeit offline und ohne Browser-Leiste nutzen.";
            stepFinish.Controls.Add(lblFDesc);

            Button btnStartNow = new Button();
            btnStartNow.Text = "🍁 NeuroSpace Studio jetzt starten";
            btnStartNow.Size = new Size(610, 52);
            btnStartNow.Location = new Point(32, 220);
            btnStartNow.FlatStyle = FlatStyle.Flat;
            btnStartNow.FlatAppearance.BorderColor = Color.FromArgb(245, 158, 11);
            btnStartNow.BackColor = Color.FromArgb(22, 163, 74);
            btnStartNow.ForeColor = Color.White;
            btnStartNow.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnStartNow.Cursor = Cursors.Hand;
            btnStartNow.Click += delegate(object s, EventArgs e)
            {
                LaunchInstalledApp();
                this.Close();
            };
            stepFinish.Controls.Add(btnStartNow);

            pnlContent.Controls.Add(stepFinish);
        }

        private void ShowStep(int step)
        {
            currentStep = step;
            stepWelcome.Visible = (step == 1);
            stepTerms.Visible = (step == 2);
            stepOptions.Visible = (step == 3);
            stepInstall.Visible = (step == 4);
            stepFinish.Visible = (step == 5);

            btnBack.Visible = (step > 1 && step < 4);

            if (step == 1)
            {
                btnNext.Text = "Weiter →";
                btnNext.Enabled = true;
            }
            else if (step == 2)
            {
                btnNext.Text = "Weiter →";
                btnNext.Enabled = chkAcceptTerms.Checked;
            }
            else if (step == 3)
            {
                btnNext.Text = "Installieren ➔";
                btnNext.Enabled = true;
            }
            else if (step == 4)
            {
                btnNext.Visible = false;
                btnBack.Visible = false;
                StartInstallTask();
            }
            else if (step == 5)
            {
                btnNext.Text = "Beenden";
                btnNext.Visible = true;
                btnNext.Enabled = true;
                btnBack.Visible = false;
            }
        }

        private void Navigate(int direction)
        {
            if (currentStep == 5 && direction > 0)
            {
                this.Close();
                return;
            }

            int next = currentStep + direction;
            if (next >= 1 && next <= 5)
            {
                ShowStep(next);
            }
        }

        private void StartInstallTask()
        {
            Thread t = new Thread(delegate()
            {
                PerformCoreInstall((pct, msg) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        progressBar.Value = Math.Min(100, Math.Max(0, pct));
                        lblInstallStatus.Text = msg;
                    });
                });

                this.Invoke((MethodInvoker)delegate
                {
                    ShowStep(5);
                });
            });
            t.IsBackground = true;
            t.Start();
        }

        public static void PerformCoreInstall(Action<int, string> reportProgress)
        {
            if (reportProgress != null) reportProgress(10, "1/6: Zielverzeichnis " + TARGET_DIR + " vorbereiten...");
            Thread.Sleep(200);

            if (!Directory.Exists(TARGET_DIR))
            {
                Directory.CreateDirectory(TARGET_DIR);
            }

            if (reportProgress != null) reportProgress(25, "2/6: Übertrage Quellcode & Kern-Engine (index.html)...");
            Thread.Sleep(200);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetHtml = Path.Combine(TARGET_DIR, "index.html");

            // WICHTIG: IMMER die eingebettete offizielle Version von NeuroSpace extrahieren!
            // Niemals fremde index.html aus dem Downloads-Ordner vorziehen!
            bool htmlOk = ExtractEmbeddedFileStatic("index.html", targetHtml);
            if (!htmlOk)
            {
                string src = Path.Combine(curDir, "index.html");
                if (File.Exists(src)) try { File.Copy(src, targetHtml, true); } catch { }
            }

            if (reportProgress != null) reportProgress(45, "3/6: Übertrage Audio & Ambient-Hintergrundvideo...");
            Thread.Sleep(250);

            string[] mediaFiles = new string[] {
                "background.mp3",
                "backround.mp4",
                "neurospace.ico",
                "neurospace_icon.png"
            };

            foreach (string mf in mediaFiles)
            {
                string dst = Path.Combine(TARGET_DIR, mf);
                bool ok = ExtractEmbeddedFileStatic(mf, dst);
                if (!ok)
                {
                    string src = Path.Combine(curDir, mf);
                    if (File.Exists(src)) try { File.Copy(src, dst, true); } catch { }
                }
            }

            if (reportProgress != null) reportProgress(65, "4/6: Übertrage Sicherheitsdokumente & Epilepsie-Warnung...");
            Thread.Sleep(200);

            string[] warningFiles = new string[] {
                "epilepsie warnung.jpg",
                "elepiesie warnung.jpg",
                "elepisie warnung.jpg",
                "nutzung.jpg"
            };

            foreach (string wf in warningFiles)
            {
                string dst = Path.Combine(TARGET_DIR, wf);
                bool ok = ExtractEmbeddedFileStatic(wf, dst);
                if (!ok)
                {
                    string src = Path.Combine(curDir, wf);
                    if (File.Exists(src)) try { File.Copy(src, dst, true); } catch { }
                }
            }

            // Dedicated frameless launcher executable
            string exeLauncher = Path.Combine(TARGET_DIR, "NeuroSpace.exe");
            bool exeOk = ExtractEmbeddedFileStatic("NeuroSpace.exe", exeLauncher);
            if (!exeOk)
            {
                string srcExe = Path.Combine(curDir, "NeuroSpace.exe");
                if (File.Exists(srcExe)) try { File.Copy(srcExe, exeLauncher, true); } catch { }
            }

            // Clear any stale browser session caches so Edge never restores old tabs
            try
            {
                string[] sessionDirs = new string[] {
                    Path.Combine(TARGET_DIR, @"EdgeData\Default\Sessions"),
                    Path.Combine(TARGET_DIR, @"EdgeData\Default\EdgeSessions"),
                    Path.Combine(TARGET_DIR, @"EdgeData\Default\Sessions_Encrypted")
                };
                foreach (string sd in sessionDirs)
                {
                    if (Directory.Exists(sd)) Directory.Delete(sd, true);
                }
            }
            catch { }

            // 4/6: Lade Roblox-Bestandteil herunter & installiere nahtlos in NeuroSpace
            string robloxDst = Path.Combine(TARGET_DIR, "Roblox");
            try
            {
                Directory.CreateDirectory(robloxDst);
                string installerExe = Path.Combine(robloxDst, "RobloxPlayerInstaller.exe");

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string robloxSrc = Path.Combine(localAppData, "Roblox");
                bool isRobloxInstalled = false;

                if (Directory.Exists(robloxSrc))
                {
                    string[] playerExes = Directory.GetFiles(robloxSrc, "RobloxPlayerBeta.exe", SearchOption.AllDirectories);
                    if (playerExes != null && playerExes.Length > 0)
                    {
                        isRobloxInstalled = true;
                    }
                }

                // 1. Download RobloxPlayerInstaller.exe with live download progress if needed
                if (!isRobloxInstalled && !File.Exists(installerExe))
                {
                    string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    string localDl = Path.Combine(userDownloads, "RobloxPlayerInstaller.exe");

                    if (File.Exists(localDl))
                    {
                        if (reportProgress != null) reportProgress(70, "4/6: Lokaler Roblox-Installer gefunden, bereite vor...");
                        try { File.Copy(localDl, installerExe, true); } catch { }
                        Thread.Sleep(300);
                    }
                    else
                    {
                        if (reportProgress != null) reportProgress(68, "4/6: Verbinde mit Download-Server für Roblox-Komponente...");
                        try
                        {
                            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                            using (WebClient client = new WebClient())
                            {
                                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) NeuroSpaceStudio/2.0");

                                DateTime lastReport = DateTime.MinValue;
                                client.DownloadProgressChanged += delegate(object s, DownloadProgressChangedEventArgs e)
                                {
                                    if ((DateTime.Now - lastReport).TotalMilliseconds > 80 || e.ProgressPercentage == 100)
                                    {
                                        lastReport = DateTime.Now;
                                        int pct = 68 + (int)(e.ProgressPercentage * 0.10); // 68% -> 78%
                                        string mbRecv = (e.BytesReceived / 1048576.0).ToString("0.0");
                                        string mbTotal = (e.TotalBytesToReceive / 1048576.0).ToString("0.0");
                                        if (reportProgress != null)
                                        {
                                            reportProgress(pct, string.Format("4/6: Lade Roblox herunter... {0}% ({1} MB / {2} MB)", e.ProgressPercentage, mbRecv, mbTotal));
                                        }
                                    }
                                };

                                AutoResetEvent downloadDone = new AutoResetEvent(false);
                                client.DownloadFileCompleted += delegate(object s, System.ComponentModel.AsyncCompletedEventArgs e)
                                {
                                    downloadDone.Set();
                                };

                                client.DownloadFileAsync(new Uri("https://setup.rbxcdn.com/RobloxPlayerInstaller.exe"), installerExe);
                                downloadDone.WaitOne(60000);
                            }
                        }
                        catch { }
                    }
                }

                // 2. Execute installer if not yet installed
                if (!isRobloxInstalled && File.Exists(installerExe))
                {
                    if (reportProgress != null) reportProgress(78, "4/6: Roblox erfolgreich heruntergeladen! Starte Installation...");
                    Thread.Sleep(300);

                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = installerExe;
                        psi.UseShellExecute = true;
                        Process rbxProc = Process.Start(psi);
                        if (rbxProc != null)
                        {
                            int elapsed = 0;
                            while (!rbxProc.HasExited && elapsed < 180)
                            {
                                Thread.Sleep(1000);
                                elapsed++;
                                CleanRobloxExternalShortcuts();
                                RemoveRobloxAutostart();
                                int progressVal = Math.Min(84, 78 + (elapsed / 4));
                                if (reportProgress != null)
                                {
                                    reportProgress(progressVal, string.Format("4/6: Installiere Roblox als integrierten Bestandteil ({0}s)...", elapsed));
                                }
                            }
                        }
                    }
                    catch { }
                }
                else
                {
                    if (reportProgress != null) reportProgress(78, "4/6: Roblox-Bestandteil ist bereits installiert und startklar!");
                    Thread.Sleep(300);
                }

                // Kill any auto-started RobloxPlayerBeta that the installer launched
                try
                {
                    foreach (Process p in Process.GetProcessesByName("RobloxPlayerBeta"))
                    {
                        try { p.Kill(); } catch { }
                    }
                }
                catch { }

                // 3. WICHTIGSTE ANFORDERUNG: KEINE VERKNÜPFUNG & KEIN AUTOSTART!
                if (reportProgress != null) reportProgress(84, "4/6: Entferne Roblox-Verknüpfungen & deaktiviere Autostart...");
                Thread.Sleep(200);
                CleanRobloxExternalShortcuts();
                RemoveRobloxAutostart();
            }
            catch { }

            // Clean up any legacy custom apps file
            try { string legacy = Path.Combine(TARGET_DIR, "neurospace_apps.json"); if (File.Exists(legacy)) File.Delete(legacy); } catch { }

            if (reportProgress != null) reportProgress(85, "5/6: Erstelle randlosen Launcher (ohne Browser-Leiste)...");
            Thread.Sleep(200);

            // Launcher with autoplay policy bypassed & dedicated app mode
            string batPath = Path.Combine(TARGET_DIR, "NeuroSpace.bat");
            string batContent = "@echo off\r\n" +
                                "cd /d \"C:\\NeuroSpace\"\r\n" +
                                "if exist \"C:\\NeuroSpace\\NeuroSpace.exe\" (\r\n" +
                                "    start \"\" \"C:\\NeuroSpace\\NeuroSpace.exe\"\r\n" +
                                "    exit\r\n" +
                                ")\r\n" +
                                "set \"EDGE=%ProgramFiles(x86)%\\Microsoft\\Edge\\Application\\msedge.exe\"\r\n" +
                                "if not exist \"%EDGE%\" set \"EDGE=%ProgramFiles%\\Microsoft\\Edge\\Application\\msedge.exe\"\r\n" +
                                "if not exist \"%EDGE%\" set \"EDGE=%LOCALAPPDATA%\\Microsoft\\Edge\\Application\\msedge.exe\"\r\n" +
                                "if exist \"%EDGE%\" (\r\n" +
                                "    start \"\" \"%EDGE%\" --app=\"file:///C:/NeuroSpace/index.html?app=desktop\" --start-fullscreen --start-maximized --user-data-dir=\"C:\\NeuroSpace\\EdgeData\" --autoplay-policy=no-user-gesture-required --disable-features=PreloadMediaEngagementData,AutoplayIgnoreWebAudio --no-first-run --no-default-browser-check\r\n" +
                                "    exit\r\n" +
                                ")\r\n" +
                                "set \"CHROME=%ProgramFiles%\\Google\\Chrome\\Application\\chrome.exe\"\r\n" +
                                "if not exist \"%CHROME%\" set \"CHROME=%ProgramFiles(x86)%\\Google\\Chrome\\Application\\chrome.exe\"\r\n" +
                                "if not exist \"%CHROME%\" set \"CHROME=%LOCALAPPDATA%\\Google\\Chrome\\Application\\chrome.exe\"\r\n" +
                                "if exist \"%CHROME%\" (\r\n" +
                                "    start \"\" \"%CHROME%\" --app=\"file:///C:/NeuroSpace/index.html?app=desktop\" --start-fullscreen --start-maximized --user-data-dir=\"C:\\NeuroSpace\\ChromeData\" --autoplay-policy=no-user-gesture-required --no-first-run --no-default-browser-check\r\n" +
                                "    exit\r\n" +
                                ")\r\n" +
                                "start \"\" \"file:///C:/NeuroSpace/index.html?app=desktop\"\r\n" +
                                "exit\r\n";
            File.WriteAllText(batPath, batContent);

            string vbsPath = Path.Combine(TARGET_DIR, "NeuroSpace.vbs");
            string vbsContent = "Set WshShell = CreateObject(\"WScript.Shell\")\r\n" +
                                "WshShell.Run chr(34) & \"" + batPath.Replace("\\", "\\\\") + "\" & chr(34), 0\r\n" +
                                "Set WshShell = Nothing\r\n";
            File.WriteAllText(vbsPath, vbsContent);

            if (reportProgress != null) reportProgress(95, "6/6: Erstelle Desktop-Verknüpfung 'NeuroSpace Studio'...");
            Thread.Sleep(150);

            string targetShortcut = File.Exists(exeLauncher) ? exeLauncher : vbsPath;
            CreateDesktopShortcutStatic(targetShortcut);

            if (reportProgress != null) reportProgress(100, "Installation erfolgreich abgeschlossen!");
            Thread.Sleep(100);
        }

                public static void CleanRobloxExternalShortcuts()
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
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        foreach (string valName in key.GetValueNames())
                        {
                            if (valName.IndexOf("roblox", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                try { key.DeleteValue(valName); } catch { }
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


        private static bool ExtractEmbeddedFileStatic(string resName, string outPath)
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
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
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

        private static void LaunchInstalledApp()
        {
            try
            {
                string exeLauncher = Path.Combine(TARGET_DIR, "NeuroSpace.exe");
                string vbs = Path.Combine(TARGET_DIR, "NeuroSpace.vbs");
                if (File.Exists(exeLauncher))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exeLauncher,
                        WorkingDirectory = TARGET_DIR,
                        UseShellExecute = true
                    });
                }
                else if (File.Exists(vbs))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "wscript.exe",
                        Arguments = "\"" + vbs + "\"",
                        WorkingDirectory = TARGET_DIR,
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "file:///C:/NeuroSpace/index.html?app=desktop",
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(Color.FromArgb(245, 158, 11), 2f))
            {
                e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 3, this.Height - 3);
            }
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
            Application.Run(new InstallerWizardForm());
        }
    }
}
