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

        public async void buildAllAsync()
        {
            await Task.Run(() => buildAll());
            long msToNextHour = Utility.msToNextHour();
            int toNextHalf = (int)msToNextHour + 1800000;
            if (msToNextHour <= 300000) msToNextHour += 3600000;
            setPaper.Interval = (int)msToNextHour;
            refresh.Interval = toNextHalf;
            setPaper.Start();
            //TODO: Uncomment when refresh is better
            //refresh.Start();
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
            archive.Interval = 86400000;
            if (!manager.archiveSettings.autoArchive) return;
            foreach (var board in boards)
            {
                if (!manager.usingWG && board == "wg") continue;
                if (!manager.usingW && board == "w") continue;
                Catalog toSave = new Catalog(board, manager, catalogType.archive);
                toSave.buildAsync();
            }
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
            lockAll();
            List<Tuple<Thread, ThreadImage>> tuple = new List<Tuple<Thread, ThreadImage>>();
            foreach(var catalog in catalogs)
            {
                if (!manager.usingWG && catalog.board == "wg") continue;
                if (!manager.usingW && catalog.board == "w") continue;
                tuple.AddRange(catalog.ToTupleList());
            }
            customThread.buildFromCustom();
            tuple.AddRange(customThread.ToTupleList());
            foreach (var monitor in manager.wallpaperManager.monitorSettings)
            {
                tuple.Shuffle();
                monitor.mode = fitMode.fit;
                //string imageUrl = "";
                //string threadUsed = "";

                foreach (var pair in tuple)
                {
                    if (manager.validThread(pair.Item1))
                    {
                        if(!pair.Item1.fromDisk)
                            pair.Item1.buildThreadImageInfo();
                        if (Utility.validImage(pair.Item2, monitor, pair.Item1.client))
                        {
                            System.Windows.Controls.Image controlImageToUse = null;
                            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                controlImageToUse = pair.Item1.fromDisk ?
                                    Utility.imageFromDisk(pair.Item2.imageurl) :
                                    Utility.imageFromURL(pair.Item2.imageurl, pair.Item1.client, false);
                                System.Drawing.Image imageToUse = 
                                    Utility.controlToDrawingImage(controlImageToUse);
                                monitor.Image = imageToUse;
                                monitor.board = pair.Item1.board;
                                monitor.thread = pair.Item1.threadId;
                                monitor.imageName = Utility.nameFromURL(pair.Item2.imageurl);
                                //monitor.setMode(pair.Item2);
                            });
                            if (monitor.Image != null)
                            {
                                Directory.CreateDirectory(Path.Combine(Utility.pathToParent(), "CurrentPaper"));
                                pair.Item1.saveImage(
                                    Path.Combine(Utility.pathToParent(), "CurrentPaper"),
                                    monitor.imageName,
                                    controlImageToUse);
                                break;
                            }
                            //imageUrl = pair.Item2.imageurl;
                            //threadUsed = pair.Item1.threadId;
                        }
                    }
                }
            }
            tuple.Clear();
            manager.wallpaperManager.buildWallpaper();
            //mutex.ReleaseMutex();
            unlockAll();
        }

        public async void setWallpaperAsync()
        {
            await Task.Run(() => setWallaper());
        }
    }
}
