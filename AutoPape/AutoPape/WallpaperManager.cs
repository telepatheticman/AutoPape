using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoPape
{
    public enum fit
    {
        center,
        stretch,
        fit,
        fill
    }

    public class MonitorSetting
    {
        public int x;
        public int y;
        public int height;
        public int width;
        public fit fitOption;
        public Image Image;
    }



    public class WallpaperManager
    {
        public int height;
        public int width;
        public int xOffset;
        public int yOffset;

        public int papers
        {
            get
            {
                return monitorSettings.Count();
            }
        }

        public List<MonitorSetting> monitorSettings;

        public Bitmap wallpaper;
        private string wallPaperName = "CurrentPaper\\current.bmp";

        public WallpaperManager()
        {
            monitorSettings = new List<MonitorSetting>();
            getScreenSpace();
            wallpaper = new Bitmap(width, height);
        }

        public void buildWallpaper()
        {
            wallpaper = new Bitmap(width, height);
            foreach(var setting in monitorSettings)
            {
                buildStretched(setting);
                using (Graphics g = Graphics.FromImage(wallpaper))
                {
                    g.DrawImage(setting.Image, setting.x + xOffset, setting.y + yOffset, setting.width, setting.height);
                }
            }
            Directory.CreateDirectory(Path.Combine(Utility.pathToParent(), "CurrentPaper"));
            wallpaper.Save(Path.Combine(Utility.pathToParent(), wallPaperName), System.Drawing.Imaging.ImageFormat.Bmp);

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            key.SetValue(@"WallpaperStyle", 1.ToString());
            key.SetValue(@"TileWallpaper", 1.ToString());

            SystemParametersInfo(SPI_SETDESKWALLPAPER,
            0,
            Path.Combine(Utility.pathToParent(), wallPaperName),
            SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }

        public void getScreenSpace()
        {
            Screen[] zScreen = Screen.AllScreens;
            string csScreens = "";

            int xMin = 0;
            int yMin = 0;
            int xMax = 0;
            int yMax = 0;

            for (int i = 0; i < zScreen.Length; i++)
            {
                // For each screen, add the screen properties to a list box.

                xMin = Math.Min(xMin, zScreen[i].Bounds.Left);
                yMin = Math.Min(yMin, zScreen[i].Bounds.Top);

                xMax = Math.Max(xMax, zScreen[i].Bounds.Left + zScreen[i].Bounds.Width);
                yMax = Math.Max(yMax, zScreen[i].Bounds.Top + zScreen[i].Bounds.Height);


                monitorSettings.Add(new MonitorSetting()
                {
                    x = zScreen[i].Bounds.Left,
                    y = zScreen[i].Bounds.Top,
                    width = zScreen[i].Bounds.Width,
                    height = zScreen[i].Bounds.Height,
                });

            }

            width = xMax - xMin;
            height = yMax - yMin;
            xOffset = Math.Abs(xMin);
            yOffset = Math.Abs(yMin);



            //Console.WriteLine(csScreens);
            //Console.WriteLine($"Width {width}, Height {hight}, xOffset {xOffset}, yOffset {yOffset}");
        }

        private void buildCentered(MonitorSetting monitor)
        {
            int xCenterOffset = (monitor.width / 2) - (monitor.Image.Width / 2);
            int yCenterOffset = (monitor.height / 2) - (monitor.Image.Height / 2);

            Bitmap bitmap = new Bitmap(monitor.width, monitor.height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(monitor.Image, xCenterOffset, yCenterOffset, monitor.Image.Width, monitor.Image.Height);
            }
            ImageConverter converter = new ImageConverter();

            var ms = new System.IO.MemoryStream((byte[])converter.ConvertTo(bitmap, typeof(byte[])));
            monitor.Image = Image.FromStream(ms);
        }

        private void buildStretched(MonitorSetting monitor)
        {
            Bitmap bitmap = new Bitmap(monitor.Image, monitor.Image.Width, monitor.Image.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            }
            ImageConverter converter = new ImageConverter();

            var ms = new System.IO.MemoryStream((byte[])converter.ConvertTo(bitmap, typeof(byte[])));
            monitor.Image = Image.FromStream(ms);
        }

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
    }
}
