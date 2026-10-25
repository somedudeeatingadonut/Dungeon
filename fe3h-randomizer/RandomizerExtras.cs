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

        // A class's weapon type and its stats are randomized by two different
        // passes, so nothing stops a class from ending up with swords and a
        // higher Magic base than Strength - which in game reads as "this class
        // gives the wrong bonuses". The identity pass trades Str and Mag when
        // they contradict the weapon type the class actually has, and trims the
        // defensive values so one lucky roll cannot turn an early enemy class
        // into a wall. Bosses and monster rows are never touched.
        public static bool classIdentity = true;
        public static int classDefenseLimit = 50;

        // A flat subtraction from the class bases, applied after every other
        // pass. A class base is a floor for every unit in the class and it is
        // the part that matters at low level, where the enemies of a run feel
        // walled: taking HP / Def / Res off it helps the early maps a lot and
        // the late game only by the same few points. 0 means "no trim".
        //
        // The game keeps one class table, so a class base is shared by the
        // player's units of that class and by the enemy's. That would make the
        // game harder, not easier - which is the opposite of the point - so the
        // same values are added back to the personal bases of every character
        // the player can field (see applyFlatTrim). Enemies, and bosses with
        // them, keep the trim.
        public static int flatHpTrim = 0;
        public static int flatDefTrim = 0;
        public static int flatResTrim = 0;

        // An extra subtraction for the story units, on top of the class trim
        // above. The class table cannot tell a boss from the soldiers standing
        // next to it (they share a class row), but the character table can: a
        // boss is a named character who is not one of the units the player can
        // deploy, and its personal base stats are its own. This is the only
        // per-unit stat lever the game data has - the growths have a separate
        // enemy column, the base stats do not.
        public static int bossHpTrim = 0;
        public static int bossDefTrim = 0;
        public static int bossResTrim = 0;

        // The characters the player can deploy, by character id. The personal
        // base of each of these gets the trim added back, so the option only
        // moves what the player fights. Rows 0..35 are the students, the
        // knights and the church staff (Jeralt included); 1040..1046 are the
        // Ashen Wolves, Jeritza and Anna. Aelfric (1044) is left out: he is a
        // boss, not a deployable unit. Everything else - the enemy generics,
        // Kostas, Lonato, Miklan, the Death Knight and the rest - keeps the
        // trim, which is exactly what is wanted.
        private static readonly int[] playableCharacters =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35,
            1040, 1041, 1042, 1043, 1045, 1046
        };

        // Abilities that are never handed out, whatever the blocklist says.
        // These either hand the holder a whole boss's kit (Twin Crests, Ten
        // Elites, Mighty King of Legend) or make a unit effectively unkillable
        // (Ancient Dragonskin and friends). They stay out of every pool, every
        // learnset and every mastery slot, for players and enemies alike.
        // Dance. Kept as a constant because the Dancer row is given it back
        // after every pass that could have rotated it away.
        public const int danceAbility = 158;
        public static int statDancerDanceRestored;

        public static readonly int[] alwaysBlockedAbilities =
        {
            190, 191, 193, 195, 197, 198, 204, 205, 206, 207, 225, 226, 244, 246, 247,
            // Dance and Special Dance belong to the Dancer alone (the Dancer
            // row is given Dance back after its slots are rolled), and 154 is
            // the blank "Missing Number" entry - neither may be dealt out.
            148, 154, 158
        };

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
        public static int statFirstSpellRank;
        public static int statBlockedAbilitiesRemoved;
        public static int statRoleSwaps;
        public static int statDefenseClamped;
        public static int statFlatTrimmed;
        public static int statFlatCompensated;
        public static int statBossTrimmed;
        public static List<string> roleSwapSamples = new List<string>();
        public static List<string> defenseClampSamples = new List<string>();
        public static List<string> flatTrimSamples = new List<string>();
        public static List<string> flatCompensationSamples = new List<string>();
        public static List<string> bossTrimSamples = new List<string>();
        public static List<int>[] statTypesServedByTier = new List<int>[8];
        public static List<int> statTypesServedGlobal = new List<int>();
        // How many weapon types were actually dealt per tier. The report needs
        // this to judge the spread fairly: a tier that was only dealt 4 types
        // cannot serve 8, whatever the safety level says.
        public static int[] statTypeDrawsByTier = new int[8];

        public static int typeDrawsForTier(int tier)
        {
            if (tier < 0 || tier >= statTypeDrawsByTier.Length)
                return 0;
            return statTypeDrawsByTier[tier];
        }

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
            statBlockedAbilitiesRemoved = 0;
            statDancerDanceRestored = 0;
            statFirstSpellRank = 0;
            statRoleSwaps = 0;
            statDefenseClamped = 0;
            statFlatTrimmed = 0;
            statFlatCompensated = 0;
            statBossTrimmed = 0;
            roleSwapSamples = new List<string>();
            defenseClampSamples = new List<string>();
            flatTrimSamples = new List<string>();
            flatCompensationSamples = new List<string>();
            bossTrimSamples = new List<string>();
            RandomizerMore.resetCounters();
            RandomizerBig.resetCounters();
            ModSupport.resetCounters();
            ModSupport.resetSources();
            for (int i = 0; i < statTypesServedByTier.Length; i++)
                statTypesServedByTier[i] = new List<int>();
            statTypesServedGlobal = new List<int>();
            for (int i = 0; i < statTypeDrawsByTier.Length; i++)
                statTypeDrawsByTier[i] = 0;
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
        public static bool isAlwaysBlocked(int ability)
        {
            for (int i = 0; i < alwaysBlockedAbilities.Length; i++)
                if (alwaysBlockedAbilities[i] == ability)
                    return true;
            return false;
        }

        public static bool isAbilityBlocked(int ability)
        {
            // The forced list is checked even when the user turned the blocklist
            // off: these are the abilities that end a run on their own.
            if (isAlwaysBlocked(ability))
                return true;
            if (!blockAbilities || blockedAbilities.Count == 0)
                return false;
            return blockedAbilities.Contains(ability);
        }

        private static int[] cachedAbilityPool;
        private static List<int> cachedAbilityAllowed;
        private static int cachedBlockedCount = -1;
        private static bool cachedBlocking;

        // Called by the UI whenever the blocklist changes.
        public static void notifyBlocklistChanged()
        {
            cachedAbilityPool = null;
            cachedAbilityAllowed = null;
            cachedBlockedCount = -1;
            cachedBlocking = false;
            filteredObtainableWeapons = null;
        }

        // Drop-in replacement for "pool[rng.Next(0, pool.Length)]".
        public static int pickAbility(int[] pool)
        {
            if (pool == null || pool.Length == 0)
                return 0;

            // The key includes the on/off switch: turning the blocklist off does
            // not change the number of entries in it.
            if (cachedAbilityPool != pool || cachedBlockedCount != blockedAbilities.Count
                || cachedBlocking != blockAbilities)
            {
                cachedAbilityPool = pool;
                cachedAbilityAllowed = allowedOnly(pool);
                cachedBlockedCount = blockedAbilities.Count;
                cachedBlocking = blockAbilities;
            }
            List<int> allowed = cachedAbilityAllowed;
            statAbilityPicks++;
            if (allowed.Count < pool.Length)
                statAbilityBlockedPicks++;
            if (allowed.Count == 0)
            {
                // Every entry of this pool is blocked, which only happens when
                // the pool is tiny. Handing out a blocked ability would defeat
                // the point, so nothing is dealt instead.
                return replacementAbility();
            }
            return allowed[Randomizer.rng.Next(0, allowed.Count)];
        }

        public static int pickAbility(List<int> pool)
        {
            if (pool == null || pool.Count == 0)
                return 0;

            List<int> allowed = allowedOnly(pool);
            statAbilityPicks++;
            if (allowed.Count < pool.Count)
                statAbilityBlockedPicks++;
            if (allowed.Count == 0)
                return replacementAbility();
            return allowed[Randomizer.rng.Next(0, allowed.Count)];
        }

        // Removes the blocked abilities from a pool that is built once.
        public static void removeBlocked(List<int> pool)
        {
            if (pool == null)
                return;
            // The forced list is always removed; the user's list only when the
            // blocklist is on.
            for (int i = 0; i < alwaysBlockedAbilities.Length; i++)
                while (pool.Remove(alwaysBlockedAbilities[i]))
                {
                }
            if (!blockAbilities)
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

        // Rank the first spell of every character spell list is written at.
        // D (rank 2) is what the game itself does. A total conversion that
        // starts units in intermediate classes - No Monastery does - already
        // has D at level 1, so every character begins with a spell; picking C
        // or later pushes the first spell back to when the rank is earned.
        // The spell tables are indexed per half rank (rank / 2), so only the
        // even ranks D, C, B and A mean anything here.
        public static int firstSpellRank(int setting)
        {
            int rank = setting;
            if (rank < 2)
                rank = 2;
            if (rank > 8)
                rank = 8;
            if (rank % 2 != 0)
                rank++;
            statFirstSpellRank = rank;
            return rank;
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
            if (count > 0)
                statSpellPicks++;
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
                if (tier < statTypeDrawsByTier.Length)
                    statTypeDrawsByTier[tier]++;
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
            bool classesLoaded = Randomizer.classLoaded && Randomizer.classData != null;
            if (classesLoaded)
            {
                if (growthShift > 0)
                    shiftEnemyGrowths();
                if (evenClassVariety)
                {
                    spreadClassStats();
                    spreadClassAbilities();
                }
            }

            // The extra options in RandomizerMore.cs: boss abilities, unique
            // personal abilities, no repeats on one class, growth clamp. They
            // guard their own data, so they run even if only the character file
            // was loaded.
            RandomizerMore.applyAfterRandomization();

            // Total conversion support (ModSupport.cs): puts the mod's class
            // mastery EXP values back when mod mode is on. Does nothing
            // otherwise.
            ModSupport.applyAfterRandomization();

            // Large scale passes from RandomizerBig.cs: class stat / growth
            // swaps, character sheet shuffle and the boss remix. They run after
            // the protection passes so a remix can intentionally override them.
            RandomizerBig.applyAfterRandomization();

            if (classesLoaded)
            {
                // Class identity last, so a profile swap or a boss remix cannot
                // leave a sword class with a mage's stats or an early enemy
                // class with a fortress's defence.
                applyClassIdentity();

                // The passes above move whole stat / growth vectors between
                // classes, which can carry a value that the range limits had
                // already clamped back in. Clamping again here means the limits
                // hold for the data that is actually written out.
                RandomizerMore.reapplyRangeLimits();

                // Flat trim last of the stat passes: it is a subtraction the
                // user asked for, so nothing may clamp it back up afterwards.
                applyFlatTrim();
            }

            // Runs last, after every other pass has written its values. It
            // guards its own data, so a run that loaded only the character file
            // still gets its learnsets cleaned.
            enforceAbilityBlocklist();
        }

        // ----------------------------------------------------- flat bulk trim
        // Subtracts a flat number of HP / Def / Res from every class base, and
        // hands the same values back to the personal bases of the characters the
        // player can deploy, so only the enemies (and the bosses) get weaker.
        // The base is what a level 1 unit of the class has, so the early maps
        // feel it most; at level 40 the same few points are noise. Boosts and
        // growths are not touched.
        //
        // Bosses are included: a chapter boss sits in an ordinary class, so it is
        // trimmed with the rest of them. The beast / monster table is a separate
        // layout with no class base stats in it, so it is not touched at all.
        public static void applyFlatTrim()
        {
            int hpTrim = Math.Max(0, Math.Min(60, flatHpTrim));
            int defTrim = Math.Max(0, Math.Min(40, flatDefTrim));
            int resTrim = Math.Max(0, Math.Min(40, flatResTrim));

            // The character file is needed either way: for the class trim it is
            // what keeps the player's own units at full strength, and for the
            // boss trim it is the only table the trim can work on.
            if (Randomizer.personData == null || Randomizer.personDataV == null)
                return;

            if (hpTrim > 0 || defTrim > 0 || resTrim > 0)
                trimClassBases(hpTrim, defTrim, resTrim);

            trimBossCharacters();
        }

        private static void trimClassBases(int hpTrim, int defTrim, int resTrim)
        {
            if (Randomizer.classData == null)
                return;

            for (int index = 0; index < 100; index++)
            {
                if (!(index < 88 || index == 91))
                    continue;

                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                long hpOffset = block + baseStatOffsets[0];
                long defOffset = block + baseStatOffsets[6];
                long resOffset = block + baseStatOffsets[7];
                if (resOffset >= Randomizer.classData.Length)
                    continue;

                bool changed = false;
                int hp = Randomizer.classData[hpOffset];
                int def = Randomizer.classData[defOffset];
                int res = Randomizer.classData[resOffset];
                // Never leave a class on 0 HP; the trim is capped per row, not
                // globally, so one tiny row cannot weaken the others.
                int rowHpTrim = Math.Min(hpTrim, Math.Max(0, hp - 1));
                if (rowHpTrim > 0)
                {
                    Randomizer.classData[hpOffset] = (byte)(hp - rowHpTrim);
                    changed = true;
                }
                if (defTrim > 0 && def > 0)
                {
                    Randomizer.classData[defOffset] = (byte)Math.Max(0, def - defTrim);
                    changed = true;
                }
                if (resTrim > 0 && res > 0)
                {
                    Randomizer.classData[resOffset] = (byte)Math.Max(0, res - resTrim);
                    changed = true;
                }
                if (!changed)
                    continue;

                statFlatTrimmed++;
                if (flatTrimSamples.Count < 6)
                    flatTrimSamples.Add(className(index) + " base HP " + hp.ToString()
                        + (rowHpTrim > 0 ? " -> " + (hp - rowHpTrim).ToString() : "")
                        + ", Def " + def.ToString()
                        + (defTrim > 0 ? " -> " + Math.Max(0, def - defTrim).ToString() : "")
                        + ", Res " + res.ToString()
                        + (resTrim > 0 ? " -> " + Math.Max(0, res - resTrim).ToString() : ""));
            }

            compensatePlayerCharacters(hpTrim, defTrim, resTrim);
        }

        // Gives the trim back to every character the player can field, in that
        // character's own personal bases (character row: HP base +43, Def base
        // +56, Res base +57 - the same offsets the character base randomization
        // writes). The result is that the player's units keep the stats they
        // had while every enemy standing in the same class keeps the trim.
        private static void compensatePlayerCharacters(int hpTrim, int defTrim, int resTrim)
        {
            if (Randomizer.personData == null)
                return;

            for (int i = 0; i < playableCharacters.Length; i++)
            {
                long row = Randomizer.charBlockStart
                    + Randomizer.charBlockLen * (long)playableCharacters[i];
                if (row + 57 >= Randomizer.personData.Length)
                    continue;

                bool changed = false;
                if (hpTrim > 0 && addCapped(Randomizer.personData, (int)(row + 43), hpTrim))
                    changed = true;
                if (defTrim > 0 && addCapped(Randomizer.personData, (int)(row + 56), defTrim))
                    changed = true;
                if (resTrim > 0 && addCapped(Randomizer.personData, (int)(row + 57), resTrim))
                    changed = true;
                if (!changed)
                    continue;

                statFlatCompensated++;
                if (flatCompensationSamples.Count < 4 && Randomizer.characterNames != null
                    && playableCharacters[i] < Randomizer.characterNames.Length
                    && Randomizer.characterNames[playableCharacters[i]] != null)
                    flatCompensationSamples.Add(Randomizer.characterNames[playableCharacters[i]]
                        + " personal base +" + hpTrim + " HP, +" + defTrim + " Def, +" + resTrim
                        + " Res  (so the class trim costs this unit nothing)");
            }
        }

        // The extra trim for the story units. A character row counts as a boss
        // when the tool's name table knows a name for it (the generic soldiers
        // have none) and it is not one of the units the player can deploy. Its
        // personal bases are its own, so this reaches exactly the chapter bosses
        // and story enemies, and stacks on top of the class trim.
        private static void trimBossCharacters()
        {
            int hpTrim = Math.Max(0, Math.Min(60, bossHpTrim));
            int defTrim = Math.Max(0, Math.Min(40, bossDefTrim));
            int resTrim = Math.Max(0, Math.Min(40, bossResTrim));
            if (hpTrim == 0 && defTrim == 0 && resTrim == 0)
                return;
            if (Randomizer.personData == null || Randomizer.characterNames == null)
                return;

            for (int index = 0; index < Randomizer.characterNames.Length; index++)
            {
                if (Randomizer.characterNames[index] == null
                    || Randomizer.characterNames[index].Length == 0)
                    continue;
                // The player's own people are never trimmed here.
                if (isPlayableCharacter(index))
                    continue;

                long row = Randomizer.charBlockStart + Randomizer.charBlockLen * (long)index;
                if (row + 57 >= Randomizer.personData.Length)
                    continue;

                bool changed = false;
                if (hpTrim > 0 && subtractFloored(Randomizer.personData, (int)(row + 43), hpTrim, 1))
                    changed = true;
                if (defTrim > 0 && subtractFloored(Randomizer.personData, (int)(row + 56), defTrim, 0))
                    changed = true;
                if (resTrim > 0 && subtractFloored(Randomizer.personData, (int)(row + 57), resTrim, 0))
                    changed = true;
                if (!changed)
                    continue;

                statBossTrimmed++;
                if (bossTrimSamples.Count < 8)
                    bossTrimSamples.Add(Randomizer.characterNames[index] + " (row "
                        + index.ToString() + ") personal base -" + hpTrim + " HP, -" + defTrim
                        + " Def, -" + resTrim + " Res");
            }
        }

        private static bool isPlayableCharacter(int index)
        {
            for (int i = 0; i < playableCharacters.Length; i++)
                if (playableCharacters[i] == index)
                    return true;
            return false;
        }

        // Subtracts and never goes below the floor (1 for HP, 0 for Def / Res).
        private static bool subtractFloored(byte[] data, int offset, int amount, int floor)
        {
            if (data == null || offset < 0 || offset >= data.Length)
                return false;
            int value = data[offset] - amount;
            if (value < floor)
                value = floor;
            if (value == data[offset])
                return false;
            data[offset] = (byte)value;
            return true;
        }

        // Adds to a byte and refuses to wrap: a personal base is small, but a mod
        // row could be near 255 already.
        private static bool addCapped(byte[] data, int offset, int amount)
        {
            if (data == null || offset < 0 || offset >= data.Length)
                return false;
            int value = data[offset] + amount;
            if (value > 255)
                value = 255;
            if (value == data[offset])
                return false;
            data[offset] = (byte)value;
            return true;
        }

        // ---------------------------------------------------- class identity
        // Which weapon types count as physical. 0 sword, 1 lance, 2 axe, 3 bow,
        // 4 brawl, 5 reason, 6 faith, 7 dark (see Randomizer.getWeaponType).
        private static bool typeIsPhysical(int type)
        {
            return type >= 0 && type < 5;
        }

        public static void applyClassIdentity()
        {
            if (Randomizer.classData == null || Randomizer.classDataV == null)
                return;
            if (!classIdentity)
                return;

            for (int index = 0; index < 100; index++)
            {
                // Rows a mod added past 88 are left alone, exactly like the
                // randomization itself leaves them alone.
                if (!(index < 88 || index == 91))
                    continue;
                if (RandomizerMore.isMonsterClass(index))
                    continue;

                long block = Randomizer.classBlockStart + Randomizer.classBlockLen * (long)index;
                if (block + 94 >= Randomizer.classData.Length || block + 94 >= Randomizer.classDataV.Length)
                    continue;

                int primaryType = Randomizer.classData[block + 79L];
                int secondaryType = Randomizer.classData[block + 80L];
                // 0..7 are the weapon types; anything else is a row this pass
                // does not understand and must not touch.
                if (primaryType > 7)
                    continue;
                // A row the run did not touch is the vanilla class, and a
                // vanilla class is not a contradiction to fix: a few existing
                // rows (59, 86, 91) deliberately have more Mag than Str in a
                // physical class. Only rows the randomizer changed are checked.
                if (!classRowChanged(block))
                    continue;
                bool physical = typeIsPhysical(primaryType);
                bool hybrid = secondaryType != 0 && typeIsPhysical(secondaryType) != physical;

                // A class built to use both a physical and a magic weapon on
                // purpose keeps whatever it rolled: it is not contradicting
                // itself, it is a hybrid.
                if (!hybrid)
                    matchRole(index, block, physical);

                if (classDefenseLimit > 0)
                    toneDownDefense(index, block);
            }
        }

        // True when anything this pass looks at differs from the vanilla row.
        private static bool classRowChanged(long block)
        {
            int[] offsets = { 16, 19, 20, 29, 30, 31, 32, 33, 34, 35, 36, 38,
                              39, 40, 41, 42, 43, 44, 45, 75, 79, 80,
                              85, 86, 87, 88, 89, 90, 91, 93 };
            for (int i = 0; i < offsets.Length; i++)
            {
                long offset = block + offsets[i];
                if (offset >= Randomizer.classData.Length || offset >= Randomizer.classDataV.Length)
                    return false;
                if (Randomizer.classData[offset] != Randomizer.classDataV[offset])
                    return true;
            }
            return false;
        }

        // Trades Str and Mag (base stats, class boosts, both growth sets) when
        // the class's highest of the two does not belong to the weapon type it
        // now has. Swapping instead of re-rolling keeps every value the run
        // produced - only which stat owns it changes.
        private static void matchRole(int classIndex, long block, bool physical)
        {
            int strBase = Randomizer.classData[block + baseStatOffsets[1]];
            int magBase = Randomizer.classData[block + baseStatOffsets[2]];
            if (physical == (strBase >= magBase))
                return;

            swapStat(block, baseStatOffsets, 1, 2);
            swapStat(block, statBoostOffsets, 1, 2);
            swapStat(block, enemyGrowthOffsets, 1, 2);
            swapStat(block, playerGrowthOffsets, 1, 2);

            statRoleSwaps++;
            if (roleSwapSamples.Count < 6)
            {
                string name = Randomizer.classNames != null && Randomizer.classNames[classIndex] != null
                    ? Randomizer.classNames[classIndex] : "class " + classIndex.ToString();
                roleSwapSamples.Add(name + " (" + (physical ? "physical" : "magic")
                    + " weapon): Str " + strBase.ToString() + " / Mag " + magBase.ToString()
                    + " -> Str " + magBase.ToString() + " / Mag " + strBase.ToString());
            }
        }

        private static void swapStat(long block, int[] offsets, int first, int second)
        {
            int firstOffset = (int)(block + offsets[first]);
            int secondOffset = (int)(block + offsets[second]);
            if (firstOffset >= Randomizer.classData.Length || secondOffset >= Randomizer.classData.Length)
                return;
            byte hold = Randomizer.classData[firstOffset];
            Randomizer.classData[firstOffset] = Randomizer.classData[secondOffset];
            Randomizer.classData[secondOffset] = hold;
        }

        // Keeps a class's defensive values within reach of the ones it had in
        // the vanilla file. The base stat is a floor for every unit in the class
        // and the boost is added on top, so an unlucky pair of rolls is enough
        // to put a beginner class in fortress armour - which is what made whole
        // enemy armies feel bloated. Vanilla rows (untouched by the run) and
        // boss / monster rows are skipped.
        private static void toneDownDefense(int classIndex, long block)
        {
            bool changed = false;
            for (int stat = 6; stat <= 7; stat++) // Def, Res
            {
                int vanillaBase = Randomizer.classDataV[block + baseStatOffsets[stat]];
                int allowedBase = vanillaBase + Math.Max(2, vanillaBase * classDefenseLimit / 100);
                int baseOffset = (int)(block + baseStatOffsets[stat]);
                int currentBase = Randomizer.classData[baseOffset]; // 0..99, unsigned
                if (currentBase > allowedBase)
                {
                    Randomizer.classData[baseOffset] = (byte)allowedBase;
                    if (defenseClampSamples.Count < 6)
                        defenseClampSamples.Add(className(classIndex) + " base "
                            + (stat == 6 ? "Def" : "Res") + " " + currentBase.ToString()
                            + " -> " + allowedBase.ToString() + " (vanilla " + vanillaBase.ToString() + ")");
                    changed = true;
                }

                // Class boosts are signed: a randomized -3 is stored as 253, so
                // the comparison and the clamp both have to be done on the
                // signed value or a negative roll would be "clamped" upwards.
                int vanillaBoost = toSigned(Randomizer.classDataV[block + statBoostOffsets[stat]]);
                int allowedBoost = vanillaBoost
                    + Math.Max(3, Math.Abs(vanillaBoost) * classDefenseLimit / 100);
                int boostOffset = (int)(block + statBoostOffsets[stat]);
                int currentBoost = toSigned(Randomizer.classData[boostOffset]);
                if (currentBoost > allowedBoost)
                {
                    Randomizer.classData[boostOffset] = toByte(allowedBoost);
                    if (defenseClampSamples.Count < 6)
                        defenseClampSamples.Add(className(classIndex) + " boost "
                            + (stat == 6 ? "Def" : "Res") + " " + currentBoost.ToString()
                            + " -> " + allowedBoost.ToString() + " (vanilla " + vanillaBoost.ToString() + ")");
                    changed = true;
                }
            }
            if (changed)
                statDefenseClamped++;
        }

        private static int toSigned(byte value)
        {
            return value < 128 ? (int)value : (int)value - 256;
        }

        private static byte toByte(int value)
        {
            if (value < -128)
                value = -128;
            if (value > 127)
                value = 127;
            return (byte)(value & 0xFF);
        }

        private static string className(int classIndex)
        {
            if (Randomizer.classNames != null && classIndex >= 0 && classIndex < Randomizer.classNames.Length
                && Randomizer.classNames[classIndex] != null)
                return Randomizer.classNames[classIndex];
            return "class " + classIndex.ToString();
        }

        // ------------------------------------------------- blocklist safety net
        // A blocked ability can still end up in a new slot when a later pass
        // *moves* values around instead of picking them (the even-variety
        // ability spread does exactly that). This pass looks at every ability slot that belongs
        // to the randomization and puts a different ability there if a blocked
        // one was moved in.
        //
        // Placements that were already in the vanilla file (the monster
        // classes' own abilities, for example) are left alone on purpose - the
        // blocklist only stops the randomizer from *introducing* them.
        public static void enforceAbilityBlocklist()
        {
            // The fixed list is not optional: with the user list switched off
            // there is still something to strip out.
            if (!blockAbilities && alwaysBlockedAbilities.Length == 0)
                return;

            if (Randomizer.classData != null)
            {
                for (int index = 0; index < 100; index++)
                {
                    long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)index;
                    if (block + 5 >= Randomizer.classData.Length)
                        break;
                    for (int slot = 0; slot < 5; slot++)
                        replaceIntroducedAbility(Randomizer.classData, Randomizer.classDataV, (int)(block + slot));
                }

                for (int index = 0; index < 100; index++)
                {
                    long block = Randomizer.classMonsterStart + Randomizer.classMonsterLen * (long)index;
                    if (block + 70 >= Randomizer.classData.Length)
                        break;
                    for (int offsetInBlock = 64; offsetInBlock <= 69; offsetInBlock++)
                        replaceIntroducedAbility(Randomizer.classData, Randomizer.classDataV, (int)(block + offsetInBlock));
                }
            }

            if (Randomizer.personData != null)
            {
                int rowCount = Randomizer.personData.Length / (int)Randomizer.charLearnLen;
                if (rowCount > 1201 * 12)
                    rowCount = 1201 * 12;
                for (int row = 0; row < rowCount; row++)
                {
                    long block = Randomizer.charLearnStart + Randomizer.charLearnLen * (long)row;
                    if (block + Randomizer.charLearnLen > Randomizer.personData.Length)
                        break;
                    // 0..4 are combat arts, 11..18 are the learned abilities.
                    for (int slot = 11; slot < 19; slot++)
                        replaceIntroducedAbility(Randomizer.personData, Randomizer.personDataV, (int)(block + slot));
                }
            }

            if (Randomizer.classData != null && Randomizer.classDataV != null)
                restoreDancerDance();
        }

        // Dance belongs to the Dancer. The even-variety ability spread rotates
        // whole slots between the classes of a tier, so the Dancer's own slot
        // can be handed another class's ability - which is how the 1.12.7 run
        // shipped a Dancer without Dance. The vanilla row says which slot it is,
        // so the value is put back instead of being guessed.
        private static void restoreDancerDance()
        {
            if (Randomizer.classData == null || Randomizer.classDataV == null)
                return;
            int dancer = ModSupport.DancerClassIndex;
            if (dancer < 0 || dancer >= 100)
                return;

            long block = Randomizer.classAbStart + Randomizer.classAbLen * (long)dancer;
            if (block + Randomizer.classAbLen > Randomizer.classData.Length)
                return;
            if (block + Randomizer.classAbLen > Randomizer.classDataV.Length)
                return;

            for (int slot = 0; slot < Randomizer.classAbLen; slot++)
            {
                if (Randomizer.classDataV[block + slot] != danceAbility)
                    continue;
                if (Randomizer.classData[block + slot] == danceAbility)
                    return;
                Randomizer.classData[block + slot] = (byte)danceAbility;
                statDancerDanceRestored++;
                return;
            }
        }

        private static void replaceIntroducedAbility(byte[] data, byte[] vanilla, int offset)
        {
            if (data == null || offset < 0 || offset >= data.Length)
                return;
            int ability = data[offset];
            if (!isAbilityBlocked(ability))
                return;
            if (vanilla != null && offset < vanilla.Length && vanilla[offset] == ability)
                return; // it was there before the randomization, leave it
            data[offset] = (byte)replacementAbility();
            statBlockedAbilitiesRemoved++;
        }

        private static int replacementAbility()
        {
            if (Randomizer.genericAbilities != null && Randomizer.genericAbilities.Length > 0)
            {
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    int candidate = Randomizer.genericAbilities[
                        Randomizer.rng.Next(0, Randomizer.genericAbilities.Length)];
                    if (!isAbilityBlocked(candidate))
                        return candidate;
                }
            }
            return 255; // no ability
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
                        // Never rotate a blocked ability into a slot that did not
                        // already have it - that would slip past the blocklist.
                        if (isAbilityBlocked(source) && Randomizer.classData[block + offsetInBlock] != source)
                            continue;
                        Randomizer.classData[block + offsetInBlock] = (byte)source;
                    }
                }
            }
        }
    }
}
