using System.IO;
using System.Security.Cryptography;

namespace FolderSync
{
    // Extension class for FileInfo
    public static class FileInfoExtension
    {
        // Method to calculate the MD5 hash of a file
        public static byte[] CalculateMD5(this FileInfo file)
        {
            // Create an MD5 hash object
            using (var md5 = MD5.Create())
            // Open the file for reading
            using (var fs = file.OpenRead())
            {
                // Compute and return the MD5 hash of the file
                return md5.ComputeHash(fs);
            }
        }
    }
}