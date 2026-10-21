// -----------------------------------------------------------------------------
//  RandomizerBig.cs
//
//  Large-scale randomization, as opposed to the small quality-of-life passes in
//  RandomizerMore.cs. These deliberately break the class lineup apart:
//
//   1. Swap class stat profiles  - base stats and stat boosts are dealt out
//      between classes again, so a Fortress Knight can end up with a Pegasus
//      stat line. Within a tier by default, or across the whole game.
//   2. Swap class growth profiles - the same for the nine class growths, so the
//      class you pick for its growths is genuinely a gamble.
//   3. Shuffle character sheets   - base stats and growths are permuted between
//      the playable characters, so "who is the fast one" changes every run.
//   4. Boss remix                 - every boss / monster class receives another
//      boss class's whole ability kit instead of its own.
//
//  All four are permutations: the same pool of numbers is dealt out in a
//  different order, so nothing is invented and no value can leave its range.
//  They run after RandomizerMore's passes (so the boss protection has already
//  put the vanilla values back) and before the blocklist safety pass.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class RandomizerBig
    {
        // -------------------------------------------------------- option state
        public static bool swapClassStatProfiles = false;
        public static bool swapClassGrowthProfiles = false;
        public static bool shuffleCharacterSheets = false;
        public static bool bossRemix = false;
        /// <summary>0 = inside one class tier, 1 = across every class.</summary>
        public static int swapScope = 0;

        // ------------------------------------------------------------ counters
        public static int statClassStatsSwapped;
        public static int statClassGrowthsSwapped;
        public static int statCharSheetsSwapped;
        public static int statBossRemixed;

        public static void resetCounters()
        {
            statClassStatsSwapped = 0;
            statClassGrowthsSwapped = 0;
            statCharSheetsSwapped = 0;
            statBossRemixed = 0;
        }

        // ------------------------------------------------------------- the run
        public static void applyAfterRandomization()
        {
            remixBosses();
            swapStatProfiles();
            swapGrowthProfiles();
            shuffleSheets();
        }

        // ----------------------------------------------------------- offsets
        private static readonly int[] statBoostOffsets = { 16, 39, 40, 41, 42, 43, 44, 45, 46, 47 };
        private static readonly int[] baseStatOffsets = { 75, 85, 86, 87, 88, 89, 90, 91, 93 };
        private static readonly int[] playerGrowthOffsets = { 20, 21, 22, 23, 24, 25, 26, 27, 29 };
        private static readonly int[] charGrowthOffsets = { 41, 60, 61, 62, 63, 64, 65, 66, 68 };
        private static readonly int[] charBaseOffsets = { 43, 51, 52, 53, 54, 55, 56, 57, 59 };

        // ------------------------------------------------------------- groups
        // The classes the rest of the tool randomizes, grouped by tier. Monster
        // and boss rows are left out: they have their own options.
        private static List<List<int>> classGroups()
        {
            List<List<int>> groups = new List<List<int>>();
            if (Randomizer.classData == null)
                return groups;

            if (swapScope == 1)
            {
                groups.Add(new List<int>());
            }
            else
            {
                for (int tier = 0; tier < 8; tier++)
                    groups.Add(new List<int>());
            }

            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 94 >= Randomizer.classData.Length)
                    break;
                if (!(index < 88 || index == 91))
                    continue;
                if (RandomizerMore.isMonsterClass(index))
                    continue;

                if (swapScope == 1)
                {
                    groups[0].Add(index);
                    continue;
                }
                int tier = Randomizer.classData[block + 69L];
                if (tier == 5)
                    tier = 3;
                if (tier < 0 || tier >= groups.Count)
                    continue;
                groups[tier].Add(index);
            }
            return groups;
        }

        private static List<int> shuffled(List<int> source)
        {
            List<int> copy = new List<int>(source);
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = Randomizer.rng.Next(0, i + 1);
                int swap = copy[i];
                copy[i] = copy[j];
                copy[j] = swap;
            }
            return copy;
        }

        // ------------------------------------------------- 1. class stat swap
        private static void swapStatProfiles()
        {
            if (!swapClassStatProfiles || Randomizer.classData == null)
                return;

            List<List<int>> groups = classGroups();
            for (int g = 0; g < groups.Count; g++)
            {
                List<int> classes = groups[g];
                if (classes.Count < 2)
                    continue;
                List<int> source = shuffled(classes);

                int[][] stats = new int[classes.Count][];
                int[][] boosts = new int[classes.Count][];
                for (int i = 0; i < classes.Count; i++)
                {
                    stats[i] = readVector(classes[i], baseStatOffsets);
                    boosts[i] = readVector(classes[i], statBoostOffsets);
                }
                for (int i = 0; i < classes.Count; i++)
                {
                    int target = classes[i];
                    int from = source[i];
                    int fromIndex = classes.IndexOf(from);
                    if (fromIndex < 0)
                        continue;
                    writeVector(target, baseStatOffsets, stats[fromIndex]);
                    writeVector(target, statBoostOffsets, boosts[fromIndex]);
                    if (target != from)
                        statClassStatsSwapped++;
                }
            }
        }

        // ----------------------------------------------- 2. class growth swap
        private static void swapGrowthProfiles()
        {
            if (!swapClassGrowthProfiles || Randomizer.classData == null)
                return;

            List<List<int>> groups = classGroups();
            for (int g = 0; g < groups.Count; g++)
            {
                List<int> classes = groups[g];
                if (classes.Count < 2)
                    continue;
                List<int> source = shuffled(classes);

                int[][] growths = new int[classes.Count][];
                for (int i = 0; i < classes.Count; i++)
                    growths[i] = readVector(classes[i], playerGrowthOffsets);

                for (int i = 0; i < classes.Count; i++)
                {
                    int target = classes[i];
                    int from = source[i];
                    int fromIndex = classes.IndexOf(from);
                    if (fromIndex < 0)
                        continue;
                    writeVector(target, playerGrowthOffsets, growths[fromIndex]);
                    if (target != from)
                        statClassGrowthsSwapped++;
                }
            }
        }

        // ------------------------------------------- 3. character sheet shuffle
        // Rows 35..37 are not used by the game (see the changelog loop), so they
        // are skipped and keep their data.
        private static void shuffleSheets()
        {
            if (!shuffleCharacterSheets)
                return;
            if (Randomizer.personData == null)
                return;

            List<int> rows = new List<int>();
            for (int index = 0; index < 45; index++)
            {
                if (index >= 35 && index <= 37)
                    continue;
                long block = Randomizer.charBlockStart + Randomizer.charBlockLen * (long)index;
                if (block + 69 >= Randomizer.personData.Length)
                    continue;
                rows.Add(index);
            }
            if (rows.Count < 2)
                return;

            List<int> source = shuffled(rows);
            int[][] growths = new int[rows.Count][];
            int[][] bases = new int[rows.Count][];
            for (int i = 0; i < rows.Count; i++)
            {
                growths[i] = readCharVector(rows[i], charGrowthOffsets);
                bases[i] = readCharVector(rows[i], charBaseOffsets);
            }
            for (int i = 0; i < rows.Count; i++)
            {
                int target = rows[i];
                int fromIndex = rows.IndexOf(source[i]);
                if (fromIndex < 0)
                    continue;
                writeCharVector(target, charGrowthOffsets, growths[fromIndex]);
                writeCharVector(target, charBaseOffsets, bases[fromIndex]);
                if (target != source[i])
                    statCharSheetsSwapped++;
            }
        }

        // ------------------------------------------------------- 4. boss remix
        // Every boss / monster class gets another boss's whole kit: the class
        // ability block and the monster latent / barrier slots.
        private static void remixBosses()
        {
            if (!bossRemix)
                return;
            if (Randomizer.classData == null)
                return;

            List<int> bosses = new List<int>();
            for (int index = 0; index < 100; index++)
                if (RandomizerMore.isMonsterClass(index))
                    bosses.Add(index);
            if (bosses.Count < 2)
                return;

            List<int> source = shuffled(bosses);
            for (int i = 0; i < bosses.Count; i++)
            {
                int target = bosses[i];
                int from = source[i];
                if (target == from)
                    continue;

                long targetAb = Randomizer.classAbStart + Randomizer.classAbLen * (long)target;
                long fromAb = Randomizer.classAbStart + Randomizer.classAbLen * (long)from;
                if (targetAb + 5 < Randomizer.classData.Length && fromAb + 5 < Randomizer.classData.Length)
                    for (int slot = 0; slot < 5; slot++)
                        Randomizer.classData[targetAb + slot] = Randomizer.classData[fromAb + slot];

                long targetMon = Randomizer.classMonsterStart + Randomizer.classMonsterLen * (long)target;
                long fromMon = Randomizer.classMonsterStart + Randomizer.classMonsterLen * (long)from;
                if (targetMon + 70 < Randomizer.classData.Length && fromMon + 70 < Randomizer.classData.Length)
                    for (int slot = 64; slot <= 69; slot++)
                        Randomizer.classData[targetMon + slot] = Randomizer.classData[fromMon + slot];

                statBossRemixed++;
            }
        }

        // ------------------------------------------------------------ helpers
        private static int[] readVector(int classIndex, int[] offsets)
        {
            int[] values = new int[offsets.Length];
            long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classIndex;
            for (int i = 0; i < offsets.Length; i++)
            {
                long offset = block + offsets[i];
                if (offset >= 0 && offset < Randomizer.classData.Length)
                    values[i] = Randomizer.classData[offset];
            }
            return values;
        }

        private static void writeVector(int classIndex, int[] offsets, int[] values)
        {
            long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classIndex;
            for (int i = 0; i < offsets.Length && i < values.Length; i++)
            {
                long offset = block + offsets[i];
                if (offset >= 0 && offset < Randomizer.classData.Length)
                    Randomizer.classData[offset] = (byte)values[i];
            }
        }

        private static int[] readCharVector(int row, int[] offsets)
        {
            int[] values = new int[offsets.Length];
            long block = Randomizer.charBlockStart + Randomizer.charBlockLen * (long)row;
            for (int i = 0; i < offsets.Length; i++)
            {
                long offset = block + offsets[i];
                if (offset >= 0 && offset < Randomizer.personData.Length)
                    values[i] = Randomizer.personData[offset];
            }
            return values;
        }

        private static void writeCharVector(int row, int[] offsets, int[] values)
        {
            long block = Randomizer.charBlockStart + Randomizer.charBlockLen * (long)row;
            for (int i = 0; i < offsets.Length && i < values.Length; i++)
            {
                long offset = block + offsets[i];
                if (offset >= 0 && offset < Randomizer.personData.Length)
                    Randomizer.personData[offset] = (byte)values[i];
            }
        }

        // --------------------------------------------------------- inspection
        // How many classes / characters currently differ from the vanilla data.
        public static int countChangedClasses()
        {
            if (Randomizer.classData == null || Randomizer.classDataV == null)
                return 0;
            int changed = 0;
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 94 >= Randomizer.classData.Length || block + 94 >= Randomizer.classDataV.Length)
                    break;
                if (!(index < 88 || index == 91))
                    continue;
                if (RandomizerMore.isMonsterClass(index))
                    continue;
                bool differs = false;
                for (int i = 0; i < playerGrowthOffsets.Length && !differs; i++)
                    if (Randomizer.classData[block + playerGrowthOffsets[i]]
                        != Randomizer.classDataV[block + playerGrowthOffsets[i]])
                        differs = true;
                for (int i = 0; i < baseStatOffsets.Length && !differs; i++)
                    if (Randomizer.classData[block + baseStatOffsets[i]]
                        != Randomizer.classDataV[block + baseStatOffsets[i]])
                        differs = true;
                if (differs)
                    changed++;
            }
            return changed;
        }

        public static int countChangedCharacters()
        {
            if (Randomizer.personData == null || Randomizer.personDataV == null)
                return 0;
            int changed = 0;
            for (int row = 0; row < 45; row++)
            {
                if (row >= 35 && row <= 37)
                    continue;
                long block = Randomizer.charBlockStart + Randomizer.charBlockLen * (long)row;
                if (block + 69 >= Randomizer.personData.Length || block + 69 >= Randomizer.personDataV.Length)
                    break;
                for (int i = 0; i < charGrowthOffsets.Length; i++)
                {
                    if (Randomizer.personData[block + charGrowthOffsets[i]]
                        != Randomizer.personDataV[block + charGrowthOffsets[i]])
                    {
                        changed++;
                        break;
                    }
                }
            }
            return changed;
        }
    }
}
