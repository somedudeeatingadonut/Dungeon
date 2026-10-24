# Dungeon
A dungeon builder/manager/roguelike game.

## Fire Emblem Three Houses Randomizer

`Fire Emblem Three Houses Randomizer.exe` is the standalone FE3H randomizer.

[`fe3h-randomizer/`](fe3h-randomizer/README.md) holds its buildable source with
these features added to the program:

* **Scan folder and load all files** - one folder scan loads every input file
  the randomizer needs instead of twenty separate file dialogs.
* **Randomize and save to output folder** - every randomized file is written
  straight into the `romfs` patch layout (`nx\sound`, `patch1\common\common`,
  `patch3\nx\data`, `patch4`, `patch4\common\common`, `patch4\nx\data`).
* **Extra options** - even spread of random classes across weapon types (with a
  safety dial, applied to the class weapon types themselves), an ability
  blocklist for dragon/monster abilities, an enemy siege tome / spell range
  limit with a bypass chance, and a level scaling growth shift from
  Str/Mag/Def/Res into HP.
* **Total conversion mod support** - data-file mods such as *No Monastery 1.3.3*
  get their table offsets lined up from their own file headers; the newer
  *No Monastery 2.1 "with Aldebaran"*, which ships no game data at all and works
  as a Skyline plugin, is recognised from its folder and needs none.
* **Standard Windows folder picking** - every Browse button opens the shell's own
  folder dialog (address bar, quick access, paste a path), not the old tree
  control, and starts at the folder you were last working in.
* **Debug report** - `FE3H Randomizer Debug.txt` with the loaded files, active
  options, per-feature run counters and PASS/WARN/FAIL checks of the output data.
* Layout and painting tweaks so switching tabs is no longer laggy.

Download the whole project in one file from the
[v1.2.0 release](https://github.com/somedudeeatingadonut/Dungeon/releases/latest)
("Source code (zip)").

Run `fe3h-randomizer\build.bat` on Windows (Visual Studio 2022 Community or the
.NET SDK with the 4.7.2 targeting pack) to produce the exe.
