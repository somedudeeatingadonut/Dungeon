// -----------------------------------------------------------------------------
// RememberedFolders.cs - the folders you picked last time, filled in for you.
//
// The tool used to forget every folder the moment it closed: the game-files
// folder, the output folder, the mod folder and the originals folder all went
// back to the exe's own directory, so every start meant browsing to the same
// places again (and the mod folder had to be found before the mod could be
// checked at all).
//
// This keeps them in one small text file and puts them back on the next start.
// A path that has moved is looked for in the obvious places before it is given
// up on - the same path on another drive letter (a USB disk that came back as
// E: instead of F:) or a folder with the same name beside it - so a remembered
// folder usually keeps working after the drive letters shuffle.
//
// The file is plain text and next to the exe when it is writable, so it travels
// with a copied build; otherwise it goes to %APPDATA%.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class RememberedFolders
    {
        public static string inputFolder = "";
        public static string outputFolder = "";
        public static string modFolder = "";
        public static string originalsFolder = "";
        public static string lastScanFolder = "";
        public static bool includeSubfolders = true;

        // Said once in the batch log, so it is visible what was restored.
        public static string restoredNote = "";

        private static bool loaded;
        private static string storePath;

        // ------------------------------------------------------------------ file
        public static string storeFile()
        {
            if (storePath != null)
                return storePath;

            string beside = "";
            try
            {
                beside = Path.Combine(Application.StartupPath, "remembered folders.txt");
                // A build folder is not somewhere to write state: the user copies
                // the exe out of bin\Release, so the file would be left behind.
                string lower = Application.StartupPath.ToLowerInvariant();
                if (lower.EndsWith("bin\\release", StringComparison.Ordinal)
                    || lower.EndsWith("bin\\debug", StringComparison.Ordinal))
                    beside = "";
            }
            catch
            {
                beside = "";
            }

            if (beside.Length > 0 && canWriteBeside(beside))
            {
                storePath = beside;
                return storePath;
            }

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (appData != null && appData.Length > 0)
                {
                    string folder = Path.Combine(appData, "FE3H Randomizer");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    storePath = Path.Combine(folder, "remembered folders.txt");
                    return storePath;
                }
            }
            catch
            {
            }

            storePath = "";
            return storePath;
        }

        // A folder we may write a 300-byte file into: the exe's own folder when it
        // is a normal unzipped build, never a protected one.
        private static bool canWriteBeside(string path)
        {
            try
            {
                string probe = path + ".probe";
                File.WriteAllText(probe, "x");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ------------------------------------------------------------------ load
        public static void load()
        {
            if (loaded)
                return;
            loaded = true;
            if (modFolder.Length == 0)
                modFolder = ModSupport.modFolder == null ? "" : ModSupport.modFolder.Trim();
            if (originalsFolder.Length == 0)
                originalsFolder = ModSupport.originalFolder == null
                    ? "" : ModSupport.originalFolder.Trim();

            string path = storeFile();
            if (path.Length == 0)
                return;
            try
            {
                if (!File.Exists(path))
                    return;
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                        continue;
                    int equals = line.IndexOf('=');
                    if (equals <= 0)
                        continue;
                    string key = line.Substring(0, equals).Trim().ToLowerInvariant();
                    string value = line.Substring(equals + 1).Trim();
                    switch (key)
                    {
                        case "input":
                            inputFolder = value;
                            break;
                        case "output":
                            outputFolder = value;
                            break;
                        case "mod":
                            modFolder = value;
                            break;
                        case "originals":
                            originalsFolder = value;
                            break;
                        case "lastscan":
                            lastScanFolder = value;
                            break;
                        case "subfolders":
                            includeSubfolders = value != "False";
                            break;
                    }
                }
            }
            catch
            {
                return;
            }

            // A path that no longer exists is looked for once; whatever comes back
            // is what the boxes get, so a moved install costs one start, not a
            // browse dialog per field.
            string note = describeRestored();
            if (note.Length > 0)
                restoredNote = note;
        }

        // Applies the remembered paths to the live state (mod folder, originals
        // folder). Called once at window load, after load().
        public static void applyToRuntime()
        {
            if (modFolder.Length > 0 && (ModSupport.modFolder == null || ModSupport.modFolder.Trim().Length == 0))
                ModSupport.modFolder = modFolder;
            if (originalsFolder.Length > 0
                && (ModSupport.originalFolder == null || ModSupport.originalFolder.Trim().Length == 0))
                ModSupport.originalFolder = originalsFolder;
            if (lastScanFolder.Length > 0
                && (ModSupport.lastScanFolder == null || ModSupport.lastScanFolder.Trim().Length == 0))
                ModSupport.lastScanFolder = lastScanFolder;
        }

        private static string describeRestored()
        {
            StringBuilder text = new StringBuilder();
            addIfSet(text, "game files", inputFolder);
            addIfSet(text, "output", outputFolder);
            addIfSet(text, "mod", modFolder);
            addIfSet(text, "originals", originalsFolder);
            return text.ToString();
        }

        private static void addIfSet(StringBuilder text, string label, string path)
        {
            if (path == null || path.Trim().Length == 0)
                return;
            if (text.Length > 0)
                text.Append("; ");
            text.Append(label).Append(": ").Append(path);
        }

        // ------------------------------------------------------------------ save
        public static void save()
        {
            string path = storeFile();
            if (path.Length == 0)
                return;
            try
            {
                StringBuilder text = new StringBuilder();
                text.AppendLine("# FE3H randomizer - the folders you picked last time.");
                text.AppendLine("# Delete this file to start from nothing again.");
                text.AppendLine("input=" + (inputFolder == null ? "" : inputFolder.Trim()));
                text.AppendLine("output=" + (outputFolder == null ? "" : outputFolder.Trim()));
                text.AppendLine("mod=" + (modFolder == null ? "" : modFolder.Trim()));
                text.AppendLine("originals=" + (originalsFolder == null ? "" : originalsFolder.Trim()));
                text.AppendLine("lastscan=" + (lastScanFolder == null ? "" : lastScanFolder.Trim()));
                text.AppendLine("subfolders=" + (includeSubfolders ? "True" : "False"));
                File.WriteAllText(path, text.ToString());
            }
            catch
            {
            }
        }

        // ----------------------------------------------------------- remembering
        public static void rememberInput(string folder)
        {
            folder = resolve(folder);
            if (folder == inputFolder)
                return;
            inputFolder = folder;
            save();
        }

        public static void rememberOutput(string folder)
        {
            folder = resolve(folder);
            if (folder == outputFolder)
                return;
            outputFolder = folder;
            save();
        }

        public static void rememberMod(string folder)
        {
            folder = resolve(folder);
            if (folder == modFolder)
                return;
            modFolder = folder;
            save();
        }

        public static void rememberOriginals(string folder)
        {
            folder = resolve(folder);
            if (folder == originalsFolder)
                return;
            originalsFolder = folder;
            save();
        }

        public static void rememberScan(string folder)
        {
            folder = resolve(folder);
            if (folder.Length > 0)
            {
                inputFolder = folder;
                lastScanFolder = folder;
            }
            save();
        }

        // Adopts whatever the live ModSupport state holds, so a folder picked in a
        // dialog that did not go through the helpers above is still kept.
        public static void captureFromRuntime()
        {
            bool changed = false;
            string mod = ModSupport.modFolder == null ? "" : ModSupport.modFolder.Trim();
            if (mod.Length > 0 && mod != modFolder)
            {
                modFolder = mod;
                changed = true;
            }
            string originals = ModSupport.originalFolder == null ? "" : ModSupport.originalFolder.Trim();
            if (originals.Length > 0 && originals != originalsFolder)
            {
                originalsFolder = originals;
                changed = true;
            }
            string scan = ModSupport.lastScanFolder == null ? "" : ModSupport.lastScanFolder.Trim();
            if (scan.Length > 0 && scan != lastScanFolder)
            {
                lastScanFolder = scan;
                inputFolder = scan;
                changed = true;
            }
            if (changed)
                save();
        }

        // -------------------------------------------------------------- resolving
        // A remembered folder that is not there any more, looked for in the places
        // it plausibly moved to. Returns "" when nothing usable is found.
        public static string resolve(string path)
        {
            if (path == null)
                return "";
            string trimmed = path.Trim();
            if (trimmed.Length == 0)
                return "";
            try
            {
                if (Directory.Exists(trimmed))
                    return trimmed;
                if (File.Exists(trimmed))
                {
                    string dir = Path.GetDirectoryName(trimmed);
                    return dir == null ? "" : dir;
                }
            }
            catch
            {
                return "";
            }

            // 1. the same path on another drive: the drive letter shuffled.
            try
            {
                string tail = null;
                if (trimmed.Length > 2 && trimmed[1] == ':')
                    tail = trimmed.Substring(2);
                if (tail != null)
                {
                    string[] drives = Directory.GetLogicalDrives();
                    for (int i = 0; i < drives.Length; i++)
                    {
                        string candidate = drives[i].TrimEnd('\\') + tail;
                        if (Directory.Exists(candidate))
                            return candidate;
                    }
                }
            }
            catch
            {
            }

            // 2. a folder with the same name under the same parent (the parent
            //    was renamed), or one level up.
            try
            {
                string leaf = Path.GetFileName(trimmed.TrimEnd('\\'));
                string parent = Path.GetDirectoryName(trimmed.TrimEnd('\\'));
                if (leaf != null && leaf.Length > 0 && parent != null)
                {
                    string[] levels = { parent, Path.GetDirectoryName(parent) };
                    for (int i = 0; i < levels.Length; i++)
                    {
                        if (levels[i] == null || levels[i].Length == 0 || !Directory.Exists(levels[i]))
                            continue;
                        string[] children = Directory.GetDirectories(levels[i]);
                        for (int j = 0; j < children.Length; j++)
                        {
                            if (string.Equals(Path.GetFileName(children[j]), leaf,
                                StringComparison.OrdinalIgnoreCase))
                                return children[j];
                        }
                    }
                }
            }
            catch
            {
            }

            return "";
        }

        // Kept for the debug report: where the file ended up.
        public static string storePathText()
        {
            string path = storeFile();
            return path.Length == 0 ? "(not writable - folders will not be remembered)" : path;
        }
    }
}
