using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoPape
{
    class CatalogManager
    {
        bool runningWallpaper = false;
        bool runningArchive = false;
        bool runningRefresh = false;
        public List<Catalog> catalogs;
        public Thread customThread;
        public List<string> boards;
        SettingsManager manager;
        Timer setPaper;
        Timer refresh;
        Timer archive;
        public CatalogManager(SettingsManager manager)
        {
            catalogs = new List<Catalog>();
            boards = new List<string>();
            setPaper = new Timer();
            refresh = new Timer();
            archive = new Timer();

            setPaper.Enabled = false;
            setPaper.Tick += new EventHandler(setPaperTick);

            refresh.Enabled = false;
            refresh.Tick += new EventHandler(refreshTick);

            archive.Tick += new EventHandler(archiveTick);
            archive.Interval = (int)Utility.msFromNowToTime("01:00:00");
            archive.Start();

            this.manager = manager;

            customThread = new Thread("Custom", "Custom", null, "Custom Papers", "The Custome Wallpapers", manager);
            customThread.buildFromCustom();
            //setPaper.Interval = 10000;
        }

        public void refreshAll(System.Threading.Mutex refreshLock = null)
        {
            if (!runningRefresh) runningRefresh = true;
            else return;
            foreach (var catalog in catalogs)
            {
                catalog.refresh(refreshLock);
            }
            runningRefresh = false;
        }

        public async void buildAllAsync()
        {
            await Task.Run(() => buildAll());
            long msToNextHour = Utility.msToNextHour();
            int toNextHalf = (int)msToNextHour + 1800000;
            if (msToNextHour <= 300000) msToNextHour += 3600000;
            setPaper.Interval = (int)msToNextHour;
            refresh.Interval = toNextHalf;
            setPaper.Start();
            refresh.Start();
            setPaperNonTick();
        }
        private void buildAll()
        {
            foreach(var catalog in catalogs)
            {
                catalog.buildAsync();
            }
        }


        public void add(Catalog toAdd)
        {
            //TODO: Make this check better later
            if(!catalogs.Contains(toAdd))
            {
                catalogs.Add(toAdd);
            }

            if(!boards.Contains(toAdd.board))
            {
                boards.Add(toAdd.board);
            }
        }

        private void setPaperNonTick()
        {
            /*Random rand = new Random();
            int toUse = 0;
            do
            {
                toUse = rand.Next(0, catalogs.Count());
            } while (catalogs[toUse].threads.Count() > 0);*/
            setWallpaperAsync();
        }

        private void setPaperTick(object sender, EventArgs e)
        {
            //Random rand = new Random();
            //int toUse = rand.Next(0, catalogs.Count());
            setPaper.Interval = manager.interval * 60 * 1000;
            setWallpaperAsync();
        }
        public void refreshTick(object sender, EventArgs e)
        {

            refresh.Interval = 3600000;
            foreach (var catalog in catalogs)
            {
                catalog.clear();
                catalog.buildAsync();
            }
        }

        public void archiveTick(object sender, EventArgs e)
        {
            if (!runningArchive) runningArchive = true;
            else return;
            archive.Interval = 86400000;
            if (!manager.archiveSettings.autoArchive) return;
            foreach (var board in boards)
            {
                if (!manager.usingWG && board == "wg") continue;
                if (!manager.usingW && board == "w") continue;
                Catalog toSave = new Catalog(board, manager, catalogType.archive);
                toSave.buildAsync();
            }
            runningArchive = false;
        }

        public void deepLockAll()
        {
            foreach (var catalog in catalogs)
            {
                catalog.DeepLock();
            }
        }

        public void deepUnlockAll()
        {
            foreach (var catalog in catalogs)
            {
                catalog.DeepUnlock();
            }
        }

        private void lockAll()
        {
            foreach(var catalog in catalogs)
            {
                catalog.Lock();
            }
        }

        private void unlockAll()
        {
            foreach(var catalog in catalogs)
            {
                catalog.Unlock();
            }
        }

        public void setWallaper()
        {
            //if (!mutex.WaitOne(300000)) return;
            if (!runningWallpaper) runningWallpaper = true;
            else return;
            lockAll();
            List<Thread> tempThreadList = new List<Thread>();
            foreach(var catalog in catalogs)
            {
                if (!manager.usingWG && catalog.board == "wg") continue;
                if (!manager.usingW && catalog.board == "w") continue;
                tempThreadList.AddRange(catalog.threads);
            }
            customThread.buildFromCustom();
            foreach (var monitor in manager.wallpaperManager.monitorSettings)
            {
                if (!monitor.useMonitor) continue;
                tempThreadList.Shuffle();
                foreach (var thread in tempThreadList)
                {
                    if (manager.validThread(thread))
                    {
                        if(thread.validImages[monitor.ToString()].Count > 0)
                        {
                            List<ThreadImage> images = thread.validImages[monitor.ToString()];
                            images.Shuffle();
                            if (thread.fromDisk && !File.Exists(images.First().imageurl)) continue;
                            System.Windows.Controls.Image controlImageToUse = null;
                            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                controlImageToUse = thread.fromDisk ?
                                    Utility.imageFromDisk(images.First().imageurl) :
                                    Utility.imageFromURL(images.First().imageurl, false);
                                System.Drawing.Image imageToUse =
                                    Utility.controlToDrawingImage(controlImageToUse);
                                monitor.Image = imageToUse;
                                monitor.board = thread.board;
                                monitor.thread = thread.threadId;
                                monitor.imageName = Utility.nameFromURL(images.First().imageurl);
                            });
                            break;
                        }
                    }
                }
                monitor.mode = fitMode.fit;
                
            }
            manager.wallpaperManager.buildWallpaper();
            //mutex.ReleaseMutex();
            unlockAll();
            runningWallpaper = false;
        }

        public async void setWallpaperAsync()
        {
            await Task.Run(() => setWallaper());
        }
    }
}
