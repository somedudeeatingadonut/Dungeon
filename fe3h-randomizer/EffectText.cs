// -----------------------------------------------------------------------------
// EffectText.cs - what a weapon's effect actually does, in one short line.
//
// describeWeapon used to write "Inflicts Survival Instinct." on the item card,
// which names the effect and says nothing about it: the only way to find out was
// to look the name up. Every effect id the randomizer can give a weapon now has
// a plain-language line that goes after the name:
//
//     Inflicts Survival Instinct - the holder's attacks get stronger as its HP falls.
//
// The text is deliberately short and qualitative. It is a summary of what the
// effect does in play, not the game's own rules text, so it never contradicts
// the game's numbers by quoting different ones. Where the effect's behaviour is
// not something this tool can state with confidence, the line is left out and
// the entry keeps the bare name rather than inventing a rule.
//
// Index = the effect id in the weapon's effect byte (Randomizer.effectNames has
// the matching names; 96 is "none").
// -----------------------------------------------------------------------------

using System;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class EffectText
    {
        private const string sealStr = "after combat the target's Str drops for one turn";
        private const string sealMag = "after combat the target's Mag drops for one turn";
        private const string sealSpd = "after combat the target's Spd drops for one turn";
        private const string sealDef = "after combat the target's Def drops for one turn";
        private const string sealRes = "after combat the target's Res drops for one turn";
        private const string sealMov = "after combat the target's Move drops for one turn";

        private const string rallyStr = "raises nearby allies' Str for one turn";
        private const string rallyMag = "raises nearby allies' Mag for one turn";
        private const string rallySpd = "raises nearby allies' Spd for one turn";
        private const string rallyDef = "raises nearby allies' Def for one turn";
        private const string rallyRes = "raises nearby allies' Res for one turn";
        private const string rallyMov = "raises nearby allies' Move for one turn";
        private const string rallyDex = "raises nearby allies' Dex for one turn";
        private const string rallyLck = "raises nearby allies' Lck for one turn";
        private const string rallyCha = "raises nearby allies' Cha for one turn";

        private const string reinforcedStr = "raises the holder's Str while it is active";
        private const string reinforcedMag = "raises the holder's Mag while it is active";
        private const string reinforcedDex = "raises the holder's Dex while it is active";
        private const string reinforcedSpd = "raises the holder's Spd while it is active";
        private const string reinforcedDef = "raises the holder's Def while it is active";
        private const string reinforcedRes = "raises the holder's Res while it is active";

        private const string poison = "the target loses HP at the start of each turn";
        private const string silence = "the target cannot cast magic for one turn";
        private const string frozen = "the target cannot move or act for one turn";
        private const string rattled = "the target's Hit and Avo drop for one turn";
        private const string luckySeven =
            "the holder gains a random stat boost at the start of each turn";

        private static readonly string[] help = new string[]
        {
            // 0
            "shields the holder from magic for one turn",
            silence,                                                   // 1  Silence
            "lowers the target's Def and Res for one turn",             // 2  Mire
            rattled,                                                   // 3  Swarm
            "cuts the target's movement for one turn",                  // 4  Banshee
            "lights up the fog of war around the user",                 // 5  Torch
            poison,                                                    // 6  Poison
            rattled,                                                   // 7  Rattled
            "the target may turn on its own side",                      // 8  Confusion
            "halves the damage taken and dealt by the units in the area for one turn", // 9
            "cuts the damage taken from monsters for one turn",         // 10 Sacred Shield
            "the target takes extra damage when it attacks",            // 11 Cause and Effect
            "lets the affected units counterattack at any range for one turn", // 12
            "raises the affected units' movement for one turn",         // 13 Stride
            "the affected units survive one lethal hit with 1 HP",       // 14 Blessing
            frozen,                                                    // 15 Frozen
            silence,                                                   // 16 Silenced
            sealStr,                                                   // 17
            sealDef,                                                   // 18
            sealRes,                                                   // 19
            "the holder's attacks get stronger as its HP falls",        // 20 Survival Instinct
            rallyStr,                                                  // 21
            rallyMag,                                                  // 22
            rallySpd,                                                  // 23
            rallyDef,                                                  // 24
            rallyRes,                                                  // 25
            rallyMov,                                                  // 26
            "steadies the holder against being provoked or rattled",    // 27 Calm
            "the holder keeps its spirits up when things go badly",     // 28 Optimist
            "the dance also grants a stat boost to the one it is used on", // 29 Special Dance
            sealStr,                                                   // 30
            sealMag,                                                   // 31
            sealSpd,                                                   // 32
            sealDef,                                                   // 33
            sealRes,                                                   // 34
            sealMov,                                                   // 35
            "the holder fights better when it is the one being attacked", // 36 Ambush Tool
            sealStr,                                                   // 37
            sealMag,                                                   // 38
            sealSpd,                                                   // 39
            sealDef,                                                   // 40
            sealRes,                                                   // 41
            reinforcedStr,                                             // 42
            reinforcedStr,                                             // 43
            reinforcedStr,                                             // 44
            reinforcedStr,                                             // 45
            reinforcedStr,                                             // 46
            reinforcedMag,                                             // 47
            reinforcedMag,                                             // 48
            reinforcedMag,                                             // 49
            reinforcedMag,                                             // 50
            reinforcedMag,                                             // 51
            reinforcedDex,                                             // 52
            reinforcedDex,                                             // 53
            reinforcedDex,                                             // 54
            reinforcedDex,                                             // 55
            reinforcedDex,                                             // 56
            reinforcedSpd,                                             // 57
            reinforcedSpd,                                             // 58
            reinforcedSpd,                                             // 59
            reinforcedSpd,                                             // 60
            reinforcedSpd,                                             // 61
            reinforcedDef,                                             // 62
            reinforcedDef,                                             // 63
            reinforcedDef,                                             // 64
            reinforcedDef,                                             // 65
            reinforcedDef,                                             // 66
            reinforcedRes,                                             // 67
            reinforcedRes,                                             // 68
            reinforcedRes,                                             // 69
            reinforcedRes,                                             // 70
            reinforcedRes,                                             // 71
            rallyDex,                                                  // 72
            rallyLck,                                                  // 73
            rallyCha,                                                  // 74
            "a random strong effect that fires when the holder is in danger", // 75 Fallen Star
            "the holder gains Avo if it waits without acting",          // 76 Alert Stance
            "the holder gains more Avo if it waits without acting",     // 77 Alert Stance+
            "the target cannot move for one turn",                      // 78 Paralysis
            "lowers the target's stats for one turn",                   // 79 Hex
            luckySeven,                                                // 80
            luckySeven,                                                // 81
            luckySeven,                                                // 82
            luckySeven,                                                // 83
            luckySeven,                                                // 84
            luckySeven,                                                // 85
            luckySeven,                                                // 86
            "changes the holder's stats after it takes magic damage",    // 87 Transmute
            "a protective blessing that softens incoming damage",       // 88 Chalice's Protection
            "a protective blessing that keeps the holder standing",     // 89 Chalice's Preservation
            "cuts the damage the holder takes",                         // 90 Absolute Defense
            "a restorative effect from the dining hall",                // 91 Battleground Cafe
            "spends the holder's own HP to help its allies",            // 92 Blood Sacrifice
            "shapes the holder's Reason magic",                         // 93 Stirrings of Reason
            "",                                                        // 94
            "",                                                        // 95
            ""                                                         // 96 = none
        };

        // The extra clarity for one effect id, or "" when there is nothing that
        // can be said about it without guessing.
        public static string forEffect(int effect)
        {
            if (effect < 0 || effect >= help.Length)
                return "";
            string text = help[effect];
            return text == null ? "" : text;
        }

        // "Inflicts Name - what it does." / "Inflicts Name." when nothing is known.
        // The "Inflicts " prefix is what the description pass looks for when it
        // re-reads a card, so it is always there.
        public static string inflictsLine(int effect, string name)
        {
            if (name == null || name.Length == 0)
                return "";
            string detail = forEffect(effect);
            if (detail.Length == 0)
                return "Inflicts " + name + ".";
            return "Inflicts " + name + " - " + detail + ".";
        }

        // Number of ids that have a line, for the debug report.
        public static int describedCount()
        {
            int count = 0;
            for (int i = 0; i < help.Length; i++)
                if (help[i] != null && help[i].Length > 0)
                    count++;
            return count;
        }
    }
}
