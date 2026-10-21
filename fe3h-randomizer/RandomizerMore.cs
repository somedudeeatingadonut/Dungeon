// -----------------------------------------------------------------------------
//  RandomizerMore.cs
//
//  The extra randomization options that live outside the decompiled code. They
//  are all implemented as passes that run *after* the normal randomization has
//  finished, so they only ever look at the finished byte arrays and tidy them
//  up - the main randomizer is not touched by any of this.
//
//   bosses      keep boss / monster class abilities      (on by default)
//               keep boss / monster class stats          (on by default)
//   abilities   no repeated ability on one class         (on by default)
//               a different mastery ability per tier     (off)
//               unique personal abilities                (off)
//   growths     grow, but only inside this range         (off, Growth Boosts tab)
//               even growth totals                       (off)
//   stats       stat boosts only inside this range       (off, Stat Boosts tab)
//               base stats only inside this range        (off, Stat Bases tab)
//
//  Every pass only replaces a value that the randomization *introduced*. Data
//  that was already in the vanilla file - Dancer's Dance, a boss's own
//  abilities and stats, a stat a class simply does not grow in - is never
//  touched.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class RandomizerMore
    {
        // -------------------------------------------------------- option state
        // bosses
        public static bool protectBossAbilities = true;
        public static bool protectBossStats = true;
        public static int bossScalingShare = 25;

        // abilities
        public static bool uniqueClassAbilities = true;
        public static bool uniqueTierMastery = false;
        public static bool uniqueTierMasteryArt = false;
        public static bool uniquePersonalAbilities = false;

        // growths
        public static bool clampGrowths = false;
        public static int growthClampMin = 10;
        public static int growthClampMax = 70;
        public static bool evenGrowthTotals = false;
        public static int growthTotalTolerance = 15;
        public static bool protectGrowthIdentity = false;

        // stats
        public static bool clampStatBoosts = false;
        public static int statBoostClampMin = -3;
        public static int statBoostClampMax = 10;
        public static bool clampStatBases = false;
        public static int statBaseClampMin = 3;
        public static int statBaseClampMax = 30;
        public static bool evenStatTotals = false;
        public static int statTotalTolerance = 15;

        // ------------------------------------------------------------ counters
        public static int statBossSlotsRestored;
        public static int statBossStatSlotsRestored;
        public static int statDuplicatePersonalRerolled;
        public static int statDuplicateClassAbRerolled;
        public static int statTierMasteryRerolled;
        public static int statGrowthsClamped;
        public static int statGrowthTotalsEvened;
        public static int statStatBoostsClamped;
        public static int statStatBasesClamped;
        public static int statBossScalingBonus;
        public static int statBossScalingBoostBonus;
        public static int statBossScalingClassCount;
        public static int statTierMasteryArtRerolled;
        public static int statGrowthIdentityKept;
        public static int statStatTotalsEvened;

        public static void resetCounters()
        {
            statBossSlotsRestored = 0;
            statBossStatSlotsRestored = 0;
            statDuplicatePersonalRerolled = 0;
            statDuplicateClassAbRerolled = 0;
            statTierMasteryRerolled = 0;
            statGrowthsClamped = 0;
            statGrowthTotalsEvened = 0;
            statStatBoostsClamped = 0;
            statStatBasesClamped = 0;
            statBossScalingBonus = 0;
            statBossScalingBoostBonus = 0;
            statBossScalingClassCount = 0;
            statTierMasteryArtRerolled = 0;
            statGrowthIdentityKept = 0;
            statStatTotalsEvened = 0;
        }

        // ------------------------------------------------------------- the run
        public static void applyAfterRandomization()
        {
            // bosses first, so the passes below already see the vanilla values
            restoreBossStats();
            restoreBossAbilities();

            removeDuplicateClassAbilities();
            makeTierMasteryUnique();
            makeTierMasteryArtUnique();
            makePersonalAbilitiesUnique();

            applyGrowthIdentity();
            applyEvenGrowthTotals();
            clampAllGrowths();
            applyEvenStatTotals();
            clampAllStatBoosts();
            clampAllStatBases();

            // Last, so a range limit can never clamp the boss bonus back off the
            // restored vanilla values.
            applyBossScalingBonus();
        }

        // ---------------------------------------------------------- offsets
        private static readonly int[] classAbilitySlots = { 0, 2, 3, 4 };
        private static readonly int[] classStatBoostOffsets = { 16, 39, 40, 41, 42, 43, 44, 45, 46, 47 };
        private static readonly int[] classBaseStatOffsets = { 75, 85, 86, 87, 88, 89, 90, 91, 93 };
        private static readonly int[] classPlayerGrowthOffsets = { 20, 21, 22, 23, 24, 25, 26, 27, 29 };
        private static readonly int[] classEnemyGrowthOffsets = { 19, 30, 31, 32, 33, 34, 35, 36, 38 };
        private static readonly int[] charGrowthOffsets = { 41, 60, 61, 62, 63, 64, 65, 66, 68 };

        private static Settings settings
        {
            get { return Randomizer.settingsForDebug; }
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
                        restoreClassSlot((int)(abBlock + slot), ref statBossSlotsRestored);

                long monsterBlock = Randomizer.classMonsterStart + Randomizer.classMonsterLen * (long)index;
                if (monsterBlock + 70 < Randomizer.classData.Length
                    && monsterBlock + 70 < Randomizer.classDataV.Length)
                    for (int slot = 64; slot <= 69; slot++)
                        restoreClassSlot((int)(monsterBlock + slot), ref statBossSlotsRestored);
            }
        }

        private static void restoreBossStats()
        {
            if (!protectBossStats)
                return;
            if (Randomizer.classData == null || Randomizer.classDataV == null)
                return;

            for (int index = 0; index < 100; index++)
            {
                if (!isMonsterClass(index))
                    continue;
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 94 >= Randomizer.classData.Length || block + 94 >= Randomizer.classDataV.Length)
                    continue;

                for (int i = 0; i < classStatBoostOffsets.Length; i++)
                    restoreClassSlot((int)(block + classStatBoostOffsets[i]), ref statBossStatSlotsRestored);
                for (int i = 0; i < classBaseStatOffsets.Length; i++)
                    restoreClassSlot((int)(block + classBaseStatOffsets[i]), ref statBossStatSlotsRestored);
            }
        }

        // Level scaling raises enemy levels by the level multiplier. Bosses keep
        // their vanilla stats (above), so without this they would fall behind
        // every other enemy in the run. They get a share of that scaling as a
        // flat bump to their base stats - a quarter of it by default, which is
        // far less than a normal enemy gets, but enough that a boss does not
        // become a pushover in the late game.
        private static void applyBossScalingBonus()
        {
            if (!protectBossStats || bossScalingShare <= 0)
                return;
            if (Randomizer.classData == null)
                return;
            Settings current = settings;
            if (current == null || !current.changeLv || current.lvMultiplier <= 100)
                return;

            int share = Math.Max(0, Math.Min(100, bossScalingShare));
            // lvMultiplier is a double (150.0 = 150%); keep the math in ints so
            // the result cannot pick up stray decimals.
            int extraPercent = (int)((current.lvMultiplier - 100.0) * share / 100.0);
            if (extraPercent <= 0)
                return;
            if (extraPercent > 50)
                extraPercent = 50; // a boss is still meant to be beatable

            for (int index = 0; index < 100; index++)
            {
                if (!isMonsterClass(index))
                    continue;
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 94 >= Randomizer.classData.Length)
                    continue;

                int raisedHere = 0;
                for (int i = 0; i < classBaseStatOffsets.Length; i++)
                {
                    int offset = (int)(block + classBaseStatOffsets[i]);
                    int value = Randomizer.classData[offset];
                    if (value == 0)
                        continue; // no base in this stat, do not invent one
                    int bonus = value * extraPercent / 100;
                    if (bonus < 1)
                        bonus = 1;
                    int raised = value + bonus;
                    if (raised > 255)
                        raised = 255;
                    Randomizer.classData[offset] = (byte)raised;
                    raisedHere++;
                }
                statBossScalingBonus += raisedHere;

                // Some entries have no base stats in the class table at all (the
                // game reads those from elsewhere). Rather than do nothing, fall
                // back to the class stat boosts, which every class has.
                if (raisedHere == 0)
                {
                    for (int i = 0; i < classStatBoostOffsets.Length; i++)
                    {
                        int offset = (int)(block + classStatBoostOffsets[i]);
                        int value = (sbyte)Randomizer.classData[offset];
                        if (value == 0)
                            continue;
                        int bonus = Math.Abs(value) * extraPercent / 100;
                        if (bonus < 1)
                            bonus = 1;
                        int raised = value > 0 ? value + bonus : value - bonus;
                        if (raised > 127)
                            raised = 127;
                        if (raised < -128)
                            raised = -128;
                        Randomizer.classData[offset] = (byte)(sbyte)raised;
                        raisedHere++;
                        statBossScalingBoostBonus++;
                    }
                }
                if (raisedHere > 0)
                    statBossScalingClassCount++;
            }
        }

        private static void restoreClassSlot(int offset, ref int counter)
        {
            if (offset < 0 || offset >= Randomizer.classData.Length || offset >= Randomizer.classDataV.Length)
                return;
            if (Randomizer.classData[offset] == Randomizer.classDataV[offset])
                return;
            Randomizer.classData[offset] = Randomizer.classDataV[offset];
            counter++;
        }

        // ------------------------------------------- 2. abilities on a class
        // Slots 0 (mastery ability), 2, 3 and 4 (class abilities) are abilities;
        // slot 1 is the mastery combat art.
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
                    if (isVanillaValue(Randomizer.classDataV, offset, ability))
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

        // One mastery ability per tier: if two classes in the same tier end up
        // with the same mastery ability, the later one is re-rolled. Only values
        // the randomization put there are touched.
        private static void makeTierMasteryUnique()
        {
            if (!uniqueTierMastery)
                return;
            if (Randomizer.classData == null)
                return;

            List<int>[] tiers = new List<int>[8];
            for (int i = 0; i < tiers.Length; i++)
                tiers[i] = new List<int>();
            collectTiers(tiers);

            for (int tier = 0; tier < tiers.Length; tier++)
            {
                List<int> seen = new List<int>();
                List<int> classes = tiers[tier];
                for (int i = 0; i < classes.Count; i++)
                {
                    int offset = (int)(Randomizer.classAbStart + Randomizer.classAbLen * (long)classes[i]);
                    if (offset < 0 || offset >= Randomizer.classData.Length)
                        continue;
                    int ability = Randomizer.classData[offset];
                    if (ability == 255)
                        continue;
                    if (!seen.Contains(ability))
                    {
                        seen.Add(ability);
                        continue;
                    }
                    if (isVanillaValue(Randomizer.classDataV, offset, ability))
                        continue;
                    int replacement = pickUnusedAbility(seen);
                    if (replacement == 255)
                        continue;
                    Randomizer.classData[offset] = (byte)replacement;
                    seen.Add(replacement);
                    statTierMasteryRerolled++;
                }
            }
        }

        // The same idea for the mastery combat art (slot 1 of the class ability
        // block). Combat arts are not abilities, so the blocklist does not apply.
        private static void makeTierMasteryArtUnique()
        {
            if (!uniqueTierMasteryArt)
                return;
            if (Randomizer.classData == null || Randomizer.combatArtNames == null)
                return;

            List<int>[] tiers = new List<int>[8];
            for (int i = 0; i < tiers.Length; i++)
                tiers[i] = new List<int>();
            collectTiers(tiers);

            for (int tier = 0; tier < tiers.Length; tier++)
            {
                List<int> seen = new List<int>();
                List<int> classes = tiers[tier];
                for (int i = 0; i < classes.Count; i++)
                {
                    long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)classes[i];
                    if (block + 1 >= Randomizer.classData.Length)
                        continue;
                    int offset = (int)(block + 1L);
                    int art = Randomizer.classData[offset];
                    if (art == 255)
                        continue;
                    if (!seen.Contains(art))
                    {
                        seen.Add(art);
                        continue;
                    }
                    if (isVanillaValue(Randomizer.classDataV, offset, art))
                        continue;
                    int replacement = pickUnusedCombatArt(seen);
                    if (replacement == 255)
                        continue;
                    Randomizer.classData[offset] = (byte)replacement;
                    seen.Add(replacement);
                    statTierMasteryArtRerolled++;
                }
            }
        }

        private static int pickUnusedCombatArt(List<int> used)
        {
            string[] names = Randomizer.combatArtNames;
            if (names == null || names.Length == 0)
                return 255;
            for (int attempt = 0; attempt < 120; attempt++)
            {
                int candidate = Randomizer.rng.Next(1, names.Length);
                if (candidate >= names.Length || names[candidate] == null || names[candidate].Length == 0)
                    continue;
                if (used != null && used.Contains(candidate))
                    continue;
                return candidate;
            }
            return 255;
        }

        // ------------------------------------------------- 3. one skill each
        // The personal ability sits at +20 (pre timeskip) and +21 (post) of every
        // character skill block; the playable characters are rows 0..44.
        private static void makePersonalAbilitiesUnique()
        {
            if (!uniquePersonalAbilities)
                return;
            if (Randomizer.personData == null || Randomizer.personDataV == null)
                return;
            if (settings != null && !settings.randPersonalAb)
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
                    if (isVanillaValue(Randomizer.personDataV, offset, ability))
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

        // -------------------------------------------------------- 4. growths
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
                bool playerOn = settings == null || settings.randPlayerGrowthBoosts;
                bool enemyOn = settings == null || settings.randEnemyGrowthBoosts;
                // Only the classes the rest of the tool randomizes. The monster
                // / boss rows keep their own data (that is what the boss option
                // is for), and some of their slots do not hold growths at all.
                List<int> classes = collectSpreads();
                for (int c = 0; c < classes.Count; c++)
                {
                    long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classes[c];
                    if (block + 40 >= Randomizer.classData.Length)
                        break;
                    if (playerOn)
                        for (int i = 0; i < classPlayerGrowthOffsets.Length; i++)
                            clampValue(Randomizer.classData, Randomizer.classDataV,
                                (int)(block + classPlayerGrowthOffsets[i]), low, high, false, ref statGrowthsClamped);
                    if (enemyOn)
                        for (int i = 0; i < classEnemyGrowthOffsets.Length; i++)
                            clampValue(Randomizer.classData, Randomizer.classDataV,
                                (int)(block + classEnemyGrowthOffsets[i]), low, high, false, ref statGrowthsClamped);
                }
            }

            if (Randomizer.personData != null && (settings == null || settings.randCharGrowths))
            {
                for (int index = 0; index < 1201; index++)
                {
                    long block = Randomizer.charBlockStart + Randomizer.charBlockLen * (long)index;
                    if (block + 69 >= Randomizer.personData.Length)
                        break;
                    for (int i = 0; i < charGrowthOffsets.Length; i++)
                        clampValue(Randomizer.personData, Randomizer.personDataV,
                            (int)(block + charGrowthOffsets[i]), low, high, false, ref statGrowthsClamped);
                }
            }
        }

        // Every class grows the same total per level, give or take the tolerance.
        // The ratio between a class's growths is kept, so a slow, tanky class
        // stays slow and tanky - it just grows the same amount overall.
        private static void applyEvenGrowthTotals()
        {
            if (!evenGrowthTotals)
                return;
            if (Randomizer.classData == null)
                return;
            if (settings != null && !settings.randPlayerGrowthBoosts)
                return;

            List<int> classes = collectSpreads();
            if (classes.Count < 2)
                return;

            long total = 0;
            int counted = 0;
            for (int i = 0; i < classes.Count; i++)
            {
                int sum = growthSum(classes[i]);
                if (sum <= 0)
                    continue;
                total += sum;
                counted++;
            }
            if (counted < 2)
                return;
            int target = (int)(total / counted);
            if (target <= 0)
                return;

            int tolerance = Math.Max(0, Math.Min(90, growthTotalTolerance));
            for (int i = 0; i < classes.Count; i++)
            {
                int sum = growthSum(classes[i]);
                if (sum <= 0)
                    continue;
                int allowed = Math.Max(1, target * tolerance / 100);
                if (Math.Abs(sum - target) <= allowed)
                    continue;
                scaleGrowths(classes[i], sum, target);
                statGrowthTotalsEvened++;
            }
        }

        private static int growthSum(int classIndex)
        {
            long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classIndex;
            if (block + 40 >= Randomizer.classData.Length)
                return 0;
            int sum = 0;
            for (int i = 0; i < classPlayerGrowthOffsets.Length; i++)
                sum += Randomizer.classData[block + classPlayerGrowthOffsets[i]];
            return sum;
        }

        private static void scaleGrowths(int classIndex, int currentTotal, int target)
        {
            long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classIndex;
            for (int i = 0; i < classPlayerGrowthOffsets.Length; i++)
            {
                int offset = (int)(block + classPlayerGrowthOffsets[i]);
                int value = Randomizer.classData[offset];
                if (value == 0)
                    continue; // no growth in this stat, keep it that way
                int scaled = (int)Math.Round((double)value * target / currentTotal);
                if (scaled < 1)
                    scaled = 1;
                if (scaled > 255)
                    scaled = 255;
                Randomizer.classData[offset] = (byte)scaled;
            }
        }

        // Every class keeps the shape it is known for: whichever growth stat was
        // its best in the vanilla file is still its best afterwards. Values are
        // only swapped between two stats, so the growth total is unchanged.
        private static void applyGrowthIdentity()
        {
            if (!protectGrowthIdentity)
                return;
            if (Randomizer.classData == null || Randomizer.classDataV == null)
                return;
            Settings current = settings;
            if (current != null && !current.randPlayerGrowthBoosts)
                return;

            List<int> classes = collectSpreads();
            for (int i = 0; i < classes.Count; i++)
            {
                int classIndex = classes[i];
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classIndex;
                if (block + 40 >= Randomizer.classData.Length || block + 40 >= Randomizer.classDataV.Length)
                    continue;

                int vanillaBest = -1;
                int vanillaBestValue = -1;
                for (int stat = 0; stat < classPlayerGrowthOffsets.Length; stat++)
                {
                    int value = Randomizer.classDataV[block + classPlayerGrowthOffsets[stat]];
                    if (value > vanillaBestValue)
                    {
                        vanillaBestValue = value;
                        vanillaBest = stat;
                    }
                }
                if (vanillaBest < 0 || vanillaBestValue <= 0)
                    continue;

                int currentBest = -1;
                int currentBestValue = -1;
                for (int stat = 0; stat < classPlayerGrowthOffsets.Length; stat++)
                {
                    int value = Randomizer.classData[block + classPlayerGrowthOffsets[stat]];
                    if (value > currentBestValue)
                    {
                        currentBestValue = value;
                        currentBest = stat;
                    }
                }
                if (currentBest < 0 || currentBest == vanillaBest)
                    continue;

                int keep = (int)(block + classPlayerGrowthOffsets[vanillaBest]);
                int give = (int)(block + classPlayerGrowthOffsets[currentBest]);
                if (Randomizer.classData[keep] > Randomizer.classData[give])
                    continue;
                byte swap = Randomizer.classData[keep];
                Randomizer.classData[keep] = Randomizer.classData[give];
                Randomizer.classData[give] = swap;
                statGrowthIdentityKept++;
            }
        }

        // ----------------------------------------------------------- 5. stats
        private static void clampAllStatBoosts()
        {
            if (!clampStatBoosts)
                return;
            if (Randomizer.classData == null)
                return;

            int low = Math.Max(-128, Math.Min(statBoostClampMin, 127));
            int high = Math.Max(-128, Math.Min(statBoostClampMax, 127));
            if (low > high)
            {
                int swap = low;
                low = high;
                high = swap;
            }

            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 48 >= Randomizer.classData.Length)
                    break;
                for (int i = 0; i < classStatBoostOffsets.Length; i++)
                    clampValue(Randomizer.classData, Randomizer.classDataV,
                        (int)(block + classStatBoostOffsets[i]), low, high, true, ref statStatBoostsClamped);
            }
        }

        private static void clampAllStatBases()
        {
            if (!clampStatBases)
                return;
            if (Randomizer.classData == null)
                return;

            int low = Math.Max(0, Math.Min(statBaseClampMin, 255));
            int high = Math.Max(0, Math.Min(statBaseClampMax, 255));
            if (low > high)
            {
                int swap = low;
                low = high;
                high = swap;
            }

            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 94 >= Randomizer.classData.Length)
                    break;
                for (int i = 0; i < classBaseStatOffsets.Length; i++)
                    clampValue(Randomizer.classData, Randomizer.classDataV,
                        (int)(block + classBaseStatOffsets[i]), low, high, false, ref statStatBasesClamped);
            }
        }

        // Same idea as the growth totals, for base stats: every class ends up
        // with roughly the same stat total, keeping the ratio between its stats
        // so the class still feels like itself.
        private static void applyEvenStatTotals()
        {
            if (!evenStatTotals)
                return;
            if (Randomizer.classData == null)
                return;
            Settings current = settings;
            if (current != null && !current.randClassBases)
                return;

            List<int> classes = collectSpreads();
            if (classes.Count < 2)
                return;

            long total = 0;
            int counted = 0;
            for (int i = 0; i < classes.Count; i++)
            {
                int sum = baseStatSum(classes[i]);
                if (sum <= 0)
                    continue;
                total += sum;
                counted++;
            }
            if (counted < 2)
                return;
            int target = (int)(total / counted);
            if (target <= 0)
                return;

            int tolerance = Math.Max(0, Math.Min(90, statTotalTolerance));
            for (int i = 0; i < classes.Count; i++)
            {
                int sum = baseStatSum(classes[i]);
                if (sum <= 0 || sum == target)
                    continue;
                int allowed = Math.Max(1, target * tolerance / 100);
                if (Math.Abs(sum - target) <= allowed)
                    continue;
                scaleBaseStats(classes[i], sum, target);
                statStatTotalsEvened++;
            }
        }

        private static int baseStatSum(int classIndex)
        {
            long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classIndex;
            if (block + 94 >= Randomizer.classData.Length)
                return 0;
            int sum = 0;
            for (int i = 0; i < classBaseStatOffsets.Length; i++)
                sum += Randomizer.classData[block + classBaseStatOffsets[i]];
            return sum;
        }

        private static void scaleBaseStats(int classIndex, int currentTotal, int target)
        {
            long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classIndex;
            for (int i = 0; i < classBaseStatOffsets.Length; i++)
            {
                int offset = (int)(block + classBaseStatOffsets[i]);
                int value = Randomizer.classData[offset];
                if (value == 0)
                    continue; // the class has no base in this stat, leave it
                int scaled = (int)Math.Round((double)value * target / currentTotal);
                if (scaled < 1)
                    scaled = 1;
                if (scaled > 255)
                    scaled = 255;
                Randomizer.classData[offset] = (byte)scaled;
            }
        }

        // ------------------------------------------------------------ helpers
        // Only values the randomization introduced are clamped: if the byte is
        // still the vanilla one, or the stat has no value at all (0), it is left
        // exactly as it is.
        private static void clampValue(byte[] data, byte[] vanilla, int offset, int low, int high,
            bool signed, ref int counter)
        {
            if (data == null || offset < 0 || offset >= data.Length)
                return;
            int value = signed ? (int)(sbyte)data[offset] : (int)data[offset];
            if (!signed && value == 0)
                return;
            if (vanilla != null && offset < vanilla.Length && vanilla[offset] == data[offset])
                return;
            int clamped = value < low ? low : (value > high ? high : value);
            if (clamped == value)
                return;
            data[offset] = (byte)(sbyte)clamped;
            counter++;
        }

        private static bool isVanillaValue(byte[] vanilla, int offset, int value)
        {
            return vanilla != null && offset >= 0 && offset < vanilla.Length && vanilla[offset] == value;
        }

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

        // Non-monster classes, the same set the even-variety code works on.
        private static List<int> collectSpreads()
        {
            List<int> classes = new List<int>();
            if (Randomizer.classData == null)
                return classes;
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 80 >= Randomizer.classData.Length)
                    break;
                if (!(index < 88 || index == 91))
                    continue;
                if (isMonsterClass(index))
                    continue;
                classes.Add(index);
            }
            return classes;
        }

        private static void collectTiers(List<int>[] tiers)
        {
            List<int> classes = collectSpreads();
            for (int i = 0; i < classes.Count; i++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classes[i];
                int tier = Randomizer.classData[block + 69];
                if (tier == 5)
                    tier = 3;
                if (tier < 0 || tier >= tiers.Length)
                    continue;
                tiers[tier].Add(classes[i]);
            }
        }

        // --------------------------------------------------------- inspection
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

        public static int countDuplicateTierMastery()
        {
            if (Randomizer.classData == null)
                return 0;
            List<int>[] tiers = new List<int>[8];
            for (int i = 0; i < tiers.Length; i++)
                tiers[i] = new List<int>();
            collectTiers(tiers);
            int duplicates = 0;
            for (int tier = 0; tier < tiers.Length; tier++)
            {
                List<int> seen = new List<int>();
                List<int> classes = tiers[tier];
                for (int i = 0; i < classes.Count; i++)
                {
                    int offset = (int)(Randomizer.classAbStart + Randomizer.classAbLen * (long)classes[i]);
                    if (offset < 0 || offset >= Randomizer.classData.Length)
                        continue;
                    int ability = Randomizer.classData[offset];
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

        // Growth totals per class, for the report: lowest, highest, average.
        public static void growthTotals(out int lowest, out int highest, out int average)
        {
            lowest = 0;
            highest = 0;
            average = 0;
            if (Randomizer.classData == null)
                return;
            List<int> classes = collectSpreads();
            int counted = 0;
            long total = 0;
            for (int i = 0; i < classes.Count; i++)
            {
                int sum = growthSum(classes[i]);
                if (sum <= 0)
                    continue;
                if (counted == 0 || sum < lowest)
                    lowest = sum;
                if (counted == 0 || sum > highest)
                    highest = sum;
                total += sum;
                counted++;
            }
            if (counted > 0)
                average = (int)(total / counted);
        }

        // Worst and best growth value in the finished class data.
        public static void growthValues(out int lowest, out int highest)
        {
            int ignoredClass;
            bool playerSide;
            growthValues(out lowest, out highest, out ignoredClass, out playerSide);
        }

        // Same, but also says where the highest growth sits, so an out of range
        // value can be tracked down from the report alone.
        public static void growthValues(out int lowest, out int highest, out int highestClass, out bool highestIsPlayer)
        {
            lowest = 0;
            highest = 0;
            highestClass = -1;
            highestIsPlayer = false;
            if (Randomizer.classData == null)
                return;

            List<int> classes = collectSpreads();
            int low = 255;
            int high = 0;
            for (int c = 0; c < classes.Count; c++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classes[c];
                if (block + 40 >= Randomizer.classData.Length)
                    break;
                for (int i = 0; i < classPlayerGrowthOffsets.Length; i++)
                {
                    int value = Randomizer.classData[block + classPlayerGrowthOffsets[i]];
                    if (value < low)
                        low = value;
                    if (value > high)
                    {
                        high = value;
                        highestClass = classes[c];
                        highestIsPlayer = true;
                    }
                }
                for (int i = 0; i < classEnemyGrowthOffsets.Length; i++)
                {
                    int value = Randomizer.classData[block + classEnemyGrowthOffsets[i]];
                    if (value < low)
                        low = value;
                    if (value > high)
                    {
                        high = value;
                        highestClass = classes[c];
                        highestIsPlayer = false;
                    }
                }
            }
            lowest = low == 255 ? 0 : low;
            highest = high;
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

        // Same for the monster class stat slots.
        public static int countChangedBossStats()
        {
            if (Randomizer.classData == null || Randomizer.classDataV == null)
                return 0;
            int changed = 0;
            for (int index = 0; index < 100; index++)
            {
                if (!isMonsterClass(index))
                    continue;
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 94 >= Randomizer.classData.Length || block + 94 >= Randomizer.classDataV.Length)
                    continue;
                for (int i = 0; i < classStatBoostOffsets.Length; i++)
                    if (Randomizer.classData[block + classStatBoostOffsets[i]] != Randomizer.classDataV[block + classStatBoostOffsets[i]])
                        changed++;
                for (int i = 0; i < classBaseStatOffsets.Length; i++)
                    if (Randomizer.classData[block + classBaseStatOffsets[i]] != Randomizer.classDataV[block + classBaseStatOffsets[i]])
                        changed++;
            }
            return changed;
        }

        // Stat boost / base stat extremes in the finished data, for the report.
        public static void statValues(out int boostLow, out int boostHigh, out int baseLow, out int baseHigh)
        {
            boostLow = 0;
            boostHigh = 0;
            baseLow = 0;
            baseHigh = 0;
            if (Randomizer.classData == null)
                return;

            int bLow = 127;
            int bHigh = -128;
            int sLow = 255;
            int sHigh = 0;
            // Only the classes the range limits are meant to constrain. Monster
            // and boss rows keep their own numbers on purpose, so including them
            // would report the vanilla values as a limit failure.
            List<int> classes = collectSpreads();
            for (int c = 0; c < classes.Count; c++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classes[c];
                if (block + 94 >= Randomizer.classData.Length)
                    break;
                for (int i = 0; i < classStatBoostOffsets.Length; i++)
                {
                    int value = (sbyte)Randomizer.classData[block + classStatBoostOffsets[i]];
                    if (value < bLow)
                        bLow = value;
                    if (value > bHigh)
                        bHigh = value;
                }
                for (int i = 0; i < classBaseStatOffsets.Length; i++)
                {
                    int value = Randomizer.classData[block + classBaseStatOffsets[i]];
                    if (value == 0)
                        continue; // a class with no base in this stat, not a zero base
                    if (value < sLow)
                        sLow = value;
                    if (value > sHigh)
                        sHigh = value;
                }
            }
            if (bLow != 127)
            {
                boostLow = bLow;
                boostHigh = bHigh;
            }
            if (sLow != 255)
            {
                baseLow = sLow;
                baseHigh = sHigh;
            }
        }
    }
}
