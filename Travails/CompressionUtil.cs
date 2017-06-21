using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using TextRuler;

namespace Travails
{
    public static class CompressionUtil
    {
        public static void Zip(string startPath, string zipPath)
        {
            ZipFile.CreateFromDirectory(startPath, zipPath);
        }

        public static void UnZip(string zipPath, string extractPath)
        {
            ZipFile.ExtractToDirectory(zipPath, extractPath);
        }

        public static bool Compress(string documentPath)
        {
            try
            {
                using (FileStream zipToOpen = new FileStream(documentPath.ChangeFileExtension("zip"), FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        ZipArchiveEntry readmeEntry = archive.CreateEntryFromFile(documentPath, documentPath);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
