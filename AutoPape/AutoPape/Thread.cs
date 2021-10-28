using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using BitmapImage = System.Windows.Media.Imaging.BitmapImage;
using PngBitmapEncoder = System.Windows.Media.Imaging.PngBitmapEncoder;

namespace AutoPape
{
    public enum orientation
    {
        [XmlEnum("Vertical")]
        vertical,
        [XmlEnum("Horizontal")]
        horizontal
    }

    public class threadImage
    {
        //public imageInfo ImageInfo

        [XmlAttribute("Name")]
        public string imagename { get; set; }
        [XmlAttribute("Width")]
        public int width = 1;
        [XmlAttribute("Height")]
        public int height = 1;
        [XmlAttribute("Orientation")]
        public orientation orientation
        {
            get
            {
                return width > height ? orientation.horizontal : orientation.vertical;
            }
        }

        [XmlIgnore]
        public string imageurl { get; set; }
        [XmlIgnore]
        public string thumburl { get; set; }
    }
    public class Thread
    {
        [XmlIgnore]
        public Image teaserThumb;
        [XmlIgnore]
        public string url;
        [XmlAttribute]
        public string board;
        [XmlAttribute]
        public string threadId;
        [XmlElement("Subject")]
        public string sub;
        [XmlElement("Teaser")]
        public string teaser;
        [XmlIgnore]
        Regex rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/wg\/[0-9]+s?\.(?i)(jpg|png|jpeg)");
        [XmlIgnore]
        Regex rxNames = new Regex("[0-9]+");
        [XmlIgnore]
        HttpClient client = null;
        [XmlIgnore]
        public StackPanel threadPanel = null;
        [XmlArray("ThreadImages")]
        public List<threadImage> threadImages;
        [XmlIgnore]
        public bool fromDisk;

        public Thread()
        {
            board = "";
            threadId = "";
            url = "";
            client = new HttpClient();
            threadImages = new List<threadImage>();
        }

        public Thread(string board, string threadId, StackPanel stackPanel, string sub, string teaser)
        {
            this.threadId = threadId;
            this.board = board;
            this.sub = sub;
            this.teaser = teaser;
            url = $"https://boards.4chan.org/{board}/thread/{threadId}";
            threadPanel = stackPanel;
            threadImages = new List<threadImage>();
            client = new HttpClient();
            rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/"+board+@"\/[0-9]+s?\.(?i)(jpg|png|jpeg)");
        }

        void buildThreadImageInfo()
        {
            foreach (var image in threadImages)
            {
                Image full =
                    Utility.imageFromURL(
                        "https://" + image.imageurl,
                        client,
                        image.imageurl == null);

                image.width = (int)full.Width;
                image.height = (int)full.Height;

            }
        }

        public async void buildThreadImageInfoAsync()
        {
            await Task.Run(() => buildThreadImageInfo()); ;
        }

        void buildThreadFromWeb()
        {
            fromDisk = false;
            var task = client.GetStringAsync(url);
            string result = task.GetAwaiter().GetResult();
            var images = rxImages.Matches(result);
            int imageNum = 0;

            foreach (Match match in images)
            {
                if (imageNum == 0)
                {
                    threadImages.Add(new threadImage());
                    threadImages.Last().imagename = rxNames.Match(match.Value.Substring(9)).Value;
                    imageNum++;
                    continue;
                }
                else if (imageNum == 1)
                {
                    imageNum++;
                    threadImages.Last().imageurl = match.Value;
                }
                else if (imageNum == 2)
                {
                    imageNum = 0;
                    threadImages.Last().thumburl = match.Value;
                }
            }

            /*foreach(var image in threadImages)
            {

                Image thumb = 
                    Utility.imageFromURL(
                        "https://" + image.thumburl, 
                        client, 
                        image.thumburl == null);
            }*/
            //saveThread();
        }

        public async void buildThreadFromWebAsync()
        {
            await Task.Run(() => buildThreadFromWeb()); ;
        }

        public void buildThreadFromDisk(string board, string thread)
        {
            fromDisk = true;
            XmlSerializer xmlSerializer = new XmlSerializer(this.GetType());
            FileStream stream = new FileStream(
                Path.Combine(
                    Utility.pathToThreadDirectory(board, thread), $"{thread}.xml"),
                FileMode.Open);
            Thread toLoad = (Thread)xmlSerializer.Deserialize(stream);
            stream.Close();
            buildThreadFromDisk(toLoad);
        }

        private void buildThreadFromDisk(Thread from)
        {
            this.board = from.board;
            this.threadId = from.threadId;
            this.sub = from.sub;
            this.teaser = from.teaser;
            this.threadImages = from.threadImages;
            int i = 0;
            foreach(var threadImage in this.threadImages)
            {
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    Image thumb = new Image();
                    if(i == 0)
                    {
                        thumb.Source = new BitmapImage(new Uri(Utility.pathToImage(board, threadId, threadImage.imagename, imageType.thumbnail)));
                        teaserThumb = thumb;
                        i++;
                    }
                    threadImage.imageurl = Utility.pathToImage(board, threadId, threadImage.imagename, imageType.fullImage);
                });
            }
        }

        public void setThreadContent(List<Image> thumbs)
        {
            for(int i = 0; i < threadImages.Count(); i++)
            {
                if (fromDisk)
                {
                    thumbs[i].Source = new BitmapImage(new Uri(Utility.pathToImage(board, threadId, threadImages[i].imagename, imageType.thumbnail)));
                }
                else
                {
                    thumbs[i] =
                    Utility.imageFromURL(
                    "https://" + threadImages[i].thumburl,
                    client,
                    threadImages[i].thumburl == null);
                }
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    threadPanel.Children.Add(thumbs[i]);
                    thumbs[i].Margin = new Thickness(10);
                });
            }
        }

        public async void setThreadContentAsync()
        {
            List<Image> thumbs = new List<Image>();
            foreach(var threadImage in this.threadImages)
            {
                thumbs.Add(new Image());
            }
            await Task.Run(() => setThreadContent(thumbs));
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
                    Image thumbToSave =
                    Utility.imageFromURL(
                        "https://" + thread.thumburl,
                        client,
                        thread.thumburl == null);
                    saveImage(thumbDirecotry, thread.imagename, thumbToSave);
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
