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

        public static Image imageFromURL(string url, HttpClient client, bool deleted)
        {
            Image image = null;
            byte[] imageByte;

            if(deleted)
            {
                ImageConverter converter = new ImageConverter();
                imageByte = (byte[])converter.ConvertTo(AutoPape.Properties.Resources.NoImage, typeof(byte[]));
            }
            else
            {
                var thumbNailTask = client.GetByteArrayAsync(url);
                imageByte = thumbNailTask.GetAwaiter().GetResult();
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
    }
}
