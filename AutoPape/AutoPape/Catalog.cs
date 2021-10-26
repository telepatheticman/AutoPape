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
        public List<Thread> threads;
        public StackPanel threadImages = null;
        WrapPanel wrapPanel = null;
        //List<System.Windows.Controls.StackPanel> 
        HttpClient client;
        AutoPape.Thread threadInfo = null;
        int numThreads { get { return threads.Count; } }

        SettingsManager manager;

        public Catalog()
        {
            
            this.board = "wg";
            url = $"https://boards.4chan.org/{board}/catalog";
            urlArchive = $"";
            threads = new List<Thread>();
            client = new HttpClient();
            //buildCatalogInfoAsync();

        }
        public Catalog(string board, WrapPanel wrapPanel, StackPanel stackPanel, SettingsManager manager)
        {
            this.board = board;
            this.wrapPanel = wrapPanel;
            this.threadImages = stackPanel;
            url = $"https://boards.4chan.org/{board}/catalog";
            threads = new List<Thread>();
            client = new HttpClient();

            this.manager = manager;
        }

        public void buildFromDisk()
        {
            System.IO.DirectoryInfo info = new DirectoryInfo(Utility.pathToBoardDirectory(board));
            foreach(var directory in info.GetDirectories())
            {
                threads.Add(new Thread());
                threads.Last().buildThreadFromDisk(board, directory.Name);
                threads.Last().threadPanel = threadImages;
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
                    string s = threads.Last().threadId;
                    string subject = threads.Last().sub;
                    string tease = threads.Last().teaser;
                    Item.Click += (o, e) => setThread(threads.Last());

                    content.Children.Add(threads.Last().teaserThumb);

                    TextBlock block = new TextBlock();
                    block.TextWrapping = TextWrapping.Wrap;

                    block.Text = subject.Length > 200 ? subject.Substring(0, 200) + "..." : subject;
                    block.Text += "\n";
                    block.Text += tease.Length > 500 ? tease.Substring(0, 500) + "..." : tease;
                    content.Children.Add(block);
                    wrapPanel.Children.Add(Item);

                });
            }
            
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
                threads.Add(new Thread(board, catalogThread.threadId, threadImages, catalogThread.sub, catalogThread.teaser));
                
                threads.Last().sub = Utility.cleanHTMLString(threads.Last().sub);

                threads.Last().teaser = Utility.cleanHTMLString(threads.Last().teaser);

                threads.Last().threadId = thread.Value.Split(':').First().Trim('\"');
                threads.Last().teaserThumb =
                    Utility.imageFromURL(
                        $"https://i.4cdn.org/{board}/{catalogThread.imgurl}s.jpg",
                        client,
                        catalogThread.imgurl == "deleted");

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
                    string s = threads.Last().threadId;
                    string subject = threads.Last().sub;
                    string tease = threads.Last().teaser;
                    var currentThread = threads.Last();
                    Item.Click += (o, e) => setThread(currentThread);

                    content.Children.Add(threads.Last().teaserThumb);

                    TextBlock block = new TextBlock();
                    block.TextWrapping = TextWrapping.Wrap;

                    block.Text = subject.Length > 200 ? subject.Substring(0, 200) + "..." : subject;
                    block.Text += "\n";
                    block.Text += tease.Length > 500 ? tease.Substring(0, 500) + "..." : tease;
                    content.Children.Add(block);
                    wrapPanel.Children.Add(Item);
                    
                });
                
            }
            buildFromDisk();
        }

        public async void buildCatalogInfoAsync(Timer timer)
        {
            await Task.Run(() => buildCatalogInfo());
            timer.Start();
        }

        private void setThread(Thread thread)
        {
            thread.clearChildren();
            thread.buildThreadFromWebAsync();
            //threadInfo.saveThread();
        }

        public void setWallpaper()
        {
            foreach(var monitor in manager.wallpaperManager.monitorSettings)
            {
                List<string> validImages = new List<string>();
                foreach(var thread in threads)
                {
                    if(thread.fromDisk)
                    {
                        foreach(var url in thread.threadImages)
                        {
                            validImages.Add(url.imageurl);
                        }
                    }
                }
                Random rand = new Random();
                string image = validImages.ElementAt(rand.Next(0, validImages.Count() - 1));
                monitor.Image = System.Drawing.Image.FromFile(image);
            }
            manager.wallpaperManager.buildWallpaper();
        }
        
    }
}
