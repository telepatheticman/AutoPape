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

namespace AutoPape
{
    class CatalogThread
    {
        public Image threadImage;
        public string imgurl { get; set; } = "deleted";
        public string sub { get; set; }
        public string teaser { get; set; }
        public string threadId { get; set; }
    }
    class Catalog
    {
        string board;
        string url;
        Regex rxFullJson = new Regex("\\{\\\"threads\\\".*?\\};");
        Regex rxThreads = new Regex("\\\"[0-9]*\\\":.*?},.*?\\},");
        List<CatalogThread> catalogThreads;
        //List<System.Windows.Controls.StackPanel> 
        HttpClient client;
        int numThreads { get { return catalogThreads.Count; } }
        public Catalog(string board)
        {
            this.board = board;
            url = $"https://boards.4chan.org/{board}/catalog";
            catalogThreads = new List<CatalogThread>();
            client = new HttpClient();
        }

        void buildCatalogInfo(WrapPanel panel)
        {
            var task = client.GetStringAsync(url);
            string result = task.GetAwaiter().GetResult();

            string FullJson = rxFullJson.Match(result).Value;
            var Threads = rxThreads.Matches(FullJson);

            foreach (Match thread in Threads)
            {
                string threadSanitized = thread.Value.Substring(thread.Value.IndexOf(':') + 1).TrimEnd(',', '}') + "}";
                catalogThreads.Add(JsonSerializer.Deserialize<CatalogThread>(threadSanitized));
                catalogThreads.Last().sub = catalogThreads.Last().sub.Replace("&amp;", "&");
                catalogThreads.Last().sub = catalogThreads.Last().sub.Replace("&quot;", "\"");;
                catalogThreads.Last().sub = catalogThreads.Last().sub.Replace("&#039;", "'");
                catalogThreads.Last().sub = catalogThreads.Last().sub.Replace("&lt;", "<");
                catalogThreads.Last().sub = catalogThreads.Last().sub.Replace("&gt;", ">");
                catalogThreads.Last().teaser = catalogThreads.Last().teaser.Replace("&amp;", "&");
                catalogThreads.Last().teaser = catalogThreads.Last().teaser.Replace("&quot;", "\"");
                catalogThreads.Last().teaser = catalogThreads.Last().teaser.Replace("&#039;", "'");
                catalogThreads.Last().teaser = catalogThreads.Last().teaser.Replace("&lt;", "<");
                catalogThreads.Last().teaser = catalogThreads.Last().teaser.Replace("&gt;", ">");
                catalogThreads.Last().threadId = thread.Value.Split(':').First().Trim('\"');
                byte[] thumbNailByte;
                if (catalogThreads.Last().imgurl != "deleted")
                {
                    var thumbNailTask = client.GetByteArrayAsync($"https://i.4cdn.org/{board}/{catalogThreads.Last().imgurl}s.jpg");
                    thumbNailByte = thumbNailTask.GetAwaiter().GetResult();
                }
                else
                {
                    ImageConverter converter = new ImageConverter();
                    thumbNailByte = (byte[])converter.ConvertTo(Properties.Resources.NoImage, typeof(byte[]));
                }

                /*try
                {
                    var thumbNailTask = client.GetByteArrayAsync($"https://i.4cdn.org/{board}/{catalogThreads.Last().imgurl}s.jpg");
                    thumbNailByte = thumbNailTask.GetAwaiter().GetResult();
                }
                catch(Exception ex)
                {
                    ImageConverter converter = new ImageConverter();
                    thumbNailByte = (byte[])converter.ConvertTo(Properties.Resources.NoImage, typeof(byte[]));
                }*/

                MemoryStream ms = new MemoryStream(thumbNailByte);
                //System.Drawing.Bitmap thumbNailBitmap = new System.Drawing.Bitmap(ms);
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    ms.Position = 0;

                    System.Windows.Media.Imaging.BitmapImage thumbNailBitmap = new System.Windows.Media.Imaging.BitmapImage();
                    //thumbNailBitmap = ;
                    thumbNailBitmap.BeginInit();
                    thumbNailBitmap.StreamSource = ms;
                    thumbNailBitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    thumbNailBitmap.EndInit();

                    catalogThreads.Last().threadImage = new Image();
                    catalogThreads.Last().threadImage.Source = thumbNailBitmap;

                    StackPanel Item = new StackPanel();
                    Item.Width = 200;
                    Item.Height = Double.NaN;
                    Item.Margin = new Thickness(10);

                    Item.Children.Add(catalogThreads.Last().threadImage);

                    //panel.Children.Add(catalogThreads.Last().threadImage);

                    //threadPanel.Children.Add(Bitmap);

                    TextBlock block = new TextBlock();
                    block.TextWrapping = TextWrapping.Wrap;
                    string subject = catalogThreads.Last().sub;
                    string tease = catalogThreads.Last().teaser;
                    block.Text = subject.Length > 200 ? subject.Substring(0, 200) + "..." : subject;
                    block.Text += "\n";
                    block.Text += tease.Length > 500 ? tease.Substring(0, 500) + "..." : tease;
                    Item.Children.Add(block);
                    panel.Children.Add(Item);
                });
            }
        }

        public async void buildCatalogInfoAsync(WrapPanel panel)
        {
            await Task.Run(() => buildCatalogInfo(panel)); ;
        }
    }
}
