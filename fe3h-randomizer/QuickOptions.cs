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
            RandomizerExtras.classIdentity = value;
            RandomizerExtras.limitSiegeTomes = value;
            RandomizerExtras.blockAbilities = value;
            if (!value)
            {
                RandomizerExtras.growthShift = 0;
                // The trim is a subtraction the user asks for; turning every
                // option off must not keep weakening the class bases.
                RandomizerExtras.setFlatTrimAllBands(0, 0, 0);
                RandomizerExtras.bossHpTrim = 0;
                RandomizerExtras.bossDefTrim = 0;
                RandomizerExtras.bossResTrim = 0;
                // A bonus the user asked for, so turning everything off takes it
                // away again - the same rule as the trims.
                RandomizerExtras.playerStartBonus = 0;
            }

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
            // Effects remix: every graft is a chance, so "on" means a solid
            // stress value rather than 100 (which would replace every effect).
            int graftChance = value ? 50 : 0;
            RandomizerBig.weaponGraftChance = graftChance;
            RandomizerBig.weaponFromArtChance = graftChance;
            RandomizerBig.weaponFromGambitChance = graftChance;
            RandomizerBig.artGraftChance = graftChance;
            RandomizerBig.artFromWeaponChance = graftChance;
            RandomizerBig.gambitGraftChance = graftChance;
            RandomizerBig.gambitFromWeaponChance = graftChance;
            RandomizerBig.uniqueWeaponEffects = value;
            // The description pass only adds text for effects that changed, so
            // enabling everything turns it on and the off-everything check
            // leaves the message table alone.
            RandomizerBig.describeWeaponEffects = value;

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
            if (this.extrasSafety_numericUpDown != null)
                this.extrasSafety_numericUpDown.Value = clampToControl(this.extrasSafety_numericUpDown,
                    RandomizerExtras.classVarietySafety);
            if (this.extrasMaxRange_numericUpDown != null)
                this.extrasMaxRange_numericUpDown.Value = clampToControl(this.extrasMaxRange_numericUpDown,
                    RandomizerExtras.maxEnemyWeaponRange);
            if (this.extrasSiegeChance_numericUpDown != null)
                this.extrasSiegeChance_numericUpDown.Value = clampToControl(this.extrasSiegeChance_numericUpDown,
                    RandomizerExtras.siegeChance);
            if (this.extrasGrowthShift_numericUpDown != null)
                this.extrasGrowthShift_numericUpDown.Value = clampToControl(this.extrasGrowthShift_numericUpDown,
                    RandomizerExtras.growthShift);
            if (this.extrasGrowthTolerance_numericUpDown != null)
                this.extrasGrowthTolerance_numericUpDown.Value = clampToControl(this.extrasGrowthTolerance_numericUpDown,
                    RandomizerMore.growthTotalTolerance);
            if (this.extrasBossScaling_numericUpDown != null)
                this.extrasBossScaling_numericUpDown.Value = clampToControl(this.extrasBossScaling_numericUpDown,
                    RandomizerMore.bossScalingShare);
            if (this.extrasStatTotalTolerance_numericUpDown != null)
                this.extrasStatTotalTolerance_numericUpDown.Value = clampToControl(this.extrasStatTotalTolerance_numericUpDown,
                    RandomizerMore.statTotalTolerance);
            if (this.extrasSiege_checkBox != null)
                this.extrasSiege_checkBox.Checked = RandomizerExtras.limitSiegeTomes;
            if (this.extrasClassIdentity_checkBox != null)
                this.extrasClassIdentity_checkBox.Checked = RandomizerExtras.classIdentity;
            if (this.extrasClassDefenseEarly_numericUpDown != null)
            {
                this.extrasClassDefenseEarly_numericUpDown.Value = clampToControl(
                    this.extrasClassDefenseEarly_numericUpDown, RandomizerExtras.classDefenseClampEarly);
                this.extrasClassDefenseEarly_numericUpDown.Enabled = RandomizerExtras.classIdentity;
            }
            if (this.extrasClassDefenseMid_numericUpDown != null)
            {
                this.extrasClassDefenseMid_numericUpDown.Value = clampToControl(
                    this.extrasClassDefenseMid_numericUpDown, RandomizerExtras.classDefenseClampMid);
                this.extrasClassDefenseMid_numericUpDown.Enabled = RandomizerExtras.classIdentity;
            }
            if (this.extrasClassDefenseAdv_numericUpDown != null)
            {
                this.extrasClassDefenseAdv_numericUpDown.Value = clampToControl(
                    this.extrasClassDefenseAdv_numericUpDown, RandomizerExtras.classDefenseClampAdv);
                this.extrasClassDefenseAdv_numericUpDown.Enabled = RandomizerExtras.classIdentity;
            }
            if (this.extrasClassDefenseMaster_numericUpDown != null)
            {
                this.extrasClassDefenseMaster_numericUpDown.Value = clampToControl(
                    this.extrasClassDefenseMaster_numericUpDown, RandomizerExtras.classDefenseClampMaster);
                this.extrasClassDefenseMaster_numericUpDown.Enabled = RandomizerExtras.classIdentity;
            }
            if (this.extrasDescribeWeapons_checkBox != null)
                this.extrasDescribeWeapons_checkBox.Checked = RandomizerBig.describeWeaponEffects;
            for (int band = 0; band < 4; band++)
            {
                if (this.extrasFlatTrimHp[band] != null)
                    this.extrasFlatTrimHp[band].Value = clampToControl(
                        this.extrasFlatTrimHp[band], RandomizerExtras.flatTrimForBand(band, 0));
                if (this.extrasFlatTrimDef[band] != null)
                    this.extrasFlatTrimDef[band].Value = clampToControl(
                        this.extrasFlatTrimDef[band], RandomizerExtras.flatTrimForBand(band, 1));
                if (this.extrasFlatTrimRes[band] != null)
                    this.extrasFlatTrimRes[band].Value = clampToControl(
                        this.extrasFlatTrimRes[band], RandomizerExtras.flatTrimForBand(band, 2));
            }
            if (this.extrasBossHp_numericUpDown != null)
                this.extrasBossHp_numericUpDown.Value = clampToControl(
                    this.extrasBossHp_numericUpDown, RandomizerExtras.bossHpTrim);
            if (this.extrasBossDef_numericUpDown != null)
                this.extrasBossDef_numericUpDown.Value = clampToControl(
                    this.extrasBossDef_numericUpDown, RandomizerExtras.bossDefTrim);
            if (this.extrasBossRes_numericUpDown != null)
                this.extrasBossRes_numericUpDown.Value = clampToControl(
                    this.extrasBossRes_numericUpDown, RandomizerExtras.bossResTrim);

            if (this.extrasPlayerStartBonus_numericUpDown != null)
                this.extrasPlayerStartBonus_numericUpDown.Value = clampToControl(
                    this.extrasPlayerStartBonus_numericUpDown, RandomizerExtras.playerStartBonus);

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
            if (this.extrasGraftWeapon_numericUpDown != null)
                this.extrasGraftWeapon_numericUpDown.Value = RandomizerBig.weaponGraftChance;
            if (this.extrasGraftWeaponFromArt_numericUpDown != null)
                this.extrasGraftWeaponFromArt_numericUpDown.Value = RandomizerBig.weaponFromArtChance;
            if (this.extrasGraftWeaponFromGambit_numericUpDown != null)
                this.extrasGraftWeaponFromGambit_numericUpDown.Value = RandomizerBig.weaponFromGambitChance;
            if (this.extrasGraftArt_numericUpDown != null)
                this.extrasGraftArt_numericUpDown.Value = RandomizerBig.artGraftChance;
            if (this.extrasGraftArtFromWeapon_numericUpDown != null)
                this.extrasGraftArtFromWeapon_numericUpDown.Value = RandomizerBig.artFromWeaponChance;
            if (this.extrasGraftGambit_numericUpDown != null)
                this.extrasGraftGambit_numericUpDown.Value = RandomizerBig.gambitGraftChance;
            if (this.extrasGraftGambitFromWeapon_numericUpDown != null)
                this.extrasGraftGambitFromWeapon_numericUpDown.Value = RandomizerBig.gambitFromWeaponChance;
            if (this.extrasBigWeaponUnique_checkBox != null)
                this.extrasBigWeaponUnique_checkBox.Checked = RandomizerBig.uniqueWeaponEffects;
            if (this.extrasBigWeaponDifferentType_checkBox != null)
                this.extrasBigWeaponDifferentType_checkBox.Checked = RandomizerBig.weaponDonorsDifferInType;
            if (this.extrasBigBossRemix_checkBox != null)
                this.extrasBigBossRemix_checkBox.Checked = RandomizerBig.bossRemix;

            if (this.growthClampPanel != null && this.growthClampPanel.check != null)
            {
                this.growthClampPanel.check.Checked = RandomizerMore.clampGrowths;
                if (this.growthClampPanel.low != null)
                    this.growthClampPanel.low.Value = clampToControl(this.growthClampPanel.low,
                        RandomizerMore.growthClampMin);
                if (this.growthClampPanel.high != null)
                    this.growthClampPanel.high.Value = clampToControl(this.growthClampPanel.high,
                        RandomizerMore.growthClampMax);
            }
            if (this.statBoostClampPanel != null && this.statBoostClampPanel.check != null)
            {
                this.statBoostClampPanel.check.Checked = RandomizerMore.clampStatBoosts;
                if (this.statBoostClampPanel.low != null)
                    this.statBoostClampPanel.low.Value = clampToControl(this.statBoostClampPanel.low,
                        RandomizerMore.statBoostClampMin);
                if (this.statBoostClampPanel.high != null)
                    this.statBoostClampPanel.high.Value = clampToControl(this.statBoostClampPanel.high,
                        RandomizerMore.statBoostClampMax);
            }
            if (this.statBaseClampPanel != null && this.statBaseClampPanel.check != null)
            {
                this.statBaseClampPanel.check.Checked = RandomizerMore.clampStatBases;
                if (this.statBaseClampPanel.low != null)
                    this.statBaseClampPanel.low.Value = clampToControl(this.statBaseClampPanel.low,
                        RandomizerMore.statBaseClampMin);
                if (this.statBaseClampPanel.high != null)
                    this.statBaseClampPanel.high.Value = clampToControl(this.statBaseClampPanel.high,
                        RandomizerMore.statBaseClampMax);
            }
        }

        // Keeps a value inside a NumericUpDown's own range instead of throwing.
        private static decimal clampToControl(NumericUpDown control, int value)
        {
            if (value < (int)control.Minimum)
                return control.Minimum;
            if (value > (int)control.Maximum)
                return control.Maximum;
            return value;
        }

        // Everything on the Extra options tab that the quick buttons and the
        // presets have to bring back in line with the actual option values.
        private void syncExtraDetailControls()
        {
            this.quickSyncExtraControls();
            if (this.extrasModMode_checkBox != null)
                this.extrasModMode_checkBox.Checked = ModSupport.modMode;
            if (this.extrasModMastery_checkBox != null)
                this.extrasModMastery_checkBox.Checked = ModSupport.keepMasteryExp;
            if (this.extrasModOriginals_textBox != null
                && this.extrasModOriginals_textBox.Text != ModSupport.originalFolder)
                this.extrasModOriginals_textBox.Text = ModSupport.originalFolder;
            if (this.extrasModFolder_textBox != null
                && this.extrasModFolder_textBox.Text != ModSupport.modFolder)
                this.extrasModFolder_textBox.Text = ModSupport.modFolder;
            if (this.extrasModKind_label != null)
                this.extrasModKind_label.Text = modKindText();
            if (this.extrasModOffsets_label != null)
                this.extrasModOffsets_label.Text = "Offsets: " + ModSupport.offsetsStatus;
            if (this.extrasPreset_comboBox != null && this.extrasPreset_comboBox.Items.Count == 0)
                this.refreshPresetList();
        }
    }
}
