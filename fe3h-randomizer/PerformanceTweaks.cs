// -----------------------------------------------------------------------------
//  PerformanceTweaks.cs
//
//  Purely cosmetic / layout tweaks that make the window feel less sluggish.
//  Nothing here changes any randomization logic:
//
//   * double buffering on the form, the tab control and every tab page
//     (removes the flicker that makes tab switches look slow)
//   * layout is suspended while a tab switch happens and resumed once, so the
//     ~2,700 controls of a page are laid out in a single pass instead of many
//     partial ones
//   * layout is batched while updateAllDisplay() re-fills thousands of
//     controls after loading files or toggling an option
// -----------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    public partial class RandomizerForm
    {
        private bool perfApplied;
        private Control perfSuspendedLayout;

        private void applyPerformanceTweaks()
        {
            if (perfApplied)
                return;
            perfApplied = true;

            // The form itself: buffered painting.
            try
            {
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
                this.DoubleBuffered = true;
                this.UpdateStyles();
            }
            catch
            {
            }

            try
            {
                if (this.FETHRand_tabControl != null)
                {
                    perfEnableDoubleBuffer(this.FETHRand_tabControl);
                    foreach (TabPage page in this.FETHRand_tabControl.TabPages)
                        perfEnableDoubleBuffer(page);

                    this.FETHRand_tabControl.Selecting += new TabControlCancelEventHandler(this.perfTabSelecting);
                    this.FETHRand_tabControl.Selected += new TabControlEventHandler(this.perfTabSelected);
                }
            }
            catch
            {
            }
        }

        // Called for the tab pages this project adds after startup.
        private static void perfPreparePage(TabPage page)
        {
            perfEnableDoubleBuffer(page);
        }

        // DoubleBuffered is protected, so use the property through reflection.
        // Only affects painting, never layout logic.
        private static void perfEnableDoubleBuffer(Control control)
        {
            if (control == null)
                return;
            try
            {
                PropertyInfo property = typeof(Control).GetProperty("DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (property != null)
                    property.SetValue(control, true, null);
            }
            catch
            {
            }
        }

        private void perfTabSelecting(object sender, TabControlCancelEventArgs e)
        {
            // Always release a page suspended by an earlier switch first, so a
            // cancelled switch can never leave a page stuck in suspended layout.
            perfResumeSuspendedLayout();
            try
            {
                if (this.FETHRand_tabControl != null)
                    this.FETHRand_tabControl.SuspendLayout();
                if (e.TabPage != null)
                {
                    e.TabPage.SuspendLayout();
                    this.perfSuspendedLayout = e.TabPage;
                }
            }
            catch
            {
            }
        }

        private void perfTabSelected(object sender, TabControlEventArgs e)
        {
            perfResumeSuspendedLayout();
            try
            {
                if (this.FETHRand_tabControl != null)
                    this.FETHRand_tabControl.ResumeLayout();
            }
            catch
            {
            }
        }

        private void perfResumeSuspendedLayout()
        {
            Control control = this.perfSuspendedLayout;
            this.perfSuspendedLayout = null;
            if (control == null)
                return;
            try
            {
                control.ResumeLayout(false);
                control.PerformLayout();
            }
            catch
            {
            }
        }
    }
}
