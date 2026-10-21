// -----------------------------------------------------------------------------
//  RandomizerExtras.cs
//
//  Four extra randomization options, kept in their own file so the decompiled
//  Randomizer.cs only needs small, targeted hooks.
//
//   1. Ability blocklist      - abilities that must never be handed out by the
//                              randomization (dragon / monster abilities by
//                              default, anything else the user ticks).
//   2. Siege tome limit       - caps the attack range of the weapons given to
//                              map units, so enemies stop spawning with
//                              Meteor / Bolting / longbows.
//   3. Growth shift           - moves part of the Str/Mag/Def/Res enemy
//                              growths into HP, so level scaling makes enemies
//                              a bit bulkier instead of more lethal.
//   4. Even class variety     - hands out random classes with an even spread of
//                              weapon types across every class tier, with an
//                              adjustable "safety" percentage. Also evens out
//                              the class growth / stat / ability / combat art
//                              spread inside each tier.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class RandomizerExtras
    {
        // ------------------------------------------------------- option state
        public static bool blockAbilities = true;
        public static readonly List<int> blockedAbilities = new List<int>();

        public static bool limitSiegeTomes = false;
        public static int maxEnemyWeaponRange = 2;
        public static int siegeChance = 5;

        public static int growthShift = 0;

        public static bool evenClassVariety = false;
        public static int classVarietySafety = 60;

        // Abilities that are dragon / monster only. They are the ones that make
        // randomized runs trivial (Ancient Dragonskin and friends).
        public static readonly int[] defaultBlockedAbilities = { 190, 191, 195, 197, 205, 207 };

        static RandomizerExtras()
        {
            for (int i = 0; i < defaultBlockedAbilities.Length; i++)
                blockedAbilities.Add(defaultBlockedAbilities[i]);
        }

        // ------------------------------------------------- class data offsets
        // Offsets inside one class block (see Randomizer.classBlockStart /
        // classBlockLen). Order is HP, Str, Mag, Dex, Spd, Lck, Def, Res, Cha.
        private static readonly int[] statBoostOffsets = { 16, 39, 40, 41, 42, 43, 44, 45 };
        private static readonly int[] enemyGrowthOffsets = { 19, 30, 31, 32, 33, 34, 35, 36, 38 };
        private static readonly int[] playerGrowthOffsets = { 20, 21, 22, 23, 24, 25, 26, 27, 29 };
        private static readonly int[] baseStatOffsets = { 75, 85, 86, 87, 88, 89, 90, 91, 93 };

        // ------------------------------------------------------ ability block
        public static bool isAbilityBlocked(int ability)
        {
            if (!blockAbilities || blockedAbilities.Count == 0)
                return false;
            return blockedAbilities.Contains(ability);
        }

        private static int[] cachedAbilityPool;
        private static List<int> cachedAbilityAllowed;
        private static int cachedBlockedCount = -1;

        // Called by the UI whenever the blocklist changes.
        public static void notifyBlocklistChanged()
        {
            cachedAbilityPool = null;
            cachedAbilityAllowed = null;
            cachedBlockedCount = -1;
            filteredObtainableWeapons = null;
        }

        // Drop-in replacement for "pool[rng.Next(0, pool.Length)]".
        public static int pickAbility(int[] pool)
        {
            if (pool == null || pool.Length == 0)
                return 0;
            if (!blockAbilities || blockedAbilities.Count == 0)
                return pool[Randomizer.rng.Next(0, pool.Length)];

            if (cachedAbilityPool != pool || cachedBlockedCount != blockedAbilities.Count)
            {
                cachedAbilityPool = pool;
                cachedAbilityAllowed = allowedOnly(pool);
                cachedBlockedCount = blockedAbilities.Count;
            }
            List<int> allowed = cachedAbilityAllowed;
            if (allowed.Count == 0)
                return pool[Randomizer.rng.Next(0, pool.Length)];
            return allowed[Randomizer.rng.Next(0, allowed.Count)];
        }

        public static int pickAbility(List<int> pool)
        {
            if (pool == null || pool.Count == 0)
                return 0;
            if (!blockAbilities || blockedAbilities.Count == 0)
                return pool[Randomizer.rng.Next(0, pool.Count)];

            List<int> allowed = allowedOnly(pool);
            if (allowed.Count == 0)
                return pool[Randomizer.rng.Next(0, pool.Count)];
            return allowed[Randomizer.rng.Next(0, allowed.Count)];
        }

        // Removes the blocked abilities from a pool that is built once.
        public static void removeBlocked(List<int> pool)
        {
            if (pool == null || !blockAbilities)
                return;
            for (int i = 0; i < blockedAbilities.Count; i++)
                while (pool.Remove(blockedAbilities[i]))
                {
                }
        }

        private static List<int> allowedOnly(int[] pool)
        {
            List<int> allowed = new List<int>();
            for (int i = 0; i < pool.Length; i++)
                if (!isAbilityBlocked(pool[i]))
                    allowed.Add(pool[i]);
            return allowed;
        }

        private static List<int> allowedOnly(List<int> pool)
        {
            List<int> allowed = new List<int>();
            for (int i = 0; i < pool.Count; i++)
                if (!isAbilityBlocked(pool[i]))
                    allowed.Add(pool[i]);
            return allowed;
        }

        // --------------------------------------------------- siege tome limit
        // Attack range of a weapon, straight out of the item data.
        public static int weaponRange(int weaponId)
        {
            if (!Randomizer.itemLoaded || Randomizer.itemData == null)
                return 1;
            long offset = Randomizer.weaponBlockStart + Randomizer.weaponBlockLen * (long)weaponId + 7L;
            if (offset < 0 || offset >= Randomizer.itemData.Length)
                return 1;
            return Randomizer.itemData[offset];
        }

        public static bool weaponRangeAllowed(int weaponId)
        {
            if (!limitSiegeTomes || maxEnemyWeaponRange <= 0)
                return true;
            int range = weaponRange(weaponId);
            // 100+ is used by the data for "special" range weapons (staffs etc.)
            if (range >= 100)
                return true;
            return range <= maxEnemyWeaponRange;
        }

        // True when this enemy is allowed to ignore the limit. The siege weapon
        // still has to be rolled from the pool afterwards, so it is a chance on
        // a chance and does not guarantee one.
        public static bool siegeAllowedThisTime()
        {
            if (!limitSiegeTomes || maxEnemyWeaponRange <= 0 || siegeChance <= 0)
                return false;
            if (siegeChance >= 100)
                return true;
            return Randomizer.rng.Next(0, 100) < siegeChance;
        }

        // Same as filterRanges, but rolled once per enemy so a small percentage
        // of enemies can still end up with a siege tome.
        public static void filterRangesForEnemy(List<int> pool)
        {
            if (siegeAllowedThisTime())
                return;
            filterRanges(pool);
        }

        // Removes out-of-range weapons from a pool of weapon ids.
        public static void filterRanges(List<int> pool)
        {
            if (!limitSiegeTomes || maxEnemyWeaponRange <= 0 || pool == null)
                return;
            for (int i = 0; i < pool.Count; )
            {
                if (weaponRangeAllowed(pool[i]))
                    i++;
                else
                    pool.RemoveAt(i);
            }
        }

        private static int[] filteredObtainableWeapons;

        // Random weapon id from the obtainable table, siege weapons filtered out.
        public static int pickEnemyWeapon()
        {
            int[] pool = Randomizer.obtainableWeapons;
            if (limitSiegeTomes && maxEnemyWeaponRange > 0 && !siegeAllowedThisTime())
            {
                if (filteredObtainableWeapons == null)
                    filteredObtainableWeapons = filterRanges(Randomizer.obtainableWeapons);
                pool = filteredObtainableWeapons;
            }
            if (pool == null || pool.Length == 0)
                return 1;
            return pool[Randomizer.rng.Next(0, pool.Length)];
        }

        // Same thing for the fixed obtainableWeapons table.
        public static int[] filterRanges(int[] pool)
        {
            if (!limitSiegeTomes || maxEnemyWeaponRange <= 0 || pool == null)
                return pool;
            List<int> allowed = new List<int>();
            for (int i = 0; i < pool.Length; i++)
                if (weaponRangeAllowed(pool[i]))
                    allowed.Add(pool[i]);
            return allowed.Count > 0 ? allowed.ToArray() : pool;
        }

        // ------------------------------------------------- class variety core
        private const int weaponTypeCount = 8;

        private static List<int>[] weaponTypesOfClass;
        private static List<int>[] tierTypeDeck = new List<int>[8];
        private static List<int> globalTypeDeck;
        private static int[] classUseCount = new int[100];

        public static void beginClassVariety(List<int>[] classWeaponTypes)
        {
            weaponTypesOfClass = classWeaponTypes;
            for (int i = 0; i < tierTypeDeck.Length; i++)
                tierTypeDeck[i] = null;
            globalTypeDeck = null;
            classUseCount = new int[100];
        }

        // Replaces "pool[rng.Next(0, pool.Count)]" for a class pool of one tier.
        public static int pickClass(int tier, List<int> pool)
        {
            if (pool == null || pool.Count == 0)
                return 0;
            if (!evenClassVariety || weaponTypesOfClass == null)
                return pool[Randomizer.rng.Next(0, pool.Count)];

            int type = nextPreferredType(tier, pool);
            List<int> candidates = pool;
            if (type >= 0)
            {
                candidates = classesOfType(pool, type);
                if (candidates.Count == 0)
                    candidates = pool;
            }

            // Prefer classes that have not been handed out yet, so the same
            // class does not show up ten times in a row.
            int lowestUse = int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
                if (classUseCount[candidates[i]] < lowestUse)
                    lowestUse = classUseCount[candidates[i]];

            List<int> best = new List<int>();
            for (int i = 0; i < candidates.Count; i++)
                if (classUseCount[candidates[i]] == lowestUse)
                    best.Add(candidates[i]);

            int chosen = best[Randomizer.rng.Next(0, best.Count)];
            classUseCount[chosen]++;
            return chosen;
        }

        // Used by Randomizer.getBestFittingClass: restricts "optimal" picks to the
        // weapon type that is currently behind in this tier.
        public static int nextPreferredType(int tier, List<int> pool)
        {
            if (!evenClassVariety || weaponTypesOfClass == null)
                return -1;

            int slot = tier;
            if (slot < 0 || slot >= tierTypeDeck.Length)
                slot = 0;

            bool perTier;
            if (classVarietySafety >= 100)
                perTier = true;
            else if (classVarietySafety <= 0)
                perTier = false;
            else
                perTier = Randomizer.rng.Next(0, 100) < classVarietySafety;

            if (perTier)
            {
                if (tierTypeDeck[slot] == null || tierTypeDeck[slot].Count == 0)
                    tierTypeDeck[slot] = buildTypeDeck(pool);
                return popUsableType(tierTypeDeck[slot], pool);
            }

            if (globalTypeDeck == null || globalTypeDeck.Count == 0)
                globalTypeDeck = buildTypeDeck(null);
            return popUsableType(globalTypeDeck, null);
        }

        private static int popUsableType(List<int> deck, List<int> pool)
        {
            for (int i = 0; i < deck.Count; i++)
            {
                int type = deck[i];
                if (pool == null || classesOfType(pool, type).Count > 0)
                {
                    deck.RemoveAt(i);
                    return type;
                }
            }
            return -1;
        }

        private static List<int> buildTypeDeck(List<int> pool)
        {
            bool[] available = new bool[weaponTypeCount];
            if (pool == null)
            {
                for (int i = 0; i < weaponTypeCount; i++)
                    available[i] = true;
            }
            else
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    List<int> types = typesOfClass(pool[i]);
                    for (int j = 0; j < types.Count; j++)
                        if (types[j] >= 0 && types[j] < weaponTypeCount)
                            available[types[j]] = true;
                }
            }

            List<int> deck = new List<int>();
            for (int i = 0; i < weaponTypeCount; i++)
                if (available[i])
                    deck.Add(i);
            deck.Shuffle();
            return deck;
        }

        private static List<int> typesOfClass(int classId)
        {
            List<int> types = new List<int>();
            if (weaponTypesOfClass == null || classId < 0 || classId >= weaponTypesOfClass.Length)
                return types;
            List<int> source = weaponTypesOfClass[classId];
            if (source == null)
                return types;
            for (int i = 0; i < source.Count; i++)
            {
                // 7 and 5 are both "magic" in the class data; keep them apart so
                // dark magic classes still count as casters.
                int type = source[i];
                if (type == 7)
                    type = 5;
                if (!types.Contains(type))
                    types.Add(type);
            }
            return types;
        }

        public static bool classHasType(int classId, int type)
        {
            return typesOfClass(classId).Contains(type);
        }

        private static List<int> classesOfType(List<int> pool, int type)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < pool.Count; i++)
                if (classHasType(pool[i], type))
                    result.Add(pool[i]);
            return result;
        }

        // ------------------------------------------------------ post passes
        public static void applyAfterRandomization()
        {
            if (!Randomizer.classLoaded || Randomizer.classData == null)
                return;
            if (growthShift > 0)
                shiftEnemyGrowths();
            if (evenClassVariety)
            {
                spreadClassStats();
                spreadClassAbilities();
            }
        }

        // Moves part of the Str/Mag/Def/Res enemy growths into HP. Enemy stats
        // scale with these per level, so this is what makes level scaling less
        // about damage and more about bulk.
        private static void shiftEnemyGrowths()
        {
            int shift = Math.Max(0, Math.Min(60, growthShift));
            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 40 >= Randomizer.classData.Length)
                    break;

                int hpOffset = (int)(block + enemyGrowthOffsets[0]);
                if (Randomizer.classData[hpOffset] == 0)
                    continue;

                int moved = 0;
                int[] shiftedStats = { 1, 2, 6, 7 }; // Str, Mag, Def, Res
                for (int i = 0; i < shiftedStats.Length; i++)
                {
                    int offset = (int)(block + enemyGrowthOffsets[shiftedStats[i]]);
                    int value = Randomizer.classData[offset];
                    int reduce = value * shift / 100;
                    moved += reduce;
                    Randomizer.classData[offset] = (byte)Math.Max(0, value - reduce);
                }
                if (moved <= 0)
                    continue;
                int hpGain = moved / shiftedStats.Length;
                int hp = Randomizer.classData[hpOffset] + hpGain;
                Randomizer.classData[hpOffset] = (byte)Math.Min(255, hp);
            }
        }

        // Even spread of the class growths / stats inside every tier: the values
        // themselves stay the same, they are only dealt out so that one class no
        // longer hogs every high roll.
        private static void spreadClassStats()
        {
            List<int>[] tiers = collectTiers();
            spreadOffsets(playerGrowthOffsets, tiers);
            spreadOffsets(enemyGrowthOffsets, tiers);
            spreadOffsets(baseStatOffsets, tiers);
            spreadOffsets(statBoostOffsets, tiers);
        }

        private static List<int>[] collectTiers()
        {
            List<int>[] tiers = new List<int>[8];
            for (int i = 0; i < tiers.Length; i++)
                tiers[i] = new List<int>();

            for (int index = 0; index < 100; index++)
            {
                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 80 >= Randomizer.classData.Length)
                    break;
                if (!(index < 88 || index == 91))
                    continue;
                if (Randomizer.classData[block + 73L] != 255)
                    continue; // monster class, left alone
                int tier = Randomizer.classData[block + 69L];
                if (tier == 5)
                    tier = 3;
                if (tier < 0 || tier >= tiers.Length)
                    continue;
                tiers[tier].Add(index);
            }
            return tiers;
        }

        private static void spreadOffsets(int[] offsets, List<int>[] tiers)
        {
            for (int tier = 0; tier < tiers.Length; tier++)
            {
                List<int> classes = tiers[tier];
                if (classes.Count < 3)
                    continue;

                for (int stat = 0; stat < offsets.Length; stat++)
                {
                    List<int> values = new List<int>();
                    for (int i = 0; i < classes.Count; i++)
                    {
                        long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)classes[i];
                        values.Add(Randomizer.classData[block + offsets[stat]]);
                    }
                    values.Sort();
                    values.Reverse();

                    int start = (stat * 3) % classes.Count;
                    for (int i = 0; i < classes.Count; i++)
                    {
                        int target = classes[(start + i) % classes.Count];
                        long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)target;
                        Randomizer.classData[block + offsets[stat]] = (byte)values[i];
                    }
                }
            }
        }

        // Same idea for class abilities (mastery ability + the three class
        // abilities) and the mastery combat art: the values are rotated inside
        // the tier so neighbouring classes do not end up with the same kit.
        private static void spreadClassAbilities()
        {
            List<int>[] tiers = collectTiers();
            for (int tier = 0; tier < tiers.Length; tier++)
            {
                List<int> classes = tiers[tier];
                if (classes.Count < 3)
                    continue;

                // 0 = mastery ability, 1 = mastery combat art, 2-4 = class abilities
                int[] abilitySlots = { 0, 1, 2, 3, 4 };
                for (int slot = 0; slot < abilitySlots.Length; slot++)
                {
                    long offsetInBlock = (long)abilitySlots[slot];
                    List<int> values = new List<int>();
                    for (int i = 0; i < classes.Count; i++)
                    {
                        long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)classes[i];
                        values.Add(Randomizer.classData[block + offsetInBlock]);
                    }

                    // rotate the values by one class per tier+slot
                    int rotation = (tier + slot + 1) % classes.Count;
                    for (int i = 0; i < classes.Count; i++)
                    {
                        int target = classes[i];
                        int source = values[(i + rotation) % classes.Count];
                        long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)target;
                        Randomizer.classData[block + offsetInBlock] = (byte)source;
                    }
                }
            }
        }
    }
}
