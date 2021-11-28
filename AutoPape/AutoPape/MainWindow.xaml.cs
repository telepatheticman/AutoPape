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
        ThreadPanelManager threadPanelManager;
        Catalog catalogWG;
        Catalog catalogWGDisk;
        Catalog catalogW;
        Catalog catalogWDisk;

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
            CurrentImage.Text = monitor.board + "/" + monitor.thread + "/" + monitor.imageName;
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

            BlackListAdd.Click +=
                (o, e) =>
                {
                    addBlackListItem(manager, BlackListText.Text);
                };

            manager = new SettingsManager();
            manager.loadSettings();
            foreach(var item in manager.blackList.keyWords)
            {
                addBlackListItem(manager, item, true);
            }
            foreach(var monitor in manager.wallpaperManager.monitorSettings)
            {
                MonitorBox.Items.Add(monitor);
                if(monitor.primary)
                {
                    MonitorBox.SelectedItem = monitor;
                    setMonitorSettingInfo(monitor);
                }
            }

            SaveButton.Click += (o, e) =>
            {
                saveClicked();
            };

            ReRoll.Click += (o, e) =>
            {
                catalogWGDisk.setWallpaperAsync();
            };
            threadPanelManager = new ThreadPanelManager(ThreadProgress, SaveButton, threadPanel);
            catalogWG = new Catalog("wg", catalogPanelWG, threadPanelManager, manager, catalogType.current);
            catalogWGDisk = new Catalog("wg", catalogPanelWGSaved, threadPanelManager, manager, catalogType.saved);
            catalogW = new Catalog("w", catalogPanelW, threadPanelManager, manager, catalogType.current);
            catalogWDisk = new Catalog("w", catalogPanelWSaved, threadPanelManager, manager, catalogType.saved);
            CatalogManager caManager = new CatalogManager(manager);
            //caManager.add(catalogWG);
            caManager.add(catalogWGDisk);
            //caManager.add(catalogW);
            //caManager.add(catalogWDisk);
            caManager.buildAllAsync();
            //catalogWG.buildCatalogInfoAsync(setWallpaper);
        }

        //Needs to move into manager
        //Remove need for single board defined
        public void saveClicked()
        {
            threadPanelManager.activeThread?.saveThreadAsync();
            foreach(var thread in catalogWGDisk.threads)
            {
                thread.refreshAsync();
            }
        }

        public void addBlackListItem(SettingsManager settings, string toAdd, bool fromList = false)
        {
            if ((!settings.blackList.keyWords.Contains(toAdd) || fromList) && !string.IsNullOrEmpty(toAdd))
            {
                //TODO: Fix this garbage
                if (!fromList) settings.blackList.keyWords.Add(toAdd);

                StackPanel item = new StackPanel();
                item.Orientation = System.Windows.Controls.Orientation.Horizontal;
                item.Margin = new Thickness(10);
                TextBlock itemText = new TextBlock();
                itemText.Text = toAdd;
                System.Windows.Controls.Button itemButton = new System.Windows.Controls.Button();
                itemButton.Content = "X";
                itemButton.Width = 10;
                itemButton.Height = 10;
                itemButton.Click += (o, e) =>
                {
                    removeBlackListItem(settings, toAdd, item);
                };
                item.Children.Add(itemText);
                item.Children.Add(itemButton);
                BlackList.Children.Add(item);
                BlackListText.Text = "";
            }
        }

        public void removeBlackListItem(SettingsManager settings, string toRemove, StackPanel panelRemove)
        {
            settings.blackList.keyWords.Remove(toRemove);
            BlackList.Children.Remove(panelRemove);
        }

        public void setWallpaperTick(object sender, EventArgs eventArgs)
        {
            catalogWG.setWallpaper();
        }

    }
}


