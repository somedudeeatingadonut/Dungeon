// -----------------------------------------------------------------------------
//  BatchTools.cs
//
//  Adds two things to the Fire Emblem Three Houses Randomizer:
//
//   1. "Scan folder and load all files"
//      Point it at one folder (for example your extracted romfs / data dump) and
//      it finds every input the randomizer needs - DATA1.bin, the fixed_*.bin
//      files, the scrdata files and BGM.ktsl2stbin - and loads them in one go,
//      instead of opening 20 separate file dialogs.
//
//   2. "Randomize and save to output folder"
//      Every randomized file is written straight into the romfs patch layout
//      (nx\sound, patch1\common\common, patch3\nx\data, patch4, patch4\nx\data
//      and patch4\common\common) inside one output folder, with no save dialogs.
//      The layout is identical to the folder structure the old hand-made
//      "move the .bin files" batch file produced.
//
//  The code lives in a partial class on purpose so the decompiled
//  RandomizerForm.cs only needs two tiny edits (see README.md).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    public partial class RandomizerForm
    {
        // ---------------------------------------------------------------- state
        private TabPage batch_tabPage;
        private TextBox batchInputFolder_textBox;
        private TextBox batchOutputFolder_textBox;
        private TextBox batchLog_textBox;
        private CheckBox batchIncludeSubfolders_checkBox;
        private CheckBox batchCopyData1_checkBox;
        private Button batchBrowseInput_button;
        private Button batchScan_button;
        private Button batchBrowseOutput_button;
        private Button batchSave_button;
        private Button batchOpenOutput_button;

        private string batchOutputFolder;

        // -------------------------------------------------- known input files
        private sealed class BatchInputSpec
        {
            public string Key;
            public string FileName;
            public long ExpectedSize;
            public string Destination;

            public BatchInputSpec(string key, string fileName, long expectedSize, string destination)
            {
                this.Key = key;
                this.FileName = fileName;
                this.ExpectedSize = expectedSize;
                this.Destination = destination;
            }
        }

        private static readonly BatchInputSpec[] batchInputSpecs = new BatchInputSpec[]
        {
            new BatchInputSpec("data1",     "DATA1.bin",                  0,         "patched in place"),
            new BatchInputSpec("person",    "fixed_persondata.bin",       139456,    @"patch4\nx\data"),
            new BatchInputSpec("class",     "fixed_classdata.bin",        23812,     @"patch4\nx\data"),
            new BatchInputSpec("item",      "fixed_data.bin",             39000,     @"patch4\nx\data"),
            new BatchInputSpec("calendar",  "fixed_calendardata.bin",     14696,     @"patch3\nx\data"),
            new BatchInputSpec("lobby",     "fixed_lobby_common.bin",     44616,     @"patch4\nx\data"),
            new BatchInputSpec("act",       "fixed_act_data.bin",         172732,    @"patch4\nx\data"),
            new BatchInputSpec("growth",    "fixed_growthdata.bin",       1404,      @"patch4\nx\data"),
            new BatchInputSpec("activity",  "fixed_lobby_activity.bin",   16504,     @"patch4\nx\data"),
            new BatchInputSpec("groupwork", "fixed_groupworkdata.bin",    4432,      @"patch4\nx\data"),
            new BatchInputSpec("present",   "fixed_presentdata.bin",      6180,      @"patch4\nx\data"),
            new BatchInputSpec("shop",      "fixed_shopdata.bin",         18676,     @"patch4\nx\data"),
            new BatchInputSpec("quest",     "fixed_lobby_quest.bin",      8672,      @"patch4\nx\data"),
            new BatchInputSpec("scenario",  "fixed_scenario.bin",         4784,      @"patch4\nx\data"),
            new BatchInputSpec("msg",       "msgdata.bin",                0,         @"patch4\common\common"),
            new BatchInputSpec("scr",       "scrdata.bin",                0,         @"patch4\common\common"),
            new BatchInputSpec("gwscr",     "gwscrdata.bin",              0,         @"patch4\common\common"),
            new BatchInputSpec("tuscr",     "tuscrdata.bin",              0,         @"patch4\common\common"),
            new BatchInputSpec("btlscr",    "btlscrdata.bin",             0,         @"patch4\common\common"),
            new BatchInputSpec("bgm",       "BGM.ktsl2stbin",             398136704, @"nx\sound")
        };

        // ------------------------------------------------------- tab creation
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var perfLoadWatch = System.Diagnostics.Stopwatch.StartNew();
            this.applyPerformanceTweaks();
            // The folders picked last time, read before anything is built so the
            // boxes come up already pointing at them.
            RememberedFolders.load();
            RememberedFolders.applyToRuntime();
            this.buildBatchTab();
            this.buildExtrasTab();
            // Range limits on the Stat Boosts / Growth Boosts / Stat Bases tabs.
            this.installMoreOptionsOnTabs();
            perfLoadWatch.Stop();
            RandomizerForm.perfExtrasBuildMs = perfLoadWatch.ElapsedMilliseconds;
        }

        private void buildBatchTab()
        {
            if (this.batch_tabPage != null || this.FETHRand_tabControl == null)
                return;

            try
            {
                this.batch_tabPage = new TabPage();
                this.batch_tabPage.SuspendLayout();
                this.batch_tabPage.Text = "Batch: folders";
                this.batch_tabPage.Padding = new Padding(10);
                this.batch_tabPage.UseVisualStyleBackColor = true;

                TableLayoutPanel layout = new TableLayoutPanel();
                layout.Dock = DockStyle.Fill;
                layout.ColumnCount = 3;
                layout.RowCount = 8;
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                for (int i = 0; i < 7; i++)
                    layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                // ---- header
                Label header = new Label();
                header.AutoSize = true;
                header.MaximumSize = new Size(1000, 0);
                header.Margin = new Padding(0, 0, 0, 8);
                header.Text = "Load every file the randomizer needs from one folder, and write all of "
                    + "the randomized files straight into the romfs patch folder structure.";
                layout.Controls.Add(header, 0, 0);
                layout.SetColumnSpan(header, 3);

                // ---- input folder
                Label inputLabel = new Label();
                inputLabel.AutoSize = true;
                inputLabel.Anchor = AnchorStyles.Left;
                inputLabel.Margin = new Padding(0, 6, 8, 6);
                inputLabel.Text = "Folder with the game files:";
                layout.Controls.Add(inputLabel, 0, 1);

                this.batchInputFolder_textBox = new TextBox();
                this.batchInputFolder_textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                this.batchInputFolder_textBox.Margin = new Padding(0, 6, 8, 6);
                this.batchInputFolder_textBox.Text = RememberedFolders.inputFolder.Length > 0
                    ? RememberedFolders.inputFolder : Application.StartupPath;
                this.batchInputFolder_textBox.TextChanged +=
                    new EventHandler(this.batchInputFolder_textBox_TextChanged);
                layout.Controls.Add(this.batchInputFolder_textBox, 1, 1);

                this.batchBrowseInput_button = new Button();
                this.batchBrowseInput_button.AutoSize = true;
                this.batchBrowseInput_button.Margin = new Padding(0, 3, 0, 3);
                this.batchBrowseInput_button.Text = "Browse...";
                this.batchBrowseInput_button.Click += new EventHandler(this.batchBrowseInput_button_Click);
                layout.Controls.Add(this.batchBrowseInput_button, 2, 1);

                // ---- subfolder option
                this.batchIncludeSubfolders_checkBox = new CheckBox();
                this.batchIncludeSubfolders_checkBox.AutoSize = true;
                this.batchIncludeSubfolders_checkBox.Checked = true;
                this.batchIncludeSubfolders_checkBox.Margin = new Padding(0, 0, 0, 6);
                this.batchIncludeSubfolders_checkBox.Text = "Search subfolders too (looks through a whole extracted romfs dump)";
                layout.Controls.Add(this.batchIncludeSubfolders_checkBox, 0, 2);
                layout.SetColumnSpan(this.batchIncludeSubfolders_checkBox, 3);

                // ---- scan button
                this.batchScan_button = new Button();
                this.batchScan_button.AutoSize = true;
                this.batchScan_button.Margin = new Padding(0, 0, 0, 12);
                this.batchScan_button.Text = "Scan folder and load all files";
                this.batchScan_button.Click += new EventHandler(this.batchScan_button_Click);
                layout.Controls.Add(this.batchScan_button, 0, 3);
                layout.SetColumnSpan(this.batchScan_button, 3);

                // ---- output folder
                Label outputLabel = new Label();
                outputLabel.AutoSize = true;
                outputLabel.Anchor = AnchorStyles.Left;
                outputLabel.Margin = new Padding(0, 6, 8, 6);
                outputLabel.Text = "Output folder:";
                layout.Controls.Add(outputLabel, 0, 4);

                this.batchOutputFolder_textBox = new TextBox();
                this.batchOutputFolder_textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                this.batchOutputFolder_textBox.Margin = new Padding(0, 6, 8, 6);
                this.batchOutputFolder_textBox.Text = RememberedFolders.outputFolder.Length > 0
                    ? RememberedFolders.outputFolder
                    : Path.Combine(Application.StartupPath, "randomizer output");
                this.batchOutputFolder_textBox.TextChanged += new EventHandler(this.batchOutputFolder_textBox_TextChanged);
                this.batchOutputFolder_textBox.TextChanged +=
                    new EventHandler(this.batchOutputFolderRemembered_TextChanged);
                layout.Controls.Add(this.batchOutputFolder_textBox, 1, 4);

                this.batchBrowseOutput_button = new Button();
                this.batchBrowseOutput_button.AutoSize = true;
                this.batchBrowseOutput_button.Margin = new Padding(0, 3, 0, 3);
                this.batchBrowseOutput_button.Text = "Browse...";
                this.batchBrowseOutput_button.Click += new EventHandler(this.batchBrowseOutput_button_Click);
                layout.Controls.Add(this.batchBrowseOutput_button, 2, 4);

                // ---- copy DATA1 option
                this.batchCopyData1_checkBox = new CheckBox();
                this.batchCopyData1_checkBox.AutoSize = true;
                this.batchCopyData1_checkBox.Margin = new Padding(0, 0, 0, 8);
                this.batchCopyData1_checkBox.Text = "Also copy the patched DATA1.bin into the output folder "
                    + "(DATA1.bin can be several GB, so this is off by default)";
                layout.Controls.Add(this.batchCopyData1_checkBox, 0, 5);
                layout.SetColumnSpan(this.batchCopyData1_checkBox, 3);

                // ---- save / open buttons
                FlowLayoutPanel buttonRow = new FlowLayoutPanel();
                buttonRow.AutoSize = true;
                buttonRow.WrapContents = false;
                buttonRow.Margin = new Padding(0, 0, 0, 8);

                this.batchSave_button = new Button();
                this.batchSave_button.AutoSize = true;
                this.batchSave_button.Text = "Randomize and save to output folder";
                this.batchSave_button.Click += new EventHandler(this.batchSave_button_Click);
                buttonRow.Controls.Add(this.batchSave_button);

                this.batchOpenOutput_button = new Button();
                this.batchOpenOutput_button.AutoSize = true;
                this.batchOpenOutput_button.Text = "Open output folder";
                this.batchOpenOutput_button.Click += new EventHandler(this.batchOpenOutput_button_Click);
                buttonRow.Controls.Add(this.batchOpenOutput_button);

                layout.Controls.Add(buttonRow, 0, 6);
                layout.SetColumnSpan(buttonRow, 3);

                // ---- log
                this.batchLog_textBox = new TextBox();
                this.batchLog_textBox.Dock = DockStyle.Fill;
                this.batchLog_textBox.Multiline = true;
                this.batchLog_textBox.ReadOnly = true;
                this.batchLog_textBox.ScrollBars = ScrollBars.Both;
                this.batchLog_textBox.WordWrap = false;
                this.batchLog_textBox.BackColor = Color.White;
                this.batchLog_textBox.Font = new Font("Consolas", 9f);
                this.batchLog_textBox.Margin = new Padding(0);
                layout.Controls.Add(this.batchLog_textBox, 0, 7);
                layout.SetColumnSpan(this.batchLog_textBox, 3);

                this.batch_tabPage.Controls.Add(layout);
                this.FETHRand_tabControl.TabPages.Add(this.batch_tabPage);

                this.batchOutputFolder = this.batchOutputFolder_textBox.Text.Trim();

                if (this.batchIncludeSubfolders_checkBox != null)
                    this.batchIncludeSubfolders_checkBox.Checked = RememberedFolders.includeSubfolders;

                this.batchWriteLog("Batch mode ready.");
                if (RememberedFolders.restoredNote.Length > 0)
                    this.batchWriteLog("Remembered folders: " + RememberedFolders.restoredNote
                        + "   (\"" + RememberedFolders.storeFile() + "\")");
                if (RememberedFolders.inputFolder.Length > 0
                    && Directory.Exists(RememberedFolders.inputFolder))
                    this.batchWriteLog("Press \"Scan folder and load all files\" - the box above already holds "
                        + "the folder from last time.");
                this.batchWriteLog("1. Pick the folder that holds your dumped game files and press \"Scan folder and load all files\".");
                this.batchWriteLog("2. Pick an output folder and press \"Randomize and save to output folder\".");
                this.batchWriteLog("Everything is written into nx\\sound, patch1\\common\\common, patch3\\nx\\data, patch4, patch4\\common\\common and patch4\\nx\\data.");
            }
            catch (Exception exception)
            {
                MessageBox.Show("The batch tools could not be added to the window:\n\n" + exception.Message,
                    "Batch tools", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                if (this.batch_tabPage != null)
                {
                    this.batch_tabPage.ResumeLayout(false);
                    perfPreparePage(this.batch_tabPage);
                }
            }
        }

        // ------------------------------------------------------------ UI events
        private void batchBrowseInput_button_Click(object sender, EventArgs e)
        {
            string folder = this.batchPickFolder("Select the folder that contains the game files to randomize",
                this.batchInputFolder_textBox.Text);
            if (folder != null)
                this.batchInputFolder_textBox.Text = folder;
        }

        private void batchBrowseOutput_button_Click(object sender, EventArgs e)
        {
            string folder = this.batchPickFolder("Select the folder the randomized files should be written to",
                this.batchOutputFolder_textBox.Text);
            if (folder != null)
                this.batchOutputFolder_textBox.Text = folder;
        }

        private void batchOutputFolder_textBox_TextChanged(object sender, EventArgs e)
        {
            this.batchOutputFolder = this.batchOutputFolder_textBox.Text.Trim();
        }

        // Remembered so the next start already points at it. Kept separate from
        // the handler above, which also runs while the window is being built.
        private void batchOutputFolderRemembered_TextChanged(object sender, EventArgs e)
        {
            if (this.batchOutputFolder_textBox == null)
                return;
            string folder = this.batchOutputFolder_textBox.Text.Trim();
            if (folder.Length == 0 || !Directory.Exists(folder))
                return;
            RememberedFolders.rememberOutput(folder);
        }

        private void batchInputFolder_textBox_TextChanged(object sender, EventArgs e)
        {
            if (this.batchInputFolder_textBox == null)
                return;
            string folder = this.batchInputFolder_textBox.Text.Trim();
            if (folder.Length == 0 || !Directory.Exists(folder))
                return;
            RememberedFolders.rememberInput(folder);
        }

        private void batchOpenOutput_button_Click(object sender, EventArgs e)
        {
            string folder = this.batchOutputFolder_textBox.Text.Trim();
            if (folder.Length == 0)
            {
                MessageBox.Show("No output folder has been selected yet.", "Output folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + folder + "\"");
            }
            catch (Exception exception)
            {
                MessageBox.Show("The output folder could not be opened:\n\n" + exception.Message,
                    "Output folder", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private string batchPickFolder(string description, string initialFolder)
        {
            return this.batchPickFolder(description, initialFolder, null);
        }

        // secondChoice is where to start when the first one is empty or not a
        // folder - the mod browse uses it for the folder the last scan read. The
        // dialog itself is FolderPicker: the shell's own folder dialog, the same
        // one the file dialogs on the other tabs show, with an address bar you
        // can paste a path into.
        private string batchPickFolder(string description, string initialFolder, string secondChoice)
        {
            string startFolder = batchFirstUsableFolder(new string[]
            {
                initialFolder,
                secondChoice,
                this.batchInputFolder_textBox == null ? null : this.batchInputFolder_textBox.Text,
                this.batchOutputFolder_textBox == null ? null : this.batchOutputFolder_textBox.Text
            });
            return FolderPicker.pick(this, description, startFolder);
        }

        // The first of these that is a real folder, or "" when none of them is.
        // Anything under the temp folder or in bin\Release / bin\Debug is
        // skipped: that is where the exe lives, not where the user keeps a mod.
        private static string batchFirstUsableFolder(string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (candidate == null)
                    continue;
                candidate = candidate.Trim();
                if (candidate.Length == 0)
                    continue;
                try
                {
                    if (File.Exists(candidate))
                        candidate = Path.GetDirectoryName(candidate);
                    else if (!Directory.Exists(candidate))
                        continue;
                }
                catch
                {
                    continue;
                }
                if (candidate == null || candidate.Length == 0)
                    continue;
                if (batchLooksLikeBuildFolder(candidate))
                    continue;
                return candidate;
            }
            return "";
        }

        private static bool batchLooksLikeBuildFolder(string folder)
        {
            string lower = folder.ToLowerInvariant();
            if (lower.IndexOf("appdata\\local\\temp", StringComparison.Ordinal) >= 0)
                return true;
            if (lower.EndsWith("bin\\release", StringComparison.Ordinal))
                return true;
            if (lower.EndsWith("bin\\debug", StringComparison.Ordinal))
                return true;
            return false;
        }

        private void batchSave_button_Click(object sender, EventArgs e)
        {
            string folder = this.batchOutputFolder_textBox.Text.Trim();
            if (folder.Length == 0)
            {
                folder = this.batchPickFolder("Select the folder the randomized files should be written to", null);
                if (folder == null)
                    return;
                this.batchOutputFolder_textBox.Text = folder;
            }
            this.batchOutputFolder = folder;
            this.batchOutputFolder_textBox.Text = folder;
            this.randomizeAndSave_button_Click(sender, e);
        }

        // ---------------------------------------------------------------- scan
        private void batchScan_button_Click(object sender, EventArgs e)
        {
            string folder = this.batchInputFolder_textBox.Text.Trim();
            if (folder.Length == 0)
            {
                folder = this.batchPickFolder("Select the folder that contains the game files to randomize", null);
                if (folder == null)
                    return;
                this.batchInputFolder_textBox.Text = folder;
            }
            if (!Directory.Exists(folder))
            {
                MessageBox.Show("That folder does not exist:\n\n" + folder,
                    "Scan folder", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            this.batchScanFolder(folder);
        }

        private void batchScanFolder(string folder)
        {
            // Remembered for the debug report, which uses it to point at other
            // copies of a data file (a mod unzipped one level too high, say) - and
            // kept for the next start, so the same folder is one click away.
            ModSupport.lastScanFolder = folder;
            RememberedFolders.rememberScan(folder);
            if (this.batchIncludeSubfolders_checkBox != null)
                RememberedFolders.includeSubfolders = this.batchIncludeSubfolders_checkBox.Checked;

            // A Skyline plugin mod keeps no data files, so there is nothing for the
            // size checks to notice. Look for its plugin folder once instead.
            ModSupport.detectSkylineNear(folder);

            // Every file is remembered with the folder it came from, so a scan that
            // mixes two installs can say so.
            ModSupport.resetSources();
            SearchOption searchOption = this.batchIncludeSubfolders_checkBox.Checked
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            string[] files;
            try
            {
                files = Directory.GetFiles(folder, "*", searchOption);
            }
            catch (Exception exception)
            {
                MessageBox.Show("The folder could not be read:\n\n" + exception.Message,
                    "Scan folder", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Dictionary<string, List<string>> byName =
                new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                if (!byName.ContainsKey(name))
                    byName[name] = new List<string>();
                byName[name].Add(file);
            }

            this.batchWriteLog("");
            this.batchWriteLog("Scanning " + folder
                + (this.batchIncludeSubfolders_checkBox.Checked ? " (including subfolders)" : " (top folder only)")
                + ", " + files.Length.ToString() + " files seen.");

            List<string> loadedNames = new List<string>();
            List<string> missingNames = new List<string>();
            List<string> problemNames = new List<string>();

            foreach (BatchInputSpec spec in batchInputSpecs)
            {
                List<string> candidates;
                if (!byName.TryGetValue(spec.FileName, out candidates) || candidates.Count == 0)
                {
                    missingNames.Add(spec.FileName);
                    this.batchWriteLog("  missing   " + spec.FileName);
                    continue;
                }

                string chosen = this.batchChooseCandidate(spec, candidates);
                if (candidates.Count > 1)
                    this.batchWriteLog("  note      " + candidates.Count.ToString() + " copies of "
                        + spec.FileName + " found, using " + chosen);

                bool sizeLooksRight;
                try
                {
                    sizeLooksRight = this.batchLoadInput(spec, chosen);
                }
                catch (Exception exception)
                {
                    problemNames.Add(spec.FileName);
                    this.batchWriteLog("  FAILED    " + spec.FileName + " (" + exception.Message + ")");
                    continue;
                }

                loadedNames.Add(spec.FileName);

                // A mod's copy of a file is bigger than the game's, and in mod mode
                // the offset maps match it, so it is not an "unexpected size".
                bool modCopy = false;
                try
                {
                    modCopy = ModSupport.modMode
                        && ModSupport.isKnownModSize(spec.FileName, new FileInfo(chosen).Length);
                }
                catch
                {
                }

                ModSupport.noteSource(spec.FileName, chosen);
                bool inLayout = ModSupport.isInsideLayout(folder, chosen);
                if (sizeLooksRight)
                    this.batchWriteLog("  loaded    " + spec.FileName + "  ->  " + chosen
                        + (inLayout ? "" : "  [outside the romfs layout]"));
                else if (modCopy)
                    this.batchWriteLog("  loaded    " + spec.FileName + "  ->  " + chosen
                        + "  [the mod's copy, offsets handled]");
                else
                    this.batchWriteLog("  loaded    " + spec.FileName + "  ->  " + chosen
                        + "  [unexpected size, check this is the right file]");
            }

            try
            {
                this.updateAllDisplay();
            }
            catch
            {
            }

            this.batchWriteLog("");
            this.batchWriteLog("Loaded " + loadedNames.Count.ToString() + " of "
                + batchInputSpecs.Length.ToString() + " files.");

            string message = "Loaded " + loadedNames.Count.ToString() + " of "
                + batchInputSpecs.Length.ToString() + " files from:\n" + folder;

            // Files that came from nowhere near the romfs tree are almost always a
            // leftover mod folder sitting in the same parent, and mixing that into
            // the output is exactly the thing to avoid.
            List<string> foreign = ModSupport.foreignSourceLines();
            if (foreign.Count > 0)
            {
                this.batchWriteLog("");
                this.batchWriteLog("  WARNING   " + foreign.Count.ToString()
                    + " file(s) came from outside the romfs layout:");
                for (int i = 0; i < foreign.Count; i++)
                    this.batchWriteLog(foreign[i]);
                this.batchWriteLog("            these look like a different install - the output would");
                this.batchWriteLog("            mix it into the game. Scan the game's romfs folder only,");
                this.batchWriteLog("            or move the other folder out of the way, then scan again.");

                message += "\n\nWARNING - " + foreign.Count.ToString()
                    + " of them did not come from the romfs tree you scanned:\n";
                for (int i = 0; i < foreign.Count && i < 8; i++)
                    message += "  " + foreign[i].Trim() + "\n";
                if (foreign.Count > 8)
                    message += "  ... and " + (foreign.Count - 8).ToString() + " more\n";
                message += "\nThose belong to a different install (a leftover mod folder, most likely).";
                message += "\nThe randomized output would mix that install into the game.";
                message += "\nPoint the scan at the game's own romfs folder (or the mod's romfs),";
                message += "\nor move that folder somewhere else, and scan again.";
            }
            if (missingNames.Count > 0)
                message += "\n\nNot found (load these by hand if you need them):\n  "
                    + string.Join("\n  ", missingNames.ToArray());
            if (problemNames.Count > 0)
                message += "\n\nCould not be read:\n  " + string.Join("\n  ", problemNames.ToArray());

            MessageBox.Show(message, "Scan folder", MessageBoxButtons.OK,
                (problemNames.Count > 0 || foreign.Count > 0)
                    ? MessageBoxIcon.Exclamation : MessageBoxIcon.Information);
        }

        private string batchChooseCandidate(BatchInputSpec spec, List<string> candidates)
        {
            if (candidates.Count == 1)
                return candidates[0];

            List<string> exactSize = new List<string>();
            if (spec.ExpectedSize > 0)
            {
                foreach (string candidate in candidates)
                {
                    try
                    {
                        long length = new FileInfo(candidate).Length;
                        if (length == spec.ExpectedSize
                            || (ModSupport.modMode && ModSupport.isKnownModSize(spec.FileName, length)))
                            exactSize.Add(candidate);
                    }
                    catch
                    {
                    }
                }
            }

            List<string> pool = exactSize.Count > 0 ? exactSize : candidates;

            // The game only ever reads these out of a "data" folder inside the
            // patch trees. A mod's zip also carries a stray copy of one of them
            // at its root, and that copy is a different build - so a copy in a
            // data folder always wins over one that is not.
            bool anyInData = false;
            foreach (string candidate in pool)
                if (batchPathHasDataFolder(candidate))
                    anyInData = true;

            string best = pool[0];
            foreach (string candidate in pool)
            {
                if (anyInData && batchPathHasDataFolder(candidate) != batchPathHasDataFolder(best))
                {
                    if (batchPathHasDataFolder(candidate))
                        best = candidate;
                    continue;
                }
                int candidateDepth = batchPathDepth(candidate);
                int bestDepth = batchPathDepth(best);
                if (candidateDepth < bestDepth
                    || (candidateDepth == bestDepth && string.Compare(candidate, best, StringComparison.OrdinalIgnoreCase) < 0))
                    best = candidate;
            }
            return best;
        }

        private static bool batchPathHasDataFolder(string path)
        {
            string[] parts = path.Split(new char[]
                { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            for (int i = 0; i < parts.Length - 1; i++)
                if (string.Equals(parts[i], "data", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static int batchPathDepth(string path)
        {
            int depth = 0;
            foreach (char character in path)
            {
                if (character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar)
                    depth++;
            }
            return depth;
        }

        // Loads one file the same way the matching "Load ..." button does.
        // Returns false when the file size is not the usual one for that file.
        private bool batchLoadInput(BatchInputSpec spec, string path)
        {
            long length = new FileInfo(path).Length;
            // A mod's copy of a file is bigger than the game's, and in mod mode
            // the offset maps move the tables to match - that is not a wrong file.
            bool modCopy = ModSupport.modMode && ModSupport.isKnownModSize(spec.FileName, length);
            bool sizeLooksRight = spec.ExpectedSize <= 0 || length == spec.ExpectedSize || modCopy;

            switch (spec.Key)
            {
                case "data1":
                    if (this.data1FileStream != null)
                    {
                        this.data1FileStream.Close();
                        this.data1FileStream = null;
                    }
                    Randomizer.data1Filename = path;
                    this.data1FileStream = File.OpenRead(path);
                    Randomizer.baiSectionDataV = new byte[Randomizer.baiSectionLength];
                    this.data1FileStream.Seek(Randomizer.baiSectionOffset, SeekOrigin.Begin);
                    this.data1FileStream.Read(Randomizer.baiSectionDataV, 0, Randomizer.baiSectionLength);
                    Randomizer.textSectionDataV = new byte[Randomizer.textSectionLength];
                    this.data1FileStream.Seek(Randomizer.textSectionOffset, SeekOrigin.Begin);
                    this.data1FileStream.Read(Randomizer.textSectionDataV, 0, Randomizer.textSectionLength);
                    Randomizer.data1Loaded = true;
                    break;

                case "person":
                    Randomizer.personFilename = path;
                    Randomizer.personDataV = File.ReadAllBytes(path);
                    Randomizer.personLoaded = true;
                    break;

                case "class":
                    Randomizer.classFilename = path;
                    Randomizer.classDataV = File.ReadAllBytes(path);
                    Randomizer.classLoaded = true;
                    break;

                case "item":
                    Randomizer.itemFilename = path;
                    Randomizer.itemDataV = File.ReadAllBytes(path);
                    Randomizer.itemLoaded = true;
                    break;

                case "calendar":
                    Randomizer.calendarFilename = path;
                    Randomizer.calendarDataV = File.ReadAllBytes(path);
                    Randomizer.calendarLoaded = true;
                    break;

                case "lobby":
                    Randomizer.lobbyFilename = path;
                    Randomizer.lobbyDataV = File.ReadAllBytes(path);
                    Randomizer.lobbyLoaded = true;
                    break;

                case "act":
                    Randomizer.actFilename = path;
                    Randomizer.actDataV = File.ReadAllBytes(path);
                    Randomizer.actLoaded = true;
                    break;

                case "growth":
                    Randomizer.growthdataFilename = path;
                    Randomizer.growthdataDataV = File.ReadAllBytes(path);
                    Randomizer.growthdataLoaded = true;
                    break;

                case "activity":
                    Randomizer.activityFilename = path;
                    Randomizer.activityDataV = File.ReadAllBytes(path);
                    Randomizer.activityLoaded = true;
                    break;

                case "groupwork":
                    Randomizer.groupWorkFilename = path;
                    Randomizer.groupWorkDataV = File.ReadAllBytes(path);
                    Randomizer.groupWorkLoaded = true;
                    break;

                case "present":
                    Randomizer.presentFilename = path;
                    Randomizer.presentDataV = File.ReadAllBytes(path);
                    Randomizer.presentLoaded = true;
                    break;

                case "shop":
                    Randomizer.shopFilename = path;
                    Randomizer.shopDataV = File.ReadAllBytes(path);
                    Randomizer.shopLoaded = true;
                    break;

                case "quest":
                    Randomizer.questFilename = path;
                    Randomizer.questDataV = File.ReadAllBytes(path);
                    Randomizer.questLoaded = true;
                    break;

                case "scenario":
                    Randomizer.scenarioFilename = path;
                    Randomizer.scenarioDataV = File.ReadAllBytes(path);
                    Randomizer.scenarioLoaded = true;
                    break;

                case "msg":
                    Randomizer.msgFilename = path;
                    Randomizer.msgDataV = File.ReadAllBytes(path);
                    Randomizer.msgLoaded = true;
                    break;

                case "scr":
                    Randomizer.scrFilename = path;
                    Randomizer.scrDataV = File.ReadAllBytes(path);
                    Randomizer.scrLoaded = true;
                    break;

                case "gwscr":
                    Randomizer.gwscrFilename = path;
                    Randomizer.gwscrDataV = File.ReadAllBytes(path);
                    Randomizer.gwscrLoaded = true;
                    break;

                case "tuscr":
                    Randomizer.tuscrFilename = path;
                    Randomizer.tuscrDataV = File.ReadAllBytes(path);
                    Randomizer.tuscrLoaded = true;
                    break;

                case "btlscr":
                    Randomizer.btlscrFilename = path;
                    Randomizer.btlscrDataV = File.ReadAllBytes(path);
                    Randomizer.btlscrLoaded = true;
                    break;

                case "bgm":
                    Randomizer.BGMFilename = path;
                    Randomizer.BGMDataV = File.ReadAllBytes(path);
                    Randomizer.BGMLoaded = true;
                    break;

                default:
                    throw new InvalidOperationException("Unknown file key '" + spec.Key + "'.");
            }

            return sizeLooksRight;
        }

        // ---------------------------------------------------------------- save
        // Called from saveFiles() in RandomizerForm.cs when an output folder has
        // been chosen on the batch tab. Writes everything into the patch layout
        // without a single save dialog.
        private void saveFilesToFolder(string root)
        {
            StringBuilder log = new StringBuilder();
            int written = 0;
            int failed = 0;

            string patch4 = Path.Combine(root, "patch4");
            string common = Path.Combine(patch4, "common", "common");
            string nxData = Path.Combine(patch4, "nx", "data");
            string patch3Data = Path.Combine(root, "patch3", "nx", "data");
            string sound = Path.Combine(root, "nx", "sound");

            log.AppendLine();
            log.AppendLine("Saving to " + root);

            try
            {
                Directory.CreateDirectory(root);
            }
            catch (Exception exception)
            {
                MessageBox.Show("The output folder could not be created:\n\n" + exception.Message,
                    "Save to folder", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            if (Randomizer.personLoaded)
                written += this.batchWriteOutput(nxData, "fixed_persondata.bin", Randomizer.personData, log);
            if (Randomizer.classLoaded)
                written += this.batchWriteOutput(nxData, "fixed_classdata.bin", Randomizer.classData, log);
            if (Randomizer.itemLoaded)
                written += this.batchWriteOutput(nxData, "fixed_data.bin", Randomizer.itemData, log);
            if (Randomizer.calendarLoaded)
                written += this.batchWriteOutput(patch3Data, "fixed_calendardata.bin", Randomizer.calendarData, log);
            if (Randomizer.lobbyLoaded)
                written += this.batchWriteOutput(nxData, "fixed_lobby_common.bin", Randomizer.lobbyData, log);
            if (Randomizer.actLoaded)
                written += this.batchWriteOutput(nxData, "fixed_act_data.bin", Randomizer.actData, log);
            if (Randomizer.activityLoaded)
                written += this.batchWriteOutput(nxData, "fixed_lobby_activity.bin", Randomizer.activityData, log);
            if (Randomizer.groupWorkLoaded)
                written += this.batchWriteOutput(nxData, "fixed_groupworkdata.bin", Randomizer.groupWorkData, log);
            if (Randomizer.presentLoaded)
                written += this.batchWriteOutput(nxData, "fixed_presentdata.bin", Randomizer.presentData, log);
            if (Randomizer.shopLoaded)
                written += this.batchWriteOutput(nxData, "fixed_shopdata.bin", Randomizer.shopData, log);
            if (Randomizer.questLoaded)
                written += this.batchWriteOutput(nxData, "fixed_lobby_quest.bin", Randomizer.questData, log);
            if (Randomizer.scenarioLoaded)
                written += this.batchWriteOutput(nxData, "fixed_scenario.bin", Randomizer.scenarioData, log);
            if (Randomizer.growthdataLoaded)
                written += this.batchWriteOutput(nxData, "fixed_growthdata.bin", Randomizer.growthdataData, log);

            if (Randomizer.msgLoaded && (this.settings.changeMsg || RandomizerBig.statWeaponDescriptionsDone > 0))
                written += this.batchWriteOutput(common, "msgdata.bin", Randomizer.msgData, log);
            if (Randomizer.scrLoaded && this.settings.changeScr)
                written += this.batchWriteOutput(common, "scrdata.bin", Randomizer.scrData, log);
            if (Randomizer.gwscrLoaded && this.settings.changeGwscr)
                written += this.batchWriteOutput(common, "gwscrdata.bin", Randomizer.gwscrData, log);
            if (Randomizer.tuscrLoaded && this.settings.changeTuscr)
                written += this.batchWriteOutput(common, "tuscrdata.bin", Randomizer.tuscrData, log);
            if (Randomizer.btlscrLoaded && this.settings.changeBtlscr)
                written += this.batchWriteOutput(common, "btlscrdata.bin", Randomizer.btlscrData, log);

            if (Randomizer.BGMLoaded)
                written += this.batchWriteOutput(sound, "BGM.ktsl2stbin", Randomizer.BGMData, log);

            if (Randomizer.data1Loaded)
            {
                // DATA1.bin is patched in place, exactly like the normal save does.
                string data1Target = Randomizer.data1Filename;
                try
                {
                    if (this.data1FileStream != null)
                    {
                        this.data1FileStream.Close();
                        this.data1FileStream = null;
                    }

                    if (this.batchCopyData1_checkBox != null
                        && this.batchCopyData1_checkBox.Checked
                        && !string.IsNullOrEmpty(Randomizer.data1Filename))
                    {
                        string copyPath = Path.Combine(common, "DATA1.bin");
                        Directory.CreateDirectory(common);
                        File.Copy(Randomizer.data1Filename, copyPath, true);
                        data1Target = copyPath;
                        log.AppendLine("  copied    DATA1.bin  ->  " + copyPath);
                    }

                    BinaryWriter writer = new BinaryWriter(File.Open(data1Target, FileMode.Open));
                    try
                    {
                        writer.Seek((int)Randomizer.baiSectionOffset, SeekOrigin.Begin);
                        writer.Write(Randomizer.baiSectionData);
                        if (this.settings.changeData1Text)
                        {
                            this.longSeek(writer, Randomizer.textSectionOffset);
                            writer.Write(Randomizer.textSectionData);
                        }
                    }
                    finally
                    {
                        writer.Close();
                    }
                    written++;
                    log.AppendLine("  patched   DATA1.bin  ->  " + data1Target);

                    this.data1FileStream = File.OpenRead(Randomizer.data1Filename);

                    if (this.batchWriteResource(patch4, "INFO0.bin",
                            "Fire_Emblem_Three_Houses_Randomizer_V2.INFO0.bin", log))
                        written++;
                    if (this.batchWriteResource(patch4, "INFO2.bin",
                            "Fire_Emblem_Three_Houses_Randomizer_V2.INFO2.bin", log))
                        written++;
                }
                catch (Exception exception)
                {
                    failed++;
                    log.AppendLine("  FAILED    DATA1.bin (" + exception.Message + ")");
                }
            }

            if (this.generateChangelog_checkBox != null && this.generateChangelog_checkBox.Checked)
            {
                try
                {
                    Directory.CreateDirectory(root);
                    File.WriteAllText(Path.Combine(root, "FE3H Randomizer Changelog.txt"), Randomizer.changelog);
                    log.AppendLine("  written   FE3H Randomizer Changelog.txt  ->  " + root);
                }
                catch (Exception exception)
                {
                    failed++;
                    log.AppendLine("  FAILED    FE3H Randomizer Changelog.txt (" + exception.Message + ")");
                }
            }

            log.AppendLine();
            log.AppendLine("Wrote " + written.ToString() + " file(s) into the patch layout.");
            if (failed > 0)
                log.AppendLine("Failed: " + failed.ToString() + " file(s).");

            this.batchWriteLog(log.ToString());

            if (RandomizerDebug.autoWrite)
            {
                string reportPath = RandomizerDebug.WriteReport(root, "after saving to the output folder");
                if (reportPath != null)
                    this.batchWriteLog("Debug report written to " + reportPath + " (" + RandomizerDebug.lastSummary + ")");
                else
                    this.batchWriteLog("Debug report could not be written: " + RandomizerDebug.lastSummary);
            }

            string message = "Wrote " + written.ToString() + " file(s) into:\n" + root
                + "\n\nCopy the nx, patch1, patch3 and patch4 folders from there into the romfs folder of your mod.";
            if (failed > 0)
                message += "\n\n" + failed.ToString() + " file(s) could not be written. Is one of them still open in another program?";
            MessageBox.Show(message, "Save to folder", MessageBoxButtons.OK,
                failed > 0 ? MessageBoxIcon.Exclamation : MessageBoxIcon.Information);
        }

        private int batchWriteOutput(string folder, string fileName, byte[] data, StringBuilder log)
        {
            if (data == null)
            {
                log.AppendLine("  skipped   " + fileName + " (no data)");
                return 0;
            }
            try
            {
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, fileName);
                File.WriteAllBytes(path, data);
                log.AppendLine("  written   " + fileName + "  ->  " + path);
                return 1;
            }
            catch (Exception exception)
            {
                log.AppendLine("  FAILED    " + fileName + " (" + exception.Message + ")");
                return 0;
            }
        }

        private bool batchWriteResource(string folder, string fileName, string resourceName, StringBuilder log)
        {
            try
            {
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, fileName);
                using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        log.AppendLine("  skipped   " + fileName + " (not embedded in the program)");
                        return false;
                    }
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    File.WriteAllBytes(path, buffer);
                }
                log.AppendLine("  written   " + fileName + "  ->  " + path);
                return true;
            }
            catch (Exception exception)
            {
                log.AppendLine("  FAILED    " + fileName + " (" + exception.Message + ")");
                return false;
            }
        }

        // --------------------------------------------------------------- log
        private void batchWriteLog(string text)
        {
            if (this.batchLog_textBox == null)
                return;
            try
            {
                this.batchLog_textBox.AppendText(text + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
