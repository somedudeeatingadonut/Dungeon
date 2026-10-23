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
//  The built in preset is the configuration of the 1.8.1 stress run, taken from
//  its debug report, so that exact run can be repeated.
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

        private const string builtInStressName = "Everything on - the 1.8.1 test run";

        // Values that differ from the tool's defaults, taken from the debug report
        // of the 1.8.1 run with every option on: 241 Settings fields and 41 pass
        // options. Applying it starts from the defaults, so this is the whole
        // configuration of that run.
        private static readonly string builtInStressData =
            "randStatBoosts=True\nrandMovStatBoosts=True\nrandMountBoosts=True\nrandMovMountBoosts=True\nrandPlayerGrowthBoosts=True\nrandMovPlayerGrowthBoosts=True\nrandEnemyGrowthBoosts=True\nrandMovEnemyGrowthBoosts=True\nrandClassBases=True\nlucUniClassBasesMax=19\nremoveWeaponRestrictions=True\nrandWeaponTypes=True\nrandTyping=True\nrandMovTypes=True\nbaseMovTypes=True\nrandClassProf=True\nbaseClassProf=True\nrandCert=True\nbaseCert=True\nrandCertItem=True\nallowNonSealCertItems=True\nrandClassAb=True\nbaseClassAb=True\nrandClassDefAb=True\nrandClassClassAb=True\nrandClassMasteryAb=True\nrandClassMasteryCa=True\nbaseClassMasteryCa=True\nrandClassMasteryExpReq=True\nrandAdjutant=True\nspecialToAdv=True\nrandExpYield=True\nunitsToScale=True\nrandMounts=True\nsetArmorAni=True\nprovideClassAni=True\nrandClassAni=True\nrandTierLv=True\nremoveGenderRestrictions=True\nrandBarrierBreakItem=True\nallowNonMineralBarrierBreakItem=True\nrandBarrierAb=True\nrandLatentAb=True\nrandWeapons=True\nrandSpells=True\n"
            + "randTurrets=True\nrandGambits=True\nrandMonsterAoe=True\nrandAtkStatusEffects=True\nrandAtkEffectiveness=True\natkEffectivenessPreserveBows=True\nrandAtkCrit=True\nrandAtkDoubleAttack=True\natkDoubleAttackPreserveGauntlets=True\nrandAtkRange=True\nrandAtkDurability=True\nrandAtkMight=True\nrandAtkHit=True\nrandAtkWeight=True\nrandAtkPhysicalMagicSwitch=True\nrandAtkIgnoreDefRes=True\nrandAtkLeaveAt1=True\nrandCa=True\nrandCaAvoid=True\nrandCaCrit=True\nrandCaEffects=True\nrandCaEffectiveness=True\nrandCaRange=True\nrandCaDurabilityCost=True\nrandCaMight=True\nrandCaHit=True\nrandBattalions=True\nrandBattalionEndurance=True\nrandBattalionBoostBases=True\nrandBattalionBoostGrowths=True\nrandBattalionStatBoosts=True\nbattalionStatBoostsIncludeAll=True\nrandBattalionGambit=True\nshuffleAssets=True\nshuffleAssetsGender=True\nshuffleAssetsAge=True\nrandModelData=True\nrandAge=True\nrandBirthday=True\nrandAniSet=True\naniSetIncludeCane=True\nrandHeight=True\nrandCrests=True\nrandPersonalAb=True\nbasePersonalAb=True\n"
            + "randSupportBonuses=True\nrandSpecialSupportBonuses=True\nsupportBonusesIncludeMore=True\nrandRecruitmentRequirements=True\npreserveCharCaps=False\nrandCharCaps=True\nrandCharBases=True\nensureGoodBases=True\nrandMovCharBases=True\nrandCharGrowths=True\nensureGoodGrowths=True\nrandMovCharGrowths=True\nrandGenericAb=True\nbaseGenericAb=True\npreserveProwessAb=True\nrandGenericCa=True\nbaseGenericCa=True\nrandCharCa=True\nbaseCharCa=True\nlimitCharCa=True\nrandBuddingTalents=True\nmultipleBuddingTalents=True\nbaseBuddingTalents=True\nrandBuddingTalentLessonCount=True\nrandSpellLists=True\nspellAtD=True\nbaseSpellLists=True\nrandCharAb=True\nbaseCharAb=True\nlimitCharAb=True\npreserveGenericCharAb=True\nrandCharProfs=True\nrandCharStartClasses=True\nrandCharPreCertClasses=True\ncharOptimalClasses=True\nrandStartInventories=True\nbaseStartInventories=True\nrandStartInventoryItem=True\nrandFacultyTraining=True\nbaseFacultyTraining=True\nrandSeminars=True\nbaseSeminars=True\nrandGoals=True\nbaseGoals=True\nrandStartRanks=True\n"
            + "baseStartRanks=True\nrandCalendar=True\nrandRareMonsterSighting=True\nrandRestrictFreeday=True\nrandCafeteriaEvent=True\nrandChoirEvent=True\nrandGardenEvent=True\nrandFishingEvent=True\nshuffleBgm=True\nchangeMsg=True\nchangeScr=True\nchangeGwscr=True\nchangeTuscr=True\nchangeBtlscr=True\nchangeData1Text=True\nqolText=True\nrandText=True\npreserveStringLength=True\nrandTextSource=True\nrandChoirProfs=True\nrandCookingProfs=True\nrandMeal=True\nrandMealPrefs=True\nrandFishingItems=True\nrandGardeningItems=True\nrandCultivationCosts=True\nrandGiftPrefs=True\nrandSaintStatueCosts=True\nrandSaintStatuePerks=True\nrandInfluencerCosts=True\nrandQuestRewards=True\nrandGroupTaskGold=True\nrandGroupTaskSkills=True\nrandGroupTaskItems=True\nrandGroupTaskProfs=True\nsameTypeItems=True\nrandTypeItemP=10\nrandWeaponShops=True\nrandItemShops=True\nrandBattalionShops=True\nrandMerchants=True\nrandAnnaShop=True\nrandPaganAltar=True\nrandShopPrices=True\nrandShopStocks=True\n"
            + "randLectureExp=True\nrandProfLvExpReq=True\nrandFunds=True\nrandLecturePointCount=True\nrandBattlePointCount=True\nrandAdjutantCount=True\nrandActivityPointCount=True\nunlockMasterClasses=True\nrandRepairItems=True\nrandForgeCosts=True\nrandForgeItems=True\nrandForgeWeapons=True\npreserveForgeWeaponTypes=True\nrandForgeProfLvReq=True\nrandMapClass=True\nincludeMonsterMapClass=True\nmapIndividualClass=True\nrandMapInventory=True\nmapInventoryP=10\nmapInventoryForceCom=True\nrandMapWeapons=True\nbaseMapWeapons=True\nrandMapEquipment=True\nmapEquipmentP=20\nrandMapItems=True\nmapItemsP=20\nrandMapDrops=True\nmapDropsP=20\nrandMapAb=True\nmapAbP=10\nmapAbForceCom=True\nbaseMapAb=True\nrandMapBattalionId=True\nrandMapBattalion=True\nmapBattalionP=10\nmapBattalionForceCom=True\nrandMapCa=True\nmapCaP=10\nmapCaForceCom=True\nbaseMapCa=True\nrandMapSpellId=True\nrandMapSpell=True\nmapSpellP=10\nmapSpellForceCom=True\nbaseMapSpell=True\n"
            + "randChestItem=True\nrandChestItemType=True\nrandChestItemTypeP=10\nmapExtraEnemies=True\nmapExtraEnemiesMax=20\nmapMaxAllies=True\nrandAllyMapSpawn=True\nrandNpcMapSpawn=True\nrandEnemyMapSpawn=True\nrandComMapSpawn=True\nrandMapSpawnP=10\nchangeLv=True\nlvMultiplier=150\namplifyMaddening=True\namplifyMaddeningAmount=50\nensureLowLvEarly=True\nRandomizerExtras.evenClassVariety=True\nRandomizerExtras.limitSiegeTomes=True\nRandomizerExtras.blockAbilities=True\nRandomizerExtras.maxEnemyWeaponRange=2\nRandomizerExtras.siegeChance=5\nRandomizerExtras.growthShift=0\nRandomizerMore.protectBossAbilities=True\nRandomizerMore.protectBossStats=True\nRandomizerMore.bossScalingShare=25\nRandomizerMore.uniqueClassAbilities=True\nRandomizerMore.uniqueTierMastery=True\nRandomizerMore.uniqueTierMasteryArt=True\nRandomizerMore.uniquePersonalAbilities=True\nRandomizerMore.evenGrowthTotals=True\nRandomizerMore.growthTotalTolerance=15\nRandomizerMore.protectGrowthIdentity=True\nRandomizerMore.evenStatTotals=True\nRandomizerMore.statTotalTolerance=90\nRandomizerMore.clampGrowths=True\nRandomizerMore.growthClampMin=10\nRandomizerMore.growthClampMax=70\nRandomizerMore.clampStatBoosts=True\nRandomizerMore.statBoostClampMin=-3\nRandomizerMore.statBoostClampMax=10\nRandomizerMore.clampStatBases=True\nRandomizerMore.statBaseClampMin=3\nRandomizerMore.statBaseClampMax=30\nRandomizerBig.swapClassStatProfiles=True\nRandomizerBig.swapClassGrowthProfiles=True\n"
            + "RandomizerBig.shuffleCharacterSheets=True\nRandomizerBig.swapScope=1\nRandomizerBig.bossRemix=True\nRandomizerBig.weaponGraftChance=50\nRandomizerBig.weaponFromArtChance=25\nRandomizerBig.weaponFromGambitChance=25\nRandomizerBig.artGraftChance=25\nRandomizerBig.artFromWeaponChance=35\nRandomizerBig.gambitGraftChance=25\nRandomizerBig.gambitFromWeaponChance=25\nRandomizerBig.uniqueWeaponEffects=True\nRandomizerBig.weaponDonorsDifferInType=True\n";

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
            builtIn.Name = builtInStressName;
            builtIn.Description = "Every option on, exactly as the 1.8.1 debug report recorded it";
            builtIn.Data = builtInStressData;
            builtIn.BuiltIn = true;
            cache.Add(builtIn);

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
            { typeof(RandomizerExtras), typeof(RandomizerMore), typeof(RandomizerBig) };

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

        // The preset that is currently applied, so the tab can show it.
        public static string lastApplied = "";

        public static string describe(Preset preset)
        {
            if (preset == null)
                return "";
            return preset.Name + " - " + preset.Description;
        }
    }
}
