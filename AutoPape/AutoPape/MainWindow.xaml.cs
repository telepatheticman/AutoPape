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
using System.Threading;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.IO;

namespace AutoPape
{

    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        SettingsManager manager;
        WallpaperManager wallManger;
        ThreadPanelManager threadPanelManager;
        CatalogManager caManager;
        Catalog catalogWG;
        Catalog catalogWGDisk;
        Catalog catalogW;
        Catalog catalogWDisk;

        private void textBoxLimit(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9]+");
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
            useMonitor.IsChecked = monitor.useMonitor;
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
            monitor.useMonitor = (bool)useMonitor.IsChecked;
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

            //setWallpaper = new Timer();
            //setWallpaper.Enabled = false;
            //setWallpaper.Interval = 5000;
            //setWallpaper.Tick += new EventHandler(setWallpaperTick);




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
             WhiteListAdd.Click +=
                 (o, e) =>
                 {
                     addWhiteListItem(manager, WhiteListText.Text);
                 };

             startBrowse.Click +=
                 (o, e) =>
                 {
                     saveBrowseClickedAsync();
                 };

             setDirectory.Click +=
                 (o, e) =>
                 {
                     setClicked();
                 };
             applyArchive.Click +=
                 (o, e) =>
                 {
                     applyArchiveSettings();
                 };
             applyGeneral.Click +=
                 (o, e) =>
                 {
                     applyGeneralSettings();
                 };
             panelDropFiles.Drop +=
                 (o, e) =>
                 {
                     fileDrop(o, e);
                 };

             manager = new SettingsManager();
             manager.loadSettings();

             if (string.IsNullOrEmpty(manager.archiveSettings.limitUnits))
             {
                 manager.archiveSettings.buildDefault();
                 manager.saveSettings();
             }



             setGeneralSettings();
             setArchivesettings();

             SaveDirectoryBox.Text = manager.saveDirectory;

             foreach (var item in manager.blackList.keyWords)
             {
                 addBlackListItem(manager, item, true);
             }
             foreach (var item in manager.whiteList.keyWords)
             {
                 addWhiteListItem(manager, item, true);
             }
             foreach (var monitor in manager.wallpaperManager.monitorSettings)
             {
                 MonitorBox.Items.Add(monitor);
                 if (monitor.primary)
                 {
                     MonitorBox.SelectedItem = monitor;
                     setMonitorSettingInfo(monitor);
                 }
             }

             SaveButton.Click += (o, e) =>
             {
                 saveClicked();
             };

             /*ReRoll.Click += (o, e) =>
             {
                 catalogWGDisk.setWallpaperAsync();
             };*/
             threadPanelManager = new ThreadPanelManager(ThreadProgress, SaveButton, threadPanel);
             catalogWG = new Catalog("wg", catalogPanelWG, threadPanelManager, manager, catalogType.current);
             catalogWGDisk = new Catalog("wg", catalogPanelWGSaved, threadPanelManager, manager, catalogType.saved);
             catalogW = new Catalog("w", catalogPanelW, threadPanelManager, manager, catalogType.current);
             catalogWDisk = new Catalog("w", catalogPanelWSaved, threadPanelManager, manager, catalogType.saved);
             caManager = new CatalogManager(manager);
             caManager.add(catalogWG);
             caManager.add(catalogWGDisk);
             caManager.add(catalogW);
             caManager.add(catalogWDisk);

             rerollPaper.Click +=
                 (o, e) =>
                 {
                     caManager.setWallpaperAsync();
                 };
             runArchive.Click +=
                 (o, e) =>
                 {
                     caManager.archiveTick(o, e);
                 };
             refreshThreads.Click +=
                 (o, e) =>
                 {
                     caManager.refreshAll();
                 };

             caManager.buildAllAsync();
            //catalogWG.buildCatalogInfoAsync(setWallpaper);
        }


        public void fileDrop(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            foreach(var file in files)
            {
                string ext = System.IO.Path.GetExtension(file);
                if (ext.ToLower() != "jpg" || ext.ToLower() != "jpeg" || ext.ToLower() != "png") continue;
                System.IO.File.Copy(
                    file, 
                    System.IO.Path.Combine(
                        manager.customDirectory, System.IO.Path.GetFileName(file)));
            }
            
        }


        //Needs to move into manager
        //Remove need for single board defined

        public void setClicked()
        {
            caManager.deepLockAll();
            threadPanelManager.threadPanel.Children.Clear();
            threadPanelManager.activeThread = null;
            string oldDirectory = manager.saveDirectory;
            manager.oldSaveDirectory = manager.saveDirectory;
            manager.saveDirectory = System.IO.Path.Combine(SaveDirectoryBox.Text, Utility.parent);
            Utility.moveDirectory(oldDirectory, manager.saveDirectory);
            manager.saveSettings();
            foreach (var catalog in caManager.catalogs)
            {
                if (catalog.type == catalogType.saved)
                {
                    catalog.build();
                }
            }
            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments = "/C ping 127.0.0.1 -n 5 && \"" + Environment.GetCommandLineArgs()[0] + "\"";
            info.UseShellExecute = true;
            info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            info.CreateNoWindow = true;
            info.FileName = "cmd.exe";
            Process.Start(info);
            Environment.Exit(-1);
            caManager.deepUnlockAll();
        }

        public void saveBrowseClicked()
        {

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.InitialDirectory = manager.saveDirectory;
            CommonFileDialogResult result = new CommonFileDialogResult();
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                result = dialog.ShowDialog();

                if(result == CommonFileDialogResult.Ok && 
                   !string.IsNullOrEmpty(dialog.FileName)) 
                    SaveDirectoryBox.Text = dialog.FileName;
            });
        }

        public async void saveBrowseClickedAsync()
        {
            await Task.Run(() => { saveBrowseClicked(); });
        }

        public void saveClicked()
        {
            Mutex refreshLock = new Mutex();
            threadPanelManager.activeThread?.saveThreadAsync(refreshLock);
            //foreach(var thread in catalogWGDisk.threads)
            //{
            //    thread.refreshAsync(refreshLock);
            //}
            caManager.refreshAll(refreshLock);
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
            if(!fromList) settings.saveSettings();
        }

        public void removeBlackListItem(SettingsManager settings, string toRemove, StackPanel panelRemove)
        {
            settings.blackList.keyWords.Remove(toRemove);
            BlackList.Children.Remove(panelRemove);
            settings.saveSettings();
        }

        public void addWhiteListItem(SettingsManager settings, string toAdd, bool fromList = false)
        {
            if ((!settings.whiteList.keyWords.Contains(toAdd) || fromList) && !string.IsNullOrEmpty(toAdd))
            {
                //TODO: Fix this garbage
                if (!fromList) settings.whiteList.keyWords.Add(toAdd);

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
                    removeWhiteListItem(settings, toAdd, item);
                };
                item.Children.Add(itemText);
                item.Children.Add(itemButton);
                WhiteList.Children.Add(item);
                WhiteListText.Text = "";
            }
            if (!fromList) settings.saveSettings();
        }

        public void removeWhiteListItem(SettingsManager settings, string toRemove, StackPanel panelRemove)
        {
            settings.whiteList.keyWords.Remove(toRemove);
            WhiteList.Children.Remove(panelRemove);
            settings.saveSettings();
        }

        public void applyGeneralSettings()
        {
            manager.interval = (int)setInterval.SelectedItem;
            manager.usingWG = (bool)useWG.IsChecked;
            manager.usingW = (bool)useW.IsChecked;
            manager.blackList.enabeld = (bool)useBlack.IsChecked;
            manager.whiteList.enabeld = (bool)useWhite.IsChecked;
            manager.whiteOverBlack = (bool)WhiteOverBlack.IsChecked;

            tabWG.Visibility = manager.usingWG ? Visibility.Visible : Visibility.Collapsed;
            tabWGDisk.Visibility = manager.usingWG ? Visibility.Visible : Visibility.Collapsed;
            catalogPanelWG.Visibility = manager.usingWG ? Visibility.Visible : Visibility.Hidden;
            catalogPanelWGSaved.Visibility = manager.usingWG ? Visibility.Visible : Visibility.Hidden;

            tabW.Visibility = manager.usingW ? Visibility.Visible : Visibility.Collapsed;
            tabWDisk.Visibility = manager.usingW ? Visibility.Visible : Visibility.Collapsed;
            catalogPanelW.Visibility = manager.usingW ? Visibility.Visible : Visibility.Hidden;
            catalogPanelWSaved.Visibility = manager.usingW ? Visibility.Visible : Visibility.Hidden;

            manager.saveSettings();
        }

        public void setGeneralSettings()
        {
            setInterval.Items.Add(5);
            setInterval.Items.Add(10);
            setInterval.Items.Add(15);
            setInterval.Items.Add(30);
            setInterval.Items.Add(60);
            setInterval.SelectedItem = manager.interval;
            useWG.IsChecked = manager.usingWG;
            useW.IsChecked = manager.usingW;

            useBlack.IsChecked = manager.blackList.enabeld;
            useWhite.IsChecked = manager.whiteList.enabeld;
            WhiteOverBlack.IsChecked = manager.whiteOverBlack;

            tabWG.Visibility = manager.usingWG ? Visibility.Visible : Visibility.Collapsed;
            tabWGDisk.Visibility = manager.usingWG ? Visibility.Visible : Visibility.Collapsed;
            catalogPanelWG.Visibility = manager.usingWG ? Visibility.Visible : Visibility.Hidden;
            catalogPanelWGSaved.Visibility = manager.usingWG ? Visibility.Visible : Visibility.Hidden;

            tabW.Visibility = manager.usingW ? Visibility.Visible : Visibility.Collapsed;
            tabWDisk.Visibility = manager.usingW ? Visibility.Visible : Visibility.Collapsed;
            catalogPanelW.Visibility = manager.usingW ? Visibility.Visible : Visibility.Hidden;
            catalogPanelWSaved.Visibility = manager.usingW ? Visibility.Visible : Visibility.Hidden;
        }

        public void applyArchiveSettings()
        {
            manager.archiveSettings.autoArchive = (bool)cbAutoArchive.IsChecked;
            manager.archiveSettings.archiveBadFit = (bool)cbArchiveBadFit.IsChecked;
            manager.archiveSettings.archiveBlacklist = (bool)cbArchiveBlacklist.IsChecked;

            manager.archiveSettings.saveBadFit = (bool)cbSaveBadFit.IsChecked;
            manager.archiveSettings.saveBlacklist = (bool)cbSaveBlacklist.IsChecked;

            manager.archiveSettings.limitSpace = (bool)cbLimitSpace.IsChecked;
            manager.archiveSettings.limitUnits = cbLimitUnit.Text;
            if (cbLimitUnit.Text == "MB") manager.archiveSettings.limitBytes = ((ulong)int.Parse(tbLimitAmount.Text) * 1024) * 1024;
            if (cbLimitUnit.Text == "GB") manager.archiveSettings.limitBytes = (((ulong)int.Parse(tbLimitAmount.Text) * 1024) * 1024) * 1024;
            manager.saveSettings();
        }

        public void setArchivesettings()
        {
            cbAutoArchive.IsChecked = manager.archiveSettings.autoArchive;
            cbArchiveBadFit.IsChecked = manager.archiveSettings.archiveBadFit;
            cbArchiveBlacklist.IsChecked = manager.archiveSettings.archiveBlacklist;

            cbSaveBadFit.IsChecked = manager.archiveSettings.saveBadFit;
            cbSaveBlacklist.IsChecked = manager.archiveSettings.saveBlacklist;

            cbLimitSpace.IsChecked = manager.archiveSettings.limitSpace;
            foreach (var item in cbLimitUnit.Items)
            {
                //TODO: This check needs to be better. Probably similer to monitor settings combobox.
                if (item.ToString() == $"System.Windows.Controls.ComboBoxItem: {manager.archiveSettings.limitUnits}")
                {
                    cbLimitUnit.SelectedItem = item;
                }
            }
            if (manager.archiveSettings.limitUnits == "MB") tbLimitAmount.Text = $"{(manager.archiveSettings.limitBytes / 1024) / 1024}";
            if (manager.archiveSettings.limitUnits == "GB") tbLimitAmount.Text = $"{((manager.archiveSettings.limitBytes / 1024) / 1024) / 1024}";
        }

    }
}


