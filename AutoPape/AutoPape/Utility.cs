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

        public static Image imageFromURL(string url, HttpClient client, bool deleted, int attempts = 3)
        {
            Image image = null;
            byte[] imageByte = null;

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
                        var thumbNailTask = client.GetByteArrayAsync(url);
                        imageByte = thumbNailTask.GetAwaiter().GetResult();
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
            System.Drawing.Image to = null;

            var ms = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            BitmapImage imageBitmap = ((BitmapImage)from.Source);
            enc.Frames.Add(BitmapFrame.Create(imageBitmap));
            enc.Save(ms);
            Bitmap image = new Bitmap(ms);
            //Bitmap image = new Bitmap()
            

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
                image.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(path));
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
        public static bool validImage(ThreadImage image, MonitorSetting settings, HttpClient client)
        {
            //This can be re-writen as a series of &= operations on valid
            bool valid = false;
            bool thumbnailCheck = image.resolution <= 1;
            if (!(settings.orientation == image.orientation)) return false;
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
                        client,
                        image.imageurl == null);
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    image.width = ((BitmapImage)full.Source).PixelWidth;
                    image.height = ((BitmapImage)full.Source).PixelHeight;
                });
            }
            if (!(image.resolution >= settings.minimumResolution)) valid = false; 
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
