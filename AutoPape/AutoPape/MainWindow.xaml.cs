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
using System.Windows.Forms;
using System.Text.RegularExpressions;

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

        private void textBoxLimit(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void applySettings(object sender, EventArgs e)
        {
            getMonitorSettingInfo((MonitorSetting)MonitorBox.SelectedItem);
            manager.saveSettings();
        }

        private void setMonitorSettingInfo(MonitorSetting monitor)
        {
            orientationInfo.Text = "Orientation: " + monitor.orientation.ToString();
            allowOpposite.IsChecked = monitor.allowOpposite;
            resolutionInfo.Text = $"Resolution: {monitor.width}x{monitor.height}";
            narrowMode.Items.Clear();
            MinRsolution.Text = monitor.minimumResolution.ToString();
            narrowMode.Items.Add(fit.Center);
            narrowMode.Items.Add(fit.Stretch);
            narrowMode.Items.Add(fit.Fit);
            narrowMode.Items.Add(fit.Fill);
            narrowMode.SelectedItem = monitor.narrowOption;
            AllowNarrow.IsChecked = monitor.allowNarrower;
            wideMode.Items.Clear();
            wideMode.Items.Add(fit.Center);
            wideMode.Items.Add(fit.Stretch);
            wideMode.Items.Add(fit.Fit);
            wideMode.Items.Add(fit.Fill);
            wideMode.SelectedItem = monitor.wideOption;
            AllowWide.IsChecked = monitor.allowWider;
        }

        private void getMonitorSettingInfo(MonitorSetting monitor)
        {
            monitor.allowOpposite = (bool)allowOpposite.IsChecked;
            monitor.narrowOption = (fit)narrowMode.SelectedItem;
            monitor.wideOption = (fit)wideMode.SelectedItem;
            monitor.minimumResolution = int.Parse(MinRsolution.Text);
            monitor.allowNarrower = (bool)AllowNarrow.IsChecked;
            monitor.allowWider = (bool)AllowWide.IsChecked;
        }

        public MainWindow()
        {
            InitializeComponent();

            setWallpaper = new Timer();
            setWallpaper.Enabled = false;
            setWallpaper.Interval = 5000;
            setWallpaper.Tick += new EventHandler(setWallpaperTick);

            MonitorBox.SelectionChanged += 
                (o, e) => 
                {
                    setMonitorSettingInfo((MonitorSetting)MonitorBox.SelectedItem);
                };

            manager = new SettingsManager();
            manager.loadSettings();
            foreach(var monitor in manager.wallpaperManager.monitorSettings)
            {
                MonitorBox.Items.Add(monitor);
                if(monitor.primary)
                {
                    MonitorBox.SelectedItem = monitor;
                    setMonitorSettingInfo(monitor);
                }
            }


            catalog = new Catalog("wg", catalogPanel, threadPanel, manager);
            catalog.buildCatalogInfoAsync(setWallpaper);
        }

        public void setWallpaperTick(object sender, EventArgs eventArgs)
        {
            catalog.setWallpaper();
        }

    }
}


