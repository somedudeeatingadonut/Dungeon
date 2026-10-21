// -----------------------------------------------------------------------------
//  MoreOptionsTab.cs
//
//  Puts the range limits where the thing they limit already lives, so they are
//  found next to the randomization they apply to:
//
//    Classes > Stat Boosts   : class stat boosts only inside this range
//    Classes > Growth Boosts : class and character growths only inside this range
//    Classes > Stat Bases    : class base stats only inside this range
//
//  Each one is a single row docked to the bottom of the page, so it does not
//  move any of the controls the original tool put there.
// -----------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    public partial class RandomizerForm
    {
        private sealed class rangeClampPanel
        {
            public CheckBox check;
            public NumericUpDown low;
            public NumericUpDown high;
            public Label note;
        }

        private rangeClampPanel growthClampPanel;
        private rangeClampPanel statBoostClampPanel;
        private rangeClampPanel statBaseClampPanel;

        private void installMoreOptionsOnTabs()
        {
            try
            {
                this.growthClampPanel = installRangeClamp(this.tabPage2,
                    "Keep randomized growths inside this range",
                    RandomizerMore.clampGrowths,
                    RandomizerMore.growthClampMin, RandomizerMore.growthClampMax,
                    0, 255, 5,
                    "only growths the randomization changed are moved - a stat with no growth stays at 0",
                    new EventHandler(this.growthClamp_changed));

                this.statBoostClampPanel = installRangeClamp(this.tabPage1,
                    "Keep randomized class stat boosts inside this range",
                    RandomizerMore.clampStatBoosts,
                    RandomizerMore.statBoostClampMin, RandomizerMore.statBoostClampMax,
                    -20, 30, 1,
                    "negative values are allowed, e.g. -3 to 10",
                    new EventHandler(this.statBoostClamp_changed));

                this.statBaseClampPanel = installRangeClamp(this.tabPage4,
                    "Keep randomized base stats inside this range",
                    RandomizerMore.clampStatBases,
                    RandomizerMore.statBaseClampMin, RandomizerMore.statBaseClampMax,
                    0, 99, 1,
                    "only base stats the randomization changed are moved",
                    new EventHandler(this.statBaseClamp_changed));
            }
            catch
            {
                // A missing page must never stop the rest of the tool from loading.
            }
        }

        private rangeClampPanel installRangeClamp(TabPage page, string text, bool enabled,
            int lowValue, int highValue, int minimum, int maximum, int increment,
            string note, EventHandler changed)
        {
            if (page == null)
                return null;

            rangeClampPanel holder = new rangeClampPanel();

            Panel panel = new Panel();
            panel.Dock = DockStyle.Bottom;
            panel.Height = 38;
            panel.Padding = new Padding(6, 2, 6, 2);

            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.WrapContents = false;
            flow.AutoScroll = false;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.Padding = new Padding(0, 4, 0, 0);

            holder.check = new CheckBox();
            holder.check.AutoSize = true;
            holder.check.Checked = enabled;
            holder.check.Text = text;
            holder.check.Margin = new Padding(0, 3, 12, 0);
            holder.check.CheckedChanged += changed;
            flow.Controls.Add(holder.check);

            Label lowLabel = new Label();
            lowLabel.AutoSize = true;
            lowLabel.Text = "Lowest:";
            lowLabel.Margin = new Padding(0, 6, 4, 0);
            flow.Controls.Add(lowLabel);

            holder.low = new NumericUpDown();
            holder.low.Minimum = minimum;
            holder.low.Maximum = maximum;
            holder.low.Increment = increment;
            holder.low.Value = Math.Max(minimum, Math.Min(maximum, lowValue));
            holder.low.Width = 62;
            holder.low.Margin = new Padding(0, 2, 10, 0);
            holder.low.Enabled = enabled;
            holder.low.ValueChanged += changed;
            flow.Controls.Add(holder.low);

            Label highLabel = new Label();
            highLabel.AutoSize = true;
            highLabel.Text = "Highest:";
            highLabel.Margin = new Padding(0, 6, 4, 0);
            flow.Controls.Add(highLabel);

            holder.high = new NumericUpDown();
            holder.high.Minimum = minimum;
            holder.high.Maximum = maximum;
            holder.high.Increment = increment;
            holder.high.Value = Math.Max(minimum, Math.Min(maximum, highValue));
            holder.high.Width = 62;
            holder.high.Margin = new Padding(0, 2, 12, 0);
            holder.high.Enabled = enabled;
            holder.high.ValueChanged += changed;
            flow.Controls.Add(holder.high);

            holder.note = new Label();
            holder.note.AutoSize = true;
            holder.note.ForeColor = Color.DimGray;
            holder.note.Text = note;
            holder.note.Margin = new Padding(0, 6, 0, 0);
            flow.Controls.Add(holder.note);

            panel.Controls.Add(flow);
            page.Controls.Add(panel);
            return holder;
        }

        // ------------------------------------------------------------ handlers
        private void growthClamp_changed(object sender, EventArgs e)
        {
            rangeClampPanel holder = this.growthClampPanel;
            if (holder == null)
                return;
            RandomizerMore.clampGrowths = holder.check.Checked;
            RandomizerMore.growthClampMin = (int)holder.low.Value;
            RandomizerMore.growthClampMax = (int)holder.high.Value;
            holder.low.Enabled = holder.check.Checked;
            holder.high.Enabled = holder.check.Checked;
        }

        private void statBoostClamp_changed(object sender, EventArgs e)
        {
            rangeClampPanel holder = this.statBoostClampPanel;
            if (holder == null)
                return;
            RandomizerMore.clampStatBoosts = holder.check.Checked;
            RandomizerMore.statBoostClampMin = (int)holder.low.Value;
            RandomizerMore.statBoostClampMax = (int)holder.high.Value;
            holder.low.Enabled = holder.check.Checked;
            holder.high.Enabled = holder.check.Checked;
        }

        private void statBaseClamp_changed(object sender, EventArgs e)
        {
            rangeClampPanel holder = this.statBaseClampPanel;
            if (holder == null)
                return;
            RandomizerMore.clampStatBases = holder.check.Checked;
            RandomizerMore.statBaseClampMin = (int)holder.low.Value;
            RandomizerMore.statBaseClampMax = (int)holder.high.Value;
            holder.low.Enabled = holder.check.Checked;
            holder.high.Enabled = holder.check.Checked;
        }
    }
}
