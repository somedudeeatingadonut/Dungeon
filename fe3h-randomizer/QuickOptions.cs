// -----------------------------------------------------------------------------
//  QuickOptions.cs
//
//  Two buttons for testing:
//
//   * Enable every option - turns on every randomization feature at once, so a
//     full-stress run can be produced without ticking a few hundred boxes.
//     Every public bool on Settings is switched on, except the interpolation
//     style selectors (Binomial / Linear / Proportional / Exponential), which
//     are alternatives to each other rather than features - the first one wins
//     inside advancedRng(), so leaving them alone keeps the math valid.
//     The extra options (class variety, blocklist, range limits, boss options
//     and the big passes) are switched on as well.
//
//   * Turn every option off - the opposite, useful to get back to a clean slate
//     without reloading the tool.
//
//  Both refresh the UI afterwards, so the tick boxes always show the truth.
// -----------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    public partial class RandomizerForm
    {
        // Interpolation styles are mutually exclusive alternatives, not features.
        private static readonly string[] quickStyleSuffixes =
            { "Binomial", "Linear", "Proportional", "Exponential" };

        private void quickEnableAll_button_Click(object sender, EventArgs e)
        {
            this.quickApplyAll(true);
            MessageBox.Show(
                "Every randomization option is now on, including the extra options in this tab\r\n"
                + "and the range limits on the Classes tabs.\r\n\r\n"
                + "The interpolation styles (binomial / linear / proportional / exponential) are\r\n"
                + "left as they were - they are alternatives to each other, not extra features.",
                "Enable every option", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void quickDisableAll_button_Click(object sender, EventArgs e)
        {
            this.quickApplyAll(false);
            MessageBox.Show(
                "Every randomization option is now off. Load a file and press Randomize to\r\n"
                + "check that the tool still writes the files out unchanged.",
                "Turn every option off", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void quickApplyAll(bool value)
        {
            if (this.settings != null)
            {
                FieldInfo[] fields = typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].FieldType != typeof(bool))
                        continue;
                    if (quickIsStyleSelector(fields[i].Name))
                        continue;
                    try
                    {
                        fields[i].SetValue(this.settings, value);
                    }
                    catch
                    {
                    }
                }
            }

            // Extra options from RandomizerExtras.
            RandomizerExtras.evenClassVariety = value;
            RandomizerExtras.limitSiegeTomes = value;
            RandomizerExtras.blockAbilities = value;
            if (!value)
                RandomizerExtras.growthShift = 0;

            // RandomizerMore passes.
            RandomizerMore.protectBossAbilities = value;
            RandomizerMore.protectBossStats = value;
            RandomizerMore.uniqueClassAbilities = value;
            RandomizerMore.uniqueTierMastery = value;
            RandomizerMore.uniqueTierMasteryArt = value;
            RandomizerMore.uniquePersonalAbilities = value;
            RandomizerMore.evenGrowthTotals = value;
            RandomizerMore.protectGrowthIdentity = value;
            RandomizerMore.evenStatTotals = value;
            RandomizerMore.clampGrowths = value;
            RandomizerMore.clampStatBoosts = value;
            RandomizerMore.clampStatBases = value;

            // Big passes. The boss remix replaces the boss protection, so it is
            // only switched on together with everything else.
            RandomizerBig.swapClassStatProfiles = value;
            RandomizerBig.swapClassGrowthProfiles = value;
            RandomizerBig.shuffleCharacterSheets = value;
            RandomizerBig.bossRemix = value;

            this.quickSyncExtraControls();
            try
            {
                this.updateAllDisplay();
            }
            catch
            {
            }
        }

        private static bool quickIsStyleSelector(string name)
        {
            for (int i = 0; i < quickStyleSuffixes.Length; i++)
                if (name.EndsWith(quickStyleSuffixes[i], StringComparison.Ordinal))
                    return true;
            return false;
        }

        // Pushes the current state back into the controls of the two tabs this
        // project adds, so they match what the buttons just changed.
        private void quickSyncExtraControls()
        {
            if (this.extrasEvenClasses_checkBox != null)
                this.extrasEvenClasses_checkBox.Checked = RandomizerExtras.evenClassVariety;
            if (this.extrasSiege_checkBox != null)
                this.extrasSiege_checkBox.Checked = RandomizerExtras.limitSiegeTomes;

            if (this.extrasBossAb_checkBox != null)
                this.extrasBossAb_checkBox.Checked = RandomizerMore.protectBossAbilities;
            if (this.extrasBossStats_checkBox != null)
                this.extrasBossStats_checkBox.Checked = RandomizerMore.protectBossStats;
            if (this.extrasUniqueClassAb_checkBox != null)
                this.extrasUniqueClassAb_checkBox.Checked = RandomizerMore.uniqueClassAbilities;
            if (this.extrasUniqueTierMastery_checkBox != null)
                this.extrasUniqueTierMastery_checkBox.Checked = RandomizerMore.uniqueTierMastery;
            if (this.extrasUniqueTierMasteryArt_checkBox != null)
                this.extrasUniqueTierMasteryArt_checkBox.Checked = RandomizerMore.uniqueTierMasteryArt;
            if (this.extrasUniquePersonal_checkBox != null)
                this.extrasUniquePersonal_checkBox.Checked = RandomizerMore.uniquePersonalAbilities;
            if (this.extrasEvenGrowthTotals_checkBox != null)
                this.extrasEvenGrowthTotals_checkBox.Checked = RandomizerMore.evenGrowthTotals;
            if (this.extrasGrowthIdentity_checkBox != null)
                this.extrasGrowthIdentity_checkBox.Checked = RandomizerMore.protectGrowthIdentity;
            if (this.extrasEvenStatTotals_checkBox != null)
                this.extrasEvenStatTotals_checkBox.Checked = RandomizerMore.evenStatTotals;

            if (this.extrasBigStatSwap_checkBox != null)
                this.extrasBigStatSwap_checkBox.Checked = RandomizerBig.swapClassStatProfiles;
            if (this.extrasBigGrowthSwap_checkBox != null)
                this.extrasBigGrowthSwap_checkBox.Checked = RandomizerBig.swapClassGrowthProfiles;
            if (this.extrasBigScope_checkBox != null)
                this.extrasBigScope_checkBox.Checked = RandomizerBig.swapScope == 1;
            if (this.extrasBigCharSheets_checkBox != null)
                this.extrasBigCharSheets_checkBox.Checked = RandomizerBig.shuffleCharacterSheets;
            if (this.extrasBigBossRemix_checkBox != null)
                this.extrasBigBossRemix_checkBox.Checked = RandomizerBig.bossRemix;

            if (this.growthClampPanel != null && this.growthClampPanel.check != null)
                this.growthClampPanel.check.Checked = RandomizerMore.clampGrowths;
            if (this.statBoostClampPanel != null && this.statBoostClampPanel.check != null)
                this.statBoostClampPanel.check.Checked = RandomizerMore.clampStatBoosts;
            if (this.statBaseClampPanel != null && this.statBaseClampPanel.check != null)
                this.statBaseClampPanel.check.Checked = RandomizerMore.clampStatBases;
        }
    }
}
