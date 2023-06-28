
using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GitCredentialManager.Interop.Windows.Native
{
    public static class Kernel32
    {
        private const string LibraryName = "kernel32.dll";

        /// <summary>
        /// Creates or opens a file or I/O device.
        /// <para/>
        /// The most commonly used I/O devices are as follows: file, file stream, directory, physical disk, volume,
        /// console buffer, tape drive, communications resource, mailslot, and pipe.
        /// <para/>
        /// The function returns a handle that can be used to access the file or device for various types of I/O
        /// depending on the file or device and the flags and attributes specified.
        /// <para/>
        /// Return a handle to the file created.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file or device to be created or opened. You may use either forward slashes (/) or
        /// backslashes (\) in this name.
        /// <para/>
        /// In the ANSI version of this function, the name is limited to `MAX_PATH` characters.
        /// <para/>
        /// To extend this limit to 32,767 wide characters, call the Unicode version of the function and prepend
        /// "\\?\" to the path.
        /// </param>
        /// <param name="desiredAccess">
        /// The requested access to the file or device, which can be summarized as read, write, both or neither zero).
        /// <para/>
        /// If this parameter is zero, the application can query certain metadata such as file, directory, or device
        /// attributes without accessing that file or device, even if `<see cref="FileAccess.GenericRead"/>` access
        /// would have been denied.
        /// <para/>
        /// You cannot request an access mode that conflicts with the sharing mode that is specified by the
        /// `<paramref name="shareMode"/>` parameter in an open request that already has an open handle.
        /// </param>
        /// <param name="shareMode">
        /// The requested sharing mode of the file or device, which can be read, write, both, delete, all of these,
        /// or none (refer to the following table).
        /// <para/>
        /// Access requests to attributes or extended attributes are not affected by this flag.
        /// <para/>
        /// If this parameter is zero and CreateFile succeeds, the file or device cannot be shared and cannot be opened
        /// again until the handle to the file or device is closed.
        /// <para/>
        /// You cannot request a sharing mode that conflicts with the access mode that is specified in an existing
        /// request that has an open handle.
        /// <para/>
        /// CreateFile would fail and the `<see cref="Marshal.GetLastWin32Error"/>` function would return
        /// `<see cref="Win32Error.SharingViloation"/>`.
        /// <para/>
        /// To enable a process to share a file or device while another process has the file or device open, use a
        /// compatible combination of one or more of the following values.
        /// </param>
        /// <param name="securityAttributes">
        /// This parameter should be `<see cref="IntPtr.Zero"/>`.
        /// </param>
        /// <param name="creationDisposition">
        /// An action to take on a file or device that exists or does not exist.
        /// <para/>
        /// For devices other than files, this parameter is usually set to
        /// `<see cref="FileCreationDisposition.OpenExisting"/>`.
        /// </param>
        /// <param name="flagsAndAttributes">
        /// The file or device attributes and flags, `<see cref="FileAttributes.Normal"/>` being the most common
        /// default value for files.
        /// <para/>
        /// This parameter can include any combination of `<see cref="FileAttributes"/>`.
        /// <para/>
        /// All other file attributes override `<see cref="FileAttributes.Normal"/>`.
        /// </param>
        /// <param name="templateFile">
        /// This parameter should be `<see cref="IntPtr.Zero"/>`.
        /// </param>
        [DllImport(LibraryName, EntryPoint = "CreateFileW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
            [In, MarshalAs(UnmanagedType.LPWStr)] string fileName,
            [In, MarshalAs(UnmanagedType.U4)] FileAccess desiredAccess,
            [In, MarshalAs(UnmanagedType.U4)] FileShare shareMode,
            [In, MarshalAs(UnmanagedType.SysInt)] IntPtr securityAttributes,
            [In, MarshalAs(UnmanagedType.U4)] FileCreationDisposition creationDisposition,
            [In, MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            [In, MarshalAs(UnmanagedType.SysInt)] IntPtr templateFile);

        /// <summary>
        /// Reads character input from the console input buffer and removes it from the buffer.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="consoleInputHandle">
        /// A handle to the console input buffer. The handle must have the `<see cref="FileAccess.GenericRead"/>`
        /// access right.
        /// </param>
        /// <param name="buffer">
        /// A pointer to a buffer that receives the data read from the console input buffer.
        /// <para/>
        /// The storage for this buffer is allocated from a shared heap for the process that is 64 KB in size.
        /// <para/>
        /// The maximum size of the buffer will depend on heap usage.
        /// </param>
        /// <param name="numberOfCharsToRead">
        /// The number of characters to be read.
        /// <para/>
        /// The size of the buffer pointed to by the `<paramref name="buffer"/>` parameter should be at least `<paramref name="NumberofCharsToRead"/>` * `<see langword="sizeof"/>(<see langword="char"/>)` bytes.
        /// </param>
        /// <param name="numberOfCharsRead">
        /// A pointer to a variable that receives the number of characters actually read.
        /// </param>
        /// <param name="reserved">
        /// Reserved; must be `<see cref="IntPtr.Zero"/>`.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2205:UseManagedEquivalentsOfWin32Api")]
        [DllImport(LibraryName, EntryPoint = "ReadConsoleW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadConsole(
            [In] SafeFileHandle consoleInputHandle,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer,
            [In, MarshalAs(UnmanagedType.U4)] uint numberOfCharsToRead,
            [Out, MarshalAs(UnmanagedType.U4)] out uint numberOfCharsRead,
            [In, MarshalAs(UnmanagedType.SysInt)] IntPtr reserved);

        /// <summary>
        /// Writes a character string to a console screen buffer beginning at the current cursor location.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="consoleOutputHandle">
        /// A handle to the console screen buffer.
        /// <para/>
        /// The handle must have the `<see cref="FileAccess.GenericWrite"/>` access right.
        /// </param>
        /// <param name="buffer">
        /// A pointer to a buffer that contains characters to be written to the console screen buffer.
        /// <para/>
        /// The storage for this buffer is allocated from a shared heap for the process that is 64 KB in size.
        /// <para/>
        /// The maximum size of the buffer will depend on heap usage.
        /// </param>
        /// <param name="numberOfCharsToWrite">
        /// The number of characters to be written.
        /// <para/>
        /// If the total size of the specified number of characters exceeds the available heap, the function fails
        /// with `<see cref="Win32Error.NotEnoughMemory"/>`.
        /// </param>
        /// <param name="numberOfCharsWritten">
        /// A pointer to a variable that receives the number of characters actually written.
        /// </param>
        /// <param name="reserved">
        /// Reserved; must be `<see cref="IntPtr.Zero"/>`.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2205:UseManagedEquivalentsOfWin32Api")]
        [DllImport(LibraryName, EntryPoint = "WriteConsoleW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteConsole(
            [In] SafeHandle consoleOutputHandle,
            [In, MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer,
            [In, MarshalAs(UnmanagedType.U4)] uint numberOfCharsToWrite,
            [Out, MarshalAs(UnmanagedType.U4)] out uint numberOfCharsWritten,
            [In, MarshalAs(UnmanagedType.SysInt)] IntPtr reserved);

        /// <summary>
        /// Retrieves the current input mode of a console's input buffer or the current output mode of a console screen
        /// buffer.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="consoleHandle">
        /// A handle to the console input buffer or the console screen buffer. The handle must have the
        /// <see cref="FileAccess.GenericRead"/> access right.
        /// </param>
        /// <param name="consoleMode">
        /// A pointer to a variable that receives the current mode of the specified buffer.
        /// <para/>
        /// If the `<paramref name="consoleHandle"/>` parameter is an input handle, the mode can be one or more of the
        /// following values.
        /// <para/>
        /// When a console is created, all input modes except `<see cref="ConsoleMode.WindowInput"/>` are enabled by
        /// default.
        /// <para/>
        /// If the `<paramref name="consoleHandle"/>` parameter is a screen buffer handle, the mode can be one or more
        /// of the following values.
        /// <para/>
        /// When a screen buffer is created, both output modes are enabled by default.
        /// </param>
        [DllImport(LibraryName, EntryPoint = "GetConsoleMode", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetConsoleMode(
            [In] SafeFileHandle consoleHandle,
            [Out, MarshalAs(UnmanagedType.U4)] out ConsoleMode consoleMode);

        /// <summary>
        /// Sets the input mode of a console's input buffer or the output mode of a console screen buffer.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="consoleHandle">
        /// A handle to the console input buffer or a console screen buffer.
        /// <para/>
        /// The handle must have the `<see cref="FileAccess.GenericRead"/>` access right.
        /// </param>
        /// <param name="consoleMode">
        /// <para>
        /// The input or output mode to be set.
        /// <para/>
        /// If the `<paramref name="consoleHandle"/>` parameter is an input handle, the mode can be one or more of the
        /// following values.
        /// <para/>
        /// When a console is created, all input modes except `<see cref="ConsoleMode.WindowInput"/>` are enabled by
        /// default.
        /// </para>
        /// <para>
        /// If the `<paramref name="consoleHandle"/>` parameter is a screen buffer handle, the mode can be one or more
        /// of the following values.
        /// <para/>
        /// When a screen buffer is created, both output modes are enabled by default.
        /// </para>
        /// </param>
        [DllImport(LibraryName, EntryPoint = "SetConsoleMode", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetConsoleMode(
            [In] SafeFileHandle consoleHandle,
            [In, MarshalAs(UnmanagedType.U4)] ConsoleMode consoleMode);

        /// <summary>
        /// Retrieves the command-line string for the current process.
        /// </summary>
        /// <returns>The return value is the command-line string for the current process.</returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetCommandLine();

        /// <summary>
        /// Frees the specified local memory object and invalidates its handle.
        /// </summary>
        /// <param name="ptr">
        /// A handle to the local memory object.
        /// This handle is returned by either the LocalAlloc or LocalReAlloc function.
        /// It is not safe to free memory allocated with GlobalAlloc.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is NULL.
        /// <para/>
        /// If the function fails, the return value is equal to a handle to the local memory object.
        /// <para/>
        /// To get extended error information, call GetLastError.
        /// </returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LocalFree(IntPtr ptr);

        /// <summary>
        /// Retrieves the window handle used by the console associated with the calling process.
        /// </summary>
        /// <returns>
        /// The return value is a handle to the window used by the console associated with the calling process or
        /// NULL if there is no such associated console.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetConsoleWindow();
    }

    [Flags]
    public enum FileAccess : uint
    {
        GenericRead = 0x80000000,
        GenericWrite = 0x40000000,
        GenericExecute = 0x20000000,
        GenericAll = 0x10000000,
    }

    [Flags]
    public enum FileAttributes : uint
    {
        /// <summary>
        /// The file is read only.
        /// <para/>
        /// Applications can read the file, but cannot write to or delete it.
        /// </summary>
        Readonly = 0x00000001,

        /// <summary>
        /// The file is hidden. Do not include it in an ordinary directory listing.
        /// </summary>
        Hidden = 0x00000002,

        /// <summary>
        /// The file is part of or used exclusively by an operating system.
        /// </summary>
        System = 0x00000004,

        Directory = 0x00000010,

        /// <summary>
        /// The file should be archived.
        /// <para/>
        /// Applications use this attribute to mark files for backup
        /// or removal.
        /// </summary>
        Archive = 0x00000020,

        Device = 0x00000040,

        /// <summary>
        /// The file does not have other attributes set.
        /// <para/>
        /// This attribute is valid only if used alone.
        /// </summary>
        Normal = 0x00000080,

        /// <summary>
        /// The file is being used for temporary storage.
        /// </summary>
        Temporary = 0x00000100,

        SparseFile = 0x00000200,

        ReparsePoint = 0x00000400,

        Compressed = 0x00000800,

        /// <summary>
        /// The data of a file is not immediately available. This attribute indicates that file data is physically
        /// moved to offline storage.
        /// <para/>
        /// This attribute is used by Remote Storage, the hierarchical storage management software.
        /// <para/>
        /// Applications should not arbitrarily change this attribute.
        /// </summary>
        Offline = 0x00001000,

        NotContentIndexed = 0x00002000,

        /// <summary>
        /// The file or directory is encrypted.
        /// <para/>For a file, this means that all data in the file is encrypted.
        /// <para/>
        /// For a directory, this means that encryption is the default for newly created files and subdirectories.
        /// <para/>
        /// This flag has no effect if <see cref="Archive"/> is also specified.
        /// <para>
        /// This flag is not supported on Home, Home Premium, Starter, or ARM editions of Windows.
        /// </summary>
        Encrypted = 0x00004000,

        FirstPipeInstance = 0x00080000,

        /// <summary>
        /// The file data is requested, but it should continue to be located in remote storage.
        /// <para/>
        /// It should not be transported back to local storage.
        /// <para/>
        /// This flag is for use by remote storage systems.
        /// </summary>
        OpenNoRecall = 0x00100000,

        /// <summary>
        /// Normal reparse point processing will not occur;
        /// `<see cref="CreateFile(string, FileAccess, FileShare, IntPtr, FileCreationDisposition, FileAttributes, IntPtr)"/>`
        /// will attempt to open the reparse point. When a file is opened, a file handle is returned, whether or not
        /// the filter that controls the reparse point is operational.
        /// <para/>
        /// This flag cannot be used with the `<see cref="FileCreationDisposition.CreateAlways"/>` flag.
        /// <para/>
        /// If the file is not a reparse point, then this flag is ignored.
        /// </summary>
        OpenReparsePoint = 0x00200000,

        /// <summary>
        /// The file or device is being opened with session awareness.
        /// <para/>
        /// If this flag is not specified, then per-session devices (such as a redirected USB device) cannot be opened
        /// by processes running in session 0.
        /// <para/>
        /// This flag has no effect for callers not in session 0.
        /// <para/>
        /// This flag is supported only on server editions of Windows.
        /// </summary>
        SessionAware = 0x00800000,

        /// <summary>
        /// Access will occur according to POSIX rules.
        /// <para/>
        /// This includes allowing multiple files with names, differing only in case, for file systems that support
        /// that naming.
        /// <para/>
        /// Use care when using this option, because files created with this flag may not be accessible by applications
        /// that are written for MS-DOS or 16-bit Windows.
        /// </summary>
        PosixSemantics = 0x01000000,

        /// <summary>
        /// The file is being opened or created for a backup or restore operation.
        /// <para/>
        /// The system ensures that the calling process overrides file security checks when the process has
        /// SE_BACKUP_NAME and SE_RESTORE_NAME privileges.
        /// <para/>
        /// You must set this flag to obtain a handle to a directory.
        /// <para/>
        /// A directory handle can be passed to some functions instead of a file handle.
        /// </summary>
        BackupSemantics = 0x02000000,

        /// <summary>
        /// The file is to be deleted immediately after all of its handles are closed, which includes the specified
        /// handle and any other open or duplicated handles.
        /// <para/>
        /// If there are existing open handles to a file, the call fails unless they were all opened with the
        /// `<see cref="FileShare.Delete"/>` share mode.
        /// <para/>
        /// Subsequent open requests for the file fail, unless the `<see cref="FileShare.Delete"/>` share mode is
        /// specified.
        /// </summary>
        DeleteOnClose = 0x04000000,

        /// <summary>
        /// Access is intended to be sequential from beginning to end. The system can use this as a hint to optimize
        /// file caching.
        /// <para/>
        /// This flag should not be used if read-behind (that is, reverse scans) will be used.
        /// <para/>
        /// This flag has no effect if the file system does not support cached I/O and `<see cref="NoBuffering"/>`.
        /// </summary>
        SequentialScan = 0x08000000,

        /// <summary>
        /// Access is intended to be random. The system can use this as a hint to optimize file caching.
        /// <para/>
        /// This flag has no effect if the file system does not support cached I/O and `<see cref="NoBuffering"/>`.
        /// </summary>
        RandomAccess = 0x10000000,

        /// <summary>
        /// The file or device is being opened with no system caching for data reads and writes.
        /// <para/>
        /// This flag does not affect hard disk caching or memory mapped files.
        /// <para/>
        /// There are strict requirements for successfully working with files opened with
        /// `<see cref="CreateFile(string, FileAccess, FileShare, IntPtr, FileCreationDisposition, FileAttributes, IntPtr)"/>`
        /// using the `<see cref="NoBuffering"/>` flag.
        /// </summary>
        NoBuffering = 0x20000000,

        /// <summary>
        /// The file or device is being opened or created for asynchronous I/O.
        /// <para/>
        /// When subsequent I/O operations are completed on this handle, the event specified in the OVERLAPPED
        /// structure will be set to the signaled state.
        /// <para/>
        /// If this flag is specified, the file can be used for simultaneous read and write operations.
        /// <para/>
        /// If this flag is not specified, then I/O operations are serialized, even if the calls to the read and write
        /// functions specify an OVERLAPPED structure.
        /// </summary>
        Overlapped = 0x40000000,

        /// <summary>
        /// Write operations will not go through any intermediate cache, they will go directly to disk.
        /// </summary>
        WriteThrough = 0x80000000,
    }

    public enum FileCreationDisposition : uint
    {
        /// <summary>
        /// Creates a new file, only if it does not already exist.
        /// <para/>
        /// If the specified file exists, the function fails and the last-error code is set to <see cref="Win32Error.FileExists"/>.
        /// <para/>
        /// If the specified file does not exist and is a valid path to a writable location, a new file is created.
        /// </summary>
        New = 1,

        /// <summary>
        /// Creates a new file, always.
        /// <para/>
        /// If the specified file exists and is writable, the function overwrites the file, the function succeeds, and
        /// last-error code is set to <see cref="Win32Error.AlreadExists"/>.
        /// <para/>
        /// If the specified file does not exist and is a valid path, a new file is created, the function succeeds, and
        /// the last-error code is set to zero.
        /// </summary>
        CreateAlways = 2,

        /// <summary>
        /// Opens a file, always.
        /// <para/>
        /// If the specified file exists, the function succeeds and the last-error code is set to
        /// <see cref="Win32Error.AlreadExists"/>.
        /// <para/>
        /// If the specified file does not exist and is a valid path to a writable location, the function creates a
        /// file and the last-error code is set to zero.
        /// </summary>
        OpenExisting = 3,

        /// <summary>
        /// Opens a file or device, only if it exists.
        /// <para/>
        /// If the specified file or device does not exist, the function fails and the last-error code is set to
        /// <see cref="Win32Error.FileNotFound"/>.
        /// </summary>
        OpenAlways = 4,

        /// <summary>
        /// Opens a file and truncates it so that its size is zero bytes, only if it exists.
        /// <para/>
        /// If the specified file does not exist, the function fails and the last-error code is
        /// set to <see cref="Win32Error.FileNotFound"/>.
        /// <para/>
        /// The calling process must open the file with <see cref="FileAccess.GenericWrite"/>.
        /// </summary>
        TruncateExisting = 5
    }

    [Flags]
    public enum FileShare : uint
    {
        /// <summary>
        /// Prevents other processes from opening a file or device if they request delete, read, or write access.
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// Enables subsequent open operations on an object to request read access.
        /// <para/>
        /// Otherwise, other processes cannot open the object if they request read access.
        /// <para/>
        /// If this flag is not specified, but the object has been opened for read access, the function fails.
        /// </summary>
        Read = 0x00000001,

        /// <summary>
        /// Enables subsequent open operations on an object to request write access.
        /// <para/>
        /// Otherwise, other processes cannot open the object if they request write access.
        /// <para/>
        /// If this flag is not specified, but the object has been opened for write access, the
        /// function fails.
        /// </summary>
        Write = 0x00000002,

        /// <summary>
        /// Enables subsequent open operations on an object to request delete access.
        /// <para/>
        /// Otherwise, other processes cannot open the object if they request delete access.
        /// <para/>
        /// If this flag is not specified, but the object has been opened for delete access, the function fails.
        /// </summary>
        Delete = 0x00000004
    }

    [Flags]
        public enum ConsoleMode : uint
        {
            /// <summary>
            /// CTRL+C is processed by the system and is not placed in the input buffer.
            /// <para/>
            /// If the input buffer is being read by
            /// `<see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/>`, other control
            /// keys are processed by the system and are not returned in the ReadConsole buffer.
            /// <para/>
            /// If the <see cref="LineInput"/> mode is also enabled, backspace, carriage return, and line feed
            /// characters are handled by the system.
            /// </summary>
            ProcessedInput = 0x0001,

            /// <summary>
            /// The `<see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/>` function
            /// returns only when a carriage return character is read.
            /// <para/>
            /// If this mode is disabled, the functions return when one or more characters are available.
            /// </summary>
            LineInput = 0x0002,

            /// <summary>
            /// Characters read by the `<see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/>`
            /// function are written to the active screen buffer as they are read.
            /// <para/>
            /// This mode can be used only if the <see cref="LineInput"/> mode is also enabled.
            /// </summary>
            EchoInput = 0x0004,

            /// <summary>
            /// User interactions that change the size of the console screen buffer are reported in the
            /// console's input buffer.
            /// <para/>
            /// Information about these events can be read from the input buffer by applications using the
            /// ReadConsoleInput function, but not by those using `<see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/>`.
            /// </summary>
            WindowInput = 0x0008,

            /// <summary>
            /// If the mouse pointer is within the borders of the console window and the window has the keyboard focus,
            /// mouse events generated by mouse movement and button presses are placed in the input buffer.
            /// <para/>
            /// These events are discarded by
            /// `<see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/>`, even when this
            /// mode is enabled.
            /// </summary>
            MouseInput = 0x0010,

            /// <summary>
            /// When enabled, text entered in a console window will be inserted at the current cursor location and all
            /// text following that location will not be overwritten.
            /// <para/>
            /// When disabled, all following text will be overwritten.
            /// </summary>
            InsertMode = 0x0020,

            /// <summary>
            /// This flag enables the user to use the mouse to select and edit text.
            /// </summary>
            QuickEdit = 0x0040,

            /// <summary>
            /// Characters written by the `<see cref="WriteConsole(SafeHandle, StringBuilder, uint, out uint, IntPtr)"/>`
            /// function or echoed by the ReadFile or ReadConsole function are parsed for ASCII control sequences, and
            /// the correct action is performed.
            /// <para/>
            /// Backspace, tab, bell, carriage return, and line feed characters are processed.
            /// </summary>
            ProcessedOuput = 0x0001,

            /// <summary>
            /// When writing with `<see cref="WriteConsole(SafeHandle, StringBuilder, uint, out uint, IntPtr)"/>` or
            /// echoing with ReadFile or ReadConsole, the cursor moves to the beginning of the next row when it reaches
            /// the end of the current row.
            /// <para/>
            /// This causes the rows displayed in the console window to scroll up automatically when the cursor advances
            /// beyond the last row in the window.
            /// <para/>
            /// It also causes the contents of the console screen buffer to scroll up (discarding the top row of the
            /// console screen buffer) when the cursor advances beyond the last row in the console screen buffer.
            /// <para/>
            /// If this mode is disabled, the last character in the row is overwritten with any subsequent characters.
            /// </summary>
            WrapAtEolOutput = 0x0002,

            AllFlags = ProcessedInput
                     | LineInput
                     | EchoInput
                     | WindowInput
                     | MouseInput
                     | InsertMode
                     | QuickEdit
                     | ProcessedOuput
                     | WrapAtEolOutput,
        }
}
