// -----------------------------------------------------------------------------
//  RandomizerExtrasTab.cs
//
//  The "Extra options" tab: class variety, ability blocklist, siege tome limit
//  and the level scaling growth shift. All values are pushed straight into
//  RandomizerExtras, which the randomization code consults while it runs.
// -----------------------------------------------------------------------------

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    public partial class RandomizerForm
    {
        private TabPage extras_tabPage;
        private CheckBox extrasEvenClasses_checkBox;
        private NumericUpDown extrasSafety_numericUpDown;
        private CheckedListBox extrasAbilities_checkedListBox;
        private CheckBox extrasSiege_checkBox;
        private NumericUpDown extrasMaxRange_numericUpDown;
        private NumericUpDown extrasSiegeChance_numericUpDown;
        private NumericUpDown extrasGrowthShift_numericUpDown;
        private CheckBox extrasBossAb_checkBox;
        private CheckBox extrasBossStats_checkBox;
        private CheckBox extrasUniquePersonal_checkBox;
        private CheckBox extrasUniqueClassAb_checkBox;
        private CheckBox extrasUniqueTierMastery_checkBox;
        private CheckBox extrasUniqueTierMasteryArt_checkBox;
        private CheckBox extrasEvenGrowthTotals_checkBox;
        private CheckBox extrasGrowthIdentity_checkBox;
        private CheckBox extrasEvenStatTotals_checkBox;
        private NumericUpDown extrasGrowthTolerance_numericUpDown;
        private NumericUpDown extrasBossScaling_numericUpDown;
        private NumericUpDown extrasStatTotalTolerance_numericUpDown;
        private CheckBox extrasBigStatSwap_checkBox;
        private CheckBox extrasBigGrowthSwap_checkBox;
        private CheckBox extrasBigScope_checkBox;
        private CheckBox extrasBigCharSheets_checkBox;
        private CheckBox extrasBigBossRemix_checkBox;
        private NumericUpDown extrasGraftWeapon_numericUpDown;
        private NumericUpDown extrasGraftWeaponFromArt_numericUpDown;
        private NumericUpDown extrasGraftWeaponFromGambit_numericUpDown;
        private NumericUpDown extrasGraftArt_numericUpDown;
        private NumericUpDown extrasGraftArtFromWeapon_numericUpDown;
        private NumericUpDown extrasGraftGambit_numericUpDown;
        private NumericUpDown extrasGraftGambitFromWeapon_numericUpDown;
        private CheckBox extrasBigWeaponUnique_checkBox;
        private CheckBox extrasBigWeaponDifferentType_checkBox;
        private CheckBox extrasModMode_checkBox;
        private TextBox extrasModFolder_textBox;
        private Label extrasModKind_label;
        private TextBox extrasModOriginals_textBox;
        private Label extrasModOffsets_label;
        private CheckBox extrasModMastery_checkBox;
        private CheckBox extrasClassIdentity_checkBox;
        private NumericUpDown extrasClassDefenseLimit_numericUpDown;
        private CheckBox extrasDescribeWeapons_checkBox;
        private NumericUpDown extrasFlatHp_numericUpDown;
        private NumericUpDown extrasFlatDef_numericUpDown;
        private NumericUpDown extrasFlatRes_numericUpDown;
        private CheckBox extrasModNoMonastery_checkBox;
        private ComboBox extrasPreset_comboBox;
        private Label extrasPresetInfo_label;
        private CheckBox extrasDebugAuto_checkBox;
        private Label extrasDebugPath_label;
        private bool extrasBuilding;

        // ---------------------------------------------------------- row helpers
        private static int addFullRow(TableLayoutPanel layout, Control control)
        {
            int row = layout.RowCount;
            layout.RowCount = row + 1;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
            return row;
        }

        private static int addSplitRow(TableLayoutPanel layout, Control left, Control right)
        {
            int row = layout.RowCount;
            layout.RowCount = row + 1;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(left, 0, row);
            layout.Controls.Add(right, 1, row);
            return row;
        }

        private static Label sectionLabel(string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Font = new Font(label.Font, FontStyle.Bold);
            label.Margin = new Padding(0, 16, 0, 4);
            label.Text = text;
            return label;
        }

        private static Label helpLabel(string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.MaximumSize = new Size(900, 0);
            label.ForeColor = Color.DimGray;
            label.Margin = new Padding(0, 2, 0, 2);
            label.Text = text;
            return label;
        }

        private static Label indentedLabel(string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Anchor = AnchorStyles.Left;
            label.Margin = new Padding(20, 6, 8, 6);
            label.Text = text;
            return label;
        }

        // ------------------------------------------------------------- the tab
        private void buildExtrasTab()
        {
            if (this.extras_tabPage != null || this.FETHRand_tabControl == null)
                return;

            // The preset system needs to know the starting values of the passes
            // in this project before anything is changed, and it needs the list of
            // preset files before the combo box is filled.
            RandomizerPresets.rememberDefaults();
            RandomizerPresets.refresh();

            this.extrasBuilding = true;
            try
            {
                this.extras_tabPage = new TabPage();
                this.extras_tabPage.SuspendLayout();
                this.extras_tabPage.Text = "Extra options";
                this.extras_tabPage.Padding = new Padding(10);
                this.extras_tabPage.UseVisualStyleBackColor = true;

                Panel scrollPanel = new Panel();
                scrollPanel.Dock = DockStyle.Fill;
                scrollPanel.AutoScroll = true;

                TableLayoutPanel layout = new TableLayoutPanel();
                layout.AutoSize = true;
                layout.Dock = DockStyle.Top;
                layout.ColumnCount = 2;
                layout.RowCount = 0;
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

                addFullRow(layout, sectionLabel("Extra randomizer options"));
                addFullRow(layout, helpLabel("These options are used when you press Randomize and Save, together with everything on the other tabs."));

                // --------------------------------------------------- quick setup
                FlowLayoutPanel quickButtons = new FlowLayoutPanel();
                quickButtons.AutoSize = true;
                quickButtons.WrapContents = true;
                quickButtons.Margin = new Padding(0, 4, 0, 4);

                Button enableAllButton = new Button();
                enableAllButton.AutoSize = true;
                enableAllButton.Text = "Enable every option (debug)";
                enableAllButton.Click += new EventHandler(this.quickEnableAll_button_Click);
                quickButtons.Controls.Add(enableAllButton);

                Button disableAllButton = new Button();
                disableAllButton.AutoSize = true;
                disableAllButton.Text = "Turn every option off";
                disableAllButton.Click += new EventHandler(this.quickDisableAll_button_Click);
                quickButtons.Controls.Add(disableAllButton);

                addFullRow(layout, quickButtons);
                addFullRow(layout, helpLabel(
                    "Enable every option ticks every randomization switch (including the extra ones on\r\n"
                    + "this tab and the range limits on the Classes tabs) in one click, for stress testing.\r\n"
                    + "The interpolation styles (binomial / linear / proportional / exponential) are left\r\n"
                    + "alone - they are alternatives to each other, not extra features."));

                // ------------------------------------------------------ presets
                addFullRow(layout, sectionLabel("Presets"));
                addFullRow(layout, helpLabel(
                    "A preset stores the whole configuration - every option on every tab plus the ones\r\n"
                    + "on this tab - and puts it back in one click. Applying a preset resets everything\r\n"
                    + "first, so the result is exactly what the preset says and not the preset plus\r\n"
                    + "whatever happened to be ticked already."));

                FlowLayoutPanel presetRow = new FlowLayoutPanel();
                presetRow.AutoSize = true;
                presetRow.WrapContents = true;
                presetRow.Margin = new Padding(0, 4, 0, 4);

                this.extrasPreset_comboBox = new ComboBox();
                this.extrasPreset_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                this.extrasPreset_comboBox.Width = 360;
                this.extrasPreset_comboBox.Margin = new Padding(0, 3, 8, 3);
                this.refreshPresetList();
                presetRow.Controls.Add(this.extrasPreset_comboBox);

                Button applyPresetButton = new Button();
                applyPresetButton.AutoSize = true;
                applyPresetButton.Text = "Apply preset";
                applyPresetButton.Click += new EventHandler(this.extrasPresetApply_button_Click);
                presetRow.Controls.Add(applyPresetButton);

                Button savePresetButton = new Button();
                savePresetButton.AutoSize = true;
                savePresetButton.Text = "Save current settings as preset...";
                savePresetButton.Click += new EventHandler(this.extrasPresetSave_button_Click);
                presetRow.Controls.Add(savePresetButton);

                Button deletePresetButton = new Button();
                deletePresetButton.AutoSize = true;
                deletePresetButton.Text = "Delete preset";
                deletePresetButton.Click += new EventHandler(this.extrasPresetDelete_button_Click);
                presetRow.Controls.Add(deletePresetButton);

                Button openPresetButton = new Button();
                openPresetButton.AutoSize = true;
                openPresetButton.Text = "Open preset folder";
                openPresetButton.Click += new EventHandler(this.extrasPresetOpen_button_Click);
                presetRow.Controls.Add(openPresetButton);

                addFullRow(layout, presetRow);

                this.extrasPresetInfo_label = new Label();
                this.extrasPresetInfo_label.AutoSize = true;
                this.extrasPresetInfo_label.MaximumSize = new Size(900, 0);
                this.extrasPresetInfo_label.ForeColor = Color.DimGray;
                this.extrasPresetInfo_label.Margin = new Padding(0, 2, 0, 2);
                this.extrasPresetInfo_label.Text = "Presets are stored as .txt files in the presets folder next to the exe.";
                addFullRow(layout, this.extrasPresetInfo_label);

                // --------------------------------------- total conversion mods
                addFullRow(layout, sectionLabel("Total conversion mod (No Monastery and friends)"));
                addFullRow(layout, helpLabel(
                    "Switch this on when the game runs a total conversion mod. Two kinds exist and the\r\n"
                    + "tool handles them differently:\r\n"
                    + "  data files (No Monastery 1.3.3 and older) - the mod ships its own fixed_*.bin,\r\n"
                    + "    so rows were added and every table after them moved. The offsets are read out\r\n"
                    + "    of the mod's own file header (1.3.3 is recognised with no folder at all), the\r\n"
                    + "    mod's mastery EXP values are put back after randomizing, and the Dancer row is\r\n"
                    + "    looked up in the loaded data instead of being assumed to be class 43\r\n"
                    + "  a Skyline plugin (No Monastery 2.1 'with Aldebaran') - the mod ships no game\r\n"
                    + "    data at all, only romfs\\skyline\\plugins\\libno_monastery.nro and an exefs\r\n"
                    + "    patch. Nothing can have moved, so the game's own files stay the input and the\r\n"
                    + "    compiled-in offsets are already right; only the options the plugin\r\n"
                    + "    recalculates (level scaling, class abilities) are listed as conflicts\r\n"
                    + "  either way the debug report lists every option that is still on and fights it"));

                this.extrasModMode_checkBox = new CheckBox();
                this.extrasModMode_checkBox.AutoSize = true;
                this.extrasModMode_checkBox.Checked = ModSupport.modMode;
                this.extrasModMode_checkBox.Text = "This game has a total conversion mod installed (mod mode)";
                this.extrasModMode_checkBox.CheckedChanged += new EventHandler(this.extrasModMode_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasModMode_checkBox);

                // ---- which mod: one folder, and the tool says what it is
                Label modFolderLabel = new Label();
                modFolderLabel.AutoSize = true;
                modFolderLabel.Anchor = AnchorStyles.Left;
                modFolderLabel.Margin = new Padding(0, 6, 8, 6);
                modFolderLabel.Text = "Mod folder (Browse or paste a path) - the unzipped mod, or"
                    + " contents/<title id> on the SD card:";

                this.extrasModFolder_textBox = new TextBox();
                this.extrasModFolder_textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                this.extrasModFolder_textBox.Margin = new Padding(0, 6, 8, 6);
                this.extrasModFolder_textBox.Width = 520;
                this.extrasModFolder_textBox.Text = ModSupport.modFolder;
                this.extrasModFolder_textBox.TextChanged += new EventHandler(this.extrasModFolder_textBox_TextChanged);

                FlowLayoutPanel modFolderRow = new FlowLayoutPanel();
                modFolderRow.AutoSize = true;
                modFolderRow.WrapContents = true;
                modFolderRow.Margin = new Padding(0, 2, 0, 2);
                modFolderRow.Controls.Add(this.extrasModFolder_textBox);

                Button modFolderBrowseButton = new Button();
                modFolderBrowseButton.AutoSize = true;
                modFolderBrowseButton.Text = "Browse...";
                modFolderBrowseButton.Click += new EventHandler(this.extrasModFolderBrowse_button_Click);
                modFolderRow.Controls.Add(modFolderBrowseButton);

                Button modCheckButton = new Button();
                modCheckButton.AutoSize = true;
                modCheckButton.Text = "Check the mod";
                modCheckButton.Click += new EventHandler(this.extrasModCheck_button_Click);
                modFolderRow.Controls.Add(modCheckButton);

                addFullRow(layout, modFolderLabel);
                addFullRow(layout, modFolderRow);

                this.extrasModKind_label = new Label();
                this.extrasModKind_label.AutoSize = true;
                this.extrasModKind_label.MaximumSize = new Size(900, 0);
                this.extrasModKind_label.ForeColor = Color.DimGray;
                this.extrasModKind_label.Margin = new Padding(0, 2, 0, 2);
                this.extrasModKind_label.Text = modKindText();
                addFullRow(layout, this.extrasModKind_label);

                // ---- originals folder: needed to work out the mod's table offsets
                Label originalsLabel = new Label();
                originalsLabel.AutoSize = true;
                originalsLabel.Anchor = AnchorStyles.Left;
                originalsLabel.Margin = new Padding(0, 6, 8, 6);
                originalsLabel.Text = "Unmodified game files (folder with the original fixed_*.bin):";

                this.extrasModOriginals_textBox = new TextBox();
                this.extrasModOriginals_textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                this.extrasModOriginals_textBox.Margin = new Padding(0, 6, 8, 6);
                this.extrasModOriginals_textBox.Width = 520;
                this.extrasModOriginals_textBox.Text = ModSupport.originalFolder;
                this.extrasModOriginals_textBox.TextChanged += new EventHandler(this.extrasModOriginals_textBox_TextChanged);

                FlowLayoutPanel originalsRow = new FlowLayoutPanel();
                originalsRow.AutoSize = true;
                originalsRow.WrapContents = true;
                originalsRow.Margin = new Padding(0, 2, 0, 2);
                originalsRow.Controls.Add(this.extrasModOriginals_textBox);

                Button originalsBrowseButton = new Button();
                originalsBrowseButton.AutoSize = true;
                originalsBrowseButton.Text = "Browse...";
                originalsBrowseButton.Click += new EventHandler(this.extrasModOriginalsBrowse_button_Click);
                originalsRow.Controls.Add(originalsBrowseButton);

                addFullRow(layout, originalsLabel);
                addFullRow(layout, originalsRow);

                this.extrasModOffsets_label = new Label();
                this.extrasModOffsets_label.AutoSize = true;
                this.extrasModOffsets_label.MaximumSize = new Size(900, 0);
                this.extrasModOffsets_label.ForeColor = Color.DimGray;
                this.extrasModOffsets_label.Margin = new Padding(0, 2, 0, 2);
                this.extrasModOffsets_label.Text = "Offsets: " + ModSupport.offsetsStatus;
                addFullRow(layout, this.extrasModOffsets_label);

                this.extrasModMastery_checkBox = new CheckBox();
                this.extrasModMastery_checkBox.AutoSize = true;
                this.extrasModMastery_checkBox.Checked = ModSupport.keepMasteryExp;
                this.extrasModMastery_checkBox.Text = "Keep the mod's class mastery EXP requirements";
                this.extrasModMastery_checkBox.CheckedChanged += new EventHandler(this.extrasModMastery_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasModMastery_checkBox);

                // ---- No Monastery mode: the parts of the run the mod makes pointless
                this.extrasModNoMonastery_checkBox = new CheckBox();
                this.extrasModNoMonastery_checkBox.AutoSize = true;
                this.extrasModNoMonastery_checkBox.Checked = ModSupport.noMonasteryMode;
                this.extrasModNoMonastery_checkBox.Enabled = ModSupport.modMode;
                this.extrasModNoMonastery_checkBox.Text =
                    "No Monastery mode: leave out the exploration and teaching options the mod removes";
                this.extrasModNoMonastery_checkBox.CheckedChanged +=
                    new EventHandler(this.extrasModNoMonastery_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasModNoMonastery_checkBox);

                Label noMonasteryInfo = helpLabel(
                    "The mod deletes exploration and weekly teaching, so goals, seminars, faculty training,\r\n"
                    + "dining hall / choir / greenhouse / fishing events, free day limits and rare monster\r\n"
                    + "sightings only write tables the game no longer opens. This mode leaves those out of\r\n"
                    + "the run (your ticks are kept, they just do not run), names every skipped option in\r\n"
                    + "the changelog and the debug report, switches mod mode on, and sets the first\r\n"
                    + "character spell to C - the mod starts units in intermediate classes, where the\r\n"
                    + "game's usual D is already earned and every unit would cast from level 1.");
                addFullRow(layout, noMonasteryInfo);

                FlowLayoutPanel modButtons = new FlowLayoutPanel();
                modButtons.AutoSize = true;
                modButtons.WrapContents = true;
                modButtons.Margin = new Padding(0, 4, 0, 4);

                Button readOffsetsButton = new Button();
                readOffsetsButton.AutoSize = true;
                readOffsetsButton.Text = "Read table offsets";
                readOffsetsButton.Click += new EventHandler(this.extrasModReadOffsets_button_Click);
                modButtons.Controls.Add(readOffsetsButton);

                Button analyseButton = new Button();
                analyseButton.AutoSize = true;
                analyseButton.Text = "Analyse the loaded files";
                analyseButton.Click += new EventHandler(this.extrasModAnalyse_button_Click);
                modButtons.Controls.Add(analyseButton);

                Button modSafePresetButton = new Button();
                modSafePresetButton.AutoSize = true;
                modSafePresetButton.Text = "Use the mod-safe preset";
                modSafePresetButton.Click += new EventHandler(this.extrasModSafePreset_button_Click);
                modButtons.Controls.Add(modSafePresetButton);

                Button modConflictsButton = new Button();
                modConflictsButton.AutoSize = true;
                modConflictsButton.Text = "Show options that fight the mod";
                modConflictsButton.Click += new EventHandler(this.extrasModConflicts_button_Click);
                modButtons.Controls.Add(modConflictsButton);

                addFullRow(layout, modButtons);

                addFullRow(layout, helpLabel(
                    "The offsets are read from the mod's own file header and compared with the\r\n"
                    + "originals in the folder above; for No Monastery 1.3.3 the same numbers are\r\n"
                    + "built in, so that folder is optional. A data mod with no built-in profile and no\r\n"
                    + "originals is not trusted: the run asks before it writes anything.\r\n"
                    + "A plugin mod needs none of that - check its folder once and the report records it.\r\n"
                    + "Analysis compares the loaded files with the original sizes, so it says whether you\r\n"
                    + "loaded the mod's copies or the game dump's. Load the mod's romfs, and write the\r\n"
                    + "result back into that same romfs folder - a file written anywhere else is\r\n"
                    + "overlaid by the mod and never reaches the game."));

                // ------------------------------------------------ class variety
                addFullRow(layout, sectionLabel("Random classes - even variety"));

                this.extrasEvenClasses_checkBox = new CheckBox();
                this.extrasEvenClasses_checkBox.AutoSize = true;
                this.extrasEvenClasses_checkBox.Checked = RandomizerExtras.evenClassVariety;
                this.extrasEvenClasses_checkBox.Text = "Spread weapon types evenly over every class tier instead of letting one type take over";
                this.extrasEvenClasses_checkBox.CheckedChanged += new EventHandler(this.extrasEvenClasses_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasEvenClasses_checkBox);

                this.extrasSafety_numericUpDown = new NumericUpDown();
                this.extrasSafety_numericUpDown.Minimum = 0;
                this.extrasSafety_numericUpDown.Maximum = 100;
                this.extrasSafety_numericUpDown.Increment = 10;
                this.extrasSafety_numericUpDown.Value = RandomizerExtras.classVarietySafety;
                this.extrasSafety_numericUpDown.Width = 70;
                this.extrasSafety_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasSafety_numericUpDown.ValueChanged += new EventHandler(this.extrasSafety_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Safety (%):"), this.extrasSafety_numericUpDown);

                addFullRow(layout, helpLabel(
                    "100% = every class tier other than the starting class gets all weapon types.\r\n"
                    + "60% (default) = most tiers cover every type, the rest stays balanced.\r\n"
                    + "0% = only one guaranteed option per weapon type across the whole run, so swords\r\n"
                    + "      could end up in a single master class.\r\n"
                    + "Higher safety also evens out the growths, stats, class abilities and combat arts\r\n"
                    + "inside each tier, so the classes you get are not all built from the same mould."));

                addFullRow(layout, sectionLabel("Class identity"));

                this.extrasClassIdentity_checkBox = new CheckBox();
                this.extrasClassIdentity_checkBox.AutoSize = true;
                this.extrasClassIdentity_checkBox.Checked = RandomizerExtras.classIdentity;
                this.extrasClassIdentity_checkBox.Text =
                    "Make each class's Str and Mag follow the weapon type it actually has now";
                this.extrasClassIdentity_checkBox.CheckedChanged +=
                    new EventHandler(this.extrasClassIdentity_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasClassIdentity_checkBox);

                this.extrasClassDefenseLimit_numericUpDown = new NumericUpDown();
                this.extrasClassDefenseLimit_numericUpDown.Minimum = 0;
                this.extrasClassDefenseLimit_numericUpDown.Maximum = 200;
                this.extrasClassDefenseLimit_numericUpDown.Increment = 10;
                this.extrasClassDefenseLimit_numericUpDown.Value = RandomizerExtras.classDefenseLimit;
                this.extrasClassDefenseLimit_numericUpDown.Width = 70;
                this.extrasClassDefenseLimit_numericUpDown.Enabled = RandomizerExtras.classIdentity;
                this.extrasClassDefenseLimit_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasClassDefenseLimit_numericUpDown.ValueChanged +=
                    new EventHandler(this.extrasClassDefenseLimit_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Class Def / Res may exceed vanilla by (%):"),
                    this.extrasClassDefenseLimit_numericUpDown);

                addFullRow(layout, helpLabel(
                    "The weapon type and the stats of a class are rolled by two different passes, so a\r\n"
                    + "class can end up using swords while its highest stat is Magic - the game then\r\n"
                    + "shows a Dark Mage with a mage's bonuses but a sword in its hand. With this on,\r\n"
                    + "Str and Mag (base stats, class boosts, both growth sets) are traded when they\r\n"
                    + "contradict the weapon type, so the numbers are kept but the right stat owns them.\r\n"
                    + "Hybrid classes, monsters and boss rows are left alone.\r\n"
                    + "The percentage is the other half: a class's base Def / Res and its Def / Res boosts\r\n"
                    + "may go this far above the vanilla class before they are pulled back. A class base is\r\n"
                    + "a floor for every unit in it and the boost is added on top, so one lucky roll used to\r\n"
                    + "be enough to make a beginner class - and every enemy in it - far tankier than it\r\n"
                    + "should be. 0% turns the pull-back off."));

                // ------------------------------------------------ flat bulk trim
                addFullRow(layout, sectionLabel("Flat enemy bulk trim"));

                this.extrasFlatHp_numericUpDown = new NumericUpDown();
                this.extrasFlatHp_numericUpDown.Minimum = 0;
                this.extrasFlatHp_numericUpDown.Maximum = 40;
                this.extrasFlatHp_numericUpDown.Increment = 1;
                this.extrasFlatHp_numericUpDown.Value = RandomizerExtras.flatHpTrim;
                this.extrasFlatHp_numericUpDown.Width = 70;
                this.extrasFlatHp_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasFlatHp_numericUpDown.ValueChanged +=
                    new EventHandler(this.extrasFlatTrim_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("HP taken off every class base:"),
                    this.extrasFlatHp_numericUpDown);

                this.extrasFlatDef_numericUpDown = new NumericUpDown();
                this.extrasFlatDef_numericUpDown.Minimum = 0;
                this.extrasFlatDef_numericUpDown.Maximum = 20;
                this.extrasFlatDef_numericUpDown.Increment = 1;
                this.extrasFlatDef_numericUpDown.Value = RandomizerExtras.flatDefTrim;
                this.extrasFlatDef_numericUpDown.Width = 70;
                this.extrasFlatDef_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasFlatDef_numericUpDown.ValueChanged +=
                    new EventHandler(this.extrasFlatTrim_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Def taken off every class base:"),
                    this.extrasFlatDef_numericUpDown);

                this.extrasFlatRes_numericUpDown = new NumericUpDown();
                this.extrasFlatRes_numericUpDown.Minimum = 0;
                this.extrasFlatRes_numericUpDown.Maximum = 20;
                this.extrasFlatRes_numericUpDown.Increment = 1;
                this.extrasFlatRes_numericUpDown.Value = RandomizerExtras.flatResTrim;
                this.extrasFlatRes_numericUpDown.Width = 70;
                this.extrasFlatRes_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasFlatRes_numericUpDown.ValueChanged +=
                    new EventHandler(this.extrasFlatTrim_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Res taken off every class base:"),
                    this.extrasFlatRes_numericUpDown);

                addFullRow(layout, helpLabel(
                    "Subtracted from the class base HP / Def / Res as the very last stat pass, so nothing\r\n"
                    + "clamps it back. A class base is what a level 1 unit of that class has and it is a\r\n"
                    + "floor for every unit in the class, so this is the part that makes the first maps\r\n"
                    + "feel walled - at level 40 the same two or three points barely register. -2 Def, -2\r\n"
                    + "Res and -6 HP is a good starting point for a No Monastery run.\r\n"
                    + "Bosses are included: the first boss of a run is an ordinary class like any other.\r\n"
                    + "(The monster / beast table is a different layout with no class base stats, so\r\n"
                    + "beast bosses keep their own numbers - it is not touched.)\r\n"
                    + "The game keeps one class table, so the same values are added to the personal bases\r\n"
                    + "of every character you can deploy (the students, the staff, Jeralt, the Ashen\r\n"
                    + "Wolves, Anna): your units keep their stats and only the enemy side gets weaker.\r\n"
                    + "Enemy copies of a playable character (an enemy Edelgard, say) keep their stats too,\r\n"
                    + "which is the price of doing it this way. It needs the character file loaded -\r\n"
                    + "without it the trim is skipped rather than silently weakening your own units.\r\n"
                    + "Boosts and growths are not touched. 0 on a row means that stat is not trimmed."));

                // ---------------------------------------------------- abilities
                addFullRow(layout, sectionLabel("Abilities that must never be randomized"));

                this.extrasAbilities_checkedListBox = new CheckedListBox();
                this.extrasAbilities_checkedListBox.CheckOnClick = true;
                this.extrasAbilities_checkedListBox.Height = 180;
                this.extrasAbilities_checkedListBox.Width = 620;
                this.extrasAbilities_checkedListBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                this.extrasAbilities_checkedListBox.Margin = new Padding(0, 3, 0, 6);
                for (int i = 0; i < Randomizer.abilityNames.Length; i++)
                {
                    string name = Randomizer.abilityNames[i];
                    if (name == null || name.Length == 0)
                        name = "(unnamed ability " + i.ToString() + ")";
                    this.extrasAbilities_checkedListBox.Items.Add(i.ToString() + ": " + name,
                        RandomizerExtras.blockedAbilities.Contains(i));
                }
                this.extrasAbilities_checkedListBox.ItemCheck += new ItemCheckEventHandler(this.extrasAbilities_checkedListBox_ItemCheck);
                addFullRow(layout, this.extrasAbilities_checkedListBox);

                FlowLayoutPanel abilityButtons = new FlowLayoutPanel();
                abilityButtons.AutoSize = true;
                abilityButtons.WrapContents = true;
                abilityButtons.Margin = new Padding(0, 0, 0, 4);

                Button dragonButton = new Button();
                dragonButton.AutoSize = true;
                dragonButton.Text = "Dragon / monster only";
                dragonButton.Click += new EventHandler(this.extrasDragonOnly_button_Click);
                abilityButtons.Controls.Add(dragonButton);

                Button noneButton = new Button();
                noneButton.AutoSize = true;
                noneButton.Text = "Tick nothing";
                noneButton.Click += new EventHandler(this.extrasTickNothing_button_Click);
                abilityButtons.Controls.Add(noneButton);

                Button vanillaButton = new Button();
                vanillaButton.AutoSize = true;
                vanillaButton.Text = "Add the abilities the randomizer already avoids";
                vanillaButton.Click += new EventHandler(this.extrasVanillaBad_button_Click);
                abilityButtons.Controls.Add(vanillaButton);

                addFullRow(layout, abilityButtons);
                addFullRow(layout, helpLabel(
                    "Anything ticked here is never handed out by the ability, class ability and learnset\r\n"
                    + "randomization. Dragon / monster abilities (Ancient Dragonskin, Ancient Dragon Wrath,\r\n"
                    + "Divine Dragon Horn, Giant Wings and the two Effect Null skills) are ticked by default.\r\n"
                    + "A second, fixed list can never be handed out whatever is ticked here: Twin Crests, Ten\r\n"
                    + "Elites, Mighty King of Legend, Dance and the rest of the boss-tier and monster skills -\r\n"
                    + "they are named in the debug report under Abilities."));

                // -------------------------------------------------- siege limit
                addFullRow(layout, sectionLabel("Enemy siege tomes and long range weapons"));

                this.extrasSiege_checkBox = new CheckBox();
                this.extrasSiege_checkBox.AutoSize = true;
                this.extrasSiege_checkBox.Checked = RandomizerExtras.limitSiegeTomes;
                this.extrasSiege_checkBox.Text = "Limit the attack range of the weapons randomized enemies receive";
                this.extrasSiege_checkBox.CheckedChanged += new EventHandler(this.extrasSiege_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasSiege_checkBox);

                this.extrasMaxRange_numericUpDown = new NumericUpDown();
                this.extrasMaxRange_numericUpDown.Minimum = 1;
                this.extrasMaxRange_numericUpDown.Maximum = 12;
                this.extrasMaxRange_numericUpDown.Value = RandomizerExtras.maxEnemyWeaponRange;
                this.extrasMaxRange_numericUpDown.Width = 70;
                this.extrasMaxRange_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasMaxRange_numericUpDown.ValueChanged += new EventHandler(this.extrasMaxRange_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Max attack range:"), this.extrasMaxRange_numericUpDown);

                this.extrasSiegeChance_numericUpDown = new NumericUpDown();
                this.extrasSiegeChance_numericUpDown.Minimum = 0;
                this.extrasSiegeChance_numericUpDown.Maximum = 100;
                this.extrasSiegeChance_numericUpDown.Increment = 5;
                this.extrasSiegeChance_numericUpDown.Value = RandomizerExtras.siegeChance;
                this.extrasSiegeChance_numericUpDown.Width = 70;
                this.extrasSiegeChance_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasSiegeChance_numericUpDown.ValueChanged += new EventHandler(this.extrasSiegeChance_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Chance an enemy still gets one (%):"), this.extrasSiegeChance_numericUpDown);

                addFullRow(layout, helpLabel(
                    "Weapons above this range are dropped from the pools that random map enemies draw\r\n"
                    + "from, so no more Meteor, Bolting or 4+ range longbows. 2 keeps the usual melee\r\n"
                    + "and 2 range setup. Player side weapons and shops are not touched.\r\n"
                    + "The chance lets that percentage of enemies ignore the limit - they still have to\r\n"
                    + "roll the siege weapon afterwards, so it is a chance on a chance."));

                // --------------------------------------------- weapon effect text
                addFullRow(layout, sectionLabel("Weapon effect text"));

                this.extrasDescribeWeapons_checkBox = new CheckBox();
                this.extrasDescribeWeapons_checkBox.AutoSize = true;
                this.extrasDescribeWeapons_checkBox.Checked = RandomizerBig.describeWeaponEffects;
                this.extrasDescribeWeapons_checkBox.Text =
                    "Write the randomized effect into the weapon's description text";
                this.extrasDescribeWeapons_checkBox.CheckedChanged +=
                    new EventHandler(this.extrasDescribeWeapons_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasDescribeWeapons_checkBox);

                addFullRow(layout, helpLabel(
                    "A grafted effect is invisible in game until it fires: the item screen still shows\r\n"
                    + "the old description, so a weapon that seals defence looks like a plain iron sword.\r\n"
                    + "With this on, every weapon whose effect, effectiveness or properties changed gets a\r\n"
                    + "first line naming it - \"Inflicts Seal Defense.\" - with the vanilla flavour text kept\r\n"
                    + "underneath when the weapon's properties did not change (a vanilla line that promised\r\n"
                    + "a property the run removed is dropped, so the text never argues with itself).\r\n"
                    + "It needs msgdata.bin loaded, and the run writes msgdata.bin even when the text\r\n"
                    + "options on the Music and Text tab are off."));

                // ------------------------------------------------- level scaling
                addFullRow(layout, sectionLabel("Level scaling"));

                this.extrasGrowthShift_numericUpDown = new NumericUpDown();
                this.extrasGrowthShift_numericUpDown.Minimum = 0;
                this.extrasGrowthShift_numericUpDown.Maximum = 60;
                this.extrasGrowthShift_numericUpDown.Increment = 5;
                this.extrasGrowthShift_numericUpDown.Value = RandomizerExtras.growthShift;
                this.extrasGrowthShift_numericUpDown.Width = 70;
                this.extrasGrowthShift_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasGrowthShift_numericUpDown.ValueChanged += new EventHandler(this.extrasGrowthShift_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Move Str / Mag / Def / Res growth into HP (%):"), this.extrasGrowthShift_numericUpDown);

                addFullRow(layout, helpLabel(
                    "Enemy stats grow per level from the class enemy growths. This moves the given\r\n"
                    + "percentage of the Str / Mag / Def / Res growths into HP, so level scaling makes\r\n"
                    + "enemies a little tankier instead of hitting much harder. 0% leaves it alone,\r\n"
                    + "20% is a noticeable but fair change."));

                // --------------------------------- more randomization options
                addFullRow(layout, sectionLabel("More randomization options"));

                addFullRow(layout, sectionLabel("Bosses and monsters"));

                this.extrasBossAb_checkBox = new CheckBox();
                this.extrasBossAb_checkBox.AutoSize = true;
                this.extrasBossAb_checkBox.Checked = RandomizerMore.protectBossAbilities;
                this.extrasBossAb_checkBox.Text = "Keep boss and monster class abilities as they are (recommended)";
                this.extrasBossAb_checkBox.CheckedChanged += new EventHandler(this.extrasBossAb_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasBossAb_checkBox);

                this.extrasBossStats_checkBox = new CheckBox();
                this.extrasBossStats_checkBox.AutoSize = true;
                this.extrasBossStats_checkBox.Checked = RandomizerMore.protectBossStats;
                this.extrasBossStats_checkBox.Text = "Keep boss and monster class stats as they are (recommended)";
                this.extrasBossStats_checkBox.CheckedChanged += new EventHandler(this.extrasBossStats_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasBossStats_checkBox);

                this.extrasBossScaling_numericUpDown = new NumericUpDown();
                this.extrasBossScaling_numericUpDown.Minimum = 0;
                this.extrasBossScaling_numericUpDown.Maximum = 100;
                this.extrasBossScaling_numericUpDown.Increment = 5;
                this.extrasBossScaling_numericUpDown.Value = RandomizerMore.bossScalingShare;
                this.extrasBossScaling_numericUpDown.Width = 70;
                this.extrasBossScaling_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasBossScaling_numericUpDown.ValueChanged += new EventHandler(this.extrasBossScaling_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Share of the level scaling bonus bosses get (%):"), this.extrasBossScaling_numericUpDown);

                addFullRow(layout, sectionLabel("Abilities"));

                this.extrasUniqueClassAb_checkBox = new CheckBox();
                this.extrasUniqueClassAb_checkBox.AutoSize = true;
                this.extrasUniqueClassAb_checkBox.Checked = RandomizerMore.uniqueClassAbilities;
                this.extrasUniqueClassAb_checkBox.Text = "Never give the same class the same ability twice";
                this.extrasUniqueClassAb_checkBox.CheckedChanged += new EventHandler(this.extrasUniqueClassAb_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasUniqueClassAb_checkBox);

                this.extrasUniqueTierMastery_checkBox = new CheckBox();
                this.extrasUniqueTierMastery_checkBox.AutoSize = true;
                this.extrasUniqueTierMastery_checkBox.Checked = RandomizerMore.uniqueTierMastery;
                this.extrasUniqueTierMastery_checkBox.Text = "Give every class in a tier a different mastery ability";
                this.extrasUniqueTierMastery_checkBox.CheckedChanged += new EventHandler(this.extrasUniqueTierMastery_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasUniqueTierMastery_checkBox);

                this.extrasUniqueTierMasteryArt_checkBox = new CheckBox();
                this.extrasUniqueTierMasteryArt_checkBox.AutoSize = true;
                this.extrasUniqueTierMasteryArt_checkBox.Checked = RandomizerMore.uniqueTierMasteryArt;
                this.extrasUniqueTierMasteryArt_checkBox.Text = "Give every class in a tier a different mastery combat art";
                this.extrasUniqueTierMasteryArt_checkBox.CheckedChanged += new EventHandler(this.extrasUniqueTierMasteryArt_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasUniqueTierMasteryArt_checkBox);

                this.extrasUniquePersonal_checkBox = new CheckBox();
                this.extrasUniquePersonal_checkBox.AutoSize = true;
                this.extrasUniquePersonal_checkBox.Checked = RandomizerMore.uniquePersonalAbilities;
                this.extrasUniquePersonal_checkBox.Text = "Give every character a different personal ability";
                this.extrasUniquePersonal_checkBox.CheckedChanged += new EventHandler(this.extrasUniquePersonal_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasUniquePersonal_checkBox);

                addFullRow(layout, sectionLabel("Growths and stats"));

                this.extrasEvenGrowthTotals_checkBox = new CheckBox();
                this.extrasEvenGrowthTotals_checkBox.AutoSize = true;
                this.extrasEvenGrowthTotals_checkBox.Checked = RandomizerMore.evenGrowthTotals;
                this.extrasEvenGrowthTotals_checkBox.Text = "Even out the growth totals of every class (keeps each class's shape)";
                this.extrasEvenGrowthTotals_checkBox.CheckedChanged += new EventHandler(this.extrasEvenGrowthTotals_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasEvenGrowthTotals_checkBox);

                this.extrasGrowthTolerance_numericUpDown = new NumericUpDown();
                this.extrasGrowthTolerance_numericUpDown.Minimum = 0;
                this.extrasGrowthTolerance_numericUpDown.Maximum = 90;
                this.extrasGrowthTolerance_numericUpDown.Increment = 5;
                this.extrasGrowthTolerance_numericUpDown.Value = RandomizerMore.growthTotalTolerance;
                this.extrasGrowthTolerance_numericUpDown.Width = 70;
                this.extrasGrowthTolerance_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasGrowthTolerance_numericUpDown.ValueChanged += new EventHandler(this.extrasGrowthTolerance_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Allowed growth difference from the average (%):"), this.extrasGrowthTolerance_numericUpDown);

                this.extrasGrowthIdentity_checkBox = new CheckBox();
                this.extrasGrowthIdentity_checkBox.AutoSize = true;
                this.extrasGrowthIdentity_checkBox.Checked = RandomizerMore.protectGrowthIdentity;
                this.extrasGrowthIdentity_checkBox.Text = "Keep each class's best growth stat as its best stat";
                this.extrasGrowthIdentity_checkBox.CheckedChanged += new EventHandler(this.extrasGrowthIdentity_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasGrowthIdentity_checkBox);

                this.extrasEvenStatTotals_checkBox = new CheckBox();
                this.extrasEvenStatTotals_checkBox.AutoSize = true;
                this.extrasEvenStatTotals_checkBox.Checked = RandomizerMore.evenStatTotals;
                this.extrasEvenStatTotals_checkBox.Text = "Even out the base stat totals of every class (keeps each class's shape)";
                this.extrasEvenStatTotals_checkBox.CheckedChanged += new EventHandler(this.extrasEvenStatTotals_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasEvenStatTotals_checkBox);

                this.extrasStatTotalTolerance_numericUpDown = new NumericUpDown();
                this.extrasStatTotalTolerance_numericUpDown.Minimum = 0;
                this.extrasStatTotalTolerance_numericUpDown.Maximum = 90;
                this.extrasStatTotalTolerance_numericUpDown.Increment = 5;
                this.extrasStatTotalTolerance_numericUpDown.Value = RandomizerMore.statTotalTolerance;
                this.extrasStatTotalTolerance_numericUpDown.Width = 70;
                this.extrasStatTotalTolerance_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasStatTotalTolerance_numericUpDown.ValueChanged += new EventHandler(this.extrasStatTotalTolerance_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Allowed base stat difference from the average (%):"), this.extrasStatTotalTolerance_numericUpDown);

                addFullRow(layout, helpLabel(
                    "Boss and monster classes keep their own abilities (class abilities, latent abilities\r\n"
                    + "and barrier abilities) and stats (stat boosts and base stats), so a boss is still a\r\n"
                    + "boss. The even class variety spread never touches them either, and the Dancer always\r\n"
                    + "keeps Dance.\r\n"
                    + "If level scaling is on, bosses get a share of it as a flat boost to their base stats -\r\n"
                    + "25% by default, so 150% scaling gives them about 12% more base stats. That is far less\r\n"
                    + "than a normal enemy gains from the same scaling, so they stay beatable.\r\n"
                    + "Unique personal abilities stops the same personal skill being handed to several\r\n"
                    + "characters - it only does anything when personal abilities are randomized.\r\n"
                    + "The no repeats option stops one class ending up with the same ability in two slots, and\r\n"
                    + "the tier options do the same for mastery abilities and mastery combat arts inside one\r\n"
                    + "class tier.\r\n"
                    + "Even growth totals scales a class's growths up or down so every class grows about the\r\n"
                    + "same amount per level, while keeping the ratio between its stats - a slow tanky class\r\n"
                    + "stays slow and tanky. The percentage is how far from the average a class may sit before\r\n"
                    + "it is touched. Even base stat totals does the same for the class base stats.\r\n"
                    + "Keeping each class's best growth stat swaps two growths around when the randomization\r\n"
                    + "buried the stat the class is known for, so the classes stay readable.\r\n"
                    + "The range limits for growths, stat boosts and base stats live on their own tabs in the\r\n"
                    + "Classes page, next to the randomization they apply to."));

                // ------------------------------------------- large scale options
                addFullRow(layout, sectionLabel("Large scale randomization"));
                addFullRow(layout, helpLabel(
                    "These break the class lineup apart instead of tidying it up. Each one is a\r\n"
                    + "permutation of the numbers already in the file, so nothing is invented and no\r\n"
                    + "value can leave its range - but the archetypes are gone."));

                this.extrasBigStatSwap_checkBox = new CheckBox();
                this.extrasBigStatSwap_checkBox.AutoSize = true;
                this.extrasBigStatSwap_checkBox.Checked = RandomizerBig.swapClassStatProfiles;
                this.extrasBigStatSwap_checkBox.Text = "Swap class stat profiles between classes (a Fortress Knight can get a Pegasus stat line)";
                this.extrasBigStatSwap_checkBox.CheckedChanged += new EventHandler(this.extrasBigStatSwap_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasBigStatSwap_checkBox);

                this.extrasBigGrowthSwap_checkBox = new CheckBox();
                this.extrasBigGrowthSwap_checkBox.AutoSize = true;
                this.extrasBigGrowthSwap_checkBox.Checked = RandomizerBig.swapClassGrowthProfiles;
                this.extrasBigGrowthSwap_checkBox.Text = "Swap class growth profiles between classes (a mage can grow like a Wyvern Rider)";
                this.extrasBigGrowthSwap_checkBox.CheckedChanged += new EventHandler(this.extrasBigGrowthSwap_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasBigGrowthSwap_checkBox);

                this.extrasBigScope_checkBox = new CheckBox();
                this.extrasBigScope_checkBox.AutoSize = true;
                this.extrasBigScope_checkBox.Checked = RandomizerBig.swapScope == 1;
                this.extrasBigScope_checkBox.Text = "Swap across the whole game instead of inside one tier";
                this.extrasBigScope_checkBox.CheckedChanged += new EventHandler(this.extrasBigScope_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasBigScope_checkBox);

                this.extrasBigCharSheets_checkBox = new CheckBox();
                this.extrasBigCharSheets_checkBox.AutoSize = true;
                this.extrasBigCharSheets_checkBox.Checked = RandomizerBig.shuffleCharacterSheets;
                this.extrasBigCharSheets_checkBox.Text = "Shuffle character stat sheets between characters (who is the fast one changes every run)";
                this.extrasBigCharSheets_checkBox.CheckedChanged += new EventHandler(this.extrasBigCharSheets_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasBigCharSheets_checkBox);

                this.extrasBigBossRemix_checkBox = new CheckBox();
                this.extrasBigBossRemix_checkBox.AutoSize = true;
                this.extrasBigBossRemix_checkBox.Checked = RandomizerBig.bossRemix;
                this.extrasBigBossRemix_checkBox.Text = "Boss remix: every boss gets another boss's ability kit (overrides the boss protection)";
                this.extrasBigBossRemix_checkBox.CheckedChanged += new EventHandler(this.extrasBigBossRemix_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasBigBossRemix_checkBox);

                addFullRow(layout, helpLabel(
                    "Swapping happens inside one class tier by default, so an advanced class still has\r\n"
                    + "advanced numbers - just not its own. Boss remix hands each boss / monster class the\r\n"
                    + "whole ability kit of another one, and takes precedence over the boss protection\r\n"
                    + "options above."));

                addFullRow(layout, sectionLabel("Weapon / art / gambit effect remix"));
                addFullRow(layout, helpLabel(
                    "Weapons, gambits and combat arts all store their effect in the same id space, so their\r\n"
                    + "effects can be moved between them. Every line is its own percentage chance, rolled\r\n"
                    + "once per entry, so you can mix them at whatever rate you like. Only the effects move -\r\n"
                    + "might, hit, crit, weight, range and durability stay exactly as the normal weapon\r\n"
                    + "randomization set them, so this stacks with it instead of replacing it."));

                this.extrasGraftWeapon_numericUpDown = new NumericUpDown();
                this.extrasGraftWeapon_numericUpDown.Minimum = 0;
                this.extrasGraftWeapon_numericUpDown.Maximum = 100;
                this.extrasGraftWeapon_numericUpDown.Increment = 5;
                this.extrasGraftWeapon_numericUpDown.Value = RandomizerBig.weaponGraftChance;
                this.extrasGraftWeapon_numericUpDown.Width = 70;
                this.extrasGraftWeapon_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasGraftWeapon_numericUpDown.ValueChanged += new EventHandler(this.extrasGraft_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Weapons take another weapon's effects (%):"), this.extrasGraftWeapon_numericUpDown);

                this.extrasGraftWeaponFromArt_numericUpDown = new NumericUpDown();
                this.extrasGraftWeaponFromArt_numericUpDown.Minimum = 0;
                this.extrasGraftWeaponFromArt_numericUpDown.Maximum = 100;
                this.extrasGraftWeaponFromArt_numericUpDown.Increment = 5;
                this.extrasGraftWeaponFromArt_numericUpDown.Value = RandomizerBig.weaponFromArtChance;
                this.extrasGraftWeaponFromArt_numericUpDown.Width = 70;
                this.extrasGraftWeaponFromArt_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasGraftWeaponFromArt_numericUpDown.ValueChanged += new EventHandler(this.extrasGraft_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Weapons take a combat art effect (%):"), this.extrasGraftWeaponFromArt_numericUpDown);

                this.extrasGraftWeaponFromGambit_numericUpDown = new NumericUpDown();
                this.extrasGraftWeaponFromGambit_numericUpDown.Minimum = 0;
                this.extrasGraftWeaponFromGambit_numericUpDown.Maximum = 100;
                this.extrasGraftWeaponFromGambit_numericUpDown.Increment = 5;
                this.extrasGraftWeaponFromGambit_numericUpDown.Value = RandomizerBig.weaponFromGambitChance;
                this.extrasGraftWeaponFromGambit_numericUpDown.Width = 70;
                this.extrasGraftWeaponFromGambit_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasGraftWeaponFromGambit_numericUpDown.ValueChanged += new EventHandler(this.extrasGraft_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Weapons take a gambit's effects (%):"), this.extrasGraftWeaponFromGambit_numericUpDown);

                this.extrasGraftArt_numericUpDown = new NumericUpDown();
                this.extrasGraftArt_numericUpDown.Minimum = 0;
                this.extrasGraftArt_numericUpDown.Maximum = 100;
                this.extrasGraftArt_numericUpDown.Increment = 5;
                this.extrasGraftArt_numericUpDown.Value = RandomizerBig.artGraftChance;
                this.extrasGraftArt_numericUpDown.Width = 70;
                this.extrasGraftArt_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasGraftArt_numericUpDown.ValueChanged += new EventHandler(this.extrasGraft_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Combat arts take another art's effect (%):"), this.extrasGraftArt_numericUpDown);

                this.extrasGraftArtFromWeapon_numericUpDown = new NumericUpDown();
                this.extrasGraftArtFromWeapon_numericUpDown.Minimum = 0;
                this.extrasGraftArtFromWeapon_numericUpDown.Maximum = 100;
                this.extrasGraftArtFromWeapon_numericUpDown.Increment = 5;
                this.extrasGraftArtFromWeapon_numericUpDown.Value = RandomizerBig.artFromWeaponChance;
                this.extrasGraftArtFromWeapon_numericUpDown.Width = 70;
                this.extrasGraftArtFromWeapon_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasGraftArtFromWeapon_numericUpDown.ValueChanged += new EventHandler(this.extrasGraft_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Combat arts take a weapon's effect (%):"), this.extrasGraftArtFromWeapon_numericUpDown);

                this.extrasGraftGambit_numericUpDown = new NumericUpDown();
                this.extrasGraftGambit_numericUpDown.Minimum = 0;
                this.extrasGraftGambit_numericUpDown.Maximum = 100;
                this.extrasGraftGambit_numericUpDown.Increment = 5;
                this.extrasGraftGambit_numericUpDown.Value = RandomizerBig.gambitGraftChance;
                this.extrasGraftGambit_numericUpDown.Width = 70;
                this.extrasGraftGambit_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasGraftGambit_numericUpDown.ValueChanged += new EventHandler(this.extrasGraft_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Gambits take another gambit's effects (%):"), this.extrasGraftGambit_numericUpDown);

                this.extrasGraftGambitFromWeapon_numericUpDown = new NumericUpDown();
                this.extrasGraftGambitFromWeapon_numericUpDown.Minimum = 0;
                this.extrasGraftGambitFromWeapon_numericUpDown.Maximum = 100;
                this.extrasGraftGambitFromWeapon_numericUpDown.Increment = 5;
                this.extrasGraftGambitFromWeapon_numericUpDown.Value = RandomizerBig.gambitFromWeaponChance;
                this.extrasGraftGambitFromWeapon_numericUpDown.Width = 70;
                this.extrasGraftGambitFromWeapon_numericUpDown.Margin = new Padding(0, 3, 0, 3);
                this.extrasGraftGambitFromWeapon_numericUpDown.ValueChanged += new EventHandler(this.extrasGraft_numericUpDown_ValueChanged);
                addSplitRow(layout, indentedLabel("Gambits take a weapon's effects (%):"), this.extrasGraftGambitFromWeapon_numericUpDown);

                this.extrasBigWeaponDifferentType_checkBox = new CheckBox();
                this.extrasBigWeaponDifferentType_checkBox.AutoSize = true;
                this.extrasBigWeaponDifferentType_checkBox.Checked = RandomizerBig.weaponDonorsDifferInType;
                this.extrasBigWeaponDifferentType_checkBox.Text = "Weapon donors must be a different weapon type";
                this.extrasBigWeaponDifferentType_checkBox.CheckedChanged += new EventHandler(this.extrasBigWeaponDifferentType_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasBigWeaponDifferentType_checkBox);

                this.extrasBigWeaponUnique_checkBox = new CheckBox();
                this.extrasBigWeaponUnique_checkBox.AutoSize = true;
                this.extrasBigWeaponUnique_checkBox.Checked = RandomizerBig.uniqueWeaponEffects;
                this.extrasBigWeaponUnique_checkBox.Text = "No two weapons share the same effect set";
                this.extrasBigWeaponUnique_checkBox.CheckedChanged += new EventHandler(this.extrasBigWeaponUnique_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasBigWeaponUnique_checkBox);

                addFullRow(layout, helpLabel(
                    "Each entry is rolled at most once, in the order shown, so a weapon takes one donor and\r\n"
                    + "stops. A weapon graft copies the status effect, the effectiveness bits and the property\r\n"
                    + "bits (double attack, magic damage, ignore defence, leave at 1 HP); art and gambit grafts\r\n"
                    + "copy their effect id. Experimental: combat art and gambit effect ids were never meant\r\n"
                    + "to sit on a weapon, and vice versa, so keep a save handy when you raise these."));

                // ------------------------------------------------------- debug
                addFullRow(layout, sectionLabel("Debug report"));

                this.extrasDebugAuto_checkBox = new CheckBox();
                this.extrasDebugAuto_checkBox.AutoSize = true;
                this.extrasDebugAuto_checkBox.Checked = RandomizerDebug.autoWrite;
                this.extrasDebugAuto_checkBox.Text = "Write a debug report automatically after every randomization";
                this.extrasDebugAuto_checkBox.CheckedChanged += new EventHandler(this.extrasDebugAuto_checkBox_CheckedChanged);
                addFullRow(layout, this.extrasDebugAuto_checkBox);

                FlowLayoutPanel debugButtons = new FlowLayoutPanel();
                debugButtons.AutoSize = true;
                debugButtons.WrapContents = true;
                debugButtons.Margin = new Padding(0, 0, 0, 4);

                Button writeDebugButton = new Button();
                writeDebugButton.AutoSize = true;
                writeDebugButton.Text = "Write debug report now";
                writeDebugButton.Click += new EventHandler(this.extrasWriteDebug_button_Click);
                debugButtons.Controls.Add(writeDebugButton);

                Button openDebugButton = new Button();
                openDebugButton.AutoSize = true;
                openDebugButton.Text = "Open debug folder";
                openDebugButton.Click += new EventHandler(this.extrasOpenDebug_button_Click);
                debugButtons.Controls.Add(openDebugButton);

                addFullRow(layout, debugButtons);

                this.extrasDebugPath_label = new Label();
                this.extrasDebugPath_label.AutoSize = true;
                this.extrasDebugPath_label.MaximumSize = new Size(900, 0);
                this.extrasDebugPath_label.ForeColor = Color.DimGray;
                this.extrasDebugPath_label.Margin = new Padding(0, 2, 0, 2);
                this.extrasDebugPath_label.Text = "The report is written next to the output folder as \"FE3H Randomizer Debug.txt\".";
                addFullRow(layout, this.extrasDebugPath_label);

                addFullRow(layout, helpLabel(
                    "The report lists every loaded file, all of these options, and checks the output data:\r\n"
                    + "weapon type spread per tier, blocked abilities that slipped in, out of range weapons on\r\n"
                    + "map units, and the growth totals before and after. It ends with a PASS / WARN / FAIL\r\n"
                    + "summary so it is obvious whether something broke."));

                Label spacer = new Label();
                spacer.AutoSize = false;
                spacer.Height = 24;
                spacer.Text = "";
                addFullRow(layout, spacer);

                scrollPanel.Controls.Add(layout);
                this.extras_tabPage.Controls.Add(scrollPanel);
                this.FETHRand_tabControl.TabPages.Add(this.extras_tabPage);
            }
            catch (Exception exception)
            {
                MessageBox.Show("The extra options tab could not be added:\n\n" + exception.Message,
                    "Extra options", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                if (this.extras_tabPage != null)
                {
                    this.extras_tabPage.ResumeLayout(false);
                    perfPreparePage(this.extras_tabPage);
                }
                this.extrasBuilding = false;
            }
        }

        // ------------------------------------------------------------- handlers
        private void extrasEvenClasses_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasEvenClasses_checkBox == null)
                return;
            RandomizerExtras.evenClassVariety = this.extrasEvenClasses_checkBox.Checked;
        }

        private void extrasSafety_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasSafety_numericUpDown == null)
                return;
            RandomizerExtras.classVarietySafety = (int)this.extrasSafety_numericUpDown.Value;
        }

        private void extrasSiege_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasSiege_checkBox == null)
                return;
            RandomizerExtras.limitSiegeTomes = this.extrasSiege_checkBox.Checked;
            RandomizerExtras.notifyBlocklistChanged();
        }

        private void extrasMaxRange_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasMaxRange_numericUpDown == null)
                return;
            RandomizerExtras.maxEnemyWeaponRange = (int)this.extrasMaxRange_numericUpDown.Value;
            RandomizerExtras.notifyBlocklistChanged();
        }

        private void extrasSiegeChance_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasSiegeChance_numericUpDown == null)
                return;
            RandomizerExtras.siegeChance = (int)this.extrasSiegeChance_numericUpDown.Value;
        }

        private void extrasClassIdentity_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasClassIdentity_checkBox == null)
                return;
            RandomizerExtras.classIdentity = this.extrasClassIdentity_checkBox.Checked;
            if (this.extrasClassDefenseLimit_numericUpDown != null)
                this.extrasClassDefenseLimit_numericUpDown.Enabled = this.extrasClassIdentity_checkBox.Checked;
        }

        private void extrasClassDefenseLimit_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasClassDefenseLimit_numericUpDown == null)
                return;
            RandomizerExtras.classDefenseLimit = (int)this.extrasClassDefenseLimit_numericUpDown.Value;
        }

        private void extrasDescribeWeapons_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasDescribeWeapons_checkBox == null)
                return;
            RandomizerBig.describeWeaponEffects = this.extrasDescribeWeapons_checkBox.Checked;
        }

        private void extrasFlatTrim_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding)
                return;
            if (this.extrasFlatHp_numericUpDown != null)
                RandomizerExtras.flatHpTrim = (int)this.extrasFlatHp_numericUpDown.Value;
            if (this.extrasFlatDef_numericUpDown != null)
                RandomizerExtras.flatDefTrim = (int)this.extrasFlatDef_numericUpDown.Value;
            if (this.extrasFlatRes_numericUpDown != null)
                RandomizerExtras.flatResTrim = (int)this.extrasFlatRes_numericUpDown.Value;
        }

        private void extrasGrowthShift_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasGrowthShift_numericUpDown == null)
                return;
            RandomizerExtras.growthShift = (int)this.extrasGrowthShift_numericUpDown.Value;
        }

        private void extrasBossAb_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBossAb_checkBox == null)
                return;
            RandomizerMore.protectBossAbilities = this.extrasBossAb_checkBox.Checked;
        }

        private void extrasUniqueClassAb_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasUniqueClassAb_checkBox == null)
                return;
            RandomizerMore.uniqueClassAbilities = this.extrasUniqueClassAb_checkBox.Checked;
        }

        private void extrasUniquePersonal_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasUniquePersonal_checkBox == null)
                return;
            RandomizerMore.uniquePersonalAbilities = this.extrasUniquePersonal_checkBox.Checked;
        }

        private void extrasBossStats_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBossStats_checkBox == null)
                return;
            RandomizerMore.protectBossStats = this.extrasBossStats_checkBox.Checked;
        }

        private void extrasUniqueTierMastery_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasUniqueTierMastery_checkBox == null)
                return;
            RandomizerMore.uniqueTierMastery = this.extrasUniqueTierMastery_checkBox.Checked;
        }

        private void extrasEvenGrowthTotals_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasEvenGrowthTotals_checkBox == null)
                return;
            RandomizerMore.evenGrowthTotals = this.extrasEvenGrowthTotals_checkBox.Checked;
            if (this.extrasGrowthTolerance_numericUpDown != null)
                this.extrasGrowthTolerance_numericUpDown.Enabled = this.extrasEvenGrowthTotals_checkBox.Checked;
        }

        private void extrasBossScaling_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBossScaling_numericUpDown == null)
                return;
            RandomizerMore.bossScalingShare = (int)this.extrasBossScaling_numericUpDown.Value;
        }

        private void extrasUniqueTierMasteryArt_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasUniqueTierMasteryArt_checkBox == null)
                return;
            RandomizerMore.uniqueTierMasteryArt = this.extrasUniqueTierMasteryArt_checkBox.Checked;
        }

        private void extrasGrowthIdentity_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasGrowthIdentity_checkBox == null)
                return;
            RandomizerMore.protectGrowthIdentity = this.extrasGrowthIdentity_checkBox.Checked;
        }

        private void extrasEvenStatTotals_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasEvenStatTotals_checkBox == null)
                return;
            RandomizerMore.evenStatTotals = this.extrasEvenStatTotals_checkBox.Checked;
            if (this.extrasStatTotalTolerance_numericUpDown != null)
                this.extrasStatTotalTolerance_numericUpDown.Enabled = this.extrasEvenStatTotals_checkBox.Checked;
        }

        private void extrasStatTotalTolerance_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasStatTotalTolerance_numericUpDown == null)
                return;
            RandomizerMore.statTotalTolerance = (int)this.extrasStatTotalTolerance_numericUpDown.Value;
        }

        private void extrasGrowthTolerance_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasGrowthTolerance_numericUpDown == null)
                return;
            RandomizerMore.growthTotalTolerance = (int)this.extrasGrowthTolerance_numericUpDown.Value;
        }

        private void extrasAbilities_checkedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (this.extrasBuilding)
                return;
            // The checked state changes after this event, so sync on the next message.
            this.BeginInvoke((MethodInvoker)delegate
            {
                this.syncBlockedAbilities();
            });
        }

        private void extrasBigStatSwap_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBigStatSwap_checkBox == null)
                return;
            RandomizerBig.swapClassStatProfiles = this.extrasBigStatSwap_checkBox.Checked;
        }

        private void extrasBigGrowthSwap_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBigGrowthSwap_checkBox == null)
                return;
            RandomizerBig.swapClassGrowthProfiles = this.extrasBigGrowthSwap_checkBox.Checked;
        }

        private void extrasBigScope_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBigScope_checkBox == null)
                return;
            RandomizerBig.swapScope = this.extrasBigScope_checkBox.Checked ? 1 : 0;
        }

        private void extrasBigCharSheets_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBigCharSheets_checkBox == null)
                return;
            RandomizerBig.shuffleCharacterSheets = this.extrasBigCharSheets_checkBox.Checked;
        }

        private void extrasBigBossRemix_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBigBossRemix_checkBox == null)
                return;
            RandomizerBig.bossRemix = this.extrasBigBossRemix_checkBox.Checked;
        }

        private void extrasGraft_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding)
                return;
            if (this.extrasGraftWeapon_numericUpDown != null)
                RandomizerBig.weaponGraftChance = (int)this.extrasGraftWeapon_numericUpDown.Value;
            if (this.extrasGraftWeaponFromArt_numericUpDown != null)
                RandomizerBig.weaponFromArtChance = (int)this.extrasGraftWeaponFromArt_numericUpDown.Value;
            if (this.extrasGraftWeaponFromGambit_numericUpDown != null)
                RandomizerBig.weaponFromGambitChance = (int)this.extrasGraftWeaponFromGambit_numericUpDown.Value;
            if (this.extrasGraftArt_numericUpDown != null)
                RandomizerBig.artGraftChance = (int)this.extrasGraftArt_numericUpDown.Value;
            if (this.extrasGraftArtFromWeapon_numericUpDown != null)
                RandomizerBig.artFromWeaponChance = (int)this.extrasGraftArtFromWeapon_numericUpDown.Value;
            if (this.extrasGraftGambit_numericUpDown != null)
                RandomizerBig.gambitGraftChance = (int)this.extrasGraftGambit_numericUpDown.Value;
            if (this.extrasGraftGambitFromWeapon_numericUpDown != null)
                RandomizerBig.gambitFromWeaponChance = (int)this.extrasGraftGambitFromWeapon_numericUpDown.Value;
        }

        private void extrasBigWeaponDifferentType_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBigWeaponDifferentType_checkBox == null)
                return;
            RandomizerBig.weaponDonorsDifferInType = this.extrasBigWeaponDifferentType_checkBox.Checked;
        }

        private void extrasBigWeaponUnique_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasBigWeaponUnique_checkBox == null)
                return;
            RandomizerBig.uniqueWeaponEffects = this.extrasBigWeaponUnique_checkBox.Checked;
        }

        // ------------------------------------------- total conversion mod mode
        private void extrasModMode_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasModMode_checkBox == null)
                return;
            ModSupport.modMode = this.extrasModMode_checkBox.Checked;
            if (this.extrasModMastery_checkBox != null)
                this.extrasModMastery_checkBox.Enabled = ModSupport.modMode;
            if (this.extrasModNoMonastery_checkBox != null)
            {
                this.extrasModNoMonastery_checkBox.Enabled = ModSupport.modMode;
                // The mode only means something with mod mode on, so it goes
                // with it rather than staying on behind a disabled box.
                if (!ModSupport.modMode && this.extrasModNoMonastery_checkBox.Checked)
                    this.extrasModNoMonastery_checkBox.Checked = false;
            }
        }

        private void extrasModNoMonastery_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasModNoMonastery_checkBox == null)
                return;
            ModSupport.noMonasteryMode = this.extrasModNoMonastery_checkBox.Checked;
            if (!ModSupport.noMonasteryMode)
                return;

            // The mod's own values are what the game reads, so mod mode goes with
            // it. Turning that on here also writes the conflicting options off.
            ModSupport.modMode = true;
            if (this.extrasModMode_checkBox != null)
                this.extrasModMode_checkBox.Checked = true;
            if (this.extrasModMastery_checkBox != null)
                this.extrasModMastery_checkBox.Enabled = true;

            // No Monastery starts every unit in an intermediate class, so the
            // rank D spell is there at level 1; C is the first rank that has to
            // be earned in play.
            if (settings != null)
            {
                settings.spellFirstRank = 4;
                updateAllDisplay();
            }
        }

        private void extrasModMastery_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasModMastery_checkBox == null)
                return;
            ModSupport.keepMasteryExp = this.extrasModMastery_checkBox.Checked;
        }

        private static string modKindText()
        {
            if (ModSupport.modKind == ModSupport.kindNone)
                return "Mod checked: none yet - point the box above at the mod's own folder and press "
                    + "\"Check the mod\" (or just apply a preset, which says which mod it is for)";
            return "Mod checked: " + ModSupport.modKind
                + (ModSupport.modKindDetail == null || ModSupport.modKindDetail.Length == 0
                    ? "" : "   (" + ModSupport.modKindDetail + ")")
                + (ModSupport.shipsNoDataFiles()
                    ? "\r\n    no game data files, so the game's own fixed_*.bin are the right input and"
                        + " nothing has to move"
                    : "\r\n    ships its own data files, so the table offsets are lined up from its header");
        }

        private void extrasModFolder_textBox_TextChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasModFolder_textBox == null)
                return;
            ModSupport.modFolder = this.extrasModFolder_textBox.Text.Trim();
        }

        private void extrasModFolderBrowse_button_Click(object sender, EventArgs e)
        {
            // Same dialog as the file buttons on the other tabs, and it starts at
            // the mod folder already set, or at the folder the last scan read, so
            // it never opens in the exe's build folder.
            string folder = this.batchPickFolder(
                "Select the mod's own folder (the unzipped mod, or contents\\<title id>)",
                ModSupport.modFolder, ModSupport.lastScanFolder);
            if (folder == null)
                return;
            if (this.extrasModFolder_textBox != null)
                this.extrasModFolder_textBox.Text = folder;
            ModSupport.modFolder = folder;
            this.extrasModCheck(folder, true);
        }

        private void extrasModCheck_button_Click(object sender, EventArgs e)
        {
            string folder = this.extrasModFolder_textBox == null
                ? ModSupport.modFolder : this.extrasModFolder_textBox.Text.Trim();
            if (folder.Length == 0)
            {
                MessageBox.Show("Point the mod folder box at the unzipped mod, or at the folder the mod\r\n"
                    + "lives in on the SD card (contents\\<title id>), then press Check the mod.",
                    "Check the mod", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            this.extrasModCheck(folder, true);
        }

        // Works out which release the folder holds and says what that means for
        // the run. No assumptions: it only reports what it found on disk.
        private void extrasModCheck(string folder, bool showResult)
        {
            if (!System.IO.Directory.Exists(folder))
            {
                if (showResult)
                    MessageBox.Show("That folder does not exist:\r\n\r\n  " + folder,
                        "Check the mod", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            bool found = ModSupport.checkModFolder(folder);
            if (found && this.extrasModMode_checkBox != null)
                this.extrasModMode_checkBox.Checked = true;
            ModSupport.readOffsets();
            ModSupport.applyOffsets();
            if (this.extrasModOffsets_label != null)
                this.extrasModOffsets_label.Text = "Offsets: " + ModSupport.offsetsStatus;
            if (this.extrasModKind_label != null)
                this.extrasModKind_label.Text = modKindText();

            if (!showResult)
                return;

            StringBuilder text = new StringBuilder();
            if (!found)
            {
                text.AppendLine("Nothing in that folder looks like a No Monastery release:");
                text.AppendLine();
                text.AppendLine("  " + folder);
                text.AppendLine();
                text.AppendLine("A data-file release has fixed_*.bin files of its own in it (data\\ or");
                text.AppendLine("patch4\\nx\\data). A Skyline release has romfs\\skyline\\plugins\\");
                text.AppendLine("libno_monastery.nro and an exefs folder.");
                if (ModSupport.modKindDetail != null && ModSupport.modKindDetail.Length > 0
                    && ModSupport.modKindDetail.StartsWith("found ", StringComparison.Ordinal))
                {
                    text.AppendLine();
                    text.AppendLine(ModSupport.modKindDetail);
                }
                MessageBox.Show(text.ToString(), "Check the mod", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }

            if (ModSupport.shipsNoDataFiles())
            {
                text.AppendLine("Found: " + ModSupport.modKind);
                text.AppendLine("       " + ModSupport.modKindDetail);
                text.AppendLine();
                text.AppendLine("This release is a plugin, not a set of data files: it ships no fixed_*.bin");
                text.AppendLine("at all, so it cannot have moved a table. The game's own files stay the");
                text.AppendLine("input, the compiled-in offsets are already correct, and this build will not");
                text.AppendLine("ask about originals or missing offsets any more.");
                text.AppendLine();
                text.AppendLine("Conflicts are just the " + ModSupport.conflictCount().ToString()
                    + " options the plugin recalculates (level scaling and class");
                text.AppendLine("abilities). \"" + ModSupport.safePresetName() + "\" turns those off and leaves");
                text.AppendLine("everything else at full strength.");
                text.AppendLine();
                text.AppendLine("Where to write the output: the same romfs tree the game reads its data from");
                text.AppendLine("(patch4\\nx\\data, patch3\\nx\\data, patch4\\common\\common ...). Writing it");
                text.AppendLine("into the mod's own romfs folder works too - it is layered over the game's -");
                text.AppendLine("but keep the skyline folder out of the way, it is not yours to rewrite.");
            }
            else
            {
                text.AppendLine("Found: " + ModSupport.modKind);
                text.AppendLine("       " + ModSupport.modKindDetail);
                text.AppendLine();
                text.AppendLine("This release replaces the game's data files with its own, so rows were added");
                text.AppendLine("and the tables after them moved. Load its copies (not the game dump's) and the");
                text.AppendLine("offsets are read from its own header. \"" + ModSupport.safePresetName()
                    + "\" handles the");
                text.AppendLine("options it owns.");
            }
            MessageBox.Show(text.ToString(), "Check the mod", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void extrasModOriginals_textBox_TextChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasModOriginals_textBox == null)
                return;
            ModSupport.originalFolder = this.extrasModOriginals_textBox.Text.Trim();
        }

        private void extrasModOriginalsBrowse_button_Click(object sender, EventArgs e)
        {
            string folder = this.batchPickFolder(
                "Select the folder with the original (unmodified) fixed_*.bin files",
                ModSupport.originalFolder, ModSupport.modFolder);
            if (folder == null)
                return;
            if (this.extrasModOriginals_textBox != null)
                this.extrasModOriginals_textBox.Text = folder;
            ModSupport.originalFolder = folder;
        }

        private void extrasModReadOffsets_button_Click(object sender, EventArgs e)
        {
            if (this.extrasModOriginals_textBox != null)
                ModSupport.originalFolder = this.extrasModOriginals_textBox.Text.Trim();

            bool haveFolder = ModSupport.originalFolder != null
                && ModSupport.originalFolder.Length > 0;

            bool ok = ModSupport.readOffsets();
            ModSupport.applyOffsets();
            if (this.extrasModOffsets_label != null)
                this.extrasModOffsets_label.Text = "Offsets: " + ModSupport.offsetsStatus;

            StringBuilder text = new StringBuilder();
            text.AppendLine(ModSupport.offsetsStatus);
            text.AppendLine();
            text.AppendLine(haveFolder
                ? "Comparing the loaded files with the originals in that folder:"
                : "No originals folder set, so this is the built-in profile (plus the loaded headers):");
            text.AppendLine(ModSupport.offsetSummary());
            text.AppendLine();
            if (!ok)
                text.AppendLine("Nothing to map yet: load the mod's files first, and for a mod without a\r\n"
                    + "built-in profile also pick the folder with the original fixed_*.bin files\r\n"
                    + "(they come from the game dump, patch4\\nx\\data, not from the mod).");
            else if (!ModSupport.modMode)
                text.AppendLine("Mod mode is off, so nothing was moved - these are only comparisons.");
            else
                text.AppendLine("Mod mode is on: the run will write to the offsets above.");
            MessageBox.Show(text.ToString(), "Table offsets", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Exclamation);
        }

        private void extrasModAnalyse_button_Click(object sender, EventArgs e)
        {
            System.Collections.Generic.List<string> lines = ModSupport.analyse();
            if (lines == null || lines.Count == 0)
            {
                MessageBox.Show("No files are loaded yet. Use \"Scan folder and load all files\" first.",
                    "Mod analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
                text.AppendLine(lines[i]);
            MessageBox.Show(text.ToString(), "Mod analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void extrasModConflicts_button_Click(object sender, EventArgs e)
        {
            System.Collections.Generic.List<string> conflicts = ModSupport.enabledConflicts(this.settings);
            System.Collections.Generic.List<string> notes =
                ModSupport.notesFor(ModSupport.modKind, this.settings);
            if (conflicts.Count == 0 && notes.Count == 0)
            {
                MessageBox.Show("No enabled option fights a total conversion mod.",
                    "Mod conflicts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            StringBuilder text = new StringBuilder();
            if (ModSupport.modKind != ModSupport.kindNone)
            {
                text.AppendLine("Mod checked: " + ModSupport.modKind);
                text.AppendLine("    so the list below is the " + ModSupport.conflictCount().ToString()
                    + " option(s) this release owns");
                text.AppendLine();
            }
            if (conflicts.Count > 0)
            {
                text.AppendLine("These options are on and the mod owns the data they change:");
                text.AppendLine();
                for (int i = 0; i < conflicts.Count; i++)
                    text.AppendLine("  " + conflicts[i]);
                text.AppendLine();
                text.AppendLine("\"Use the mod-safe preset\" turns exactly these off and leaves the rest.");
            }
            if (notes.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("These still work, but the mod recalculates the result:");
                for (int i = 0; i < notes.Count; i++)
                    text.AppendLine("  " + notes[i]);
            }
            MessageBox.Show(text.ToString(), "Mod conflicts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void extrasModSafePreset_button_Click(object sender, EventArgs e)
        {
            RandomizerPresets.Preset preset = null;
            System.Collections.Generic.List<RandomizerPresets.Preset> presets = RandomizerPresets.all();
            string wanted = ModSupport.safePresetName();
            for (int i = 0; i < presets.Count; i++)
                if (presets[i].BuiltIn && preset == null
                    && string.Equals(presets[i].Name, wanted, StringComparison.Ordinal))
                    preset = presets[i];
            for (int i = 0; i < presets.Count; i++)
                if (presets[i].BuiltIn && preset == null
                    && presets[i].Name.StartsWith("No Monastery", StringComparison.Ordinal))
                    preset = presets[i];
            if (preset == null)
            {
                MessageBox.Show("The mod-safe preset is missing from this build.",
                    "Mod safe preset", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            int applied;
            int skipped = RandomizerPresets.apply(this.settings, preset, out applied);
            RandomizerPresets.lastApplied = preset.Name;
            this.quickSyncExtraControls();
            this.syncExtraDetailControls();
            try
            {
                this.updateAllDisplay();
            }
            catch
            {
            }
            if (this.extrasModMode_checkBox != null)
                this.extrasModMode_checkBox.Checked = ModSupport.modMode;
            if (this.extrasModMastery_checkBox != null)
                this.extrasModMastery_checkBox.Checked = ModSupport.keepMasteryExp;
            if (this.extrasModNoMonastery_checkBox != null)
                this.extrasModNoMonastery_checkBox.Checked = ModSupport.noMonasteryMode;

            MessageBox.Show("Applied \"" + preset.Name + "\".\r\n\r\n"
                + applied.ToString() + " setting(s) set"
                + (skipped > 0 ? ", " + skipped.ToString() + " line(s) skipped" : "")
                + ".\r\n\r\nEverything the mod owns is left to the mod: for a data-file release that is its\r\n"
                + "dialogue, shops, monastery options, mastery EXP and level curve; for the 2.1\r\n"
                + "plugin it is its level scaling and the class abilities it hands out by skill rank.\r\n"
                + "The rest of the randomization (classes, characters, weapons, effects) still runs\r\n"
                + "at full strength.",
                "Mod safe preset", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ------------------------------------------------------------ presets
        private void refreshPresetList()
        {
            if (this.extrasPreset_comboBox == null)
                return;
            string keep = null;
            if (this.extrasPreset_comboBox.SelectedItem != null)
                keep = ((RandomizerPresets.Preset)this.extrasPreset_comboBox.SelectedItem).Name;

            this.extrasPreset_comboBox.BeginUpdate();
            this.extrasPreset_comboBox.Items.Clear();
            System.Collections.Generic.List<RandomizerPresets.Preset> presets = RandomizerPresets.all();
            for (int i = 0; i < presets.Count; i++)
                this.extrasPreset_comboBox.Items.Add(presets[i]);
            this.extrasPreset_comboBox.EndUpdate();

            if (this.extrasPreset_comboBox.Items.Count == 0)
                return;
            int selected = 0;
            if (keep != null)
                for (int i = 0; i < this.extrasPreset_comboBox.Items.Count; i++)
                {
                    RandomizerPresets.Preset preset =
                        (RandomizerPresets.Preset)this.extrasPreset_comboBox.Items[i];
                    if (string.Equals(preset.Name, keep, StringComparison.Ordinal))
                    {
                        selected = i;
                        break;
                    }
                }
            this.extrasPreset_comboBox.SelectedIndex = selected;
        }

        private RandomizerPresets.Preset selectedPreset()
        {
            if (this.extrasPreset_comboBox == null)
                return null;
            return this.extrasPreset_comboBox.SelectedItem as RandomizerPresets.Preset;
        }

        private void extrasPresetApply_button_Click(object sender, EventArgs e)
        {
            RandomizerPresets.Preset preset = this.selectedPreset();
            if (preset == null)
            {
                MessageBox.Show("Pick a preset first.", "Presets",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int applied;
            int skipped = RandomizerPresets.apply(this.settings, preset, out applied);
            this.quickSyncExtraControls();
            this.syncExtraDetailControls();
            try
            {
                this.updateAllDisplay();
            }
            catch
            {
            }

            RandomizerPresets.lastApplied = preset.Name;

            string message = "Applied \"" + preset.Name + "\".\r\n\r\n"
                + applied.ToString() + " setting(s) set.";
            if (skipped > 0)
                message += "\r\n" + skipped.ToString()
                    + " line(s) in the preset could not be used (unknown name or bad value).";
            message += "\r\n\r\nEverything not mentioned by the preset is back at its default.";
            if (this.extrasPresetInfo_label != null)
                this.extrasPresetInfo_label.Text = "Applied preset: " + RandomizerPresets.describe(preset);
            MessageBox.Show(message, "Presets", MessageBoxButtons.OK,
                skipped > 0 ? MessageBoxIcon.Exclamation : MessageBoxIcon.Information);
        }

        private void extrasPresetSave_button_Click(object sender, EventArgs e)
        {
            string name = this.promptForText("Save preset",
                "Name for this preset:\r\n\r\nThe whole current configuration is stored, including the options on this tab.",
                "My settings");
            if (name == null)
                return;

            try
            {
                string path = RandomizerPresets.save(this.settings, name);
                this.refreshPresetList();
                if (this.extrasPresetInfo_label != null)
                    this.extrasPresetInfo_label.Text = "Saved preset: " + path;
                MessageBox.Show("Preset saved to:\r\n\r\n" + path
                    + "\r\n\r\nIt is a plain text file, so it can be edited or shared.",
                    "Presets", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show("The preset could not be saved:\r\n\r\n" + exception.Message,
                    "Presets", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void extrasPresetDelete_button_Click(object sender, EventArgs e)
        {
            RandomizerPresets.Preset preset = this.selectedPreset();
            if (preset == null)
                return;
            if (preset.BuiltIn)
            {
                MessageBox.Show("That preset is built in and cannot be deleted.",
                    "Presets", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete the preset \"" + preset.Name + "\"?\r\n\r\n" + preset.Path,
                    "Presets", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            if (RandomizerPresets.delete(preset))
            {
                this.refreshPresetList();
                if (this.extrasPresetInfo_label != null)
                    this.extrasPresetInfo_label.Text = "Preset deleted.";
            }
        }

        private void extrasPresetOpen_button_Click(object sender, EventArgs e)
        {
            try
            {
                string folder = RandomizerPresets.presetFolder();
                System.IO.Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + folder + "\"");
            }
            catch (Exception exception)
            {
                MessageBox.Show("The preset folder could not be opened:\r\n\r\n" + exception.Message,
                    "Presets", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // Small input box - WinForms has no built in one.
        private string promptForText(string title, string message, string initial)
        {
            Form dialog = new Form();
            dialog.Text = title;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.MinimizeBox = false;
            dialog.MaximizeBox = false;
            dialog.ClientSize = new Size(430, 150);

            Label label = new Label();
            label.AutoSize = true;
            label.MaximumSize = new Size(410, 0);
            label.Location = new Point(12, 12);
            label.Text = message;
            dialog.Controls.Add(label);

            TextBox box = new TextBox();
            box.Location = new Point(12, 78);
            box.Width = 406;
            box.Text = initial == null ? "" : initial;
            dialog.Controls.Add(box);

            Button ok = new Button();
            ok.Text = "OK";
            ok.DialogResult = DialogResult.OK;
            ok.Location = new Point(262, 112);
            ok.AutoSize = true;
            dialog.Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Location = new Point(348, 112);
            cancel.AutoSize = true;
            dialog.Controls.Add(cancel);

            dialog.AcceptButton = ok;
            dialog.CancelButton = cancel;

            string result = dialog.ShowDialog(this) == DialogResult.OK ? box.Text.Trim() : null;
            dialog.Dispose();
            if (result != null && result.Length == 0)
                return null;
            return result;
        }

        private void extrasDebugAuto_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.extrasBuilding || this.extrasDebugAuto_checkBox == null)
                return;
            RandomizerDebug.autoWrite = this.extrasDebugAuto_checkBox.Checked;
        }

        private void extrasWriteDebug_button_Click(object sender, EventArgs e)
        {
            string folder = this.batchOutputFolder;
            if (folder == null || folder.Length == 0)
                folder = Application.StartupPath;
            string path = RandomizerDebug.WriteReport(folder, "written from the Extra options tab");
            if (path == null)
            {
                MessageBox.Show("The debug report could not be written:\n\n" + RandomizerDebug.lastSummary,
                    "Debug report", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (this.extrasDebugPath_label != null)
                this.extrasDebugPath_label.Text = "Last report: " + path + "  (" + RandomizerDebug.lastSummary + ")";
            MessageBox.Show("Debug report written to:\n\n" + path + "\n\n" + RandomizerDebug.lastSummary,
                "Debug report", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void extrasOpenDebug_button_Click(object sender, EventArgs e)
        {
            string folder = this.batchOutputFolder;
            if (folder == null || folder.Length == 0)
                folder = Application.StartupPath;
            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + folder + "\"");
            }
            catch (Exception exception)
            {
                MessageBox.Show("The folder could not be opened:\n\n" + exception.Message,
                    "Debug report", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void syncBlockedAbilities()
        {
            if (this.extrasAbilities_checkedListBox == null)
                return;
            RandomizerExtras.blockedAbilities.Clear();
            for (int i = 0; i < this.extrasAbilities_checkedListBox.Items.Count; i++)
                if (this.extrasAbilities_checkedListBox.GetItemChecked(i))
                    RandomizerExtras.blockedAbilities.Add(i);
            RandomizerExtras.notifyBlocklistChanged();
        }

        private void extrasDragonOnly_button_Click(object sender, EventArgs e)
        {
            this.setBlockedAbilities(RandomizerExtras.defaultBlockedAbilities);
        }

        private void extrasTickNothing_button_Click(object sender, EventArgs e)
        {
            this.setBlockedAbilities(new int[0]);
        }

        private void extrasVanillaBad_button_Click(object sender, EventArgs e)
        {
            int[] defaults = RandomizerExtras.defaultBlockedAbilities;
            int[] alreadyAvoided = Randomizer.badAbilities;
            int[] combined = new int[defaults.Length + alreadyAvoided.Length];
            for (int i = 0; i < defaults.Length; i++)
                combined[i] = defaults[i];
            for (int i = 0; i < alreadyAvoided.Length; i++)
                combined[defaults.Length + i] = alreadyAvoided[i];
            this.setBlockedAbilities(combined);
        }

        private void setBlockedAbilities(int[] abilities)
        {
            if (this.extrasAbilities_checkedListBox == null)
                return;
            this.extrasBuilding = true;
            int count = this.extrasAbilities_checkedListBox.Items.Count;
            for (int i = 0; i < count; i++)
                this.extrasAbilities_checkedListBox.SetItemChecked(i, false);
            for (int i = 0; i < abilities.Length; i++)
                if (abilities[i] >= 0 && abilities[i] < count)
                    this.extrasAbilities_checkedListBox.SetItemChecked(abilities[i], true);
            this.extrasBuilding = false;
            this.syncBlockedAbilities();
        }
    }
}
