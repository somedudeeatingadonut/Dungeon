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
            for (int i = 0; i < statTypesServedByTier.Length; i++)
                statTypesServedByTier[i] = new List<int>();
        }

        // ------------------------------------------------------ run counters
        // Reset at the start of every randomization and printed by the debug
        // report, so it is visible whether each new feature actually ran.
        public static int statClassPicks;
        public static int statClassTypeDraws;
        public static int statClassTypeBagRefills;
        public static int statTypeDeckRebuilds;
        public static int statAbilityPicks;
        public static int statAbilityBlockedPicks;
        public static int statRangeFilterCalls;
        public static int statRangeFilteredWeapons;
        public static int statSiegeBypassRolls;
        public static int statSpellPicks;
        public static int statSpellsFiltered;
        public static int statGrowthShiftedClasses;
        public static int statSpreadPasses;
        public static List<int>[] statTypesServedByTier = new List<int>[8];
        public static List<int> statTypesServedGlobal = new List<int>();

        public static void beginRandomization()
        {
            statClassPicks = 0;
            statClassTypeDraws = 0;
            statClassTypeBagRefills = 0;
            statTypeDeckRebuilds = 0;
            statAbilityPicks = 0;
            statAbilityBlockedPicks = 0;
            statRangeFilterCalls = 0;
            statRangeFilteredWeapons = 0;
            statSiegeBypassRolls = 0;
            statSpellPicks = 0;
            statSpellsFiltered = 0;
            statGrowthShiftedClasses = 0;
            statSpreadPasses = 0;
            for (int i = 0; i < statTypesServedByTier.Length; i++)
                statTypesServedByTier[i] = new List<int>();
            statTypesServedGlobal = new List<int>();
            resetClassVariety();
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
            statAbilityPicks++;
            if (allowed.Count < pool.Length)
                statAbilityBlockedPicks++;
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
            statAbilityPicks++;
            if (allowed.Count < pool.Count)
                statAbilityBlockedPicks++;
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
            {
                statSiegeBypassRolls++;
                return true;
            }
            if (Randomizer.rng.Next(0, 100) < siegeChance)
            {
                statSiegeBypassRolls++;
                return true;
            }
            return false;
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
            statRangeFilterCalls++;
            for (int i = 0; i < pool.Count; )
            {
                if (weaponRangeAllowed(pool[i]))
                    i++;
                else
                {
                    pool.RemoveAt(i);
                    statRangeFilteredWeapons++;
                }
            }
        }

        // Siege *spells* (Meteor, Bolting, ...) live in the magic table, not the
        // weapon table, so they need their own range lookup.
        public static int spellRange(int spellId)
        {
            if (!Randomizer.itemLoaded || Randomizer.itemData == null)
                return 0;
            long offset = Randomizer.magicBlockStart + Randomizer.magicBlockLen * (long)spellId + 7L;
            if (offset < 0 || offset >= Randomizer.itemData.Length)
                return 0;
            return Randomizer.itemData[offset];
        }

        public static bool spellRangeAllowed(int spellId)
        {
            if (!limitSiegeTomes || maxEnemyWeaponRange <= 0)
                return true;
            int range = spellRange(spellId);
            if (range <= 0 || range >= 100)
                return true;
            return range <= maxEnemyWeaponRange;
        }

        // Drop-in replacement for getSpell(...) on the enemy side only, so player
        // spell lists keep their siege tomes.
        public static int pickEnemySpell(int type, int rank, bool forceRank, bool forceOffensive)
        {
            int spell = Randomizer.getSpell(type, rank, forceRank, forceOffensive);
            if (!limitSiegeTomes || maxEnemyWeaponRange <= 0)
                return spell;

            statSpellPicks++;
            if (siegeAllowedThisTime())
                return spell;
            if (spellRangeAllowed(spell))
                return spell;

            int closest = spell;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int candidate = Randomizer.getSpell(type, rank, forceRank, forceOffensive);
                if (spellRangeAllowed(candidate))
                {
                    statSpellsFiltered++;
                    return candidate;
                }
                if (spellRange(candidate) < spellRange(closest))
                    closest = candidate;
            }
            statSpellsFiltered++;
            return closest;
        }

        // Replacement for "rng.Next(0, 38)" where the enemy spell slot was just a
        // random spell index.
        public static int pickRandomEnemySpell(int count)
        {
            int spell = Randomizer.rng.Next(0, count);
            if (!limitSiegeTomes || maxEnemyWeaponRange <= 0 || count <= 0)
                return spell;
            if (siegeAllowedThisTime() || spellRangeAllowed(spell))
                return spell;

            List<int> allowed = new List<int>();
            for (int i = 0; i < count; i++)
                if (spellRangeAllowed(i))
                    allowed.Add(i);
            statSpellsFiltered++;
            if (allowed.Count == 0)
                return spell;
            return allowed[Randomizer.rng.Next(0, allowed.Count)];
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

        private static List<int>[] classTypeBag = new List<int>[8];
        private static List<int> globalClassTypeBag;

        public static void beginClassVariety(List<int>[] classWeaponTypes)
        {
            weaponTypesOfClass = classWeaponTypes;
            resetClassVariety();
        }

        public static void resetClassVariety()
        {
            for (int i = 0; i < tierTypeDeck.Length; i++)
            {
                tierTypeDeck[i] = null;
                classTypeBag[i] = null;
            }
            globalTypeDeck = null;
            globalClassTypeBag = null;
            classUseCount = new int[100];
        }

        // 100% = strict per tier, 0% = only the whole run has to be covered.
        private static bool perTierMode()
        {
            if (classVarietySafety >= 100)
                return true;
            if (classVarietySafety <= 0)
                return false;
            return Randomizer.rng.Next(0, 100) < classVarietySafety;
        }

        private static void recordTypeServed(int tier, int type)
        {
            if (type < 0 || type >= weaponTypeCount)
                return;
            if (tier >= 0 && tier < statTypesServedByTier.Length)
            {
                if (statTypesServedByTier[tier] == null)
                    statTypesServedByTier[tier] = new List<int>();
                if (!statTypesServedByTier[tier].Contains(type))
                    statTypesServedByTier[tier].Add(type);
            }
            else
            {
                if (statTypesServedGlobal == null)
                    statTypesServedGlobal = new List<int>();
                if (!statTypesServedGlobal.Contains(type))
                    statTypesServedGlobal.Add(type);
            }
        }

        // Replaces "pool[rng.Next(0, pool.Count)]" for a class pool of one tier.
        public static int pickClass(int tier, List<int> pool)
        {
            if (pool == null || pool.Count == 0)
                return 0;
            if (!evenClassVariety || weaponTypesOfClass == null)
                return pool[Randomizer.rng.Next(0, pool.Count)];

            statClassPicks++;

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

            if (perTierMode())
            {
                if (tierTypeDeck[slot] == null || tierTypeDeck[slot].Count == 0)
                {
                    tierTypeDeck[slot] = buildTypeDeck(pool);
                    statTypeDeckRebuilds++;
                }
                return popUsableType(tierTypeDeck[slot], pool, slot);
            }

            if (globalTypeDeck == null || globalTypeDeck.Count == 0)
            {
                globalTypeDeck = buildTypeDeck(null);
                statTypeDeckRebuilds++;
            }
            return popUsableType(globalTypeDeck, null, -1);
        }

        private static int popUsableType(List<int> deck, List<int> pool, int tier)
        {
            for (int i = 0; i < deck.Count; i++)
            {
                int type = deck[i];
                if (pool == null || classesOfType(pool, type).Count > 0)
                {
                    deck.RemoveAt(i);
                    recordTypeServed(tier, type);
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

        // Balances the weapon types the *classes themselves* get. Randomizer.cs
        // calls this instead of rng.Next(0, 7) when class weapon types are
        // randomized, so a tier cannot turn into fifteen axe classes.
        public static int nextClassWeaponType(int tier, int slot)
        {
            if (!evenClassVariety)
                return -1;

            int safeTier = tier;
            if (safeTier < 0 || safeTier >= classTypeBag.Length)
                safeTier = 0;

            if (slot <= 0 || perTierMode())
            {
                if (classTypeBag[safeTier] == null || classTypeBag[safeTier].Count == 0)
                {
                    classTypeBag[safeTier] = buildClassTypeBag();
                    statClassTypeBagRefills++;
                }
                List<int> bag = classTypeBag[safeTier];
                int type = bag[bag.Count - 1];
                bag.RemoveAt(bag.Count - 1);
                recordTypeServed(safeTier, type);
                statClassTypeDraws++;
                return type;
            }

            if (globalClassTypeBag == null || globalClassTypeBag.Count == 0)
            {
                globalClassTypeBag = buildClassTypeBag();
                statClassTypeBagRefills++;
            }
            int globalType = globalClassTypeBag[globalClassTypeBag.Count - 1];
            globalClassTypeBag.RemoveAt(globalClassTypeBag.Count - 1);
            recordTypeServed(-1, globalType);
            statClassTypeDraws++;
            return globalType;
        }

        private static List<int> buildClassTypeBag()
        {
            List<int> bag = new List<int>();
            for (int copy = 0; copy < 2; copy++)
                for (int type = 0; type < weaponTypeCount; type++)
                    bag.Add(type);
            bag.Shuffle();
            return bag;
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
                // 7 and 5 are both "magic" in the class data; keep them together
                // so dark magic classes still count as casters.
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
                statGrowthShiftedClasses++;
            }
        }

        // Even spread of the class growths / stats inside every tier: the values
        // themselves stay the same, they are only dealt out so that one class no
        // longer hogs every high roll. Only values the user is already
        // randomizing are touched, so vanilla kits are never disturbed.
        private static void spreadClassStats()
        {
            Settings settings = Randomizer.settingsForDebug;
            if (settings == null)
                return;

            List<int>[] tiers = collectTiers();
            if (settings.randPlayerGrowthBoosts)
                spreadOffsets(playerGrowthOffsets, tiers);
            if (settings.randEnemyGrowthBoosts)
                spreadOffsets(enemyGrowthOffsets, tiers);
            if (settings.randClassBases)
                spreadOffsets(baseStatOffsets, tiers);
            if (settings.randStatBoosts)
                spreadOffsets(statBoostOffsets, tiers);
            statSpreadPasses++;
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
            Settings settings = Randomizer.settingsForDebug;
            if (settings == null)
                return;

            // Only rotate the slots the user is randomizing anyway.
            List<int> slots = new List<int>();
            if (settings.randClassAb && settings.randClassMasteryAb)
                slots.Add(0);
            if (settings.randClassMasteryCa)
                slots.Add(1);
            if (settings.randClassAb && settings.randClassClassAb)
            {
                slots.Add(2);
                slots.Add(3);
                slots.Add(4);
            }
            if (slots.Count == 0)
                return;

            statSpreadPasses++;
            List<int>[] tiers = collectTiers();
            for (int tier = 0; tier < tiers.Length; tier++)
            {
                List<int> classes = tiers[tier];
                if (classes.Count < 3)
                    continue;

                int[] abilitySlots = slots.ToArray();
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
