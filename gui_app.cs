using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

public class MainForm : Form
{
    private readonly TextBox log;
    private readonly Button btnExport;
    private readonly Button btnImport;

    public MainForm()
    {
        Text = "Codex History Migration";
        Width = 560;
        Height = 380;
        StartPosition = FormStartPosition.CenterScreen;

        var title = new Label { Text = "Codex History Migration", AutoSize = true, Left = 12, Top = 12, Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold) };
        var subtitle = new Label { Text = "Choose an action:", AutoSize = true, Left = 12, Top = 40 };

        btnExport = new Button { Text = "Export (source)", Left = 12, Top = 70, Width = 140 };
        btnImport = new Button { Text = "Import (target)", Left = 160, Top = 70, Width = 140 };

        btnExport.Click += (s, e) => RunExport();
        btnImport.Click += (s, e) => RunImport();

        var logLabel = new Label { Text = "Log:", AutoSize = true, Left = 12, Top = 110 };
        log = new TextBox { Left = 12, Top = 130, Width = 520, Height = 180, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };

        var note = new Label { Text = "Note: Close Codex before importing.", AutoSize = true, Left = 12, Top = 320 };

        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(btnExport);
        Controls.Add(btnImport);
        Controls.Add(logLabel);
        Controls.Add(log);
        Controls.Add(note);
    }

    private void RunExport()
    {
        try
        {
            var src = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
            if (!Directory.Exists(src))
            {
                MessageBox.Show("Codex data not found at " + src, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var outZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "codex-history-export.zip");
            var tmp = Path.Combine(Path.GetTempPath(), "codex-export-" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tmp);

            CopyDir(Path.Combine(src, "sessions"), Path.Combine(tmp, "sessions"));
            var archived = Path.Combine(src, "archived_sessions");
            if (Directory.Exists(archived))
                CopyDir(archived, Path.Combine(tmp, "archived_sessions"));

            CopyFileIfExists(Path.Combine(src, "session_index.jsonl"), Path.Combine(tmp, "session_index.jsonl"));
            CopyFileIfExists(Path.Combine(src, "state_5.sqlite"), Path.Combine(tmp, "state_5.sqlite"));
            CopyFileIfExists(Path.Combine(src, "state_5.sqlite-wal"), Path.Combine(tmp, "state_5.sqlite-wal"));
            CopyFileIfExists(Path.Combine(src, "state_5.sqlite-shm"), Path.Combine(tmp, "state_5.sqlite-shm"));

            File.WriteAllText(Path.Combine(tmp, "README.txt"),
                "This export contains Codex history in native format.\r\n" +
                "Copy the contents into the target machine's %USERPROFILE%\\.codex directory.\r\n" +
                "Close Codex before importing.\r\n");

            if (File.Exists(outZip)) File.Delete(outZip);
            ZipFile.CreateFromDirectory(tmp, outZip, CompressionLevel.Optimal, false);
            Directory.Delete(tmp, true);

            log.AppendText("Export complete: " + outZip + "\r\n");
            MessageBox.Show("Export complete", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            log.AppendText(ex + "\r\n");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RunImport()
    {
        try
        {
            var zip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "codex-history-export.zip");
            if (!File.Exists(zip))
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Title = "Select codex-history-export.zip";
                    dlg.Filter = "Zip files (*.zip)|*.zip|All files (*.*)|*.*";
                    dlg.Multiselect = false;
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;
                    zip = dlg.FileName;
                }
            }

            var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
            if (!Directory.Exists(dest))
            {
                MessageBox.Show("Codex data folder not found at " + dest, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var tmp = Path.Combine(Path.GetTempPath(), "codex-import-" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tmp);
            ZipFile.ExtractToDirectory(zip, tmp);

            CopyDir(Path.Combine(tmp, "sessions"), Path.Combine(dest, "sessions"));
            var archived = Path.Combine(tmp, "archived_sessions");
            if (Directory.Exists(archived))
                CopyDir(archived, Path.Combine(dest, "archived_sessions"));

            CopyFileIfExists(Path.Combine(tmp, "session_index.jsonl"), Path.Combine(dest, "session_index.jsonl"));
            CopyFileIfExists(Path.Combine(tmp, "state_5.sqlite"), Path.Combine(dest, "state_5.sqlite"));
            CopyFileIfExists(Path.Combine(tmp, "state_5.sqlite-wal"), Path.Combine(dest, "state_5.sqlite-wal"));
            CopyFileIfExists(Path.Combine(tmp, "state_5.sqlite-shm"), Path.Combine(dest, "state_5.sqlite-shm"));

            Directory.Delete(tmp, true);

            log.AppendText("Import complete. Restart Codex.\r\n");
            MessageBox.Show("Import complete", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            log.AppendText(ex + "\r\n");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void CopyDir(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir)) return;
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDir(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }

    private static void CopyFileIfExists(string src, string dest)
    {
        if (File.Exists(src)) File.Copy(src, dest, true);
    }
}
