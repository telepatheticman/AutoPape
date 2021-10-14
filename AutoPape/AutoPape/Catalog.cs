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

namespace Catalog
{
    public class CatalogThread
    {
        public Image threadImage;
        public string imgurl { get; set; } = "deleted";
        public string sub { get; set; }
        public string teaser { get; set; }
        public string threadId { get; set; }
    }
    public class Catalog
    {
        string board;
        string url;
        Regex rxFullJson = new Regex("\\{\\\"threads\\\".*?\\};");
        Regex rxThreads = new Regex("\\\"[0-9]*\\\":.*?},.*?\\},");
        public List<CatalogThread> catalogThreads;
        public StackPanel threadImages = null;
        WrapPanel wrapPanel = null;
        //List<System.Windows.Controls.StackPanel> 
        HttpClient client;
        AutoPape.Thread threadInfo = null;
        int numThreads { get { return catalogThreads.Count; } }

        public Catalog()
        {
            
            this.board = "wg";
            url = $"https://boards.4chan.org/{board}/catalog";
            catalogThreads = new List<CatalogThread>();
            client = new HttpClient();
            //buildCatalogInfoAsync();

        }
        public Catalog(string board, WrapPanel wrapPanel, StackPanel stackPanel)
        {
            this.board = board;
            this.wrapPanel = wrapPanel;
            this.threadImages = stackPanel;
            url = $"https://boards.4chan.org/{board}/catalog";
            catalogThreads = new List<CatalogThread>();
            client = new HttpClient();
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
                catalogThreads.Add(JsonSerializer.Deserialize<CatalogThread>(threadSanitized));
                
                catalogThreads.Last().sub = AutoPape.Utility.cleanHTMLString(catalogThreads.Last().sub);

                catalogThreads.Last().teaser = AutoPape.Utility.cleanHTMLString(catalogThreads.Last().teaser);

                catalogThreads.Last().threadId = thread.Value.Split(':').First().Trim('\"');
                catalogThreads.Last().threadImage =
                    AutoPape.Utility.imageFromURL(
                        $"https://i.4cdn.org/{board}/{catalogThreads.Last().imgurl}s.jpg",
                        client,
                        catalogThreads.Last().imgurl == "deleted");

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
                    string s = catalogThreads.Last().threadId;
                    string subject = catalogThreads.Last().sub;
                    string tease = catalogThreads.Last().teaser;
                    Item.Click += (o, e) => setThread(s, subject, tease);

                    content.Children.Add(catalogThreads.Last().threadImage);

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

        public async void buildCatalogInfoAsync()
        {
            await Task.Run(() => buildCatalogInfo()); ;
        }

        private void setThread(string threadID, string sub, string teaser)
        {
            threadInfo = new AutoPape.Thread(board, threadID, threadImages, sub, teaser);
            threadInfo.clearChildren();
            threadInfo.buildThreadFromWebAsync();
            //threadInfo.saveThread();
        }
    }
}
