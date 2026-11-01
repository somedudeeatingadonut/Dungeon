// -----------------------------------------------------------------------------
// EffectRules.cs - which status effect can work on which kind of item.
//
// The effect remix (and the ordinary "randomize status effects" option) used to
// hand out any of the 94 status ids to anything. Most of them cannot fire from
// the kinds of item they were landing on, which is why weapons that "raise the
// holder's Str" did nothing at all.
//
// The reason is in the item data itself. A weapon / gambit / magic row is
// 24 bytes and has TWO fields that matter:
//
//   byte +4  the item's use mode  ("ExtraMagEffect": 0 deals damage, 1 heals an
//            ally, 7 use on self, 9 gives 7 Res to an ally, 10 silences,
//            11 rescues, 12 warps, 14 the support gambits)
//   byte +12 the status effect it applies (0..93, 96 = none)
//
// The mode decides WHO the status lands on, so the two have to agree. Vanilla
// data shows it plainly:
//
//   * every damaging item (all 500 weapon rows, the damaging spells, the
//     damaging gambits, the monster area attacks) has mode 0 and - if it has a
//     status at all - a *debuff* status: Poison on the four Venin weapons,
//     Mire / Swarm / Banshee on their spells, the Seals and Frozen on the
//     monster attacks, Poison on Poisoned Arrows.
//   * every *buff* status in the game is carried by a self or ally item:
//     Torch (mode 7, use on self), Ward (mode 9, ally), and Impregnable Wall /
//     Sacred Shield / Retribution / Stride / Blessing / Absolute Defense /
//     Battleground Cafe (mode 14, the support gambits).
//   * not one of the 500 weapons carries a buff status, and no mode-0 item
//     anywhere carries one.
//
// So a buff status on a weapon is a promise the item cannot keep: the attack
// only has the unit it hit to apply it to. The tool now keeps the two in step
// instead of writing effects that cannot happen.
//
// The one exception worth knowing about: the Sword of Seiros, the only vanilla
// weapon with a non-zero mode (6, "heal half damage"), which is how a weapon
// can affect its own holder at all. The tool does not try to fake a self buff
// that way - see the README.
//
// Where the two kinds come from: the game's own status table (fixed_data.bin,
// the 96-entry block of `amount, unknown, VFX, stat`). VFX says which way the
// status points - Debuff / Debuff_Paralysis / Debuff_Rattled /
// Debuff_Confusion for the ones that land on an enemy, Buff / Buff_Alt /
// Buff_Mov for the ones that land on the user or their allies - and `stat` says
// which stat it moves, which is what lets the item card quote the number.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class EffectRules
    {
        public const int statusNone = 96;

        // One character per status id, from the table above:
        //   'a' lands on the unit the attack hit   - safe on a weapon or an art
        //   's' lands on the user or their allies  - only a support mode applies it
        //   '-' no name, nothing uses it           - never handed out
        private const string kinds =
            "saaaasaaassssssaaaaassssssssssaaaaaasaaaaassssssssssssssssssssssssssssssssssssaassssssssssssss--";

        // The status table as the game stores it: 96 entries of four bytes.
        private static readonly int[] amounts = new int[96];
        private static readonly int[] stats = new int[96];
        private static bool tableRead;

        private static readonly string[] statWords =
        {
            "", "", "Str", "Mag", "Dex", "Spd", "Lck", "Def", "Res", "Mov", "Cha",
            "", "", "", "", "", "Avo", "", "", "Hit", "", "all stats"
        };

        // ------------------------------------------------------------- the kinds
        public static bool worksOnAttack(int effect)
        {
            return effect >= 0 && effect < kinds.Length && kinds[effect] == 'a';
        }

        public static bool worksOnSupport(int effect)
        {
            return effect >= 0 && effect < kinds.Length && kinds[effect] == 's';
        }

        public static bool assignable(int effect)
        {
            return worksOnAttack(effect) || worksOnSupport(effect);
        }

        // True when the status is in the id space at all (as opposed to "none").
        public static bool isStatus(int effect)
        {
            return effect >= 0 && effect < statusNone;
        }

        public static string kindName(int effect)
        {
            if (worksOnAttack(effect))
                return "lands on the unit that was hit";
            if (worksOnSupport(effect))
                return "landed on the user or their allies by a support effect";
            return "unused";
        }

        // ------------------------------------------------------- the id pools
        public static int attackCount()
        {
            int count = 0;
            for (int i = 0; i < statusNone; i++)
                if (worksOnAttack(i))
                    count++;
            return count;
        }

        public static int supportCount()
        {
            int count = 0;
            for (int i = 0; i < statusNone; i++)
                if (worksOnSupport(i))
                    count++;
            return count;
        }

        // A random status a damaging item can actually apply.
        public static int randomAttack(Random rng)
        {
            return randomOf(true, rng);
        }

        // The pool that matches an item's own use mode: a damaging use can only
        // leave a debuff on the unit it hit, a support use can only hand out a
        // buff to the user or their allies.
        public static int randomForMode(int mode, Random rng)
        {
            return randomOf(!supportMode(mode), rng);
        }

        // The modes that hand the effect to the user or to their allies. Read
        // off the vanilla items that use each one:
        //   1  heal an ally (Heal, Recover, Physic)      2  heal every ally (Fortify)
        //   3  reduces damage                            6  heals half the damage dealt
        //   7  use on self (Torch)                       8  cures every ally (Restore)
        //   9  gives 7 Res to an ally (Ward)             11 rescue     12 warp
        //   14 the support gambits
        // Everything else is a damaging use, where the only unit the effect can
        // reach is the one that was hit: 0 (every weapon and most spells),
        // 5 and 13 (Meteor and Quake, the area damage spells) and 10 (Silence,
        // which silences the target).
        public static bool supportMode(int mode)
        {
            return mode == 1 || mode == 2 || mode == 3 || mode == 6 || mode == 7
                || mode == 8 || mode == 9 || mode == 11 || mode == 12 || mode == 14;
        }

        private static int randomOf(bool attack, Random rng)
        {
            List<int> pool = new List<int>();
            for (int i = 0; i < statusNone; i++)
                if (attack ? worksOnAttack(i) : worksOnSupport(i))
                    pool.Add(i);
            if (pool.Count == 0 || rng == null)
                return statusNone;
            return pool[rng.Next(0, pool.Count)];
        }

        // --------------------------------------------------- the status table
        // Reads the 96-entry status block out of the loaded item file, so the
        // numbers on the card are the game's own. Walks the file's own section
        // directory instead of trusting an offset, and simply leaves the
        // amounts at zero (no number on the card) if the file is not the one
        // this build expects.
        public static void attach(byte[] itemData)
        {
            tableRead = false;
            for (int i = 0; i < amounts.Length; i++)
            {
                amounts[i] = 0;
                stats[i] = 0;
            }
            if (itemData == null || itemData.Length < 64)
                return;

            try
            {
                int sections = BitConverter.ToInt32(itemData, 0);
                if (sections <= 0 || sections > 64 || 4 + sections * 8 > itemData.Length)
                    return;
                for (int section = 0; section < sections; section++)
                {
                    int pointer = BitConverter.ToInt32(itemData, 4 + section * 8);
                    if (pointer < 0 || pointer + 64 > itemData.Length)
                        continue;
                    int count = BitConverter.ToInt32(itemData, pointer + 4);
                    int blockSize = BitConverter.ToInt32(itemData, pointer + 8);
                    if (count != statusNone || blockSize != 4)
                        continue;
                    int start = pointer + 52 + 12;
                    if (start + count * blockSize > itemData.Length)
                        return;
                    for (int i = 0; i < count; i++)
                    {
                        int raw = itemData[start + i * blockSize];
                        // 250, 251, 253 and 255 are -6, -5, -3 and -1: the same
                        // byte, written as an unsigned value.
                        amounts[i] = raw >= 128 ? raw - 256 : raw;
                        stats[i] = itemData[start + i * blockSize + 3];
                    }
                    // A layout this build does not know would give the cards
                    // nonsense numbers, so check two entries the vanilla table
                    // is unambiguous about (Poison is 5 damage, Seal Strength
                    // moves Str) and leave the amounts at zero otherwise.
                    tableRead = amounts[6] == 5 && stats[30] == 2;
                    if (!tableRead)
                    {
                        for (int i = 0; i < amounts.Length; i++)
                        {
                            amounts[i] = 0;
                            stats[i] = 0;
                        }
                    }
                    return;
                }
            }
            catch
            {
            }
        }

        public static bool tableKnown()
        {
            return tableRead;
        }

        // Signed magnitude of the status, 0 when it does not move a number.
        public static int amount(int effect)
        {
            if (!tableRead || effect < 0 || effect >= amounts.Length)
                return 0;
            return amounts[effect];
        }

        // "Str", "Def", ... or "" when the status is not a plain stat change.
        public static string statName(int effect)
        {
            if (!tableRead || effect < 0 || effect >= stats.Length)
                return "";
            int stat = stats[effect];
            if (stat < 0 || stat >= statWords.Length)
                return "";
            return statWords[stat];
        }

        // ------------------------------------------------- art effect bridge
        // A combat art has no status byte. Byte +7 of an art is a *different*
        // list (CAEffect: 18 leaves the foe at 1 HP, 20 hits twice, 24 freezes,
        // 25 silences, 26/27/28 take 5 Str / Def / Res, 30 restores HP, 43 is
        // effective against everything ...). The remix used to copy those
        // numbers straight into a weapon's status byte and back, which is how a
        // weapon ended up with "Strength Rally" - a number that means nothing on
        // that side. These four maps are the only places where the two lists
        // share a meaning, checked against the vanilla arts (Encloser 24,
        // Ward Arrow 25, Waning Shot 26, Break Shot 27, Schism Shot 28).
        public static int statusForArtEffect(int artEffect, int[] extraPropertiesBits)
        {
            switch (artEffect)
            {
                case 24: return 15; // freezes the foe        -> Frozen
                case 25: return 1;  // silences the foe       -> Silence
                case 26: return 17; // -5 Str for one turn    -> Seal Strength (alt 1)
                case 27: return 18; // -5 Def for one turn    -> Seal Defense (alt 1)
                case 28: return 19; // -5 Res for one turn    -> Seal Resistance (alt 1)
                case 18:
                    if (extraPropertiesBits != null) extraPropertiesBits[0] |= 0x04; // leaves at 1 HP
                    return statusNone;
                case 20: // hits twice
                case 21: // hits three times
                    if (extraPropertiesBits != null) extraPropertiesBits[0] |= 0x20; // attacks twice
                    return statusNone;
                case 43: // effective against every foe
                    if (extraPropertiesBits != null) extraPropertiesBits[1] |= 0x3F;
                    return statusNone;
                default:
                    return -1;
            }
        }

        public static int artEffectForStatus(int status)
        {
            switch (status)
            {
                case 15: return 24;
                case 1:
                case 16: return 25;
                case 17: return 26;
                case 18: return 27;
                case 19: return 28;
                default: return -1;
            }
        }

        // A weapon's property / effectiveness bits can carry the same ideas.
        public static int artEffectForWeapon(int properties, int effectiveness)
        {
            if ((properties & 0x04) != 0)
                return 18; // leaves the foe at 1 HP
            if ((properties & 0x20) != 0)
                return 20; // attacks twice
            if ((effectiveness & 0x3F) == 0x3F)
                return 43; // effective against everything
            return -1;
        }

        // ------------------------------------------------------- debug report
        public static string summary()
        {
            StringBuilder text = new StringBuilder();
            text.Append(attackCount().ToString()).Append(" of ").Append(statusNone.ToString())
                .Append(" status effect(s) land on the unit an attack hits; ")
                .Append(supportCount().ToString())
                .Append(" are self / ally buffs that only a support mode can apply, and ")
                .Append((statusNone - attackCount() - supportCount()).ToString())
                .Append(" are unused ids nobody can be given");
            if (!tableRead)
                text.Append(" (status table not read - no numbers on the cards)");
            return text.ToString();
        }
    }
}
