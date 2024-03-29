﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace AutoPape
{
    public enum fit
    {
        Center,
        Stretch,
        Fit, //Height based fit
        Fill //Width based fit
    }

    public enum fitMode
    {
        fit,
        narrow,
        wide
    }

    public class MonitorSetting
    {
        [XmlAttribute("MonitorName")]
        public string name = "monitor";
        [XmlAttribute("IsPrimary")]
        public bool primary = false;
        public bool allowOpposite = false;
        public bool allowNarrower = false;
        public bool allowWider = false;
        public bool useMonitor = false;
        [XmlIgnore]
        public int x = 0;
        [XmlIgnore]
        public int y = 0;
        [XmlIgnore]
        public int height = 100;
        [XmlIgnore]
        public int width = 200;
        public int minimumResolution = 1080;
        public fit fitOption = fit.Center;
        public fit narrowOption = fit.Fill;
        public fit wideOption = fit.Fill;
        [XmlIgnore]
        public fitMode mode = fitMode.fit;
        [XmlIgnore]
        public orientation orientation
        {
            get
            {
                return width >= height ? orientation.horizontal : orientation.vertical;
            }
        }
        public double aspectRatio
        {
            get
            {
                return orientation == orientation.horizontal ?
                    (double)width / (double)height : (double)height / (double)width;
            }
        }
        [XmlIgnore]
        public int resolution
        {
            get
            {
                return width >= height ? height : width;
            }
        }
        [XmlIgnore]
        public Image Image;
        [XmlIgnore]
        public string board;
        [XmlIgnore]
        public string thread;
        [XmlIgnore]
        public string imageName;

        public override string ToString()
        {
            return name + (primary ? "(Primary)" : "") ;
        }
    }



    public class WallpaperManager
    {
        [XmlIgnore]
        public int height;
        [XmlIgnore]
        public int width;
        [XmlIgnore]
        public int xOffset;
        [XmlIgnore]
        public int yOffset;
        [XmlIgnore]
        public int papers
        {
            get
            {
                return monitorSettings.Count();
            }
        }
        [XmlArrayAttribute("MonitorSettings")]
        [XmlArrayItem("Monitor")]
        public List<MonitorSetting> monitorSettings;
        [XmlIgnore]
        public Bitmap wallpaper;
        [XmlIgnore]
        private string wallPaperName = "CurrentPaper\\current.bmp";

        public WallpaperManager()
        {
            //monitorSettings = new List<MonitorSetting>();
            //getScreenSpace();
        }

        private void buildPortionFit(MonitorSetting setting, fit toSwitch)
        {
            switch (toSwitch)
            {
                case fit.Center:
                    buildCentered(setting);
                    break;
                case fit.Stretch:
                    buildStretched(setting);
                    break;
                case fit.Fill:
                    buildFill(setting);
                    break;
                case fit.Fit:
                    buildFit(setting);
                    break;
            }
        }

        private void buildPortionMode(MonitorSetting setting)
        {
            switch (setting.mode)
            {
                case fitMode.narrow:
                    buildPortionFit(setting, setting.narrowOption);
                    break;
                case fitMode.wide:
                    buildPortionFit(setting, setting.wideOption);
                    break;
                case fitMode.fit:
                    buildStretched(setting);
                    break;
            }
        }

        public void buildWallpaper()
        {
            wallpaper = new Bitmap(width, height);
            foreach(var setting in monitorSettings)
            {
                if (!setting.useMonitor) continue;
                if (setting.Image != null)
                {
                    buildPortionMode(setting);
                }
                else
                {
                    buildDefaultPaper(setting);
                }

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

                bool overlap = false;

                foreach(var setting in monitorSettings)
                {
                    if (zScreen[i].Primary) break;
                    if
                        (
                        zScreen[i].Bounds.Left >= setting.x &&
                        zScreen[i].Bounds.Left < setting.x + setting.width &&
                        zScreen[i].Bounds.Top >= setting.y &&
                        zScreen[i].Bounds.Top < setting.y + setting.height
                        )
                        overlap = true;
                }

                if (!overlap)
                {
                    monitorSettings.Add(new MonitorSetting()
                    {
                        x = zScreen[i].Bounds.Left,
                        y = zScreen[i].Bounds.Top,
                        width = zScreen[i].Bounds.Width,
                        height = zScreen[i].Bounds.Height,
                        name = zScreen[i].DeviceName,
                        primary = zScreen[i].Primary
                    });
                }

            }

            width = xMax - xMin;
            height = yMax - yMin;
            xOffset = Math.Abs(xMin);
            yOffset = Math.Abs(yMin);
        }

        public void refreshScreens()
        {
            if (monitorSettings == null)
            {
                monitorSettings = new List<MonitorSetting>();
                getScreenSpace();
                return;
            }
            Screen[] Screens = Screen.AllScreens;

            int xMin = 0;
            int yMin = 0;
            int xMax = 0;
            int yMax = 0;

            for(int i = 0; i < Screens.Length; i++)
            {
                xMin = Math.Min(xMin, Screens[i].Bounds.Left);
                yMin = Math.Min(yMin, Screens[i].Bounds.Top);

                xMax = Math.Max(xMax, Screens[i].Bounds.Left + Screens[i].Bounds.Width);
                yMax = Math.Max(yMax, Screens[i].Bounds.Top + Screens[i].Bounds.Height);
                bool refreshed = false;
                foreach(var setting in monitorSettings)
                {
                    if(setting.name == Screens[i].DeviceName)
                    {
                        setting.x = Screens[i].Bounds.Left;
                        setting.y = Screens[i].Bounds.Top;
                        setting.width = Screens[i].Bounds.Width;
                        setting.height = Screens[i].Bounds.Height;
                        setting.name = Screens[i].DeviceName;
                        setting.primary = Screens[i].Primary;
                        refreshed = true;
                    }
                }
                if(!refreshed)
                {
                    monitorSettings.Add(new MonitorSetting()
                    {
                        x = Screens[i].Bounds.Left,
                        y = Screens[i].Bounds.Top,
                        width = Screens[i].Bounds.Width,
                        height = Screens[i].Bounds.Height,
                        name = Screens[i].DeviceName,
                        primary = Screens[i].Primary
                    });
                }
            }

            width = xMax - xMin;
            height = yMax - yMin;
            xOffset = Math.Abs(xMin);
            yOffset = Math.Abs(yMin);
        }


        private void buildDefaultPaper(MonitorSetting monitor)
        {
            Random random = new Random();
            int red = random.Next(0, 256);
            int green = random.Next(0, 256);
            int blue = random.Next(0, 255);
            string hexColor = "#" + red.ToString("X2") + green.ToString("X2") + red.ToString("X2");
            Color c = Color.FromArgb(red, green, blue);
            SolidBrush brush = new SolidBrush(c);
            Bitmap bitmap = new Bitmap(monitor.width, monitor.height);
            Font font = new Font("Arial", 50, FontStyle.Bold, GraphicsUnit.Point);
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.WordBreak;
            Rectangle rect = new Rectangle(0, 0, monitor.width, monitor.height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.FillRectangle(brush, 0, 0, monitor.width, monitor.height);
                TextRenderer.DrawText(g, hexColor, font, rect, Color.White, flags);
                //g.DrawImage(monitor.Image, xCenterOffset, yCenterOffset, monitor.Image.Width, monitor.Image.Height);
            }
            brush.Dispose();
            font.Dispose();
            ImageConverter converter = new ImageConverter();

            var ms = new System.IO.MemoryStream((byte[])converter.ConvertTo(bitmap, typeof(byte[])));
            monitor.Image = Image.FromStream(ms);
            //monitor.imageName = "Test";
        }

        //TODO: Condence the build modes. Lots of shared code

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
            Bitmap bitmap = new Bitmap(monitor.width, monitor.height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(monitor.Image, 0, 0, monitor.width, monitor.height);
            }
            ImageConverter converter = new ImageConverter();

            var ms = new System.IO.MemoryStream((byte[])converter.ConvertTo(bitmap, typeof(byte[])));
            monitor.Image = Image.FromStream(ms);
        }

        //Fill width
        private void buildFill(MonitorSetting monitor)
        {
            double ratio = (double)monitor.Image.Width / (double)monitor.width;
            int newWidth = (int)((double)monitor.Image.Width / ratio);
            int newHeight = (int)((double)monitor.Image.Height / ratio);

            int xCenterOffset = (monitor.width / 2) - (newWidth / 2);
            int yCenterOffset = (monitor.height / 2) - (newHeight / 2);

            Bitmap bitmap = new Bitmap(monitor.width, monitor.height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(monitor.Image, xCenterOffset, yCenterOffset, newWidth, newHeight);
            }
            ImageConverter converter = new ImageConverter();

            var ms = new System.IO.MemoryStream((byte[])converter.ConvertTo(bitmap, typeof(byte[])));
            monitor.Image = Image.FromStream(ms);
        }

        //Fit height
        private void buildFit(MonitorSetting monitor)
        {
            double ratio = (double)monitor.Image.Height / (double)monitor.height;
            int newWidth = (int)((double)monitor.Image.Width / ratio);
            int newHeight = (int)((double)monitor.Image.Height / ratio);

            int xCenterOffset = (monitor.width / 2) - (newWidth / 2);
            int yCenterOffset = (monitor.height / 2) - (newHeight / 2);

            Bitmap bitmap = new Bitmap(monitor.width, monitor.height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(monitor.Image, xCenterOffset, yCenterOffset, newWidth, newHeight);
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
