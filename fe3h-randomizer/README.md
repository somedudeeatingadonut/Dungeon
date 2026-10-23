# FE3H Randomizer — buildable source with the batch tools built in

This is the source of **Fire Emblem Three Houses Randomizer V2**, with two
features added to the program itself:

1. **Scan folder and load all files** — point it at one folder (your extracted
   romfs dump, for example) and it finds and loads every input file it needs.
   No more clicking twenty "Load …" buttons and hunting for each file.
2. **Randomize and save to output folder** — every randomized file is written
   straight into the `romfs` patch folder structure inside one output folder.
   No more save dialogs, and no more hand-written `.bat` to move the files
   afterwards.

Both live in a new **"Batch: folders"** tab inside the program.

```
fe3h-randomizer/
    Fire Emblem Three Houses Randomizer.sln     open in Visual Studio
    Fire Emblem Three Houses Randomizer.csproj
    build.bat                                   one-click build
    BatchTools.cs                               <- folder scan + output writing (new)
    RandomizerExtras.cs                         <- the four extra options (new)
    RandomizerExtrasTab.cs                      <- "Extra options" tab (new)
    PerformanceTweaks.cs                        <- layout / painting tweaks (new)
    DebugReport.cs                              <- the debug report (new)
    RandomizerForm.cs                           <- original code, 2 tiny edits
    Randomizer.cs, Settings.cs, ...             <- original code, untouched
    Resources\DATA0.bin, INFO0.bin, INFO2.bin   <- data extracted from the exe
    LICENSE                                     GPL-3.0
```

## Build it

1. Install **Visual Studio 2022 Community** (free) with the
   *Desktop development with C#* workload, **or** the .NET SDK 8 plus the
   *.NET Framework 4.7.2 targeting pack*. The project targets .NET Framework
   4.7.2, exactly like the exe you already have.
2. Double click **`build.bat`**.
3. Result: `bin\Release\Fire Emblem Three Houses Randomizer V2.exe`

   `build.bat /install` also copies the result over
   `..\Fire Emblem Three Houses Randomizer.exe` and keeps your current exe as
   `Fire Emblem Three Houses Randomizer.exe.original.bak`.

## Extra options tab

Four more options live on the **"Extra options"** tab. They apply when you run
the randomization, so they combine with every other setting.

### Random classes - even variety (the big one)

Randomizing classes used to hand out whatever the RNG felt like, which often
meant fifteen axe classes and a single mage. With this on, the class picks are
spread over weapon types instead:

* The weapon types the **classes themselves** get are dealt from a balanced
  per-tier bag instead of being rolled independently, which is what used to
  produce fifteen axe classes and one mage in a tier.
* Every tier keeps its own "deck" of weapon types, so a tier cannot end up
  without, say, magic while handing out axes all day.
* The same class is never handed out twice while an unused one is available, so
  the actual classes (and therefore their growths, stats, abilities and combat
  arts) differ as well.
* "Optimal class" mode is filtered the same way: the best fitting class is
  picked *inside* the weapon type that is currently behind.
* After randomizing the class data, the growth, stat, class ability and combat
  art values inside each tier are rotated, so one class no longer hogs every
  high roll and two neighbouring classes do not end up with the same kit. Only
  the values you are already randomizing are touched (growth randomization for
  growths, class ability randomization for abilities, and so on), so vanilla
  kits are never disturbed.

The **Safety (%)** slider sets how strictly this is enforced:

| Safety | Behaviour |
| --- | --- |
| 100% | Every class tier except the starting class gets all weapon types. |
| 60% (default) | Most tiers cover every type, the rest stays balanced without being rigid. |
| 0% | Only one guaranteed option per weapon type across the whole run - swords could end up in a single master class - but never nothing. |

### Abilities that must never be randomized

A tick list of all 252 abilities. Anything ticked is never handed out by the
ability, class ability or learnset randomization. The dragon / monster
abilities - **Ancient Dragonskin**, **Ancient Dragon Wrath**, **Divine Dragon
Horn**, **Giant Wings**, **Dragon Effect Null** and **Monster Effect Null** -
are ticked by default. "Add the abilities the randomizer already avoids" brings
in the tool's own `badAbilities` list as well.

As a last step of every run the randomizer re-reads every class ability slot, the
monster latent/barrier slots and every character learnset, and replaces a blocked
ability that was *moved* into a slot by a later pass (the even-variety ability
spread rotates values around). Placements that were already in the vanilla file -
the monster classes' own abilities, for example - are left alone; the blocklist
only stops the randomizer from introducing them.

Two things are deliberately exempt from the blocklist:

* **Dance (158) always goes to the Dancer class.** Writing 158 there is the only
  way to get Dance, and that is the point of the class, so blocking it does not
  remove it. The variety spread will not copy it to another class either.
* **Boss and monster classes keep their own abilities** (see below).

### Enemy siege tomes and long range weapons

Tick it and set the maximum range: weapons above that range are removed from the
pools that random map enemies draw their weapons from, and enemy **spells** are
re-rolled until they fit as well (Meteor and Bolting are spells, not weapons, so
they are checked against the magic table). Range 2 keeps the usual melee / 2
range setup. Player weapons, player spell lists and shop stock are untouched.

**Chance an enemy still gets one (%)** lets a percentage of enemies ignore the
limit. It is rolled once per enemy, and the siege weapon still has to come out of
the pool afterwards, so it is a chance on a chance - at 5% roughly a twentieth of
enemies are allowed to roll one, and most of those still end up with something
normal because the siege tome is a small part of the weapon pool. Set it to 0 to
never allow them, 100 to only limit the pool (the old behaviour before this
option existed).

### Level scaling

Moves a percentage of the enemy Str / Mag / Def / Res growths into HP. Enemy
stats grow per level from the class enemy growths, so this makes level scaling
produce tankier enemies instead of harder hitting ones. 0% leaves it alone, 20%
is a noticeable but fair change, 60% is the maximum.

### More randomization options

Extra passes that run after the normal randomization, in the *More randomization
options* section of the tab:

| Option | Default | What it does |
| --- | --- | --- |
| Keep boss and monster class abilities as they are | on | Monster and boss classes get their class ability, latent ability and barrier ability slots back from the vanilla file. A boss stays a boss even when the monster tiers are being randomized, and the even-variety spread never touches monster classes either. |
| Keep boss and monster class stats as they are | on | Same idea for the stat boosts and base stats of monster classes, so a boss is not turned into a glass cannon. |
| Share of the level scaling bonus bosses get (%) | 25 | When level scaling is on, bosses keep their vanilla stats and would fall behind every other enemy in the run. They get a share of the scaling as a flat bump to their base stats - 25% by default, so a 150% multiplier gives them about 12% more base stats. That is a fraction of what a normal enemy gains, so they stay beatable; capped at 50% of the scaling. |
| Never give the same class the same ability twice | on | A class cannot end up with one ability in two of its slots (which just wastes a slot). |
| Give every class in a tier a different mastery ability | off | Inside one class tier, no two classes share a mastery ability. |
| Give every class in a tier a different mastery combat art | off | The same for the mastery combat art slot. |
| Give every character a different personal ability | off | No two characters share a personal ability. Only does anything when personal abilities are being randomized. |
| Even out the growth totals of every class | off | Scales a class's growths up or down so every class grows roughly the same amount per level, while keeping the ratio between its stats - a slow tanky class stays slow and tanky. The percentage is how far from the average a class may sit before it is touched (default 15%). |
| Keep each class's best growth stat as its best stat | off | If the randomization buries the stat a class is known for, two growth values are swapped so that stat is still the class's highest. Totals are unchanged. |
| Even out the base stat totals of every class | off | The same idea as the growth totals, for base stats: every class ends up with a similar stat total while keeping the ratio between its stats (default 15% tolerance). |

Like the other extra options, these are not saved between runs - set them each
time you start the tool.

They are all *post passes*: they work on the finished file, after everything else
has been decided, and each one only replaces a value the randomization
**introduced**. Vanilla data - Dancer's Dance, a boss's own abilities and stats, a
stat the class simply does not grow in - is never taken away.

### Large scale randomization

The options above tidy things up. These break the class lineup apart - each one is
a **permutation** of the numbers already in the file, so nothing is invented and
no value can leave its range, but the archetypes are gone:

| Option | Default | What it does |
| --- | --- | --- |
| Swap class stat profiles between classes | off | Base stats and stat boosts are dealt out again between the classes of a tier, so a Fortress Knight can end up with a Pegasus stat line. |
| Swap class growth profiles between classes | off | The same for the nine class growths - the class you pick for its growths becomes a gamble. |
| Swap across the whole game instead of inside one tier | off | The two swaps above then ignore tier boundaries, so a beginner class can receive a master class's numbers. |
| Shuffle character stat sheets between characters | off | Base stats and growths are permuted between the playable characters, so "who is the fast one" changes every run. Character caps are not moved, so nobody is permanently capped out. |
| Boss remix | off | Every boss / monster class receives another boss's whole ability kit (class abilities, latent abilities and barriers) instead of its own. Takes precedence over the boss protection options. |

They run after the protection and tidy-up passes and before the blocklist safety
pass, so a remixed boss still cannot be handed a blocked ability.

### Weapon effects remix

Weapons, gambits and combat arts store their effect in the **same id space**
(weapon and gambit effect = byte +12 of a 24-byte block, combat art effect =
byte +7 of an 18-byte block, 0..93 with 96 meaning "none"), which makes a real
crossover possible without inventing anything:

Every graft is its own **percentage chance**, rolled once per entry, so they can be
mixed at whatever rate you like. An entry is rolled at most once, in the order
shown below, and stops at the first graft that succeeds.

| Option | Default | What it does |
| --- | --- | --- |
| Weapons take another weapon's effects (%) | 0 | Copies the status effect, the effectiveness bits and the property bits (double attack, magic damage, ignore defence, leave at 1 HP) from another weapon. |
| Weapons take a combat art effect (%) | 0 | The weapon's status effect is replaced by a combat-art effect id. |
| Weapons take a gambit's effects (%) | 0 | The full effect set comes from a gambit. |
| Combat arts take another art's effect (%) | 0 | The art's effect id is replaced by another art's. |
| Combat arts take a weapon's effect (%) | 0 | The art's effect id comes from a weapon. |
| Gambits take another gambit's effects (%) | 0 | The gambit's whole effect set comes from another gambit. |
| Gambits take a weapon's effects (%) | 0 | The gambit's whole effect set comes from a weapon. |
| Weapon donors must be a different weapon type | off | Applies to the three weapon grafts: the donor must be a different weapon type, so a sword can genuinely behave like a bow. |
| No two weapons share the same effect set | off | Duplicate effect combinations are re-rolled. Effect ids are 0..95, so a run with more than 96 weapons that have an effect cannot make every one of them unique - the report says how many had to share in that case instead of calling it a failure. |

Only the **effects** move - might, hit, crit, weight, range and durability stay
exactly as the normal weapon randomization set them, so these stack with
`randAtk*` instead of replacing it. A weapon, art or gambit with no eligible donor
is left alone, and unused art rows (flag 63) and empty gambit rows are skipped.

The changelog gets a `---EFFECT REMIX---` section with the counts, the chance
used for each graft, and the first 40 renamed weapon effects, because the normal
changelog is built before these passes run.

### Quick setup for testing

Two buttons at the top of the Extra options tab:

* **Enable every option (debug)** - ticks every randomization switch at once
  (all of `Settings`, plus the extra options on this tab and the range limits on
  the Classes tabs). The interpolation styles (binomial / linear / proportional /
  exponential) are deliberately left alone: they are alternatives to each other,
  not features.
* **Turn every option off** - back to a clean slate without restarting the tool.

### Presets - save the whole configuration

The **Presets** row at the top of the Extra options tab stores everything at
once: every option on every tab, plus the extra options and the range limits.
Useful when a run needs a specific setup, or when you want to try one thing at a
time without re-ticking 400 boxes.

| Button | What it does |
| --- | --- |
| Apply preset | Resets everything to the tool's defaults, then applies the preset |
| Save current settings as preset... | Writes the current state to `presets\<name>.txt` next to the exe |
| Delete preset | Removes a saved preset (the built-in one cannot be deleted) |
| Open preset folder | Shows the folder the files live in |

A preset is a plain text file, one `name=value` per line:

```
randClassBases=True
RandomizerBig.swapScope=1
RandomizerBig.weaponGraftChance=50
RandomizerExtras.blockedAbilities=190,191,195,197,205,207
```

Because applying a preset resets to the defaults first, the result is exactly
what the file says - a preset never means "the file plus whatever was already
ticked". That also means a half-written preset is a valid way to start from a
known state and change one thing.

One preset is built in: **Everything on - the 1.8.1 test run**. It is the exact
configuration of the stress run recorded in the 1.8.1 debug report (241 settings,
41 pass options), reconstructed from that report, so that run can be repeated
byte for byte. The debug report prints the applied preset name at the top.

### Range limits, next to the thing they limit

The three "keep it inside this range" options live on the tab that randomizes
what they limit, instead of being buried in the extra tab:

| Tab | Option | Default range |
| --- | --- | --- |
| Classes > **Stat Boosts** | Keep randomized class stat boosts inside this range | off, -3 to 10 (negatives allowed) |
| Classes > **Growth Boosts** | Keep randomized growths inside this range | off, 10% to 70% |
| Classes > **Stat Bases** | Keep randomized base stats inside this range | off, 3 to 30 |

Each one is a single row docked to the bottom of its page, so nothing the
original tool put there has been moved. Like the other passes they only touch
values the randomization changed - a growth the class does not have (0%) or a
stat that is still vanilla is left alone. They also skip the monster / boss
class rows on purpose: those are covered by the boss options, and some of their
slots do not hold growths or stats at all.

The **Stat Boosts** range is signed (-3 to 10 by default) because class stat
boosts can be negative.

The order the passes run in matters: boss abilities and stats are restored first,
then the ability uniqueness passes, then growths, then stats, then the range
limits, and the boss level-scaling bonus last - so a range limit can never clamp
the boss bonus back off.

The clamps run a **second time** once the large scale passes are done. Those
passes move whole stat and growth vectors between classes, which would otherwise
carry an out of range value back in after the limits had already been applied
(a swapped growth profile is the usual culprit). The second run is the one that
decides what gets written, and it still leaves vanilla values and boss rows
alone.

The report only measures values the randomization actually changed, so a vanilla
growth or base stat that sits outside the range is listed once as "kept on
purpose" instead of turning the whole check into a warning.

The debug report gets a **BIG OPTIONS** section too: how many classes and
characters were swapped, the swap scope, and a PASS / WARN line per pass (a swap
that was on but changed nothing is a warning, not a silent success).

The debug report gets a **MORE OPTIONS** section for all of this: how many slots
each pass changed, how many duplicates are left, the class growth range and
growth totals after the run, the stat boost and base stat extremes, with a
PASS / WARN / FAIL line per option.

## Use it

**Batch: folders** tab:

| Control | What it does |
| --- | --- |
| Folder with the game files + **Browse…** | The folder to search. Defaults to the folder the exe is in. |
| **Search subfolders too** | On by default, so a whole extracted `romfs` dump works. |
| **Scan folder and load all files** | Finds every needed file, loads it, and prints a per-file report in the log box. |
| Output folder + **Browse…** | Where the randomized files should end up. Defaults to `randomizer output` next to the exe. |
| **Also copy the patched DATA1.bin into the output folder** | Off by default, because `DATA1.bin` can be several GB. Otherwise `DATA1.bin` is patched in place, like the original program does. |
| **Randomize and save to output folder** | Runs the randomization and writes every output into the patch layout. |
| **Open output folder** | Opens the result in Explorer. |

The output folder is remembered, so the normal *Randomize and Save* button also
writes into it (no dialogs) once you have picked one. Clear the box to get the
old per-file save dialogs back.

### What the scan recognises

Each file is matched by name, and the usual file size is checked so the log can
warn you when a file looks wrong. When several copies exist (a dump often has
more than one `DATA1.bin`) it prefers the copy with the expected size, then the
shallowest path, and logs which one it used.

`DATA1.bin`, `fixed_persondata.bin`, `fixed_classdata.bin`, `fixed_data.bin`,
`fixed_calendardata.bin`, `fixed_lobby_common.bin`, `fixed_act_data.bin`,
`fixed_growthdata.bin`, `fixed_lobby_activity.bin`, `fixed_groupworkdata.bin`,
`fixed_presentdata.bin`, `fixed_shopdata.bin`, `fixed_lobby_quest.bin`,
`fixed_scenario.bin`, `msgdata.bin`, `scrdata.bin`, `gwscrdata.bin`,
`tuscrdata.bin`, `btlscrdata.bin`, `BGM.ktsl2stbin`.

### Where the output goes

| File | Destination |
| --- | --- |
| `BGM.ktsl2stbin` | `nx\sound` |
| `msgdata.bin`, `scrdata.bin`, `gwscrdata.bin`, `tuscrdata.bin`, `btlscrdata.bin` | `patch4\common\common` |
| `fixed_calendardata.bin` | `patch3\nx\data` |
| every other `fixed_*.bin` | `patch4\nx\data` |
| `INFO0.bin`, `INFO2.bin` | `patch4` |
| `DATA1.bin` | patched in place (optionally copied to `patch4\common\common`) |
| `FE3H Randomizer Changelog.txt` | output folder root, only if the changelog box is ticked |

Copy `nx`, `patch1`, `patch3` and `patch4` from the output folder into the
`romfs` folder of your mod.

Empty folders are not created, so you only get the patch folders that actually
received files.

## Performance tweaks

Small, paint-and-layout-only changes - no randomization logic is touched:

* **Composited painting (`WS_EX_COMPOSITED`).** The window holds about 1,300
  controls and, without this, every one of them paints itself the moment it
  becomes visible - which is why a tab change looked like it was being drawn in
  strips, top to bottom. With composited painting the whole window is drawn into
  an off-screen buffer and appears in one go.
* **Every tab page is sized once, up front.** A page keeps its small designer
  size until the first time you click it - and *then* Windows resizes it and lays
  out hundreds of controls while you wait. Right after the window opens, each
  hidden page is given its real size, one page per message so the window stays
  responsive, so the first visit to any tab is instant.
* double buffering on the form, the tab control and every tab page.
* `updateAllDisplay()`, which re-fills thousands of controls after loading a file
  or toggling an option, batches its layout work into a single pass.
* the two added tabs suspend their layout while they are being built.

> Note for anyone comparing with the previous build: the tab-switch
> `SuspendLayout`/`ResumeLayout` pair that v1.3.0 added has been removed. It
> forced a **full** re-layout of the incoming page on every switch, which cost
> more than it saved.

The debug report now has a **PERFORMANCE** section (window build time, display
refresh time, page pre-layout times, last/worst tab switch, and how many
switches took over 250 ms), so if switching still feels slow the numbers say
where the time goes.

## Debug report

The **Extra options** tab has a *Debug report* section:

* **"Write a debug report automatically after every randomization"** (on by
  default) - after a run, `FE3H Randomizer Debug.txt` is written to the output
  folder (or next to the exe if no output folder is set).
* **"Write debug report now"** - writes one on demand, e.g. right after loading
  files, to check that the scan found everything.
* **"Open debug folder"** - opens the folder with the report.

The report is plain text and contains:

| Section | Contents |
| --- | --- |
| Environment | exe path, assembly/file version, OS, .NET version, 32/64-bit |
| Performance | window build time, display-refresh time, page pre-layout times, last/worst tab switch, switches over 250 ms |
| New options | every extra option and the full blocked ability list with names |
| Loaded files | all 20 possible inputs: loaded or not, path, size, size mismatch warnings |
| Did it run? | counters for every new code path (class picks, weapon type draws, blocked ability picks, range filters, bypass rolls, growth shifts), with warnings when an enabled option never fired |
| Class variety | weapon type spread per class tier, before vs after, the types the balancer served, and a PASS/FAIL against the safety level |
| Ability blocklist | scans class abilities, monster latent/barrier abilities and character learnset ability slots (combat arts are skipped - the same id means something else there); lists blocked abilities the randomization *introduced* (vanilla ones are reported separately, they are intentional) and how many the safety pass replaced |
| Siege limit | out of range weapons and spells on every map unit, marked as "same as vanilla" or "NEW" |
| Growth shift | per stat growth totals before and after, plus the number of classes changed |
| Summary | every PASS / WARN / FAIL line and a final count |
| Settings dump | the full settings object, same dump the changelog uses |

A crash during randomization now also writes the report (with the stack trace in
the message box) instead of only failing with a dialog.

## What changed in the original code

Everything new is in **`BatchTools.cs`**, a partial class file. `RandomizerForm.cs`
has exactly two edits:

```diff
-    public class RandomizerForm : Form
+    public partial class RandomizerForm : Form
```

```diff
     private void saveFiles()
     {
+        // Added by BatchTools.cs: when an output folder is selected, write
+        // every file straight into the patch layout instead of 20 dialogs.
+        if (this.batchOutputFolder != null && this.batchOutputFolder.Length > 0)
+        {
+            this.saveFilesToFolder(this.batchOutputFolder);
+            return;
+        }
+
         if (Randomizer.personLoaded)
```

`Randomizer.cs` (the randomization itself) only gets small, targeted hooks:

* `RandomizerExtras.beginClassVariety(classWeaponTypes)` once, and the plain
  class picks become `RandomizerExtras.pickClass(tier, pool)`.
* `Randomizer.getBestFittingClass` gains a preferred-type filter with a
  fallback to the old behaviour.
* Ability picks become `RandomizerExtras.pickAbility(pool)` (147 call sites),
  so the blocklist applies everywhere abilities are handed out.
* The two enemy weapon pools are filtered with `RandomizerExtras.filterRanges`.
* One `RandomizerExtras.applyAfterRandomization()` call before the changelog is
  written, which runs the growth shift and the tier spread passes.

Why a partial class: the decompiled form is 36,000 lines of machine-generated
layout code. Adding separate files means the new features cannot break existing
behaviour, and the original per-file *Load* and *Save* buttons keep working
exactly as before.

Two smaller changes outside the code:

* `Resources\DATA0.bin`, `INFO0.bin` and `INFO2.bin` are now part of the
  project. The original exe carried them inside itself and the published source
  did not include them; they were extracted from the shipped exe byte for byte
  and are embedded under their original logical names.
* `build.bat` was added.

## Notes and credits

* Original program: **Bly / Nifyr**, GPL-3.0 — see `LICENSE`. This source is the
  published decompilation from `Nifyr/Fire-Emblem-Three-Houses-Randomizer`,
  whose release build is the same size as the exe in this repo (2,435,072 bytes).
* The new code is written in plain C# (no C# 6+ syntax), so an older compiler
  can handle it too.
* This project was assembled in a Linux sandbox with no .NET/Mono toolchain, so
  it has not been compiled here. All files were parsed with a C# grammar (no
  syntax errors), and every member the new code touches was checked against
  `Randomizer.cs`, `Settings.cs` and `RandomizerForm.cs`. If `build.bat` reports
  an error, paste the first error line and it can be fixed quickly.
