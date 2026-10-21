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
                    + "Divine Dragon Horn, Giant Wings and the two Effect Null skills) are ticked by default."));

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
