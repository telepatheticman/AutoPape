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
            threads = new List<Thread>();
            activeThread = null;
        }

        void buildItem(Thread thread, bool needsTeaserThumb = false)
        {
            if (!thread.fromDisk) thread.buildThreadFromWeb(needsTeaserThumb);
            if (thread.threadImages.Count() == 0) return;
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {

                string ID = thread.threadId;
                string imageNum = thread.threadImages.Count().ToString();
                string subject = thread.sub;
                string tease = thread.teaser;
                thread.threadButton.setClick((o, e) => setThread(thread));
                if (!thread.fromDisk) thread.buildThreadImageInfoAsync();

                thread.threadButton.threadImage = thread.teaserThumb;

                thread.threadButton.addTextLine("Thread: " + ID);
                thread.threadButton.addTextLine("Images: " + imageNum);
                thread.threadButton.addTextLine(subject.Length > 200 ? subject.Substring(0, 200) + "..." : subject);
                thread.threadButton.addTextLine(tease.Length > 500 ? tease.Substring(0, 500) + "..." : tease);

                //thread.threadButton.threadImage = thread.teaserThumb;
                //Item.Click += (o, e) => setThread(thread);
                if (!thread.fromDisk) thread.buildThreadImageInfoAsync();

                //content.Children.Add(thread.teaserThumb);
                //
                //TextBlock block = new TextBlock();
                //block.TextWrapping = TextWrapping.Wrap;
                //
                //block.Text = "Thread: " + ID;
                //block.Text += "\n";
                //block.Text += "Images: " + imageNum;
                //block.Text += "\n";
                //block.Text += subject.Length > 200 ? subject.Substring(0, 200) + "..." : subject;
                //block.Text += "\n";
                //block.Text += tease.Length > 500 ? tease.Substring(0, 500) + "..." : tease;
                //content.Children.Add(block);
                wrapPanel.Children.Add(thread.threadButton.button);

            });
        }

        public void buildFromDisk()
        {
            if (!mutex.WaitOne(300000)) return;
            System.IO.DirectoryInfo info = new DirectoryInfo(Utility.pathToBoardDirectory(board));
            foreach(var directory in info.GetDirectories())
            {
                threads.Add(new Thread());
                threads.Last().buildThreadFromDisk(board, directory.Name);
                threads.Last().threadPanel = threadPanel;
                buildItem(threads.Last());
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
                threads.Add(new Thread(board, threadID, threadPanel, sub, teaser));
                //buildItem(threads.Last(), true);
                threads.Last().buildThreadFromWeb();
                threads.Last().saveThread();
            }
        }

        void buildCatalogInfo()
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
                threads.Add(new Thread(board, catalogThread.threadId, threadPanel, catalogThread.sub, catalogThread.teaser));
                
                threads.Last().sub = Utility.cleanHTMLString(threads.Last().sub);

                threads.Last().teaser = Utility.cleanHTMLString(threads.Last().teaser);

                threads.Last().threadId = thread.Value.Split(':').First().Trim('\"');
                threads.Last().teaserThumb =
                    Utility.imageFromURL(
                        $"https://i.4cdn.org/{board}/{catalogThread.imgurl}s.jpg",
                        client,
                        catalogThread.imgurl == "deleted");
                //threads.Last().buildThreadImageInfoAsync();

                buildItem(threads.Last());
            }
            //buildFromDisk();
            mutex.ReleaseMutex();
        }

        public async void buildAsync()
        {
            switch(type)
            {
                case catalogType.current:
                    await Task.Run(() => buildCatalogInfo());
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
                    buildCatalogInfo();
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

        public void setWallpaper()
        {
            if (!mutex.WaitOne(300000)) return;
            foreach (var monitor in manager.wallpaperManager.monitorSettings)
            {
                string imageUrl = "";
                List<int> threadIndexList = Enumerable.Range(0, threads.Count()).ToList();
                threadIndexList.Shuffle();
                foreach(int threadIndex in threadIndexList)
                {
                    List<int> imageIndexList = Enumerable.Range(0, threads[threadIndex].threadImages.Count()).ToList();
                    imageIndexList.Shuffle();
                    if(manager.validThread(threads[threadIndex]))
                    {
                        foreach(int imageIndex in imageIndexList)
                        {
                            if (Utility.validImage(threads[threadIndex].threadImages[imageIndex], monitor, client))
                                imageUrl = threads[threadIndex].threadImages[imageIndex].imageurl;
                        }
                    }
                    if (!string.IsNullOrEmpty(imageUrl)) break;
                }
                if(!string.IsNullOrEmpty(imageUrl))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        monitor.Image = type != catalogType.saved ?
                        Utility.controlToDrawingImage(Utility.imageFromURL(imageUrl, client, false)) :
                        Utility.controlToDrawingImage(Utility.imageFromDisk(imageUrl));
                    });
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
