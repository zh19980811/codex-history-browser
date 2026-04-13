using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        InitLog();
        Application.ThreadException += (s, e) => LogCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => LogCrash(e.ExceptionObject as Exception);
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    static void LogCrash(Exception ex)
    {
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "codex-history-browser");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "crash.log");
            File.AppendAllText(path, DateTime.Now.ToString("s") + "\n" + (ex != null ? ex.ToString() : "<null>") + "\n\n", Encoding.UTF8);
            MessageBox.Show("App crashed. Log: " + path);
        }
        catch { }
    }

    static void InitLog()
    {
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "codex-history-browser");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "run.log");
            File.AppendAllText(path, DateTime.Now.ToString("s") + " app start\n", Encoding.UTF8);
        }
        catch { }
    }
}

public class MainForm : Form
{
    private TextBox txtRoot;
    private Button btnBrowse;
    private Button btnOpenRoot;
    private Button btnDetect;
    private ComboBox cmbRoots;
    private Button btnRefresh;
    private TextBox txtSearch;
    private Button btnClearSearch;
    private ComboBox cmbMoveCwd;
    private Button btnMoveToSelected;
    private ComboBox cmbLang;
    private MenuStrip menu;
    private Panel row1Panel;
    private Panel row2Panel;
    private TableLayoutPanel layout;
    private Panel bottomPanel;
    private ToolStripMenuItem menuFile;
    private ToolStripMenuItem menuEdit;
    private ToolStripMenuItem menuView;
    private ToolStripMenuItem miExport;
    private ToolStripMenuItem miImport;
    private ToolStripMenuItem miExit;
    private ToolStripMenuItem miRename;
    private ToolStripMenuItem miMoveChecked;
    private ToolStripMenuItem miMoveToSelected;
    private ToolStripMenuItem miClearChecks;
    private ToolStripMenuItem miHideMeta;
    private SplitContainer split;
    private TreeView tree;
    private RichTextBox preview;
    private Label lblChatTitle;
    private Label lblStatus;
    private CheckBox chkHideMeta;

    private readonly JavaScriptSerializer _json = new JavaScriptSerializer();
    private readonly Dictionary<string, string> threadNames = new Dictionary<string, string>();
    private readonly Dictionary<string, long> threadUpdated = new Dictionary<string, long>();
    private readonly Dictionary<string, string> threadCwds = new Dictionary<string, string>();
    private readonly Dictionary<string, SessionItem> sessionByPath = new Dictionary<string, SessionItem>();
    private List<SessionItem> allItems = new List<SessionItem>();
    private string currentLang = "zh";
    private bool _handlingCheck = false;

    public MainForm()
    {
        Text = "Codex History Browser";
        Width = 1200;
        Height = 720;
        MinimumSize = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        BackColor = Color.FromArgb(240, 242, 245);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildMenu();
        BuildMainArea();
        BuildTopControls();

        txtRoot.Text = FindNearestCodexRoot();
        cmbLang.Items.Add("中文");
        cmbLang.Items.Add("English");
        cmbLang.SelectedIndex = 0;

        ApplyLanguage();
        DetectRoots();
        LoadSessions();
    }

    private void BuildMenu()
    {
        menu = new MenuStrip { Dock = DockStyle.Top };
        menuFile = new ToolStripMenuItem("File");
        menuEdit = new ToolStripMenuItem("Edit");
        menuView = new ToolStripMenuItem("View");
        miExport = new ToolStripMenuItem("Export Sessions");
        miImport = new ToolStripMenuItem("Import Sessions");
        miExit = new ToolStripMenuItem("Exit");
        miRename = new ToolStripMenuItem("Rename Checked");
        miMoveChecked = new ToolStripMenuItem("Move Checked");
        miMoveToSelected = new ToolStripMenuItem("Move To Selected");
        miClearChecks = new ToolStripMenuItem("Clear Checks");
        miHideMeta = new ToolStripMenuItem("Hide Meta Blocks") { Checked = true, CheckOnClick = true };
        var miViewLog = new ToolStripMenuItem("View Log");
        miViewLog.Click += (s, e) => OpenLog();

        miExport.Click += (s, e) => ExportChecked();
        miImport.Click += (s, e) => ImportPackage();
        miExit.Click += (s, e) => Close();
        miRename.Click += (s, e) => RenameChecked();
        miMoveChecked.Click += (s, e) => MoveChecked();
        miMoveToSelected.Click += (s, e) => MoveToSelectedWorkspace();
        miClearChecks.Click += (s, e) => ClearChecks();
        miHideMeta.CheckedChanged += (s, e) => { chkHideMeta.Checked = miHideMeta.Checked; LoadPreview(); };

        menuFile.DropDownItems.Add(miExport);
        menuFile.DropDownItems.Add(miImport);
        menuFile.DropDownItems.Add(new ToolStripSeparator());
        menuFile.DropDownItems.Add(miExit);

        menuEdit.DropDownItems.Add(miRename);
        menuEdit.DropDownItems.Add(miMoveChecked);
        menuEdit.DropDownItems.Add(miMoveToSelected);
        menuEdit.DropDownItems.Add(new ToolStripSeparator());
        menuEdit.DropDownItems.Add(miClearChecks);

        menuView.DropDownItems.Add(miHideMeta);
        menuView.DropDownItems.Add(miViewLog);

        menu.Items.Add(menuFile);
        menu.Items.Add(menuEdit);
        menu.Items.Add(menuView);
        MainMenuStrip = menu;
    }

    private void BuildTopControls()
    {
        txtRoot = new TextBox { Height = 24 };
        btnBrowse = new Button { Width = 80, Height = 26 };
        btnOpenRoot = new Button { Width = 70, Height = 26 };
        btnDetect = new Button { Width = 80, Height = 26 };
        cmbRoots = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Height = 24 };

        btnRefresh = new Button { Width = 90, Height = 26 };
        txtSearch = new TextBox { Height = 24 };
        btnClearSearch = new Button { Width = 70, Height = 26 };
        cmbMoveCwd = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Height = 24 };
        btnMoveToSelected = new Button { Width = 140, Height = 26 };
        cmbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Height = 24 };

        row1Panel.Controls.AddRange(new Control[] {
            txtRoot, btnBrowse, btnOpenRoot, btnDetect, cmbRoots
        });
        row2Panel.Controls.AddRange(new Control[] {
            btnRefresh, txtSearch, btnClearSearch, cmbMoveCwd, btnMoveToSelected, cmbLang
        });

        btnBrowse.Click += (s, e) => BrowseRoot();
        btnOpenRoot.Click += (s, e) => OpenRoot();
        btnDetect.Click += (s, e) => DetectRoots();
        cmbRoots.SelectedIndexChanged += (s, e) => SelectRootFromCombo();
        btnRefresh.Click += (s, e) => LoadSessions();
        txtSearch.TextChanged += (s, e) => ApplyFilter();
        btnClearSearch.Click += (s, e) => { txtSearch.Text = ""; };
        btnMoveToSelected.Click += (s, e) => MoveToSelectedWorkspace();
        cmbLang.SelectedIndexChanged += (s, e) => ApplyLanguage();

        StyleButton(btnBrowse);
        StyleButton(btnOpenRoot);
        StyleButton(btnDetect);
        StyleButton(btnRefresh);
        StyleButton(btnClearSearch);
        StyleButton(btnMoveToSelected);
        StyleInput(txtRoot);
        StyleInput(txtSearch);
        StyleInput(cmbRoots);
        StyleInput(cmbMoveCwd);

        LayoutTopControls();
    }
    private void BuildMainArea()
    {
        split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 520, IsSplitterFixed = false };
        split.BackColor = Color.FromArgb(240, 242, 245);

        tree = new TreeView { Dock = DockStyle.Fill, CheckBoxes = true };
        tree.ShowRootLines = false;
        tree.ShowLines = false;
        tree.ShowPlusMinus = true;
        tree.BorderStyle = BorderStyle.None;
        tree.Indent = 18;
        tree.HideSelection = false;

        lblChatTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        preview = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, WordWrap = true, BorderStyle = BorderStyle.None };
        chkHideMeta = new CheckBox { Height = 22, Checked = true };
        lblStatus = new Label { Height = 20 };
        bottomPanel = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };
        bottomPanel.Controls.Add(chkHideMeta);
        bottomPanel.Controls.Add(lblStatus);

        row1Panel = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };
        row2Panel = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };

        split.Panel1.Controls.Add(tree);
        split.Panel2.Controls.Add(preview);
        split.Panel2.Controls.Add(lblChatTitle);

        layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.Controls.Add(menu, 0, 0);
        layout.Controls.Add(row1Panel, 0, 1);
        layout.Controls.Add(row2Panel, 0, 2);
        layout.Controls.Add(split, 0, 3);
        layout.Controls.Add(bottomPanel, 0, 4);
        Controls.Add(layout);

        StyleTree(tree);
        StylePreview(preview);

        tree.AfterSelect += (s, e) => LoadPreview();
        tree.AfterCheck += (s, e) => HandleCheck(e.Node);
        chkHideMeta.CheckedChanged += (s, e) => LoadPreview();

        Resize += (s, e) => LayoutTopControls();
        row1Panel.Resize += (s, e) => LayoutTopControls();
        row2Panel.Resize += (s, e) => LayoutTopControls();
        bottomPanel.Resize += (s, e) => LayoutTopControls();
    }

    private void LayoutTopControls()
    {
        if (row1Panel == null || row2Panel == null || bottomPanel == null || txtRoot == null || btnBrowse == null || btnOpenRoot == null || btnDetect == null || cmbRoots == null || btnRefresh == null || txtSearch == null || btnClearSearch == null || cmbMoveCwd == null || btnMoveToSelected == null || cmbLang == null || chkHideMeta == null || lblStatus == null)
        {
            return;
        }

        int width = row1Panel.ClientSize.Width;
        if (width <= 0) return;
        int row1Y = 4;
        int row2Y = 4;

        int left = 12;
        int right = width - 12;
        int gap = 8;

        int wBrowse = btnBrowse.Width;
        int wOpen = btnOpenRoot.Width;
        int wDetect = btnDetect.Width;
        int wRoots = 240;

        cmbRoots.SetBounds(right - wRoots, row1Y, wRoots, 24);
        btnDetect.SetBounds(cmbRoots.Left - gap - wDetect, row1Y - 1, wDetect, 26);
        btnOpenRoot.SetBounds(btnDetect.Left - gap - wOpen, row1Y - 1, wOpen, 26);
        btnBrowse.SetBounds(btnOpenRoot.Left - gap - wBrowse, row1Y - 1, wBrowse, 26);

        int txtLeft = left;
        int txtRight = btnBrowse.Left - gap;
        if (txtRight < txtLeft + 120) txtRight = txtLeft + 120;
        txtRoot.SetBounds(txtLeft, row1Y, txtRight - txtLeft, 24);

        int wLang = 120;
        cmbLang.SetBounds(right - wLang, row2Y, wLang, 24);

        int wMoveBtn = btnMoveToSelected.Width;
        btnMoveToSelected.SetBounds(cmbLang.Left - gap - wMoveBtn, row2Y - 1, wMoveBtn, 26);

        int wMoveCwd = 380;
        int moveRight = btnMoveToSelected.Left - gap;
        int moveLeft = moveRight - wMoveCwd;
        if (moveLeft < left + 420) moveLeft = left + 420;
        cmbMoveCwd.SetBounds(moveLeft, row2Y, moveRight - moveLeft, 24);

        btnClearSearch.SetBounds(cmbMoveCwd.Left - gap - btnClearSearch.Width, row2Y - 1, btnClearSearch.Width, 26);
        int searchRight = btnClearSearch.Left - gap;
        int searchLeft = left + btnRefresh.Width + gap;
        if (searchRight < searchLeft + 120) searchRight = searchLeft + 120;
        txtSearch.SetBounds(searchLeft, row2Y, searchRight - searchLeft, 24);
        btnRefresh.SetBounds(left, row2Y - 1, btnRefresh.Width, 26);

        chkHideMeta.SetBounds(12, 4, 200, 20);
        lblStatus.SetBounds(220, 4, bottomPanel.ClientSize.Width - 232, 20);
    }

    private void ApplyLanguage()
    {
        bool zh = cmbLang.SelectedIndex == 0;
        currentLang = zh ? "zh" : "en";

        Text = zh ? "Codex 会话浏览器" : "Codex History Browser";
        btnBrowse.Text = zh ? "浏览" : "Browse";
        btnOpenRoot.Text = zh ? "打开" : "Open";
        btnDetect.Text = zh ? "检测" : "Detect";
        btnRefresh.Text = zh ? "刷新" : "Refresh";
        btnClearSearch.Text = zh ? "清除" : "Clear";
        btnMoveToSelected.Text = zh ? "移动到所选" : "Move To Selected";
        chkHideMeta.Text = zh ? "隐藏元数据" : "Hide meta blocks";

        menuFile.Text = zh ? "文件" : "File";
        menuEdit.Text = zh ? "编辑" : "Edit";
        menuView.Text = zh ? "视图" : "View";
        miExport.Text = zh ? "导出会话" : "Export Sessions";
        miImport.Text = zh ? "导入会话" : "Import Sessions";
        miExit.Text = zh ? "退出" : "Exit";
        miRename.Text = zh ? "批量改名" : "Rename Checked";
        miMoveChecked.Text = zh ? "移动勾选" : "Move Checked";
        miMoveToSelected.Text = zh ? "移动到所选" : "Move To Selected";
        miClearChecks.Text = zh ? "清除勾选" : "Clear Checks";
        miHideMeta.Text = zh ? "隐藏元数据" : "Hide Meta Blocks";
        foreach (ToolStripMenuItem it in menuView.DropDownItems)
        {
            if (it.Text == "View Log" || it.Text == "查看日志") it.Text = zh ? "查看日志" : "View Log";
        }

        LayoutTopControls();
        ApplyFilter();
        LoadPreview();
    }
    private void OpenLog()
    {
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "codex-history-browser");
            string path = Path.Combine(dir, "crash.log");
            if (!File.Exists(path)) path = Path.Combine(dir, "run.log");
            if (File.Exists(path)) Process.Start("notepad.exe", path);
        }
        catch { }
    }

    private void LoadSessions()
    {
        threadNames.Clear();
        threadUpdated.Clear();
        threadCwds.Clear();
        sessionByPath.Clear();
        allItems.Clear();
        tree.Nodes.Clear();

        string root = txtRoot.Text.Trim();
        if (!Directory.Exists(root))
        {
            SetStatus(L("找不到 Codex 根目录。", "Codex root not found."));
            return;
        }

        string indexPath = Path.Combine(root, "session_index.jsonl");
        LoadThreadIndex(indexPath);

        string sessionsDir = Path.Combine(root, "sessions");
        if (!Directory.Exists(sessionsDir))
        {
            SetStatus(L("找不到 sessions 目录。", "Sessions directory not found."));
            return;
        }

        int fileCount = 0;
        foreach (var file in Directory.GetFiles(sessionsDir, "*.jsonl", SearchOption.AllDirectories))
        {
            fileCount++;
            var item = ReadSessionMeta(file, root);
            if (item == null) continue;
            allItems.Add(item);
            sessionByPath[file] = item;
        }

        ApplyFilter();
        SetStatus(L(string.Format("加载 {0} 条会话 / 共 {1} 文件", allItems.Count, fileCount),
                    string.Format("Loaded {0} sessions / {1} files", allItems.Count, fileCount)));
    }

    private void ApplyFilter()
    {
        string q = txtSearch.Text.Trim();
        IEnumerable<SessionItem> items = allItems;
        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(i =>
                (i.Title ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (i.Cwd ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        tree.BeginUpdate();
        tree.Nodes.Clear();

        var groups = items.GroupBy(i => string.IsNullOrWhiteSpace(i.Cwd) ? "(unknown)" : i.Cwd)
            .OrderBy(g => g.Key);

        foreach (var g in groups)
        {
            var groupNode = new TreeNode(string.Format("{0} ({1})", g.Key, g.Count()));
            foreach (var item in g.OrderByDescending(i => i.UpdatedAt))
            {
                var node = new TreeNode(string.Format("{0} ({1})", item.Title, FormatRelative(item.UpdatedAt)));
                node.Tag = item;
                groupNode.Nodes.Add(node);
            }
            groupNode.Collapse();
            tree.Nodes.Add(groupNode);
        }
        tree.EndUpdate();
    }

    private void LoadPreview()
    {
        var node = tree.SelectedNode;
        if (node == null || node.Tag == null)
        {
            preview.Clear();
            lblChatTitle.Text = L("请选择会话", "Select a session");
            return;
        }

        var item = (SessionItem)node.Tag;
        lblChatTitle.Text = item.Title;
        preview.Clear();
        if (!File.Exists(item.Path)) return;

        var lines = ReadAllLinesShared(item.Path);
        if (lines == null) return;
        foreach (var line in lines)
        {
            var dict = ParseJson(line);
            if (dict == null) continue;
            if (chkHideMeta.Checked && IsMeta(dict)) continue;

            string role = GetString(dict, "role") ?? "";
            string content = GetString(dict, "content") ?? "";
            if (string.IsNullOrWhiteSpace(content)) continue;

            Color color = role == "assistant" ? Color.FromArgb(52, 104, 192)
                : role == "system" ? Color.FromArgb(120, 120, 120)
                : Color.FromArgb(30, 30, 30);

            AppendLine(preview, string.Format("{0}: {1}", role, content), color);
        }
    }

    private void HandleCheck(TreeNode node)
    {
        if (_handlingCheck) return;
        try
        {
            _handlingCheck = true;
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = node.Checked;
            }
            if (node.Parent != null && !node.Checked)
            {
                node.Parent.Checked = false;
            }
        }
        finally
        {
            _handlingCheck = false;
        }
    }
    private void RenameChecked()
    {
        var items = GetCheckedItems();
        if (items.Count == 0)
        {
MessageBox.Show(L("请先勾选会话。", "Please check sessions first."));
            return;
        }

string prompt = L("请输入新标题（可用 {n} 做编号）：", "Enter new title (use {n} for numbering):");
        string input = Microsoft.VisualBasic.Interaction.InputBox(prompt, L("批量改名", "Batch Rename"), "");
        if (string.IsNullOrWhiteSpace(input)) return;

        var updates = new Dictionary<string, string>();
        for (int i = 0; i < items.Count; i++)
        {
            string title = input.Contains("{n}") ? input.Replace("{n}", (i + 1).ToString())
                : items.Count == 1 ? input : string.Format("{0} {1}", input, i + 1);
            updates[items[i].Id] = title;
            items[i].Title = title;
        }

        UpdateSessionIndexTitles(txtRoot.Text.Trim(), updates);
        UpdateSqliteTitles(txtRoot.Text.Trim(), updates);
        ApplyFilter();
SetStatus(L("标题已更新。", "Titles updated."));
    }

    private void MoveChecked()
    {
        var items = GetCheckedItems();
        if (items.Count == 0)
        {
MessageBox.Show(L("请先勾选会话。", "Please check sessions first."));
            return;
        }

        string target = cmbMoveCwd.Text.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
MessageBox.Show(L("请输入目标工作区路径。", "Please input target workspace path."));
            return;
        }

        var updates = new Dictionary<string, string>();
        foreach (var item in items)
        {
            if (UpdateSessionCwd(item.Path, target))
            {
                item.Cwd = target;
                updates[item.Id] = target;
            }
        }

        UpdateSessionIndexCwds(txtRoot.Text.Trim(), updates);
        UpdateSqliteCwds(txtRoot.Text.Trim(), updates);
        ApplyFilter();
SetStatus(L("已移动到目标工作区。", "Moved to target workspace."));
    }

    private void MoveToSelectedWorkspace()
    {
        var node = tree.SelectedNode;
        if (node == null || node.Tag == null)
        {
MessageBox.Show(L("请先选择一个会话。", "Please select a session."));
            return;
        }
        string target = cmbMoveCwd.Text.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
MessageBox.Show(L("请输入目标工作区路径。", "Please input target workspace path."));
            return;
        }

        var item = (SessionItem)node.Tag;
        if (UpdateSessionCwd(item.Path, target))
        {
            item.Cwd = target;
            UpdateSessionIndexCwds(txtRoot.Text.Trim(), new Dictionary<string, string> { { item.Id, target } });
            UpdateSqliteCwds(txtRoot.Text.Trim(), new Dictionary<string, string> { { item.Id, target } });
        }
        ApplyFilter();
SetStatus(L("已移动到所选工作区。", "Moved to selected workspace."));
    }

    private void ClearChecks()
    {
        foreach (TreeNode root in tree.Nodes)
        {
            ClearChecksRecursive(root);
        }
    }

    private void ClearChecksRecursive(TreeNode node)
    {
        node.Checked = false;
        foreach (TreeNode child in node.Nodes) ClearChecksRecursive(child);
    }

    private void ExportChecked()
    {
        var items = GetCheckedItems();
        if (items.Count == 0)
        {
MessageBox.Show(L("请先勾选会话。", "Please check sessions first."));
            return;
        }

        using (var sfd = new SaveFileDialog())
        {
            sfd.Filter = "Codex Package (*.codexpkg)|*.codexpkg";
            sfd.FileName = "codex-sessions.codexpkg";
            if (sfd.ShowDialog() != DialogResult.OK) return;

            string tmp = Path.Combine(Path.GetTempPath(), "codex-export-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            string sessionsDir = Path.Combine(tmp, "sessions");
            Directory.CreateDirectory(sessionsDir);

            var manifest = new Manifest { version = 1, items = new List<ManifestItem>() };
            foreach (var item in items)
            {
                string dest = Path.Combine(sessionsDir, Path.GetFileName(item.Path));
                File.Copy(item.Path, dest, true);
                manifest.items.Add(new ManifestItem
                {
                    id = item.Id,
                    title = item.Title,
                    cwd = item.Cwd,
                    updated_at = item.UpdatedAt,
                    file = Path.GetFileName(item.Path)
                });
            }

            string manifestPath = Path.Combine(tmp, "manifest.json");
            File.WriteAllText(manifestPath, _json.Serialize(manifest), new UTF8Encoding(false));

            if (File.Exists(sfd.FileName)) File.Delete(sfd.FileName);
            ZipFile.CreateFromDirectory(tmp, sfd.FileName);
            Directory.Delete(tmp, true);

MessageBox.Show(L("导出完成。", "Export completed."));
        }
    }

    private void ImportPackage()
    {
        using (var ofd = new OpenFileDialog())
        {
            ofd.Filter = "Codex Package (*.codexpkg)|*.codexpkg";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            string tmp = Path.Combine(Path.GetTempPath(), "codex-import-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            ZipFile.ExtractToDirectory(ofd.FileName, tmp);

            string manifestPath = Path.Combine(tmp, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                MessageBox.Show(L("Invalid manifest.", "Invalid manifest."));
                Directory.Delete(tmp, true);
                return;
            }

            var manifest = ReadManifest(manifestPath);
            if (manifest == null || manifest.items.Count == 0)
            {
                MessageBox.Show(L("Invalid manifest.", "Invalid manifest."));
                Directory.Delete(tmp, true);
                return;
            }

            var targets = allItems.Select(i => i.Cwd).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            var form = new ImportForm(manifest.items, targets, currentLang);
            if (form.ShowDialog() != DialogResult.OK)
            {
                Directory.Delete(tmp, true);
                return;
            }

            string root = txtRoot.Text.Trim();
            string sessionsDir = Path.Combine(root, "sessions");
            Directory.CreateDirectory(sessionsDir);

            var titleUpdates = new Dictionary<string, string>();
            var cwdUpdates = new Dictionary<string, string>();
            var finalIds = new Dictionary<string, string>();

            foreach (var item in form.SelectedItems)
            {
                string srcFile = Path.Combine(tmp, "sessions", item.file);
                if (!File.Exists(srcFile)) continue;

                string newId = item.id;
                string destFile = Path.Combine(sessionsDir, Path.GetFileName(srcFile));
                if (File.Exists(destFile))
                {
                    newId = Guid.NewGuid().ToString("N");
                    destFile = Path.Combine(sessionsDir, newId + ".jsonl");
                }

                File.Copy(srcFile, destFile, true);
                UpdateSessionCwd(destFile, item.target_cwd);
                UpdateSessionIds(destFile, item.id, newId);

                titleUpdates[newId] = item.title;
                cwdUpdates[newId] = item.target_cwd;
                finalIds[item.id] = newId;
            }

            UpdateSessionIndexTitles(root, titleUpdates);
            UpdateSessionIndexCwds(root, cwdUpdates);
            UpdateSqliteTitles(root, titleUpdates);
            UpdateSqliteCwds(root, cwdUpdates);

            Directory.Delete(tmp, true);
            LoadSessions();
MessageBox.Show(L("导入完成。", "Import completed."));
        }
    }

    private List<SessionItem> GetCheckedItems()
    {
        var items = new List<SessionItem>();
        foreach (TreeNode group in tree.Nodes)
        {
            foreach (TreeNode node in group.Nodes)
            {
                var item = node.Tag as SessionItem;
                if (node.Checked && item != null)
                {
                    items.Add(item);
                }
            }
        }
        return items;
    }
    private SessionItem ReadSessionMeta(string file, string root)
    {
        string id = Path.GetFileNameWithoutExtension(file);
        string cwd = null;
        long updated = 0;

        var lines = ReadAllLinesShared(file);
        if (lines == null) return null;
        foreach (var line in lines)
        {
            var dict = ParseJson(line);
            if (dict == null) continue;
            var payload = GetPayload(dict);
            string type = GetString(dict, "type");
            string threadId = GetString(payload, "id") ?? GetString(payload, "thread_id") ?? GetString(dict, "thread_id") ?? GetString(dict, "id");
            if (!string.IsNullOrWhiteSpace(threadId)) id = threadId;
            string maybeCwd = GetString(payload, "cwd") ?? GetString(dict, "cwd");
            if (!string.IsNullOrWhiteSpace(maybeCwd)) cwd = maybeCwd;
            long maybeUpdated = GetEpoch(payload, "updated_at");
            if (maybeUpdated == 0) maybeUpdated = GetEpoch(dict, "updated_at");
            if (maybeUpdated == 0) maybeUpdated = GetEpoch(payload, "timestamp");
            if (maybeUpdated == 0) maybeUpdated = GetEpoch(dict, "timestamp");
            if (maybeUpdated > 0) updated = maybeUpdated;
            if (type == "session_meta") break;
        }

        string title = threadNames.ContainsKey(id) ? threadNames[id] : GetTitleFromSessionFile(file);
        if (string.IsNullOrWhiteSpace(title)) title = L("(无标题)", "(no title)");

        if (threadUpdated.ContainsKey(id)) updated = threadUpdated[id];
        if (updated == 0) updated = new DateTimeOffset(File.GetLastWriteTimeUtc(file)).ToUnixTimeSeconds();

        if (string.IsNullOrWhiteSpace(cwd) && threadCwds.ContainsKey(id)) cwd = threadCwds[id];

        return new SessionItem
        {
            Id = id,
            Title = title,
            Cwd = cwd,
            UpdatedAt = updated,
            Path = file,
            Root = root
        };
    }

    private string GetTitleFromSessionFile(string file)
    {
        var lines = ReadAllLinesShared(file);
        if (lines == null) return null;
        foreach (var line in lines)
        {
            var dict = ParseJson(line);
            if (dict == null) continue;
            var payload = GetPayload(dict);
            string role = GetString(payload, "role") ?? GetString(dict, "role");
            string content = GetString(payload, "content") ?? GetString(dict, "content");
            if (role == "user" && !string.IsNullOrWhiteSpace(content))
            {
                content = content.Replace("\r", " ").Replace("\n", " ").Trim();
                return content.Length > 50 ? content.Substring(0, 50) + "..." : content;
            }
        }
        return null;
    }

    private void LoadThreadIndex(string indexPath)
    {
        if (!File.Exists(indexPath)) return;
        var lines = ReadAllLinesShared(indexPath);
        if (lines == null) return;
        foreach (var line in lines)
        {
            var dict = ParseJson(line);
            if (dict == null) continue;
            string id = GetString(dict, "thread_id") ?? GetString(dict, "id");
            if (string.IsNullOrWhiteSpace(id)) continue;
            string title = GetString(dict, "thread_name") ?? GetString(dict, "title");
            if (!string.IsNullOrWhiteSpace(title)) threadNames[id] = title;
            long updated = GetEpoch(dict, "updated_at");
            if (updated > 0) threadUpdated[id] = updated;
            string cwd = GetString(dict, "cwd");
            if (!string.IsNullOrWhiteSpace(cwd)) threadCwds[id] = cwd;
        }
    }

    private void BrowseRoot()
    {
        using (var dlg = new FolderBrowserDialog())
        {
            dlg.SelectedPath = txtRoot.Text.Trim();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtRoot.Text = dlg.SelectedPath;
                LoadSessions();
            }
        }
    }

    private void OpenRoot()
    {
        string root = txtRoot.Text.Trim();
        if (Directory.Exists(root))
        {
            Process.Start("explorer.exe", root);
        }
    }

    private void DetectRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        roots.Add(Path.Combine(user, ".codex"));
        string envHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (!string.IsNullOrWhiteSpace(envHome)) roots.Add(envHome);
        roots.Add(txtRoot.Text.Trim());

        foreach (var dir in new[] { user, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) })
        {
            try
            {
                foreach (var d in Directory.GetDirectories(dir, ".codex", SearchOption.AllDirectories))
                {
                    roots.Add(d);
                }
            }
            catch { }
        }

        cmbRoots.Items.Clear();
        foreach (var r in roots.Where(Directory.Exists)) cmbRoots.Items.Add(r);
        if (cmbRoots.Items.Count > 0) cmbRoots.SelectedIndex = 0;
    }

    private string FindNearestCodexRoot()
    {
        string envHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        DirectoryInfo dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 6 && dir != null; i++)
        {
            string cand = Path.Combine(dir.FullName, ".codex");
            if (Directory.Exists(cand)) return cand;
            dir = dir.Parent;
        }
        if (!string.IsNullOrWhiteSpace(envHome) && Directory.Exists(envHome)) return envHome;
        string user = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        if (Directory.Exists(user)) return user;
        return !string.IsNullOrWhiteSpace(envHome) ? envHome : user;
    }

    private void SelectRootFromCombo()
    {
        if (cmbRoots.SelectedItem == null) return;
        txtRoot.Text = cmbRoots.SelectedItem.ToString();
        LoadSessions();
    }

    private void SetStatus(string text)
    {
        lblStatus.Text = text;
    }

    private string L(string zh, string en)
    {
        return currentLang == "zh" ? zh : en;
    }

    private string FormatRelative(long unix)
    {
        DateTimeOffset dt = DateTimeOffset.FromUnixTimeSeconds(unix);
        var span = DateTimeOffset.Now - dt;
        if (span.TotalMinutes < 60) return currentLang == "zh" ? string.Format("{0} 分钟", (int)span.TotalMinutes) : string.Format("{0}m", (int)span.TotalMinutes);
        if (span.TotalHours < 24) return currentLang == "zh" ? string.Format("{0} 小时", (int)span.TotalHours) : string.Format("{0}h", (int)span.TotalHours);
        if (span.TotalDays < 7) return currentLang == "zh" ? string.Format("{0} 天", (int)span.TotalDays) : string.Format("{0}d", (int)span.TotalDays);
        if (span.TotalDays < 30) return currentLang == "zh" ? string.Format("{0} 周", (int)(span.TotalDays / 7)) : string.Format("{0}w", (int)(span.TotalDays / 7));
        if (span.TotalDays < 365) return currentLang == "zh" ? string.Format("{0} 月", (int)(span.TotalDays / 30)) : string.Format("{0}mo", (int)(span.TotalDays / 30));
        return currentLang == "zh" ? string.Format("{0} 年", (int)(span.TotalDays / 365)) : string.Format("{0}y", (int)(span.TotalDays / 365));
    }

    private void StyleButton(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.FromArgb(210, 214, 220);
        b.BackColor = Color.White;
    }

    private void StyleInput(Control c)
    {
        c.BackColor = Color.White;
    }

    private void StyleTree(TreeView t)
    {
        t.BackColor = Color.White;
    }

    private void StylePreview(RichTextBox r)
    {
        r.BackColor = Color.White;
    }

    private Dictionary<string, object> ParseJson(string line)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            return _json.Deserialize<Dictionary<string, object>>(line);
        }
        catch
        {
            return null;
        }
    }

    private bool IsMeta(Dictionary<string, object> dict)
    {
        string t = GetString(dict, "type");
        if (!string.IsNullOrWhiteSpace(t) && t == "meta") return true;
        return false;
    }

    private static string GetString(Dictionary<string, object> dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key)) return null;
        var v = dict[key];
        return v == null ? null : v.ToString();
    }

    private static Dictionary<string, object> GetPayload(Dictionary<string, object> dict)
    {
        if (dict == null || !dict.ContainsKey("payload")) return null;
        return dict["payload"] as Dictionary<string, object>;
    }

    private static long GetEpoch(Dictionary<string, object> dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key)) return 0;
        var v = dict[key];
        if (v == null) return 0;
        if (v is long) return (long)v;
        if (v is int) return (int)v;
        if (v is double) return (long)(double)v;
        long r;
        if (long.TryParse(v.ToString(), out r)) return r;
        DateTimeOffset dto;
        if (DateTimeOffset.TryParse(v.ToString(), out dto)) return dto.ToUnixTimeSeconds();
        return 0;
    }

    private static long GetLong(Dictionary<string, object> dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key)) return 0;
        var v = dict[key];
        if (v == null) return 0;
        if (v is long) return (long)v;
        if (v is int) return (int)v;
        if (v is double) return (long)(double)v;
        long r;
        long.TryParse(v.ToString(), out r);
        return r;
    }

    private static void AppendLine(RichTextBox box, string text, Color color)
    {
        int start = box.TextLength;
        box.AppendText(text);
        int end = box.TextLength;
        box.Select(start, end - start);
        box.SelectionColor = color;
        box.SelectionLength = 0;
        box.SelectionColor = box.ForeColor;
    }

    private static List<string> ReadAllLinesShared(string path)
    {
        try
        {
            var lines = new List<string>();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs, Encoding.UTF8, true, 4096))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines;
        }
        catch
        {
            return null;
        }
    }

    private bool UpdateSessionCwd(string path, string targetCwd)
    {
        try
        {
            var lines = new List<string>();
            foreach (var line in File.ReadLines(path, Encoding.UTF8))
            {
                var dict = ParseJson(line);
                if (dict != null && dict.ContainsKey("cwd"))
                {
                    dict["cwd"] = targetCwd;
                    lines.Add(_json.Serialize(dict));
                }
                else
                {
                    lines.Add(line);
                }
            }
            File.WriteAllLines(path, lines, new UTF8Encoding(false));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void UpdateSessionIds(string path, string oldId, string newId)
    {
        if (oldId == newId) return;
        var lines = new List<string>();
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            var dict = ParseJson(line);
            if (dict != null)
            {
                if (dict.ContainsKey("thread_id") && GetString(dict, "thread_id") == oldId) dict["thread_id"] = newId;
                if (dict.ContainsKey("id") && GetString(dict, "id") == oldId) dict["id"] = newId;
                lines.Add(_json.Serialize(dict));
            }
            else
            {
                lines.Add(line);
            }
        }
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private void UpdateSessionIndexTitles(string root, Dictionary<string, string> updates)
    {
        string indexPath = Path.Combine(root, "session_index.jsonl");
        if (!File.Exists(indexPath) || updates.Count == 0) return;
        var lines = new List<string>();
        foreach (var line in File.ReadLines(indexPath, Encoding.UTF8))
        {
            var dict = ParseJson(line);
            if (dict != null)
            {
                string id = GetString(dict, "thread_id") ?? GetString(dict, "id");
                if (!string.IsNullOrWhiteSpace(id) && updates.ContainsKey(id))
                {
                    dict["title"] = updates[id];
                    dict["updated_at"] = DateTimeOffset.Now.ToUnixTimeSeconds();
                    lines.Add(_json.Serialize(dict));
                    continue;
                }
            }
            lines.Add(line);
        }
        File.WriteAllLines(indexPath, lines, new UTF8Encoding(false));
    }

    private void UpdateSessionIndexCwds(string root, Dictionary<string, string> updates)
    {
        string indexPath = Path.Combine(root, "session_index.jsonl");
        if (!File.Exists(indexPath) || updates.Count == 0) return;
        var lines = new List<string>();
        foreach (var line in File.ReadLines(indexPath, Encoding.UTF8))
        {
            var dict = ParseJson(line);
            if (dict != null)
            {
                string id = GetString(dict, "thread_id") ?? GetString(dict, "id");
                if (!string.IsNullOrWhiteSpace(id) && updates.ContainsKey(id))
                {
                    dict["cwd"] = updates[id];
                    dict["updated_at"] = DateTimeOffset.Now.ToUnixTimeSeconds();
                    lines.Add(_json.Serialize(dict));
                    continue;
                }
            }
            lines.Add(line);
        }
        File.WriteAllLines(indexPath, lines, new UTF8Encoding(false));
    }
    private void UpdateSqliteTitles(string root, Dictionary<string, string> updates)
    {
        if (updates.Count == 0) return;
        RunSqliteUpdate(root, updates, "title");
    }

    private void UpdateSqliteCwds(string root, Dictionary<string, string> updates)
    {
        if (updates.Count == 0) return;
        RunSqliteUpdate(root, updates, "cwd");
    }

    private void RunSqliteUpdate(string root, Dictionary<string, string> updates, string field)
    {
        string db = Path.Combine(root, "state_5.sqlite");
        if (!File.Exists(db)) return;

        string tmpDir = Path.Combine(Path.GetTempPath(), "codex-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        string updatesJson = Path.Combine(tmpDir, "updates.json");
        File.WriteAllText(updatesJson, _json.Serialize(updates), new UTF8Encoding(false));

        string scriptPath = Path.Combine(tmpDir, "update_sqlite.py");
        var script = new StringBuilder();
        script.AppendLine("import json, sqlite3, time");
        script.AppendLine("import codecs");
        script.AppendLine("db = r'" + db.Replace("'", "''") + "'");
        script.AppendLine("with codecs.open(r'" + updatesJson.Replace("'", "''") + "', 'r', encoding='utf-8-sig') as f:");
        script.AppendLine("    updates = json.load(f)");
        script.AppendLine("conn = sqlite3.connect(db)");
        script.AppendLine("cur = conn.cursor()");
        script.AppendLine("now = int(time.time())");
        script.AppendLine("for k,v in updates.items():");
        script.AppendLine("    cur.execute('UPDATE threads SET " + field + " = ?, updated_at = ? WHERE id = ?', (v, now, k))");
        script.AppendLine("conn.commit()");
        script.AppendLine("conn.close()");
        File.WriteAllText(scriptPath, script.ToString(), new UTF8Encoding(false));

        string python = FindPython();
        if (string.IsNullOrWhiteSpace(python))
        {
MessageBox.Show(L("找不到 Python，SQLite 未更新。", "Python not found; SQLite not updated."));
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = python,
            Arguments = "\"" + scriptPath + "\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };
        try { Process.Start(psi).WaitForExit(); } catch { }
    }

    private string FindPython()
    {
        var candidates = new[]
        {
            @"D:\conda\envsode-env\python.exe",
            @"D:\conda\python.exe",
            @"F:\ocr_envs\py310\python.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"miniconda3\python.exe"),
            "python"
        };
        foreach (var c in candidates)
        {
            if (c == "python") return c;
            if (File.Exists(c)) return c;
        }
        return null;
    }

    private Manifest ReadManifest(string manifestPath)
    {
        try
        {
            string json = File.ReadAllText(manifestPath, Encoding.UTF8);
            if (json.TrimStart().StartsWith("["))
            {
                var items = _json.Deserialize<List<ManifestItem>>(json);
                return new Manifest { version = 1, items = items ?? new List<ManifestItem>() };
            }
            var obj = _json.Deserialize<Dictionary<string, object>>(json);
            if (obj != null && obj.ContainsKey("items"))
            {
                var arr = obj["items"] as ArrayList;
                var items = new List<ManifestItem>();
                if (arr != null)
                {
                    foreach (var it in arr)
                    {
                        var dict = it as Dictionary<string, object>;
                        if (dict == null) continue;
                        items.Add(new ManifestItem
                        {
                            id = GetString(dict, "id"),
                            title = GetString(dict, "title"),
                            cwd = GetString(dict, "cwd"),
                            file = GetString(dict, "file"),
                            updated_at = GetLong(dict, "updated_at")
                        });
                    }
                }
                return new Manifest { version = 1, items = items };
            }
        }
        catch { }
        return null;
    }
}
public class SessionItem
{
    public string Id;
    public string Title;
    public string Cwd;
    public long UpdatedAt;
    public string Path;
    public string Root;
}

public class Manifest
{
    public int version { get; set; }
    public List<ManifestItem> items { get; set; }
}

public class ManifestItem
{
    public string id { get; set; }
    public string title { get; set; }
    public string cwd { get; set; }
    public long updated_at { get; set; }
    public string file { get; set; }
    public string target_cwd { get; set; }
}

public class ImportForm : Form
{
    private DataGridView grid;
    private Button btnOk;
    private Button btnCancel;
    private readonly string lang;
    public List<ManifestItem> SelectedItems { get; private set; }

    public ImportForm(List<ManifestItem> items, List<string> targets, string lang)
    {
        this.lang = lang;
        SelectedItems = new List<ManifestItem>();
        Text = L("导入会话", "Import Sessions");
        Width = 900;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;

        grid = new DataGridView
        {
            Dock = DockStyle.Top,
            Height = 420,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false
        };

        var colCheck = new DataGridViewCheckBoxColumn { HeaderText = L("导入", "Import"), Width = 60 };
        var colTitle = new DataGridViewTextBoxColumn { HeaderText = L("标题", "Title"), Width = 260 };
var colCwd = new DataGridViewTextBoxColumn { HeaderText = L("来源工作区", "Source CWD"), Width = 260 };
        var colTarget = new DataGridViewComboBoxColumn
        {
            HeaderText = L("目标工作区", "Target CWD"),
            Width = 260,
            FlatStyle = FlatStyle.Flat,
            DataSource = targets
        };

        grid.Columns.Add(colCheck);
        grid.Columns.Add(colTitle);
        grid.Columns.Add(colCwd);
        grid.Columns.Add(colTarget);

        foreach (var item in items)
        {
            int idx = grid.Rows.Add(true, item.title, item.cwd, item.cwd);
            grid.Rows[idx].Tag = item;
        }

        btnOk = new Button { Text = L("导入", "Import"), Width = 120, Height = 30, Left = 620, Top = 440 };
        btnCancel = new Button { Text = L("取消", "Cancel"), Width = 120, Height = 30, Left = 750, Top = 440 };
        btnOk.Click += (s, e) => OnOk();
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        Controls.Add(grid);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
    }

    private void OnOk()
    {
        SelectedItems.Clear();
        foreach (DataGridViewRow row in grid.Rows)
        {
            bool import = false;
            if (row.Cells[0].Value is bool)
            {
                import = (bool)row.Cells[0].Value;
            }
            if (!import) continue;
            var item = row.Tag as ManifestItem;
            if (item == null) continue;
            var val = row.Cells[3].Value;
            item.target_cwd = val != null ? val.ToString() : item.cwd;
            SelectedItems.Add(item);
        }
        DialogResult = DialogResult.OK;
    }

    private string L(string zh, string en)
    {
        return lang == "zh" ? zh : en;
    }
}










