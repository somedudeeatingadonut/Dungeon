// -----------------------------------------------------------------------------
//  PerformanceTweaks.cs
//
//  Cosmetic / layout work only. Nothing here changes a single byte of the
//  randomization logic - it only changes *when* and *how often* Windows is asked
//  to lay out and paint the window.
//
//  What it does:
//
//   1. WS_EX_COMPOSITED on the window. Without it every one of the ~1,300
//      controls paints itself the moment it becomes visible, so a tab switch is
//      drawn in strips - that is the "big line" sweeping down the window. With
//      it, the whole window is painted into an off-screen buffer and appears in
//      one go.
//
//   2. Double buffering on the form, the tab control and every tab page.
//
//   3. Every tab page is given its real size once, shortly after the window
//      opens, while it is still hidden. Otherwise a page keeps its small
//      designer size until the first time you click it - and *then* Windows
//      resizes it and lays out hundreds of controls while you wait. Doing that
//      up front, one page per message so the UI never blocks, makes the first
//      visit to every tab instant.
//
//   4. Timing for all of the above, collected so the debug report can show
//      exactly where the time goes (window build, display refresh, page
//      pre-layout, tab switches).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    public partial class RandomizerForm
    {
        private const int WS_EX_COMPOSITED = 0x02000000;

        private bool perfApplied;
        private bool perfPresizeStarted;
        private readonly List<TabPage> perfPresizeQueue = new List<TabPage>();
        private Stopwatch perfTabSwitchWatch;
        private string perfTabSwitchName = "";

        // ---------------------------------------------------------- measurements
        // Read by DebugReport. Only touched from the UI thread.
        internal static readonly List<string> perfNotes = new List<string>();
        internal static long perfWindowBuildMs;
        internal static long perfExtrasBuildMs;
        internal static int perfDisplayCalls;
        internal static long perfDisplayTotalMs;
        internal static long perfDisplayMaxMs;
        internal static int perfSlowSwitches;
        internal static string perfLastSwitch = "none yet";
        internal static string perfWorstSwitch = "none yet";
        private static long perfWorstSwitchMs;

        internal static void perfNote(string what, long milliseconds)
        {
            try
            {
                if (perfNotes.Count < 40)
                    perfNotes.Add(what + " : " + milliseconds.ToString() + " ms");
            }
            catch
            {
            }
        }

        internal static void perfDisplay(long milliseconds)
        {
            perfDisplayCalls++;
            perfDisplayTotalMs += milliseconds;
            if (milliseconds > perfDisplayMaxMs)
                perfDisplayMaxMs = milliseconds;
        }

        // Composited painting: the whole window, children included, is drawn
        // off-screen and blitted once instead of one control at a time.
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WS_EX_COMPOSITED;
                return parameters;
            }
        }

        // ------------------------------------------------------------- startup
        private void applyPerformanceTweaks()
        {
            if (perfApplied)
                return;
            perfApplied = true;

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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Let the first tab paint normally, then prepare the others while
            // they are still hidden.
            try
            {
                this.BeginInvoke(new MethodInvoker(this.perfStartPresize));
            }
            catch
            {
            }
        }

        // --------------------------------------------------- page pre-layout
        private void perfStartPresize()
        {
            if (perfPresizeStarted)
                return;
            perfPresizeStarted = true;

            this.perfPresizeQueue.Clear();
            try
            {
                if (this.FETHRand_tabControl != null)
                    foreach (TabPage page in this.FETHRand_tabControl.TabPages)
                        if (page != this.FETHRand_tabControl.SelectedTab)
                            this.perfPresizeQueue.Add(page);
            }
            catch
            {
            }

            try
            {
                this.BeginInvoke(new MethodInvoker(this.perfPresizeNextPage));
            }
            catch
            {
            }
        }

        private void perfPresizeNextPage()
        {
            if (this.perfPresizeQueue.Count == 0)
                return;

            TabPage page = this.perfPresizeQueue[0];
            this.perfPresizeQueue.RemoveAt(0);

            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                perfFitTabPage(this.FETHRand_tabControl, page);
            }
            catch
            {
            }
            watch.Stop();
            perfNote("pre-layout of tab \"" + perfPageName(page) + "\"", watch.ElapsedMilliseconds);

            // One page per message keeps the window responsive while the other
            // tabs are prepared.
            try
            {
                this.BeginInvoke(new MethodInvoker(this.perfPresizeNextPage));
            }
            catch
            {
            }
        }

        private static string perfPageName(TabPage page)
        {
            try
            {
                return page.Text;
            }
            catch
            {
                return "?";
            }
        }

        // Gives a hidden page the size it will have when it is selected, so its
        // layout runs now instead of the first time it is clicked.
        private static void perfFitTabPage(TabControl tabs, TabPage page)
        {
            if (page == null || tabs == null)
                return;

            Rectangle area = tabs.DisplayRectangle;
            if (area.Width <= 0 || area.Height <= 0)
                return;
            if (page.Width != area.Width || page.Height != area.Height)
                page.Bounds = new Rectangle(area.X, area.Y, area.Width, area.Height);

            perfFitChildren(page, 1);
        }

        // Nested tab controls (the class / item / map pages use them) resize
        // their own pages the same way, so prepare those too.
        private static void perfFitChildren(Control parent, int depth)
        {
            if (parent == null || depth > 4)
                return;
            try
            {
                foreach (Control child in parent.Controls)
                {
                    TabControl tabs = child as TabControl;
                    if (tabs != null)
                    {
                        Rectangle area = tabs.DisplayRectangle;
                        foreach (TabPage page in tabs.TabPages)
                        {
                            try
                            {
                                if (area.Width > 0 && area.Height > 0
                                    && (page.Width != area.Width || page.Height != area.Height))
                                    page.Bounds = new Rectangle(area.X, area.Y, area.Width, area.Height);
                                perfFitChildren(page, depth + 1);
                            }
                            catch
                            {
                            }
                        }
                    }
                    else
                    {
                        perfFitChildren(child, depth + 1);
                    }
                }
            }
            catch
            {
            }
        }

        // ------------------------------------------------------ switch timing
        private void perfTabSelected(object sender, TabControlEventArgs e)
        {
            try
            {
                this.perfTabSwitchName = e.TabPage == null ? "?" : perfPageName(e.TabPage);
                this.perfTabSwitchWatch = Stopwatch.StartNew();
                this.BeginInvoke(new MethodInvoker(this.perfTabSwitchFinished));
            }
            catch
            {
            }
        }

        private void perfTabSwitchFinished()
        {
            if (this.perfTabSwitchWatch == null)
                return;
            this.perfTabSwitchWatch.Stop();
            long milliseconds = this.perfTabSwitchWatch.ElapsedMilliseconds;
            this.perfTabSwitchWatch = null;

            perfLastSwitch = this.perfTabSwitchName + " : " + milliseconds.ToString() + " ms";
            if (milliseconds > perfWorstSwitchMs)
            {
                perfWorstSwitchMs = milliseconds;
                perfWorstSwitch = perfLastSwitch;
            }
            if (milliseconds > 250)
                perfSlowSwitches++;
        }
    }
}
