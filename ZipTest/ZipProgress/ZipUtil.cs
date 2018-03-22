using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipUtil
{
    /// <summary>
    /// Zip files using standard .Net compression library
    /// Calls are asynchronous and allow optional progress reporting
    /// </summary>
    public static class ZipUtil
    {
        #region PublicMethods

        /// <summary>
        /// Add file (fileName) to Zip archive (archiveName)
        /// Progress is reported to fileProgress and totalProgress
        /// archiveName and fileName must contain full file path
        /// </summary>
        public static async Task<bool> Zip(string archiveName, string fileName,
                                           IProgress<double> fileProgress = null, IProgress<double> totalProgress = null,
                                           bool storePathInfo = false)
        {
            return await PrimitiveZip(archiveName, new List<string> { fileName }, fileProgress, totalProgress, storePathInfo);
        }


        /// <summary>
        /// Add files (fileNames) to Zip archive (archiveName)
        /// Progress is reported to fileProgress and totalProgress
        /// archiveName and fileName must contain full file path
        /// </summary>
        public static async Task<bool> Zip(string archiveName, List<string> fileNames,
                                           IProgress<double> fileProgress = null, IProgress<double> totalProgress = null,
                                           bool storePathInfo = false)
        {
            return await PrimitiveZip(archiveName, fileNames, fileProgress, totalProgress, storePathInfo);
        }
        
        /// <summary>
        /// UnZip a zip file archive
        /// Must specifify the archive name and destination path (destinationDir)
        /// </summary>
        public static async Task<bool> UnZip(string archiveName, string destinationDir,
                                             IProgress<double> fileProgress = null, IProgress<double> totalProgress = null,
                                             bool respectDirs = false)
        {
            using (ZipArchive archive = ZipFile.OpenRead(archiveName))
            {
                long totalBytes = archive.Entries.Sum(e => e.Length);
                long processedBytes = 0;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fileName = ((respectDirs) && (entry.FullName != entry.Name)) ? entry.FullName : Path.Combine(destinationDir, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    using (Stream inputStream = entry.Open())
                    {
                        using (Stream outputStream = File.OpenWrite(fileName))
                            processedBytes = await ProcessStreams(inputStream, outputStream, entry.Length, processedBytes, totalBytes, fileProgress, totalProgress);
                    }
                    File.SetLastWriteTime(fileName, entry.LastWriteTime.LocalDateTime);

                }
            }
            return true;
        }

        #endregion


        #region PrivateMethods

        /// <summary>
        /// Internal Zip method to handle all scenarios and report progress, if progress objects are present
        /// </summary>
        private static async Task<bool> PrimitiveZip(string ZipFilename, List<string> fileNames,
                                                     IProgress<double> fileProgress = null, IProgress<double> totalProgress = null,
                                                     bool storePathInfo = false)
        {
            totalProgress?.Report(0);
            long totalBytes = GetTotalSize(fileNames);
            long processedBytes = 0;
            bool completed = false;
            using (var archive = ZipFile.Open(ZipFilename, GetArchiveMode(ZipFilename)))
            {
                foreach (var file in fileNames)
                {
                    var source = new FileInfo(file);
                    long sourceLength = source.Length;
                    ZipArchiveEntry zip = archive.CreateEntry((storePathInfo) ? source.FullName : source.Name); //Detect stored path or not
                    zip.LastWriteTime = source.LastWriteTime;
                    using (Stream inputStream = File.OpenRead(source.FullName))
                    {
                        using (Stream outputStream = zip.Open())
                            processedBytes = await ProcessStreams(inputStream, outputStream, sourceLength, processedBytes, totalBytes, fileProgress, totalProgress);
                    }
                }
                completed = true;
            }
            return completed;
        }


        private static ZipArchiveMode GetArchiveMode(string archiveName)
        {
            return (File.Exists(archiveName)) ? ZipArchiveMode.Update : ZipArchiveMode.Create;
        }


        private static long GetTotalSize(List<string> fileNames)
        {
            long size = 0;
            foreach (string file in fileNames)
                size += new FileInfo(file).Length;
            return size;
        }

        /// <summary>
        /// Process the zip/unzip stream 4096 bytes at a time
        /// For each iteration, update the progress indicators when they are not null
        /// </summary>
        private static async Task<long> ProcessStreams(Stream inputStream, Stream outputStream,
                                                       long sourceLength, long currentBytes, long totalBytes,
                                                       IProgress<double> fileProgress = null, IProgress<double> totalProgress = null)
        {
            byte[] buffer = new byte[4096];
            long fileBytes = 0;
            fileProgress?.Report(0);
            int byteCounter = 0;
            do
            {
                byteCounter = await Task.Run(() => inputStream.ReadAsync(buffer, 0, buffer.Length));
                fileBytes += byteCounter;
                await Task.Run(() => outputStream.WriteAsync(buffer, 0, byteCounter));
                fileProgress?.Report(Math.Round(fileBytes * 100.0 / sourceLength, 2));
                currentBytes += byteCounter;
                totalProgress?.Report(Math.Round(currentBytes * 100.0 / totalBytes, 2));
            } while (byteCounter > 0);
            return currentBytes;
        }

        #endregion
    }
}
