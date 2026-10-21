// -----------------------------------------------------------------------------
//  RandomizerMore.cs
//
//  Four more options, all implemented as passes that run *after* the normal
//  randomization has finished. That keeps them out of the way of the existing
//  code: they only look at the finished byte arrays and tidy them up.
//
//   1. Keep boss / monster class abilities  - monster and boss classes get their
//      own ability slots (class abilities, latent abilities, barrier abilities)
//      back from the vanilla file, so a boss is still a boss. The even-variety
//      spread never touched them either.
//
//   2. Unique personal abilities - no two characters share a personal ability,
//      so a run does not hand the same personal skill to five people.
//
//   3. No repeated abilities on one class - a class cannot end up with the same
//      ability in two of its slots, which just wastes a slot.
//
//   4. Growth clamp - keeps every class (and character) growth inside a range,
//      so the randomization cannot produce 0% or 200% growths.
//
//  Every pass only ever replaces a value that the randomization *introduced*, so
//  data that was already in the vanilla file (Dancer's Dance, a boss's own
//  abilities, the monster latent skills) is never taken away.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class RandomizerMore
    {
        // -------------------------------------------------------- option state
        public static bool protectBossAbilities = true;
        public static bool uniquePersonalAbilities = false;
        public static bool uniqueClassAbilities = true;
        public static bool clampGrowths = false;
        public static int growthClampMin = 10;
        public static int growthClampMax = 70;

        // ------------------------------------------------------------ counters
        public static int statBossSlotsRestored;
        public static int statDuplicatePersonalRerolled;
        public static int statDuplicateClassAbRerolled;
        public static int statGrowthsClamped;

        public static void resetCounters()
        {
            statBossSlotsRestored = 0;
            statDuplicatePersonalRerolled = 0;
            statDuplicateClassAbRerolled = 0;
            statGrowthsClamped = 0;
        }

        // ------------------------------------------------------------- the run
        public static void applyAfterRandomization()
        {
            restoreBossAbilities();
            removeDuplicateClassAbilities();
            makePersonalAbilitiesUnique();
            clampAllGrowths();
        }

        // ---------------------------------------------------------- 1. bosses
        // Monster classes are the ones whose class block has anything other than
        // 255 at +73 (see the tier loop in Randomizer.randomize).
        public static bool isMonsterClass(int classIndex)
        {
            if (Randomizer.classData == null)
                return false;
            long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classIndex;
            if (block + 74 >= Randomizer.classData.Length)
                return false;
            byte[] source = Randomizer.classDataV != null && block + 74 < Randomizer.classDataV.Length
                ? Randomizer.classDataV : Randomizer.classData;
            return source[block + 73] != 255;
        }

        private static void restoreBossAbilities()
        {
            if (!protectBossAbilities)
                return;
            if (Randomizer.classData == null || Randomizer.classDataV == null)
                return;

            for (int index = 0; index < 100; index++)
            {
                if (!isMonsterClass(index))
                    continue;

                long abBlock = Randomizer.classAbStart + Randomizer.classAbLen * (long)index;
                if (abBlock + 5 < Randomizer.classData.Length && abBlock + 5 < Randomizer.classDataV.Length)
                    for (int slot = 0; slot < 5; slot++)
                        restoreSlot((int)(abBlock + slot));

                long monsterBlock = Randomizer.classMonsterStart + Randomizer.classMonsterLen * (long)index;
                if (monsterBlock + 70 < Randomizer.classData.Length
                    && monsterBlock + 70 < Randomizer.classDataV.Length)
                    for (int slot = 64; slot <= 69; slot++)
                        restoreSlot((int)(monsterBlock + slot));
            }
        }

        private static void restoreSlot(int offset)
        {
            if (offset < 0 || offset >= Randomizer.classData.Length || offset >= Randomizer.classDataV.Length)
                return;
            if (Randomizer.classData[offset] == Randomizer.classDataV[offset])
                return;
            Randomizer.classData[offset] = Randomizer.classDataV[offset];
            statBossSlotsRestored++;
        }

        // ------------------------------------------------- 2. one skill each
        // The personal ability sits at +20 (pre timeskip) and +21 (post) of every
        // character skill block; the playable characters are rows 0..44.
        private static void makePersonalAbilitiesUnique()
        {
            if (!uniquePersonalAbilities)
                return;
            if (Randomizer.personData == null || Randomizer.personDataV == null)
                return;
            if (Randomizer.settingsForDebug != null && !Randomizer.settingsForDebug.randPersonalAb)
                return; // personal abilities were not randomized, nothing to do

            List<int> used = new List<int>();
            for (int index = 0; index < 45; index++)
            {
                if (index >= 35 && index <= 37)
                    continue; // rows the game does not use (see the changelog loop)
                long block = Randomizer.charSkillStart + Randomizer.charSkillLen * (long)index;
                for (int slot = 20; slot <= 21; slot++)
                {
                    int offset = (int)(block + slot);
                    if (offset < 0 || offset >= Randomizer.personData.Length)
                        continue;
                    int ability = Randomizer.personData[offset];
                    if (ability == 255)
                        continue;
                    if (!used.Contains(ability))
                    {
                        used.Add(ability);
                        continue;
                    }
                    // Already taken by someone else. Only step in when the
                    // randomization put it here, never when it is vanilla data.
                    if (offset < Randomizer.personDataV.Length && Randomizer.personDataV[offset] == ability)
                        continue;
                    int replacement = pickUnusedAbility(used);
                    if (replacement == 255)
                        continue;
                    Randomizer.personData[offset] = (byte)replacement;
                    used.Add(replacement);
                    statDuplicatePersonalRerolled++;
                }
            }
        }

        // ------------------------------------------- 3. no repeats on a class
        // Slots 0 (mastery ability), 2, 3 and 4 (class abilities) are abilities;
        // slot 1 is the mastery combat art.
        private static readonly int[] classAbilitySlots = { 0, 2, 3, 4 };

        private static void removeDuplicateClassAbilities()
        {
            if (!uniqueClassAbilities)
                return;
            if (Randomizer.classData == null)
                return;

            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)index;
                if (block + 5 >= Randomizer.classData.Length)
                    break;

                List<int> used = new List<int>();
                for (int i = 0; i < classAbilitySlots.Length; i++)
                {
                    int offset = (int)(block + classAbilitySlots[i]);
                    int ability = Randomizer.classData[offset];
                    if (ability == 255)
                        continue;
                    if (!used.Contains(ability))
                    {
                        used.Add(ability);
                        continue;
                    }
                    if (Randomizer.classDataV != null && offset < Randomizer.classDataV.Length
                        && Randomizer.classDataV[offset] == ability)
                        continue; // vanilla duplicate, leave it alone
                    int replacement = pickUnusedAbility(used);
                    if (replacement == 255)
                        continue;
                    Randomizer.classData[offset] = (byte)replacement;
                    used.Add(replacement);
                    statDuplicateClassAbRerolled++;
                }
            }
        }

        // ------------------------------------------------------- 4. growths
        private static readonly int[] classPlayerGrowthOffsets = { 20, 21, 22, 23, 24, 25, 26, 27, 29 };
        private static readonly int[] classEnemyGrowthOffsets = { 19, 30, 31, 32, 33, 34, 35, 36, 38 };
        private static readonly int[] charGrowthOffsets = { 41, 60, 61, 62, 63, 64, 65, 66, 68 };

        private static void clampAllGrowths()
        {
            if (!clampGrowths)
                return;

            int low = Math.Max(0, Math.Min(growthClampMin, 255));
            int high = Math.Max(0, Math.Min(growthClampMax, 255));
            if (low > high)
            {
                int swap = low;
                low = high;
                high = swap;
            }

            if (Randomizer.classData != null)
            {
                for (int index = 0; index < 100; index++)
                {
                    long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                    if (block + 40 >= Randomizer.classData.Length)
                        break;
                    for (int i = 0; i < classPlayerGrowthOffsets.Length; i++)
                        clampByte(Randomizer.classData, (int)(block + classPlayerGrowthOffsets[i]), low, high);
                    for (int i = 0; i < classEnemyGrowthOffsets.Length; i++)
                        clampByte(Randomizer.classData, (int)(block + classEnemyGrowthOffsets[i]), low, high);
                }
            }

            if (Randomizer.personData != null)
            {
                for (int index = 0; index < 1201; index++)
                {
                    long block = Randomizer.charBlockStart + Randomizer.charBlockLen * (long)index;
                    if (block + 69 >= Randomizer.personData.Length)
                        break;
                    if (!characterRowUsed((int)block))
                        continue;
                    for (int i = 0; i < charGrowthOffsets.Length; i++)
                        clampByte(Randomizer.personData, (int)(block + charGrowthOffsets[i]), low, high);
                }
            }
        }

        // Empty character rows are all zero; do not invent growths for them.
        private static bool characterRowUsed(int block)
        {
            for (int i = 0; i < charGrowthOffsets.Length; i++)
                if (Randomizer.personData[block + charGrowthOffsets[i]] != 0)
                    return true;
            return false;
        }

        private static void clampByte(byte[] data, int offset, int low, int high)
        {
            if (offset < 0 || offset >= data.Length)
                return;
            int value = data[offset];
            // A stat the game does not grow at all stays at 0 - the clamp is not
            // there to invent growths, only to remove extreme ones.
            if (value == 0)
                return;
            int clamped = value < low ? low : (value > high ? high : value);
            if (clamped == value)
                return;
            data[offset] = (byte)clamped;
            statGrowthsClamped++;
        }

        // ------------------------------------------------------------ helpers
        private static int pickUnusedAbility(List<int> used)
        {
            int[] pool = Randomizer.genericAbilities;
            if (pool != null && pool.Length > 0)
            {
                for (int attempt = 0; attempt < 80; attempt++)
                {
                    int candidate = pool[Randomizer.rng.Next(0, pool.Length)];
                    if (RandomizerExtras.isAbilityBlocked(candidate))
                        continue;
                    if (used != null && used.Contains(candidate))
                        continue;
                    return candidate;
                }
            }
            return 255;
        }

        // --------------------------------------------------------- inspection
        // Used by the debug report to check the finished files.
        public static int countDuplicateClassAbilities()
        {
            if (Randomizer.classData == null)
                return 0;
            int duplicates = 0;
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)index;
                if (block + 5 >= Randomizer.classData.Length)
                    break;
                List<int> seen = new List<int>();
                for (int i = 0; i < classAbilitySlots.Length; i++)
                {
                    int ability = Randomizer.classData[block + classAbilitySlots[i]];
                    if (ability == 255)
                        continue;
                    if (seen.Contains(ability))
                        duplicates++;
                    else
                        seen.Add(ability);
                }
            }
            return duplicates;
        }

        public static int countDuplicatePersonalAbilities()
        {
            if (Randomizer.personData == null)
                return 0;
            int duplicates = 0;
            List<int> seen = new List<int>();
            for (int index = 0; index < 45; index++)
            {
                if (index >= 35 && index <= 37)
                    continue;
                long block = Randomizer.charSkillStart + Randomizer.charSkillLen * (long)index;
                for (int slot = 20; slot <= 21; slot++)
                {
                    int offset = (int)(block + slot);
                    if (offset < 0 || offset >= Randomizer.personData.Length)
                        continue;
                    int ability = Randomizer.personData[offset];
                    if (ability == 255)
                        continue;
                    if (seen.Contains(ability))
                        duplicates++;
                    else
                        seen.Add(ability);
                }
            }
            return duplicates;
        }

        // Worst growth in the finished class data (0 means "no growth"), so the
        // report can show whether the clamp actually held.
        public static int lowestClassGrowth()
        {
            if (Randomizer.classData == null)
                return 0;
            int lowest = 255;
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 40 >= Randomizer.classData.Length)
                    break;
                for (int i = 0; i < classPlayerGrowthOffsets.Length; i++)
                    if (Randomizer.classData[block + classPlayerGrowthOffsets[i]] < lowest)
                        lowest = Randomizer.classData[block + classPlayerGrowthOffsets[i]];
            }
            return lowest == 255 ? 0 : lowest;
        }

        public static int highestClassGrowth()
        {
            if (Randomizer.classData == null)
                return 0;
            int highest = 0;
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 40 >= Randomizer.classData.Length)
                    break;
                for (int i = 0; i < classPlayerGrowthOffsets.Length; i++)
                {
                    int value = Randomizer.classData[block + classPlayerGrowthOffsets[i]];
                    if (value > highest)
                        highest = value;
                }
                for (int i = 0; i < classEnemyGrowthOffsets.Length; i++)
                {
                    int value = Randomizer.classData[block + classEnemyGrowthOffsets[i]];
                    if (value > highest)
                        highest = value;
                }
            }
            return highest;
        }

        // How many monster class ability slots differ from vanilla right now.
        public static int countChangedBossSlots()
        {
            if (Randomizer.classData == null || Randomizer.classDataV == null)
                return 0;
            int changed = 0;
            for (int index = 0; index < 100; index++)
            {
                if (!isMonsterClass(index))
                    continue;
                long abBlock = Randomizer.classAbStart + Randomizer.classAbLen * (long)index;
                if (abBlock + 5 < Randomizer.classData.Length && abBlock + 5 < Randomizer.classDataV.Length)
                    for (int slot = 0; slot < 5; slot++)
                        if (Randomizer.classData[abBlock + slot] != Randomizer.classDataV[abBlock + slot])
                            changed++;
                long monsterBlock = Randomizer.classMonsterStart + Randomizer.classMonsterLen * (long)index;
                if (monsterBlock + 70 < Randomizer.classData.Length
                    && monsterBlock + 70 < Randomizer.classDataV.Length)
                    for (int slot = 64; slot <= 69; slot++)
                        if (Randomizer.classData[monsterBlock + slot] != Randomizer.classDataV[monsterBlock + slot])
                            changed++;
            }
            return changed;
        }
    }
}
