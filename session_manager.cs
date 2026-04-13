using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
    private TextBox txtRoot;
    private Button btnBrowse;
    private Button btnRefresh;
    private Button btnExport;
    private Button btnImport;
    private Button btnOpenRoot;
    private ListView list;

    public MainForm()
    {
        Text = "Codex Session Manager";
        Width = 980;
        Height = 620;
        StartPosition = FormStartPosition.CenterScreen;

        var lblRoot = new Label { Left = 12, Top = 14, Text = "Codex Root:", AutoSize = true };
        txtRoot = new TextBox { Left = 90, Top = 10, Width = 720 };
        btnBrowse = new Button { Left = 820, Top = 8, Width = 70, Text = "Browse" };
        btnOpenRoot = new Button { Left = 900, Top = 8, Width = 60, Text = "Open" };

        btnRefresh = new Button { Left = 12, Top = 42, Width = 80, Text = "Refresh" };
        btnExport = new Button { Left = 100, Top = 42, Width = 140, Text = "Export Selected" };
        btnImport = new Button { Left = 250, Top = 42, Width = 140, Text = "Import Zip" };

        list = new ListView { Left = 12, Top = 78, Width = 948, Height = 480, View = View.Details, FullRowSelect = true, GridLines = true };
        list.Columns.Add("Type", 80);
        list.Columns.Add("Last Write", 150);
        list.Columns.Add("File", 300);
        list.Columns.Add("Path", 400);

        Controls.Add(lblRoot);
        Controls.Add(txtRoot);
        Controls.Add(btnBrowse);
        Controls.Add(btnOpenRoot);
        Controls.Add(btnRefresh);
        Controls.Add(btnExport);
        Controls.Add(btnImport);
        Controls.Add(list);

        btnBrowse.Click += (s, e) => BrowseRoot();
        btnOpenRoot.Click += (s, e) => OpenRoot();
        btnRefresh.Click += (s, e) => LoadSessions();
        btnExport.Click += (s, e) => ExportSelected();
        btnImport.Click += (s, e) => ImportZip();

        txtRoot.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        LoadSessions();
    }

    private void BrowseRoot()
    {
        using (var dlg = new FolderBrowserDialog())
        {
            dlg.SelectedPath = txtRoot.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtRoot.Text = dlg.SelectedPath;
                LoadSessions();
            }
        }
    }

    private void OpenRoot()
    {
        var root = txtRoot.Text;
        if (Directory.Exists(root))
        {
            Process.Start(new ProcessStartInfo { FileName = root, UseShellExecute = true });
        }
        else
        {
            MessageBox.Show("Codex root not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadSessions()
    {
        list.Items.Clear();
        var root = txtRoot.Text;
        var sessions = Path.Combine(root, "sessions");
        var archived = Path.Combine(root, "archived_sessions");

        if (!Directory.Exists(root))
        {
            MessageBox.Show("Codex root not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        AddFiles("session", sessions);
        AddFiles("archived", archived);
    }

    private void AddFiles(string type, string dir)
    {
        if (!Directory.Exists(dir)) return;
        var files = Directory.EnumerateFiles(dir, "*.jsonl", SearchOption.AllDirectories)
            .Select(p => new FileInfo(p))
            .OrderByDescending(f => f.LastWriteTimeUtc);

        foreach (var f in files)
        {
            var item = new ListViewItem(type);
            item.SubItems.Add(f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
            item.SubItems.Add(f.Name);
            item.SubItems.Add(f.FullName);
            list.Items.Add(item);
        }
    }

    private void ExportSelected()
    {
        if (list.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select at least one session file.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using (var dlg = new SaveFileDialog())
        {
            dlg.Filter = "Zip (*.zip)|*.zip";
            dlg.FileName = "codex-sessions-export.zip";
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var root = txtRoot.Text;
            if (File.Exists(dlg.FileName)) File.Delete(dlg.FileName);

            using (var zip = ZipFile.Open(dlg.FileName, ZipArchiveMode.Create))
            {
                foreach (ListViewItem item in list.SelectedItems)
                {
                    var fullPath = item.SubItems[3].Text;
                    if (!File.Exists(fullPath)) continue;
                    var rel = MakeRelative(root, fullPath);
                    zip.CreateEntryFromFile(fullPath, rel, CompressionLevel.Optimal);
                }
            }

            MessageBox.Show("Export complete.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ImportZip()
    {
        using (var dlg = new OpenFileDialog())
        {
            dlg.Filter = "Zip (*.zip)|*.zip";
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var root = txtRoot.Text;
            if (!Directory.Exists(root))
            {
                MessageBox.Show("Codex root not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ZipFile.ExtractToDirectory(dlg.FileName, root);
            MessageBox.Show("Import complete. Restart Codex.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadSessions();
        }
    }

    private string MakeRelative(string root, string path)
    {
        if (!root.EndsWith(Path.DirectorySeparatorChar.ToString())) root += Path.DirectorySeparatorChar;
        var rootUri = new Uri(root);
        var fileUri = new Uri(path);
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar));
    }
}
