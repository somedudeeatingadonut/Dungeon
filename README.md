# Dungeon
A dungeon builder/manager/roguelike game.

## Fire Emblem Three Houses Randomizer

`Fire Emblem Three Houses Randomizer.exe` is the standalone FE3H randomizer.

[`fe3h-randomizer/`](fe3h-randomizer/README.md) holds its buildable source with
two features added to the program:

* **Scan folder and load all files** - one folder scan loads every input file
  the randomizer needs instead of twenty separate file dialogs.
* **Randomize and save to output folder** - every randomized file is written
  straight into the `romfs` patch layout (`nx\sound`, `patch1\common\common`,
  `patch3\nx\data`, `patch4`, `patch4\common\common`, `patch4\nx\data`).

Run `fe3h-randomizer\build.bat` on Windows (Visual Studio 2022 Community or the
.NET SDK with the 4.7.2 targeting pack) to produce the exe.
