using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Text.Json;
using System.Drawing;
using System.Drawing.Imaging;
using Catalog;
using System.Windows.Forms;

namespace AutoPape
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Timer setWallpaper;
        SettingsManager manager;
        WallpaperManager wallManger;
        Catalog catalog;

        public MainWindow()
        {
            InitializeComponent();

            setWallpaper = new Timer();
            setWallpaper.Enabled = false;
            setWallpaper.Interval = 5000;
            setWallpaper.Tick += new EventHandler(setWallpaperTick);



            wallManger = new WallpaperManager();

            manager = new SettingsManager(wallManger);
            manager.loadSettings();

            catalog = new Catalog("wg", catalogPanel, threadPanel, manager);
            catalog.buildCatalogInfoAsync(setWallpaper);

            Console.WriteLine(Utility.msToNextHour());
        }

        public void setWallpaperTick(object sender, EventArgs eventArgs)
        {
            catalog.setWallpaper();
        }

    }
}


