using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Goheer.EXIF;

namespace SortPhotos
{
    /// <summary>
    /// Console application to organise digital photos into folders according to date taken.
    /// </summary>
    /// <author>Tim Booker</author>
    /// <date>14/01/2012</date>
    class Program
    {
        static void Main(string[] args)
        {
            //var inputDirectory = @"D:\Pictures\Test\Source";
            var inputDirectory = @"D:\Pictures\_Photos to sort";
            //var inputDirectory = @"C:\Users\Tim\Desktop\temp";
            var outputDirectory = @"D:\Pictures\Digital Photos";

            var inputFiles  = Directory.GetFiles(inputDirectory, "*.*", SearchOption.AllDirectories);

            foreach (var file in inputFiles)
            {
                try
                {
                    var bmp = new Bitmap(file);

                    var exifData = new EXIFextractor(ref bmp, "\n");

                    var stringDate = exifData["DTOrig"] as string;

                    if (stringDate == null)
                        stringDate = exifData["Date Time"] as string;

                    bmp.Dispose();

                    var dateTime = DateTime.MinValue;

                    if (stringDate == null)
                    {
                        // Attempt to read date from filename

                        var dateRegex1 = new Regex("(?<year>20[0-9]{2})_(?<month>[0-9]{2})(?<day>[0-9]{2})_(?<hour>[0-9]{2})(?<minute>[0-9]{2})(?<second>[0-9]{2})");
                        var dateRegex2 = new Regex("(?<year>20[0-9]{2})(?<month>[0-9]{2})(?<day>[0-9]{2})-(?<hour>[0-9]{2})(?<minute>[0-9]{2})(?<second>[0-9]{2})");

                        if (dateRegex1.IsMatch(file))
                        {
                            dateTime = DateTime.ParseExact(dateRegex1.Match(file).Value, "yyyy_MMdd_HHmmss",
                                System.Globalization.CultureInfo.CurrentCulture);
                        }
                        else if (dateRegex2.IsMatch(file))
                        {
                            dateTime = DateTime.ParseExact(dateRegex2.Match(file).Value, "yyyyMMdd-HHmmss",
                                System.Globalization.CultureInfo.CurrentCulture);
                        }
                        else
                        {
                            //var errorDirectory = string.Format(@{0}\Errors", outputDirectory);
                            //if (!Directory.Exists(errorDirectory))
                            //{
                            //    Console.WriteLine("Creating directory: {0}", errorDirectory);
                            //    Directory.CreateDirectory(errorDirectory);
                            //}

                            //Console.WriteLine("No date found in file: {0}", file);

                            //var errorFile = string.Format(@"{0}\{1}", errorDirectory, new FileInfo(file).Name);

                            //if (!File.Exists(errorFile))
                            //    File.Copy(file, errorFile);

                            continue;
                        }
                    }

                   if(dateTime == DateTime.MinValue)
                       dateTime = DateTime.ParseExact(stringDate, "yyyy:MM:dd HH:mm:ss\0",
                        System.Globalization.CultureInfo.CurrentCulture);

                    var datedDirectory = string.Format(@"{0}\{1}\{2}\", outputDirectory, dateTime.ToString("yyyy"), dateTime.ToString("MM"));
                    if (!Directory.Exists(datedDirectory))
                    {
                        Console.WriteLine("Creating directory: {0}", datedDirectory);
                        Directory.CreateDirectory(datedDirectory);
                    }

                    var attempt = 1;
                    while (true)
                    {
                        //var datedFile = string.Format("{0}_{1}.jpg", dateTime.ToString(@"yyyy-MM-dd_HH-mm-ss"), attempt.ToString("00"));

                        var suffix = string.Empty;
                        if (attempt > 1)
                            suffix = string.Format("-{0}", attempt);

                        var datedFile = string.Format("{0}{1}{2}", dateTime.ToString(@"yyyyMMdd-HHmmss"), suffix, new FileInfo(file).Extension).ToLower();

                        var outputFile = string.Concat(datedDirectory, datedFile);

                        if (!File.Exists(outputFile))
                        {
                            Console.WriteLine("Moving file: {0} to {1}", file, outputFile);

                            //File.Copy(file, outputFile);
                            File.Move(file, outputFile);

                            break;
                        }
                        else
                        {
                            var checksum1 = GetChecksum(file);
                            var checksum2 = GetChecksum(outputFile);

                            if (checksum1 == checksum2)
                            {
                                //Console.WriteLine("File already exists: {0} is identical to {1}", file, outputFile);
                                
                                File.Delete(file);

                                break;
                            }
                            attempt++;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR PROCESSING FILE: {0}", file);
                    Console.WriteLine(e.Message);
                }
            }

            Console.WriteLine("Deleting empty directories.");

            DeleteEmptyDirectories(inputDirectory);

            Console.WriteLine("Done.");
            Console.ReadLine();

        }

        private static string GetChecksum(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        private static void DeleteEmptyDirectories(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteEmptyDirectories(directory);
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

    }
}
