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
using System.Windows.Media.Imaging;
using BitmapImage = System.Windows.Media.Imaging.BitmapImage;
using PngBitmapEncoder = System.Windows.Media.Imaging.PngBitmapEncoder;
using System.Xml;
using System.Xml.Serialization;

namespace AutoPape
{
    public class threadImage
    {
        [XmlIgnore]
        public Image thumb;
        [XmlIgnore]
        public Image full;
        [XmlAttribute]
        public string imagename { get; set; }
        [XmlIgnore]
        public string extention { get; set; }
        [XmlIgnore]
        public string imageurl { get; set; }
        [XmlIgnore]
        public string thumburl { get; set; }
    }
    public class Thread
    {
        [XmlIgnore]
        string url;
        [XmlAttribute]
        public string board;
        [XmlAttribute]
        public string threadId;
        public string subject;
        public string teaser;
        [XmlIgnore]
        Regex rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/wg\/[0-9]+s?\.(?i)(jpg|png|jpeg)");
        [XmlIgnore]
        Regex rxNames = new Regex("[0-9]+");
        [XmlIgnore]
        Regex rxExtention = new Regex("(jpg|png|jpeg)");
        [XmlIgnore]
        HttpClient client = null;
        [XmlIgnore]
        StackPanel threadPanel = null;

        public List<threadImage> threadImages;

        public Thread()
        {
            board = "";
            threadId = "";
            url = "";
        }

        public Thread(string board, string threadId, StackPanel stackPanel, string sub, string teaser)
        {
            this.threadId = threadId;
            this.board = board;
            subject = sub;
            this.teaser = teaser;
            url = $"https://boards.4chan.org/{board}/thread/{threadId}";
            threadPanel = stackPanel;
            threadImages = new List<threadImage>();
            client = new HttpClient();
            rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/"+board+@"\/[0-9]+s?\.(?i)(jpg|png|jpeg)");
        }

        void buildThreadFromWeb()
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
                    threadImages.Last().imagename = rxNames.Match(match.Value.Substring(9)).Value;
                    threadImages.Last().extention = rxExtention.Match(match.Value).Value;
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
            saveThread();
        }

        public async void buildThreadFromWebAsync()
        {
            await Task.Run(() => buildThreadFromWeb()); ;
        }

        public void saveThread()
        {
            string fullDirectory = Utility.pathToImageDirectory(board, threadId, imageType.fullImage);
            string thumbDirecotry = Utility.pathToImageDirectory(board, threadId, imageType.thumbnail);
            Directory.CreateDirectory(fullDirectory);
            Directory.CreateDirectory(thumbDirecotry);

            foreach(var thread in threadImages)
            {
                if(thread.imagename != null)
                {
                    Image toSave =
                    Utility.imageFromURL(
                        "https://" + thread.imageurl,
                        client,
                        thread.imageurl == null);
                    saveImage(fullDirectory, thread.imagename, toSave);
                }
                if(thread.thumb != null)
                {
                    saveImage(thumbDirecotry, thread.imagename, thread.thumb);
                }
            }

            XmlSerializer xmlSerializer = new XmlSerializer(this.GetType());
            FileStream stream = new FileStream(
                Path.Combine(
                    Utility.pathToThreadDirectory(board, threadId),
                    threadId + ".xml"
                    ), 
                FileMode.Create
                );
            xmlSerializer.Serialize(stream, this);
        }

        public void saveImage(string path, string name, Image toSave)
        {
            BitmapImage imageBitmap = new BitmapImage();
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                imageBitmap = ((BitmapImage)toSave.Source);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                FileStream stream = new FileStream(Path.Combine(path, name + ".png"), FileMode.Create);
                encoder.Interlace = PngInterlaceOption.On;
                encoder.Frames.Add(BitmapFrame.Create(imageBitmap));
                encoder.Save(stream);
                stream.Flush();
                stream.Close();
            });
        }

        public void clearChildren()
        {
            threadPanel.Children.Clear();
        }
    }
}
