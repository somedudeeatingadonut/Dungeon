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
//  The built in preset is the configuration of the run in the 1.8.2 debug
//  report that ships next to the exe, so that exact run can be repeated. The
//  mod safe one is the same run with every option the mod owns turned off.
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
        //  The built in preset is the configuration of the run recorded in the
        //  debug report that ships next to the exe (1.8.2): its full settings
        //  dump, value for value, plus the extra options read out of the same
        //  report's NEW / MORE / BIG sections. Nothing here was retyped from a
        //  conversation - apply it and you get that run again.
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
            + "uniMovStatBoostsMax=2\nrandMountBoosts=True\nrandMonMountBoosts=False\nrandStaMountBoosts=False\n"
            + "randBegMountBoosts=False\nrandIntMountBoosts=False\nrandAdvMountBoosts=False\n"
            + "randMasMountBoosts=True\nrandUniMountBoosts=False\nrandMovMountBoosts=False\n"
            + "monMountBoostsPosCountMin=0\nmonMountBoostsPosCountMax=0\nstaMountBoostsPosCountMin=0\n"
            + "staMountBoostsPosCountMax=0\nbegMountBoostsPosCountMin=0\nbegMountBoostsPosCountMax=0\n"
            + "intMountBoostsPosCountMin=0\nintMountBoostsPosCountMax=1\nadvMountBoostsPosCountMin=0\n"
            + "advMountBoostsPosCountMax=2\nmasMountBoostsPosCountMin=0\nmasMountBoostsPosCountMax=2\n"
            + "uniMountBoostsPosCountMin=1\nuniMountBoostsPosCountMax=2\nmonMountBoostsNegCountMin=0\n"
            + "monMountBoostsNegCountMax=0\nstaMountBoostsNegCountMin=0\nstaMountBoostsNegCountMax=0\n"
            + "begMountBoostsNegCountMin=0\nbegMountBoostsNegCountMax=0\nintMountBoostsNegCountMin=0\n"
            + "intMountBoostsNegCountMax=1\nadvMountBoostsNegCountMin=0\nadvMountBoostsNegCountMax=1\n"
            + "masMountBoostsNegCountMin=0\nmasMountBoostsNegCountMax=0\nuniMountBoostsNegCountMin=0\n"
            + "uniMountBoostsNegCountMax=1\nmonMountBoostsPosMin=0\nmonMountBoostsPosMax=0\n"
            + "staMountBoostsPosMin=0\nstaMountBoostsPosMax=0\nbegMountBoostsPosMin=0\nbegMountBoostsPosMax=0\n"
            + "intMountBoostsPosMin=2\nintMountBoostsPosMax=3\nadvMountBoostsPosMin=1\nadvMountBoostsPosMax=2\n"
            + "masMountBoostsPosMin=1\nmasMountBoostsPosMax=3\nuniMountBoostsPosMin=1\nuniMountBoostsPosMax=3\n"
            + "monMountBoostsNegMin=0\nmonMountBoostsNegMax=0\nstaMountBoostsNegMin=0\nstaMountBoostsNegMax=0\n"
            + "begMountBoostsNegMin=0\nbegMountBoostsNegMax=0\nintMountBoostsNegMin=-2\nintMountBoostsNegMax=-2\n"
            + "advMountBoostsNegMin=-2\nadvMountBoostsNegMax=-2\nmasMountBoostsNegMin=0\nmasMountBoostsNegMax=0\n"
            + "uniMountBoostsNegMin=-2\nuniMountBoostsNegMax=-2\nmonMovMountBoostsMin=0\nmonMovMountBoostsMax=0\n"
            + "staMovMountBoostsMin=0\nstaMovMountBoostsMax=0\nbegMovMountBoostsMin=0\nbegMovMountBoostsMax=0\n"
            + "intMovMountBoostsMin=2\nintMovMountBoostsMax=3\nadvMovMountBoostsMin=2\nadvMovMountBoostsMax=2\n"
            + "masMovMountBoostsMin=1\nmasMovMountBoostsMax=3\nuniMovMountBoostsMin=2\nuniMovMountBoostsMax=3\n"
            + "randPlayerGrowthBoosts=True\nrandMonPlayerGrowthBoosts=False\nrandStaPlayerGrowthBoosts=True\n"
            + "randBegPlayerGrowthBoosts=True\nrandIntPlayerGrowthBoosts=True\nrandAdvPlayerGrowthBoosts=True\n"
            + "randMasPlayerGrowthBoosts=True\nrandUniPlayerGrowthBoosts=True\nrandMovPlayerGrowthBoosts=False\n"
            + "monPlayerGrowthBoostsPosCountMin=0\nmonPlayerGrowthBoostsPosCountMax=0\n"
            + "staPlayerGrowthBoostsPosCountMin=0\nstaPlayerGrowthBoostsPosCountMax=1\n"
            + "begPlayerGrowthBoostsPosCountMin=3\nbegPlayerGrowthBoostsPosCountMax=3\n"
            + "intPlayerGrowthBoostsPosCountMin=3\nintPlayerGrowthBoostsPosCountMax=5\n"
            + "advPlayerGrowthBoostsPosCountMin=3\nadvPlayerGrowthBoostsPosCountMax=7\n"
            + "masPlayerGrowthBoostsPosCountMin=2\nmasPlayerGrowthBoostsPosCountMax=6\n"
            + "uniPlayerGrowthBoostsPosCountMin=2\nuniPlayerGrowthBoostsPosCountMax=6\n"
            + "monPlayerGrowthBoostsNegCountMin=0\nmonPlayerGrowthBoostsNegCountMax=0\n"
            + "staPlayerGrowthBoostsNegCountMin=0\nstaPlayerGrowthBoostsNegCountMax=0\n"
            + "begPlayerGrowthBoostsNegCountMin=0\nbegPlayerGrowthBoostsNegCountMax=1\n"
            + "intPlayerGrowthBoostsNegCountMin=0\nintPlayerGrowthBoostsNegCountMax=2\n"
            + "advPlayerGrowthBoostsNegCountMin=0\nadvPlayerGrowthBoostsNegCountMax=2\n"
            + "masPlayerGrowthBoostsNegCountMin=0\nmasPlayerGrowthBoostsNegCountMax=2\n"
            + "uniPlayerGrowthBoostsNegCountMin=0\nuniPlayerGrowthBoostsNegCountMax=3\n"
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
            + "randMonClassBases=False\nrandStaClassBases=True\nrandBegClassBases=True\nrandIntClassBases=True\n"
            + "randAdvClassBases=True\nrandMasClassBases=True\nrandUniClassBases=True\nhpMonClassBasesMin=0\n"
            + "hpMonClassBasesMax=0\nhpStaClassBasesMin=1\nhpStaClassBasesMax=1\nhpBegClassBasesMin=20\n"
            + "hpBegClassBasesMax=20\nhpIntClassBasesMin=25\nhpIntClassBasesMax=30\nhpAdvClassBasesMin=20\n"
            + "hpAdvClassBasesMax=35\nhpMasClassBasesMin=32\nhpMasClassBasesMax=34\nhpUniClassBasesMin=28\n"
            + "hpUniClassBasesMax=38\nstrMonClassBasesMin=0\nstrMonClassBasesMax=0\nstrStaClassBasesMin=0\n"
            + "strStaClassBasesMax=0\nstrBegClassBasesMin=4\nstrBegClassBasesMax=8\nstrIntClassBasesMin=4\n"
            + "strIntClassBasesMax=10\nstrAdvClassBasesMin=5\nstrAdvClassBasesMax=20\nstrMasClassBasesMin=8\n"
            + "strMasClassBasesMax=20\nstrUniClassBasesMin=8\nstrUniClassBasesMax=18\nmagMonClassBasesMin=0\n"
            + "magMonClassBasesMax=0\nmagStaClassBasesMin=0\nmagStaClassBasesMax=0\nmagBegClassBasesMin=3\n"
            + "magBegClassBasesMax=8\nmagIntClassBasesMin=6\nmagIntClassBasesMax=10\nmagAdvClassBasesMin=6\n"
            + "magAdvClassBasesMax=17\nmagMasClassBasesMin=10\nmagMasClassBasesMax=17\nmagUniClassBasesMin=8\n"
            + "magUniClassBasesMax=17\ndexMonClassBasesMin=0\ndexMonClassBasesMax=0\ndexStaClassBasesMin=0\n"
            + "dexStaClassBasesMax=1\ndexBegClassBasesMin=6\ndexBegClassBasesMax=6\ndexIntClassBasesMin=7\n"
            + "dexIntClassBasesMax=11\ndexAdvClassBasesMin=7\ndexAdvClassBasesMax=12\ndexMasClassBasesMin=14\n"
            + "dexMasClassBasesMax=14\ndexUniClassBasesMin=11\ndexUniClassBasesMax=14\nspeMonClassBasesMin=0\n"
            + "speMonClassBasesMax=0\nspeStaClassBasesMin=0\nspeStaClassBasesMax=0\nspeBegClassBasesMin=6\n"
            + "speBegClassBasesMax=6\nspeIntClassBasesMin=4\nspeIntClassBasesMax=11\nspeAdvClassBasesMin=8\n"
            + "speAdvClassBasesMax=14\nspeMasClassBasesMin=10\nspeMasClassBasesMax=20\nspeUniClassBasesMin=6\n"
            + "speUniClassBasesMax=20\nlucMonClassBasesMin=0\nlucMonClassBasesMax=0\nlucStaClassBasesMin=0\n"
            + "lucStaClassBasesMax=0\nlucBegClassBasesMin=4\nlucBegClassBasesMax=4\nlucIntClassBasesMin=8\n"
            + "lucIntClassBasesMax=8\nlucAdvClassBasesMin=8\nlucAdvClassBasesMax=12\nlucMasClassBasesMin=12\n"
            + "lucMasClassBasesMax=12\nlucUniClassBasesMin=19\nlucUniClassBasesMax=19\ndefMonClassBasesMin=0\n"
            + "defMonClassBasesMax=0\ndefStaClassBasesMin=0\ndefStaClassBasesMax=0\ndefBegClassBasesMin=3\n"
            + "defBegClassBasesMax=5\ndefIntClassBasesMin=3\ndefIntClassBasesMax=12\ndefAdvClassBasesMin=5\n"
            + "defAdvClassBasesMax=17\ndefMasClassBasesMin=13\ndefMasClassBasesMax=17\ndefUniClassBasesMin=13\n"
            + "defUniClassBasesMax=17\nresMonClassBasesMin=0\nresMonClassBasesMax=0\nresStaClassBasesMin=0\n"
            + "resStaClassBasesMax=0\nresBegClassBasesMin=2\nresBegClassBasesMax=4\nresIntClassBasesMin=1\n"
            + "resIntClassBasesMax=5\nresAdvClassBasesMin=4\nresAdvClassBasesMax=15\nresMasClassBasesMin=10\n"
            + "resMasClassBasesMax=16\nresUniClassBasesMin=8\nresUniClassBasesMax=16\nchaMonClassBasesMin=0\n"
            + "chaMonClassBasesMax=0\nchaStaClassBasesMin=0\nchaStaClassBasesMax=0\nchaBegClassBasesMin=4\n"
            + "chaBegClassBasesMax=5\nchaIntClassBasesMin=5\nchaIntClassBasesMax=9\nchaAdvClassBasesMin=6\n"
            + "chaAdvClassBasesMax=14\nchaMasClassBasesMin=12\nchaMasClassBasesMax=16\nchaUniClassBasesMin=9\n"
            + "chaUniClassBasesMax=16\nremoveWeaponRestrictions=True\nrandWeaponTypes=True\nmon2ndWeaponTypeP=0\n"
            + "sta2ndWeaponTypeP=0\nbeg2ndWeaponTypeP=5\nint2ndWeaponTypeP=15\nadv2ndWeaponTypeP=35\n"
            + "mas2ndWeaponTypeP=75\nuni2ndWeaponTypeP=50\nrandTyping=True\nrandMovTypes=False\n"
            + "baseMovTypes=False\nrandClassProf=True\nbaseClassProf=True\nrandCert=True\nrandMonCert=True\n"
            + "randStaCert=True\nrandBegCert=True\nrandIntCert=True\nrandAdvCert=True\nrandMasCert=True\n"
            + "randUniCert=True\nmainMonCertMin=0\nmainMonCertMax=0\nmainStaCertMin=0\nmainStaCertMax=0\n"
            + "mainBegCertMin=2\nmainBegCertMax=2\nmainIntCertMin=4\nmainIntCertMax=4\nmainAdvCertMin=6\n"
            + "mainAdvCertMax=8\nmainMasCertMin=7\nmainMasCertMax=8\nmainUniCertMin=0\nmainUniCertMax=0\n"
            + "typeMonCertMin=0\ntypeMonCertMax=0\ntypeStaCertMin=0\ntypeStaCertMax=0\ntypeBegCertMin=0\n"
            + "typeBegCertMax=0\ntypeIntCertMin=1\ntypeIntCertMax=2\ntypeAdvCertMin=4\ntypeAdvCertMax=6\n"
            + "typeMasCertMin=7\ntypeMasCertMax=9\ntypeUniCertMin=0\ntypeUniCertMax=0\notherMonCertMin=0\n"
            + "otherMonCertMax=0\notherStaCertMin=0\notherStaCertMax=0\notherBegCertMin=0\notherBegCertMax=2\n"
            + "otherIntCertMin=0\notherIntCertMax=2\notherAdvCertMin=4\notherAdvCertMax=4\notherMasCertMin=4\n"
            + "otherMasCertMax=6\notherUniCertMin=0\notherUniCertMax=0\nbaseCert=True\nrandCertItem=False\n"
            + "allowNonSealCertItems=False\nrandClassAb=True\nbaseClassAb=True\nrandClassDefAb=True\n"
            + "randMonClassDefAb=True\nrandStaClassDefAb=True\nrandBegClassDefAb=True\nrandIntClassDefAb=True\n"
            + "randAdvClassDefAb=True\nrandMasClassDefAb=True\nrandUniClassDefAb=True\nmonClassDefAbCountMin=0\n"
            + "monClassDefAbCountMax=0\nstaClassDefAbCountMin=0\nstaClassDefAbCountMax=0\nintClassDefAbCountMin=2\n"
            + "intClassDefAbCountMax=2\nbegClassDefAbCountMin=0\nbegClassDefAbCountMax=1\nadvClassDefAbCountMin=3\n"
            + "advClassDefAbCountMax=3\nmasClassDefAbCountMin=4\nmasClassDefAbCountMax=4\nuniClassDefAbCountMin=5\n"
            + "uniClassDefAbCountMax=5\nrandClassClassAb=True\nrandMonClassClassAb=True\nrandStaClassClassAb=True\n"
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
            + "uniClassMasteryExpReqMin=100\nuniClassMasteryExpReqMax=200\nrandAdjutant=True\nspecialToAdv=True\n"
            + "randExpYield=False\nrandMonExpYield=True\nrandStaExpYield=True\nrandBegExpYield=True\n"
            + "randIntExpYield=True\nrandAdvExpYield=True\nrandMasExpYield=True\nrandUniExpYield=True\n"
            + "monExpYieldMin=25\nmonExpYieldMax=75\nstaExpYieldMin=15\nstaExpYieldMax=45\nbegExpYieldMin=15\n"
            + "begExpYieldMax=45\nintExpYieldMin=18\nintExpYieldMax=54\nadvExpYieldMin=20\nadvExpYieldMax=60\n"
            + "masExpYieldMin=20\nmasExpYieldMax=60\nuniExpYieldMin=25\nuniExpYieldMax=75\nunitsToScale=True\n"
            + "randMounts=True\nsetArmorAni=True\nprovideClassAni=True\nrandClassAni=False\nrandTierLv=False\n"
            + "randBegTierLv=True\nrandIntTierLv=True\nrandAdvTierLv=True\nrandMasTierLv=True\nbegTierLvMin=4\n"
            + "begTierLvMax=6\nintTierLvMin=8\nintTierLvMax=12\nadvTierLvMin=16\nadvTierLvMax=24\nmasTierLvMin=24\n"
            + "masTierLvMax=36\nremoveGenderRestrictions=True\nrandBarrierBreakItem=False\n"
            + "allowNonMineralBarrierBreakItem=False\nrandBarrierAb=False\nrandLatentAb=True\nrandWeapons=True\n"
            + "randSpells=True\nrandTurrets=False\nrandGambits=True\nrandMonsterAoe=True\n"
            + "randAtkStatusEffects=True\natkStatusEffectsP=10\nrandAtkEffectiveness=True\natkEffectivenessP=10\n"
            + "atkEffectivenessPreserveBows=False\nrandAtkCrit=False\natkCritP=30\natkCritMax=50\n"
            + "randAtkDoubleAttack=False\natkDoubleAttackP=5\natkDoubleAttackPreserveGauntlets=True\n"
            + "randAtkRange=True\nrandAtkRangeP=10\natkRangeBinomial=True\natkRangeLinear=False\n"
            + "atkRangeLinearDev=2\natkRangeProportional=False\natkRangeProportionalDev=1\n"
            + "atkRangeExponential=False\natkRangeExponentialDev=2\nrandAtkDurability=True\nrandAtkDurabilityP=50\n"
            + "atkDurabilityBinomial=False\natkDurabilityLinear=False\natkDurabilityLinearDev=10\n"
            + "atkDurabilityProportional=True\natkDurabilityProportionalDev=0.5\natkDurabilityExponential=False\n"
            + "atkDurabilityExponentialDev=2\nrandAtkMight=True\nrandAtkMightP=50\natkMightBinomial=False\n"
            + "atkMightLinear=False\natkMightLinearDev=5\natkMightProportional=True\natkMightProportionalDev=0.5\n"
            + "atkMightExponential=False\natkMightExponentialDev=2\nrandAtkHit=True\nrandAtkHitP=50\n"
            + "atkHitBinomial=False\natkHitLinear=False\natkHitLinearDev=25\natkHitProportional=True\n"
            + "atkHitProportionalDev=0.25\natkHitExponential=False\natkHitExponentialDev=1.5\nrandAtkWeight=True\n"
            + "randAtkWeightP=50\natkWeightBinomial=True\natkWeightLinear=False\natkWeightLinearDev=6\n"
            + "atkWeightProportional=False\natkWeightProportionalDev=0.5\natkWeightExponential=False\n"
            + "atkWeightExponentialDev=2\nrandAtkPhysicalMagicSwitch=True\nrandAtkPhysicalMagicSwitchP=10\n"
            + "randAtkIgnoreDefRes=False\nrandAtkIgnoreDefResP=0.5\nrandAtkLeaveAt1=False\nrandAtkLeaveAt1P=0.5\n"
            + "randCa=True\nrandCaAvoid=True\nrandCaAvoidP=20\nrandCaAvoidMax=25\nrandCaCrit=False\n"
            + "randCaCritP=35\nrandCaCritMax=40\nrandCaEffects=True\nrandCaEffectsP=75\nrandCaEffectiveness=True\n"
            + "randCaEffectivenessP=20\nrandCaRange=True\nrandCaRangeP=10\ncaRangeBinomial=True\n"
            + "caRangeLinear=False\ncaRangeLinearDev=2\ncaRangeProportional=False\ncaRangeProportionalDev=1\n"
            + "caRangeExponential=False\ncaRangeExponentialDev=2\nrandCaDurabilityCost=True\n"
            + "randCaDurabilityCostP=50\ncaDurabilityCostBinomial=False\ncaDurabilityCostLinear=False\n"
            + "caDurabilityCostLinearDev=2\ncaDurabilityCostProportional=True\n"
            + "caDurabilityCostProportionalDev=0.5\ncaDurabilityCostExponential=False\n"
            + "caDurabilityCostExponentialDev=2\nrandCaMight=True\nrandCaMightP=50\ncaMightBinomial=False\n"
            + "caMightLinear=False\ncaMightLinearDev=5\ncaMightProportional=True\ncaMightProportionalDev=0.5\n"
            + "caMightExponential=False\ncaMightExponentialDev=2\nrandCaHit=True\ncaHitMin=-35\ncaHitMax=35\n"
            + "randCaHitP=25\nrandBattalions=True\nrandBattalionEndurance=True\nrandBattalionEnduranceP=80\n"
            + "battalionEnduranceBinomial=False\nbattalionEnduranceLinear=False\nbattalionEnduranceLinearDev=30\n"
            + "battalionEnduranceProportional=True\nbattalionEnduranceProportionalDev=0.5\n"
            + "battalionEnduranceExponential=False\nbattalionEnduranceExponentialDev=2\n"
            + "randBattalionBoostBases=True\nrandBattalionBoostGrowths=True\nrandBattalionStatBoosts=False\n"
            + "battalionStatBoostsIncludeAll=False\nrandBattalionGambit=True\nshuffleAssets=False\n"
            + "shuffleAssetsGender=False\nshuffleAssetsAge=False\nrandModelData=True\nrandAge=False\nageMin=9\n"
            + "ageMax=73\nrandBirthday=True\nrandAniSet=False\naniSetIncludeCane=False\nrandHeight=True\n"
            + "heightMin=135\nheightMax=204\nmodelSize1Min=0.85\nmodelSize1Max=1.15\nmodelSize2Min=0.85\n"
            + "modelSize2Max=1.16\nmodelPreTsChestSizeMin=0.08\nmodelPreTsChestSizeMax=1.75\n"
            + "modelPostTsChestSizeMin=0.1\nmodelPostTsChestSizeMax=2.45\nrandCrests=True\ncrest1P=60\ncrest2P=10\n"
            + "randPersonalAb=True\nbasePersonalAb=True\nnewPersonalAbPostTsP=5\nrandSupportBonuses=True\n"
            + "randSpecialSupportBonuses=True\nsupportBonusesIncludeMore=False\nrandRecruitmentRequirements=True\n"
            + "recruitmentRequirementStatsMin=8\nrecruitmentRequirementStatsMax=22\n"
            + "recruitmentRequirementRanksMin=3\nrecruitmentRequirementRanksMax=6\npreserveCharCaps=False\n"
            + "randCharCaps=True\nremoveCharCaps=False\nhpCharCapsMin=80\nhpCharCapsMax=200\nstrCharCapsMin=30\n"
            + "strCharCapsMax=90\nmagCharCapsMin=30\nmagCharCapsMax=90\ndexCharCapsMin=55\ndexCharCapsMax=90\n"
            + "speCharCapsMin=35\nspeCharCapsMax=90\nlucCharCapsMin=0\nlucCharCapsMax=255\ndefCharCapsMin=35\n"
            + "defCharCapsMax=70\nresCharCapsMin=35\nresCharCapsMax=70\nchaCharCapsMin=35\nchaCharCapsMax=100\n"
            + "randCharBases=True\nensureGoodBases=True\nhpCharBasesMin=20\nhpCharBasesMax=35\nstrCharBasesMin=4\n"
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
            + "charCaCountMax=3\nrandBuddingTalents=True\nmultipleBuddingTalents=True\nbaseBuddingTalents=True\n"
            + "randBuddingTalentLessonCount=True\nbuddingTalentLessonCountMin=10\nbuddingTalentLessonCountMax=14\n"
            + "randSpellLists=True\nspellAtD=True\nbaseSpellLists=True\nregularSpellListReasonCountMin=3\n"
            + "regularSpellListReasonCountMax=3\nregularSpellListFaithCountMin=2\nregularSpellListFaithCountMax=5\n"
            + "proficientSpellListReasonCountMin=5\nproficientSpellListReasonCountMax=5\n"
            + "proficientSpellListFaithCountMin=5\nproficientSpellListFaithCountMax=5\n"
            + "unknownSpellListReasonCountMin=2\nunknownSpellListReasonCountMax=5\n"
            + "unknownSpellListFaithCountMin=3\nunknownSpellListFaithCountMax=5\nrandCharAb=True\nbaseCharAb=True\n"
            + "limitCharAb=True\ncharAbCountMin=1\ncharAbCountMax=3\npreserveGenericCharAb=True\n"
            + "randCharProfs=True\nstrengthCountMin=2\nstrengthCountMax=5\nweaknessCountMin=1\nweaknessCountMax=4\n"
            + "randCharStartClasses=False\nrandCharPreCertClasses=True\ncharOptimalClasses=True\n"
            + "randStartInventories=True\nbaseStartInventories=True\nrandStartInventoryItem=True\n"
            + "startInventoryItemP=100\nrandFacultyTraining=True\nbaseFacultyTraining=True\nrandSeminars=True\n"
            + "baseSeminars=True\nrandGoals=True\nbaseGoals=True\nrandStartRanks=True\nbaseStartRanks=True\n"
            + "randCalendar=False\nrandRareMonsterSighting=False\nrandRareMonsterSightingP=20\n"
            + "randRestrictFreeday=False\nrandRestrictFreedayP=10\nrandCafeteriaEvent=False\n"
            + "randCafeteriaEventP=50\nrandChoirEvent=False\nrandChoirEventP=10\nrandGardenEvent=False\n"
            + "randGardenEventP=10\nrandFishingEvent=False\nrandFishingEventP=30\nshuffleBgm=True\n"
            + "changeMsg=False\nchangeScr=False\nchangeGwscr=False\nchangeTuscr=False\nchangeBtlscr=False\n"
            + "changeData1Text=False\nbaseText=True\nqolText=False\nrandText=False\npreserveStringLength=False\n"
            + "randTextSource=False\nrandChoirProfs=False\nrandCookingProfs=False\nrandMeal=False\n"
            + "randMealPrefs=False\nrandFishingItems=False\nrandGardeningItems=False\nrandCultivationCosts=False\n"
            + "randGiftPrefs=False\nrandSaintStatueCosts=False\nrandSaintStatuePerks=False\n"
            + "randInfluencerCosts=False\nrandQuestRewards=False\nrandGroupTaskGold=False\n"
            + "randGroupTaskSkills=False\nrandGroupTaskItems=False\nrandGroupTaskProfs=False\n"
            + "sameFunctionItems=True\nsameTypeItems=False\nrandTypeItems=False\nrandTypeItemP=10\n"
            + "randWeaponShops=False\nrandItemShops=False\nrandBattalionShops=False\nrandMerchants=False\n"
            + "randAnnaShop=True\nrandPaganAltar=False\nrandShopPrices=False\nrandShopStocks=False\n"
            + "randLectureExp=False\nrandProfLvExpReq=False\nrandFunds=False\nrandLecturePointCount=False\n"
            + "randBattlePointCount=False\nrandAdjutantCount=False\nrandActivityPointCount=False\n"
            + "unlockMasterClasses=True\nrandRepairItems=False\nrandForgeCosts=False\nrandForgeItems=False\n"
            + "randForgeWeapons=True\npreserveForgeWeaponTypes=False\nrandForgeProfLvReq=False\nrandMapClass=True\n"
            + "includeMonsterMapClass=False\nmapGroupClass=True\nmapIndividualClass=False\nrandMapInventory=True\n"
            + "mapInventoryP=10\nmapInventoryForceCom=True\nrandMapWeapons=True\nbaseMapWeapons=True\n"
            + "randMapEquipment=True\nmapEquipmentP=20\nrandMapItems=True\nmapItemsP=20\nrandMapDrops=True\n"
            + "mapDropsP=20\nrandMapAb=False\nmapAbP=10\nmapAbForceCom=False\nbaseMapAb=False\n"
            + "randMapBattalionId=False\nrandMapBattalion=False\nmapBattalionP=10\nmapBattalionForceCom=False\n"
            + "randMapCa=False\nmapCaP=10\nmapCaForceCom=False\nbaseMapCa=False\nrandMapSpellId=False\n"
            + "randMapSpell=False\nmapSpellP=10\nmapSpellForceCom=False\nbaseMapSpell=False\nrandChestItem=True\n"
            + "randChestItemType=True\nrandChestItemTypeP=50\nmapExtraEnemies=True\nmapExtraEnemiesMin=10\n"
            + "mapExtraEnemiesMax=10\nmapMaxAllies=False\nrandAllyMapSpawn=False\nrandNpcMapSpawn=False\n"
            + "randEnemyMapSpawn=True\nrandComMapSpawn=False\nrandMapSpawnP=15\nchangeLv=True\nlvMultiplier=150\n"
            + "amplifyMaddening=False\namplifyMaddeningAmount=50\nensureLowLvEarly=True\n"
            + "RandomizerExtras.evenClassVariety=True\nRandomizerExtras.classVarietySafety=25\n"
            + "RandomizerExtras.blockAbilities=True\n"
            + "RandomizerExtras.blockedAbilities=148,154,158,190,191,193,195,197,198,199,201,205,206,207,225,226,240,246,247,249\n"
            + "RandomizerExtras.limitSiegeTomes=True\nRandomizerExtras.maxEnemyWeaponRange=4\n"
            + "RandomizerExtras.siegeChance=10\nRandomizerExtras.growthShift=10\n"
            + "RandomizerMore.protectBossAbilities=True\nRandomizerMore.protectBossStats=True\n"
            + "RandomizerMore.bossScalingShare=75\nRandomizerMore.uniqueClassAbilities=True\n"
            + "RandomizerMore.uniqueTierMastery=True\nRandomizerMore.uniqueTierMasteryArt=True\n"
            + "RandomizerMore.uniquePersonalAbilities=True\nRandomizerMore.evenGrowthTotals=False\n"
            + "RandomizerMore.growthTotalTolerance=40\nRandomizerMore.protectGrowthIdentity=True\n"
            + "RandomizerMore.evenStatTotals=False\nRandomizerMore.statTotalTolerance=25\n"
            + "RandomizerMore.clampGrowths=True\nRandomizerMore.growthClampMin=15\n"
            + "RandomizerMore.growthClampMax=105\nRandomizerMore.clampStatBoosts=True\n"
            + "RandomizerMore.statBoostClampMin=-4\nRandomizerMore.statBoostClampMax=12\n"
            + "RandomizerMore.clampStatBases=True\nRandomizerMore.statBaseClampMin=4\n"
            + "RandomizerMore.statBaseClampMax=99\nRandomizerBig.swapClassStatProfiles=False\n"
            + "RandomizerBig.swapClassGrowthProfiles=False\nRandomizerBig.shuffleCharacterSheets=False\n"
            + "RandomizerBig.swapScope=0\nRandomizerBig.bossRemix=False\nRandomizerBig.weaponGraftChance=40\n"
            + "RandomizerBig.weaponFromArtChance=10\nRandomizerBig.weaponFromGambitChance=10\n"
            + "RandomizerBig.artGraftChance=40\nRandomizerBig.artFromWeaponChance=10\n"
            + "RandomizerBig.gambitGraftChance=40\nRandomizerBig.gambitFromWeaponChance=10\n"
            + "RandomizerBig.uniqueWeaponEffects=False\nRandomizerBig.weaponDonorsDifferInType=False\n";

        private const string builtInRunName = "My run - the 1.8.2 debug report";

        // Rank 4 is C. No Monastery starts units in intermediate classes, so a
        // spell written at the game's usual D is already known on the first
        // map for every character; C delays it to the rank actually earned.
        private const int firstSpellRankForMod = 4;
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
            // The mod starts units in intermediate classes, so a rank D first
            // spell is earned at level 1 and everybody can cast from the start.
            text.AppendLine("spellFirstRank=" + firstSpellRankForMod.ToString());
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
            text.AppendLine("spellFirstRank=" + firstSpellRankForMod.ToString());
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
                + " values, straight out of the 1.8.2 debug report next to the exe";
            builtIn.Data = builtInRunData;
            builtIn.BuiltIn = true;
            cache.Add(builtIn);

            Preset modSafe = new Preset();
            modSafe.Name = builtInModSafeName;
            modSafe.Description = "No Monastery 1.3.3, the data-file release: the same run with everything"
                + " the mod ships handed back to it (" + ModSupport.conflictCount(ModSupport.kindDataMod).ToString()
                + " options off), first spell at C";
            modSafe.Data = builtInModSafeData();
            modSafe.BuiltIn = true;
            cache.Add(modSafe);

            Preset pluginSafe = new Preset();
            pluginSafe.Name = builtInPluginSafeName;
            pluginSafe.Description = "No Monastery 2.1, the Skyline plugin release: the same run with only"
                + " the " + ModSupport.conflictCount(ModSupport.kindPluginMod).ToString()
                + " options the plugin recalculates turned off, first spell at C";
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

        // Counters (statFoo) and the built in name are not options.
        private static bool skipExtra(string name)
        {
            if (name == null)
                return true;
            if (name.StartsWith("stat", StringComparison.Ordinal))
                return true;
            return false;
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
