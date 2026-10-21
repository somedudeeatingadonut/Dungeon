# FE3H Randomizer – automatic output organizer

Two tiny batch files that replace the hand-made `create and move` script for
**Fire Emblem Three Houses Randomizer** output.

The randomizer asks for a save location once per file (`fixed_data.bin`,
`msgdata.bin`, `BGM.ktsl2stbin`, …). These scripts take all of those loose
files and drop them into the `romfs` patch layout the game expects, inside a
single `output` folder that you can copy straight onto your SD card or into an
emulator mod folder.

```
fe3h-randomizer-tools\
    Run Randomizer Then Sort.bat      <- start here (double click)
    Organize Randomized Files.bat     <- the sorter on its own
    README.md
```

## Quick start

1. Keep this folder next to `Fire Emblem Three Houses Randomizer.exe`
   (the launcher also finds the exe one folder up).
2. Double click **`Run Randomizer Then Sort.bat`**.
3. The randomizer opens with its working folder already set to
   `fe3h-randomizer-tools\output`. Save every randomized file there.
4. Close the randomizer. The sorter runs automatically and reports what it
   moved.
5. Copy `nx`, `patch1`, `patch3` and `patch4` from `output\` into the `romfs`
   folder of your mod.

If you prefer the manual route, double click **`Organize Randomized Files.bat`**
after saving, or drag the saved files onto it.

## Command line

```bat
"Organize Randomized Files.bat"
    Sorts the files beside the .bat into .\output.
    If that folder is empty, the folder above it is checked too.

"Organize Randomized Files.bat" "D:\where\you\saved"
    Sorts the files in that folder into .\output.

"Organize Randomized Files.bat" "D:\saved\files" "D:\mod\romfs"
    Sorts files from the first folder straight into the second one.

/c    copy instead of move (originals are kept)
/n    preview mode - prints the plan, touches nothing
```

## Where each file goes

| File | Destination |
| --- | --- |
| `BGM.ktsl2stbin` (and any other `*.ktsl2stbin` / `*.ktsl2asbin`) | `nx\sound` |
| `gwscrdata.bin` | `patch1\common\common` |
| `fixed_calendardata.bin` | `patch3\nx\data` |
| `btlscrdata.bin`, `msgdata.bin`, `scrdata.bin`, `tuscrdata.bin` | `patch4\common\common` |
| `INFO0.bin`, `INFO2.bin` | `patch4` |
| `fixed_*.bin` (persondata, classdata, data, growthdata, scenario, shopdata, presentdata, groupworkdata, lobby_common, lobby_activity, lobby_quest, act_data, …) | `patch4\nx\data` |

Anything that is not a randomized output file (`DATA1.bin`, the changelog, …)
is left alone and listed in the console output. `DATA1.bin` is patched in place
by the randomizer itself, so it never needs moving.

## Why this beats the old script

The original `create and move` batch used a chain of `else if exist` tests:

* **It only handled one file per run.** The first matching branch moved its
  file and every later branch was skipped, so a full randomize needed the
  script run about twenty times, once per output file. This sorter handles
  every file in a single pass.
* **It relied on `mkdir` failing silently.** `mkdir` on a folder that already
  exists prints an error; this version only creates what is missing.
* **It used one `move "fixed_*.bin"` catch-all.** That crashes when the
  wildcard matches nothing, and it silently skips files it does not know
  about. Here the catch-all is a real fallback after the known names are
  matched, and unknown files are reported instead of ignored.
* **It had a duplicated `INFO0.bin` branch and no `INFO2.bin` branch.** Both
  info files are now mapped explicitly, in a table that is checked against the
  full list of files the randomizer can write.
* **You had to generate the `.bat` inside the output folder each time.** This
  is a permanent script, and it creates the `output` tree for you.
* **Overwrites were invisible.** Replacing an older copy of a file is reported
  as `[replaced older copy]`, and failures (file locked by another program,
  read-only target) are counted instead of being swallowed.
* **It ran in whatever window was open.** This one finds its own folder, so
  double clicking it always works.

Extras: drag and drop support, `/n` dry run, `/c` copy mode, an optional target
folder (useful for sorting straight into an emulator mod), and a printed tree
of the result.
