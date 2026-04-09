using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace StartMenuMGR.Services;

/// <summary>
/// .lnk ファイルを COM 経由で直接読み取るためのヘルパー（COMReference不要）
/// </summary>
public static class ShellLinkReader
{
    public static (string targetPath, string description, string arguments, string workingDir)? ReadShortcut(string lnkPath)
    {
        try
        {
            var shellLink = (IShellLinkW)new CShellLink();
            var persistFile = (IPersistFile)shellLink;
            persistFile.Load(lnkPath, 0);

            var targetPath = new StringBuilder(260);
            shellLink.GetPath(targetPath, targetPath.Capacity, IntPtr.Zero, 0);

            var description = new StringBuilder(1024);
            shellLink.GetDescription(description, description.Capacity);

            var arguments = new StringBuilder(1024);
            shellLink.GetArguments(arguments, arguments.Capacity);

            var workingDir = new StringBuilder(260);
            shellLink.GetWorkingDirectory(workingDir, workingDir.Capacity);

            Marshal.ReleaseComObject(shellLink);

            return (targetPath.ToString(), description.ToString(),
                    arguments.ToString(), workingDir.ToString());
        }
        catch
        {
            return null;
        }
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class CShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
            int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
            int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
