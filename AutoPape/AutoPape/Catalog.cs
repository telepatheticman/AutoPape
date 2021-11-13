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

namespace AutoPape
{
    public class CatalogThread
    {
        public string imgurl { get; set; } = "deleted";
        public string sub { get; set; }
        public string teaser { get; set; }
        public string threadId { get; set; }
    }
    public class Catalog
    {
        string board;
        string url;
        string urlArchive;
        Regex rxFullJson = new Regex("\\{\\\"threads\\\".*?\\};");
        Regex rxThreads = new Regex("\\\"[0-9]*\\\":.*?},.*?\\},");

        Regex rxArchiveThreads = new Regex(@"\<td\>[0-9]+");
        Regex reArchiveSub = new Regex("\\<span class=\\\"subject\\\"\\>.*?\\<\\/span\\>");
        Regex rxArchiveTeaser = new Regex("\\<blockquote.*?\\\"m.*?\\\"\\>.*?\\<\\/blockquote\\>");
        public List<Thread> threads;
        public Thread activeThread;
        public ThreadPanelManager threadPanel = null;
        WrapPanel wrapPanel = null;
        //List<System.Windows.Controls.StackPanel> 
        HttpClient client;
        //AutoPape.Thread threadInfo = null;
        int numThreads { get { return threads.Count; } }

        bool fromDisk;

        SettingsManager manager;

        public Catalog()
        {
            
            this.board = "wg";
            url = $"https://boards.4chan.org/{board}/catalog";
            urlArchive = $"https://boards.4chan.org/{board}/archive";
            threads = new List<Thread>();
            client = new HttpClient();
            //buildCatalogInfoAsync();

        }
        public Catalog(string board, WrapPanel wrapPanel, ThreadPanelManager threadPanel, SettingsManager manager, bool fromDisk)
        {
            this.board = board;
            this.wrapPanel = wrapPanel;
            this.threadPanel = threadPanel;
            url = $"https://boards.4chan.org/{board}/catalog";
            threads = new List<Thread>();
            client = new HttpClient();
            this.fromDisk = fromDisk;
            this.manager = manager;
        }

        public void clear()
        {
            threads = new List<Thread>();
            activeThread = null;
        }

        void buildItem(Thread thread)
        {
            if (!thread.fromDisk) thread.buildThreadFromWeb();
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Button Item = new Button();
                StackPanel content = new StackPanel();
                content.VerticalAlignment = VerticalAlignment.Top;
                Item.VerticalContentAlignment = VerticalAlignment.Top;
                Item.Content = content;
                Item.Width = 200;
                Item.Height = Double.NaN;
                Item.Margin = new Thickness(10);
                string ID = thread.threadId;
                string imageNum = thread.threadImages.Count().ToString();
                string subject = thread.sub;
                string tease = thread.teaser;
                Item.Click += (o, e) => setThread(thread);
                if (!thread.fromDisk) thread.buildThreadImageInfoAsync();

                content.Children.Add(thread.teaserThumb);

                TextBlock block = new TextBlock();
                block.TextWrapping = TextWrapping.Wrap;

                block.Text = "Thread: " + ID;
                block.Text += "\n";
                block.Text += "Images: " + imageNum;
                block.Text += "\n";
                block.Text += subject.Length > 200 ? subject.Substring(0, 200) + "..." : subject;
                block.Text += "\n";
                block.Text += tease.Length > 500 ? tease.Substring(0, 500) + "..." : tease;
                content.Children.Add(block);
                wrapPanel.Children.Add(Item);

            });
        }

        public void buildFromDisk()
        {
            System.IO.DirectoryInfo info = new DirectoryInfo(Utility.pathToBoardDirectory(board));
            foreach(var directory in info.GetDirectories())
            {
                threads.Add(new Thread());
                threads.Last().buildThreadFromDisk(board, directory.Name);
                threads.Last().threadPanel = threadPanel;
                buildItem(threads.Last());
            }
            
        }

        public async void buildFromDiskAsync()
        {
            await Task.Run(() => buildFromDisk());
        }

        void buildArchive()
        {
            var task = client.GetStringAsync(url);
            string result = task.GetAwaiter().GetResult();
        }

        void buildCatalogInfo()
        {
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
        }

        public async void buildCatalogInfoAsync()
        {
            await Task.Run(() => buildCatalogInfo());
            //timer.Start();
        }

        public async void buildAsync()
        {
            if(fromDisk)
            {
                await Task.Run(() => buildFromDisk());
            }
            else
            {
                await Task.Run(() => buildCatalogInfo());
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
            foreach(var monitor in manager.wallpaperManager.monitorSettings)
            {
                List<string> validImages = new List<string>();
                foreach(var thread in threads)
                {
                    if(manager.validThread(thread))
                    {
                        foreach(var image in thread.threadImages)
                        {
                            if (Utility.validImage(image, monitor, client)) validImages.Add(image.imageurl);
                        }
                    }
                    if (validImages.Count() >= 10) break;
                }
                if(validImages.Count() > 0)
                {
                    Random rand = new Random();
                    string imageUrl = validImages.ElementAt(rand.Next(0, validImages.Count() - 1));
                    //monitor.Image = System.Drawing.Image.FromFile(image);
                    monitor.Image = Utility.controlToDrawingImage(Utility.imageFromURL(imageUrl, client, false));
                }

            }
            manager.wallpaperManager.buildWallpaper();
        }
        
    }
}
