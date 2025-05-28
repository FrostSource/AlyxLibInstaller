using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
