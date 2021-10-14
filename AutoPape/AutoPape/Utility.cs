using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
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
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, parent, board);
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
    }
}
