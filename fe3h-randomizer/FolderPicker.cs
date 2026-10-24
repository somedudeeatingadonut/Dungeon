// -----------------------------------------------------------------------------
//  FolderPicker.cs
//
//  One folder dialog for the whole tool, the same one the file dialogs use.
//
//  The .NET FolderBrowserDialog is the old tree control: no address bar, no
//  quick access, no way to paste a path, and when the starting folder cannot be
//  shown it drops you somewhere deep (a build folder, or the temp folder Windows
//  makes for a zip that was never extracted). Picking a mod folder that lives a
//  dozen folders down was painful with it.
//
//  This uses the Windows shell's own folder dialog (IFileOpenDialog with
//  FOS_PICKFOLDERS), which is exactly the dialog an OpenFileDialog shows:
//  breadcrumbs, an address bar you can paste into, the sidebar, and a search
//  box. If the shell dialog cannot be created - it needs Vista or newer - it
//  quietly falls back to FolderBrowserDialog, so the button always works.
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    internal static class FolderPicker
    {
        private static readonly char quoteChar = (char)34;

        // Remembered for the rest of the session: the next browse starts where
        // the last one ended, which is what the file dialogs do too.
        private static string lastFolder = "";

        public static string lastPickedFolder()
        {
            return lastFolder;
        }

        // Returns the chosen folder, or null when the user cancelled.
        public static string pick(Control owner, string title, string initialFolder)
        {
            string start = usableFolder(initialFolder);
            if (start.Length == 0)
                start = usableFolder(lastFolder);
            if (start.Length == 0)
                start = usableFolder(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            bool cancelled = false;
            string chosen = null;
            try
            {
                chosen = showShellDialog(owner, title, start, out cancelled);
            }
            catch
            {
                cancelled = false;
            }
            if (chosen == null && !cancelled)
                chosen = showTreeDialog(owner, title, start);
            if (chosen != null)
                lastFolder = chosen;
            return chosen;
        }

        // A folder the dialog can actually open: an existing folder, or the
        // folder a file lives in. Anything else is not worth passing on (the
        // shell dialog would ignore it and land who knows where).
        private static string usableFolder(string folder)
        {
            if (folder == null)
                return "";
            string trimmed = folder.Trim().Trim(quoteChar);
            if (trimmed.Length == 0)
                return "";
            string folder2 = trimmed;
            try
            {
                if (File.Exists(folder2))
                    folder2 = Path.GetDirectoryName(folder2);
            }
            catch
            {
                return "";
            }
            if (folder2 == null || folder2.Length == 0)
                return "";
            try
            {
                if (!Directory.Exists(folder2))
                    return "";
            }
            catch
            {
                return "";
            }
            // A build folder or the temp folder Windows makes for a zip that was
            // never extracted is never where the user keeps a mod or a romfs, and
            // starting there is what made the old dialog feel lost.
            if (isThrowawayFolder(folder2))
                return "";
            return folder2;
        }

        private static bool isThrowawayFolder(string folder)
        {
            if (folder == null)
                return false;
            string lower = folder.ToLowerInvariant().TrimEnd(quoteChar);
            if (lower.IndexOf("appdata" + Path.DirectorySeparatorChar + "local"
                + Path.DirectorySeparatorChar + "temp", StringComparison.Ordinal) >= 0)
                return true;
            if (lower.IndexOf("appdata" + Path.DirectorySeparatorChar + "local"
                + Path.DirectorySeparatorChar + "microsoft" + Path.DirectorySeparatorChar
                + "windows" + Path.DirectorySeparatorChar + "inetcache", StringComparison.Ordinal) >= 0)
                return true;
            if (lower.EndsWith("bin" + Path.DirectorySeparatorChar + "release", StringComparison.Ordinal))
                return true;
            if (lower.EndsWith("bin" + Path.DirectorySeparatorChar + "debug", StringComparison.Ordinal))
                return true;
            return false;
        }

        // ---------------------------------------------------------- shell dialog
        private const uint fosPickFolders = 0x00000020;
        private const uint fosForceFileSystem = 0x00000040;
        private const uint fosNoChangeDir = 0x00000008;
        private const uint fosPathMustExist = 0x00000800;
        private const uint sigdnFileSysPath = 0x80058000;
        private const int errorCancelled = unchecked((int)0x800704C7);

        private static string showShellDialog(Control owner, string title, string start,
            out bool cancelled)
        {
            cancelled = false;

            object dialogObject = null;
            IShellItem startItem = null;
            IShellItem resultItem = null;
            try
            {
                Type type = Type.GetTypeFromCLSID(new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7"), true);
                dialogObject = Activator.CreateInstance(type);
                IFileOpenDialog dialog = (IFileOpenDialog)dialogObject;

                if (title != null && title.Length > 0)
                    dialog.SetTitle(title);
                dialog.SetOkButtonLabel("Select this folder");

                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(options | fosPickFolders | fosForceFileSystem
                    | fosNoChangeDir | fosPathMustExist);

                if (start.Length > 0 && Directory.Exists(start))
                {
                    startItem = createItem(start);
                    if (startItem != null)
                        dialog.SetFolder(startItem);
                }

                int hr = dialog.Show(owner == null ? IntPtr.Zero : owner.Handle);
                if (hr != 0)
                {
                    // 0x800704C7 is "the user cancelled", which is not a failure -
                    // only a real error falls back to the tree dialog below.
                    if (hr == errorCancelled)
                        cancelled = true;
                    return null;
                }

                dialog.GetResult(out resultItem);
                string path = pathOf(resultItem);
                if (path == null || path.Length == 0)
                    return null;
                // Keep the backslash of a drive root ("D:" plus a slash), drop it
                // from every other folder.
                if (path.Length > 3)
                    path = path.TrimEnd('\\');
                return path;
            }
            catch
            {
                return null;
            }
            finally
            {
                release(resultItem);
                release(startItem);
                release(dialogObject);
            }
        }

        private static IShellItem createItem(string path)
        {
            Guid iid = typeof(IShellItem).GUID;
            IShellItem item;
            shellCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out item);
            return item;
        }

        private static string pathOf(IShellItem item)
        {
            if (item == null)
                return null;
            IntPtr pointer;
            item.GetDisplayName(sigdnFileSysPath, out pointer);
            string path = pointer == IntPtr.Zero ? null : Marshal.PtrToStringUni(pointer);
            if (pointer != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pointer);
            return path;
        }

        private static void release(object comObject)
        {
            if (comObject == null)
                return;
            try
            {
                if (Marshal.IsComObject(comObject))
                    Marshal.ReleaseComObject(comObject);
            }
            catch
            {
            }
        }

        // ------------------------------------------------------------ fallback
        // Nothing fancy: the old tree, but at least started at the right folder.
        private static string showTreeDialog(Control owner, string title, string start)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = title;
            dialog.ShowNewFolderButton = true;
            if (start != null && start.Length > 0 && Directory.Exists(start))
                dialog.SelectedPath = start;
            DialogResult result = owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
            if (result != DialogResult.OK)
                return null;
            return dialog.SelectedPath;
        }

        // --------------------------------------------------------- COM plumbing
        // Declared as one flat interface: COM interop wants the base interface's
        // methods listed first, in order, and this is IModalWindow + IFileDialog
        // + IFileOpenDialog in exactly that order.
        [ComImport]
        [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);

            void SetFileTypes(uint count, IntPtr filters);
            void SetFileTypeIndex(uint index);
            void GetFileTypeIndex(out uint index);
            void Advise(IntPtr events, out uint cookie);
            void Unadvise(uint cookie);
            void SetOptions(uint options);
            void GetOptions(out uint options);
            void SetDefaultFolder(IShellItem item);
            void SetFolder(IShellItem item);
            void GetFolder(out IShellItem item);
            void GetCurrentSelection(out IShellItem item);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
            void GetResult(out IShellItem item);
            void AddPlace(IShellItem item, int place);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
            void Close(int result);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr filter);
            void GetResults(out IntPtr items);
            void GetSelectedItems(out IntPtr items);
        }

        [ComImport]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr bindContext, ref Guid handler,
                ref Guid interfaceId, out IntPtr result);
            void GetParent(out IShellItem parent);
            void GetDisplayName(uint form, out IntPtr name);
            void GetAttributes(uint mask, out uint attributes);
            void Compare(IShellItem other, uint hint, out int order);
        }

        [DllImport("shell32.dll", EntryPoint = "SHCreateItemFromParsingName",
            CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void shellCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string path, IntPtr bindContext,
            ref Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out IShellItem item);
    }
}
