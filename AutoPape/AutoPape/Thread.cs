using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AutoPape
{
    public class threadImage
    {
        public Image thumb;

        public Image full;
        public string imagename { get; set; }
        public string imageurl { get; set; }
        public string thumburl { get; set; }
    }
    public class Thread
    {
        string url;
        string threadId;
        Regex rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/wg\/[0-9]+s?\.(?i)(jpg|png|jpeg)");
        HttpClient client = null;
        StackPanel threadPanel = null;

        List<threadImage> threadImages;

        public Thread(string board, string threadId, StackPanel stackPanel)
        {
            this.threadId = threadId;
            url = $"https://boards.4chan.org/{board}/thread/{threadId}";
            threadPanel = stackPanel;
            threadImages = new List<threadImage>();
            client = new HttpClient();
        }

        void buildImages()
        {
            var task = client.GetStringAsync(url);
            string result = task.GetAwaiter().GetResult();
            var images = rxImages.Matches(result);
            int image = 0;

            foreach (Match match in images)
            {
                if (image == 0)
                {
                    threadImages.Add(new threadImage());
                    image++;
                    continue;
                }
                else if (image == 1)
                {
                    image++;
                    threadImages.Last().imageurl = match.Value;
                }
                else if (image == 2)
                {
                    image = 0;
                    threadImages.Last().thumburl = match.Value;
                }
            }

            foreach(var thread in threadImages)
            {

                thread.thumb = 
                    Utility.imageFromURL(
                        "https://" + thread.thumburl, 
                        client, 
                        thread.thumburl == null);



                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    threadPanel.Children.Add(thread.thumb);
                    thread.thumb.Margin = new Thickness(10);
                });

            }
        }

        public async void buildImagesAsync()
        {
            await Task.Run(() => buildImages()); ;
        }

        public void clearChildren()
        {
            threadPanel.Children.Clear();
        }
    }
}
