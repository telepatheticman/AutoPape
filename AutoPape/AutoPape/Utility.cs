using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using Image = System.Windows.Controls.Image;
using BitmapImage = System.Windows.Media.Imaging.BitmapImage;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Net;

namespace AutoPape
{
    public enum imageType
    {
        thumbnail,
        fullImage
    }



    public static class Utility
    {
        public const string fullImagePath = "Full_Images";
        public const string thumbnailPath = "Thumbnails";
        public const string parent = "AutoPape";

        public static string cleanArchiveString(string toClean)
        {
            string clean = toClean;
            Regex rxHTMLElement = new Regex("\\<.*?\\>");
            var elements = rxHTMLElement.Matches(clean);

            clean = cleanHTMLString(clean);
            foreach(var match in elements)
            {
                clean = clean.Replace(match.ToString(), "\n");
            }
            clean = clean.Trim('\n');
            return clean;
        }
        public static string cleanHTMLString(string toClean)
        {
            string clean = toClean;

            clean = clean.Replace("&amp;", "&");
            clean = clean.Replace("&quot;", "\"");
            clean = clean.Replace("&#039;", "'");
            clean = clean.Replace("&lt;", "<");
            clean = clean.Replace("&gt;", ">");

            return clean;
        }

        public static string pathToImage(string board, string thread, string image, imageType type)
        {
            string path = pathToImageDirectory(board, thread, type);
            path = Path.Combine(path, image + ".png");
            return path;
        }

        public static string pathToImageDirectory(string board, string thread, imageType type)
        {
            string path = pathToThreadDirectory(board, thread);
            switch (type)
            {
                case imageType.thumbnail:
                    path = Path.Combine(path, thumbnailPath);
                    break;
                case imageType.fullImage:
                    path = Path.Combine(path, fullImagePath);
                    break;
            }
            return path;
        }

        public static string pathToThreadDirectory(string board, string thread)
        {
            string path = pathToBoardDirectory(board);
            path = Path.Combine(path, thread);
            return path;
        }

        public static string pathToBoardDirectory(string board)
        {
            string path = pathToParent();
            path = Path.Combine(path, board);
            return path;
        }

        public static string pathToParent()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, parent);
            return path;
        }

        public static Image imageFromURL(string url, bool deleted, int attempts = 3)
        {
            Image image = null;
            byte[] imageByte = null;

            WebClient webClient = new WebClient();

            if (!url.Contains("https")) url = "https://" + url;

            if(deleted)
            {
                ImageConverter converter = new ImageConverter();
                imageByte = (byte[])converter.ConvertTo(AutoPape.Properties.Resources.NoImage, typeof(byte[]));
            }
            else
            {
                for(int i = 0; i < attempts; i++)
                {
                    try
                    {
                        imageByte = webClient.DownloadData(url);
                        //var thumbNailTask = client.GetByteArrayAsync(url);
                        //imageByte = thumbNailTask.GetAwaiter().GetResult();
                        break;
                    }
                    catch(Exception ex)
                    {
                        if (i + 1 == attempts) return null;
                        continue;
                    }
                }

            }

            MemoryStream ms = new MemoryStream(imageByte);
            ms.Position = 0;
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                image = new Image();
                System.Windows.Media.Imaging.BitmapImage imageBitmap = new System.Windows.Media.Imaging.BitmapImage();
                imageBitmap.BeginInit();
                imageBitmap.StreamSource = ms;
                imageBitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                imageBitmap.EndInit();

                image.Source = imageBitmap;

            });
            return image;
        }

        public static System.Drawing.Image controlToDrawingImage(Image from)
        {
            if(from == null) return null;
            System.Drawing.Image to = null;

            var ms = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            BitmapImage imageBitmap = ((BitmapImage)from.Source);
            enc.Frames.Add(BitmapFrame.Create(imageBitmap));
            enc.Save(ms);
            Bitmap image = new Bitmap(ms);

            ImageConverter converter = new ImageConverter();

            ms = new System.IO.MemoryStream((byte[])converter.ConvertTo(image, typeof(byte[])));
            to = System.Drawing.Image.FromStream(ms);

            return to;
        }

        public static Image imageFromDisk(string path)
        {
            Image image = null;

            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                image = new Image();
                try
                {
                    image.Source = new BitmapImage(new Uri(path));
                }
                catch(Exception ex)
                {
                    image = null;
                }
            });
            return image;
        }

        public static string imageExtention(string url)
        {
            string extention = "none";

            var items = url.Split('.');
            if (items.Count() > 1) extention = items.Last();

            return extention;
        }

        public static long msToNextHour()
        {
            long ms = 0;

            string now = DateTime.Now.ToString("HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
            int nextHourInt = int.Parse(now.Split(':').First()) + 1;
            nextHourInt = nextHourInt == 24 ? 0 : nextHourInt;
            string nextHour = $"{nextHourInt}:00:00";
            var nowDT = DateTime.Parse(now);
            var nextHourDT = DateTime.Parse(nextHour);
            ms = (long)nextHourDT.Subtract(nowDT).TotalMilliseconds;
            ms = ms < 0 ? 86400000 - Math.Abs(ms) : ms;

            return ms;
        }

        public static long msFromNowToTime(string nextTime)
        {
            long ms = 0;

            string now = DateTime.Now.ToString("HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
            var nowDT = DateTime.Parse(now);
            var nextHourDT = DateTime.Parse(nextTime);
            ms = (long)nextHourDT.Subtract(nowDT).TotalMilliseconds;
            ms = ms < 0 ? 86400000 - Math.Abs(ms) : ms;

            return ms;
        }
        //Should be writen for a bit safer saved thread check
        public static bool validImage(ThreadImage image, MonitorSetting settings)
        {
            //This can be re-writen as a series of &= operations on valid
            if (!settings.useMonitor) return false;
            bool valid = false;
            bool thumbnailCheck = image.resolution <= 1;
            if (settings.orientation != image.orientation) return false;
            double tolerance = thumbnailCheck ? .05 : .02; // +/- .05 if thumbnail, +/- .02 if full
            double ratio = thumbnailCheck ? image.thumbAspectRatio : image.aspectRatio;
            valid |= settings.allowWider && ratio > settings.aspectRatio;
            valid |= settings.allowNarrower && ratio < settings.aspectRatio;
            valid |= ratio < settings.aspectRatio + tolerance && ratio > settings.aspectRatio - tolerance;
            if (!valid) return valid;
            if(thumbnailCheck)
            {
                Image full =
                    imageFromURL(
                        "https://" + image.imageurl,
                        image.imageurl == null);
                if (full == null) return false;
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    image.width = ((BitmapImage)full.Source).PixelWidth;
                    image.height = ((BitmapImage)full.Source).PixelHeight;
                });
            }
            if (!(image.resolution >= settings.minimumResolution)) valid = false;

            if (ratio < settings.aspectRatio + tolerance) settings.mode = fitMode.narrow;
            else if (ratio > settings.aspectRatio - tolerance) settings.mode = fitMode.wide;
            else settings.mode = fitMode.fit;

            return valid;
        }

        public static void Shuffle<T> (this List<T> list)
        {
            Random rand = new Random();
            for(int i = 0; i < 3; i++)
            {
                for(int indexA = 0; indexA < list.Count(); indexA++)
                {
                    int indexB;
                    do
                    {
                        indexB = rand.Next(0, list.Count());
                    } while (indexB == indexA && list.Count() > 1);
                    var temp = list[indexA];
                    list[indexA] = list[indexB];
                    list[indexB] = temp;
                }
            }
        }

        public static string nameFromURL(string url)
        {
            string name = "";
            Regex rxName = new Regex("[0-9]+\\.(jpg|jpeg|png)");
            name = rxName.Match(url).ToString().TrimEnd('.', 'j', 'p', 'e', 'n', 'g');

            return name;
        }

        public static void moveDirectory(string oldDir, string newDir)
        {
            // If newDir contains oldDir, it is a sub directory and should not be used
            // TODO: Display a warning if newDir is a sub directory and do not move

            moveDirectory(new DirectoryInfo(oldDir), new DirectoryInfo(newDir));
        }

        private static void moveDirectory(DirectoryInfo oldDir, DirectoryInfo newDir)
        {
            Directory.CreateDirectory(newDir.FullName);

            foreach(FileInfo info in oldDir.GetFiles())
            {
                if (info.Name != "Settings.xml")
                {
                    try
                    {
                        info.CopyTo(Path.Combine(newDir.FullName, info.Name), true);
                        info.Delete();
                    }
                    catch { };

                }
            }

            foreach(DirectoryInfo oldDirSub in oldDir.GetDirectories())
            {
                if (oldDirSub.Name != "CurrentPaper")
                {
                    DirectoryInfo nextNewDirSub = newDir.CreateSubdirectory(oldDirSub.Name);
                    moveDirectory(oldDirSub, nextNewDirSub);
                }
            }
        }

        public static void deleteOld(string dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            if(dir != pathToParent())
            {
                dirInfo.Delete(true);
                return;
            }
            foreach(DirectoryInfo subInfo in dirInfo.GetDirectories())
            {
                if (subInfo.Name != "CurrentPaper")
                {
                    subInfo.Delete(true);
                }
            }
        }

        public static ulong saveDirectorySize(string dir)
        {
            ulong size = 0;

            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            foreach(var info in dirInfo.GetFiles())
            {
                if(info.Name != "Settings.xml")
                {
                    size += (ulong)info.Length;
                }
            }

            foreach (DirectoryInfo subDirInfo in dirInfo.GetDirectories())
            {
                if(subDirInfo.Name != "CurrentPaper")
                {
                    size += saveDirectorySize(subDirInfo.FullName);
                }
            }

            return size;
        }

        /*public static void setMode(this MonitorSetting monitor, ThreadImage threadImage)
        {
            if (threadImage.aspectRatio < monitor.aspectRatio) monitor.mode = fitMode.narrow;
        }*/

        public static T DeepCopy<T>(T other)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Context = new StreamingContext(StreamingContextStates.Clone);
                formatter.Serialize(ms, other);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }

    }
}
