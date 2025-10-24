using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment
{
    public class FileUtils
    {
        public static string GetFileHash(string filePath)
        {
            // Exception for directories
            if (Directory.Exists(filePath))
                throw new ArgumentException("File hashing cannot be performed on directories");

            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public static string GetStringHash(string content)
        {
            // Convert the input string to a byte array and compute the hash.
            using (var sha256Hash = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                var hashBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Ensures that all parent directories of the specified file path exist.
        /// 
        /// <para>"C:\MyFolder\SubFolder\file.txt" will create "C:\MyFolder\SubFolder" if it does not exist.</para>
        /// <para>"C:\MyFolder\SubFolder\" will create "C:\MyFolder" if it does not exist.</para>
        /// </summary>
        /// <remarks>If the parent directories of the specified path do not exist, they will be created. 
        /// If the path does not have a parent directory (e.g., it is a root path), no action is taken.</remarks>
        /// <param name="path">The file path for which parent directories should be created.  This must be a valid file path and cannot be
        /// null, empty, or consist only of whitespace.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null, empty, or consists only of whitespace.</exception>
        public static void CreateParentDirectories(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

            // Normalize path by trimming trailing directory separators
            var normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Get parent directory
            var parentDir = Path.GetDirectoryName(normalizedPath);

            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
        }

        public static string? GetContainingFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            // Normalize to full path
            path = Path.GetFullPath(path);

            // If it's a file, return its directory
            if (File.Exists(path))
            {
                return Path.GetDirectoryName(path);
            }

            // If it's a directory, return its parent
            if (Directory.Exists(path))
            {
                return Directory.GetParent(path)?.FullName;
            }

            return null;
        }

        public static bool TryGetContainingFolder(string path, out string containingFolder)
        {
            containingFolder = GetContainingFolder(path)!;
            return containingFolder != null;
        }


        /// <summary>
        /// Deletes the specified file or directory, including all its contents if it is a directory.
        /// </summary>
        /// <remarks>If the specified path is a directory, all its contents, including subdirectories and
        /// files, will be deleted recursively. If the path is a file, the file will be deleted.</remarks>
        /// <param name="path">The path to the file or directory to delete. This can be a relative or absolute path.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null, empty, or consists only of whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified <paramref name="path"/> does not exist.</exception>
        public static void DeletePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                throw new FileNotFoundException($"The specified path does not exist: {path}");
            }
        }

        public static bool PathExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            return File.Exists(path) || Directory.Exists(path);
        }

        /// <summary>
        /// Determines whether the specified path represents a symbolic link.
        /// </summary>
        /// <remarks>This method checks whether the given path points to a symbolic link. If the path does
        /// not exist, the method returns <see langword="false"/>.</remarks>
        /// <param name="path">The file or directory path to check. This cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the specified path is a symbolic link; otherwise, <see langword="false"/>.</returns>
        public static bool IsSymbolicLink(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return false;

            var info = new FileInfo(path);
            return info.LinkTarget != null;
        }

        /// <summary>
        /// Determines whether the specified symbolic link points to the given target path.
        /// </summary>
        /// <remarks>This method first verifies that the specified path is a symbolic link. If it is not,
        /// the method returns <see langword="false"/>. The comparison between the symbolic link's target and the
        /// specified <paramref name="targetPath"/> is case-insensitive.</remarks>
        /// <param name="symlinkPath">The path to the symbolic link. This must not be null, empty, or whitespace.</param>
        /// <param name="targetPath">The target path to compare against the symbolic link's target. This must not be null, empty, or whitespace.</param>
        /// <returns><see langword="true"/> if the symbolic link at <paramref name="symlinkPath"/> points to <paramref
        /// name="targetPath"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="symlinkPath"/> or <paramref name="targetPath"/> is null, empty, or consists only
        /// of whitespace.</exception>
        public static bool SymbolicLinkPointsTo(string symlinkPath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(symlinkPath) || string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("Paths cannot be null or whitespace.");
            if (!IsSymbolicLink(symlinkPath))
                return false;
            var info = new FileInfo(symlinkPath);

            if (string.IsNullOrWhiteSpace(info.LinkTarget))
                return false;

            string path1 = Path.GetFullPath(info.LinkTarget).TrimEnd(Path.DirectorySeparatorChar);
            string path2 = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar);

            return string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        /// <summary>
        /// Creates a hard link from source file to destination file path.
        /// </summary>
        /// <param name="sourceFilePath">The existing source file.</param>
        /// <param name="destinationFilePath">The new hard link to create.</param>
        /// <exception cref="IOException">Thrown if the hard link could not be created.</exception>
        public static void CreateHardLinkFile(string sourceFilePath, string destinationFilePath, bool replaceExisting = false)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException($"Source file '{sourceFilePath}' does not exist.", sourceFilePath);

            if (!replaceExisting && File.Exists(destinationFilePath))
                throw new IOException($"Destination file '${destinationFilePath}' already exists.");

            bool result = CreateHardLink(destinationFilePath, sourceFilePath, IntPtr.Zero);
            if (!result)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new IOException($"Failed to create hard link. Win32 Error Code: {errorCode}");
            }
        }

        /// <summary>
        /// Trims the specified path by removing all segments up to and including the first occurrence of the specified
        /// directory name.
        /// </summary>
        /// <remarks>This method splits the path into segments using both <see
        /// cref="Path.DirectorySeparatorChar"/> and <see cref="Path.AltDirectorySeparatorChar"/>  as delimiters. If the
        /// specified directory name appears multiple times in the path, only the first occurrence is
        /// considered.</remarks>
        /// <param name="path">The full path to be trimmed. This must be a valid file or directory path.</param>
        /// <param name="directoryName">The name of the directory up to which the path should be trimmed. The comparison is case-insensitive.</param>
        /// <returns>A string representing the trimmed path, starting from the segment immediately after the specified directory.
        /// If the directory is not found in the path, the original path is returned unchanged.</returns>
        public static string TrimPathUpToDirectory(string path, string directoryName)
        {
            var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var index = Array.FindIndex(parts, part =>
                string.Equals(part, directoryName, StringComparison.OrdinalIgnoreCase));

            if (index == -1)
                return path; // directory not found, return original

            // Skip up to and including the found directory
            var trimmedParts = parts.Skip(index + 1);

            return Path.Combine(trimmedParts.ToArray());
        }

        private const int BYTES_TO_READ = sizeof(Int64);

        /// <summary>
        /// Compares the contents of two FileStreams for equality.
        /// </summary>
        /// <param name="stream1">The first FileStream to compare. Must be readable and seekable.</param>
        /// <param name="stream2">The second FileStream to compare. Must be readable and seekable.</param>
        /// <returns>True if the streams have the same length and identical content; otherwise, false.</returns>
        public static bool FilesAreEqual(FileStream stream1, FileStream stream2)
        {
            //if (stream1 == null)
            //    throw new ArgumentNullException(nameof(stream1));
            //if (stream2 == null)
            //    throw new ArgumentNullException(nameof(stream2));
            //if (!stream1.CanRead || !stream1.CanSeek)
            //    throw new ArgumentException("stream1 must be readable and seekable.", nameof(stream1));
            //if (!stream2.CanRead || !stream2.CanSeek)
            //    throw new ArgumentException("stream2 must be readable and seekable.", nameof(stream2));

            return StreamsAreEqual(stream1, stream2, BYTES_TO_READ);
        }

        public static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            if (!first.Exists || !second.Exists)
                return false;

            if (first.Length != second.Length)
                return false;

            if (string.Equals(first.FullName, second.FullName, StringComparison.OrdinalIgnoreCase))
                return true;

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                return StreamsAreEqual(fs1, fs2, BYTES_TO_READ);
            }
        }

        public static bool FilesAreEqual(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
                return false;
            FileInfo file1 = new FileInfo(first);
            FileInfo file2 = new FileInfo(second);
            return FilesAreEqual(file1, file2);
        }

        public static bool FilesAreEqual(string first, Stream second)
        {
            if (string.IsNullOrWhiteSpace(first) || second == null || !second.CanRead || !second.CanSeek)
                return false;
            FileInfo file1 = new FileInfo(first);
            if (!file1.Exists)
                return false;
            if (file1.Length != second.Length)
                return false;
            using (FileStream fs1 = file1.OpenRead())
            {
                return StreamsAreEqual(fs1, second, BYTES_TO_READ);
            }
        }

        public static bool FilesAreEqual(Stream first, string second)
        {
            if (string.IsNullOrWhiteSpace(second) || first == null || !first.CanRead || !first.CanSeek)
                return false;
            FileInfo file2 = new FileInfo(second);
            if (!file2.Exists)
                return false;
            if (file2.Length != first.Length)
                return false;
            using (FileStream fs2 = file2.OpenRead())
            {
                return StreamsAreEqual(first, fs2, BYTES_TO_READ);
            }
        }

        /// <summary>
        /// Compares the contents of two streams for equality.
        /// </summary>
        /// <param name="stream1">First stream (must be readable and seekable).</param>
        /// <param name="stream2">Second stream (must be readable and seekable).</param>
        /// <param name="bufferSize">Buffer size for reading.</param>
        /// <returns>True if streams are equal, false otherwise.</returns>
        private static bool StreamsAreEqual(Stream stream1, Stream stream2, int bufferSize)
        {
            if (stream1.Length != stream2.Length)
                return false;

            long originalPosition1 = stream1.Position;
            long originalPosition2 = stream2.Position;

            try
            {
                stream1.Position = 0;
                stream2.Position = 0;

                byte[] buffer1 = new byte[bufferSize];
                byte[] buffer2 = new byte[bufferSize];

                int bytesRead1, bytesRead2;
                do
                {
                    bytesRead1 = stream1.Read(buffer1, 0, bufferSize);
                    bytesRead2 = stream2.Read(buffer2, 0, bufferSize);

                    if (bytesRead1 != bytesRead2)
                        return false;

                    for (int i = 0; i < bytesRead1; i++)
                    {
                        if (buffer1[i] != buffer2[i])
                            return false;
                    }
                } while (bytesRead1 > 0);
            }
            finally
            {
                stream1.Position = originalPosition1;
                stream2.Position = originalPosition2;
            }

            return true;
        }
    }
}
