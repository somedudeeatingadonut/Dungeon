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
using System.Text;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class RandomizerBig
    {
        // -------------------------------------------------------- option state
        public static bool swapClassStatProfiles = false;
        public static bool swapClassGrowthProfiles = false;
        public static bool shuffleCharacterSheets = false;
        public static bool bossRemix = false;

        // weapon / art / gambit effect remix - every graft is its own chance
        // (percent of entries, rolled once per entry)
        public static int weaponGraftChance;        // weapon <- another weapon
        public static int weaponFromArtChance;      // weapon <- a combat art effect
        public static int weaponFromGambitChance;   // weapon <- a gambit
        public static int artGraftChance;           // combat art <- another combat art
        public static int artFromWeaponChance;      // combat art <- a weapon's effect
        public static int gambitGraftChance;        // gambit <- another gambit
        public static int gambitFromWeaponChance;   // gambit <- a weapon's effects
        public static bool uniqueWeaponEffects = false;
        public static bool weaponDonorsDifferInType = false;

        // Writing the new effect into the weapon's own description text. The
        // item text is the only place a player can see what a weapon does, and
        // a grafted effect that is not written there is invisible until it
        // fires. This needs msgdata.bin loaded and makes the run write it.
        public static bool describeWeaponEffects = true;

        public static bool anyEffectRemix()
        {
            return weaponGraftChance > 0 || weaponFromArtChance > 0 || weaponFromGambitChance > 0
                || artGraftChance > 0 || artFromWeaponChance > 0
                || gambitGraftChance > 0 || gambitFromWeaponChance > 0
                || uniqueWeaponEffects;
        }
        /// <summary>0 = inside one class tier, 1 = across every class.</summary>
        public static int swapScope = 0;

        // ------------------------------------------------------------ counters
        public static int statClassStatsSwapped;
        public static int statClassGrowthsSwapped;
        public static int statCharSheetsSwapped;
        public static int statBossRemixed;
        public static int statWeaponEffectsGrafted;
        public static int statWeaponFromArtGrafted;
        public static int statWeaponFromGambitGrafted;
        public static int statArtEffectsGrafted;
        public static int statArtFromWeaponGrafted;
        public static int statGambitEffectsGrafted;
        public static int statGambitFromWeaponGrafted;
        public static int statWeaponEffectsRerolled;
        public static int statWeaponDescriptionsDone;
        // Weapons whose effect changed but that have no description entry at the
        // index this pass writes to. A non-zero value means the message table is
        // laid out differently (another language set, another mod), and the
        // report says so instead of silently describing nothing.
        public static int statWeaponTextMissing;

        public static void resetCounters()
        {
            statClassStatsSwapped = 0;
            statClassGrowthsSwapped = 0;
            statCharSheetsSwapped = 0;
            statBossRemixed = 0;
            statWeaponEffectsGrafted = 0;
            statWeaponFromArtGrafted = 0;
            statWeaponFromGambitGrafted = 0;
            statArtEffectsGrafted = 0;
            statArtFromWeaponGrafted = 0;
            statGambitEffectsGrafted = 0;
            statGambitFromWeaponGrafted = 0;
            statWeaponEffectsRerolled = 0;
            statWeaponDescriptionsDone = 0;
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

        // ------------------------------------- 5. weapon / art / gambit effects
        // Weapons, gambits and combat arts all store their effect in the same id
        // space (weapon and gambit: byte +12 of a 24 byte block, combat art:
        // byte +7 of an 18 byte block, 0..93 with 96 meaning "none"). Every
        // graft below is its own percentage chance, rolled once per entry, so
        // they can be mixed at whatever rate you like.
        //
        // Only the *effects* move. Might / hit / crit / weight / range /
        // durability are left exactly as the normal randomization set them, so
        // this composes with randAtk* instead of overwriting it.
        private const int weaponSlotEffect = 12;
        private const int weaponSlotEffectiveness = 14;
        private const int weaponSlotProperties = 22;
        private const int weaponSlotType = 5;
        private const int artSlotEffect = 7;
        private const int artSlotUsedFlag = 13;   // 63 marks an unused art row
        private const int artCount = 80;
        private const int gambitCount = 80;
        private const int weaponCount = 500;
        private const int effectNone = 96;

        private static void remixWeaponEffects()
        {
            if (Randomizer.itemData == null)
                return;
            if (!anyEffectRemix())
                return;

            remixWeapons();
            remixCombatArts();
            remixGambits();

            if (uniqueWeaponEffects)
                makeWeaponEffectsUnique();

            if (Randomizer.changelog != null)
                appendWeaponChangelog();
        }

        private static bool roll(int percent)
        {
            if (percent <= 0)
                return false;
            if (percent >= 100)
                return true;
            return Randomizer.rng.Next(0, 100) < percent;
        }

        // ------------------------------------------------------- weapons
        private static void remixWeapons()
        {
            byte[] data = Randomizer.itemData;
            for (int index = 0; index < weaponCount; index++)
            {
                int target = (int)Randomizer.weaponBlockStart + (int)Randomizer.weaponBlockLen * index;
                if (target + weaponSlotProperties >= data.Length)
                    break;

                if (roll(weaponGraftChance))
                {
                    int donor = pickWeaponDonor(index);
                    if (donor >= 0)
                    {
                        graftEffect(data, weaponBlock(donor), target);
                        statWeaponEffectsGrafted++;
                        continue;
                    }
                }
                if (roll(weaponFromArtChance))
                {
                    int donor = pickArtDonor(-1);
                    if (donor >= 0)
                    {
                        data[target + weaponSlotEffect] = data[artBlock(donor) + artSlotEffect];
                        statWeaponFromArtGrafted++;
                        continue;
                    }
                }
                if (roll(weaponFromGambitChance))
                {
                    int donor = pickGambitDonor(-1);
                    if (donor >= 0)
                    {
                        graftEffect(data, gambitBlock(donor), target);
                        statWeaponFromGambitGrafted++;
                    }
                }
            }
        }

        // -------------------------------------------------- combat arts
        private static void remixCombatArts()
        {
            byte[] data = Randomizer.itemData;
            for (int index = 0; index < artCount; index++)
            {
                int target = artBlock(index);
                if (target + artSlotUsedFlag >= data.Length)
                    break;
                if (data[target + artSlotUsedFlag] == 63)
                    continue; // unused row

                if (roll(artGraftChance))
                {
                    int donor = pickArtDonor(index);
                    if (donor >= 0)
                    {
                        data[target + artSlotEffect] = data[artBlock(donor) + artSlotEffect];
                        statArtEffectsGrafted++;
                        continue;
                    }
                }
                if (roll(artFromWeaponChance))
                {
                    int donor = pickWeaponDonor(-1);
                    if (donor >= 0)
                    {
                        data[target + artSlotEffect] = data[weaponBlock(donor) + weaponSlotEffect];
                        statArtFromWeaponGrafted++;
                    }
                }
            }
        }

        // ------------------------------------------------------- gambits
        private static void remixGambits()
        {
            byte[] data = Randomizer.itemData;
            for (int index = 0; index < gambitCount; index++)
            {
                int target = gambitBlock(index);
                if (target + weaponSlotProperties >= data.Length)
                    break;
                // completely empty rows are not gambits at all
                if (data[target + weaponSlotEffect] == 0 && data[target + weaponSlotEffectiveness] == 0
                    && data[target + weaponSlotProperties] == 0)
                    continue;

                if (roll(gambitGraftChance))
                {
                    int donor = pickGambitDonor(index);
                    if (donor >= 0)
                    {
                        graftEffect(data, gambitBlock(donor), target);
                        statGambitEffectsGrafted++;
                        continue;
                    }
                }
                if (roll(gambitFromWeaponChance))
                {
                    int donor = pickWeaponDonor(-1);
                    if (donor >= 0)
                    {
                        graftEffect(data, weaponBlock(donor), target);
                        statGambitFromWeaponGrafted++;
                    }
                }
            }
        }

        // ------------------------------------------------- weapon description
        // Where a weapon's description lives in msgdata.bin. The item help text
        // starts at a fixed index and runs in weapon id order (the same base the
        // text options already use).
        public const int weaponDescriptionStart = 11162;

        // 0..299 are the named weapons; the text run ends there.
        public const int weaponDescriptionCount = 300;

        // Builds the description of one weapon from the data that is actually
        // being written, so it names whatever the remix put there - including
        // the parts of the effect that are not an id (magic damage, ignores
        // Def/Res, leaves at 1 HP, double attack, effectiveness).
        public static string describeWeapon(byte[] data, int block)
        {
            if (data == null || block + weaponSlotProperties >= data.Length)
                return "";
            StringBuilder text = new StringBuilder();
            int effect = data[block + weaponSlotEffect];
            if (effect != effectNone && effect >= 0 && effect < Randomizer.effectNames.Length
                && Randomizer.effectNames[effect] != null && Randomizer.effectNames[effect].Length > 0)
            {
                // The name alone ("Inflicts Survival Instinct.") told the player
                // nothing without a wiki, so the effect's own one-line summary
                // follows it. EffectText leaves the line out when there is
                // nothing it can say without guessing.
                text.Append(EffectText.inflictsLine(effect, Randomizer.effectNames[effect])).Append("\n");
            }

            int effectiveness = data[block + weaponSlotEffectiveness];
            if (effectiveness != 0)
            {
                // Same order and names as Randomizer.getClassTyping, which the
                // changelog uses for the effectiveness bits.
                string[] typingNames = { "Infantry", "Heavy Armor", "Cavalry", "Flying", "Dragon", "Monster" };
                StringBuilder list = new StringBuilder();
                for (int i = 0; i < typingNames.Length; i++)
                {
                    if ((effectiveness & (1 << i)) == 0)
                        continue;
                    if (list.Length > 0)
                        list.Append(" / ");
                    list.Append(typingNames[i]);
                }
                if (list.Length > 0)
                    text.Append("Effective against ").Append(list.ToString()).Append(".\n");
            }

            int properties = data[block + weaponSlotProperties];
            if ((properties & 1) != 0)
                text.Append("Deals magic damage.\n");
            if ((properties & 2) != 0)
                text.Append("Ignores Def/Res.\n");
            if ((properties & 4) != 0)
                text.Append("Leaves the target at 1 HP.\n");
            if ((properties & 32) != 0)
                text.Append("Attacks twice when initiating combat.\n");

            return text.ToString().TrimEnd('\n');
        }

        // Drops the lines this tool generates (and only those) from the front of a
        // description, so the vanilla flavour text is what is left.
        private static string stripGeneratedLines(string text)
        {
            if (text == null)
                return "";
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            int first = 0;
            while (first < lines.Length && isGeneratedLine(lines[first]))
                first++;
            if (first >= lines.Length)
                return "";
            StringBuilder builder = new StringBuilder();
            for (int i = first; i < lines.Length; i++)
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(lines[i]);
            }
            return builder.ToString().Trim('\n');
        }

        private static bool isGeneratedLine(string line)
        {
            if (line == null)
                return false;
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
                return false;
            return trimmed.StartsWith("Inflicts ", StringComparison.Ordinal)
                || trimmed.StartsWith("Effective against ", StringComparison.Ordinal)
                || trimmed.StartsWith("Deals magic damage", StringComparison.Ordinal)
                || trimmed.StartsWith("Ignores Def/Res", StringComparison.Ordinal)
                || trimmed.StartsWith("Leaves the target at 1 HP", StringComparison.Ordinal)
                || trimmed.StartsWith("Leaves target at 1 HP", StringComparison.Ordinal)
                || trimmed.StartsWith("Attacks twice", StringComparison.Ordinal)
                || trimmed == "No special attributes.";
        }

        // Rewrites the description of every weapon whose effect changed in this
        // run. The vanilla text is kept underneath when it fits, so the flavour
        // line is not lost, but the new effect is always the first line.
        public static int writeWeaponDescriptions(List<string> strings)
        {
            statWeaponDescriptionsDone = 0;
            statWeaponTextMissing = 0;
            if (!describeWeaponEffects || strings == null)
                return 0;
            if (Randomizer.itemData == null || Randomizer.itemDataV == null)
                return 0;

            byte[] data = Randomizer.itemData;
            byte[] vanilla = Randomizer.itemDataV;
            // The item help text is written in weapon id order from index 11162.
            // The original code only ever wrote 0..299 (the named weapons) and
            // 300 ("Viskam") is past the end of that run of text, so this pass
            // stops at 299 as well.
            for (int index = 0; index < weaponCount && index < weaponDescriptionCount; index++)
            {
                int block = weaponBlock(index);
                if (block + weaponSlotProperties >= data.Length)
                    break;
                if (Randomizer.weaponNames == null || index >= Randomizer.weaponNames.Length
                    || Randomizer.weaponNames[index] == null)
                    continue;

                bool changed = block + weaponSlotProperties < vanilla.Length
                    && (data[block + weaponSlotEffect] != vanilla[block + weaponSlotEffect]
                        || data[block + weaponSlotEffectiveness] != vanilla[block + weaponSlotEffectiveness]
                        || data[block + weaponSlotProperties] != vanilla[block + weaponSlotProperties]);
                if (!changed)
                    continue;

                int textIndex = weaponDescriptionStart + index;
                if (textIndex >= strings.Count)
                {
                    statWeaponTextMissing++;
                    continue;
                }

                string effect = describeWeapon(data, block);
                if (effect.Length == 0)
                    effect = "No special attributes.";
                // The text options on the Music and Text tab already write a
                // description of their own for the weapons they touch, and that
                // one has no flavour line. Strip it before prepending, or the
                // item screen would show the same effect twice.
                string original = stripGeneratedLines(strings[textIndex]);

                // A vanilla description often names the very property that was
                // randomized away ("...will attack twice when initiating
                // combat"), so it is only kept when the property byte is the
                // one the weapon had before. Otherwise the flavour line would
                // contradict the line above it.
                bool sameProperties = block + weaponSlotProperties < vanilla.Length
                    && data[block + weaponSlotProperties] == vanilla[block + weaponSlotProperties];
                string combined = effect;
                if (sameProperties && original != null && original.Length > 0
                    && !original.StartsWith(effect) && effect.Length + original.Length < 220)
                    combined = effect + "\n" + original;
                if (combined.Length > 240)
                    combined = combined.Substring(0, 240);
                if (combined == original)
                    continue;
                strings[textIndex] = combined;
                statWeaponDescriptionsDone++;
            }
            return statWeaponDescriptionsDone;
        }

        // ------------------------------------------------------- uniqueness
        // Effect ids are 0..95 (96 means "none"), so a run with more than 96
        // weapons that have an effect cannot give every one of them its own
        // combination. The pass still does the best it can: it walks the whole id
        // space in random order for each duplicate instead of guessing, which is
        // what leaves only the unavoidable repetitions behind.
        public const int weaponEffectIdSpace = effectNone;

        private static void makeWeaponEffectsUnique()
        {
            byte[] data = Randomizer.itemData;
            List<string> seen = new List<string>();
            int[] candidates = new int[weaponEffectIdSpace];
            for (int i = 0; i < candidates.Length; i++)
                candidates[i] = i;

            for (int index = 0; index < weaponCount; index++)
            {
                int target = weaponBlock(index);
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

                // Shuffle the id space with the seeded rng, then take the first
                // effect that gives this weapon a combination nobody has yet.
                for (int i = candidates.Length - 1; i > 0; i--)
                {
                    int swap = Randomizer.rng.Next(0, i + 1);
                    int hold = candidates[i];
                    candidates[i] = candidates[swap];
                    candidates[swap] = hold;
                }

                byte original = data[target + weaponSlotEffect];
                int found = -1;
                for (int i = 0; i < candidates.Length; i++)
                {
                    data[target + weaponSlotEffect] = (byte)candidates[i];
                    if (!seen.Contains(weaponEffectKey(data, target)))
                    {
                        found = candidates[i];
                        break;
                    }
                }
                if (found < 0)
                {
                    data[target + weaponSlotEffect] = original;
                }
                else
                {
                    seen.Add(weaponEffectKey(data, target));
                    statWeaponEffectsRerolled++;
                }
            }
        }

        // --------------------------------------------------------- helpers
        private static int weaponBlock(int index)
        {
            return (int)Randomizer.weaponBlockStart + (int)Randomizer.weaponBlockLen * index;
        }

        private static int artBlock(int index)
        {
            return (int)Randomizer.artBlockStart + (int)Randomizer.artBlockLen * index;
        }

        private static int gambitBlock(int index)
        {
            return (int)Randomizer.gambitBlockStart + (int)Randomizer.gambitBlockLen * index;
        }

        private static string weaponEffectKey(byte[] data, int block)
        {
            return data[block + weaponSlotEffect].ToString() + "/"
                + data[block + weaponSlotEffectiveness].ToString() + "/"
                + data[block + weaponSlotProperties].ToString();
        }

        // Copies the whole effect set: status effect, effectiveness, properties.
        private static void graftEffect(byte[] data, int source, int target)
        {
            data[target + weaponSlotEffect] = data[source + weaponSlotEffect];
            data[target + weaponSlotEffectiveness] = data[source + weaponSlotEffectiveness];
            data[target + weaponSlotProperties] = data[source + weaponSlotProperties];
        }

        private static int pickWeaponDonor(int targetIndex)
        {
            byte[] data = Randomizer.itemData;
            int targetType = -1;
            if (targetIndex >= 0)
            {
                int targetBlock = weaponBlock(targetIndex);
                if (targetBlock + weaponSlotType < data.Length)
                    targetType = data[targetBlock + weaponSlotType];
            }

            for (int attempt = 0; attempt < 24; attempt++)
            {
                int donor = Randomizer.rng.Next(0, weaponCount);
                if (donor == targetIndex)
                    continue;
                int source = weaponBlock(donor);
                if (source + weaponSlotProperties >= data.Length)
                    continue;
                if (data[source + weaponSlotEffect] == effectNone)
                    continue; // donor has no effect, nothing to give
                if (weaponDonorsDifferInType && targetType >= 0
                    && data[source + weaponSlotType] == targetType)
                    continue;
                return donor;
            }
            return -1;
        }

        private static int pickArtDonor(int skipIndex)
        {
            byte[] data = Randomizer.itemData;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                int donor = Randomizer.rng.Next(0, artCount);
                if (donor == skipIndex)
                    continue;
                int source = artBlock(donor);
                if (source + artSlotUsedFlag >= data.Length)
                    continue;
                if (data[source + artSlotUsedFlag] == 63)
                    continue; // unused row
                if (data[source + artSlotEffect] == effectNone)
                    continue;
                return donor;
            }
            return -1;
        }

        private static int pickGambitDonor(int skipIndex)
        {
            byte[] data = Randomizer.itemData;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                int donor = Randomizer.rng.Next(0, gambitCount);
                if (donor == skipIndex)
                    continue;
                int source = gambitBlock(donor);
                if (source + weaponSlotProperties >= data.Length)
                    continue;
                if (data[source + weaponSlotEffect] == effectNone)
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
            Randomizer.changelog += "\n\n\n---EFFECT REMIX---";
            Randomizer.changelog += "\nWeapon <- weapon: " + statWeaponEffectsGrafted.ToString()
                + "  (chance " + weaponGraftChance + "%)";
            Randomizer.changelog += "\nWeapon <- combat art: " + statWeaponFromArtGrafted.ToString()
                + "  (chance " + weaponFromArtChance + "%)";
            Randomizer.changelog += "\nWeapon <- gambit: " + statWeaponFromGambitGrafted.ToString()
                + "  (chance " + weaponFromGambitChance + "%)";
            Randomizer.changelog += "\nCombat art <- combat art: " + statArtEffectsGrafted.ToString()
                + "  (chance " + artGraftChance + "%)";
            Randomizer.changelog += "\nCombat art <- weapon: " + statArtFromWeaponGrafted.ToString()
                + "  (chance " + artFromWeaponChance + "%)";
            Randomizer.changelog += "\nGambit <- gambit: " + statGambitEffectsGrafted.ToString()
                + "  (chance " + gambitGraftChance + "%)";
            Randomizer.changelog += "\nGambit <- weapon: " + statGambitFromWeaponGrafted.ToString()
                + "  (chance " + gambitFromWeaponChance + "%)";
            if (uniqueWeaponEffects)
                Randomizer.changelog += "\nWeapons with a duplicate effect set re-rolled: " + statWeaponEffectsRerolled.ToString();
            if (weaponDonorsDifferInType)
                Randomizer.changelog += "\nWeapon donors were always of a different weapon type.";

            int shown = 0;
            byte[] data = Randomizer.itemData;
            for (int index = 0; index < weaponCount && shown < 40; index++)
            {
                int block = weaponBlock(index);
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
                string effectDetail = EffectText.forEffect(effect);
                Randomizer.changelog += "\n" + name + " : " + effectName
                    + (effectDetail.Length > 0 ? "  (" + effectDetail + ")" : "");
                shown++;
            }
        }

        // ---------------------------------------------------------- inspection
        public static int countWeaponsWithEffects()
        {
            if (Randomizer.itemData == null)
                return 0;
            int count = 0;
            for (int index = 0; index < weaponCount; index++)
            {
                int block = weaponBlock(index);
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
            for (int index = 0; index < weaponCount; index++)
            {
                int block = weaponBlock(index);
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
