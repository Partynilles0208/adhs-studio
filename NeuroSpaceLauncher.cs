using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace NeuroSpaceLauncher
{
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Starten von NeuroSpace: " + ex.Message, "NeuroSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
