using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO;
using System.Drawing;
using Image = System.Windows.Controls.Image;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Timer = System.Windows.Forms.Timer;
using System.Threading;

namespace AutoPape
{
    public enum catalogType
    {
        current,
        archive,
        saved
    }
    public class CatalogThread
    {
        public string imgurl { get; set; } = "deleted";
        public string sub { get; set; }
        public string teaser { get; set; }
        public string threadId { get; set; }
    }
    public class Catalog
    {
        public string board;
        string url;
        string urlArchive;
        Regex rxFullJson = new Regex("\\{\\\"threads\\\".*?\\};");
        Regex rxThreads = new Regex("\\\"[0-9]*\\\":.*?},.*?\\},");

        Regex rxArchiveThreads = new Regex(@"\<td\>[0-9]+");
        Regex rxArchiveSub = new Regex("\\<span class=\\\"subject\\\"\\>.*?\\<\\/span\\>");
        Regex rxArchiveTeaser = new Regex("\\<blockquote.*?\\\"m.*?\\\"\\>.*?\\<\\/blockquote\\>");
        public List<Thread> threads;
        public Thread activeThread;
        public ThreadPanelManager threadPanel = null;
        WrapPanel wrapPanel = null;
        //List<System.Windows.Controls.StackPanel> 
        HttpClient client;
        //AutoPape.Thread threadInfo = null;
        int numThreads { get { return threads.Count; } }

        private Mutex mutex = new Mutex();

        catalogType type;

        SettingsManager manager;

        public Catalog()
        {
            
            this.board = "wg";
            url = $"https://boards.4chan.org/{board}/catalog";
            urlArchive = $"https://boards.4chan.org/{board}/archive";
            threads = new List<Thread>();
            client = new HttpClient();
            //Replace with an enum type instead
            type = catalogType.current;
            //buildCatalogInfoAsync();

        }
        public Catalog(string board, SettingsManager manager, catalogType type)
        {
            this.board = board;
            url = $"https://boards.4chan.org/{board}/catalog";
            urlArchive = $"https://boards.4chan.org/{board}/archive";
            threads = new List<Thread>();
            client = new HttpClient();
            this.type = type;
            this.manager = manager;
        }
        public Catalog(string board, WrapPanel wrapPanel, ThreadPanelManager threadPanel, SettingsManager manager, catalogType type)
        {
            this.board = board;
            this.wrapPanel = wrapPanel;
            this.threadPanel = threadPanel;
            url = $"https://boards.4chan.org/{board}/catalog";
            urlArchive = $"https://boards.4chan.org/{board}/archive";
            threads = new List<Thread>();
            client = new HttpClient();
            this.type = type;
            this.manager = manager;
        }

        public void clear()
        {
            //TODO: Make this thread safe
            threads = new List<Thread>();
            activeThread = null;
        }

        public void refreshFromDisk()
        {
            if (!mutex.WaitOne(300000)) return;

            foreach(var thread in threads)
            {
                try
                {
                    thread.refresh();
                }
                catch(Exception ex)
                {
                    wrapPanel.Children.Remove(thread.threadButton.button);
                }
            }

            System.IO.DirectoryInfo info = new DirectoryInfo(manager.pathToBoardDirectory(board));

            try
            {
                foreach (var directory in info.GetDirectories())
                {
                    bool threadExists = false;
                    foreach(var thread in threads)
                    {
                        if(thread.threadId == directory.Name)
                        {
                            threadExists = true;
                            break;
                        }
                    }
                    if (!threadExists)
                    {
                        threads.Last().buildThreadFromDisk(board, directory.Name, manager);
                        threads.Last().threadPanel = threadPanel;
                        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            var currentThread = threads.Last();
                            threads.Last().threadButton.setClick((o, e) => setThread(currentThread));
                            wrapPanel.Children.Add(threads.Last().threadButton.button);
                        });
                    }
                }
            }
            catch (Exception ex)
            {

            }

            mutex.ReleaseMutex();
        }

        public async void refreshFromDiskAsync()
        {
            await Task.Run(() => refreshFromDisk());
        }

        public void buildFromDisk()
        {
            if (!mutex.WaitOne(300000)) return;
            System.IO.DirectoryInfo info = new DirectoryInfo(manager.pathToBoardDirectory(board));
            try
            {
                foreach (var directory in info.GetDirectories())
                {
                    threads.Add(new Thread());
                    threads.Last().buildThreadFromDisk(board, directory.Name, manager);
                    threads.Last().threadPanel = threadPanel;
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        var currentThread = threads.Last();
                        threads.Last().threadButton.setClick((o, e) => setThread(currentThread));
                        wrapPanel.Children.Add(threads.Last().threadButton.button);
                    });
                }
            }
            catch(Exception ex)
            {

            }
            mutex.ReleaseMutex();
        }

        void buildArchive()
        {
            //TODO: Might need mutex here. Might not.
            //if (!mutex.WaitOne(300000)) return;
            var task = client.GetStringAsync(urlArchive);
            string result = task.GetAwaiter().GetResult();
            var Threads = rxArchiveThreads.Matches(result);
            foreach(var thread in Threads)
            {
                string threadID = Utility.cleanArchiveString(thread.ToString());
                var threadURL = $"https://boards.4chan.org/{board}/thread/{threadID}";
                var threadTask = client.GetStringAsync(threadURL);
                string threadContent = threadTask.GetAwaiter().GetResult();
                string sub = rxArchiveSub.Match(threadContent).ToString();
                sub = Utility.cleanArchiveString(sub);
                string teaser = rxArchiveTeaser.Match(threadContent).ToString();
                teaser = Utility.cleanArchiveString(teaser);
                threads.Add(new Thread(board, threadID, threadPanel, sub, teaser, manager));
                //buildItem(threads.Last(), true);
                threads.Last().buildThreadFromWeb();
                threads.Last().saveThread();
            }
        }

        void buildFromWeb()
        {
            if (!mutex.WaitOne(300000)) return;
            var task = client.GetStringAsync(url);
            string result = task.GetAwaiter().GetResult();

            string FullJson = rxFullJson.Match(result).Value;
            var Threads = rxThreads.Matches(FullJson);

            foreach (Match thread in Threads)
            {
                string threadSanitized = thread.Value.Substring(thread.Value.IndexOf(':') + 1).TrimEnd(',', '}') + "}";
                CatalogThread catalogThread = JsonSerializer.Deserialize<CatalogThread>(threadSanitized);
                catalogThread.threadId = thread.Value.Split(':').First().Trim('\"');
                threads.Add(new Thread(board, catalogThread.threadId, threadPanel, catalogThread.sub, catalogThread.teaser, manager));
                
                threads.Last().sub = Utility.cleanHTMLString(threads.Last().sub);

                threads.Last().teaser = Utility.cleanHTMLString(threads.Last().teaser);

                threads.Last().threadId = thread.Value.Split(':').First().Trim('\"');
                threads.Last().teaserThumb =
                    Utility.imageFromURL(
                        $"https://i.4cdn.org/{board}/{catalogThread.imgurl}s.jpg",
                        client,
                        catalogThread.imgurl == "deleted");
                //threads.Last().buildThreadImageInfoAsync();
                threads.Last().buildThreadFromWebAsync();
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    var currentThread = threads.Last();
                    threads.Last().threadButton.setClick((o, e) => setThread(currentThread));
                    wrapPanel.Children.Add(threads.Last().threadButton.button);
                });
                //buildItem(threads.Last());
            }
            //buildFromDisk();
            mutex.ReleaseMutex();
        }

        public async void buildAsync()
        {
            switch(type)
            {
                case catalogType.current:
                    await Task.Run(() => buildFromWeb());
                    break;
                case catalogType.archive:
                    await Task.Run(() => buildArchive());
                    break;
                case catalogType.saved:
                    await Task.Run(() => buildFromDisk());
                    break;
                default:
                    return;

            }
        }

        public void build()
        {
            switch (type)
            {
                case catalogType.current:
                    buildFromWeb();
                    break;
                case catalogType.archive:
                    buildArchive();
                    break;
                case catalogType.saved:
                    buildFromDisk();
                    break;
                default:
                    return;

            }
        }

        private void setThread(Thread thread)
        {
            activeThread = thread;
            thread.setThreadContentAsync();
            //threadInfo.saveThread();
        }

        public List<Tuple<Thread, ThreadImage>> ToTupleList()
        {
            List<Tuple<Thread, ThreadImage>> tuple = new List<Tuple<Thread, ThreadImage>>();
            foreach(var thread in threads)
            {
                thread.Lock();
                tuple.AddRange(thread.ToTupleList());
                thread.Unlock();
            }
            return tuple;
        }


        public void Lock()
        {
            if (!mutex.WaitOne(300000)) return;
        }

        public void Unlock()
        {
            mutex.ReleaseMutex();
        }

        public void setWallpaper()
        {
            if (!mutex.WaitOne(300000)) return;
            List<Tuple<Thread, ThreadImage>> tuple = ToTupleList();
            tuple.Shuffle();
            foreach (var monitor in manager.wallpaperManager.monitorSettings)
            {
                //string imageUrl = "";
                //string threadUsed = "";

                foreach(var pair in tuple)
                {
                    if(manager.validThread(pair.Item1))
                    {
                        if (Utility.validImage(pair.Item2, monitor, client))
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                System.Drawing.Image imageToUse = pair.Item1.fromDisk ?
                                Utility.controlToDrawingImage(Utility.imageFromDisk(pair.Item2.imageurl)) :
                                Utility.controlToDrawingImage(Utility.imageFromURL(pair.Item2.imageurl, client, false));
                                monitor.Image = imageToUse;
                                monitor.board = board;
                                monitor.thread = pair.Item1.threadId;
                                monitor.imageName = Utility.nameFromURL(pair.Item2.imageurl);
                            });
                            if (monitor.Image != null) break;
                            //imageUrl = pair.Item2.imageurl;
                            //threadUsed = pair.Item1.threadId;
                        }
                    }
                }
            }
            manager.wallpaperManager.buildWallpaper();
            mutex.ReleaseMutex();
        }

        public async void setWallpaperAsync()
        {
            await Task.Run(() => setWallpaper());
        }
        
    }
}
