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

using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

using System.Text.Json;
using System.Drawing;
using System.Drawing.Imaging;
using Catalog;

public class catalogThread
{
    public long date { get; set; }
    public string imgurl { get; set; } = "test";
    public string sub { get; set; }
    public string teaser { get; set; }

    public string threadId { get; set; }
}



namespace AutoPape
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        void getCatalog()
        {

            using var client = new HttpClient();

            //getCatalog();

            //var task = getCatalog();

            var task = client.GetStringAsync("https://boards.4chan.org/wg/catalog");

            //task.Wait();

            string result = task.GetAwaiter().GetResult();
            Console.WriteLine(result);
            Console.WriteLine("Got catalog");


            Regex rxFullJson = new Regex("\\{\\\"threads\\\".*?\\};");
            Regex rxThreads = new Regex("\\\"[0-9]*\\\":.*?},.*?\\},");

            Regex rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/wg\/[0-9]+s?\.(?i)(jpg|png|jpeg)");
            Regex rxNames = new Regex("[0-9]+");

            string FullJson = rxFullJson.Match(result).Value;
            var Threads = rxThreads.Matches(FullJson);

            Threads[0].Value.IndexOf(":");

            foreach (Match thread in Threads)
            {
                string testDecode = thread.Value.Substring(thread.Value.IndexOf(':') + 1).TrimEnd(',', '}') + "}";
                catalogThread test = new catalogThread();
                test = JsonSerializer.Deserialize<catalogThread>(testDecode);
                test.threadId = thread.Value.Split(':').First().Trim('\"');
                Console.WriteLine(test.threadId);
                Console.WriteLine(test.sub);
                Console.WriteLine("\t" + test.teaser);

                string ThreadInfo = $"{test.threadId}\n{test.sub}\n{test.teaser}\n";

                task = client.GetStringAsync($"https://boards.4chan.org/wg/thread/{test.threadId}");
                result = task.GetAwaiter().GetResult();
                var images = rxImages.Matches(result);
                int image = 0;
                int linkNum = 0;

                var thumbTask = client.GetByteArrayAsync($"https://{images[2].Value}");
                byte[] thumbNail = thumbTask.GetAwaiter().GetResult();
                var ms = new System.IO.MemoryStream(thumbNail);
                Bitmap thumbNailBitmap = new Bitmap(ms);

                //thumbNailBitmap.Save(ms,)
                

                foreach (Match match in images)
                {
                    if (image == 0)
                    {
                        image++;
                        continue;
                    }
                    else if (image == 1)
                    {
                        image++;
                        ThreadInfo += $"Image {linkNum}: {match.Value}\n";
                    }
                    else if (image == 2)
                    {
                        image = 0;
                        ThreadInfo += $"Thumb {linkNum}: {match.Value}\n";
                        linkNum++;
                    }
                }
                //System.Windows.Controls.Image cThumb = new System.Windows.Controls.Image();
                //cThumb.Source = thumbNailBitmap;
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    ms.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                    image.Source = bitmapImage;

                    threadPanel.Children.Add(image);

                    //threadPanel.Children.Add(Bitmap);

                    TextBlock block = new TextBlock();
                    block.TextWrapping = TextWrapping.Wrap;
                    block.Text = ThreadInfo;
                    threadPanel.Children.Add(block);
                });
                   
                
            }
        }
        async void getCatalogAsync()
        {
            await Task.Run(getCatalog);
        }

        public MainWindow()
        {

            //ThreadCatalog catalog = new ThreadCatalog("wg");
            InitializeComponent();
            //catalog.init();
            //testControl.DataContext = catalog;
            //this.RemoveLogicalChild(catalog.scrollPanelCatalog);
            //stackPanelTest.Children.Add(catalog.scrollPanelCatalog);
            //catalog.testButton.IsEnabled = false;
            Catalog.Catalog catalog = new Catalog.Catalog("wg", catalogPanel, threadPanel);
            catalog.buildCatalogInfoAsync();
            
        }
    }
}


