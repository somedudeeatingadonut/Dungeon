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

        // weapon effect remix
        public static bool graftWeaponEffects = false;
        public static bool graftCombatArtEffects = false;
        public static bool graftGambitEffects = false;
        public static bool uniqueWeaponEffects = false;
        public static bool weaponDonorsDifferInType = false;
        /// <summary>0 = inside one class tier, 1 = across every class.</summary>
        public static int swapScope = 0;

        // ------------------------------------------------------------ counters
        public static int statClassStatsSwapped;
        public static int statClassGrowthsSwapped;
        public static int statCharSheetsSwapped;
        public static int statBossRemixed;
        public static int statWeaponEffectsGrafted;
        public static int statArtEffectsGrafted;
        public static int statGambitEffectsGrafted;
        public static int statWeaponEffectsRerolled;

        public static void resetCounters()
        {
            statClassStatsSwapped = 0;
            statClassGrowthsSwapped = 0;
            statCharSheetsSwapped = 0;
            statBossRemixed = 0;
            statWeaponEffectsGrafted = 0;
            statArtEffectsGrafted = 0;
            statGambitEffectsGrafted = 0;
            statWeaponEffectsRerolled = 0;
        }

        // ------------------------------------------------------------- the run
        public static void applyAfterRandomization()
        {
            remixBosses();
            swapStatProfiles();
            swapGrowthProfiles();
            shuffleSheets();
            remixWeaponEffects();
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

        // ------------------------------------------------- 5. weapon effect remix
        // Weapons, gambits and combat arts all store their effect in the same id
        // space (weapon and gambit effect: byte +12 of a 24 byte block, combat
        // art effect: byte +7 of an 18 byte block, 0..93 with 96 meaning "none").
        // That makes the crossover possible without inventing anything:
        //
        //   * graft another weapon's effects onto a weapon (status effect,
        //     effectiveness and the property bits - double attack, magic damage,
        //     ignore defence, leave at 1 HP), optionally only from weapons of a
        //     different type so the result is visibly different;
        //   * give a weapon one of the combat art effect ids;
        //   * give a weapon a gambit's effects.
        //
        // Only the *effects* move. Might / hit / crit / weight / range / durability
        // are left exactly as the normal randomization set them, so this composes
        // with randAtk* instead of overwriting it.
        private const int weaponSlotEffect = 12;
        private const int weaponSlotEffectiveness = 14;
        private const int weaponSlotProperties = 22;
        private const int weaponSlotType = 5;
        private const int effectNone = 96;
        private const int artEffectMin = 18;
        private const int artEffectMaxExclusive = 55;

        private static void remixWeaponEffects()
        {
            if (Randomizer.itemData == null)
                return;
            if (!graftWeaponEffects && !graftCombatArtEffects && !graftGambitEffects
                && !uniqueWeaponEffects)
                return;

            byte[] data = Randomizer.itemData;
            int weapons = 500;
            int gambits = 80;

            if (graftWeaponEffects || graftCombatArtEffects || graftGambitEffects)
            {
                for (int index = 0; index < weapons; index++)
                {
                    int target = (int)Randomizer.weaponBlockStart + (int)Randomizer.weaponBlockLen * index;
                    if (target + weaponSlotProperties >= data.Length)
                        break;

                    if (graftWeaponEffects)
                    {
                        int donor = pickWeaponDonor(index, weapons);
                        if (donor >= 0)
                        {
                            int source = (int)Randomizer.weaponBlockStart + (int)Randomizer.weaponBlockLen * donor;
                            graftEffect(data, source, target);
                            statWeaponEffectsGrafted++;
                            continue;
                        }
                    }
                    if (graftCombatArtEffects)
                    {
                        data[target + weaponSlotEffect] =
                            (byte)Randomizer.rng.Next(artEffectMin, artEffectMaxExclusive);
                        statArtEffectsGrafted++;
                        continue;
                    }
                    if (graftGambitEffects)
                    {
                        int donor = pickGambitDonor(gambits);
                        if (donor >= 0)
                        {
                            int source = (int)Randomizer.gambitBlockStart + (int)Randomizer.gambitBlockLen * donor;
                            if (source + weaponSlotProperties < data.Length)
                            {
                                graftEffect(data, source, target);
                                statGambitEffectsGrafted++;
                            }
                        }
                    }
                }
            }

            if (uniqueWeaponEffects)
            {
                List<string> seen = new List<string>();
                for (int index = 0; index < weapons; index++)
                {
                    int target = (int)Randomizer.weaponBlockStart + (int)Randomizer.weaponBlockLen * index;
                    if (target + weaponSlotProperties >= data.Length)
                        break;
                    if (data[target + weaponSlotEffect] == effectNone)
                        continue; // no effect at all, nothing to keep unique

                    string key = weaponEffectKey(data, target);
                    if (!seen.Contains(key))
                    {
                        seen.Add(key);
                        continue;
                    }
                    // Someone already has this exact effect set - reroll the
                    // status effect until the combination is new.
                    for (int attempt = 0; attempt < 20; attempt++)
                    {
                        data[target + weaponSlotEffect] =
                            (byte)Randomizer.rng.Next(0, effectNone);
                        string candidate = weaponEffectKey(data, target);
                        if (!seen.Contains(candidate))
                        {
                            seen.Add(candidate);
                            statWeaponEffectsRerolled++;
                            break;
                        }
                    }
                }
            }

            if (Randomizer.changelog != null)
                appendWeaponChangelog();
        }

        private static string weaponEffectKey(byte[] data, int block)
        {
            return data[block + weaponSlotEffect].ToString() + "/"
                + data[block + weaponSlotEffectiveness].ToString() + "/"
                + data[block + weaponSlotProperties].ToString();
        }

        private static void graftEffect(byte[] data, int source, int target)
        {
            data[target + weaponSlotEffect] = data[source + weaponSlotEffect];
            data[target + weaponSlotEffectiveness] = data[source + weaponSlotEffectiveness];
            data[target + weaponSlotProperties] = data[source + weaponSlotProperties];
        }

        private static int pickWeaponDonor(int targetIndex, int weaponCount)
        {
            int targetType = -1;
            int targetBlock = (int)Randomizer.weaponBlockStart
                + (int)Randomizer.weaponBlockLen * targetIndex;
            if (targetBlock + weaponSlotType < Randomizer.itemData.Length)
                targetType = Randomizer.itemData[targetBlock + weaponSlotType];

            for (int attempt = 0; attempt < 24; attempt++)
            {
                int donor = Randomizer.rng.Next(0, weaponCount);
                if (donor == targetIndex)
                    continue;
                int source = (int)Randomizer.weaponBlockStart
                    + (int)Randomizer.weaponBlockLen * donor;
                if (source + weaponSlotProperties >= Randomizer.itemData.Length)
                    continue;
                if (Randomizer.itemData[source + weaponSlotEffect] == effectNone)
                    continue; // donor has no effect, nothing to give
                if (weaponDonorsDifferInType && targetType >= 0
                    && Randomizer.itemData[source + weaponSlotType] == targetType)
                    continue;
                return donor;
            }
            return -1;
        }

        private static int pickGambitDonor(int gambitCount)
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                int donor = Randomizer.rng.Next(0, gambitCount);
                int source = (int)Randomizer.gambitBlockStart
                    + (int)Randomizer.gambitBlockLen * donor;
                if (source + weaponSlotProperties >= Randomizer.itemData.Length)
                    continue;
                if (Randomizer.itemData[source + weaponSlotEffect] == effectNone)
                    continue;
                return donor;
            }
            return -1;
        }

        // Adds a short section to the changelog, so the grafted effects are
        // visible in the output instead of only in the debug report. The
        // changelog is written after the passes have run.
        private static void appendWeaponChangelog()
        {
            byte[] vanilla = Randomizer.itemDataV;
            Randomizer.changelog += "\n\n\n---WEAPON EFFECT REMIX---";
            if (graftWeaponEffects)
                Randomizer.changelog += "\nEffects grafted from other weapons: " + statWeaponEffectsGrafted.ToString();
            if (graftCombatArtEffects)
                Randomizer.changelog += "\nWeapons given a combat art effect: " + statArtEffectsGrafted.ToString();
            if (graftGambitEffects)
                Randomizer.changelog += "\nWeapons given a gambit's effects: " + statGambitEffectsGrafted.ToString();
            if (uniqueWeaponEffects)
                Randomizer.changelog += "\nWeapons with a duplicate effect set re-rolled: " + statWeaponEffectsRerolled.ToString();
            if (weaponDonorsDifferInType)
                Randomizer.changelog += "\nDonors were always weapons of a different type.";

            int shown = 0;
            int weapons = 500;
            byte[] data = Randomizer.itemData;
            for (int index = 0; index < weapons && shown < 40; index++)
            {
                int block = (int)Randomizer.weaponBlockStart + (int)Randomizer.weaponBlockLen * index;
                if (block + weaponSlotProperties >= data.Length)
                    break;
                if (Randomizer.weaponNames == null || index >= Randomizer.weaponNames.Length)
                    break;
                string name = Randomizer.weaponNames[index];
                if (name == null || name.Length == 0)
                    continue;
                if (vanilla != null && block + weaponSlotEffect < vanilla.Length
                    && data[block + weaponSlotEffect] == vanilla[block + weaponSlotEffect])
                    continue;

                int effect = data[block + weaponSlotEffect];
                string effectName = effect >= 0 && effect < Randomizer.effectNames.Length
                    ? Randomizer.effectNames[effect] : "(unknown)";
                if (Randomizer.changelog != null)
                {
                    Randomizer.changelog += "\n" + name + " : " + effectName;
                    shown++;
                }
            }
        }

        // ---------------------------------------------------------- inspection
        public static int countWeaponsWithEffects()
        {
            if (Randomizer.itemData == null)
                return 0;
            int count = 0;
            for (int index = 0; index < 500; index++)
            {
                int block = (int)Randomizer.weaponBlockStart + (int)Randomizer.weaponBlockLen * index;
                if (block + weaponSlotProperties >= Randomizer.itemData.Length)
                    break;
                if (Randomizer.itemData[block + weaponSlotEffect] != effectNone)
                    count++;
            }
            return count;
        }

        public static int countDuplicateWeaponEffects()
        {
            if (Randomizer.itemData == null)
                return 0;
            List<string> seen = new List<string>();
            int duplicates = 0;
            for (int index = 0; index < 500; index++)
            {
                int block = (int)Randomizer.weaponBlockStart + (int)Randomizer.weaponBlockLen * index;
                if (block + weaponSlotProperties >= Randomizer.itemData.Length)
                    break;
                if (Randomizer.itemData[block + weaponSlotEffect] == effectNone)
                    continue;
                string key = weaponEffectKey(Randomizer.itemData, block);
                if (seen.Contains(key))
                    duplicates++;
                else
                    seen.Add(key);
            }
            return duplicates;
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
