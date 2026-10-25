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

* **Dance (158) and Special Dance (148) belong to the Dancer class.** Both are on
  the fixed list below, so no class slot, learnset or mastery roll can hand them
  to anyone else. The Dancer row is written back with Dance after its slots are
  rolled - that is the point of the class, and the safety pass leaves it alone
  because it was already there in the vanilla file.
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
stats grow per level from the class enemy growths, so this trades per-level
attack and *defence* for HP. Defence is the number that decides whether a weak
unit can hurt an enemy at all, so a run where enemies feel unkillable usually
wants this higher rather than lower: 0% leaves it alone, 10% is the recorded
run, 30% is what the No Monastery presets use, 60% is the maximum.

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

### Total conversion mods (No Monastery)

The Extra options tab has a **Total conversion mod** section. Mod mode is off by
default; switch it on when the game runs a mod such as *Three Houses: No
Monastery*.

#### Two releases, two completely different shapes

No Monastery changed how it is built, and the tool needs to know which one is
installed before it does anything:

| Release | What it ships | What that means for the randomizer |
| --- | --- | --- |
| **1.3.3 and older** | thirteen `fixed_*.bin` of its own under `data/`, plus `bmap/`, `castle/`, `event/`, `text/` | The game's data files are *replaced*, rows were added, so every table after them moved. The offsets have to be lined up (built-in profile, below). |
| **2.1 "with Aldebaran"** | **no game data at all**: `romfs/skyline/plugins/libno_monastery.nro`, `exefs/main.npdm`, `exefs/subsdk9`, and a changelog | Nothing of the game's data is replaced, so nothing can have moved. The game's own `fixed_*.bin` are the correct input and the compiled-in offsets are already right - the tool needs no maps, no originals folder and asks no questions. Its own changelog says the same: *"No changes to gamedata (no classes, character data, etc.. changed)"*. |

**Point it at the mod once.** Type the mod's folder into **Mod folder** and press
**Check the mod** (or use *Browse...*), and the tool looks for the markers of
either release - `romfs\skyline\plugins\*.nro` and `exefs/subsdk9` for 2.1,
`fixed_classdata.bin` of a known mod size for 1.3.3 - and records what it found in
the debug report:

```
Mod checked                    : No Monastery 2.1 (Skyline plugin)   - a Skyline plugin: no game data files, nothing to line up
Mod folder                     : F:\mods\NoMonastery with Aldebaran
Mod detail                     : romfs\skyline\plugins\libno_monastery.nro, exefs\subsdk9
```

A folder scan does the same thing on its own when the mod's `skyline\plugins`
folder is at or above the folder being scanned, so pointing the normal scan at
`contents/<title id>` also works. If the 2.1 zip was never unzipped, **Check the
mod** says so instead of guessing.

**Browse... opens the standard Windows folder dialog** - the same one the
`Load ...` buttons on the other tabs use, with the breadcrumb bar, an address bar
you can paste a path into, and the quick-access sidebar - not the old .NET tree
control that has no address bar and drops you deep in a build or temp folder. It
starts at the mod folder already set, otherwise at the folder the last scan read,
and otherwise where you last picked a folder. The path box beside the button
takes a pasted path too, so a mod a dozen folders down is one paste away. The
same dialog is used for the game-files and output folders on the Batch tab.

#### No Monastery mode: leave out what the mod deletes

The mod does not just conflict with a handful of options - it removes whole
systems. Exploration and the weekly teaching loop are gone, so an option that
only writes the tables behind them produces a changelog entry for a screen the
game will never open, and buries the options that do matter.

**No Monastery mode** sits under the mod-mode box on the Extra options tab, and
does four things when it is ticked:

| What | Why |
| --- | --- |
| Switches mod mode on | the mod's own values are what the game reads |
| Leaves 9 options out of the run | goals, seminars, faculty training, dining hall / choir / greenhouse / fishing events, free day limits, rare monster sightings |
| Sets the first character spell to **C** | the mod starts units in intermediate classes, so the game's usual D is already earned and every unit would cast from level 1 |
| Names every skipped option in the changelog and the debug report | a skip that nobody can see is worse than a wasted pass |

The ticks are **not** cleared - the run simply uses a copy of the settings with
those switches off, and the report lists them under
`left out of this run : 9 option(s) - weekly goals, seminars, ...`. Untick the
mode and the next run is exactly what the boxes say again.

Calendar randomization is deliberately **not** skipped: the mod ships its own
calendar, so a randomized day type can land on a week the mod no longer has.
The report prints a note when that option is on with the mode.

Both built-in No Monastery presets apply the mode, so applying one gets all of
the above in one click.

#### The 2.1 plugin: what still fights it

Because 2.1 changes the game by code instead of by data, the list of options it
owns is short - only the systems it hooks, taken from the plugin itself
(`unit_level_up_hook`, `calculate_exp`, `class_cert_display`, `unit_can_equip_hook`,
`setup_recruitment_hook`, ...) and its changelog:

| Option | Why |
| --- | --- |
| `changeLv` | The plugin scales enemy levels to the player's own (3 / 5 / 8 levels apart on Normal / Hard / Maddening) and can reclass them; randomizing levels as well fights it. |
| `randClassMasteryAb` | The plugin hands class and mastery abilities out by skill rank, so randomised mastery abilities are not what it grants. |
| `randClassMasteryCa` | Same for mastery combat arts. |

Everything the 1.3.3 list warns about - its dialogue, shops, monastery activities,
mastery EXP scale - is **not** a conflict for 2.1, because the plugin never
touches those files: randomize them freely. A few options still work but behave
differently, and the report names them instead of quietly changing the result:

* `randExpYield` - the plugin recalculates exp (catch-up exp, aux battles divided
  by turn count).
* `randCert` - the plugin auto-certs anything at 90% or better, so units may
  certify into the randomized classes when they are recruited.
* `randClassAb` / `randClassClassAb` - the plugin grants the class's own ability
  list by skill rank, so it will grant whatever the randomization put there.

Its `no_monastery.toml` settings (`growth_mode`, `free_deployment`,
`class_weapon_lock`, `auto_support_completer`, `dialog_skip`, cutscene skip) live
in `sd:/Aldebaran/` and are read at runtime; they change nothing about the data
the randomizer writes.

#### The 1.3.3 data files

What mod mode was originally checked against, from that release's own
`fixed_*.bin`:

| Behaviour | Why |
| --- | --- |
| Every table offset is moved to where the mod's file keeps it | The mod's files are larger, and the extra bytes are **not** all at the end. Each file's header lists where its sub-tables start; comparing that list with the original file gives the shift of every section. See below. |
| Class mastery EXP values are put back after randomizing | The mod's mastery rework runs on its own **0 / 20 / 50 / 100 / 150 / 200** scale (Noble and Commoner are 200 for Aptitude). Random values break it. |
| The Dancer row is looked up instead of assumed to be class 43 | In the mod's data the Dancer is still row 43, but Dance (ability 158) sits in the **third** ability slot instead of the first, and the class table has **101** rows instead of 100. The lookup scans the ability slots of the loaded data, so a mod that moves the Dancer still works; if the lookup is ambiguous, 43 is kept. |
| The report lists the options that fight the mod | Its dialogue, shops, monastery, level curve and mastery values are all things the mod owns. |

#### The table offsets

The randomizer writes at fixed byte offsets into `fixed_classdata.bin`,
`fixed_data.bin` and `fixed_persondata.bin`. No Monastery adds rows, which moves
everything after them, and *by a different amount in every section* - so no
single correction is enough. The header of each of those files starts with a
directory of sub-table starts, and the mod keeps that directory up to date.
Point **Unmodified game files** on this tab at a folder with the original three
files, press **Read table offsets**, and the tool compares the two directories:

| File | Sections | Shift per section |
| --- | --- | --- |
| `fixed_classdata.bin` | 10 | +118, +120, +136, +144 from the 5th on |
| `fixed_data.bin` | 16 | none, then +288, then +648 from the 9th on |
| `fixed_persondata.bin` | 18 | none, +68 .. +320, then +6320 from the 12th on |

That moves 5 class, 8 item and 15 character table offsets (class rows,
certifications, tiers, abilities, monster data; weapons, spells, turrets,
gambits, blow data, combat arts, gambit extras, battalions; characters, weapon
ranks, spell lists, skills, starting inventory, combat arts, supports, special
supports, budding talents, learnsets, faculty training, seminars, goals,
portraits, enemy skills). Tables that the mod did not move keep their original
offset, so vanilla users see exactly the old behaviour.

The same directory is read for every other data file the randomizer writes -
`fixed_act_data.bin`, `fixed_shopdata.bin`, `fixed_lobby_common.bin`,
`fixed_lobby_activity.bin`, `fixed_growthdata.bin`, `fixed_groupworkdata.bin`,
`fixed_presentdata.bin`, `fixed_lobby_quest.bin` and `fixed_scenario.bin` - so
those tables move too, as soon as their original copy is in the folder.

**No Monastery 1.3.3 needs no folder at all.** The measured map for that release
is built into the exe, so the originals folder is optional: the tool recognises
the mod by its own file headers - the size and every sub-table start of
`fixed_classdata.bin`, `fixed_data.bin`, `fixed_persondata.bin`,
`fixed_act_data.bin`, `fixed_shopdata.bin` and `fixed_scenario.bin` - and only
uses a profile when all of them match. The debug report says `from the built-in
profile for No Monastery 1.3.3` when that happens. A different build - or a
different mod - never matches silently: it falls back to the originals folder,
and without one the run asks first.

**What is verified:** those six files were checked against the original game
copies, every section start in the mod's header was matched one for one, and the
content at every moved offset lines up (the bytes that differ are the mod's own
edits). Measured shifts:

| File | Sections | Shift per section |
| --- | --- | --- |
| `fixed_classdata.bin` | 10 | +118, +120, +136, +144 from the 5th on |
| `fixed_data.bin` | 16 | none, then +288, then +648 from the 9th on |
| `fixed_persondata.bin` | 18 | none, +68 .. +320, then +6320 from the 12th on |
| `fixed_act_data.bin` | 29 | none, then +192 from the 3rd on |
| `fixed_shopdata.bin` | 13 | none, then +312 from the 6th on |
| `fixed_scenario.bin` | 2 | none (the +11 bytes land in the last section) |

The other files the randomizer writes are unchanged in size and layout; for those
the tool still reads the directory, so a future mod that moves them is handled
the same way.

The debug report prints the originals folder, every map with its per-section
deltas, every adjusted offset, and the files whose offsets were not checked.

**Analyse the loaded files** compares every loaded file with the original sizes
and reports the deltas, which is the quickest way to see whether you loaded the
mod's copies or the game dump's. It also prints the class table findings: tier
range, monster rows, the mastery values in use, the Dancer row, and any populated
class rows outside the range the randomizer writes (88..99, except 91) - the mod
has one at row 89, and those rows are deliberately left alone.

**Use the mod-safe preset** applies the recorded run with everything the release
that was found owns turned off, and switches mod mode on. It picks the matching
one by itself: 1.3.3 gets the 38-option data-file preset, 2.1 gets the 3-option
plugin preset, and with no mod checked it applies the 1.3.3 one.

**Where the files have to go:** load the files the game actually reads and write
the output back over that same set. Two setups work:

| Setup | What to load | Where the output goes |
| --- | --- | --- |
| No Monastery 2.1 (Skyline plugin) | the **game's own** `fixed_*.bin` - the plugin ships none | anywhere the game reads: the game romfs folder, or the mod's own `contents/<title id>/romfs` (it is layered over the game's) |
| 1.3.3 in a load-path overlay (normal) | the mod's copies (`load\<game id>\...\romfs\patch4\nx\data`), not the game dump | the same overlay folder |
| 1.3.3 copied over the game's own files | the modded game folder - the files there are the mod's | the same folder (they are already the mod's) |

A romfs overlay wins over the dump, so a randomized file written anywhere else is
ignored by the game. `DATA1.bin` is patched in place and is not part of the
overlay. The output writer only ever creates folders, never deletes them, so
writing into the mod's own `romfs` cannot disturb its `skyline` folder.

**The one mistake to avoid (1.3.3 only):** randomizing the *unmodified* dump
while the game runs a data-file mod. The output is rebuilt from the originals, so
copying it over the game puts the original tables back and wipes the mod out of
every file it overwrites. The tool notices that case: if mod mode is on with a
data mod and every loaded file is the exact original size, it says so in the
debug report and asks before the run. The file list names every file as either
"the game dump's copy" or "the mod's copy" so it is obvious which one you loaded.
With the 2.1 plugin none of that applies - the game's own files *are* the right
input, so it has nothing to warn about.

**When the report still says the loaded files are the game dump's**, the mod's
copies are somewhere else on disk. The report now prints the folder the last scan
read and, under the no-map-needed note, every mod-sized copy of those six files it
could find at or just above that folder, each marked `<- not under
patch4\nx\data` when the game would never read it. That is the usual cause: the
mod zip stores `data\...` with no `nx\` or `romfs\` in front, so unzipping it one
level too high leaves `...\data\fixed_*.bin` beside the real `patch4\nx\data`
instead of inside it, and the scan legitimately loads the game's own copies. Move
that `data` folder to `<romfs>\patch4\nx\data` and scan again.

**The same zip carries a stray `fixed_persondata.bin` at its root**, which is a
different build of the file (13,159 bytes differ from the copy in `data`). The
folder scan ignores it: a copy inside a `data` folder always wins over one that is
not.

**Mixing two installs is caught, not guessed at.** A scan looks through
subfolders, so it can pick up a file from a *different* install that happens to sit
under the folder you pointed at - a leftover `No Monastery\data` from the 1.3.3
release next to the romfs, for example, which would then supply
`fixed_growthdata.bin`, `fixed_lobby_common.bin` and `msgdata.bin` while the game's
own `patch4\nx\data` supplied the rest. Every loaded file is remembered with the
folder it came from, and anything whose path does not start with part of a romfs
tree (`patch1`..`patch4`, `nx`, `romfs`, `data`, `common`, `sound`, `contents`,
`atmosphere`, ...) is marked:

* in the scan log and its summary: `[outside the romfs layout]`, plus a warning
  listing the files and their folders;
* in the debug report, under the loaded files and again under mod compatibility;
* in a question before the run, which defaults to **No**.

The fix is to scan the game's own romfs folder only (or the mod's `romfs`, for a
data-file mod) and move the other folder out of the way. Files loaded one by one
with the `Load ...` dialogs are never flagged - there is no scan folder to compare
against.

**What the offsets cannot answer:** they describe the `fixed_*.bin` files the
randomizer writes. `DATA1.bin` is patched in place by section offset and No
Monastery does not ship it, so nothing there moves. The randomizer writes class
rows 0..99; the mod's extra class row 100 is left alone.

### Reading the debug report

The summary counts **passes**, **warnings** and **failures**, and only a `FAIL` means
something went wrong. Two of the warnings are settings doing their job rather than
faults:

* **`X served fewer weapon types than it was dealt`** — only a warning at safety
  levels below 100 %, where the dial deliberately leaves part of the spread to the
  ordinary draw. It is printed as a `note` line in the class variety section, and
  becomes a real `FAIL` at 100 % safety. Raise the safety dial on the Extra options
  tab for a fuller spread.
* **`tab switching is still slow`** — the Extra options tab paints in ~300 ms on
  some machines; it changes nothing about the output.

The range check now gives spells the same verdict as weapons: a *new* out-of-range
spell or weapon on a map unit is only expected from the **siege bypass chance**, so
the report says `allowed by the bypass chance` when the chance is above 0 and warns
when it is exactly 0 %. New offenders are listed before the ones that were already
in the vanilla data, so they are never buried.

`classes with changed enemy growths : N (the growth shift itself changed M)` — N
counts every class whose growths differ from vanilla, which includes plain growth
randomization; M counts the ones the growth shift moved.

### When a run stops with an error

The debug report names the stage the randomizer had reached (`Last stage : weapons: row 240`,
for instance) and, when the run died with an exception, a **CRASH DETAIL** block with its
type, its message, its stack trace and the stage marker. A crash report is therefore
enough to find the code that stopped.

One crash has its own message now. Every randomized number is rolled through one of four
interpolation styles - Binomial, Linear, Proportional, Exponential - and an option whose
four style boxes are **all unticked** used to stop the whole run with an empty error the
moment it rolled a value. That is easy to hit by unticking Proportional on the Weapon or
Class tabs, or by hand-editing a preset. In that case:

* before the run, a dialog lists every such option and says they will use **Linear**;
* the run then completes, and the report counts the values rolled that way and warns about it.

Tick one style per option (or untick the option itself) to go back to an exact choice.

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

Three presets are built in:

| Preset | What it is |
| --- | --- |
| **My run - No Monastery 2.1, 2026-10-24** | The owner's own *Save current settings as preset* file from that run: all 1,099 values, including the five range limits the saver used to drop and the options added since. Applying it repeats that run. |
| **No Monastery (mod safe)** | No Monastery **1.3.3** (the data-file release): the same run with the 38 options the mod ships written off explicitly, mod mode on and the mod's mastery EXP kept. |
| **No Monastery 2.1 (plugin, mod safe)** | No Monastery **2.1 "with Aldebaran"** (the Skyline plugin): the same run with only the 3 options the plugin recalculates written off - level scaling and the mastery abilities / combat arts it hands out by skill rank - mod mode on, and mastery EXP left randomised because the plugin has no values of its own to keep. |

All three are generated from that saved file rather than typed in by hand, so the
values match it one for one; his machine's paths and mod switches (`ModSupport.*`)
are stripped from the base preset and put back by the two mod ones. The mod-safe
presets do not merely *omit* the conflicting options: they write each of them as
`False` after the run's values, because the tool's default for a few (the mastery
EXP requirements, for instance) is on and a preset is applied on top of the
defaults.

Both No Monastery presets also change the three balance values of 1.12.8, because
the mod itself makes the problem worse - it starts units in intermediate classes
and raises the level of every enemy, and levels turn the class growths straight
into Def and Res:

| Value | Recorded run | No Monastery presets | Why |
| --- | --- | --- | --- |
| Level scaling (growth shift) | 10% | **30%** | the mod's extra levels buy HP, which any unit can chip through, instead of defence |
| Class identity | on (default) | on, written out | so the preset does not depend on a default that may change |
| Defence clamp | 50% on every band (default) | 20 / 35 / 50 / 50 | clamp the class base Def / Res, which is a floor for every enemy in that class - per level band |
| Weapon effect text | on (default) | on, written out | a grafted effect has to be readable in the item screen |
| Flat class base trim | off | **-6 HP, -2 Def, -2 Res** | the level 1 value is the part that makes the first maps feel walled; given back to your own units |
| Boss trim | off | **-6 HP, -2 Def, -2 Res** | on top, on the story units' own rows - the chapter 1 boss included |

The debug report prints the applied preset name at the top together with how
many values it set and how many lines it had to skip - a preset that silently
half-applies is visible at a glance.

Until 1.12.9 the saver dropped every option whose name begins with `stat`, which
is how a preset could be missing the five range limits (`statBoostClampMin/Max`,
`statBaseClampMin/Max`, `statTotalTolerance`): they were neither saved nor
restored, so a preset always ran those numbers at the tool's defaults. They are
saved now, and any preset file that predates the fix simply leaves them at the
defaults.

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

### Weapon effect text - say what the weapon does

The effect remix moves *effects* between weapons, and the item screen is the
only place a player can read what a weapon does. Until 1.12.8 the description was
left alone, so a sword that seals defence looked exactly like a plain iron sword
until it fired. **Extra options > Weapon effect text** (on by default) writes the
new effect into the weapon's own description:

```
Inflicts Seal Defense.
Deals magic damage.
```

The generated line covers everything the run actually wrote - the effect id, the
effectiveness bits (Infantry / Heavy Armor / Cavalry / Flying / Dragon / Monster)
and the property bits (magic damage, ignores Def/Res, leaves the target at 1 HP,
attacks twice). The vanilla flavour text is kept underneath when it fits, so a
weapon does not lose its identity, and the summary is always the first line. It
is dropped when the weapon's *properties* changed, because a vanilla line such as
"will attack twice when initiating combat" would then contradict the line above
it. It
covers both sources of a changed effect: the *status effect* option on the
Weapons tab and the effect remix on the Extra options tab.

Descriptions live in `msgdata.bin`, so the run writes that file even when none of
the text options on the Music and Text tab is on. If `msgdata.bin` was not
loaded, the debug report says the effect cannot be described instead of quietly
writing nothing.

### Class identity - the numbers have to match the class

A class's weapon type and its stats are rolled by two different passes, so
nothing tied them together: a class could end up using swords while its highest
base stat, boost and growth were all Magic, which in game reads as "this class
gives the wrong bonuses".

**Extra options > Class identity** (on by default) runs last and fixes both
halves of that:

* **Str / Mag** are *traded* when they contradict the weapon type the class now
  has - base stats, class boosts and both growth sets. Nothing is re-rolled, so
  every value the run produced survives; only which stat owns it changes. Hybrid
  classes (a physical and a magic weapon on purpose), monsters and boss rows are
  left alone, and so is any row the run did not change: a handful of vanilla
  classes have more Mag than Str on purpose (the extended lord rows and the
  special magic class), and a vanilla row is not a contradiction to fix.
* **Def / Res** may exceed the vanilla class by a percentage - **one percentage
  per level band**, because a class base is a flat addition and the same points
  are a different thing on a level 4 unit and on a level 32 one. Four boxes:
  **level 1-9** (`Noble`, `Commoner`, `Beginner`), **level 10-19**
  (`Intermediate`), **level 20-29** (`Advanced`), **level 30+** (`Master`, the
  unique and story classes). The band comes from the game's own tier byte at
  class row `+69`, not from a guess, and 0% means "do not pull this band back".

  The reason to split it is the floor: a class base is a floor for every unit in
  the class and the class boost is added on top, so one pair of lucky rolls used
  to be enough to turn a beginner class into a wall - and *every enemy in it*
  along with it. Holding the low bands close to vanilla and leaving the high
  bands room fixes exactly that, and the No Monastery presets do it (20 / 35 /
  50 / 50). The pull-back is reported class by class, with the band and the
  allowance that was used:

  ```
  classes pulled back  : 23
      Brigand base Def 11 -> 9 (vanilla 6, lv 10-19 35%)
      Brawler base Def 12 -> 9 (vanilla 6, lv 10-19 35%)
      Mage base Def 10 -> 5 (vanilla 3, lv 10-19 35%)
  ```

  This clamp is a limit on the roll, not a debuff: it hits both sides, so your
  own early classes are held to it too. That is what the **player start bonus**
  below is for - tighten the bands, then buy the difference back on your own
  units' personal bases. For an actual subtraction that goes *below* vanilla, the
  flat trim is the lever, and it does not touch your units at all.

Both numbers, the samples and a PASS line are in the debug report under
`--- CLASS IDENTITY ---`.

### Flat enemy bulk trim

**Extra options > Flat enemy bulk trim** subtracts a fixed amount of HP / Def /
Res from **every class base**. 0 on a row means that stat is not trimmed, so the
default (0 / 0 / 0) is off.

Why a flat subtraction instead of a percentage: a class base is what a level 1
unit of that class has, and it is a floor for every unit in the class. That is
the number that makes the first few maps feel walled, and at level 40 the same
two or three points are noise. -2 Def, -2 Res and -6 HP is the setting both
No Monastery presets use.

**It only weakens the enemy side.** The game keeps one class table, so a trimmed
class is trimmed for your own units of that class as well - which would make the
game harder, not easier. The same values are therefore added back to the
**personal base stats of every character you can deploy** (the students, the
knights and staff, Jeralt, the Ashen Wolves, Jeritza and Anna). Your units end up
with exactly the stats they had; the enemies standing in the same classes keep
the trim. The one price: an *enemy* copy of a playable character (an enemy
Edelgard in another route, for instance) keeps its stats too.

Bosses are included: the first boss of a run sits in an ordinary class like any
other, so it gets the trim with the rest of them. The monster / beast table is a
separate layout that holds no class base stats (its class rows are all zero), so
beast bosses keep their own numbers - that table is not touched at all.

It runs as the very last stat pass, after the range limits, so nothing can clamp
it back up, and boosts and growths are not touched. If `fixed_persondata.bin` was
not loaded the trim is **skipped** with a warning instead of quietly weakening
your own army.

### Boss trim (extra, on top)

**Extra options > Boss trim** - the same three spinners again, for the story
units only: **-6 HP / -2 Def / -2 Res** in both No Monastery presets, 0 = off.

A boss shares its class row with the soldiers next to it, so the class table
cannot tell them apart. The character table can: a boss is a **named character
who is not one of your units**, and its personal bases are its own. That is what
this trims, so it stacks on top of the class trim for exactly the fights that
matter - and never touches the player's people.

```
boss rows trimmed    : 37   (HP -6, Def -2, Res -2 off the named, non-playable character rows, on top of the class trim)
    Kostas (row 43) personal base -6 HP, -2 Def, -2 Res
    Lonato (row 44) personal base -6 HP, -2 Def, -2 Res
    Death Knight (row 42) personal base -6 HP, -2 Def, -2 Res
```

Story NPCs on your side (Rodrigue, Judith, Nader, Rhea) are in the same group -
they are not units you can field, so the tool cannot tell an allied NPC from an
enemy one. Against a chapter boss that is a rounding error; if an allied NPC ever
feels too fragile, drop the number to 0 and use the class trim alone.

### Player start bonus (the other direction)

**Extra options > Player starting stats > Levels' worth of stats every playable
unit starts with** - **5** in both No Monastery presets, 0 = off.

The reason the first map of a No Monastery run hurts is not only that the enemies
are trimmed *up*: the mod starts them at **level 6 on Hard and 9 on Maddening**
while your units still start where they always did. Taking the enemy down was one
answer; this is the other, and it is the one that does not make the fights mushy.

A character's **level itself cannot be written from the data files**. It is not in
`fixed_persondata.bin` (the character rows hold class, bases, growths and the two
ability slots - nothing else) and it is not in any of the other `fixed_*.bin`
files this tool writes: the level is part of the save, and the closest thing in
the data - the start level per chapter in `fixed_scenario.bin` - is a value the
*mod itself rewrites*, so touching it would fight the mod instead of helping.
That is also why this option is not called "start at level 3": it cannot make the
number on the unit card change.

What a level actually *gives* is stats, and the average gain of one level is that
unit's own growth rate. So the option converts levels into stats the honest way:

```
gain = N levels x growth / 100, rounded
```

applied to **all nine stats at once** (HP, Str, Mag, Dex, Spd, Lck, Def, Res, Cha)
on that character's own personal base. A unit with a 45% HP growth and 5 levels
gets **+2 HP**; a 55% Spd growth gets **+3 Spd**; a 30% Res growth gets **+2 Res**.
Zero-growth stats stay untouched, every stat still stops at the byte's 255 limit
(no character is anywhere near it), and the personal base is the same byte pair
the flat-trim compensation already uses - the mirror image of the trim, pointing
the other way.

The growths it reads are the ones **the run actually wrote**, so the bonus follows
the roll: a unit whose growths were shuffled towards Str gets the Str, one whose
Dex growth was clamped down gets less Dex. That is deliberate - the number of
levels is fixed, what a level of *that unit* is worth is not.

A real level also adds the *class*'s growth on top of the character's, which this
leaves out, so the figure is the conservative half of "N levels" - the right side
to err on. The debug report prints the exact per-stat gains, so there is nothing
to guess at.

* **Who gets it:** the same `playableCharacters` list the compensation uses - the
  units you can deploy. Never an enemy, and never a boss row.
* **Who does not:** every story boss, so the boss trim and this option do not
  cancel each other out.
* **What it does not change:** the displayed level, EXP gain, class requirements,
  class mastery, or the gender/stat caps. This is the stats half of a level, not a
  level.
* **Where it stacks:** on top of the class bases the unit is standing in, so it
  applies whichever class it reclasses into - which is the point.

```
player start bonus    : 42   (5 level(s) of stats - N x that unit's own growth / 100 - added to every playable unit's personal bases)
    Byleth (+5 level(s) of stats): HP +2, Str +2, Mag +2, Dex +2, Spd +2, Lck +2, Def +2, Res +2, Cha +2
    Edelgard (+5 level(s) of stats): HP +2, Str +3, Mag +2, Dex +2, Spd +2, Lck +2, Def +2, Res +2, Cha +3
```

**Why this and not a "weaken the first three maps" option:** a chapter-specific
nerf is invisible everywhere else in the run and has to be re-tuned per difficulty;
a stat block on your own units is the same currency the enemy levels are spent in,
so the two sides stay on one scale. It also composes with the trims: with the mod
presets the enemy loses -6 HP / -2 Def / -2 Res on the class base while you gain
several levels of growth, which is a much wider gap than either lever alone.

### Why there is no "enemy base stats" option

The class table splits **growths** into two columns - a player set at `+20..+29`
and an enemy set at `+19/+30..+38` - which is why the level scaling can move the
enemy growths into HP without touching your own units' growths.

The **base stats** are not split: `+75` (HP) and `+85..+93` are one block per
class, read by the player's units and by every enemy standing in that class. There
is no second copy to aim at, and no other table in the game's data files that
holds per-class enemy base stats. The two things the tool can do about that are
exactly what is on the tab now:

| Lever | What it reaches |
| --- | --- |
| Class base trim (with the compensation) | every enemy in every class, bosses and generic soldiers alike |
| Boss trim | the named story units, on their own personal bases, on top |

If a run still feels over-tuned on a specific fight, the boss numbers are the
fine dial; the class numbers are the coarse one.

```
flat trim applied    : 84   (HP -6, Def -2, Res -2 off every class base, last, monsters and boss rows included)
    Myrmidon base HP 20 -> 14, Def 5 -> 3, Res 2 -> 0
    Soldier base HP 20 -> 14, Def 5 -> 3, Res 2 -> 0
player units given it back : 42   (the trim is added to their personal bases, so only the enemy side gets weaker)
    Byleth personal base +6 HP, +2 Def, +2 Res  (so the class trim costs this unit nothing)
    Edelgard personal base +6 HP, +2 Def, +2 Res  (so the class trim costs this unit nothing)
```

### Abilities that are never handed out

Some abilities end a run on their own: **Twin Crests**, **Ten Elites**, **Mighty
King of Legend**, **Path of the Conqueror**, **Ancient Dragonskin**, **Ancient
Dragon Wrath**, **Surging Light**, **Divine Dragon Horn**, **Giant Wings**, **Air
of Intimidation**, **Dragon / Monster Effect Null**, **Monstrous Appeal**,
**Umbral Leach** and **Manifest Phantoms** - plus **Dance**, **Special Dance** and
the blank *Missing Number* entry (154). They are now on a second, fixed list that
is applied on top of the tick list above: they never appear in a randomized pool,
a learnset, a class ability slot or a mastery slot, for players *and* enemies, and
turning the blocklist off does not re-enable them.

The safety pass that re-reads every slot at the end of a run runs even when the
tick list is off, because the fixed list is not optional. The debug report names
every entry in it, counts the placements found in each of the two lists, and fails
the run if the randomization introduced one:

```
always blocked       : 18 ability(ies), whatever the blocklist says
                       190 Dragon Effect Null, 191 Monster Effect Null, ... 148
                       Special Dance, 154 Missing Number, 158 Dance
```

As with the tick list, a placement that already existed in the vanilla file is
kept: the Dancer's own Dance, the monster classes' own abilities.

### Always Spell on Rank - D, C, B or A

Every character's spell list starts with one spell written at a fixed rank, so a
character has something to cast as soon as they reach it. That rank used to be
hard wired to **D**, which is what the game itself does. The **Spell Lists**
group on the Characters tab now has a small drop down next to the
*Always Spell on Rank* box:

| Choice | Rank value | When the first spell arrives |
| --- | --- | --- |
| D (default) | 2 | as soon as Reason / Faith hits D |
| C | 4 | at C |
| B | 6 | at B |
| A | 8 | at A |

The spell tables only know half ranks (the tool looks a spell up with
`rank / 2`), so D+ would behave exactly like D and is not offered.

This matters with a total conversion. No Monastery starts every unit in an
intermediate class, which means the rank D that vanilla only reaches after a few
weeks is already there on the first map - so *every* character can cast from
turn one, which is not what the option promises. Pick **C** (or later) to push
the first spell back to a rank that still has to be earned. All three built-in
presets apply at C: the recorded run was a No Monastery run, so it was played at
C and the preset keeps it.

The debug report prints the rank under the run counters
(`first character spell rank : C (rank 4)`), and when mod mode is on while the
rank is still D it adds a warning saying why every unit starts with a spell.

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
| Ability blocklist | names every entry of the fixed list, scans class abilities, monster latent/barrier abilities and character learnset ability slots (combat arts are skipped - the same id means something else there); lists blocked abilities the randomization *introduced* (vanilla ones are reported separately, they are intentional), how many the safety pass replaced and whether the Dancer's own Dance had to be written back |
| Class identity | whether the pass is on, the Def / Res allowance per level band with a clamp count for each band, every Str / Mag trade with before/after values, every class whose Def / Res was pulled back to vanilla + the allowance, and a PASS line when nothing needed fixing |
| Weapon effect text | whether the descriptions are on, whether `msgdata.bin` was loaded, how many weapon descriptions name the effect the run gave them, and a warning if effects were remixed but there is no message table to say so |
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
