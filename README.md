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
  safety dial), an ability blocklist for dragon/monster abilities, an enemy
  siege tome / long range limit, and a level scaling growth shift from
  Str/Mag/Def/Res into HP.

Download the whole project in one file from the
[v1.2.0 release](https://github.com/somedudeeatingadonut/Dungeon/releases/tag/v1.2.0)
("Source code (zip)").

Run `fe3h-randomizer\build.bat` on Windows (Visual Studio 2022 Community or the
.NET SDK with the 4.7.2 targeting pack) to produce the exe.
