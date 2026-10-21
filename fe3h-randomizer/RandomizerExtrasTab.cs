// -----------------------------------------------------------------------------
//  RandomizerExtrasTab.cs
//
//  The "Extra options" tab: class variety, ability blocklist, siege tome limit
//  and the level scaling growth shift. All values are pushed straight into
//  RandomizerExtras, which the randomization code consults while it runs.
// -----------------------------------------------------------------------------

using System;
using System.Drawing;
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
            this.extrasBuilding = false;
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
