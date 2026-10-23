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
//   * the mod's class table has one more row than the original (101 against
//     100) and its Dance ability (158) sits in the third ability slot of the
//     Dancer row instead of the first. The base randomizer hardcodes class 43
//     and writes Dance into the first slot, so in mod mode the row is looked up
//     by scanning the ability slots of the loaded data instead of being assumed.
//
//   * the mod's data files are larger than the original ones, and the extra
//     bytes are NOT all appended at the end: each file's header lists where its
//     sub-tables start, and those starts are the authority for how far a table
//     moved. Comparing that list with the original file gives the shift of every
//     section (fixed_classdata.bin +118 .. +144, fixed_data.bin 0 .. +648,
//     fixed_persondata.bin 0 .. +6320). The randomizer's table constants are
//     moved by those shifts before anything is written; without the original
//     files the map cannot be built and the analysis says so.
//
//  Mod mode is off by default and changes nothing unless it is switched on.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class ModSupport
    {
        // ------------------------------------------------------------- state
        public static bool modMode = false;
        public static bool keepMasteryExp = true;

        // Folder holding the unmodified game files. When it is set, the offset
        // map below is built from them; without it the map cannot be verified.
        public static string originalFolder = "";

        // Result of the last readOffsets() call, for the tab and the report.
        public static string offsetsStatus = "not read yet";
        public static int statOffsetsApplied;

        // ---------------------------------------------------------- counters
        public static int statMasteryExpRestored;
        public static int statClassesAnalysed;

        public static void resetCounters()
        {
            statMasteryExpRestored = 0;
            statClassesAnalysed = 0;
            statOffsetsApplied = 0;
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

        // ------------------------------------------------------ offset maps
        // Every fixed_*.bin starts with a directory of its sub-table starts:
        //   u32[0] = number of sub-tables, u32[1] = size of the first one,
        //   u32[2] = start of the first, u32[3] = start of the second, then
        //   u32[5], u32[7], ... for the rest.
        // A total conversion mod adds rows to some of those sub-tables, which
        // moves every sub-table after it - by a different amount in each file
        // and each section. Comparing the directory of the mod's file with the
        // original one gives that shift per section, which is the only way to
        // know where a table really is.
        private sealed class OffsetMap
        {
            public string FileName;
            public int[] Original;
            public int[] Modded;
            public bool Valid;
            public string Problem;
            // set when the map came from a measured, built-in profile instead of
            // from an original file the user supplied
            public bool FromProfile;
            public string ProfileName;

            public int Count
            {
                get { return this.Original == null ? 0 : this.Original.Length; }
            }

            public int map(int originalOffset)
            {
                if (!this.Valid || this.Original == null || this.Modded == null)
                    return originalOffset;
                int delta = 0;
                for (int i = 0; i < this.Original.Length; i++)
                {
                    if (this.Original[i] > originalOffset)
                        break;
                    delta = this.Modded[i] - this.Original[i];
                }
                return originalOffset + delta;
            }

            public int deltaAt(int originalOffset)
            {
                return map(originalOffset) - originalOffset;
            }
        }

        // One loaded data file, the randomizer fields that point into it, and
        // the map between its original and its loaded layout.
        private sealed class TableGroup
        {
            public string FileName;
            public Func<bool> IsLoaded;
            public Func<byte[]> Data;
            public string[] Fields;
            public string[] Labels;
            public OffsetMap Map;
        }

        private static List<TableGroup> groups;

        private static void addGroup(string fileName, Func<bool> isLoaded, Func<byte[]> data,
            string[] fields, string[] labels)
        {
            TableGroup group = new TableGroup();
            group.FileName = fileName;
            group.IsLoaded = isLoaded;
            group.Data = data;
            group.Fields = fields;
            group.Labels = labels;
            groups.Add(group);
        }

        private static string[] textOf(params string[] items)
        {
            return items;
        }

        private static List<TableGroup> buildGroups()
        {
            groups = new List<TableGroup>();

            addGroup("fixed_classdata.bin",
                delegate { return Randomizer.classLoaded; },
                delegate { return Randomizer.classDataV; },
                textOf("classBlockStart", "classCertStart", "classTierStart", "classAbStart",
                    "classMonsterStart"),
                textOf("class rows", "certifications", "tiers", "abilities", "monster data"));

            addGroup("fixed_data.bin",
                delegate { return Randomizer.itemLoaded; },
                delegate { return Randomizer.itemDataV; },
                textOf("weaponBlockStart", "magicBlockStart", "turretBlockStart", "gambitBlockStart",
                    "blowBlockStart", "artBlockStart", "gambitExtraStart", "battalionBlockStart"),
                textOf("weapons", "spells", "turrets", "gambits", "blow data", "combat arts",
                    "gambit extras", "battalions"));

            addGroup("fixed_persondata.bin",
                delegate { return Randomizer.personLoaded; },
                delegate { return Randomizer.personDataV; },
                textOf("charBlockStart", "charWepRankStart", "charSpellStart", "charSkillStart",
                    "charWepStart", "charArtStart", "charSupStart", "charSpeSupStart",
                    "charBudStart", "charLearnStart", "charFacultyStart", "charSeminarStart",
                    "charGoalStart", "charPortraitStart", "charEnemySkillStart"),
                textOf("characters", "weapon ranks", "spell lists", "skills", "starting inventory",
                    "combat arts", "support bonuses", "special support bonuses", "budding talents",
                    "learnsets", "faculty training", "seminars", "goals", "portraits",
                    "enemy skills"));

            addGroup("fixed_act_data.bin",
                delegate { return Randomizer.actLoaded; },
                delegate { return Randomizer.actDataV; },
                textOf("classBaseAniStart", "classAniSetStart"),
                textOf("base animation sets", "animation sets"));

            addGroup("fixed_shopdata.bin",
                delegate { return Randomizer.shopLoaded; },
                delegate { return Randomizer.shopDataV; },
                textOf("weaponShopStart", "equipmentShopStart", "itemShopStart", "battalionShopStart",
                    "forgingStart", "statueStart", "merchantStart", "annaShopStart",
                    "influencerStart", "paganWeaponStart", "paganEquipmentStart", "paganItemStart"),
                textOf("weapon shop", "equipment shop", "item shop", "battalion shop", "forging",
                    "statues", "merchant", "Anna's shop", "influencer", "pagan weapons",
                    "pagan equipment", "pagan items"));

            addGroup("fixed_lobby_common.bin",
                delegate { return Randomizer.lobbyLoaded; },
                delegate { return Randomizer.lobbyDataV; },
                textOf("reqruitStart", "teaResponseStart", "teaTopicStart"),
                textOf("recruitment", "tea responses", "tea topics"));

            addGroup("fixed_lobby_activity.bin",
                delegate { return Randomizer.activityLoaded; },
                delegate { return Randomizer.activityDataV; },
                textOf("charActivityStart", "mealStart", "mealPrefStart", "fishingStart",
                    "gardenStart", "cultivationStart", "profLvStart"),
                textOf("activities", "meals", "meal preferences", "fishing", "garden",
                    "cultivation", "professor level"));

            addGroup("fixed_growthdata.bin",
                delegate { return Randomizer.growthdataLoaded; },
                delegate { return Randomizer.growthdataDataV; },
                textOf("expStart"),
                textOf("EXP values"));

            addGroup("fixed_groupworkdata.bin",
                delegate { return Randomizer.groupWorkLoaded; },
                delegate { return Randomizer.groupWorkDataV; },
                textOf("groupTaskStart", "groupTaskProfsStart"),
                textOf("group tasks", "group task proficiencies"));

            addGroup("fixed_presentdata.bin",
                delegate { return Randomizer.presentLoaded; },
                delegate { return Randomizer.presentDataV; },
                textOf("giftStart", "giftPrefsStart"),
                textOf("gifts", "gift preferences"));

            addGroup("fixed_lobby_quest.bin",
                delegate { return Randomizer.questLoaded; },
                delegate { return Randomizer.questDataV; },
                textOf("questStart", "annaQuestStart"),
                textOf("quests", "Anna's quests"));

            addGroup("fixed_scenario.bin",
                delegate { return Randomizer.scenarioLoaded; },
                delegate { return Randomizer.scenarioDataV; },
                textOf("scenarioStart"),
                textOf("scenario text"));

            return groups;
        }

        private static List<TableGroup> tableGroups()
        {
            if (groups == null)
                buildGroups();
            return groups;
        }

        // ------------------------------------------------- built-in profiles
        // The same map, measured once from a known mod release and stored here,
        // so that release needs no originals folder. A profile is only used when
        // the loaded file's own header matches the stored directory exactly - a
        // different build that happens to have the same size is not trusted.
        private sealed class BuiltInProfile
        {
            public string Name;
            public string FileName;
            public int LoadedSize;
            public int[] Original;
            public int[] Modded;
        }

        private static readonly BuiltInProfile[] builtInProfiles = new BuiltInProfile[]
        {
            new BuiltInProfile
            {
                Name = "No Monastery 1.3.3",
                FileName = "fixed_classdata.bin",
                LoadedSize = 23956,
                Original = new int[] { 11864, 11948, 13512, 13584, 14148, 16612, 21476,
                    22180, 23364, 23508 },
                Modded = new int[] { 11982, 12068, 13648, 13720, 14292, 16756, 21620,
                    22324, 23508, 23652 }
            },
            new BuiltInProfile
            {
                Name = "No Monastery 1.3.3",
                FileName = "fixed_data.bin",
                LoadedSize = 39648,
                Original = new int[] { 12064, 12196, 13172, 13308, 15292, 16796, 18060,
                    22924, 24428, 25532, 25776, 26224, 37692, 37876, 38300, 38924 },
                Modded = new int[] { 12064, 12196, 13460, 13596, 15580, 17084, 18348,
                    23212, 25076, 26180, 26424, 26872, 38340, 38524, 38948, 39572 }
            },
            new BuiltInProfile
            {
                Name = "No Monastery 1.3.3",
                FileName = "fixed_persondata.bin",
                LoadedSize = 145776,
                Original = new int[] { 96144, 96292, 107156, 108148, 109744, 110708,
                    113564, 114260, 115676, 116868, 117624, 120928, 122432, 122760,
                    123132, 123504, 124528, 138992 },
                Modded = new int[] { 96144, 96292, 107156, 108148, 109812, 110816,
                    113796, 114520, 115996, 117188, 123944, 127248, 128752, 129080,
                    129452, 129824, 130848, 145312 }
            }
        };

        // A profile for this file, but only if the loaded file's header is
        // exactly the one that was measured.
        private static OffsetMap profileMap(TableGroup group)
        {
            byte[] data = group.Data();
            if (data == null)
                return null;

            for (int i = 0; i < builtInProfiles.Length; i++)
            {
                BuiltInProfile profile = builtInProfiles[i];
                if (profile.FileName != group.FileName || profile.LoadedSize != data.Length)
                    continue;

                int count;
                List<int> starts = sectionStarts(data, out count);
                if (starts.Count != profile.Modded.Length || count != starts.Count)
                    continue;

                bool same = true;
                for (int k = 0; k < starts.Count; k++)
                {
                    if (starts[k] != profile.Modded[k])
                    {
                        same = false;
                        break;
                    }
                }
                if (!same)
                    continue;

                OffsetMap map = new OffsetMap();
                map.FileName = group.FileName;
                map.Original = profile.Original;
                map.Modded = profile.Modded;
                map.Valid = true;
                map.FromProfile = true;
                map.ProfileName = profile.Name;
                return map;
            }
            return null;
        }

        // Reads the sub-table directory out of a fixed_*.bin header.
        private static List<int> sectionStarts(byte[] data, out int count)
        {
            List<int> starts = new List<int>();
            count = 0;
            if (data == null || data.Length < 16)
                return starts;

            count = BitConverter.ToInt32(data, 0);
            int wordCount = data.Length / 4;
            if (count <= 0 || count > 64)
            {
                count = 0;
                return starts;
            }

            starts.Add(BitConverter.ToInt32(data, 8));
            if (wordCount > 3)
                starts.Add(BitConverter.ToInt32(data, 12));
            for (int k = 1; ; k++)
            {
                int index = 3 + 2 * k;
                if (index + 1 > wordCount - 1)
                    break;
                int value = BitConverter.ToInt32(data, index * 4);
                if (value <= starts[starts.Count - 1] || value >= data.Length)
                    break;
                starts.Add(value);
            }
            return starts;
        }

        private static OffsetMap buildMap(string fileName, byte[] original, byte[] modded)
        {
            OffsetMap map = new OffsetMap();
            map.FileName = fileName;
            if (original == null || modded == null)
            {
                map.Problem = "no original file to compare with";
                return map;
            }

            int originalCount;
            int moddedCount;
            List<int> originalStarts = sectionStarts(original, out originalCount);
            List<int> moddedStarts = sectionStarts(modded, out moddedCount);

            if (originalCount == 0 || moddedCount == 0)
            {
                map.Problem = "the sub-table directory could not be read";
                return map;
            }
            if (originalCount != moddedCount || originalStarts.Count != moddedStarts.Count)
            {
                map.Problem = "different number of sub-tables (" + originalCount.ToString()
                    + " original, " + moddedCount.ToString() + " loaded)";
                return map;
            }

            map.Original = originalStarts.ToArray();
            map.Modded = moddedStarts.ToArray();
            map.Valid = true;
            return map;
        }

        private static byte[] readFile(string folder, string name)
        {
            if (folder == null || folder.Trim().Length == 0)
                return null;
            try
            {
                string path = System.IO.Path.Combine(folder.Trim(), name);
                if (System.IO.File.Exists(path))
                    return System.IO.File.ReadAllBytes(path);
            }
            catch
            {
            }
            return null;
        }

        // Builds the maps from the original files. Returns false when nothing
        // could be read.
        public static bool readOffsets()
        {
            List<TableGroup> list = tableGroups();
            int valid = 0;
            int attempted = 0;
            int missing = 0;
            int fromProfile = 0;
            for (int i = 0; i < list.Count; i++)
            {
                TableGroup group = list[i];
                group.Map = null;
                if (!group.IsLoaded())
                    continue;
                byte[] original = readFile(originalFolder, group.FileName);
                if (original == null)
                {
                    group.Map = profileMap(group);
                    if (group.Map != null)
                    {
                        valid++;
                        attempted++;
                        fromProfile++;
                        continue;
                    }
                    missing++;
                    continue;
                }
                attempted++;
                group.Map = buildMap(group.FileName, original, group.Data());
                if (group.Map.Valid)
                    valid++;
            }

            if (valid > 0)
                offsetsStatus = valid.ToString() + " of " + attempted.ToString()
                    + " table maps read"
                    + (fromProfile > 0
                        ? " (" + fromProfile.ToString() + " from the built-in profile)" : "")
                    + (missing > 0
                        ? ", " + missing.ToString() + " loaded file(s) had no original" : "");
            else if (missing > 0 && attempted == 0)
                offsetsStatus = "no table maps read - the originals for the loaded files are not "
                    + "in that folder";
            else
                offsetsStatus = "no table maps read - put the original fixed_*.bin files in the "
                    + "originals folder";
            return valid > 0;
        }

        // ------------------------------------------------- applied offsets
        // The offsets as compiled in, so applying a map twice cannot shift twice.
        private static bool baseCaptured;
        private static Dictionary<string, int> baseValues = new Dictionary<string, int>();

        private static System.Reflection.FieldInfo fieldOf(string name)
        {
            return typeof(Randomizer).GetField(name,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        }

        private static void captureBase()
        {
            if (baseCaptured)
                return;
            baseCaptured = true;
            List<TableGroup> list = tableGroups();
            for (int i = 0; i < list.Count; i++)
                for (int f = 0; f < list[i].Fields.Length; f++)
                {
                    System.Reflection.FieldInfo field = fieldOf(list[i].Fields[f]);
                    if (field != null && !baseValues.ContainsKey(list[i].Fields[f]))
                        baseValues[list[i].Fields[f]] = Convert.ToInt32(field.GetValue(null));
                }
        }

        private static bool setField(string name, int value)
        {
            System.Reflection.FieldInfo field = fieldOf(name);
            if (field == null)
                return false;
            field.SetValue(null, Convert.ChangeType(value, field.FieldType));
            return true;
        }

        // Called once before the randomization: reads the maps if needed, then
        // points the randomizer at the mod's tables.
        public static void prepareForRun()
        {
            if (!modMode)
                return;
            // Re-read every run: the user may have loaded different files since,
            // and the built-in profile needs no folder at all.
            readOffsets();
            applyOffsets();
        }

        public static bool hasOffsets()
        {
            List<TableGroup> list = tableGroups();
            for (int i = 0; i < list.Count; i++)
                if (list[i].Map != null && list[i].Map.Valid)
                    return true;
            return false;
        }

        public static void applyOffsets()
        {
            statOffsetsApplied = 0;
            captureBase();
            // Off the record unless mod mode is on: vanilla offsets must stay
            // vanilla, even when a mod's files were loaded to look at.
            if (!modMode)
                return;

            // Every offset is set from the compiled-in value, so running this
            // twice, or after a map was dropped, cannot leave a half-moved set.
            List<TableGroup> list = tableGroups();
            for (int i = 0; i < list.Count; i++)
            {
                TableGroup group = list[i];
                for (int f = 0; f < group.Fields.Length; f++)
                {
                    int baseValue;
                    if (!baseValues.TryGetValue(group.Fields[f], out baseValue))
                        continue;
                    int target = baseValue;
                    if (group.Map != null && group.Map.Valid)
                        target = group.Map.map(baseValue);
                    if (setField(group.Fields[f], target) && target != baseValue)
                        statOffsetsApplied++;
                }
            }

            // Looking the Dancer row up again costs nothing and keeps it right
            // when the offsets are applied after a first look.
            dancerLookedUp = false;
        }

        // Files that grew or shrank but have no map: their internal layout was
        // not checked, so a write could land in the wrong table.
        public static List<string> unverifiedFiles()
        {
            List<string> names = new List<string>();
            List<TableGroup> list = tableGroups();
            for (int i = 0; i < list.Count; i++)
            {
                TableGroup group = list[i];
                if (!group.IsLoaded())
                    continue;
                if (group.Map != null && group.Map.Valid)
                    continue;
                if (sizeDiffers(group.FileName, true, group.Data()))
                    names.Add(group.FileName);
            }
            return names;
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

        // Rows in the loaded class table, straight out of its header.
        public static int classRowCount()
        {
            if (Randomizer.classDataV == null)
                return 100;
            int count = headerWord(Randomizer.classDataV, 22);
            if (count <= 0 || count > 4096)
                return 100;
            return count;
        }

        // Word at index 22 of a class file header: the row count. Words 2..21 are
        // the ten sub-table starts, so this is 2 + 2 * subTableCount.
        private static int headerWord(byte[] data, int word)
        {
            int at = word * 4;
            if (data == null || at + 4 > data.Length)
                return -1;
            return data[at] | (data[at + 1] << 8) | (data[at + 2] << 16) | (data[at + 3] << 24);
        }

        private static void lookupDancerRow()
        {
            dancerLookedUp = true;
            dancerClassIndex = 43;
            if (Randomizer.classDataV == null)
                return;

            int found = -1;
            int count = 0;
            // A total conversion may add class rows; the file says how many there are.
            int rows = classRowCount();
            for (int index = 0; index < rows; index++)
            {
                long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)index;
                if (block + 5 > Randomizer.classDataV.Length)
                    break;
                // +2..+4 are the three ability slots the randomizer writes; the
                // first two bytes of a row are not abilities.
                for (int slot = 2; slot < 5; slot++)
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

            int classRows = classRowCount();
            for (int index = 0; index < classRows; index++)
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
                        if (files[i].Name == "fixed_classdata.bin" || files[i].Name == "fixed_data.bin"
                            || files[i].Name == "fixed_persondata.bin")
                            lines.Add("    the tables in this file are laid out differently from the original -\r\n"
                                + "    the offset map moves the randomizer's offsets to match (read the\r\n"
                                + "    original files in first, or the run falls back to the compiled-in ones)");
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

            int classRows = classRowCount();
            for (int index = 0; index < classRows; index++)
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

            lines.Add("class rows in the loaded file: " + classRowCount().ToString());
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
                lines.Add("populated class rows outside the randomized range (88.."
                    + (classRows - 1).ToString() + ", except 91): " + rowsText.ToString());
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

        // Short version for the message box on the tab.
        public static string offsetSummary()
        {
            captureBase();
            StringBuilder text = new StringBuilder();
            List<TableGroup> list = tableGroups();
            for (int i = 0; i < list.Count; i++)
            {
                TableGroup group = list[i];
                if (group.Map == null)
                {
                    text.AppendLine("  " + group.FileName + " : no original to compare with");
                    continue;
                }
                if (!group.Map.Valid)
                {
                    text.AppendLine("  " + group.FileName + " : "
                        + (group.Map.Problem == null ? "not usable" : group.Map.Problem));
                    continue;
                }
                text.AppendLine("  " + group.FileName + " : deltas " + deltaList(group.Map)
                    + (group.Map.FromProfile ? "   (built-in profile: " + group.Map.ProfileName + ")"
                        : ""));
            }

            List<string> open = unverifiedFiles();
            if (open.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("not checked (no original in that folder, but the loaded file is a");
                text.AppendLine("different size, so its tables may have moved):");
                for (int i = 0; i < open.Count; i++)
                    text.AppendLine("  " + open[i]);
            }
            return text.ToString();
        }

        private static void appendOffsetReport(StringBuilder text, List<string> passes, List<string> warnings,
            List<string> failures)
        {
            captureBase();
            text.AppendLine("original files folder          : "
                + (originalFolder == null || originalFolder.Trim().Length == 0
                    ? "(not set)" : originalFolder.Trim()));
            text.AppendLine("offset maps                    : " + offsetsStatus);
            text.AppendLine("table offsets adjusted         : " + statOffsetsApplied);

            List<TableGroup> list = tableGroups();
            bool anyLoaded = false;
            for (int i = 0; i < list.Count; i++)
                if (list[i].IsLoaded())
                    anyLoaded = true;

            for (int i = 0; i < list.Count; i++)
            {
                TableGroup group = list[i];
                if (group.Map == null)
                    continue;
                if (!group.Map.Valid)
                {
                    text.AppendLine(group.FileName + " : "
                        + (group.Map.Problem == null ? "not usable" : group.Map.Problem));
                    continue;
                }
                appendMap(text, group);
            }

            List<string> open = unverifiedFiles();
            if (open.Count > 0)
            {
                StringBuilder names = new StringBuilder();
                for (int i = 0; i < open.Count; i++)
                {
                    if (i > 0)
                        names.Append(", ");
                    names.Append(open[i]);
                }
                text.AppendLine("not checked                    : " + names.ToString());
                text.AppendLine("    these loaded files are not the original size, so a mod added or removed");
                text.AppendLine("    rows, and their table offsets were NOT verified - put the original copies");
                text.AppendLine("    of them in the originals folder to have the offsets moved as well");
                if (modMode)
                    warnings.Add("the table offsets of " + names.ToString()
                        + " were not checked: no original to compare with, and the loaded file is a "
                        + "different size. Writes into those files may land in the wrong table");
            }

            if (!anyLoaded)
                text.AppendLine("no data files loaded yet, nothing to compare");

            if (!hasOffsets() && anyLoaded && modMode)
                warnings.Add("mod mode is on but no table map could be built, so the compiled-in "
                    + "offsets were used - that is only right for an unmodified game");

            if (statOffsetsApplied > 0)
                passes.Add("mod table offsets: " + statOffsetsApplied.ToString()
                    + " table offset(s) moved to where the mod's file really has them");
            text.AppendLine();
        }

        private static void appendMap(StringBuilder text, TableGroup group)
        {
            OffsetMap map = group.Map;
            StringBuilder moved = new StringBuilder();
            int count = 0;
            for (int i = 0; i < group.Fields.Length && i < group.Labels.Length; i++)
            {
                int baseValue;
                if (!baseValues.TryGetValue(group.Fields[i], out baseValue))
                    continue;
                int mapped = map.map(baseValue);
                if (mapped == baseValue)
                    continue;
                count++;
                if (moved.Length > 0)
                    moved.Append(", ");
                moved.Append(group.Labels[i]).Append(" ")
                    .Append(baseValue.ToString()).Append(" -> ").Append(mapped.ToString())
                    .Append(" (").Append(map.deltaAt(baseValue) > 0 ? "+" : "")
                    .Append(map.deltaAt(baseValue).ToString()).Append(")");
            }

            text.AppendLine(group.FileName
                + (count > 0
                    ? " : " + count.ToString() + " of " + group.Fields.Length.ToString()
                        + " table(s) moved"
                    : " : same layout as the original, nothing to adjust")
                + ", sub-tables " + map.Count.ToString() + ", deltas " + deltaList(map)
                + (map.FromProfile ? "\r\n    from the built-in profile for " + map.ProfileName
                    + " (the loaded header matches the measured one exactly)" : ""));
            if (moved.Length > 0)
                text.AppendLine("    " + moved.ToString());
        }

        // Called by the form right before a run. A mod that moved a table makes
        // every write land somewhere else, so this asks instead of quietly
        // writing into the neighbouring table.
        private static bool offsetsAcknowledged;

        public static bool confirmOffsets()
        {
            if (!modMode || offsetsAcknowledged)
                return true;

            List<string> open = unverifiedFiles();
            if (open.Count == 0)
                return true;

            StringBuilder names = new StringBuilder();
            for (int i = 0; i < open.Count && i < 6; i++)
            {
                if (i > 0)
                    names.Append("\r\n  ");
                names.Append(open[i]);
            }
            if (open.Count > 6)
                names.Append("\r\n  ... and ").Append((open.Count - 6).ToString()).Append(" more");

            DialogResult answer = MessageBox.Show(
                "Mod mode is on, but the table offsets of these files could not be checked:\r\n\r\n  "
                + names.ToString()
                + "\r\n\r\nThey are not the original size, so the mod added or removed rows and the "
                + "tables inside them may have moved. Writing at the compiled-in offsets would then land "
                + "in the wrong table.\r\n\r\nPut the original copies of those files in the folder set "
                + "as \"Unmodified game files\" on the Extra options tab.\r\n\r\n"
                + "Randomize anyway (not recommended)?",
                "Table offsets unknown", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (answer == DialogResult.Yes)
                offsetsAcknowledged = true;
            return answer == DialogResult.Yes;
        }

        private static bool sizeDiffers(string name, bool loaded, byte[] data)
        {
            if (!loaded || data == null)
                return false;
            long original = originalSize(name);
            return original > 0 && data.Length != original;
        }

        private static string deltaList(OffsetMap map)
        {
            StringBuilder list = new StringBuilder();
            int last = int.MinValue;
            for (int i = 0; i < map.Original.Length; i++)
            {
                int delta = map.Modded[i] - map.Original[i];
                if (delta == last)
                    continue;
                if (list.Length > 0)
                    list.Append(", ");
                list.Append(delta.ToString());
                last = delta;
            }
            return list.ToString();
        }

        // ----------------------------------------------------------- report
        public static void appendReport(StringBuilder text, List<string> passes, List<string> warnings,
            List<string> failures)
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

            appendOffsetReport(text, passes, warnings, failures);

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
