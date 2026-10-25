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

            public override string ToString()
            {
                return this.BuiltIn ? this.Name + "  (built in)" : this.Name;
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
        private static readonly string builtInRunData =
              "randStatBoosts=True\nrandMonStatBoosts=False\nrandStaStatBoosts=True\nrandBegStatBoosts=True\n"
            + "randIntStatBoosts=True\nrandAdvStatBoosts=True\nrandMasStatBoosts=True\nrandUniStatBoosts=True\n"
            + "randMovStatBoosts=True\nmonStatBoostsPosCountMin=0\nmonStatBoostsPosCountMax=0\n"
            + "staStatBoostsPosCountMin=0\nstaStatBoostsPosCountMax=0\nbegStatBoostsPosCountMin=1\n"
            + "begStatBoostsPosCountMax=1\nintStatBoostsPosCountMin=2\nintStatBoostsPosCountMax=4\n"
            + "advStatBoostsPosCountMin=2\nadvStatBoostsPosCountMax=5\nmasStatBoostsPosCountMin=3\n"
            + "masStatBoostsPosCountMax=7\nuniStatBoostsPosCountMin=3\nuniStatBoostsPosCountMax=9\n"
            + "monStatBoostsNegCountMin=0\nmonStatBoostsNegCountMax=0\nstaStatBoostsNegCountMin=0\n"
            + "staStatBoostsNegCountMax=0\nbegStatBoostsNegCountMin=0\nbegStatBoostsNegCountMax=0\n"
            + "intStatBoostsNegCountMin=0\nintStatBoostsNegCountMax=2\nadvStatBoostsNegCountMin=1\n"
            + "advStatBoostsNegCountMax=2\nmasStatBoostsNegCountMin=0\nmasStatBoostsNegCountMax=1\n"
            + "uniStatBoostsNegCountMin=0\nuniStatBoostsNegCountMax=1\nmonStatBoostsPosMin=0\n"
            + "monStatBoostsPosMax=0\nstaStatBoostsPosMin=0\nstaStatBoostsPosMax=0\nbegStatBoostsPosMin=1\n"
            + "begStatBoostsPosMax=1\nintStatBoostsPosMin=1\nintStatBoostsPosMax=4\nadvStatBoostsPosMin=1\n"
            + "advStatBoostsPosMax=10\nmasStatBoostsPosMin=1\nmasStatBoostsPosMax=6\nuniStatBoostsPosMin=1\n"
            + "uniStatBoostsPosMax=8\nmonStatBoostsNegMin=0\nmonStatBoostsNegMax=0\nstaStatBoostsNegMin=0\n"
            + "staStatBoostsNegMax=0\nbegStatBoostsNegMin=0\nbegStatBoostsNegMax=0\nintStatBoostsNegMin=-2\n"
            + "intStatBoostsNegMax=-1\nadvStatBoostsNegMin=-4\nadvStatBoostsNegMax=-2\nmasStatBoostsNegMin=-2\n"
            + "masStatBoostsNegMax=-1\nuniStatBoostsNegMin=-4\nuniStatBoostsNegMax=-1\nmonMovStatBoostsMin=0\n"
            + "monMovStatBoostsMax=2\nstaMovStatBoostsMin=0\nstaMovStatBoostsMax=0\nbegMovStatBoostsMin=0\n"
            + "begMovStatBoostsMax=0\nintMovStatBoostsMin=0\nintMovStatBoostsMax=1\nadvMovStatBoostsMin=0\n"
            + "advMovStatBoostsMax=2\nmasMovStatBoostsMin=1\nmasMovStatBoostsMax=2\nuniMovStatBoostsMin=1\n"
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
            + "monMountBoostsPosMax=0\nstaMountBoostsPosMin=0\nstaMountBoostsPosMax=0\nbegMountBoostsPosMin=0\n"
            + "begMountBoostsPosMax=0\nintMountBoostsPosMin=2\nintMountBoostsPosMax=3\nadvMountBoostsPosMin=1\n"
            + "advMountBoostsPosMax=2\nmasMountBoostsPosMin=1\nmasMountBoostsPosMax=3\nuniMountBoostsPosMin=1\n"
            + "uniMountBoostsPosMax=3\nmonMountBoostsNegMin=0\nmonMountBoostsNegMax=0\nstaMountBoostsNegMin=0\n"
            + "staMountBoostsNegMax=0\nbegMountBoostsNegMin=0\nbegMountBoostsNegMax=0\n"
            + "intMountBoostsNegMin=-2\nintMountBoostsNegMax=-2\nadvMountBoostsNegMin=-2\n"
            + "advMountBoostsNegMax=-2\nmasMountBoostsNegMin=0\nmasMountBoostsNegMax=0\n"
            + "uniMountBoostsNegMin=-2\nuniMountBoostsNegMax=-2\nmonMovMountBoostsMin=0\n"
            + "monMovMountBoostsMax=0\nstaMovMountBoostsMin=0\nstaMovMountBoostsMax=0\nbegMovMountBoostsMin=0\n"
            + "begMovMountBoostsMax=0\nintMovMountBoostsMin=2\nintMovMountBoostsMax=3\nadvMovMountBoostsMin=2\n"
            + "advMovMountBoostsMax=2\nmasMovMountBoostsMin=1\nmasMovMountBoostsMax=3\nuniMovMountBoostsMin=2\n"
            + "uniMovMountBoostsMax=3\nrandPlayerGrowthBoosts=True\nrandMonPlayerGrowthBoosts=False\n"
            + "randStaPlayerGrowthBoosts=True\nrandBegPlayerGrowthBoosts=True\n"
            + "randIntPlayerGrowthBoosts=True\nrandAdvPlayerGrowthBoosts=True\n"
            + "randMasPlayerGrowthBoosts=True\nrandUniPlayerGrowthBoosts=True\n"
            + "randMovPlayerGrowthBoosts=False\nmonPlayerGrowthBoostsPosCountMin=0\n"
            + "monPlayerGrowthBoostsPosCountMax=0\nstaPlayerGrowthBoostsPosCountMin=0\n"
            + "staPlayerGrowthBoostsPosCountMax=1\nbegPlayerGrowthBoostsPosCountMin=3\n"
            + "begPlayerGrowthBoostsPosCountMax=3\nintPlayerGrowthBoostsPosCountMin=3\n"
            + "intPlayerGrowthBoostsPosCountMax=5\nadvPlayerGrowthBoostsPosCountMin=3\n"
            + "advPlayerGrowthBoostsPosCountMax=7\nmasPlayerGrowthBoostsPosCountMin=2\n"
            + "masPlayerGrowthBoostsPosCountMax=6\nuniPlayerGrowthBoostsPosCountMin=2\n"
            + "uniPlayerGrowthBoostsPosCountMax=6\nmonPlayerGrowthBoostsNegCountMin=0\n"
            + "monPlayerGrowthBoostsNegCountMax=0\nstaPlayerGrowthBoostsNegCountMin=0\n"
            + "staPlayerGrowthBoostsNegCountMax=0\nbegPlayerGrowthBoostsNegCountMin=0\n"
            + "begPlayerGrowthBoostsNegCountMax=1\nintPlayerGrowthBoostsNegCountMin=0\n"
            + "intPlayerGrowthBoostsNegCountMax=2\nadvPlayerGrowthBoostsNegCountMin=0\n"
            + "advPlayerGrowthBoostsNegCountMax=2\nmasPlayerGrowthBoostsNegCountMin=0\n"
            + "masPlayerGrowthBoostsNegCountMax=2\nuniPlayerGrowthBoostsNegCountMin=0\n"
            + "uniPlayerGrowthBoostsNegCountMax=3\nmonPlayerGrowthBoostsPosMin=0\n"
            + "monPlayerGrowthBoostsPosMax=0\nstaPlayerGrowthBoostsPosMin=5\nstaPlayerGrowthBoostsPosMax=5\n"
            + "begPlayerGrowthBoostsPosMin=5\nbegPlayerGrowthBoostsPosMax=10\nintPlayerGrowthBoostsPosMin=5\n"
            + "intPlayerGrowthBoostsPosMax=30\nadvPlayerGrowthBoostsPosMin=5\nadvPlayerGrowthBoostsPosMax=40\n"
            + "masPlayerGrowthBoostsPosMin=5\nmasPlayerGrowthBoostsPosMax=40\nuniPlayerGrowthBoostsPosMin=5\n"
            + "uniPlayerGrowthBoostsPosMax=40\nmonPlayerGrowthBoostsNegMin=0\nmonPlayerGrowthBoostsNegMax=0\n"
            + "staPlayerGrowthBoostsNegMin=0\nstaPlayerGrowthBoostsNegMax=0\nbegPlayerGrowthBoostsNegMin=-5\n"
            + "begPlayerGrowthBoostsNegMax=-5\nintPlayerGrowthBoostsNegMin=-10\n"
            + "intPlayerGrowthBoostsNegMax=-5\nadvPlayerGrowthBoostsNegMin=-10\n"
            + "advPlayerGrowthBoostsNegMax=-5\nmasPlayerGrowthBoostsNegMin=-5\n"
            + "masPlayerGrowthBoostsNegMax=-5\nuniPlayerGrowthBoostsNegMin=-5\n"
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
            + "randMovEnemyGrowthBoosts=False\nmonEnemyGrowthBoostsPosCountMin=1\n"
            + "monEnemyGrowthBoostsPosCountMax=1\nstaEnemyGrowthBoostsPosCountMin=0\n"
            + "staEnemyGrowthBoostsPosCountMax=0\nbegEnemyGrowthBoostsPosCountMin=3\n"
            + "begEnemyGrowthBoostsPosCountMax=3\nintEnemyGrowthBoostsPosCountMin=3\n"
            + "intEnemyGrowthBoostsPosCountMax=6\nadvEnemyGrowthBoostsPosCountMin=3\n"
            + "advEnemyGrowthBoostsPosCountMax=6\nmasEnemyGrowthBoostsPosCountMin=4\n"
            + "masEnemyGrowthBoostsPosCountMax=6\nuniEnemyGrowthBoostsPosCountMin=3\n"
            + "uniEnemyGrowthBoostsPosCountMax=5\nmonEnemyGrowthBoostsNegCountMin=1\n"
            + "monEnemyGrowthBoostsNegCountMax=1\nstaEnemyGrowthBoostsNegCountMin=0\n"
            + "staEnemyGrowthBoostsNegCountMax=0\nbegEnemyGrowthBoostsNegCountMin=1\n"
            + "begEnemyGrowthBoostsNegCountMax=1\nintEnemyGrowthBoostsNegCountMin=0\n"
            + "intEnemyGrowthBoostsNegCountMax=2\nadvEnemyGrowthBoostsNegCountMin=0\n"
            + "advEnemyGrowthBoostsNegCountMax=2\nmasEnemyGrowthBoostsNegCountMin=1\n"
            + "masEnemyGrowthBoostsNegCountMax=1\nuniEnemyGrowthBoostsNegCountMin=1\n"
            + "uniEnemyGrowthBoostsNegCountMax=2\nmonEnemyGrowthBoostsPosMin=5\nmonEnemyGrowthBoostsPosMax=5\n"
            + "staEnemyGrowthBoostsPosMin=0\nstaEnemyGrowthBoostsPosMax=0\nbegEnemyGrowthBoostsPosMin=5\n"
            + "begEnemyGrowthBoostsPosMax=25\nintEnemyGrowthBoostsPosMin=5\nintEnemyGrowthBoostsPosMax=40\n"
            + "advEnemyGrowthBoostsPosMin=5\nadvEnemyGrowthBoostsPosMax=60\nmasEnemyGrowthBoostsPosMin=5\n"
            + "masEnemyGrowthBoostsPosMax=60\nuniEnemyGrowthBoostsPosMin=15\nuniEnemyGrowthBoostsPosMax=60\n"
            + "monEnemyGrowthBoostsNegMin=-10\nmonEnemyGrowthBoostsNegMax=-10\nstaEnemyGrowthBoostsNegMin=0\n"
            + "staEnemyGrowthBoostsNegMax=0\nbegEnemyGrowthBoostsNegMin=-5\nbegEnemyGrowthBoostsNegMax=0\n"
            + "intEnemyGrowthBoostsNegMin=-30\nintEnemyGrowthBoostsNegMax=-5\nadvEnemyGrowthBoostsNegMin=-30\n"
            + "advEnemyGrowthBoostsNegMax=-5\nmasEnemyGrowthBoostsNegMin=-10\nmasEnemyGrowthBoostsNegMax=-5\n"
            + "uniEnemyGrowthBoostsNegMin=-20\nuniEnemyGrowthBoostsNegMax=-5\nmonMovEnemyGrowthBoostsMin=0\n"
            + "monMovEnemyGrowthBoostsMax=0\nstaMovEnemyGrowthBoostsMin=1\nstaMovEnemyGrowthBoostsMax=5\n"
            + "begMovEnemyGrowthBoostsMin=1\nbegMovEnemyGrowthBoostsMax=5\nintMovEnemyGrowthBoostsMin=1\n"
            + "intMovEnemyGrowthBoostsMax=5\nadvMovEnemyGrowthBoostsMin=1\nadvMovEnemyGrowthBoostsMax=5\n"
            + "masMovEnemyGrowthBoostsMin=1\nmasMovEnemyGrowthBoostsMax=5\nuniMovEnemyGrowthBoostsMin=1\n"
            + "uniMovEnemyGrowthBoostsMax=5\nmonMovEnemyGrowthBoostsP=0\nstaMovEnemyGrowthBoostsP=10\n"
            + "begMovEnemyGrowthBoostsP=10\nintMovEnemyGrowthBoostsP=10\nadvMovEnemyGrowthBoostsP=10\n"
            + "masMovEnemyGrowthBoostsP=10\nuniMovEnemyGrowthBoostsP=10\nrandClassBases=True\n"
            + "randMonClassBases=False\nrandStaClassBases=True\nrandBegClassBases=True\n"
            + "randIntClassBases=True\nrandAdvClassBases=True\nrandMasClassBases=True\nrandUniClassBases=True\n"
            + "hpMonClassBasesMin=0\nhpMonClassBasesMax=0\nhpStaClassBasesMin=1\nhpStaClassBasesMax=1\n"
            + "hpBegClassBasesMin=20\nhpBegClassBasesMax=20\nhpIntClassBasesMin=25\nhpIntClassBasesMax=30\n"
            + "hpAdvClassBasesMin=20\nhpAdvClassBasesMax=35\nhpMasClassBasesMin=32\nhpMasClassBasesMax=34\n"
            + "hpUniClassBasesMin=28\nhpUniClassBasesMax=38\nstrMonClassBasesMin=0\nstrMonClassBasesMax=0\n"
            + "strStaClassBasesMin=0\nstrStaClassBasesMax=0\nstrBegClassBasesMin=4\nstrBegClassBasesMax=8\n"
            + "strIntClassBasesMin=4\nstrIntClassBasesMax=10\nstrAdvClassBasesMin=5\nstrAdvClassBasesMax=20\n"
            + "strMasClassBasesMin=8\nstrMasClassBasesMax=20\nstrUniClassBasesMin=8\nstrUniClassBasesMax=18\n"
            + "magMonClassBasesMin=0\nmagMonClassBasesMax=0\nmagStaClassBasesMin=0\nmagStaClassBasesMax=0\n"
            + "magBegClassBasesMin=3\nmagBegClassBasesMax=8\nmagIntClassBasesMin=6\nmagIntClassBasesMax=10\n"
            + "magAdvClassBasesMin=6\nmagAdvClassBasesMax=17\nmagMasClassBasesMin=10\nmagMasClassBasesMax=17\n"
            + "magUniClassBasesMin=8\nmagUniClassBasesMax=17\ndexMonClassBasesMin=0\ndexMonClassBasesMax=0\n"
            + "dexStaClassBasesMin=0\ndexStaClassBasesMax=1\ndexBegClassBasesMin=6\ndexBegClassBasesMax=6\n"
            + "dexIntClassBasesMin=7\ndexIntClassBasesMax=11\ndexAdvClassBasesMin=7\ndexAdvClassBasesMax=12\n"
            + "dexMasClassBasesMin=14\ndexMasClassBasesMax=14\ndexUniClassBasesMin=11\ndexUniClassBasesMax=14\n"
            + "speMonClassBasesMin=0\nspeMonClassBasesMax=0\nspeStaClassBasesMin=0\nspeStaClassBasesMax=0\n"
            + "speBegClassBasesMin=6\nspeBegClassBasesMax=6\nspeIntClassBasesMin=4\nspeIntClassBasesMax=11\n"
            + "speAdvClassBasesMin=8\nspeAdvClassBasesMax=14\nspeMasClassBasesMin=10\nspeMasClassBasesMax=20\n"
            + "speUniClassBasesMin=6\nspeUniClassBasesMax=20\nlucMonClassBasesMin=0\nlucMonClassBasesMax=0\n"
            + "lucStaClassBasesMin=0\nlucStaClassBasesMax=0\nlucBegClassBasesMin=4\nlucBegClassBasesMax=4\n"
            + "lucIntClassBasesMin=8\nlucIntClassBasesMax=8\nlucAdvClassBasesMin=8\nlucAdvClassBasesMax=12\n"
            + "lucMasClassBasesMin=12\nlucMasClassBasesMax=12\nlucUniClassBasesMin=19\nlucUniClassBasesMax=19\n"
            + "defMonClassBasesMin=0\ndefMonClassBasesMax=0\ndefStaClassBasesMin=0\ndefStaClassBasesMax=0\n"
            + "defBegClassBasesMin=3\ndefBegClassBasesMax=5\ndefIntClassBasesMin=3\ndefIntClassBasesMax=12\n"
            + "defAdvClassBasesMin=5\ndefAdvClassBasesMax=17\ndefMasClassBasesMin=13\ndefMasClassBasesMax=17\n"
            + "defUniClassBasesMin=13\ndefUniClassBasesMax=17\nresMonClassBasesMin=0\nresMonClassBasesMax=0\n"
            + "resStaClassBasesMin=0\nresStaClassBasesMax=0\nresBegClassBasesMin=2\nresBegClassBasesMax=4\n"
            + "resIntClassBasesMin=1\nresIntClassBasesMax=5\nresAdvClassBasesMin=4\nresAdvClassBasesMax=15\n"
            + "resMasClassBasesMin=10\nresMasClassBasesMax=16\nresUniClassBasesMin=8\nresUniClassBasesMax=16\n"
            + "chaMonClassBasesMin=0\nchaMonClassBasesMax=0\nchaStaClassBasesMin=0\nchaStaClassBasesMax=0\n"
            + "chaBegClassBasesMin=4\nchaBegClassBasesMax=5\nchaIntClassBasesMin=5\nchaIntClassBasesMax=9\n"
            + "chaAdvClassBasesMin=6\nchaAdvClassBasesMax=14\nchaMasClassBasesMin=12\nchaMasClassBasesMax=16\n"
            + "chaUniClassBasesMin=9\nchaUniClassBasesMax=16\nremoveWeaponRestrictions=True\n"
            + "randWeaponTypes=True\nmon2ndWeaponTypeP=0\nsta2ndWeaponTypeP=0\nbeg2ndWeaponTypeP=5\n"
            + "int2ndWeaponTypeP=15\nadv2ndWeaponTypeP=35\nmas2ndWeaponTypeP=75\nuni2ndWeaponTypeP=50\n"
            + "randTyping=True\nrandMovTypes=False\nbaseMovTypes=False\nrandClassProf=True\nbaseClassProf=True\n"
            + "randCert=True\nrandMonCert=True\nrandStaCert=True\nrandBegCert=True\nrandIntCert=True\n"
            + "randAdvCert=True\nrandMasCert=True\nrandUniCert=True\nmainMonCertMin=0\nmainMonCertMax=0\n"
            + "mainStaCertMin=0\nmainStaCertMax=0\nmainBegCertMin=2\nmainBegCertMax=2\nmainIntCertMin=4\n"
            + "mainIntCertMax=4\nmainAdvCertMin=6\nmainAdvCertMax=8\nmainMasCertMin=7\nmainMasCertMax=8\n"
            + "mainUniCertMin=0\nmainUniCertMax=0\ntypeMonCertMin=0\ntypeMonCertMax=0\ntypeStaCertMin=0\n"
            + "typeStaCertMax=0\ntypeBegCertMin=0\ntypeBegCertMax=0\ntypeIntCertMin=1\ntypeIntCertMax=2\n"
            + "typeAdvCertMin=4\ntypeAdvCertMax=6\ntypeMasCertMin=7\ntypeMasCertMax=9\ntypeUniCertMin=0\n"
            + "typeUniCertMax=0\notherMonCertMin=0\notherMonCertMax=0\notherStaCertMin=0\notherStaCertMax=0\n"
            + "otherBegCertMin=0\notherBegCertMax=2\notherIntCertMin=0\notherIntCertMax=2\notherAdvCertMin=4\n"
            + "otherAdvCertMax=4\notherMasCertMin=4\notherMasCertMax=6\notherUniCertMin=0\notherUniCertMax=0\n"
            + "baseCert=True\nrandCertItem=False\nallowNonSealCertItems=False\nrandClassAb=True\n"
            + "baseClassAb=True\nrandClassDefAb=True\nrandMonClassDefAb=True\nrandStaClassDefAb=True\n"
            + "randBegClassDefAb=True\nrandIntClassDefAb=True\nrandAdvClassDefAb=True\nrandMasClassDefAb=True\n"
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
            + "uniClassClassAbCountMax=3\nrandClassMasteryAb=False\nrandClassMasteryCa=False\n"
            + "baseClassMasteryCa=True\nrandClassMasteryExpReq=True\nrandMonClassMasteryExpReq=False\n"
            + "randStaClassMasteryExpReq=True\nrandBegClassMasteryExpReq=True\n"
            + "randIntClassMasteryExpReq=True\nrandAdvClassMasteryExpReq=True\n"
            + "randMasClassMasteryExpReq=True\nrandUniClassMasteryExpReq=True\nmonClassMasteryExpReqMin=0\n"
            + "monClassMasteryExpReqMax=0\nstaClassMasteryExpReqMin=10\nstaClassMasteryExpReqMax=30\n"
            + "begClassMasteryExpReqMin=30\nbegClassMasteryExpReqMax=80\nintClassMasteryExpReqMin=50\n"
            + "intClassMasteryExpReqMax=140\nadvClassMasteryExpReqMin=150\nadvClassMasteryExpReqMax=210\n"
            + "masClassMasteryExpReqMin=160\nmasClassMasteryExpReqMax=255\nuniClassMasteryExpReqMin=100\n"
            + "uniClassMasteryExpReqMax=200\nrandAdjutant=True\nspecialToAdv=True\nrandExpYield=False\n"
            + "randMonExpYield=True\nrandStaExpYield=True\nrandBegExpYield=True\nrandIntExpYield=True\n"
            + "randAdvExpYield=True\nrandMasExpYield=True\nrandUniExpYield=True\nmonExpYieldMin=25\n"
            + "monExpYieldMax=75\nstaExpYieldMin=15\nstaExpYieldMax=45\nbegExpYieldMin=15\nbegExpYieldMax=45\n"
            + "intExpYieldMin=18\nintExpYieldMax=54\nadvExpYieldMin=20\nadvExpYieldMax=60\nmasExpYieldMin=20\n"
            + "masExpYieldMax=60\nuniExpYieldMin=25\nuniExpYieldMax=75\nunitsToScale=True\nrandMounts=True\n"
            + "setArmorAni=True\nprovideClassAni=True\nrandClassAni=False\nrandTierLv=False\n"
            + "randBegTierLv=True\nrandIntTierLv=True\nrandAdvTierLv=True\nrandMasTierLv=True\nbegTierLvMin=4\n"
            + "begTierLvMax=6\nintTierLvMin=8\nintTierLvMax=12\nadvTierLvMin=16\nadvTierLvMax=24\n"
            + "masTierLvMin=24\nmasTierLvMax=36\nremoveGenderRestrictions=True\nrandBarrierBreakItem=False\n"
            + "allowNonMineralBarrierBreakItem=False\nrandBarrierAb=False\nrandLatentAb=True\n"
            + "randWeapons=True\nrandSpells=True\nrandTurrets=False\nrandGambits=True\nrandMonsterAoe=True\n"
            + "randAtkStatusEffects=True\natkStatusEffectsP=10\nrandAtkEffectiveness=True\n"
            + "atkEffectivenessP=10\natkEffectivenessPreserveBows=False\nrandAtkCrit=False\natkCritP=30\n"
            + "atkCritMax=50\nrandAtkDoubleAttack=False\natkDoubleAttackP=5\n"
            + "atkDoubleAttackPreserveGauntlets=True\nrandAtkRange=True\nrandAtkRangeP=10\n"
            + "atkRangeBinomial=True\natkRangeLinear=False\natkRangeLinearDev=2\natkRangeProportional=False\n"
            + "atkRangeProportionalDev=1\natkRangeExponential=False\natkRangeExponentialDev=2\n"
            + "randAtkDurability=True\nrandAtkDurabilityP=50\natkDurabilityBinomial=False\n"
            + "atkDurabilityLinear=False\natkDurabilityLinearDev=10\natkDurabilityProportional=False\n"
            + "atkDurabilityProportionalDev=0.5\natkDurabilityExponential=False\n"
            + "atkDurabilityExponentialDev=2\nrandAtkMight=True\nrandAtkMightP=50\natkMightBinomial=False\n"
            + "atkMightLinear=False\natkMightLinearDev=5\natkMightProportional=False\n"
            + "atkMightProportionalDev=0.5\natkMightExponential=False\natkMightExponentialDev=2\n"
            + "randAtkHit=True\nrandAtkHitP=50\natkHitBinomial=False\natkHitLinear=False\natkHitLinearDev=25\n"
            + "atkHitProportional=False\natkHitProportionalDev=0.25\natkHitExponential=False\n"
            + "atkHitExponentialDev=1.5\nrandAtkWeight=True\nrandAtkWeightP=50\natkWeightBinomial=True\n"
            + "atkWeightLinear=False\natkWeightLinearDev=6\natkWeightProportional=False\n"
            + "atkWeightProportionalDev=0.5\natkWeightExponential=False\natkWeightExponentialDev=2\n"
            + "randAtkPhysicalMagicSwitch=True\nrandAtkPhysicalMagicSwitchP=10\nrandAtkIgnoreDefRes=False\n"
            + "randAtkIgnoreDefResP=0.5\nrandAtkLeaveAt1=False\nrandAtkLeaveAt1P=0.5\nrandCa=True\n"
            + "randCaAvoid=True\nrandCaAvoidP=20\nrandCaAvoidMax=25\nrandCaCrit=False\nrandCaCritP=35\n"
            + "randCaCritMax=40\nrandCaEffects=True\nrandCaEffectsP=75\nrandCaEffectiveness=True\n"
            + "randCaEffectivenessP=20\nrandCaRange=True\nrandCaRangeP=10\ncaRangeBinomial=True\n"
            + "caRangeLinear=False\ncaRangeLinearDev=2\ncaRangeProportional=False\ncaRangeProportionalDev=1\n"
            + "caRangeExponential=False\ncaRangeExponentialDev=2\nrandCaDurabilityCost=True\n"
            + "randCaDurabilityCostP=50\ncaDurabilityCostBinomial=False\ncaDurabilityCostLinear=False\n"
            + "caDurabilityCostLinearDev=2\ncaDurabilityCostProportional=False\n"
            + "caDurabilityCostProportionalDev=0.5\ncaDurabilityCostExponential=False\n"
            + "caDurabilityCostExponentialDev=2\nrandCaMight=True\nrandCaMightP=50\ncaMightBinomial=False\n"
            + "caMightLinear=False\ncaMightLinearDev=5\ncaMightProportional=False\ncaMightProportionalDev=0.5\n"
            + "caMightExponential=False\ncaMightExponentialDev=2\nrandCaHit=True\ncaHitMin=-35\ncaHitMax=35\n"
            + "randCaHitP=25\nrandBattalions=True\nrandBattalionEndurance=True\nrandBattalionEnduranceP=80\n"
            + "battalionEnduranceBinomial=False\nbattalionEnduranceLinear=False\n"
            + "battalionEnduranceLinearDev=30\nbattalionEnduranceProportional=False\n"
            + "battalionEnduranceProportionalDev=0.5\nbattalionEnduranceExponential=False\n"
            + "battalionEnduranceExponentialDev=2\nrandBattalionBoostBases=True\n"
            + "randBattalionBoostGrowths=True\nrandBattalionStatBoosts=False\n"
            + "battalionStatBoostsIncludeAll=False\nrandBattalionGambit=True\nshuffleAssets=False\n"
            + "shuffleAssetsGender=False\nshuffleAssetsAge=False\nrandModelData=True\nrandAge=False\nageMin=9\n"
            + "ageMax=73\nrandBirthday=True\nrandAniSet=False\naniSetIncludeCane=False\nrandHeight=True\n"
            + "heightMin=135\nheightMax=204\nmodelSize1Min=0.85\nmodelSize1Max=1.15\nmodelSize2Min=0.85\n"
            + "modelSize2Max=1.16\nmodelPreTsChestSizeMin=0.08\nmodelPreTsChestSizeMax=1.75\n"
            + "modelPostTsChestSizeMin=0.1\nmodelPostTsChestSizeMax=2.45\nrandCrests=True\ncrest1P=60\n"
            + "crest2P=10\nrandPersonalAb=True\nbasePersonalAb=True\nnewPersonalAbPostTsP=5\n"
            + "randSupportBonuses=True\nrandSpecialSupportBonuses=True\nsupportBonusesIncludeMore=False\n"
            + "randRecruitmentRequirements=True\nrecruitmentRequirementStatsMin=8\n"
            + "recruitmentRequirementStatsMax=22\nrecruitmentRequirementRanksMin=3\n"
            + "recruitmentRequirementRanksMax=6\npreserveCharCaps=False\nrandCharCaps=False\n"
            + "removeCharCaps=False\nhpCharCapsMin=80\nhpCharCapsMax=200\nstrCharCapsMin=30\nstrCharCapsMax=90\n"
            + "magCharCapsMin=30\nmagCharCapsMax=90\ndexCharCapsMin=55\ndexCharCapsMax=90\nspeCharCapsMin=35\n"
            + "speCharCapsMax=90\nlucCharCapsMin=0\nlucCharCapsMax=255\ndefCharCapsMin=35\ndefCharCapsMax=70\n"
            + "resCharCapsMin=35\nresCharCapsMax=70\nchaCharCapsMin=35\nchaCharCapsMax=100\nrandCharBases=True\n"
            + "ensureGoodBases=True\nhpCharBasesMin=20\nhpCharBasesMax=35\nstrCharBasesMin=4\n"
            + "strCharBasesMax=13\nmagCharBasesMin=4\nmagCharBasesMax=13\ndexCharBasesMin=6\ndexCharBasesMax=7\n"
            + "speCharBasesMin=5\nspeCharBasesMax=10\nlucCharBasesMin=0\nlucCharBasesMax=11\ndefCharBasesMin=3\n"
            + "defCharBasesMax=8\nresCharBasesMin=1\nresCharBasesMax=10\nchaCharBasesMin=1\nchaCharBasesMax=12\n"
            + "randMovCharBases=False\nrandMovCharBasesP=10\nmovCharBasesMin=3\nmovCharBasesMax=5\n"
            + "randCharGrowths=True\nensureGoodGrowths=True\nhpCharGrowthsMin=50\nhpCharGrowthsMax=100\n"
            + "strCharGrowthsMin=20\nstrCharGrowthsMax=60\nmagCharGrowthsMin=20\nmagCharGrowthsMax=60\n"
            + "dexCharGrowthsMin=30\ndexCharGrowthsMax=60\nspeCharGrowthsMin=15\nspeCharGrowthsMax=70\n"
            + "lucCharGrowthsMin=0\nlucCharGrowthsMax=100\ndefCharGrowthsMin=20\ndefCharGrowthsMax=50\n"
            + "resCharGrowthsMin=20\nresCharGrowthsMax=50\nchaCharGrowthsMin=10\nchaCharGrowthsMax=65\n"
            + "randMovCharGrowths=False\nrandMovCharGrowthsP=10\nmovCharGrowthsMin=1\nmovCharGrowthsMax=10\n"
            + "randGenericAb=True\nbaseGenericAb=True\npreserveProwessAb=True\nrandGenericCa=True\n"
            + "baseGenericCa=True\nrandCharCa=True\nbaseCharCa=True\nlimitCharCa=True\ncharCaCountMin=2\n"
            + "charCaCountMax=3\nrandBuddingTalents=True\nmultipleBuddingTalents=True\n"
            + "baseBuddingTalents=True\nrandBuddingTalentLessonCount=True\nbuddingTalentLessonCountMin=10\n"
            + "buddingTalentLessonCountMax=14\nrandSpellLists=True\nspellAtD=True\nbaseSpellLists=True\n"
            + "regularSpellListReasonCountMin=3\nregularSpellListReasonCountMax=3\n"
            + "regularSpellListFaithCountMin=2\nregularSpellListFaithCountMax=5\n"
            + "proficientSpellListReasonCountMin=5\nproficientSpellListReasonCountMax=5\n"
            + "proficientSpellListFaithCountMin=5\nproficientSpellListFaithCountMax=5\n"
            + "unknownSpellListReasonCountMin=2\nunknownSpellListReasonCountMax=5\n"
            + "unknownSpellListFaithCountMin=3\nunknownSpellListFaithCountMax=5\nrandCharAb=True\n"
            + "baseCharAb=True\nlimitCharAb=True\ncharAbCountMin=1\ncharAbCountMax=3\n"
            + "preserveGenericCharAb=True\nrandCharProfs=True\nstrengthCountMin=2\nstrengthCountMax=5\n"
            + "weaknessCountMin=1\nweaknessCountMax=4\nrandCharStartClasses=False\n"
            + "randCharPreCertClasses=True\ncharOptimalClasses=True\nrandStartInventories=True\n"
            + "baseStartInventories=True\nrandStartInventoryItem=True\nstartInventoryItemP=100\n"
            + "randFacultyTraining=True\nbaseFacultyTraining=True\nrandSeminars=True\nbaseSeminars=True\n"
            + "randGoals=True\nbaseGoals=True\nrandStartRanks=True\nbaseStartRanks=True\nrandCalendar=False\n"
            + "randRareMonsterSighting=False\nrandRareMonsterSightingP=20\nrandRestrictFreeday=False\n"
            + "randRestrictFreedayP=10\nrandCafeteriaEvent=False\nrandCafeteriaEventP=50\n"
            + "randChoirEvent=False\nrandChoirEventP=10\nrandGardenEvent=False\nrandGardenEventP=10\n"
            + "randFishingEvent=False\nrandFishingEventP=30\nshuffleBgm=True\nchangeMsg=False\nchangeScr=False\n"
            + "changeGwscr=False\nchangeTuscr=False\nchangeBtlscr=False\nchangeData1Text=False\nbaseText=True\n"
            + "qolText=False\nrandText=False\npreserveStringLength=False\nrandTextSource=False\n"
            + "randChoirProfs=False\nrandCookingProfs=False\nrandMeal=False\nrandMealPrefs=False\n"
            + "randFishingItems=False\nrandGardeningItems=False\nrandCultivationCosts=False\n"
            + "randGiftPrefs=False\nrandSaintStatueCosts=False\nrandSaintStatuePerks=False\n"
            + "randInfluencerCosts=False\nrandQuestRewards=False\nrandGroupTaskGold=False\n"
            + "randGroupTaskSkills=False\nrandGroupTaskItems=False\nrandGroupTaskProfs=False\n"
            + "sameFunctionItems=True\nsameTypeItems=False\nrandTypeItems=False\nrandTypeItemP=10\n"
            + "randWeaponShops=False\nrandItemShops=False\nrandBattalionShops=False\nrandMerchants=False\n"
            + "randAnnaShop=True\nrandPaganAltar=False\nrandShopPrices=False\nrandShopStocks=False\n"
            + "randLectureExp=False\nrandProfLvExpReq=False\nrandFunds=False\nrandLecturePointCount=False\n"
            + "randBattlePointCount=False\nrandAdjutantCount=False\nrandActivityPointCount=False\n"
            + "unlockMasterClasses=True\nrandRepairItems=False\nrandForgeCosts=False\nrandForgeItems=False\n"
            + "randForgeWeapons=True\npreserveForgeWeaponTypes=False\nrandForgeProfLvReq=False\n"
            + "randMapClass=True\nincludeMonsterMapClass=False\nmapGroupClass=True\nmapIndividualClass=False\n"
            + "randMapInventory=True\nmapInventoryP=10\nmapInventoryForceCom=True\nrandMapWeapons=True\n"
            + "baseMapWeapons=True\nrandMapEquipment=True\nmapEquipmentP=20\nrandMapItems=True\nmapItemsP=20\n"
            + "randMapDrops=True\nmapDropsP=20\nrandMapAb=False\nmapAbP=10\nmapAbForceCom=False\n"
            + "baseMapAb=False\nrandMapBattalionId=False\nrandMapBattalion=False\nmapBattalionP=10\n"
            + "mapBattalionForceCom=False\nrandMapCa=False\nmapCaP=10\nmapCaForceCom=False\nbaseMapCa=False\n"
            + "randMapSpellId=False\nrandMapSpell=False\nmapSpellP=10\nmapSpellForceCom=False\n"
            + "baseMapSpell=False\nrandChestItem=True\nrandChestItemType=True\nrandChestItemTypeP=50\n"
            + "mapExtraEnemies=True\nmapExtraEnemiesMin=10\nmapExtraEnemiesMax=10\nmapMaxAllies=False\n"
            + "randAllyMapSpawn=False\nrandNpcMapSpawn=False\nrandEnemyMapSpawn=True\nrandComMapSpawn=False\n"
            + "randMapSpawnP=15\nchangeLv=False\nlvMultiplier=150\namplifyMaddening=False\n"
            + "amplifyMaddeningAmount=50\nensureLowLvEarly=False\n"
            + "RandomizerExtras.blockedAbilities=148,154,158,190,191,193,195,197,198,199,201,205,206,207,225,226,240,246,247,249\n"
            + "RandomizerExtras.blockAbilities=True\nRandomizerExtras.limitSiegeTomes=True\n"
            + "RandomizerExtras.maxEnemyWeaponRange=4\nRandomizerExtras.siegeChance=10\n"
            + "RandomizerExtras.growthShift=10\nRandomizerExtras.evenClassVariety=True\n"
            + "RandomizerExtras.classVarietySafety=25\nRandomizerMore.protectBossAbilities=True\n"
            + "RandomizerMore.protectBossStats=True\nRandomizerMore.bossScalingShare=75\n"
            + "RandomizerMore.uniqueClassAbilities=True\nRandomizerMore.uniqueTierMastery=True\n"
            + "RandomizerMore.uniqueTierMasteryArt=True\nRandomizerMore.uniquePersonalAbilities=True\n"
            + "RandomizerMore.clampGrowths=True\nRandomizerMore.growthClampMin=10\n"
            + "RandomizerMore.growthClampMax=70\nRandomizerMore.evenGrowthTotals=False\n"
            + "RandomizerMore.growthTotalTolerance=40\nRandomizerMore.protectGrowthIdentity=True\n"
            + "RandomizerMore.clampStatBoosts=True\nRandomizerMore.clampStatBases=True\n"
            + "RandomizerMore.evenStatTotals=False\nRandomizerBig.swapClassStatProfiles=False\n"
            + "RandomizerBig.swapClassGrowthProfiles=False\nRandomizerBig.shuffleCharacterSheets=False\n"
            + "RandomizerBig.bossRemix=False\nRandomizerBig.weaponGraftChance=40\n"
            + "RandomizerBig.weaponFromArtChance=10\nRandomizerBig.weaponFromGambitChance=10\n"
            + "RandomizerBig.artGraftChance=10\nRandomizerBig.artFromWeaponChance=5\n"
            + "RandomizerBig.gambitGraftChance=30\nRandomizerBig.gambitFromWeaponChance=5\n"
            + "RandomizerBig.uniqueWeaponEffects=True\nRandomizerBig.weaponDonorsDifferInType=False\n"
            + "RandomizerBig.swapScope=0\nRandomizerMore.statBoostClampMin=-3\n"
            + "RandomizerMore.statBoostClampMax=10\nRandomizerMore.statBaseClampMin=3\n"
            + "RandomizerMore.statBaseClampMax=90\nRandomizerMore.statTotalTolerance=25\n"
            + "RandomizerExtras.classIdentity=True\nRandomizerExtras.classDefenseClampEarly=50\n"
            + "RandomizerExtras.classDefenseClampMid=50\nRandomizerExtras.classDefenseClampAdv=50\n"
            + "RandomizerExtras.classDefenseClampMaster=50\n"
            + "RandomizerExtras.flatHpTrim=0\nRandomizerExtras.flatDefTrim=0\nRandomizerExtras.flatResTrim=0\n"
            + "RandomizerBig.describeWeaponEffects=True\nspellFirstRank=4\n";

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
        private const int modFlatHpTrim = 6;
        private const int modFlatDefTrim = 2;
        private const int modFlatResTrim = 2;

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

        private static void appendModBulkLines(StringBuilder text)
        {
            text.AppendLine("RandomizerExtras.growthShift=" + modEnemyGrowthShift.ToString());
            text.AppendLine("RandomizerExtras.classIdentity=True");
            text.AppendLine("RandomizerExtras.classDefenseClampEarly=" + modClassDefenseEarly.ToString());
            text.AppendLine("RandomizerExtras.classDefenseClampMid=" + modClassDefenseMid.ToString());
            text.AppendLine("RandomizerExtras.classDefenseClampAdv=" + modClassDefenseAdv.ToString());
            text.AppendLine("RandomizerExtras.classDefenseClampMaster=" + modClassDefenseMaster.ToString());
            text.AppendLine("RandomizerBig.describeWeaponEffects=True");
            text.AppendLine("RandomizerExtras.flatHpTrim=" + modFlatHpTrim.ToString());
            text.AppendLine("RandomizerExtras.flatDefTrim=" + modFlatDefTrim.ToString());
            text.AppendLine("RandomizerExtras.flatResTrim=" + modFlatResTrim.ToString());
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
                + " values: his own preset file of 2026-10-24 plus the range limits it was missing";
            builtIn.Data = builtInRunData;
            builtIn.BuiltIn = true;
            cache.Add(builtIn);

            Preset modSafe = new Preset();
            modSafe.Name = builtInModSafeName;
            modSafe.Description = "No Monastery 1.3.3, the data-file release: the same run with everything"
                + " the mod ships handed back to it (" + ModSupport.conflictCount(ModSupport.kindDataMod).ToString()
                + " options off), first spell at C, 30% of the enemy growth shifted into HP,"
                + " -6 HP / -2 Def / -2 Res on every class base and on every boss";
            modSafe.Data = builtInModSafeData();
            modSafe.BuiltIn = true;
            cache.Add(modSafe);

            Preset pluginSafe = new Preset();
            pluginSafe.Name = builtInPluginSafeName;
            pluginSafe.Description = "No Monastery 2.1, the Skyline plugin release: the same run with only"
                + " the " + ModSupport.conflictCount(ModSupport.kindPluginMod).ToString()
                + " options the plugin recalculates turned off, first spell at C,"
                + " 30% of the enemy growth shifted into HP, -6 HP / -2 Def / -2 Res on every class base"
                + " and on every boss";
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
