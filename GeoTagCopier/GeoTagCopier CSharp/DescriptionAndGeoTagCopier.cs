using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using ExifLibrary;
using XperiCode.JpegMetadata;

namespace GeoTagCopier_CSharp
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
            var sourceFiles = GetJpegFiles(_sourceFolder);
            var destFiles = GetJpegFiles(_destFolder);

            var worker = (sender as BackgroundWorker);
            worker.ReportProgress(0);
            foreach (var sourceFile in sourceFiles)
            {
                try
                {
                    var sourceFileName = new FileInfo(sourceFile).Name;
                    if (!destFiles.Exists(f => new FileInfo(f).Name.Equals(sourceFileName))) 
                    {
                        errorFiles.Add(sourceFileName);
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
                worker.ReportProgress(100 / sourceFiles.Count);
            }

            doWorkEventArgs.Result = Tuple.Create(sourceFiles, errorFiles);
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
                int t = (int)IFD.GPS + shift;
                var tag = (ExifTag)t;
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


            //var longitude = exifFile.Properties[ExifTag.GPSLongitude] as GPSLatitudeLongitude;
            //var latitude = exifFile.Properties[ExifTag.GPSLatitude] as GPSLatitudeLongitude;
            //var altitude = exifFile.Properties[ExifTag.GPSAltitude] as ExifSRational;


            //using (var reader = new ExifReader(fileName))
            //{
            //    object longitude, latitude, altitude;
            //    reader.GetTagValue(ExifTags.GPSLatitude, out latitude);
            //    reader.GetTagValue(ExifTags.GPSLongitude, out longitude);
            //    reader.GetTagValue(ExifTags.GPSAltitude, out altitude);

            //    Console.WriteLine("{0}, {1}, {2}", altitude, latitude, longitude);
            //}
        }
    }
}
