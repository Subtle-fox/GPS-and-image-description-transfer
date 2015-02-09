using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using ExifLibrary;
using XperiCode.JpegMetadata;

namespace GeoTagCopier
{
    class DescriptionAndGeoTagCopier
    {
        private readonly string _sourceFolder;
        private readonly string _destFolder;

        public DescriptionAndGeoTagCopier(string sourceFolder, string destFolder)
        {
            _sourceFolder = sourceFolder;
            _destFolder = destFolder;
        }
        
        public void Run(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            var errorFiles = new List<string>();
            var skippedFiles = new List<string>();
            var sourceFiles = GetJpegFiles(_sourceFolder);
            var destFiles = GetJpegFiles(_destFolder);

            var worker = (sender as BackgroundWorker);
            var progress = 0;
            worker.ReportProgress(progress);
            foreach (var sourceFile in sourceFiles)
            {
                try
                {
                    var sourceFileName = new FileInfo(sourceFile).Name;
                    if (!destFiles.Exists(f => new FileInfo(f).Name.Equals(sourceFileName)))
                    {
                        skippedFiles.Add(sourceFileName);
                        continue;
                    }

                    var destFile = Path.Combine(_destFolder, sourceFileName);
                    CopyGeoTags(sourceFile, destFile);
                    ChangeDescription(sourceFile, destFile);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    errorFiles.Add(new FileInfo(sourceFile).Name);
                }
                finally
                {
                    progress++;
                    worker.ReportProgress(100 * progress / sourceFiles.Count);
                }
            }

            doWorkEventArgs.Result = Tuple.Create(sourceFiles, skippedFiles, errorFiles);
        }

        List<string> GetJpegFiles(string folder)
        {
            var files = Directory.GetFiles(folder);
            return files.Where(IsJpeg).ToList();
        }

        private static bool IsJpeg(string fileName)
        {
            var extension = new FileInfo(fileName).Extension.ToLower();
            return extension == ".jpg" || extension == ".jpeg";
        }


        static void ChangeDescription(string sourceFile, string destFile)
        {
            var sourceMetadata = new JpegMetadataAdapter(sourceFile).Metadata;
            var destAdapter = new JpegMetadataAdapter(destFile);
            var destMetadata = destAdapter.Metadata;
            destMetadata.Subject = sourceMetadata.Subject;
            destMetadata.Title = sourceMetadata.Title;
            destMetadata.Comments = sourceMetadata.Comments;
            destMetadata.Keywords = sourceMetadata.Keywords;
            destMetadata.Rating = sourceMetadata.Rating;

            destAdapter.Save();
        }

        static void CopyGeoTags(string fileName, string destName)
        {
            var sourceExifFile = ExifFile.Read(fileName);
            var destExifFile = ExifFile.Read(destName);
            for (int shift = 0; shift < 31; shift++)
            {
                var tag = (ExifTag)((int)IFD.GPS + shift);
                if (sourceExifFile.Properties.ContainsKey(tag))
                {
                    var gpsValue = sourceExifFile.Properties[tag];
                    destExifFile.Properties[tag] = gpsValue;
                }
            }
            var tmpName = destName + "_tmp";
            destExifFile.Save(tmpName, false);
            File.Delete(destName);
            File.Move(tmpName, destName);
        }
    }
}
