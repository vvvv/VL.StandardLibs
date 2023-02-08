using System;
using VL.Core;

namespace VL.Lib.IO
{
    //directly copied from mscorlib /system.dll until import works
    public enum SpecialFolder
    {
        //
        // Summary:
        //     The logical Desktop rather than the physical file system location.
        Desktop = 0,
        //
        // Summary:
        //     The directory that contains the user's program groups.
        Programs = 2,
        //
        // Summary:
        //     The directory that serves as a common repository for documents.
        Personal = 5,
        //
        // Summary:
        //     The My Documents folder.
        MyDocuments = 5,
        //
        // Summary:
        //     The directory that serves as a common repository for the user's favorite items.
        Favorites = 6,
        //
        // Summary:
        //     The directory that corresponds to the user's Startup program group.
        Startup = 7,
        //
        // Summary:
        //     The directory that contains the user's most recently used documents.
        Recent = 8,
        //
        // Summary:
        //     The directory that contains the Send To menu items.
        SendTo = 9,
        //
        // Summary:
        //     The directory that contains the Start menu items.
        StartMenu = 11,
        //
        // Summary:
        //     The My Music folder.
        MyMusic = 13,
        //
        // Summary:
        //     The file system directory that serves as a repository for videos that belong
        //     to a user. Added in the .NET Framework 4.
        MyVideos = 14,
        //
        // Summary:
        //     The directory used to physically store file objects on the desktop.
        DesktopDirectory = 16,
        //
        // Summary:
        //     The My Computer folder.
        MyComputer = 17,
        //
        // Summary:
        //     A file system directory that contains the link objects that may exist in the
        //     My Network Places virtual folder. Added in the .NET Framework 4.
        NetworkShortcuts = 19,
        //
        // Summary:
        //     A virtual folder that contains fonts. Added in the .NET Framework 4.
        Fonts = 20,
        //
        // Summary:
        //     The directory that serves as a common repository for document templates.
        Templates = 21,
        //
        // Summary:
        //     The file system directory that contains the programs and folders that appear
        //     on the Start menu for all users. This special folder is valid only for Windows
        //     NT systems. Added in the .NET Framework 4.
        CommonStartMenu = 22,
        //
        // Summary:
        //     A folder for components that are shared across applications. This special folder
        //     is valid only for Windows NT, Windows 2000, and Windows XP systems. Added in
        //     the .NET Framework 4.
        CommonPrograms = 23,
        //
        // Summary:
        //     The file system directory that contains the programs that appear in the Startup
        //     folder for all users. This special folder is valid only for Windows NT systems.
        //     Added in the .NET Framework 4.
        CommonStartup = 24,
        //
        // Summary:
        //     The file system directory that contains files and folders that appear on the
        //     desktop for all users. This special folder is valid only for Windows NT systems.
        //     Added in the .NET Framework 4.
        CommonDesktopDirectory = 25,
        //
        // Summary:
        //     The directory that serves as a common repository for application-specific data
        //     for the current roaming user.
        ApplicationData = 26,
        //
        // Summary:
        //     The file system directory that contains the link objects that can exist in the
        //     Printers virtual folder. Added in the .NET Framework 4.
        PrinterShortcuts = 27,
        //
        // Summary:
        //     The directory that serves as a common repository for application-specific data
        //     that is used by the current, non-roaming user.
        LocalApplicationData = 28,
        //
        // Summary:
        //     The directory that serves as a common repository for temporary Internet files.
        InternetCache = 32,
        //
        // Summary:
        //     The directory that serves as a common repository for Internet cookies.
        Cookies = 33,
        //
        // Summary:
        //     The directory that serves as a common repository for Internet history items.
        History = 34,
        //
        // Summary:
        //     The directory that serves as a common repository for application-specific data
        //     that is used by all users.
        CommonApplicationData = 35,
        //
        // Summary:
        //     The Windows directory or SYSROOT. This corresponds to the %windir% or %SYSTEMROOT%
        //     environment variables. Added in the .NET Framework 4.
        Windows = 36,
        //
        // Summary:
        //     The System directory.
        System = 37,
        //
        // Summary:
        //     The program files directory.On a non-x86 system, passing System.Environment.SpecialFolder.ProgramFiles
        //     to the System.Environment.GetFolderPath(System.Environment.SpecialFolder) method
        //     returns the path for non-x86 programs. To get the x86 program files directory
        //     on a non-x86 system, use the System.Environment.SpecialFolder.ProgramFilesX86
        //     member.
        ProgramFiles = 38,
        //
        // Summary:
        //     The My Pictures folder.
        MyPictures = 39,
        //
        // Summary:
        //     The user's profile folder. Applications should not create files or folders at
        //     this level; they should put their data under the locations referred to by System.Environment.SpecialFolder.ApplicationData.
        //     Added in the .NET Framework 4.
        UserProfile = 40,
        //
        // Summary:
        //     The Windows System folder. Added in the .NET Framework 4.
        SystemX86 = 41,
        //
        // Summary:
        //     The Program Files folder. Added in the .NET Framework 4. On an x86 system, passing
        //     the System.Environment.SpecialFolder.ProgramFilesX86 member to the System.Environment.GetFolderPath(System.Environment.SpecialFolder)
        //     method returns System.String.Empty; use the System.Environment.SpecialFolder.ProgramFiles
        //     member instead. You can determine whether Windows is a 32-bit operating system
        //     by calling the System.Environment.Is64BitOperatingSystem property.
        ProgramFilesX86 = 42,
        //
        // Summary:
        //     The directory for components that are shared across applications.To get the x86
        //     common program files directory on a non-x86 system, use the System.Environment.SpecialFolder.ProgramFilesX86
        //     member.
        CommonProgramFiles = 43,
        //
        // Summary:
        //     The Program Files folder. Added in the .NET Framework 4.
        CommonProgramFilesX86 = 44,
        //
        // Summary:
        //     The file system directory that contains the templates that are available to all
        //     users. This special folder is valid only for Windows NT systems. Added in the
        //     .NET Framework 4.
        CommonTemplates = 45,
        //
        // Summary:
        //     The file system directory that contains documents that are common to all users.
        //     This special folder is valid for Windows NT systems, Windows 95, and Windows
        //     98 systems with Shfolder.dll installed. Added in the .NET Framework 4.
        CommonDocuments = 46,
        //
        // Summary:
        //     The file system directory that contains administrative tools for all users of
        //     the computer. Added in the .NET Framework 4.
        CommonAdminTools = 47,
        //
        // Summary:
        //     The file system directory that is used to store administrative tools for an individual
        //     user. The Microsoft Management Console (MMC) will save customized consoles to
        //     this directory, and it will roam with the user. Added in the .NET Framework 4.
        AdminTools = 48,
        //
        // Summary:
        //     The file system directory that serves as a repository for music files common
        //     to all users. Added in the .NET Framework 4.
        CommonMusic = 53,
        //
        // Summary:
        //     The file system directory that serves as a repository for image files common
        //     to all users. Added in the .NET Framework 4.
        CommonPictures = 54,
        //
        // Summary:
        //     The file system directory that serves as a repository for video files common
        //     to all users. Added in the .NET Framework 4.
        CommonVideos = 55,
        //
        // Summary:
        //     The file system directory that contains resource data. Added in the .NET Framework
        //     4.
        Resources = 56,
        //
        // Summary:
        //     The file system directory that contains localized resource data. Added in the
        //     .NET Framework 4.
        LocalizedResources = 57,
        //
        // Summary:
        //     This value is recognized in Windows Vista for backward compatibility, but the
        //     special folder itself is no longer used. Added in the .NET Framework 4.
        CommonOemLinks = 58,
        //
        // Summary:
        //     The file system directory that acts as a staging area for files waiting to be
        //     written to a CD. Added in the .NET Framework 4.
        CDBurning = 59
    }
}
