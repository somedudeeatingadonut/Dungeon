// -----------------------------------------------------------------------------
//  Presets.cs
//
//  Saves and restores a whole configuration in one click.
//
//  A preset is a plain text file of "name=value" lines:
//
//      randClassBases=True             a Settings field
//      RandomizerBig.swapScope=1       an option from the passes in this project
//
//  Applying a preset resets everything to the tool's defaults first, so what you
//  get is exactly what the preset describes, not "the preset plus whatever was
//  already ticked". A saved preset is a full snapshot; the built in one only
//  lists the values that differ from the defaults.
//
//  The built in preset is the owner's own saved settings of the run he actually
//  played (2026-10-24, No Monastery 2.1), so that run can be repeated. The mod
//  safe presets are the same run with every option the mod owns turned off, and
//  with the enemy bulk dial turned up.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class RandomizerPresets
    {
        public sealed class Preset
        {
            public string Name;
            public string Description;
            public string Data;
            public bool BuiltIn;
            public string Path;

            // Where this preset came from, in one phrase: "built in" or the
            // file it was read from. Two presets can share a name - a file the
            // user saved is very likely to have the same name as the built-in it
            // was derived from - and without this the list and the debug report
            // could not tell them apart. A run was debugged against the wrong
            // one because of exactly that.
            public string Source()
            {
                if (this.BuiltIn)
                    return "built in";
                if (this.Path != null && this.Path.Length > 0)
                    return "file: " + this.Path;
                return "file";
            }

            public string SourceShort()
            {
                if (this.BuiltIn)
                    return "built in";
                if (this.Path != null && this.Path.Length > 0)
                    return "file: " + System.IO.Path.GetFileName(this.Path);
                return "file";
            }

            public override string ToString()
            {
                return this.Name + "   [" + this.SourceShort() + "]";
            }
        }

        public static string presetFolder()
        {
            return Path.Combine(Application.StartupPath, "presets");
        }

        // ---------------------------------------------------------------------
        //  The built in preset is a real run, not something typed in from a
        //  conversation: the owner's own "Save current settings as preset" file
        //  of 2026-10-24, with the numbers that file was missing because the
        //  saver dropped every option whose name starts with "stat" (the five
        //  range limits, read back out of that run's debug report) and the
        //  options that were added after the file was saved. Nothing of his
        //  machine is in it: the ModSupport.* lines (mod folder, mod switches)
        //  are stripped here and put back by the two mod presets.
        // ---------------------------------------------------------------------
        // ------------------------------------------------------------------
        //  The exact settings of the 1.14.1 run of 2026-10-28 (seed 613943720),
        //  1119 values: every one of the 1051 fields the debug report dumped, in
        //  dump order, plus the passes that live outside Settings (the extras,
        //  the range limits, the graft chances, the mod switches). Nothing is
        //  left to the defaults any more - that was why the roller ranges and
        //  the interpolation styles used to come back as something else.
        // ------------------------------------------------------------------
        private static readonly string builtInRunData =
              "randStatBoosts=True\nrandMonStatBoosts=False\nrandStaStatBoosts=True\n"
            + "randBegStatBoosts=True\nrandIntStatBoosts=True\nrandAdvStatBoosts=True\n"
            + "randMasStatBoosts=True\nrandUniStatBoosts=True\nrandMovStatBoosts=True\n"
            + "monStatBoostsPosCountMin=0\nmonStatBoostsPosCountMax=0\nstaStatBoostsPosCountMin=0\n"
            + "staStatBoostsPosCountMax=0\nbegStatBoostsPosCountMin=1\nbegStatBoostsPosCountMax=1\n"
            + "intStatBoostsPosCountMin=2\nintStatBoostsPosCountMax=4\nadvStatBoostsPosCountMin=2\n"
            + "advStatBoostsPosCountMax=5\nmasStatBoostsPosCountMin=3\nmasStatBoostsPosCountMax=7\n"
            + "uniStatBoostsPosCountMin=3\nuniStatBoostsPosCountMax=9\nmonStatBoostsNegCountMin=0\n"
            + "monStatBoostsNegCountMax=0\nstaStatBoostsNegCountMin=0\nstaStatBoostsNegCountMax=0\n"
            + "begStatBoostsNegCountMin=0\nbegStatBoostsNegCountMax=0\nintStatBoostsNegCountMin=0\n"
            + "intStatBoostsNegCountMax=2\nadvStatBoostsNegCountMin=1\nadvStatBoostsNegCountMax=2\n"
            + "masStatBoostsNegCountMin=0\nmasStatBoostsNegCountMax=1\nuniStatBoostsNegCountMin=0\n"
            + "uniStatBoostsNegCountMax=1\nmonStatBoostsPosMin=0\nmonStatBoostsPosMax=0\n"
            + "staStatBoostsPosMin=0\nstaStatBoostsPosMax=0\nbegStatBoostsPosMin=1\n"
            + "begStatBoostsPosMax=1\nintStatBoostsPosMin=1\nintStatBoostsPosMax=4\n"
            + "advStatBoostsPosMin=1\nadvStatBoostsPosMax=10\nmasStatBoostsPosMin=1\n"
            + "masStatBoostsPosMax=6\nuniStatBoostsPosMin=1\nuniStatBoostsPosMax=8\n"
            + "monStatBoostsNegMin=0\nmonStatBoostsNegMax=0\nstaStatBoostsNegMin=0\n"
            + "staStatBoostsNegMax=0\nbegStatBoostsNegMin=0\nbegStatBoostsNegMax=0\n"
            + "intStatBoostsNegMin=-2\nintStatBoostsNegMax=-1\nadvStatBoostsNegMin=-4\n"
            + "advStatBoostsNegMax=-2\nmasStatBoostsNegMin=-2\nmasStatBoostsNegMax=-1\n"
            + "uniStatBoostsNegMin=-4\nuniStatBoostsNegMax=-1\nmonMovStatBoostsMin=0\n"
            + "monMovStatBoostsMax=2\nstaMovStatBoostsMin=0\nstaMovStatBoostsMax=0\n"
            + "begMovStatBoostsMin=0\nbegMovStatBoostsMax=0\nintMovStatBoostsMin=0\n"
            + "intMovStatBoostsMax=1\nadvMovStatBoostsMin=0\nadvMovStatBoostsMax=2\n"
            + "masMovStatBoostsMin=1\nmasMovStatBoostsMax=2\nuniMovStatBoostsMin=1\n"
            + "uniMovStatBoostsMax=2\nrandMountBoosts=True\nrandMonMountBoosts=False\n"
            + "randStaMountBoosts=False\nrandBegMountBoosts=False\nrandIntMountBoosts=False\n"
            + "randAdvMountBoosts=False\nrandMasMountBoosts=True\nrandUniMountBoosts=False\n"
            + "randMovMountBoosts=False\nmonMountBoostsPosCountMin=0\nmonMountBoostsPosCountMax=0\n"
            + "staMountBoostsPosCountMin=0\nstaMountBoostsPosCountMax=0\nbegMountBoostsPosCountMin=0\n"
            + "begMountBoostsPosCountMax=0\nintMountBoostsPosCountMin=0\nintMountBoostsPosCountMax=1\n"
            + "advMountBoostsPosCountMin=0\nadvMountBoostsPosCountMax=2\nmasMountBoostsPosCountMin=0\n"
            + "masMountBoostsPosCountMax=2\nuniMountBoostsPosCountMin=1\nuniMountBoostsPosCountMax=2\n"
            + "monMountBoostsNegCountMin=0\nmonMountBoostsNegCountMax=0\nstaMountBoostsNegCountMin=0\n"
            + "staMountBoostsNegCountMax=0\nbegMountBoostsNegCountMin=0\nbegMountBoostsNegCountMax=0\n"
            + "intMountBoostsNegCountMin=0\nintMountBoostsNegCountMax=1\nadvMountBoostsNegCountMin=0\n"
            + "advMountBoostsNegCountMax=1\nmasMountBoostsNegCountMin=0\nmasMountBoostsNegCountMax=0\n"
            + "uniMountBoostsNegCountMin=0\nuniMountBoostsNegCountMax=1\nmonMountBoostsPosMin=0\n"
            + "monMountBoostsPosMax=0\nstaMountBoostsPosMin=0\nstaMountBoostsPosMax=0\n"
            + "begMountBoostsPosMin=0\nbegMountBoostsPosMax=0\nintMountBoostsPosMin=2\n"
            + "intMountBoostsPosMax=3\nadvMountBoostsPosMin=1\nadvMountBoostsPosMax=2\n"
            + "masMountBoostsPosMin=1\nmasMountBoostsPosMax=3\nuniMountBoostsPosMin=1\n"
            + "uniMountBoostsPosMax=3\nmonMountBoostsNegMin=0\nmonMountBoostsNegMax=0\n"
            + "staMountBoostsNegMin=0\nstaMountBoostsNegMax=0\nbegMountBoostsNegMin=0\n"
            + "begMountBoostsNegMax=0\nintMountBoostsNegMin=-2\nintMountBoostsNegMax=-2\n"
            + "advMountBoostsNegMin=-2\nadvMountBoostsNegMax=-2\nmasMountBoostsNegMin=0\n"
            + "masMountBoostsNegMax=0\nuniMountBoostsNegMin=-2\nuniMountBoostsNegMax=-2\n"
            + "monMovMountBoostsMin=0\nmonMovMountBoostsMax=0\nstaMovMountBoostsMin=0\n"
            + "staMovMountBoostsMax=0\nbegMovMountBoostsMin=0\nbegMovMountBoostsMax=0\n"
            + "intMovMountBoostsMin=2\nintMovMountBoostsMax=3\nadvMovMountBoostsMin=2\n"
            + "advMovMountBoostsMax=2\nmasMovMountBoostsMin=1\nmasMovMountBoostsMax=3\n"
            + "uniMovMountBoostsMin=2\nuniMovMountBoostsMax=3\nrandPlayerGrowthBoosts=True\n"
            + "randMonPlayerGrowthBoosts=False\nrandStaPlayerGrowthBoosts=True\nrandBegPlayerGrowthBoosts=True\n"
            + "randIntPlayerGrowthBoosts=True\nrandAdvPlayerGrowthBoosts=True\nrandMasPlayerGrowthBoosts=True\n"
            + "randUniPlayerGrowthBoosts=True\nrandMovPlayerGrowthBoosts=False\nmonPlayerGrowthBoostsPosCountMin=0\n"
            + "monPlayerGrowthBoostsPosCountMax=0\nstaPlayerGrowthBoostsPosCountMin=0\nstaPlayerGrowthBoostsPosCountMax=1\n"
            + "begPlayerGrowthBoostsPosCountMin=3\nbegPlayerGrowthBoostsPosCountMax=3\nintPlayerGrowthBoostsPosCountMin=3\n"
            + "intPlayerGrowthBoostsPosCountMax=5\nadvPlayerGrowthBoostsPosCountMin=3\nadvPlayerGrowthBoostsPosCountMax=7\n"
            + "masPlayerGrowthBoostsPosCountMin=2\nmasPlayerGrowthBoostsPosCountMax=6\nuniPlayerGrowthBoostsPosCountMin=2\n"
            + "uniPlayerGrowthBoostsPosCountMax=6\nmonPlayerGrowthBoostsNegCountMin=0\nmonPlayerGrowthBoostsNegCountMax=0\n"
            + "staPlayerGrowthBoostsNegCountMin=0\nstaPlayerGrowthBoostsNegCountMax=0\nbegPlayerGrowthBoostsNegCountMin=0\n"
            + "begPlayerGrowthBoostsNegCountMax=1\nintPlayerGrowthBoostsNegCountMin=0\nintPlayerGrowthBoostsNegCountMax=2\n"
            + "advPlayerGrowthBoostsNegCountMin=0\nadvPlayerGrowthBoostsNegCountMax=2\nmasPlayerGrowthBoostsNegCountMin=0\n"
            + "masPlayerGrowthBoostsNegCountMax=2\nuniPlayerGrowthBoostsNegCountMin=0\nuniPlayerGrowthBoostsNegCountMax=3\n"
            + "monPlayerGrowthBoostsPosMin=0\nmonPlayerGrowthBoostsPosMax=0\nstaPlayerGrowthBoostsPosMin=5\n"
            + "staPlayerGrowthBoostsPosMax=5\nbegPlayerGrowthBoostsPosMin=5\nbegPlayerGrowthBoostsPosMax=10\n"
            + "intPlayerGrowthBoostsPosMin=5\nintPlayerGrowthBoostsPosMax=30\nadvPlayerGrowthBoostsPosMin=5\n"
            + "advPlayerGrowthBoostsPosMax=40\nmasPlayerGrowthBoostsPosMin=5\nmasPlayerGrowthBoostsPosMax=40\n"
            + "uniPlayerGrowthBoostsPosMin=5\nuniPlayerGrowthBoostsPosMax=40\nmonPlayerGrowthBoostsNegMin=0\n"
            + "monPlayerGrowthBoostsNegMax=0\nstaPlayerGrowthBoostsNegMin=0\nstaPlayerGrowthBoostsNegMax=0\n"
            + "begPlayerGrowthBoostsNegMin=-5\nbegPlayerGrowthBoostsNegMax=-5\nintPlayerGrowthBoostsNegMin=-10\n"
            + "intPlayerGrowthBoostsNegMax=-5\nadvPlayerGrowthBoostsNegMin=-10\nadvPlayerGrowthBoostsNegMax=-5\n"
            + "masPlayerGrowthBoostsNegMin=-5\nmasPlayerGrowthBoostsNegMax=-5\nuniPlayerGrowthBoostsNegMin=-5\n"
            + "uniPlayerGrowthBoostsNegMax=-5\nmonMovPlayerGrowthBoostsMin=0\nmonMovPlayerGrowthBoostsMax=0\n"
            + "staMovPlayerGrowthBoostsMin=1\nstaMovPlayerGrowthBoostsMax=5\nbegMovPlayerGrowthBoostsMin=1\n"
            + "begMovPlayerGrowthBoostsMax=5\nintMovPlayerGrowthBoostsMin=1\nintMovPlayerGrowthBoostsMax=5\n"
            + "advMovPlayerGrowthBoostsMin=1\nadvMovPlayerGrowthBoostsMax=5\nmasMovPlayerGrowthBoostsMin=1\n"
            + "masMovPlayerGrowthBoostsMax=5\nuniMovPlayerGrowthBoostsMin=1\nuniMovPlayerGrowthBoostsMax=5\n"
            + "monMovPlayerGrowthBoostsP=0\nstaMovPlayerGrowthBoostsP=10\nbegMovPlayerGrowthBoostsP=10\n"
            + "intMovPlayerGrowthBoostsP=10\nadvMovPlayerGrowthBoostsP=10\nmasMovPlayerGrowthBoostsP=10\n"
            + "uniMovPlayerGrowthBoostsP=10\nrandEnemyGrowthBoosts=True\nrandMonEnemyGrowthBoosts=True\n"
            + "randStaEnemyGrowthBoosts=True\nrandBegEnemyGrowthBoosts=True\nrandIntEnemyGrowthBoosts=True\n"
            + "randAdvEnemyGrowthBoosts=True\nrandMasEnemyGrowthBoosts=True\nrandUniEnemyGrowthBoosts=True\n"
            + "randMovEnemyGrowthBoosts=False\nmonEnemyGrowthBoostsPosCountMin=1\nmonEnemyGrowthBoostsPosCountMax=1\n"
            + "staEnemyGrowthBoostsPosCountMin=0\nstaEnemyGrowthBoostsPosCountMax=0\nbegEnemyGrowthBoostsPosCountMin=3\n"
            + "begEnemyGrowthBoostsPosCountMax=3\nintEnemyGrowthBoostsPosCountMin=3\nintEnemyGrowthBoostsPosCountMax=6\n"
            + "advEnemyGrowthBoostsPosCountMin=3\nadvEnemyGrowthBoostsPosCountMax=6\nmasEnemyGrowthBoostsPosCountMin=4\n"
            + "masEnemyGrowthBoostsPosCountMax=6\nuniEnemyGrowthBoostsPosCountMin=3\nuniEnemyGrowthBoostsPosCountMax=5\n"
            + "monEnemyGrowthBoostsNegCountMin=1\nmonEnemyGrowthBoostsNegCountMax=1\nstaEnemyGrowthBoostsNegCountMin=0\n"
            + "staEnemyGrowthBoostsNegCountMax=0\nbegEnemyGrowthBoostsNegCountMin=1\nbegEnemyGrowthBoostsNegCountMax=1\n"
            + "intEnemyGrowthBoostsNegCountMin=0\nintEnemyGrowthBoostsNegCountMax=2\nadvEnemyGrowthBoostsNegCountMin=0\n"
            + "advEnemyGrowthBoostsNegCountMax=2\nmasEnemyGrowthBoostsNegCountMin=1\nmasEnemyGrowthBoostsNegCountMax=1\n"
            + "uniEnemyGrowthBoostsNegCountMin=1\nuniEnemyGrowthBoostsNegCountMax=2\nmonEnemyGrowthBoostsPosMin=5\n"
            + "monEnemyGrowthBoostsPosMax=5\nstaEnemyGrowthBoostsPosMin=0\nstaEnemyGrowthBoostsPosMax=0\n"
            + "begEnemyGrowthBoostsPosMin=5\nbegEnemyGrowthBoostsPosMax=25\nintEnemyGrowthBoostsPosMin=5\n"
            + "intEnemyGrowthBoostsPosMax=40\nadvEnemyGrowthBoostsPosMin=5\nadvEnemyGrowthBoostsPosMax=60\n"
            + "masEnemyGrowthBoostsPosMin=5\nmasEnemyGrowthBoostsPosMax=60\nuniEnemyGrowthBoostsPosMin=15\n"
            + "uniEnemyGrowthBoostsPosMax=60\nmonEnemyGrowthBoostsNegMin=-10\nmonEnemyGrowthBoostsNegMax=-10\n"
            + "staEnemyGrowthBoostsNegMin=0\nstaEnemyGrowthBoostsNegMax=0\nbegEnemyGrowthBoostsNegMin=-5\n"
            + "begEnemyGrowthBoostsNegMax=0\nintEnemyGrowthBoostsNegMin=-30\nintEnemyGrowthBoostsNegMax=-5\n"
            + "advEnemyGrowthBoostsNegMin=-30\nadvEnemyGrowthBoostsNegMax=-5\nmasEnemyGrowthBoostsNegMin=-10\n"
            + "masEnemyGrowthBoostsNegMax=-5\nuniEnemyGrowthBoostsNegMin=-20\nuniEnemyGrowthBoostsNegMax=-5\n"
            + "monMovEnemyGrowthBoostsMin=0\nmonMovEnemyGrowthBoostsMax=0\nstaMovEnemyGrowthBoostsMin=1\n"
            + "staMovEnemyGrowthBoostsMax=5\nbegMovEnemyGrowthBoostsMin=1\nbegMovEnemyGrowthBoostsMax=5\n"
            + "intMovEnemyGrowthBoostsMin=1\nintMovEnemyGrowthBoostsMax=5\nadvMovEnemyGrowthBoostsMin=1\n"
            + "advMovEnemyGrowthBoostsMax=5\nmasMovEnemyGrowthBoostsMin=1\nmasMovEnemyGrowthBoostsMax=5\n"
            + "uniMovEnemyGrowthBoostsMin=1\nuniMovEnemyGrowthBoostsMax=5\nmonMovEnemyGrowthBoostsP=0\n"
            + "staMovEnemyGrowthBoostsP=10\nbegMovEnemyGrowthBoostsP=10\nintMovEnemyGrowthBoostsP=10\n"
            + "advMovEnemyGrowthBoostsP=10\nmasMovEnemyGrowthBoostsP=10\nuniMovEnemyGrowthBoostsP=10\n"
            + "randClassBases=True\nrandMonClassBases=False\nrandStaClassBases=True\n"
            + "randBegClassBases=True\nrandIntClassBases=True\nrandAdvClassBases=True\n"
            + "randMasClassBases=True\nrandUniClassBases=True\nhpMonClassBasesMin=0\n"
            + "hpMonClassBasesMax=0\nhpStaClassBasesMin=1\nhpStaClassBasesMax=1\n"
            + "hpBegClassBasesMin=20\nhpBegClassBasesMax=20\nhpIntClassBasesMin=25\n"
            + "hpIntClassBasesMax=30\nhpAdvClassBasesMin=20\nhpAdvClassBasesMax=35\n"
            + "hpMasClassBasesMin=32\nhpMasClassBasesMax=34\nhpUniClassBasesMin=28\n"
            + "hpUniClassBasesMax=38\nstrMonClassBasesMin=0\nstrMonClassBasesMax=0\n"
            + "strStaClassBasesMin=0\nstrStaClassBasesMax=0\nstrBegClassBasesMin=4\n"
            + "strBegClassBasesMax=8\nstrIntClassBasesMin=4\nstrIntClassBasesMax=10\n"
            + "strAdvClassBasesMin=5\nstrAdvClassBasesMax=20\nstrMasClassBasesMin=8\n"
            + "strMasClassBasesMax=20\nstrUniClassBasesMin=8\nstrUniClassBasesMax=18\n"
            + "magMonClassBasesMin=0\nmagMonClassBasesMax=0\nmagStaClassBasesMin=0\n"
            + "magStaClassBasesMax=0\nmagBegClassBasesMin=3\nmagBegClassBasesMax=8\n"
            + "magIntClassBasesMin=6\nmagIntClassBasesMax=10\nmagAdvClassBasesMin=6\n"
            + "magAdvClassBasesMax=17\nmagMasClassBasesMin=10\nmagMasClassBasesMax=17\n"
            + "magUniClassBasesMin=8\nmagUniClassBasesMax=17\ndexMonClassBasesMin=0\n"
            + "dexMonClassBasesMax=0\ndexStaClassBasesMin=0\ndexStaClassBasesMax=1\n"
            + "dexBegClassBasesMin=6\ndexBegClassBasesMax=6\ndexIntClassBasesMin=7\n"
            + "dexIntClassBasesMax=11\ndexAdvClassBasesMin=7\ndexAdvClassBasesMax=12\n"
            + "dexMasClassBasesMin=14\ndexMasClassBasesMax=14\ndexUniClassBasesMin=11\n"
            + "dexUniClassBasesMax=14\nspeMonClassBasesMin=0\nspeMonClassBasesMax=0\n"
            + "speStaClassBasesMin=0\nspeStaClassBasesMax=0\nspeBegClassBasesMin=6\n"
            + "speBegClassBasesMax=6\nspeIntClassBasesMin=4\nspeIntClassBasesMax=11\n"
            + "speAdvClassBasesMin=8\nspeAdvClassBasesMax=14\nspeMasClassBasesMin=10\n"
            + "speMasClassBasesMax=20\nspeUniClassBasesMin=6\nspeUniClassBasesMax=20\n"
            + "lucMonClassBasesMin=0\nlucMonClassBasesMax=0\nlucStaClassBasesMin=0\n"
            + "lucStaClassBasesMax=0\nlucBegClassBasesMin=4\nlucBegClassBasesMax=4\n"
            + "lucIntClassBasesMin=8\nlucIntClassBasesMax=8\nlucAdvClassBasesMin=8\n"
            + "lucAdvClassBasesMax=12\nlucMasClassBasesMin=12\nlucMasClassBasesMax=12\n"
            + "lucUniClassBasesMin=19\nlucUniClassBasesMax=19\ndefMonClassBasesMin=0\n"
            + "defMonClassBasesMax=0\ndefStaClassBasesMin=0\ndefStaClassBasesMax=0\n"
            + "defBegClassBasesMin=3\ndefBegClassBasesMax=5\ndefIntClassBasesMin=3\n"
            + "defIntClassBasesMax=12\ndefAdvClassBasesMin=5\ndefAdvClassBasesMax=17\n"
            + "defMasClassBasesMin=13\ndefMasClassBasesMax=17\ndefUniClassBasesMin=13\n"
            + "defUniClassBasesMax=17\nresMonClassBasesMin=0\nresMonClassBasesMax=0\n"
            + "resStaClassBasesMin=0\nresStaClassBasesMax=0\nresBegClassBasesMin=2\n"
            + "resBegClassBasesMax=4\nresIntClassBasesMin=1\nresIntClassBasesMax=5\n"
            + "resAdvClassBasesMin=4\nresAdvClassBasesMax=15\nresMasClassBasesMin=10\n"
            + "resMasClassBasesMax=16\nresUniClassBasesMin=8\nresUniClassBasesMax=16\n"
            + "chaMonClassBasesMin=0\nchaMonClassBasesMax=0\nchaStaClassBasesMin=0\n"
            + "chaStaClassBasesMax=0\nchaBegClassBasesMin=4\nchaBegClassBasesMax=5\n"
            + "chaIntClassBasesMin=5\nchaIntClassBasesMax=9\nchaAdvClassBasesMin=6\n"
            + "chaAdvClassBasesMax=14\nchaMasClassBasesMin=12\nchaMasClassBasesMax=16\n"
            + "chaUniClassBasesMin=9\nchaUniClassBasesMax=16\nremoveWeaponRestrictions=True\n"
            + "randWeaponTypes=True\nmon2ndWeaponTypeP=0\nsta2ndWeaponTypeP=0\n"
            + "beg2ndWeaponTypeP=5\nint2ndWeaponTypeP=15\nadv2ndWeaponTypeP=35\n"
            + "mas2ndWeaponTypeP=75\nuni2ndWeaponTypeP=50\nrandTyping=True\n"
            + "randMovTypes=False\nbaseMovTypes=False\nrandClassProf=True\n"
            + "baseClassProf=True\nrandCert=True\nrandMonCert=True\n"
            + "randStaCert=True\nrandBegCert=True\nrandIntCert=True\n"
            + "randAdvCert=True\nrandMasCert=True\nrandUniCert=True\n"
            + "mainMonCertMin=0\nmainMonCertMax=0\nmainStaCertMin=0\n"
            + "mainStaCertMax=0\nmainBegCertMin=2\nmainBegCertMax=2\n"
            + "mainIntCertMin=4\nmainIntCertMax=4\nmainAdvCertMin=6\n"
            + "mainAdvCertMax=8\nmainMasCertMin=7\nmainMasCertMax=8\n"
            + "mainUniCertMin=0\nmainUniCertMax=0\ntypeMonCertMin=0\n"
            + "typeMonCertMax=0\ntypeStaCertMin=0\ntypeStaCertMax=0\n"
            + "typeBegCertMin=0\ntypeBegCertMax=0\ntypeIntCertMin=1\n"
            + "typeIntCertMax=2\ntypeAdvCertMin=4\ntypeAdvCertMax=6\n"
            + "typeMasCertMin=7\ntypeMasCertMax=9\ntypeUniCertMin=0\n"
            + "typeUniCertMax=0\notherMonCertMin=0\notherMonCertMax=0\n"
            + "otherStaCertMin=0\notherStaCertMax=0\notherBegCertMin=0\n"
            + "otherBegCertMax=2\notherIntCertMin=0\notherIntCertMax=2\n"
            + "otherAdvCertMin=4\notherAdvCertMax=4\notherMasCertMin=4\n"
            + "otherMasCertMax=6\notherUniCertMin=0\notherUniCertMax=0\n"
            + "baseCert=True\nrandCertItem=False\nallowNonSealCertItems=False\n"
            + "randClassAb=True\nbaseClassAb=True\nrandClassDefAb=True\n"
            + "randMonClassDefAb=True\nrandStaClassDefAb=True\nrandBegClassDefAb=True\n"
            + "randIntClassDefAb=True\nrandAdvClassDefAb=True\nrandMasClassDefAb=True\n"
            + "randUniClassDefAb=True\nmonClassDefAbCountMin=0\nmonClassDefAbCountMax=0\n"
            + "staClassDefAbCountMin=0\nstaClassDefAbCountMax=0\nintClassDefAbCountMin=2\n"
            + "intClassDefAbCountMax=2\nbegClassDefAbCountMin=0\nbegClassDefAbCountMax=1\n"
            + "advClassDefAbCountMin=3\nadvClassDefAbCountMax=3\nmasClassDefAbCountMin=4\n"
            + "masClassDefAbCountMax=4\nuniClassDefAbCountMin=5\nuniClassDefAbCountMax=5\n"
            + "randClassClassAb=True\nrandMonClassClassAb=True\nrandStaClassClassAb=True\n"
            + "randBegClassClassAb=True\nrandIntClassClassAb=True\nrandAdvClassClassAb=True\n"
            + "randMasClassClassAb=True\nrandUniClassClassAb=True\nmonClassClassAbCountMin=3\n"
            + "monClassClassAbCountMax=3\nstaClassClassAbCountMin=0\nstaClassClassAbCountMax=0\n"
            + "intClassClassAbCountMin=0\nintClassClassAbCountMax=2\nbegClassClassAbCountMin=0\n"
            + "begClassClassAbCountMax=0\nadvClassClassAbCountMin=2\nadvClassClassAbCountMax=3\n"
            + "masClassClassAbCountMin=3\nmasClassClassAbCountMax=3\nuniClassClassAbCountMin=2\n"
            + "uniClassClassAbCountMax=3\nrandClassMasteryAb=True\nrandClassMasteryCa=True\n"
            + "baseClassMasteryCa=True\nrandClassMasteryExpReq=True\nrandMonClassMasteryExpReq=False\n"
            + "randStaClassMasteryExpReq=True\nrandBegClassMasteryExpReq=True\nrandIntClassMasteryExpReq=True\n"
            + "randAdvClassMasteryExpReq=True\nrandMasClassMasteryExpReq=True\nrandUniClassMasteryExpReq=True\n"
            + "monClassMasteryExpReqMin=0\nmonClassMasteryExpReqMax=0\nstaClassMasteryExpReqMin=10\n"
            + "staClassMasteryExpReqMax=30\nbegClassMasteryExpReqMin=30\nbegClassMasteryExpReqMax=80\n"
            + "intClassMasteryExpReqMin=50\nintClassMasteryExpReqMax=140\nadvClassMasteryExpReqMin=150\n"
            + "advClassMasteryExpReqMax=210\nmasClassMasteryExpReqMin=160\nmasClassMasteryExpReqMax=255\n"
            + "uniClassMasteryExpReqMin=100\nuniClassMasteryExpReqMax=200\nrandAdjutant=True\n"
            + "specialToAdv=True\nrandExpYield=False\nrandMonExpYield=True\n"
            + "randStaExpYield=True\nrandBegExpYield=True\nrandIntExpYield=True\n"
            + "randAdvExpYield=True\nrandMasExpYield=True\nrandUniExpYield=True\n"
            + "monExpYieldMin=25\nmonExpYieldMax=75\nstaExpYieldMin=15\n"
            + "staExpYieldMax=45\nbegExpYieldMin=15\nbegExpYieldMax=45\n"
            + "intExpYieldMin=18\nintExpYieldMax=54\nadvExpYieldMin=20\n"
            + "advExpYieldMax=60\nmasExpYieldMin=20\nmasExpYieldMax=60\n"
            + "uniExpYieldMin=25\nuniExpYieldMax=75\nunitsToScale=True\n"
            + "randMounts=True\nsetArmorAni=True\nprovideClassAni=True\n"
            + "randClassAni=False\nrandTierLv=False\nrandBegTierLv=True\n"
            + "randIntTierLv=True\nrandAdvTierLv=True\nrandMasTierLv=True\n"
            + "begTierLvMin=4\nbegTierLvMax=6\nintTierLvMin=8\n"
            + "intTierLvMax=12\nadvTierLvMin=16\nadvTierLvMax=24\n"
            + "masTierLvMin=24\nmasTierLvMax=36\nremoveGenderRestrictions=True\n"
            + "randBarrierBreakItem=False\nallowNonMineralBarrierBreakItem=False\nrandBarrierAb=False\n"
            + "randLatentAb=True\nrandWeapons=True\nrandSpells=True\n"
            + "randTurrets=False\nrandGambits=True\nrandMonsterAoe=True\n"
            + "randAtkStatusEffects=True\natkStatusEffectsP=10\nrandAtkEffectiveness=True\n"
            + "atkEffectivenessP=10\natkEffectivenessPreserveBows=False\nrandAtkCrit=False\n"
            + "atkCritP=30\natkCritMax=50\nrandAtkDoubleAttack=False\n"
            + "atkDoubleAttackP=5\natkDoubleAttackPreserveGauntlets=True\nrandAtkRange=True\n"
            + "randAtkRangeP=10\natkRangeBinomial=True\natkRangeLinear=False\n"
            + "atkRangeLinearDev=2\natkRangeProportional=False\natkRangeProportionalDev=1\n"
            + "atkRangeExponential=False\natkRangeExponentialDev=2\nrandAtkDurability=True\n"
            + "randAtkDurabilityP=50\natkDurabilityBinomial=False\natkDurabilityLinear=False\n"
            + "atkDurabilityLinearDev=10\natkDurabilityProportional=True\natkDurabilityProportionalDev=0.5\n"
            + "atkDurabilityExponential=False\natkDurabilityExponentialDev=2\nrandAtkMight=True\n"
            + "randAtkMightP=50\natkMightBinomial=False\natkMightLinear=False\n"
            + "atkMightLinearDev=5\natkMightProportional=True\natkMightProportionalDev=0.5\n"
            + "atkMightExponential=False\natkMightExponentialDev=2\nrandAtkHit=True\n"
            + "randAtkHitP=50\natkHitBinomial=False\natkHitLinear=False\n"
            + "atkHitLinearDev=25\natkHitProportional=True\natkHitProportionalDev=0.25\n"
            + "atkHitExponential=False\natkHitExponentialDev=1.5\nrandAtkWeight=True\n"
            + "randAtkWeightP=50\natkWeightBinomial=False\natkWeightLinear=True\n"
            + "atkWeightLinearDev=6\natkWeightProportional=False\natkWeightProportionalDev=0.5\n"
            + "atkWeightExponential=False\natkWeightExponentialDev=2\nrandAtkPhysicalMagicSwitch=True\n"
            + "randAtkPhysicalMagicSwitchP=10\nrandAtkIgnoreDefRes=False\nrandAtkIgnoreDefResP=0.5\n"
            + "randAtkLeaveAt1=False\nrandAtkLeaveAt1P=0.5\nrandCa=True\n"
            + "randCaAvoid=True\nrandCaAvoidP=20\nrandCaAvoidMax=25\n"
            + "randCaCrit=False\nrandCaCritP=35\nrandCaCritMax=40\n"
            + "randCaEffects=True\nrandCaEffectsP=75\nrandCaEffectiveness=True\n"
            + "randCaEffectivenessP=20\nrandCaRange=True\nrandCaRangeP=10\n"
            + "caRangeBinomial=True\ncaRangeLinear=False\ncaRangeLinearDev=2\n"
            + "caRangeProportional=False\ncaRangeProportionalDev=1\ncaRangeExponential=False\n"
            + "caRangeExponentialDev=2\nrandCaDurabilityCost=True\nrandCaDurabilityCostP=50\n"
            + "caDurabilityCostBinomial=False\ncaDurabilityCostLinear=False\ncaDurabilityCostLinearDev=2\n"
            + "caDurabilityCostProportional=True\ncaDurabilityCostProportionalDev=0.5\ncaDurabilityCostExponential=False\n"
            + "caDurabilityCostExponentialDev=2\nrandCaMight=True\nrandCaMightP=50\n"
            + "caMightBinomial=False\ncaMightLinear=False\ncaMightLinearDev=5\n"
            + "caMightProportional=True\ncaMightProportionalDev=0.5\ncaMightExponential=False\n"
            + "caMightExponentialDev=2\nrandCaHit=True\ncaHitMin=-35\n"
            + "caHitMax=35\nrandCaHitP=25\nrandBattalions=True\n"
            + "randBattalionEndurance=True\nrandBattalionEnduranceP=80\nbattalionEnduranceBinomial=True\n"
            + "battalionEnduranceLinear=False\nbattalionEnduranceLinearDev=30\nbattalionEnduranceProportional=False\n"
            + "battalionEnduranceProportionalDev=0.5\nbattalionEnduranceExponential=False\nbattalionEnduranceExponentialDev=2\n"
            + "randBattalionBoostBases=True\nrandBattalionBoostGrowths=True\nrandBattalionStatBoosts=False\n"
            + "battalionStatBoostsIncludeAll=False\nrandBattalionGambit=True\nshuffleAssets=False\n"
            + "shuffleAssetsGender=False\nshuffleAssetsAge=False\nrandModelData=True\n"
            + "randAge=False\nageMin=9\nageMax=73\n"
            + "randBirthday=True\nrandAniSet=False\naniSetIncludeCane=False\n"
            + "randHeight=True\nheightMin=135\nheightMax=204\n"
            + "modelSize1Min=0.85\nmodelSize1Max=1.15\nmodelSize2Min=0.85\n"
            + "modelSize2Max=1.16\nmodelPreTsChestSizeMin=0.07\nmodelPreTsChestSizeMax=1.75\n"
            + "modelPostTsChestSizeMin=0.08\nmodelPostTsChestSizeMax=2.45\nrandCrests=True\n"
            + "crest1P=60\ncrest2P=10\nrandPersonalAb=True\n"
            + "basePersonalAb=True\nnewPersonalAbPostTsP=5\nrandSupportBonuses=True\n"
            + "randSpecialSupportBonuses=True\nsupportBonusesIncludeMore=False\nrandRecruitmentRequirements=True\n"
            + "recruitmentRequirementStatsMin=8\nrecruitmentRequirementStatsMax=22\nrecruitmentRequirementRanksMin=3\n"
            + "recruitmentRequirementRanksMax=6\npreserveCharCaps=False\nrandCharCaps=False\n"
            + "removeCharCaps=False\nhpCharCapsMin=80\nhpCharCapsMax=200\n"
            + "strCharCapsMin=30\nstrCharCapsMax=90\nmagCharCapsMin=30\n"
            + "magCharCapsMax=90\ndexCharCapsMin=55\ndexCharCapsMax=90\n"
            + "speCharCapsMin=35\nspeCharCapsMax=90\nlucCharCapsMin=0\n"
            + "lucCharCapsMax=255\ndefCharCapsMin=35\ndefCharCapsMax=70\n"
            + "resCharCapsMin=35\nresCharCapsMax=70\nchaCharCapsMin=35\n"
            + "chaCharCapsMax=100\nrandCharBases=True\nensureGoodBases=True\n"
            + "hpCharBasesMin=20\nhpCharBasesMax=35\nstrCharBasesMin=4\n"
            + "strCharBasesMax=13\nmagCharBasesMin=4\nmagCharBasesMax=13\n"
            + "dexCharBasesMin=6\ndexCharBasesMax=7\nspeCharBasesMin=5\n"
            + "speCharBasesMax=10\nlucCharBasesMin=0\nlucCharBasesMax=11\n"
            + "defCharBasesMin=3\ndefCharBasesMax=8\nresCharBasesMin=1\n"
            + "resCharBasesMax=10\nchaCharBasesMin=1\nchaCharBasesMax=12\n"
            + "randMovCharBases=False\nrandMovCharBasesP=10\nmovCharBasesMin=3\n"
            + "movCharBasesMax=5\nrandCharGrowths=True\nensureGoodGrowths=True\n"
            + "hpCharGrowthsMin=50\nhpCharGrowthsMax=100\nstrCharGrowthsMin=20\n"
            + "strCharGrowthsMax=60\nmagCharGrowthsMin=20\nmagCharGrowthsMax=60\n"
            + "dexCharGrowthsMin=30\ndexCharGrowthsMax=60\nspeCharGrowthsMin=15\n"
            + "speCharGrowthsMax=70\nlucCharGrowthsMin=0\nlucCharGrowthsMax=100\n"
            + "defCharGrowthsMin=20\ndefCharGrowthsMax=50\nresCharGrowthsMin=20\n"
            + "resCharGrowthsMax=50\nchaCharGrowthsMin=10\nchaCharGrowthsMax=65\n"
            + "randMovCharGrowths=False\nrandMovCharGrowthsP=10\nmovCharGrowthsMin=1\n"
            + "movCharGrowthsMax=10\nrandGenericAb=True\nbaseGenericAb=True\n"
            + "preserveProwessAb=True\nrandGenericCa=True\nbaseGenericCa=True\n"
            + "randCharCa=True\nbaseCharCa=True\nlimitCharCa=True\n"
            + "charCaCountMin=2\ncharCaCountMax=3\nrandBuddingTalents=True\n"
            + "multipleBuddingTalents=True\nbaseBuddingTalents=True\nrandBuddingTalentLessonCount=True\n"
            + "buddingTalentLessonCountMin=10\nbuddingTalentLessonCountMax=14\nrandSpellLists=True\n"
            + "spellAtD=True\nspellFirstRank=4\nbaseSpellLists=True\n"
            + "regularSpellListReasonCountMin=3\nregularSpellListReasonCountMax=3\nregularSpellListFaithCountMin=2\n"
            + "regularSpellListFaithCountMax=5\nproficientSpellListReasonCountMin=5\nproficientSpellListReasonCountMax=5\n"
            + "proficientSpellListFaithCountMin=5\nproficientSpellListFaithCountMax=5\nunknownSpellListReasonCountMin=2\n"
            + "unknownSpellListReasonCountMax=5\nunknownSpellListFaithCountMin=3\nunknownSpellListFaithCountMax=5\n"
            + "randCharAb=True\nbaseCharAb=True\nlimitCharAb=True\n"
            + "charAbCountMin=1\ncharAbCountMax=3\npreserveGenericCharAb=True\n"
            + "randCharProfs=True\nstrengthCountMin=2\nstrengthCountMax=5\n"
            + "weaknessCountMin=1\nweaknessCountMax=4\nrandCharStartClasses=False\n"
            + "randCharPreCertClasses=True\ncharOptimalClasses=True\nrandStartInventories=True\n"
            + "baseStartInventories=True\nrandStartInventoryItem=True\nstartInventoryItemP=100\n"
            + "randFacultyTraining=False\nbaseFacultyTraining=True\nrandSeminars=False\n"
            + "baseSeminars=True\nrandGoals=False\nbaseGoals=True\n"
            + "randStartRanks=True\nbaseStartRanks=True\nrandCalendar=False\n"
            + "randRareMonsterSighting=False\nrandRareMonsterSightingP=20\nrandRestrictFreeday=False\n"
            + "randRestrictFreedayP=10\nrandCafeteriaEvent=False\nrandCafeteriaEventP=50\n"
            + "randChoirEvent=False\nrandChoirEventP=10\nrandGardenEvent=False\n"
            + "randGardenEventP=10\nrandFishingEvent=False\nrandFishingEventP=30\n"
            + "shuffleBgm=True\nchangeMsg=True\nchangeScr=False\n"
            + "changeGwscr=False\nchangeTuscr=False\nchangeBtlscr=False\n"
            + "changeData1Text=False\nbaseText=True\nqolText=True\n"
            + "randText=False\npreserveStringLength=False\nrandTextSource=False\n"
            + "randChoirProfs=False\nrandCookingProfs=False\nrandMeal=False\n"
            + "randMealPrefs=False\nrandFishingItems=False\nrandGardeningItems=False\n"
            + "randCultivationCosts=False\nrandGiftPrefs=False\nrandSaintStatueCosts=False\n"
            + "randSaintStatuePerks=False\nrandInfluencerCosts=False\nrandQuestRewards=False\n"
            + "randGroupTaskGold=False\nrandGroupTaskSkills=False\nrandGroupTaskItems=False\n"
            + "randGroupTaskProfs=False\nsameFunctionItems=True\nsameTypeItems=False\n"
            + "randTypeItems=False\nrandTypeItemP=10\nrandWeaponShops=False\n"
            + "randItemShops=False\nrandBattalionShops=False\nrandMerchants=False\n"
            + "randAnnaShop=True\nrandPaganAltar=False\nrandShopPrices=False\n"
            + "randShopStocks=False\nrandLectureExp=False\nrandProfLvExpReq=False\n"
            + "randFunds=False\nrandLecturePointCount=False\nrandBattlePointCount=False\n"
            + "randAdjutantCount=False\nrandActivityPointCount=False\nunlockMasterClasses=True\n"
            + "randRepairItems=False\nrandForgeCosts=False\nrandForgeItems=False\n"
            + "randForgeWeapons=True\npreserveForgeWeaponTypes=False\nrandForgeProfLvReq=False\n"
            + "randMapClass=True\nincludeMonsterMapClass=False\nmapGroupClass=True\n"
            + "mapIndividualClass=False\nrandMapInventory=True\nmapInventoryP=10\n"
            + "mapInventoryForceCom=True\nrandMapWeapons=True\nbaseMapWeapons=True\n"
            + "randMapEquipment=True\nmapEquipmentP=20\nrandMapItems=True\n"
            + "mapItemsP=20\nrandMapDrops=True\nmapDropsP=20\n"
            + "randMapAb=False\nmapAbP=10\nmapAbForceCom=False\n"
            + "baseMapAb=False\nrandMapBattalionId=False\nrandMapBattalion=False\n"
            + "mapBattalionP=10\nmapBattalionForceCom=False\nrandMapCa=False\n"
            + "mapCaP=10\nmapCaForceCom=False\nbaseMapCa=False\n"
            + "randMapSpellId=False\nrandMapSpell=False\nmapSpellP=10\n"
            + "mapSpellForceCom=False\nbaseMapSpell=False\nrandChestItem=True\n"
            + "randChestItemType=True\nrandChestItemTypeP=50\nmapExtraEnemies=True\n"
            + "mapExtraEnemiesMin=10\nmapExtraEnemiesMax=10\nmapMaxAllies=False\n"
            + "randAllyMapSpawn=False\nrandNpcMapSpawn=False\nrandEnemyMapSpawn=True\n"
            + "randComMapSpawn=False\nrandMapSpawnP=15\nchangeLv=False\n"
            + "lvMultiplier=150\namplifyMaddening=False\namplifyMaddeningAmount=50\n"
            + "ensureLowLvEarly=False\nRandomizerExtras.blockedAbilities=148,154,158,190,191,193,195,197,198,199,201,205,206,207,225,226,240,246,247,249\nRandomizerExtras.blockAbilities=True\n"
            + "RandomizerExtras.limitSiegeTomes=True\nRandomizerExtras.maxEnemyWeaponRange=4\nRandomizerExtras.siegeChance=10\n"
            + "RandomizerExtras.growthShift=10\nRandomizerExtras.evenClassVariety=True\nRandomizerExtras.classVarietySafety=35\n"
            + "RandomizerExtras.classIdentity=True\nRandomizerExtras.classDefenseClampEarly=20\nRandomizerExtras.classDefenseClampMid=35\n"
            + "RandomizerExtras.classDefenseClampAdv=50\nRandomizerExtras.classDefenseClampMaster=50\nRandomizerExtras.flatHpTrimEarly=6\n"
            + "RandomizerExtras.flatHpTrimMid=4\nRandomizerExtras.flatHpTrimAdv=1\nRandomizerExtras.flatHpTrimMaster=1\n"
            + "RandomizerExtras.flatDefTrimEarly=2\nRandomizerExtras.flatDefTrimMid=2\nRandomizerExtras.flatDefTrimAdv=1\n"
            + "RandomizerExtras.flatDefTrimMaster=0\nRandomizerExtras.flatResTrimEarly=7\nRandomizerExtras.flatResTrimMid=2\n"
            + "RandomizerExtras.flatResTrimAdv=1\nRandomizerExtras.flatResTrimMaster=0\nRandomizerExtras.bossHpTrim=1\n"
            + "RandomizerExtras.bossDefTrim=1\nRandomizerExtras.bossResTrim=1\nRandomizerExtras.playerStartBonus=1\n"
            + "RandomizerMore.protectBossAbilities=True\nRandomizerMore.protectBossStats=True\nRandomizerMore.bossScalingShare=50\n"
            + "RandomizerMore.uniqueClassAbilities=True\nRandomizerMore.uniqueTierMastery=True\nRandomizerMore.uniqueTierMasteryArt=True\n"
            + "RandomizerMore.uniquePersonalAbilities=True\nRandomizerMore.clampGrowths=True\nRandomizerMore.growthClampMin=5\n"
            + "RandomizerMore.growthClampMax=100\nRandomizerMore.evenGrowthTotals=True\nRandomizerMore.growthTotalTolerance=60\n"
            + "RandomizerMore.protectGrowthIdentity=True\nRandomizerMore.clampStatBoosts=True\nRandomizerMore.clampStatBases=True\n"
            + "RandomizerMore.evenStatTotals=True\nRandomizerMore.statTotalTolerance=30\nRandomizerMore.statBoostClampMin=-3\n"
            + "RandomizerMore.statBoostClampMax=10\nRandomizerMore.statBaseClampMin=3\nRandomizerMore.statBaseClampMax=30\n"
            + "RandomizerBig.swapClassStatProfiles=False\nRandomizerBig.swapClassGrowthProfiles=False\nRandomizerBig.shuffleCharacterSheets=False\n"
            + "RandomizerBig.bossRemix=False\nRandomizerBig.weaponGraftChance=40\nRandomizerBig.weaponFromArtChance=10\n"
            + "RandomizerBig.weaponFromGambitChance=10\nRandomizerBig.artGraftChance=20\nRandomizerBig.artFromWeaponChance=10\n"
            + "RandomizerBig.gambitGraftChance=20\nRandomizerBig.gambitFromWeaponChance=80\nRandomizerBig.uniqueWeaponEffects=True\n"
            + "RandomizerBig.weaponDonorsDifferInType=False\nRandomizerBig.swapScope=0\nRandomizerBig.describeWeaponEffects=True\n"
            + "ModSupport.modMode=True\nModSupport.keepMasteryExp=True\nModSupport.noMonasteryMode=True\n";

        private const string builtInRunName = "My run - No Monastery 2.1, 2026-10-24";

        // Rank 4 is C. No Monastery starts units in intermediate classes, so a
        // spell written at the game's usual D is already known on the first
        // map for every character; C delays it to the rank actually earned.
        private const int firstSpellRankForMod = 4;

        // No Monastery raises the level of every enemy, and levels convert the
        // class growths straight into Def and Res - which is what made every
        // enemy of the 1.12.7 run feel armoured. The record run already moved
        // 10% of the enemy Str/Mag/Def/Res growth into HP; the mod presets move
        // 30%, so the extra levels buy HP (which every unit can chip through)
        // instead of a defence wall (which a weak unit cannot).
        private const int modEnemyGrowthShift = 30;
        // The clamp is per level band. The early classes are the ones that make
        // the first maps feel armoured - a base is a floor for every enemy in
        // the class, and at level 4 the floor is most of the stat - so they are
        // held much closer to vanilla than the late ones.
        private const int modClassDefenseEarly = 20;
        private const int modClassDefenseMid = 35;
        private const int modClassDefenseAdv = 50;
        private const int modClassDefenseMaster = 50;

        // The same argument, one step further: a flat subtraction from the class
        // bases. The base is the level 1 value and a floor for every unit in the
        // class, so this is what un-walls the first maps; at level 40 the same
        // two or three points are noise. Boost and growth are not touched.
        //
        // Per band. The late bands keep the numbers the presets have always
        // used; the level 1-9 row is turned up, because that is where a base is
        // most of the stat and where the first maps are decided. The player
        // compensation takes the largest band, so the extra early subtraction
        // costs the player's units nothing - it is a pure enemy-side cut.
        private const int modFlatHpTrim = 6;
        private const int modFlatDefTrim = 2;
        private const int modFlatResTrim = 2;
        private const int modFlatHpTrimEarly = 10;
        private const int modFlatDefTrimEarly = 4;
        private const int modFlatResTrimEarly = 4;

        // ... and the same again for the story units, whose personal bases are
        // their own: a boss that needs a level 12 Jeralt to kill is not a fight,
        // it is a wall.
        private const int modBossHpTrim = 6;
        private const int modBossDefTrim = 2;
        private const int modBossResTrim = 2;

        // No Monastery starts the enemies at level 6 on Hard and 9 on Maddening
        // while the player's own units keep their vanilla start, so the first
        // map asks for a level 12 Jeralt. The trims above take the enemy down;
        // this takes the player up by the same currency a level is made of -
        // that unit's own growth, once per level, on every stat. Five levels is
        // the Maddening gap (9 - ~4); it is also a sane gain on Hard.
        private const int modPlayerStartBonus = 5;

        private static string modBandSuffix(int band)
        {
            switch (band)
            {
                case 0: return "Early";
                case 1: return "Mid";
                case 2: return "Adv";
                default: return "Master";
            }
        }

        private static void appendModBulkLines(StringBuilder text)
        {
            text.AppendLine("RandomizerExtras.growthShift=" + modEnemyGrowthShift.ToString());
            text.AppendLine("RandomizerExtras.classIdentity=True");
            text.AppendLine("RandomizerExtras.classDefenseClampEarly=" + modClassDefenseEarly.ToString());
            text.AppendLine("RandomizerExtras.classDefenseClampMid=" + modClassDefenseMid.ToString());
            text.AppendLine("RandomizerExtras.classDefenseClampAdv=" + modClassDefenseAdv.ToString());
            text.AppendLine("RandomizerExtras.classDefenseClampMaster=" + modClassDefenseMaster.ToString());
            text.AppendLine("RandomizerBig.describeWeaponEffects=True");
            // Level 1-9 is cut harder than the rest; the other three bands keep
            // the numbers the presets have always used.
            for (int band = 0; band < 4; band++)
            {
                int hp = band == 0 ? modFlatHpTrimEarly : modFlatHpTrim;
                int def = band == 0 ? modFlatDefTrimEarly : modFlatDefTrim;
                int res = band == 0 ? modFlatResTrimEarly : modFlatResTrim;
                text.AppendLine("RandomizerExtras.flatHpTrim" + modBandSuffix(band)
                    + "=" + hp.ToString());
                text.AppendLine("RandomizerExtras.flatDefTrim" + modBandSuffix(band)
                    + "=" + def.ToString());
                text.AppendLine("RandomizerExtras.flatResTrim" + modBandSuffix(band)
                    + "=" + res.ToString());
            }
            text.AppendLine("RandomizerExtras.bossHpTrim=" + modBossHpTrim.ToString());
            text.AppendLine("RandomizerExtras.bossDefTrim=" + modBossDefTrim.ToString());
            text.AppendLine("RandomizerExtras.bossResTrim=" + modBossResTrim.ToString());
            text.AppendLine("RandomizerExtras.playerStartBonus=" + modPlayerStartBonus.ToString());
        }
        private const string builtInModSafeName = "No Monastery (mod safe)";
        private const string builtInPluginSafeName = "No Monastery 2.1 (plugin, mod safe)";

        // The same run with every option that fights a total conversion written
        // off explicitly. Leaving a conflicting option out is not enough: the
        // tool's default for a few of them is on, and the preset applies on top
        // of the defaults.
        private static string builtInModSafeData()
        {
            StringBuilder text = new StringBuilder();
            string[] lines = builtInRunData.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                int equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    text.AppendLine(line);
                    continue;
                }
                string key = line.Substring(0, equals);
                if (key.IndexOf('.') <= 0 && ModSupport.isConflictKey(key, ModSupport.kindDataMod))
                    continue;
                text.AppendLine(line);
            }

            for (int i = 0; i < ModSupport.conflictCount(ModSupport.kindDataMod); i++)
                text.AppendLine(ModSupport.conflictKeyAt(ModSupport.kindDataMod, i) + "=False");
            text.AppendLine("ModSupport.modMode=True");
            text.AppendLine("ModSupport.keepMasteryExp=True");
            text.AppendLine("ModSupport.noMonasteryMode=True");
            // The mod starts units in intermediate classes, so a rank D first
            // spell is earned at level 1 and everybody can cast from the start.
            text.AppendLine("spellFirstRank=" + firstSpellRankForMod.ToString());
            appendModBulkLines(text);
            return text.ToString();
        }

        // No Monastery 2.1 is a Skyline plugin, not a pile of data files: it owns
        // only the handful of systems it hooks, so only those are written off.
        // keepMasteryExp is off here - unlike 1.3.3 there are no mod values to
        // preserve, so randomized mastery requirements are left randomized.
        private static string builtInPluginSafeData()
        {
            StringBuilder text = new StringBuilder();
            string[] lines = builtInRunData.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                int equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    text.AppendLine(line);
                    continue;
                }
                string key = line.Substring(0, equals);
                if (key.IndexOf('.') <= 0 && ModSupport.isConflictKey(key, ModSupport.kindPluginMod))
                    continue;
                text.AppendLine(line);
            }

            for (int i = 0; i < ModSupport.conflictCount(ModSupport.kindPluginMod); i++)
                text.AppendLine(ModSupport.conflictKeyAt(ModSupport.kindPluginMod, i) + "=False");
            text.AppendLine("ModSupport.modMode=True");
            text.AppendLine("ModSupport.keepMasteryExp=False");
            text.AppendLine("ModSupport.noMonasteryMode=True");
            text.AppendLine("spellFirstRank=" + firstSpellRankForMod.ToString());
            appendModBulkLines(text);
            return text.ToString();
        }

        // ------------------------------------------------------------- registry
        private static List<Preset> cache;

        // What the run is using that the applied preset did not set. The report
        // used to show only the values the run ended up with, so a value that was
        // changed after the preset was applied looked exactly like a value the
        // preset produced - which is unanswerable from a debug report and cost a
        // whole round of guessing.
        //
        // Returns one line per key: "key: preset=X, run=Y".
        public static List<string> diffFromApplied(Settings settings)
        {
            List<string> result = new List<string>();
            if (lastAppliedData == null || lastAppliedData.Length == 0)
                return result;

            Dictionary<string, string> wanted = parse(lastAppliedData);
            foreach (KeyValuePair<string, string> pair in wanted)
            {
                string key = pair.Key;
                if (key == "ModSupport.originalFolder" || key == "ModSupport.modFolder")
                    continue; // a path, not an option
                string mine = currentValue(settings, key);
                if (mine == null)
                    continue; // not something this build has (an older preset)
                if (string.Equals(mine, pair.Value, StringComparison.Ordinal))
                    continue;
                result.Add(key + ": preset=" + pair.Value + ", run=" + mine);
            }
            return result;
        }

        // Reads one setting back out, in the same "name=value" vocabulary the
        // preset files use. null when this build has no such setting.
        private static string currentValue(Settings settings, string key)
        {
            if (key == "RandomizerExtras.blockedAbilities")
            {
                // Order does not matter for the blocklist, so compare it sorted.
                List<int> ids = new List<int>(RandomizerExtras.blockedAbilities);
                ids.Sort();
                StringBuilder list = new StringBuilder();
                for (int i = 0; i < ids.Count; i++)
                {
                    if (list.Length > 0)
                        list.Append(',');
                    list.Append(ids[i].ToString());
                }
                return list.ToString();
            }

            int dot = key.IndexOf('.');
            if (dot > 0)
            {
                string typeName = key.Substring(0, dot);
                string fieldName = key.Substring(dot + 1);
                for (int t = 0; t < extraTypes.Length; t++)
                {
                    if (!string.Equals(extraTypes[t].Name, typeName, StringComparison.Ordinal))
                        continue;
                    FieldInfo field = extraTypes[t].GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                    if (field == null || !isSimple(field))
                        return null;
                    try
                    {
                        return format(field.GetValue(null));
                    }
                    catch
                    {
                        return null;
                    }
                }
                return null;
            }

            if (settings == null)
                return null;
            FieldInfo settingsField = typeof(Settings).GetField(key, BindingFlags.Public | BindingFlags.Instance);
            if (settingsField == null || !isSimple(settingsField))
                return null;
            try
            {
                return format(settingsField.GetValue(settings));
            }
            catch
            {
                return null;
            }
        }

        // Names that appear more than once in the list - normally a user file
        // saved under the same name as the built-in it came from. Applying "the"
        // preset of that name is then a coin toss, so both the list and the
        // report say so.
        public static List<string> duplicateNames()
        {
            List<string> result = new List<string>();
            List<Preset> presets = all();
            for (int i = 0; i < presets.Count; i++)
            {
                if (result.Contains(presets[i].Name))
                    continue;
                for (int j = i + 1; j < presets.Count; j++)
                    if (string.Equals(presets[i].Name, presets[j].Name, StringComparison.Ordinal))
                    {
                        result.Add(presets[i].Name);
                        break;
                    }
            }
            return result;
        }

        public static List<Preset> all()
        {
            if (cache == null)
                refresh();
            return cache;
        }

        public static void refresh()
        {
            cache = new List<Preset>();

            Preset builtIn = new Preset();
            builtIn.Name = builtInRunName;
            builtIn.Description = countLines(builtInRunData).ToString()
                + " values: his own preset file of 2026-10-24, the range limits it was missing,"
                + " and the tuned grid (clamp 20/35/50/50, class trim -10/-4/-4 on level 1-9 and"
                + " -6/-2/-2 after, boss trim -6/-2/-2, player start bonus 5)";
            builtIn.Data = builtInRunData;
            builtIn.BuiltIn = true;
            cache.Add(builtIn);

            Preset modSafe = new Preset();
            modSafe.Name = builtInModSafeName;
            modSafe.Description = "No Monastery 1.3.3, the data-file release: the same run with everything"
                + " the mod ships handed back to it (" + ModSupport.conflictCount(ModSupport.kindDataMod).ToString()
                + " options off), first spell at C, 30% of the enemy growth shifted into HP,"
                + " a per-level Def / Res clamp of 20/35/50/50, -10 HP / -4 Def / -4 Res on the"
                + " level 1-9 class bases and -6 / -2 / -2 after, -6 / -2 / -2 on every boss, and"
                + " five levels of stats on your own units";
            modSafe.Data = builtInModSafeData();
            modSafe.BuiltIn = true;
            cache.Add(modSafe);

            Preset pluginSafe = new Preset();
            pluginSafe.Name = builtInPluginSafeName;
            pluginSafe.Description = "No Monastery 2.1, the Skyline plugin release: the same run with only"
                + " the " + ModSupport.conflictCount(ModSupport.kindPluginMod).ToString()
                + " options the plugin recalculates turned off, first spell at C,"
                + " 30% of the enemy growth shifted into HP, a per-level Def / Res clamp of"
                + " 20/35/50/50, -10 HP / -4 Def / -4 Res on the level 1-9 class bases and"
                + " -6 / -2 / -2 after, -6 / -2 / -2 on every boss, and five levels of stats on"
                + " your own units";
            pluginSafe.Data = builtInPluginSafeData();
            pluginSafe.BuiltIn = true;
            cache.Add(pluginSafe);

            string folder = presetFolder();
            try
            {
                if (Directory.Exists(folder))
                {
                    string[] files = Directory.GetFiles(folder, "*.txt");
                    Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < files.Length; i++)
                    {
                        try
                        {
                            Preset preset = new Preset();
                            preset.Path = files[i];
                            preset.Name = Path.GetFileNameWithoutExtension(files[i]);
                            preset.Data = File.ReadAllText(files[i]);
                            preset.Description = countLines(preset.Data).ToString() + " value(s)";
                            preset.BuiltIn = false;
                            cache.Add(preset);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static int countLines(string data)
        {
            int count = 0;
            string[] lines = data.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].Trim().Length > 0 && !lines[i].TrimStart().StartsWith("#"))
                    count++;
            return count;
        }

        // ------------------------------------------------------------- capture
        private static readonly Type[] extraTypes =
            { typeof(RandomizerExtras), typeof(RandomizerMore), typeof(RandomizerBig), typeof(ModSupport) };

        // The values the extra passes start with, remembered the first time this
        // is looked at (during window load, before anything is ticked).
        private static Dictionary<string, string> extraDefaults;

        public static void rememberDefaults()
        {
            if (extraDefaults != null)
                return;
            extraDefaults = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int t = 0; t < extraTypes.Length; t++)
            {
                FieldInfo[] fields = extraTypes[t].GetFields(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (!isSimple(field) || skipExtra(field.Name))
                        continue;
                    try
                    {
                        extraDefaults[extraTypes[t].Name + "." + field.Name] = format(field.GetValue(null));
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static bool isSimple(FieldInfo field)
        {
            if (field.IsInitOnly || field.IsLiteral)
                return false;
            Type type = field.FieldType;
            return type == typeof(bool) || type == typeof(int) || type == typeof(double);
        }

        // The counters are named stat<Something>, which is also how a few of the
        // real options start (the range limits below). Skipping every name that
        // begins with "stat" silently dropped those numbers from every preset
        // and left them on their defaults when one was applied, so the handful
        // of options are listed here by name.
        private static readonly string[] statNamedOptions =
        {
            "statBoostClampMin", "statBoostClampMax",
            "statBaseClampMin", "statBaseClampMax",
            "statTotalTolerance"
        };

        private static bool skipExtra(string name)
        {
            if (name == null)
                return true;
            if (!name.StartsWith("stat", StringComparison.Ordinal))
                return false;
            for (int i = 0; i < statNamedOptions.Length; i++)
                if (statNamedOptions[i] == name)
                    return false;
            return true;
        }

        // Everything the configuration consists of, in preset file form.
        public static string capture(Settings settings)
        {
            rememberDefaults();
            StringBuilder text = new StringBuilder();
            text.AppendLine("# FE3H randomizer preset - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            text.AppendLine("# Apply with: Extra options > Presets > Apply preset");

            if (settings != null)
            {
                FieldInfo[] fields = typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!isSimple(fields[i]))
                        continue;
                    try
                    {
                        text.AppendLine(fields[i].Name + "=" + format(fields[i].GetValue(settings)));
                    }
                    catch
                    {
                    }
                }
            }

            text.AppendLine("# options from this project");
            text.AppendLine("RandomizerExtras.blockedAbilities=" + joinBlocked());
            text.AppendLine("ModSupport.originalFolder=" + (ModSupport.originalFolder == null
                ? "" : ModSupport.originalFolder.Replace("\n", " ").Replace("\r", " ")));
            for (int t = 0; t < extraTypes.Length; t++)
            {
                FieldInfo[] fields = extraTypes[t].GetFields(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (!isSimple(field) || skipExtra(field.Name))
                        continue;
                    try
                    {
                        text.AppendLine(extraTypes[t].Name + "." + field.Name + "="
                            + format(field.GetValue(null)));
                    }
                    catch
                    {
                    }
                }
            }
            return text.ToString();
        }

        private static string joinBlocked()
        {
            StringBuilder list = new StringBuilder();
            for (int i = 0; i < RandomizerExtras.blockedAbilities.Count; i++)
            {
                if (i > 0)
                    list.Append(",");
                list.Append(RandomizerExtras.blockedAbilities[i].ToString(CultureInfo.InvariantCulture));
            }
            return list.ToString();
        }

        private static void applyBlocked(string value)
        {
            RandomizerExtras.blockedAbilities.Clear();
            if (value == null)
                return;
            string[] parts = value.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0)
                    continue;
                int id;
                if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                    && !RandomizerExtras.blockedAbilities.Contains(id))
                    RandomizerExtras.blockedAbilities.Add(id);
            }
        }

        private static string format(object value)
        {
            if (value is bool)
                return ((bool)value) ? "True" : "False";
            if (value is double)
                return ((double)value).ToString("R", CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        // --------------------------------------------------------------- apply
        // Resets to the defaults first, then applies every line of the preset.
        // Returns the number of values it could not use (unknown name, bad value).
        public static int apply(Settings settings, Preset preset, out int applied)
        {
            applied = 0;
            lastAppliedSet = 0;
            lastAppliedSkipped = 0;
            if (preset == null)
                return 0;

            rememberDefaults();
            resetToDefaults(settings);

            Dictionary<string, string> pairs = parse(preset.Data);
            lastApplied = preset.Name;
            lastAppliedData = preset.Data;
            lastAppliedSource = preset.Source();
            lastAppliedPath = preset.Path == null ? "" : preset.Path;
            lastAppliedKeys = pairs.Count;
            int skipped = 0;
            foreach (KeyValuePair<string, string> pair in pairs)
            {
                if (setOne(settings, pair.Key, pair.Value))
                    applied++;
                else
                    skipped++;
            }

            // Class variety and the passes keep their own copies of a few values.
            RandomizerMore.bossScalingSlots.Clear();
            lastAppliedSet = applied;
            lastAppliedSkipped = skipped;
            return skipped;
        }

        private static void resetToDefaults(Settings settings)
        {
            if (settings != null)
            {
                Settings defaults = new Settings();
                FieldInfo[] fields = typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!isSimple(fields[i]))
                        continue;
                    try
                    {
                        fields[i].SetValue(settings, fields[i].GetValue(defaults));
                    }
                    catch
                    {
                    }
                }
            }

            RandomizerExtras.blockedAbilities.Clear();
            for (int i = 0; i < RandomizerExtras.defaultBlockedAbilities.Length; i++)
                RandomizerExtras.blockedAbilities.Add(RandomizerExtras.defaultBlockedAbilities[i]);
            ModSupport.originalFolder = "";

            for (int t = 0; t < extraTypes.Length; t++)
            {
                FieldInfo[] fields = extraTypes[t].GetFields(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (!isSimple(field) || skipExtra(field.Name))
                        continue;
                    string key = extraTypes[t].Name + "." + field.Name;
                    string value;
                    if (!extraDefaults.TryGetValue(key, out value))
                        continue;
                    try
                    {
                        field.SetValue(null, convert(field.FieldType, value));
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static bool setOne(Settings settings, string key, string value)
        {
            if (key == null || key.Length == 0)
                return false;

            if (key == "ModSupport.originalFolder")
            {
                ModSupport.originalFolder = value == null ? "" : value;
                return true;
            }

            if (key == "RandomizerExtras.blockedAbilities")
            {
                applyBlocked(value);
                return true;
            }

            int dot = key.IndexOf('.');
            if (dot > 0)
            {
                string typeName = key.Substring(0, dot);
                string fieldName = key.Substring(dot + 1);

                // The clamp used to be one number. A preset written before it
                // was split says classDefenseLimit, and the honest reading of
                // that is "the same allowance for every band", not "ignore me".
                if (typeName == "RandomizerExtras" && fieldName == "classDefenseLimit")
                {
                    int old;
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out old))
                        return false;
                    RandomizerExtras.classDefenseClampEarly = old;
                    RandomizerExtras.classDefenseClampMid = old;
                    RandomizerExtras.classDefenseClampAdv = old;
                    RandomizerExtras.classDefenseClampMaster = old;
                    return true;
                }

                // Same story for the flat trim, which was one number too. An old
                // preset means "that much in every band", not "ignore it".
                if (typeName == "RandomizerExtras"
                    && (fieldName == "flatHpTrim" || fieldName == "flatDefTrim"
                        || fieldName == "flatResTrim"))
                {
                    int old;
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out old))
                        return false;
                    int stat = fieldName == "flatHpTrim" ? 0 : fieldName == "flatDefTrim" ? 1 : 2;
                    for (int band = 0; band < 4; band++)
                        RandomizerExtras.setFlatTrim(band, stat, old);
                    return true;
                }
                for (int t = 0; t < extraTypes.Length; t++)
                {
                    if (!string.Equals(extraTypes[t].Name, typeName, StringComparison.Ordinal))
                        continue;
                    FieldInfo field = extraTypes[t].GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                    if (field == null || !isSimple(field))
                        return false;
                    try
                    {
                        field.SetValue(null, convert(field.FieldType, value));
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                return false;
            }

            if (settings == null)
                return false;
            FieldInfo settingsField = typeof(Settings).GetField(key, BindingFlags.Public | BindingFlags.Instance);
            if (settingsField == null || !isSimple(settingsField))
                return false;
            try
            {
                settingsField.SetValue(settings, convert(settingsField.FieldType, value));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object convert(Type type, string value)
        {
            value = value == null ? "" : value.Trim();
            if (type == typeof(bool))
                return value.Equals("True", StringComparison.OrdinalIgnoreCase) || value == "1";
            if (type == typeof(int))
                return (int)double.Parse(value, CultureInfo.InvariantCulture);
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, string> parse(string data)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.Ordinal);
            if (data == null)
                return pairs;
            string[] lines = data.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                int equals = line.IndexOf('=');
                if (equals <= 0)
                    continue;
                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1).Trim();
                if (key.Length == 0)
                    continue;
                pairs[key] = value;
            }
            return pairs;
        }

        // ----------------------------------------------------------------- disk
        public static string save(Settings settings, string name)
        {
            if (name == null || name.Trim().Length == 0)
                return null;
            string safeName = sanitize(name.Trim());
            // A file may not take a built-in name. Two presets with the same
            // name are indistinguishable in the list, in the report and in the
            // combo box, so whichever one the user meant, the other can be
            // applied instead and the values look like they came from nowhere.
            if (builtInNameExists(safeName))
                safeName = safeName + " (my file)";
            string folder = presetFolder();
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, safeName + ".txt");
            File.WriteAllText(path, capture(settings));
            refresh();
            return path;
        }

        public static bool delete(Preset preset)
        {
            if (preset == null || preset.BuiltIn || preset.Path == null || preset.Path.Length == 0)
                return false;
            try
            {
                File.Delete(preset.Path);
                refresh();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // True when one of the presets the exe ships is called this.
        private static bool builtInNameExists(string name)
        {
            string[] builtIn = { builtInRunName, builtInModSafeName, builtInPluginSafeName };
            for (int i = 0; i < builtIn.Length; i++)
                if (string.Equals(builtIn[i], name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string sanitize(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder clean = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char character = name[i];
                bool bad = false;
                for (int j = 0; j < invalid.Length; j++)
                    if (character == invalid[j])
                    {
                        bad = true;
                        break;
                    }
                clean.Append(bad ? '_' : character);
            }
            string result = clean.ToString().Trim();
            return result.Length == 0 ? "preset" : result;
        }

        // The preset that is currently applied, so the tab can show it - and how
        // much of it actually landed, which is how a broken preset shows up.
        public static string lastApplied = "";
        // Provenance of the last applied preset: where it came from, and how many
        // keys it actually had. A preset that sets 1116 of its 1116 keys is not
        // the same thing as one that silently skipped a few.
        public static string lastAppliedData = "";
        public static string lastAppliedSource = "";
        public static string lastAppliedPath = "";
        public static int lastAppliedKeys;
        public static int lastAppliedSet;
        public static int lastAppliedSkipped;

        public static string describe(Preset preset)
        {
            if (preset == null)
                return "";
            return preset.Name + " - " + preset.Description;
        }
    }
}
