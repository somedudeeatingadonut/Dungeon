// -----------------------------------------------------------------------------
//  ModSupport.cs
//
//  Support for total conversion mods (the reference case is "Three Houses: No
//  Monastery").
//
//  Everything in here was checked against that mod's own fixed_*.bin files, not
//  against its feature list:
//
//   * its class mastery EXP requirements are a 0 / 20 / 50 / 100 / 150 / 200
//     scale (Noble and Commoner are 200, the Aptitude rework). The base
//     randomizer would overwrite those with random values, which is why mod mode
//     keeps them.
//
//   * its Dancer is not the class in row 43 any more - the only row holding
//     ability 158 (Dance) is row 71. The base randomizer hardcodes 43 when it
//     puts Dance back, so in mod mode the row is looked up in the loaded data
//     instead.
//
//   * the mod's data files are slightly larger than the original ones
//     (fixed_classdata.bin +144 bytes, fixed_data.bin +648 = exactly 36 combat
//     arts of 18 bytes, fixed_persondata.bin +6320, and so on). Whether those
//     extra bytes were appended at the end - which keeps every fixed offset
//     valid - or inserted in the middle - which would shift every offset - can
//     only be answered by comparing with the original file, so the analysis
//     reports the deltas and says so instead of guessing.
//
//  Mod mode is off by default and changes nothing unless it is switched on.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class ModSupport
    {
        // ------------------------------------------------------------- state
        public static bool modMode = false;
        public static bool keepMasteryExp = true;

        // ---------------------------------------------------------- counters
        public static int statMasteryExpRestored;
        public static int statClassesAnalysed;

        public static void resetCounters()
        {
            statMasteryExpRestored = 0;
            statClassesAnalysed = 0;
        }

        // ------------------------------------------------------ option table
        // Options that fight a total conversion, with the reason. Used by the
        // preset builder and by the report.
        private static readonly string[,] conflictTable = new string[,]
        {
            { "randClassMasteryExpReq",      "class mastery EXP requirements - the mod uses its own 0/20/50/100/150/200 scale" },
            { "randMonClassMasteryExpReq",   "monster class mastery EXP requirements - the mod defines these" },
            { "randStaClassMasteryExpReq",   "beginner class mastery EXP requirements" },
            { "randBegClassMasteryExpReq",   "beginner class mastery EXP requirements - beginner certification is gone" },
            { "randClassMasteryAb",         "mastery abilities - the mod hands them out by skill rank" },
            { "randClassMasteryCa",         "mastery combat arts - the mod hands them out by skill rank" },
            { "changeLv",                   "level scaling - the mod already retunes enemy levels per difficulty" },
            { "changeMsg",                  "message text - the mod ships its own dialogue" },
            { "changeScr",                  "script text - the mod ships its own dialogue and events" },
            { "changeGwscr",                "group work script text - there is no monastery" },
            { "changeTuscr",                "teaching script text - there is no weekly teaching" },
            { "changeBtlscr",               "battle script text - the mod rewrites battle events" },
            { "randActivityPointCount",     "activity points - exploration is disabled" },
            { "randShopPrices",             "shop prices - the mod rebalances the shops" },
            { "randWeaponShops",            "weapon shop stock - the mod changes what is for sale" },
            { "randItemShops",              "item shop stock - the mod changes what is for sale" },
            { "randBattalionShops",         "battalion guild stock - the mod unlocks it in chapter 3" },
            { "randAnnaShop",               "Anna's shop - the mod unlocks it at the start of part 2" },
            { "randQuestRewards",           "quest rewards - the mod rewrites the quests" },
            { "randGiftPrefs",              "gift preferences - no monastery" },
            { "randMeal",                   "meals - no dining hall" },
            { "randMealPrefs",              "meal preferences - no dining hall" },
            { "randCookingProfs",           "cooking - no dining hall" },
            { "randChoirProfs",             "choir - no cathedral" },
            { "randFishingItems",           "fishing - no fishing pond" },
            { "randGardeningItems",         "gardening - no greenhouse" },
            { "randCultivationCosts",       "cultivation - no greenhouse" },
            { "randSaintStatueCosts",       "saint statue costs - no monastery" },
            { "randSaintStatuePerks",       "saint statue perks - no monastery" },
            { "randInfluencerCosts",        "influencer costs - no monastery" },
            { "randGroupTaskGold",          "group task gold - no monastery" },
            { "randGroupTaskSkills",        "group task skills - no monastery" },
            { "randGroupTaskItems",         "group task items - no monastery" },
            { "randGroupTaskProfs",         "group task proficiencies - no monastery" },
            { "randChoirEvent",             "choir events - no cathedral" },
            { "randGardenEvent",            "greenhouse events - no greenhouse" },
            { "randFishingEvent",           "fishing events - no fishing pond" },
            { "randCafeteriaEvent",         "cafeteria events - no dining hall" }
        };

        public static bool isConflictKey(string key)
        {
            for (int i = 0; i < conflictTable.GetLength(0); i++)
                if (string.Equals(conflictTable[i, 0], key, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public static List<string> enabledConflicts(Settings settings)
        {
            List<string> found = new List<string>();
            if (settings == null)
                return found;
            for (int i = 0; i < conflictTable.GetLength(0); i++)
            {
                System.Reflection.FieldInfo field = typeof(Settings).GetField(conflictTable[i, 0],
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field == null || field.FieldType != typeof(bool))
                    continue;
                try
                {
                    if ((bool)field.GetValue(settings))
                        found.Add(conflictTable[i, 0] + " : " + conflictTable[i, 1]);
                }
                catch
                {
                }
            }
            return found;
        }

        // ------------------------------------------------------ the Dancer row
        // The base randomizer puts Dance back on class 43. The mod's class rows
        // do not line up with that any more, so the row is looked up instead:
        // the one row whose ability slots hold 158. If that is ambiguous, 43 is
        // kept.
        private static int dancerClassIndex = 43;
        private static bool dancerLookedUp;

        public static int DancerClassIndex
        {
            get
            {
                if (!dancerLookedUp)
                    lookupDancerRow();
                return dancerClassIndex;
            }
        }

        private static void lookupDancerRow()
        {
            dancerLookedUp = true;
            dancerClassIndex = 43;
            if (Randomizer.classDataV == null)
                return;

            int found = -1;
            int count = 0;
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)index;
                if (block + 5 >= Randomizer.classDataV.Length)
                    break;
                for (int slot = 0; slot < 5; slot++)
                {
                    if (Randomizer.classDataV[block + slot] != 158)
                        continue;
                    count++;
                    found = index;
                }
            }
            if (count == 1 && found >= 0)
                dancerClassIndex = found;
        }

        // -------------------------------------------------- the mastery pass
        // Class block +74 is the mastery requirement. Only the rows the base
        // randomizer writes are considered, and only values it changed.
        public static void applyAfterRandomization()
        {
            resetCounters();
            if (!modMode || !keepMasteryExp)
                return;
            if (Randomizer.classData == null || Randomizer.classDataV == null)
                return;

            for (int index = 0; index < 100; index++)
            {
                if (!(index < 88 || index == 91))
                    continue;
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index + 74L;
                if (block >= Randomizer.classData.Length || block >= Randomizer.classDataV.Length)
                    break;
                if (Randomizer.classData[block] == Randomizer.classDataV[block])
                    continue;
                Randomizer.classData[block] = Randomizer.classDataV[block];
                statMasteryExpRestored++;
            }

            if (statMasteryExpRestored > 0 && Randomizer.changelog != null)
            {
                Randomizer.changelog += "\n\n\n---MOD COMPATIBILITY---";
                Randomizer.changelog += "\nClass mastery EXP requirements kept at the mod's values: "
                    + statMasteryExpRestored.ToString();
            }
        }

        // --------------------------------------------------------- analysis
        // The original sizes of every file the randomizer loads. Anything bigger
        // or smaller means the mod replaced that file.
        private static readonly string[,] originalSizes = new string[,]
        {
            { "fixed_classdata.bin", "23812" },
            { "fixed_persondata.bin", "139456" },
            { "fixed_data.bin", "39000" },
            { "fixed_act_data.bin", "172732" },
            { "fixed_calendardata.bin", "14696" },
            { "fixed_lobby_common.bin", "44616" },
            { "fixed_growthdata.bin", "1404" },
            { "fixed_lobby_activity.bin", "16504" },
            { "fixed_groupworkdata.bin", "4432" },
            { "fixed_presentdata.bin", "6180" },
            { "fixed_shopdata.bin", "18676" },
            { "fixed_lobby_quest.bin", "8672" },
            { "fixed_scenario.bin", "4784" }
        };

        private static long originalSize(string name)
        {
            for (int i = 0; i < originalSizes.GetLength(0); i++)
                if (string.Equals(originalSizes[i, 0], name, StringComparison.Ordinal))
                    return long.Parse(originalSizes[i, 1]);
            return -1;
        }

        private sealed class LoadedFile
        {
            public string Name;
            public bool Loaded;
            public string Path;
            public byte[] Data;

            public LoadedFile(string name, bool loaded, string path, byte[] data)
            {
                this.Name = name;
                this.Loaded = loaded;
                this.Path = path;
                this.Data = data;
            }
        }

        private static List<LoadedFile> loadedFiles()
        {
            List<LoadedFile> list = new List<LoadedFile>();
            list.Add(new LoadedFile("fixed_classdata.bin", Randomizer.classLoaded, Randomizer.classFilename, Randomizer.classDataV));
            list.Add(new LoadedFile("fixed_persondata.bin", Randomizer.personLoaded, Randomizer.personFilename, Randomizer.personDataV));
            list.Add(new LoadedFile("fixed_data.bin", Randomizer.itemLoaded, Randomizer.itemFilename, Randomizer.itemDataV));
            list.Add(new LoadedFile("fixed_act_data.bin", Randomizer.actLoaded, Randomizer.actFilename, Randomizer.actDataV));
            list.Add(new LoadedFile("fixed_calendardata.bin", Randomizer.calendarLoaded, Randomizer.calendarFilename, Randomizer.calendarDataV));
            list.Add(new LoadedFile("fixed_lobby_common.bin", Randomizer.lobbyLoaded, Randomizer.lobbyFilename, Randomizer.lobbyDataV));
            list.Add(new LoadedFile("fixed_growthdata.bin", Randomizer.growthdataLoaded, Randomizer.growthdataFilename, Randomizer.growthdataDataV));
            list.Add(new LoadedFile("fixed_lobby_activity.bin", Randomizer.activityLoaded, Randomizer.activityFilename, Randomizer.activityDataV));
            list.Add(new LoadedFile("fixed_groupworkdata.bin", Randomizer.groupWorkLoaded, Randomizer.groupWorkFilename, Randomizer.groupWorkDataV));
            list.Add(new LoadedFile("fixed_presentdata.bin", Randomizer.presentLoaded, Randomizer.presentFilename, Randomizer.presentDataV));
            list.Add(new LoadedFile("fixed_shopdata.bin", Randomizer.shopLoaded, Randomizer.shopFilename, Randomizer.shopDataV));
            list.Add(new LoadedFile("fixed_lobby_quest.bin", Randomizer.questLoaded, Randomizer.questFilename, Randomizer.questDataV));
            list.Add(new LoadedFile("fixed_scenario.bin", Randomizer.scenarioLoaded, Randomizer.scenarioFilename, Randomizer.scenarioDataV));
            return list;
        }

        // What the loaded files look like compared with the original data, plus
        // the class table checks. Returns null when nothing is loaded.
        public static List<string> analyse()
        {
            List<string> lines = null;
            try
            {
                lines = new List<string>();
                List<LoadedFile> files = loadedFiles();
                int replaced = 0;
                int loaded = 0;
                for (int i = 0; i < files.Count; i++)
                {
                    if (!files[i].Loaded)
                        continue;
                    loaded++;
                    long original = originalSize(files[i].Name);
                    long actual = files[i].Data == null ? -1 : files[i].Data.Length;
                    if (original < 0 || actual < 0)
                    {
                        lines.Add(files[i].Name + " : loaded, size unknown");
                        continue;
                    }
                    long delta = actual - original;
                    if (delta == 0)
                        lines.Add(files[i].Name + " : same size as the original (" + original.ToString() + " bytes)");
                    else
                    {
                        replaced++;
                        lines.Add(files[i].Name + " : " + actual.ToString() + " bytes, "
                            + (delta > 0 ? "+" : "") + delta.ToString() + " vs the original "
                            + original.ToString());
                        if (delta % 18 == 0 && files[i].Name == "fixed_data.bin")
                            lines.Add("    " + (delta / 18).ToString()
                                + " extra combat art row(s) of 18 bytes - appended at the end is safe, "
                                + "inserted in the middle would move every offset after it");
                    }
                }
                if (loaded == 0)
                    return null;

                lines.Add("");
                lines.Add("files that differ from the original data: " + replaced.ToString()
                    + " of " + loaded.ToString());
                if (replaced == 0)
                    lines.Add("    nothing here came from a mod - if a total conversion is installed, the");
                if (replaced == 0)
                    lines.Add("    files above were most likely taken from the game dump instead");

                List<string> table = analyseClassTable();
                if (table.Count > 0)
                {
                    lines.Add("");
                    lines.Add("class table");
                    for (int i = 0; i < table.Count; i++)
                        lines.Add("    " + table[i]);
                }
            }
            catch (Exception exception)
            {
                if (lines != null)
                    lines.Add("analysis failed: " + exception.Message);
            }
            return lines;
        }

        private static List<string> analyseClassTable()
        {
            List<string> lines = new List<string>();
            byte[] data = Randomizer.classDataV;
            if (data == null)
                return lines;

            int rows = 0;
            int minTier = 255;
            int maxTier = 0;
            int monsters = 0;
            List<int> masteryValues = new List<int>();
            List<int> extraRows = new List<int>();

            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 94 >= data.Length)
                    break;
                rows++;
                int tier = data[block + 69];
                if (tier < minTier)
                    minTier = tier;
                if (tier > maxTier)
                    maxTier = tier;
                if (data[block + 73] != 255)
                    monsters++;
                int mastery = data[block + 74];
                if (!masteryValues.Contains(mastery))
                    masteryValues.Add(mastery);
                if (index >= 88 && index != 91 && rowLooksUsed(data, (int)block))
                    extraRows.Add(index);
            }

            statClassesAnalysed = rows;
            lines.Add("rows read: " + rows.ToString()
                + ", tier values " + minTier.ToString() + " to " + maxTier.ToString()
                + ", monster rows: " + monsters.ToString());

            masteryValues.Sort();
            StringBuilder mastery = new StringBuilder();
            for (int i = 0; i < masteryValues.Count && i < 10; i++)
            {
                if (i > 0)
                    mastery.Append(", ");
                mastery.Append(masteryValues[i].ToString());
            }
            lines.Add("mastery EXP values in use: " + mastery.ToString()
                + "   (the base randomizer would replace these)");

            lines.Add("Dancer row: " + DancerClassIndex.ToString()
                + (DancerClassIndex == 43 ? "  (default)" : "  (looked up: the only row holding Dance)"));

            if (extraRows.Count > 0)
            {
                StringBuilder rowsText = new StringBuilder();
                for (int i = 0; i < extraRows.Count; i++)
                {
                    if (i > 0)
                        rowsText.Append(", ");
                    rowsText.Append(extraRows[i].ToString());
                }
                lines.Add("populated class rows outside the randomized range (88..99, except 91): "
                    + rowsText.ToString());
                lines.Add("    those rows are left alone on purpose - a class a mod added there keeps");
                lines.Add("    its own stats, growths and abilities");
            }
            return lines;
        }

        private static bool rowLooksUsed(byte[] data, int block)
        {
            if (data[block + 73] != 255)
                return true;
            if (data[block + 69] != 255)
                return true;
            int[] bases = { 75, 85, 86, 87, 88, 89, 90, 91, 93 };
            for (int i = 0; i < bases.Length; i++)
                if (data[block + bases[i]] != 0)
                    return true;
            int[] growths = { 20, 21, 22, 23, 24, 25, 26, 27, 29 };
            for (int i = 0; i < growths.Length; i++)
                if (data[block + growths[i]] != 0)
                    return true;
            return false;
        }

        // ----------------------------------------------------------- report
        public static void appendReport(StringBuilder text, List<string> passes, List<string> warnings)
        {
            text.AppendLine("--- MOD COMPATIBILITY -----------------------------------------");
            text.AppendLine("Mod mode                       : " + modMode);
            text.AppendLine("Keep class mastery EXP         : " + keepMasteryExp
                + "   (restored " + statMasteryExpRestored.ToString() + " requirement(s))");

            if (!modMode)
            {
                text.AppendLine("note: off. Switch it on in Extra options > Total conversion mod when the");
                text.AppendLine("      game runs a mod such as No Monastery, so the mastery EXP values and");
                text.AppendLine("      the Dancer row are taken from the loaded data instead of the originals.");
                text.AppendLine();
                return;
            }

            List<string> analysis = analyse();
            if (analysis == null)
                text.AppendLine("no files loaded yet, nothing to analyse");
            else
                for (int i = 0; i < analysis.Count; i++)
                    text.AppendLine(analysis[i]);

            List<string> conflicts = enabledConflicts(Randomizer.settingsForDebug);
            if (conflicts.Count == 0)
                passes.Add("mod mode: no enabled option fights the mod");
            else
            {
                text.AppendLine("enabled options that fight the mod: " + conflicts.Count.ToString());
                for (int i = 0; i < conflicts.Count; i++)
                    text.AppendLine("    " + conflicts[i]);
                warnings.Add(conflicts.Count.ToString()
                    + " enabled option(s) conflict with the mod - apply the \"No Monastery (mod safe)\" preset");
            }
            text.AppendLine();
        }
    }
}
