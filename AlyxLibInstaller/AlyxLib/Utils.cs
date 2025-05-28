using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AlyxLibInstaller.AlyxLib;

internal static class Utils
{
    public static string GetFileHash(string filePath)
    {
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