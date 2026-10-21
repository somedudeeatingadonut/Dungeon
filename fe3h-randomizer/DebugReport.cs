// -----------------------------------------------------------------------------
//  DebugReport.cs
//
//  Writes a plain text report ("FE3H Randomizer Debug.txt") that says what the
//  program loaded, which options were active, whether each of the new features
//  actually ran, and - most importantly - verifies the output data itself:
//
//    * class variety   : weapon type spread per tier, types the balancer served
//    * ability blocklist: blocked abilities found in the randomized output that
//                        were not in the vanilla data (these would be bugs)
//    * siege limit     : out of range weapons given to map units
//    * growth shift    : per stat growth totals before / after
//
//  Everything is wrapped in try/catch: a broken report can never break a run.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class RandomizerDebug
    {
        public static bool autoWrite = true;
        public static string lastReportPath = "";
        public static string lastSummary = "";

        private static readonly string[] statNames = { "HP", "Str", "Mag", "Dex", "Spd", "Lck", "Def", "Res", "Cha" };
        private static readonly string[] weaponTypeNames =
            { "Sword", "Lance", "Axe", "Bow", "Brawl", "Magic", "???", "Dark" };

        // ------------------------------------------------------------- writing
        public static string WriteReport(string folder, string reason)
        {
            string text;
            try
            {
                text = BuildReport(reason);
            }
            catch (Exception exception)
            {
                text = "FE3H Randomizer debug report\r\n"
                    + "The report itself failed to build: " + exception.ToString() + "\r\n";
            }

            if (folder == null || folder.Length == 0)
                folder = Application.StartupPath;

            try
            {
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "FE3H Randomizer Debug.txt");
                File.WriteAllText(path, text);
                lastReportPath = path;
                return path;
            }
            catch (Exception exception)
            {
                lastSummary = "The debug report could not be written to " + folder + " (" + exception.Message + ")";
                try
                {
                    string fallback = Path.Combine(Path.GetTempPath(), "FE3H Randomizer Debug.txt");
                    File.WriteAllText(fallback, text);
                    lastReportPath = fallback;
                    return fallback;
                }
                catch
                {
                }
            }
            return null;
        }

        // ------------------------------------------------------------- report
        public static string BuildReport(string reason)
        {
            StringBuilder text = new StringBuilder();
            List<string> passes = new List<string>();
            List<string> warnings = new List<string>();
            List<string> failures = new List<string>();

            AppendHeader(text, reason);
            AppendEnvironment(text);
            AppendPerformance(text, warnings);
            AppendOptions(text);
            AppendMoreOptions(text, passes, warnings, failures);
            AppendBigOptions(text, passes, warnings, failures);
            AppendInputs(text, warnings);
            AppendCounters(text, passes, warnings);
            AppendClassVarietyCheck(text, passes, warnings, failures);
            AppendAbilityCheck(text, passes, warnings, failures);
            AppendSiegeCheck(text, passes, warnings);
            AppendGrowthShiftCheck(text, passes, warnings);
            AppendSummary(text, passes, warnings, failures);
            AppendSettingsDump(text);

            lastSummary = passes.Count + " passed, " + warnings.Count + " warnings, " + failures.Count + " failures";
            return text.ToString();
        }

        private static void AppendHeader(StringBuilder text, string reason)
        {
            text.AppendLine("================================================================");
            text.AppendLine(" FE3H Randomizer - debug report");
            text.AppendLine("================================================================");
            text.AppendLine();
            text.AppendLine("Written      : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            text.AppendLine("Reason       : " + (reason == null ? "" : reason));
            text.AppendLine("Seed         : " + (Randomizer.seed.ToString()));
            text.AppendLine("Random numbers generated: " + (Randomizer.rng == null ? "n/a" : Randomizer.rng.GetRandomNumberCount().ToString()));
            text.AppendLine();
        }

        private static void AppendEnvironment(StringBuilder text)
        {
            text.AppendLine("--- ENVIRONMENT ------------------------------------------------");
            try
            {
                text.AppendLine("Exe path     : " + Application.ExecutablePath);
                text.AppendLine("Exe folder   : " + Application.StartupPath);
                text.AppendLine("Assembly     : " + typeof(Randomizer).Assembly.GetName().Version.ToString());
                text.AppendLine("File version : " + Application.ProductVersion);
                text.AppendLine("OS           : " + Environment.OSVersion.VersionString);
                text.AppendLine(".NET         : " + Environment.Version);
                text.AppendLine("64-bit process: " + (IntPtr.Size == 8));
                text.AppendLine("UI culture   : " + CultureInfo.CurrentUICulture.Name);
            }
            catch (Exception exception)
            {
                text.AppendLine("(environment could not be read: " + exception.Message + ")");
            }
            text.AppendLine();
        }

        // -------------------------------------------------------- performance
        private static void AppendPerformance(StringBuilder text, List<string> warnings)
        {
            text.AppendLine("--- PERFORMANCE ------------------------------------------------");
            text.AppendLine("window build (all controls) : " + RandomizerForm.perfWindowBuildMs.ToString() + " ms");
            text.AppendLine("extra tabs built            : " + RandomizerForm.perfExtrasBuildMs.ToString() + " ms");
            if (RandomizerForm.perfDisplayCalls > 0)
            {
                long average = RandomizerForm.perfDisplayTotalMs / RandomizerForm.perfDisplayCalls;
                text.AppendLine("display refresh             : " + RandomizerForm.perfDisplayCalls.ToString()
                    + " calls, " + RandomizerForm.perfDisplayTotalMs.ToString() + " ms total, "
                    + average.ToString() + " ms average, " + RandomizerForm.perfDisplayMaxMs.ToString() + " ms worst");
            }
            text.AppendLine("last tab switch             : " + RandomizerForm.perfLastSwitch);
            text.AppendLine("worst tab switch            : " + RandomizerForm.perfWorstSwitch);
            text.AppendLine("tab switches over 250 ms    : " + RandomizerForm.perfSlowSwitches.ToString());
            if (RandomizerForm.perfNotes.Count > 0)
            {
                text.AppendLine("startup / layout timings:");
                foreach (string note in RandomizerForm.perfNotes)
                    text.AppendLine("    " + note);
            }
            if (RandomizerForm.perfSlowSwitches > 0)
                warnings.Add("tab switching is still slow (" + RandomizerForm.perfWorstSwitch + ")");
            text.AppendLine();
        }

        private static void AppendOptions(StringBuilder text)
        {
            text.AppendLine("--- NEW OPTIONS ------------------------------------------------");
            text.AppendLine("Even class variety      : " + RandomizerExtras.evenClassVariety);
            text.AppendLine("Class variety safety    : " + RandomizerExtras.classVarietySafety + "%");
            text.AppendLine("Ability blocklist on    : " + RandomizerExtras.blockAbilities);
            text.AppendLine("Blocked abilities       : " + RandomizerExtras.blockedAbilities.Count);
            foreach (int ability in RandomizerExtras.blockedAbilities)
            {
                string name = ability >= 0 && ability < Randomizer.abilityNames.Length
                    ? Randomizer.abilityNames[ability] : "(unknown)";
                text.AppendLine("    " + ability.ToString() + " : " + name);
            }
            text.AppendLine("Siege limit on          : " + RandomizerExtras.limitSiegeTomes);
            text.AppendLine("Max enemy weapon range  : " + RandomizerExtras.maxEnemyWeaponRange);
            text.AppendLine("Siege bypass chance     : " + RandomizerExtras.siegeChance + "%");
            text.AppendLine("Growth shift            : " + RandomizerExtras.growthShift + "%");
            text.AppendLine();
        }

        private static void AppendMoreOptions(StringBuilder text, List<string> passes,
            List<string> warnings, List<string> failures)
        {
            text.AppendLine("--- MORE OPTIONS -----------------------------------------------");
            if (Randomizer.classData == null && Randomizer.personData == null)
            {
                text.AppendLine("nothing randomized yet - the passes run when you press Randomize");
                text.AppendLine();
                return;
            }

            int changedBossSlots = RandomizerMore.countChangedBossSlots();
            int changedBossStats = RandomizerMore.countChangedBossStats();
            text.AppendLine("Boss abilities kept as they are: " + RandomizerMore.protectBossAbilities
                + "   (slots restored " + RandomizerMore.statBossSlotsRestored.ToString()
                + ", still changed " + changedBossSlots.ToString() + ")");
            text.AppendLine("Boss stats kept as they are    : " + RandomizerMore.protectBossStats
                + "   (slots restored " + RandomizerMore.statBossStatSlotsRestored.ToString()
                + ", still changed " + changedBossStats.ToString() + ")");
            bool scalingOn = Randomizer.settingsForDebug != null && Randomizer.settingsForDebug.changeLv;
            int multiplier = Randomizer.settingsForDebug == null
                ? 100 : (int)Randomizer.settingsForDebug.lvMultiplier;
            text.AppendLine("Boss level scaling bonus       : " + RandomizerMore.bossScalingShare + "% of the scaling"
                + "   (level scaling " + (scalingOn ? "on" : "off") + ", multiplier " + multiplier + "%"
                + ", base stats raised " + RandomizerMore.statBossScalingBonus.ToString()
                + ", stat boosts raised " + RandomizerMore.statBossScalingBoostBonus.ToString()
                + ", bosses touched " + RandomizerMore.statBossScalingClassCount.ToString() + ")");

            text.AppendLine("No repeated class ability      : " + RandomizerMore.uniqueClassAbilities
                + "   (rerolled " + RandomizerMore.statDuplicateClassAbRerolled.ToString()
                + ", left " + RandomizerMore.countDuplicateClassAbilities().ToString() + ")");
            text.AppendLine("One mastery ability per tier   : " + RandomizerMore.uniqueTierMastery
                + "   (rerolled " + RandomizerMore.statTierMasteryRerolled.ToString()
                + ", left " + RandomizerMore.countDuplicateTierMastery().ToString() + ")");
            text.AppendLine("One mastery art per tier       : " + RandomizerMore.uniqueTierMasteryArt
                + "   (rerolled " + RandomizerMore.statTierMasteryArtRerolled.ToString() + ")");
            text.AppendLine("Unique personal abilities      : " + RandomizerMore.uniquePersonalAbilities
                + "   (rerolled " + RandomizerMore.statDuplicatePersonalRerolled.ToString()
                + ", left " + RandomizerMore.countDuplicatePersonalAbilities().ToString() + ")");

            int growthLow;
            int growthHigh;
            int growthStuck;
            bool growthStuckPlayer;
            RandomizerMore.growthValues(out growthLow, out growthHigh, out growthStuck, out growthStuckPlayer);
            // 0 means "this class has no growth in that stat" and is left alone on
            // purpose, so only a non-zero low counts against the limit.
            bool growthOutOfRange = (growthLow != 0 && growthLow < RandomizerMore.growthClampMin)
                || growthHigh > RandomizerMore.growthClampMax;
            if (!growthOutOfRange)
                growthStuck = -1;
            int totalLow;
            int totalHigh;
            int totalAverage;
            RandomizerMore.growthTotals(out totalLow, out totalHigh, out totalAverage);

            text.AppendLine("Growth range limit             : " + RandomizerMore.clampGrowths
                + "  (" + RandomizerMore.growthClampMin.ToString() + "% to "
                + RandomizerMore.growthClampMax.ToString() + "%)"
                + "   (pulled back " + RandomizerMore.statGrowthsClamped.ToString()
                + ", class growths now " + growthLow.ToString() + "% to " + growthHigh.ToString() + "%)");
            if (RandomizerMore.clampGrowths && growthStuck >= 0)
                text.AppendLine("    out of range value left in class " + growthStuck.ToString()
                    + " (" + (growthStuckPlayer ? "player" : "enemy") + " growth = "
                    + growthHigh.ToString() + "%, unchanged from vanilla on purpose)");
            text.AppendLine("Even growth totals             : " + RandomizerMore.evenGrowthTotals
                + "  (tolerance " + RandomizerMore.growthTotalTolerance.ToString() + "%)"
                + "   (classes evened " + RandomizerMore.statGrowthTotalsEvened.ToString() + ")");
            text.AppendLine("Keep each class's best growth  : " + RandomizerMore.protectGrowthIdentity
                + "   (classes kept " + RandomizerMore.statGrowthIdentityKept.ToString() + ")");
            text.AppendLine("    class growth totals now   : " + totalLow.ToString() + " to "
                + totalHigh.ToString() + ", average " + totalAverage.ToString());

            int boostLow;
            int boostHigh;
            int baseLow;
            int baseHigh;
            RandomizerMore.statValues(out boostLow, out boostHigh, out baseLow, out baseHigh);
            text.AppendLine("Stat boost range limit         : " + RandomizerMore.clampStatBoosts
                + "  (" + RandomizerMore.statBoostClampMin.ToString() + " to "
                + RandomizerMore.statBoostClampMax.ToString() + ")"
                + "   (pulled back " + RandomizerMore.statStatBoostsClamped.ToString()
                + ", boosts now " + boostLow.ToString() + " to " + boostHigh.ToString() + ")");
            text.AppendLine("Even base stat totals          : " + RandomizerMore.evenStatTotals
                + "  (tolerance " + RandomizerMore.statTotalTolerance.ToString() + "%)"
                + "   (classes evened " + RandomizerMore.statStatTotalsEvened.ToString() + ")");
            text.AppendLine("Base stat range limit          : " + RandomizerMore.clampStatBases
                + "  (" + RandomizerMore.statBaseClampMin.ToString() + " to "
                + RandomizerMore.statBaseClampMax.ToString() + ")"
                + "   (pulled back " + RandomizerMore.statStatBasesClamped.ToString()
                + ", bases now " + baseLow.ToString() + " to " + baseHigh.ToString() + ")");
            text.AppendLine();

            if (RandomizerMore.protectBossAbilities)
            {
                if (RandomizerBig.bossRemix)
                    text.AppendLine("note: boss remix is on, so the boss ability slots are intentionally shuffled");
                else if (changedBossSlots == 0)
                    passes.Add("boss / monster abilities are untouched");
                else
                    failures.Add(changedBossSlots.ToString() + " boss ability slot(s) still differ from vanilla");
            }
            if (RandomizerMore.protectBossStats)
            {
                if (changedBossStats == 0)
                    passes.Add("boss / monster stats are untouched");
                else
                    failures.Add(changedBossStats.ToString() + " boss stat slot(s) still differ from vanilla");
            }
            if (RandomizerMore.uniqueClassAbilities)
            {
                int duplicates = RandomizerMore.countDuplicateClassAbilities();
                if (duplicates == 0)
                    passes.Add("no class has the same ability twice");
                else
                    warnings.Add(duplicates.ToString() + " class(es) still have a repeated ability (vanilla ones are kept)");
            }
            if (RandomizerMore.uniqueTierMastery)
            {
                int duplicates = RandomizerMore.countDuplicateTierMastery();
                if (duplicates == 0)
                    passes.Add("every class in a tier has a different mastery ability");
                else
                    warnings.Add(duplicates.ToString() + " repeated mastery abilit(ies) inside a tier (vanilla ones are kept)");
            }
            if (RandomizerMore.uniqueTierMasteryArt)
                passes.Add("mastery combat arts: " + RandomizerMore.statTierMasteryArtRerolled.ToString()
                    + " re-rolled to keep the tier unique");
            if (RandomizerMore.protectBossStats && RandomizerMore.bossScalingShare > 0)
            {
                if (Randomizer.settingsForDebug == null || !Randomizer.settingsForDebug.changeLv
                    || Randomizer.settingsForDebug.lvMultiplier <= 100)
                    passes.Add("boss scaling bonus: not needed (level scaling is off)");
                else if (RandomizerMore.statBossScalingBonus + RandomizerMore.statBossScalingBoostBonus > 0)
                    passes.Add("boss scaling bonus: "
                        + (RandomizerMore.statBossScalingBonus + RandomizerMore.statBossScalingBoostBonus).ToString()
                        + " boss stat(s) raised across " + RandomizerMore.statBossScalingClassCount.ToString()
                        + " boss class(es)");
                else
                    warnings.Add("boss scaling bonus was on but no boss stat was raised - the monster class rows have no non-zero stat fields");
            }
            if (RandomizerMore.protectGrowthIdentity && RandomizerMore.statGrowthIdentityKept == 0)
                warnings.Add("no class needed its best growth stat restored (nothing wrong, the randomization may have kept them)");
            if (RandomizerMore.uniquePersonalAbilities)
            {
                int duplicates = RandomizerMore.countDuplicatePersonalAbilities();
                if (duplicates == 0)
                    passes.Add("every character has a different personal ability");
                else
                    warnings.Add(duplicates.ToString() + " duplicate personal abilit(ies) left (vanilla ones are kept)");
            }
            if (RandomizerMore.clampGrowths)
            {
                if (!growthOutOfRange)
                    passes.Add("growth range limit held (class growths " + growthLow.ToString()
                        + "% to " + growthHigh.ToString() + "%)");
                else
                    warnings.Add("growth range limit: class growths are " + growthLow.ToString()
                        + "% to " + growthHigh.ToString() + "% after the run");
            }
            if (RandomizerMore.evenGrowthTotals)
                passes.Add("growth totals evened: " + RandomizerMore.statGrowthTotalsEvened.ToString()
                    + " class(es) scaled, totals now " + totalLow.ToString() + " to " + totalHigh.ToString());
            if (RandomizerMore.clampStatBoosts)
            {
                if (boostLow >= RandomizerMore.statBoostClampMin && boostHigh <= RandomizerMore.statBoostClampMax)
                    passes.Add("stat boost range limit held (" + boostLow.ToString() + " to " + boostHigh.ToString() + ")");
                else
                    warnings.Add("stat boost range limit: boosts are " + boostLow.ToString()
                        + " to " + boostHigh.ToString() + " after the run");
            }
            if (RandomizerMore.clampStatBases)
            {
                if (baseLow >= RandomizerMore.statBaseClampMin && baseHigh <= RandomizerMore.statBaseClampMax
                    && baseHigh > 0)
                    passes.Add("base stat range limit held (" + baseLow.ToString() + " to " + baseHigh.ToString() + ")");
                else
                    warnings.Add("base stat range limit: bases are " + baseLow.ToString()
                        + " to " + baseHigh.ToString() + " after the run");
            }
        }

        // ------------------------------------------------------ big options
        private static void AppendBigOptions(StringBuilder text, List<string> passes,
            List<string> warnings, List<string> failures)
        {
            text.AppendLine("--- BIG OPTIONS ------------------------------------------------");
            if (Randomizer.classData == null && Randomizer.personData == null)
            {
                text.AppendLine("nothing randomized yet - these passes run when you press Randomize");
                text.AppendLine();
                return;
            }

            text.AppendLine("Swap class stat profiles       : " + RandomizerBig.swapClassStatProfiles
                + "   (classes swapped " + RandomizerBig.statClassStatsSwapped.ToString()
                + ", scope " + (RandomizerBig.swapScope == 1 ? "whole game" : "per tier") + ")");
            text.AppendLine("Swap class growth profiles     : " + RandomizerBig.swapClassGrowthProfiles
                + "   (classes swapped " + RandomizerBig.statClassGrowthsSwapped.ToString() + ")");
            text.AppendLine("Shuffle character sheets       : " + RandomizerBig.shuffleCharacterSheets
                + "   (characters swapped " + RandomizerBig.statCharSheetsSwapped.ToString() + ")");
            text.AppendLine("Boss remix                     : " + RandomizerBig.bossRemix
                + "   (bosses remixed " + RandomizerBig.statBossRemixed.ToString() + ")");
            text.AppendLine("Graft weapon effects          : " + RandomizerBig.graftWeaponEffects
                + "   (grafted " + RandomizerBig.statWeaponEffectsGrafted.ToString()
                + ", donors of a different type: " + RandomizerBig.weaponDonorsDifferInType + ")");
            text.AppendLine("Combat art effects on weapons : " + RandomizerBig.graftCombatArtEffects
                + "   (weapons given one " + RandomizerBig.statArtEffectsGrafted.ToString() + ")");
            text.AppendLine("Gambit effects on weapons     : " + RandomizerBig.graftGambitEffects
                + "   (weapons given one " + RandomizerBig.statGambitEffectsGrafted.ToString() + ")");
            text.AppendLine("Unique weapon effect sets     : " + RandomizerBig.uniqueWeaponEffects
                + "   (rerolled " + RandomizerBig.statWeaponEffectsRerolled.ToString()
                + ", left " + RandomizerBig.countDuplicateWeaponEffects().ToString() + ")");
            text.AppendLine("    weapons with an effect : " + RandomizerBig.countWeaponsWithEffects().ToString());
            text.AppendLine("    classes differing from vanilla : " + RandomizerBig.countChangedClasses().ToString());
            text.AppendLine("    characters differing from vanilla : " + RandomizerBig.countChangedCharacters().ToString());
            text.AppendLine();

            if (RandomizerBig.swapClassStatProfiles)
            {
                if (RandomizerBig.statClassStatsSwapped > 0)
                    passes.Add("class stat profiles swapped: " + RandomizerBig.statClassStatsSwapped.ToString()
                        + " class(es) got another class's stat line");
                else
                    warnings.Add("class stat profile swap was on but no class changed - a tier needs at least two classes");
            }
            if (RandomizerBig.swapClassGrowthProfiles)
            {
                if (RandomizerBig.statClassGrowthsSwapped > 0)
                    passes.Add("class growth profiles swapped: " + RandomizerBig.statClassGrowthsSwapped.ToString()
                        + " class(es) got another class's growths");
                else
                    warnings.Add("class growth profile swap was on but no class changed");
            }
            if (RandomizerBig.shuffleCharacterSheets)
            {
                if (RandomizerBig.statCharSheetsSwapped > 0)
                    passes.Add("character sheets shuffled: " + RandomizerBig.statCharSheetsSwapped.ToString()
                        + " character(s) got another character's bases and growths");
                else
                    warnings.Add("character sheet shuffle was on but no character changed");
            }
            if (RandomizerBig.graftWeaponEffects)
            {
                if (RandomizerBig.statWeaponEffectsGrafted > 0)
                    passes.Add("weapon effects grafted: " + RandomizerBig.statWeaponEffectsGrafted.ToString()
                        + " weapon(s) took another weapon's effects");
                else if (Randomizer.itemData == null)
                    warnings.Add("weapon effect graft was on but fixed_data.bin is not loaded");
                else
                    warnings.Add("weapon effect graft was on but no donor with an effect was found");
            }
            if (RandomizerBig.graftCombatArtEffects)
            {
                if (RandomizerBig.statArtEffectsGrafted > 0)
                    passes.Add("combat art effects on weapons: " + RandomizerBig.statArtEffectsGrafted.ToString()
                        + " weapon(s) given an art effect id");
                else
                    warnings.Add("combat art effect graft was on but nothing changed (fix_data.bin not loaded?)");
            }
            if (RandomizerBig.graftGambitEffects)
            {
                if (RandomizerBig.statGambitEffectsGrafted > 0)
                    passes.Add("gambit effects on weapons: " + RandomizerBig.statGambitEffectsGrafted.ToString()
                        + " weapon(s) given a gambit's effects");
                else
                    warnings.Add("gambit effect graft was on but nothing changed");
            }
            if (RandomizerBig.uniqueWeaponEffects)
            {
                int duplicates = RandomizerBig.countDuplicateWeaponEffects();
                if (duplicates == 0)
                    passes.Add("no two weapons share an effect set");
                else
                    warnings.Add(duplicates.ToString() + " weapon(s) still share an effect set");
            }
            if (RandomizerBig.bossRemix)
            {
                if (RandomizerBig.statBossRemixed > 0)
                    passes.Add("boss remix: " + RandomizerBig.statBossRemixed.ToString()
                        + " boss class(es) took another boss's ability kit");
                else
                    warnings.Add("boss remix was on but no boss class changed");
            }
        }

        private static void AppendInputs(StringBuilder text, List<string> warnings)
        {
            text.AppendLine("--- LOADED FILES -----------------------------------------------");
            AppendFile(text, "DATA1.bin", Randomizer.data1Loaded, Randomizer.data1Filename,
                Randomizer.baiSectionDataV, warnings);
            AppendFile(text, "fixed_persondata.bin", Randomizer.personLoaded, Randomizer.personFilename,
                Randomizer.personDataV, warnings);
            AppendFile(text, "fixed_classdata.bin", Randomizer.classLoaded, Randomizer.classFilename,
                Randomizer.classDataV, warnings);
            AppendFile(text, "fixed_data.bin", Randomizer.itemLoaded, Randomizer.itemFilename,
                Randomizer.itemDataV, warnings);
            AppendFile(text, "fixed_calendardata.bin", Randomizer.calendarLoaded, Randomizer.calendarFilename,
                Randomizer.calendarDataV, warnings);
            AppendFile(text, "fixed_lobby_common.bin", Randomizer.lobbyLoaded, Randomizer.lobbyFilename,
                Randomizer.lobbyDataV, warnings);
            AppendFile(text, "fixed_act_data.bin", Randomizer.actLoaded, Randomizer.actFilename,
                Randomizer.actDataV, warnings);
            AppendFile(text, "fixed_growthdata.bin", Randomizer.growthdataLoaded, Randomizer.growthdataFilename,
                Randomizer.growthdataDataV, warnings);
            AppendFile(text, "fixed_lobby_activity.bin", Randomizer.activityLoaded, Randomizer.activityFilename,
                Randomizer.activityDataV, warnings);
            AppendFile(text, "fixed_groupworkdata.bin", Randomizer.groupWorkLoaded, Randomizer.groupWorkFilename,
                Randomizer.groupWorkDataV, warnings);
            AppendFile(text, "fixed_presentdata.bin", Randomizer.presentLoaded, Randomizer.presentFilename,
                Randomizer.presentDataV, warnings);
            AppendFile(text, "fixed_shopdata.bin", Randomizer.shopLoaded, Randomizer.shopFilename,
                Randomizer.shopDataV, warnings);
            AppendFile(text, "fixed_lobby_quest.bin", Randomizer.questLoaded, Randomizer.questFilename,
                Randomizer.questDataV, warnings);
            AppendFile(text, "fixed_scenario.bin", Randomizer.scenarioLoaded, Randomizer.scenarioFilename,
                Randomizer.scenarioDataV, warnings);
            AppendFile(text, "msgdata.bin", Randomizer.msgLoaded, Randomizer.msgFilename,
                Randomizer.msgDataV, warnings);
            AppendFile(text, "scrdata.bin", Randomizer.scrLoaded, Randomizer.scrFilename,
                Randomizer.scrDataV, warnings);
            AppendFile(text, "gwscrdata.bin", Randomizer.gwscrLoaded, Randomizer.gwscrFilename,
                Randomizer.gwscrDataV, warnings);
            AppendFile(text, "tuscrdata.bin", Randomizer.tuscrLoaded, Randomizer.tuscrFilename,
                Randomizer.tuscrDataV, warnings);
            AppendFile(text, "btlscrdata.bin", Randomizer.btlscrLoaded, Randomizer.btlscrFilename,
                Randomizer.btlscrDataV, warnings);
            AppendFile(text, "BGM.ktsl2stbin", Randomizer.BGMLoaded, Randomizer.BGMFilename,
                Randomizer.BGMDataV, warnings);
            text.AppendLine();
        }

        private static void AppendFile(StringBuilder text, string label, bool loaded, string path,
            byte[] loadedData, List<string> warnings)
        {
            if (!loaded)
            {
                text.AppendLine(label + " : not loaded");
                return;
            }
            string state = "(no path recorded)";
            try
            {
                if (path != null && path.Length > 0)
                {
                    if (File.Exists(path))
                    {
                        long size = new FileInfo(path).Length;
                        state = path + "  [file " + size.ToString() + " bytes]";
                        if (loadedData != null && loadedData.Length > 0 && size != loadedData.Length)
                        {
                            state += "  [WARNING: loaded " + loadedData.Length.ToString()
                                + " bytes, file changed afterwards?]";
                            warnings.Add(label + " changed on disk after it was loaded");
                        }
                    }
                    else
                    {
                        state = path + "  [file not found any more]";
                    }
                }
            }
            catch (Exception exception)
            {
                state = path + "  [could not stat: " + exception.Message + "]";
            }
            text.AppendLine(label + " : loaded  " + state);
        }

        private static void AppendCounters(StringBuilder text, List<string> passes, List<string> warnings)
        {
            text.AppendLine("--- NEW CODE - DID IT RUN? -------------------------------------");
            text.AppendLine("class variety picks           : " + RandomizerExtras.statClassPicks);
            text.AppendLine("class weapon type draws       : " + RandomizerExtras.statClassTypeDraws);
            text.AppendLine("class type bag refills        : " + RandomizerExtras.statClassTypeBagRefills);
            text.AppendLine("class pick deck rebuilds      : " + RandomizerExtras.statTypeDeckRebuilds);
            text.AppendLine("ability picks                 : " + RandomizerExtras.statAbilityPicks);
            text.AppendLine("ability picks with blocks     : " + RandomizerExtras.statAbilityBlockedPicks);
            text.AppendLine("weapon range filter calls     : " + RandomizerExtras.statRangeFilterCalls);
            text.AppendLine("weapons removed by range      : " + RandomizerExtras.statRangeFilteredWeapons);
            text.AppendLine("siege bypass rolls won        : " + RandomizerExtras.statSiegeBypassRolls);
            text.AppendLine("enemy spells picked           : " + RandomizerExtras.statSpellPicks);
            text.AppendLine("spells replaced for range     : " + RandomizerExtras.statSpellsFiltered);
            text.AppendLine("classes growth shifted        : " + RandomizerExtras.statGrowthShiftedClasses);
            text.AppendLine("tier spread passes            : " + RandomizerExtras.statSpreadPasses);

            if (RandomizerExtras.evenClassVariety)
            {
                if (RandomizerExtras.statClassTypeDraws > 0 || RandomizerExtras.statClassPicks > 0)
                    passes.Add("class variety code ran (" + RandomizerExtras.statClassPicks.ToString()
                        + " class picks, " + RandomizerExtras.statClassTypeDraws.ToString() + " weapon type draws)");
                else
                    warnings.Add("class variety is enabled but nothing was balanced - is class data loaded,"
                        + " and is 'randomize class weapon types' / 'randomize classes' turned on?");
            }
            if (RandomizerExtras.limitSiegeTomes && RandomizerExtras.statRangeFilterCalls == 0)
                warnings.Add("siege limit is enabled but no weapon pool was filtered - are randomized map weapons on?");
            if (RandomizerExtras.blockAbilities && RandomizerExtras.blockedAbilities.Count > 0
                && RandomizerExtras.statAbilityPicks == 0)
                warnings.Add("ability blocklist is enabled but no ability was picked - is ability randomization on?");
            if (RandomizerExtras.growthShift > 0 && RandomizerExtras.statGrowthShiftedClasses == 0)
                warnings.Add("growth shift is set but no class was changed - is class data loaded?");
            text.AppendLine();
        }

        // --------------------------------------------------- class variety
        private static void AppendClassVarietyCheck(StringBuilder text, List<string> passes,
            List<string> warnings, List<string> failures)
        {
            text.AppendLine("--- CLASS VARIETY CHECK ----------------------------------------");
            if (!Randomizer.classLoaded || Randomizer.classData == null || Randomizer.classDataV == null)
            {
                text.AppendLine("class data not loaded, nothing to check");
                text.AppendLine();
                return;
            }

            int[][] before = countWeaponTypesByTier(Randomizer.classDataV);
            int[][] after = countWeaponTypesByTier(Randomizer.classData);

            text.AppendLine("Weapon types per class tier (classes, not characters):");
            text.AppendLine("tier      " + Header());
            for (int tier = 0; tier < after.Length; tier++)
            {
                if (Total(before[tier]) == 0 && Total(after[tier]) == 0)
                    continue;
                text.AppendLine("before " + TierName(tier) + " " + Row(before[tier]));
                text.AppendLine("after  " + TierName(tier) + " " + Row(after[tier]));
            }
            text.AppendLine();

            text.AppendLine("What the balancer actually served:");
            for (int tier = 0; tier < RandomizerExtras.statTypesServedByTier.Length; tier++)
            {
                List<int> served = RandomizerExtras.statTypesServedByTier[tier];
                if (served == null || served.Count == 0)
                    continue;
                text.AppendLine("  " + TierName(tier) + " : " + served.Count.ToString() + " of 8 types"
                    + " (" + TypeList(served) + ")");
            }
            if (RandomizerExtras.statTypesServedGlobal != null && RandomizerExtras.statTypesServedGlobal.Count > 0)
                text.AppendLine("  whole run : " + RandomizerExtras.statTypesServedGlobal.Count.ToString() + " of 8 types"
                    + " (" + TypeList(RandomizerExtras.statTypesServedGlobal) + ")");
            text.AppendLine();

            if (!RandomizerExtras.evenClassVariety)
            {
                text.AppendLine("even class variety is off, only informational");
                text.AppendLine();
                return;
            }

            int safety = RandomizerExtras.classVarietySafety;
            int[] classCount = countClassesByTier(Randomizer.classData);
            bool anyFailure = false;

            for (int tier = 1; tier < 8; tier++)
            {
                List<int> served = RandomizerExtras.statTypesServedByTier[tier];
                if (served == null || served.Count == 0)
                    continue;

                // A tier can only serve as many types as were actually dealt to
                // it, and never more than it has classes. Judging it against the
                // class count alone produced false alarms: a tier that was dealt
                // 4 types cannot serve 8, whatever the safety level says.
                int draws = RandomizerExtras.typeDrawsForTier(tier);
                int expected = classCount[tier];
                if (expected > 8)
                    expected = 8;
                if (draws > 0 && draws < expected)
                    expected = draws;
                if (expected < 1)
                    expected = 1;

                if (served.Count < expected)
                {
                    text.AppendLine("WARN  " + TierName(tier) + " only served " + served.Count.ToString()
                        + " weapon types out of " + expected.ToString()
                        + " dealt (safety " + safety.ToString() + "%)");
                    if (safety >= 100 && served.Count < expected)
                    {
                        failures.Add(TierName(tier) + " did not get all weapon types at 100% safety");
                        anyFailure = true;
                    }
                    else
                    {
                        warnings.Add(TierName(tier) + " served fewer weapon types than it was dealt");
                    }
                }
            }

            if (RandomizerExtras.statTypesServedGlobal != null && RandomizerExtras.statTypesServedGlobal.Count > 0
                && RandomizerExtras.statTypesServedGlobal.Count < 8)
            {
                text.AppendLine("FAIL  the whole run only served "
                    + RandomizerExtras.statTypesServedGlobal.Count.ToString() + " of 8 weapon types");
                failures.Add("not every weapon type was reachable in this run");
                anyFailure = true;
            }

            // duplicates: how often the most used class shows up in the class table
            text.AppendLine("Most common weapon type per tier: " + MostCommonPerTier(after));

            if (!anyFailure && safety >= 100)
                passes.Add("class variety: every tier served all weapon types");
            else if (!anyFailure)
                passes.Add("class variety: spread is within the " + safety.ToString() + "% safety level");
            text.AppendLine();
        }

        private static int[][] countWeaponTypesByTier(byte[] classData)
        {
            int[][] counts = new int[8][];
            for (int tier = 0; tier < counts.Length; tier++)
                counts[tier] = new int[8];

            if (classData == null)
                return counts;

            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 80 >= classData.Length)
                    break;
                if (!(index < 88 || index == 91))
                    continue;
                if (classData[block + 73L] != 255)
                    continue; // monster classes have no normal weapon type
                int tier = classData[block + 69L];
                if (tier == 5)
                    tier = 3;
                if (tier < 0 || tier >= counts.Length)
                    continue;

                int first = classData[block + 79L];
                int second = classData[block + 80L];
                AddType(counts[tier], first);
                if (second != 0)
                    AddType(counts[tier], second);
            }
            return counts;
        }

        private static int[] countClassesByTier(byte[] classData)
        {
            int[] counts = new int[8];
            if (classData == null)
                return counts;
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 80 >= classData.Length)
                    break;
                if (!(index < 88 || index == 91))
                    continue;
                if (classData[block + 73L] != 255)
                    continue;
                int tier = classData[block + 69L];
                if (tier == 5)
                    tier = 3;
                if (tier >= 0 && tier < counts.Length)
                    counts[tier]++;
            }
            return counts;
        }

        private static void AddType(int[] counts, int type)
        {
            if (type == 7)
                type = 5;
            if (type >= 0 && type < counts.Length)
                counts[type]++;
        }

        private static int Total(int[] counts)
        {
            int total = 0;
            for (int i = 0; i < counts.Length; i++)
                total += counts[i];
            return total;
        }

        private static string Header()
        {
            StringBuilder line = new StringBuilder();
            for (int i = 0; i < weaponTypeNames.Length; i++)
                line.Append(weaponTypeNames[i].PadLeft(6));
            return line.ToString();
        }

        private static string Row(int[] counts)
        {
            StringBuilder line = new StringBuilder();
            for (int i = 0; i < counts.Length; i++)
                line.Append(counts[i].ToString().PadLeft(6));
            return line.ToString();
        }

        private static string TierName(int tier)
        {
            switch (tier)
            {
                case 0: return "start ";
                case 1: return "begin ";
                case 2: return "inter ";
                case 3: return "adv   ";
                case 4: return "master";
                case 6: return "unique";
                default: return "tier" + tier.ToString();
            }
        }

        private static string TypeList(List<int> types)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < types.Count; i++)
            {
                if (i > 0)
                    text.Append(", ");
                int type = types[i];
                text.Append(type >= 0 && type < weaponTypeNames.Length ? weaponTypeNames[type] : "?");
            }
            return text.ToString();
        }

        private static string MostCommonPerTier(int[][] counts)
        {
            StringBuilder text = new StringBuilder();
            for (int tier = 0; tier < counts.Length; tier++)
            {
                if (Total(counts[tier]) == 0)
                    continue;
                int best = 0;
                for (int i = 1; i < counts[tier].Length; i++)
                    if (counts[tier][i] > counts[tier][best])
                        best = i;
                text.Append(TierName(tier) + "=" + weaponTypeNames[best] + " x" + counts[tier][best].ToString() + "  ");
            }
            return text.ToString();
        }

        // ------------------------------------------------------- abilities
        private static void AppendAbilityCheck(StringBuilder text, List<string> passes,
            List<string> warnings, List<string> failures)
        {
            text.AppendLine("--- ABILITY BLOCKLIST CHECK ------------------------------------");
            if (!RandomizerExtras.blockAbilities || RandomizerExtras.blockedAbilities.Count == 0)
            {
                text.AppendLine("blocklist is empty, nothing to check");
                text.AppendLine();
                return;
            }
            if (Randomizer.classData == null || Randomizer.classDataV == null)
            {
                text.AppendLine("class data not loaded, nothing to check");
                text.AppendLine();
                return;
            }

            int checkedSlots = 0;
            int vanillaHits = 0;
            int newHits = 0;

            // class ability slots, 5 per class
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)index;
                if (block + 5 >= Randomizer.classData.Length)
                    break;
                for (int slot = 0; slot < 5; slot++)
                {
                    int offset = (int)(block + slot);
                    ScanAbility(text, Randomizer.classData, Randomizer.classDataV, offset,
                        ref checkedSlots, ref vanillaHits, ref newHits);
                }
            }

            // monster latent / barrier abilities
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classMonsterStart + Randomizer.classMonsterLen * (long)index;
                if (block + 70 >= Randomizer.classData.Length)
                    break;
                int[] slots = { 64, 65, 66, 67, 68, 69 };
                for (int i = 0; i < slots.Length; i++)
                {
                    int offset = (int)(block + slots[i]);
                    ScanAbility(text, Randomizer.classData, Randomizer.classDataV, offset,
                        ref checkedSlots, ref vanillaHits, ref newHits);
                }
            }

            // character learnsets: 5 combat arts and 8 abilities per rank row
            if (Randomizer.personData != null && Randomizer.personDataV != null)
            {
                int rowCount = Randomizer.personData.Length / (int)Randomizer.charLearnLen;
                if (rowCount > 1201 * 12)
                    rowCount = 1201 * 12;
                for (int row = 0; row < rowCount; row++)
                {
                    long block = Randomizer.charLearnStart + Randomizer.charLearnLen * (long)row;
                    if (block + Randomizer.charLearnLen > Randomizer.personData.Length)
                        break;
                    // slots 0..4 hold combat arts, not abilities, so they are not
                    // checked here (the same id means something else there).
                    for (int slot = 11; slot < 19; slot++)
                        ScanAbility(text, Randomizer.personData, Randomizer.personDataV, (int)(block + slot),
                            ref checkedSlots, ref vanillaHits, ref newHits);
                }
            }

            text.AppendLine("ability slots scanned  : " + checkedSlots.ToString());
            text.AppendLine("blocked abilities found: " + (vanillaHits + newHits).ToString()
                + "  (" + vanillaHits.ToString() + " already in the vanilla file, "
                + newHits.ToString() + " new)");
            text.AppendLine("blocked abilities replaced by the safety pass: "
                + RandomizerExtras.statBlockedAbilitiesRemoved.ToString());
            if (newHits > 0)
            {
                text.AppendLine("FAIL  the randomization introduced blocked abilities, they are listed above");
                failures.Add(newHits.ToString() + " blocked ability pickup(s) were introduced by the randomization");
            }
            else
            {
                passes.Add("ability blocklist: no blocked ability was introduced");
                if (vanillaHits > 0)
                    text.AppendLine("note: the vanilla data already contains these on purpose (monster classes),"
                        + " they are not touched by the blocklist");
            }
            text.AppendLine();
        }

        private static void ScanAbility(StringBuilder text, byte[] data, byte[] vanilla, int offset,
            ref int checkedSlots, ref int vanillaHits, ref int newHits)
        {
            if (data == null || offset < 0 || offset >= data.Length)
                return;
            checkedSlots++;
            int ability = data[offset];
            if (!RandomizerExtras.blockedAbilities.Contains(ability))
                return;

            bool inVanilla = vanilla != null && offset < vanilla.Length && vanilla[offset] == ability;
            if (inVanilla)
            {
                vanillaHits++;
                return;
            }
            newHits++;
            if (newHits <= 25)
            {
                string name = ability < Randomizer.abilityNames.Length ? Randomizer.abilityNames[ability] : "?";
                text.AppendLine("  blocked ability at offset " + offset.ToString() + " : " + ability.ToString() + " " + name);
            }
        }

        // ----------------------------------------------------------- siege
        private static void AppendSiegeCheck(StringBuilder text, List<string> passes, List<string> warnings)
        {
            text.AppendLine("--- SIEGE / RANGE CHECK ----------------------------------------");
            if (!RandomizerExtras.limitSiegeTomes)
            {
                text.AppendLine("siege limit is off, nothing to check");
                text.AppendLine();
                return;
            }
            if (!Randomizer.itemLoaded || Randomizer.itemData == null)
            {
                text.AppendLine("item data not loaded, nothing to check");
                text.AppendLine();
                return;
            }

            int above = 0;
            for (int i = 0; i < Randomizer.obtainableWeapons.Length; i++)
                if (!RandomizerExtras.weaponRangeAllowed(Randomizer.obtainableWeapons[i]))
                    above++;
            text.AppendLine("obtainable weapons above range " + RandomizerExtras.maxEnemyWeaponRange.ToString()
                + " : " + above.ToString() + " (still in the game data, filtered from enemy pools)");

            int spellsAbove = 0;
            for (int spellId = 0; spellId < Randomizer.spellNames.Length; spellId++)
                if (!RandomizerExtras.spellRangeAllowed(spellId))
                    spellsAbove++;
            text.AppendLine("spells above range " + RandomizerExtras.maxEnemyWeaponRange.ToString()
                + " : " + spellsAbove.ToString()
                + " (replaced by " + RandomizerExtras.statSpellsFiltered.ToString() + " rerolls in this run)");

            if (Randomizer.baiSectionData == null || Randomizer.baiSectionDataV == null)
            {
                text.AppendLine("map unit data was not written in this run (randomized map weapons off)");
                text.AppendLine();
                return;
            }

            try
            {
                int scanned = 0;
                int offenders = 0;
                int spellOffenders = 0;
                List<string> samples = new List<string>();
                List<string> spellSamples = new List<string>();

                for (int scenario = 0; scenario < Randomizer.baiOffsets.Length; scenario++)
                {
                    int baiStart = (int)(Randomizer.baiOffsets[scenario] - Randomizer.baiSectionOffset);
                    if (baiStart < 0 || baiStart + 24 >= Randomizer.baiSectionData.Length)
                        break;

                    int[] routeStarts = new int[3];
                    routeStarts[0] = baiStart + 24 + BitConverter.ToInt32(Randomizer.baiSectionData, baiStart + 12);
                    routeStarts[1] = baiStart + 24 + BitConverter.ToInt32(Randomizer.baiSectionData, baiStart + 16);
                    routeStarts[2] = baiStart + 24 + BitConverter.ToInt32(Randomizer.baiSectionData, baiStart + 20);

                    for (int route = 0; route < 3; route++)
                    {
                        int unitTableStart = routeStarts[route];
                        for (int unit = 0; unit < Randomizer.baiUnitEntries; unit++)
                        {
                            int unitStart = unitTableStart + unit * Randomizer.baiUnitsLen;
                            if (unitStart + Randomizer.baiUnitsLen > Randomizer.baiSectionData.Length)
                                break;
                            if (BitConverter.ToUInt16(Randomizer.baiSectionData, unitStart) == 0)
                                continue;
                            scanned++;

                            for (int slot = 0; slot < 6; slot++)
                            {
                                int raw = BitConverter.ToUInt16(Randomizer.baiSectionData, unitStart + 6 + 2 * slot);
                                if (raw == 0)
                                    continue;

                                // stored as (general item id + 1); weapons sit in
                                // [itemTypeIdOffsets[0], itemTypeIdOffsets[1])
                                int generalId = raw - 1;
                                if (generalId < Randomizer.itemTypeIdOffsets[0]
                                    || generalId >= Randomizer.itemTypeIdOffsets[1])
                                    continue;
                                int weaponId = generalId - Randomizer.itemTypeIdOffsets[0];
                                if (RandomizerExtras.weaponRangeAllowed(weaponId))
                                    continue;
                                offenders++;
                                if (samples.Count < 15)
                                {
                                    int range = RandomizerExtras.weaponRange(weaponId);
                                    bool wasThere = false;
                                    int vanillaOffset = unitStart + 6 + 2 * slot;
                                    if (vanillaOffset + 2 <= Randomizer.baiSectionDataV.Length)
                                    {
                                        int vanillaRaw = BitConverter.ToUInt16(Randomizer.baiSectionDataV, vanillaOffset);
                                        if (vanillaRaw == raw)
                                            wasThere = true;
                                    }
                                    samples.Add("  scenario " + scenario.ToString() + " unit " + unit.ToString()
                                        + " : weapon " + weaponId.ToString() + " range " + range.ToString()
                                        + (wasThere ? " (same as vanilla)" : " (NEW)"));
                                }
                            }

                            // the two enemy spell slots, stored as spell index + 1
                            for (int slot = 36; slot <= 37; slot++)
                            {
                                int rawSpell = Randomizer.baiSectionData[unitStart + slot];
                                if (rawSpell == 0)
                                    continue;
                                int spellId = rawSpell - 1;
                                if (RandomizerExtras.spellRangeAllowed(spellId))
                                    continue;
                                spellOffenders++;
                                if (spellSamples.Count < 10)
                                    spellSamples.Add("  scenario " + scenario.ToString() + " unit " + unit.ToString()
                                        + " : spell " + spellId.ToString() + " range "
                                        + RandomizerExtras.spellRange(spellId).ToString());
                            }
                        }
                    }
                }

                text.AppendLine("map units scanned          : " + scanned.ToString());
                text.AppendLine("out of range spell slots   : " + spellOffenders.ToString());
                foreach (string spellSample in spellSamples)
                    text.AppendLine(spellSample);
                text.AppendLine("out of range weapon slots  : " + offenders.ToString()
                    + "   (bypass chance " + RandomizerExtras.siegeChance.ToString() + "%, bypass rolls won "
                    + RandomizerExtras.statSiegeBypassRolls.ToString() + ")");
                foreach (string sample in samples)
                    text.AppendLine(sample);

                if (RandomizerExtras.siegeChance <= 0 && offenders > 0)
                    warnings.Add("out of range weapons on map units while the bypass chance is 0%");
                else
                    passes.Add("siege limit: map unit weapons are within range (or a bypass roll allowed them)");
            }
            catch (Exception exception)
            {
                text.AppendLine("map unit scan failed: " + exception.Message);
                warnings.Add("the map unit range scan could not complete");
            }
            text.AppendLine();
        }

        // ---------------------------------------------------- growth shift
        private static void AppendGrowthShiftCheck(StringBuilder text, List<string> passes, List<string> warnings)
        {
            text.AppendLine("--- GROWTH SHIFT CHECK -----------------------------------------");
            if (RandomizerExtras.growthShift <= 0)
            {
                text.AppendLine("growth shift is 0, nothing to check");
                text.AppendLine();
                return;
            }
            if (Randomizer.classData == null || Randomizer.classDataV == null)
            {
                text.AppendLine("class data not loaded, nothing to check");
                text.AppendLine();
                return;
            }

            int[] playerBefore = new int[9];
            int[] playerAfter = new int[9];
            int[] enemyBefore = new int[9];
            int[] enemyAfter = new int[9];
            int changed = 0;

            int[] playerOffsets = { 20, 21, 22, 23, 24, 25, 26, 27, 29 };
            int[] enemyOffsets = { 19, 30, 31, 32, 33, 34, 35, 36, 38 };

            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 40 >= Randomizer.classData.Length)
                    break;

                bool classChanged = false;
                for (int stat = 0; stat < 9; stat++)
                {
                    int playerOffset = (int)(block + playerOffsets[stat]);
                    int enemyOffset = (int)(block + enemyOffsets[stat]);
                    if (playerOffset >= Randomizer.classData.Length)
                        break;
                    playerBefore[stat] += Randomizer.classDataV[playerOffset];
                    playerAfter[stat] += Randomizer.classData[playerOffset];
                    enemyBefore[stat] += Randomizer.classDataV[enemyOffset];
                    enemyAfter[stat] += Randomizer.classData[enemyOffset];
                    if (Randomizer.classData[enemyOffset] != Randomizer.classDataV[enemyOffset])
                        classChanged = true;
                }
                if (classChanged)
                    changed++;
            }

            text.AppendLine("classes with changed enemy growths: " + changed.ToString()
                + "  (counter says " + RandomizerExtras.statGrowthShiftedClasses.ToString() + ")");
            text.AppendLine("stat      enemy growth total  before -> after   |  player before -> after");
            for (int stat = 0; stat < 9; stat++)
                text.AppendLine(statNames[stat].PadRight(8)
                    + enemyBefore[stat].ToString().PadLeft(10) + " -> " + enemyAfter[stat].ToString().PadLeft(6)
                    + "   |  " + playerBefore[stat].ToString().PadLeft(6) + " -> " + playerAfter[stat].ToString().PadLeft(6));

            if (enemyAfter[0] > enemyBefore[0])
                passes.Add("growth shift: enemy HP growth total rose, Str/Mag/Def/Res fell");
            else
                warnings.Add("growth shift ran but the enemy HP growth total did not rise");
            text.AppendLine();
        }

        // ---------------------------------------------------------- summary
        private static void AppendSummary(StringBuilder text, List<string> passes,
            List<string> warnings, List<string> failures)
        {
            text.AppendLine("--- SUMMARY ----------------------------------------------------");
            text.AppendLine("passed  : " + passes.Count.ToString());
            text.AppendLine("warnings: " + warnings.Count.ToString());
            text.AppendLine("failures: " + failures.Count.ToString());
            foreach (string pass in passes)
                text.AppendLine("  PASS  " + pass);
            foreach (string warning in warnings)
                text.AppendLine("  WARN  " + warning);
            foreach (string failure in failures)
                text.AppendLine("  FAIL  " + failure);
            text.AppendLine();
            text.AppendLine("If anything says FAIL or a new option looks like it did nothing,");
            text.AppendLine("send this file along with the build and it can be checked quickly.");
            text.AppendLine();
        }

        private static void AppendSettingsDump(StringBuilder text)
        {
            text.AppendLine("--- FULL SETTINGS DUMP -----------------------------------------");
            try
            {
                if (Randomizer.settingsForDebug != null)
                    text.AppendLine(ObjectDumper.Dump(Randomizer.settingsForDebug, 0));
                else
                    text.AppendLine("(settings object not available)");
            }
            catch (Exception exception)
            {
                text.AppendLine("(settings dump failed: " + exception.Message + ")");
            }
        }
    }
}
