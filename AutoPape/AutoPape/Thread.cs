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
        vertical,
        horizontal
    }

    public class ThreadImage
    {
        //public imageInfo ImageInfo

        [XmlAttribute("Name")]
        public string imagename { get; set; }
        [XmlAttribute("Width")]
        public int width = 1;
        [XmlAttribute("Height")]
        public int height = 1;
        [XmlIgnore]
        public orientation orientation
        {
            get
            {
                return width >= height ? orientation.horizontal : orientation.vertical;
            }
        }
        [XmlIgnore]
        public double aspectRatio
        {
            get
            {
                return orientation == orientation.horizontal ? 
                    (double)width / (double)height : (double)height / (double)width;
            }
        }
        [XmlIgnore]
        public int resolution
        {
            get
            {
                return width >= height ? height : width;
            }
        }
        [XmlIgnore]
        public int thumbWidth = 1;
        [XmlIgnore]
        public int thumbHeight = 1;
        [XmlIgnore]
        public orientation thumbOrientation
        {
            get
            {
                return thumbWidth >= thumbHeight ? orientation.horizontal : orientation.vertical;
            }
        }
        [XmlIgnore]
        public double thumbAspectRatio
        {
            get
            {
                return thumbOrientation == orientation.horizontal ?
                    (double)thumbWidth / (double)thumbHeight : (double)thumbHeight / (double)thumbWidth;
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
        Regex rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/wg\/[0-9]+s?\.(?i)[a-zA-Z0-9]+");
        [XmlIgnore]
        Regex rxBlock;
        [XmlIgnore]
        string extentions = "jpg|png|jpeg";
        [XmlIgnore]
        Regex rxNames = new Regex("[0-9]+");
        [XmlIgnore]
        HttpClient client = null;
        [XmlIgnore]
        public ThreadPanelManager threadPanel = null;
        [XmlArray("ThreadImages")]
        public List<ThreadImage> threadImages;
        [XmlIgnore]
        public bool fromDisk;

        public Thread()
        {
            board = "";
            threadId = "";
            url = "";
            client = new HttpClient();
            threadImages = new List<ThreadImage>();
        }

        public Thread(string board, string threadId, ThreadPanelManager threadPanel, string sub, string teaser)
        {
            this.threadId = threadId;
            this.board = board;
            this.sub = sub;
            this.teaser = teaser;
            url = $"https://boards.4chan.org/{board}/thread/{threadId}";
            this.threadPanel = threadPanel;
            threadImages = new List<ThreadImage>();
            client = new HttpClient();
            rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/"+board+@"\/[0-9]+s?\.(?i)[a-zA-Z0-9]+");
            rxBlock = new Regex(@"\<blockquote.*?\<\/blockquote\>");
        }

        private void buildThreadImageInfo()
        {
            foreach (var image in threadImages)
            {
                Image thumb =
                    Utility.imageFromURL(
                        "https://" + image.thumburl,
                        client,
                        image.thumburl == null);
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    image.thumbWidth = ((BitmapImage)thumb.Source).PixelWidth;
                    image.thumbHeight = ((BitmapImage)thumb.Source).PixelHeight;
                });

            }
        }

        public async void buildThreadImageInfoAsync()
        {
            await Task.Run(() => buildThreadImageInfo());
        }

        public void buildThreadFromWeb(bool needsTeaserImage = false)
        {
            fromDisk = false;
            var task = client.GetStringAsync(url);
            string result = task.GetAwaiter().GetResult();
            var Blocks = rxBlock.Matches(result);
            foreach(var badBlock in Blocks)
            {
                var imageLinks = rxImages.Matches(badBlock.ToString());
                var replaceString = badBlock.ToString();
                foreach(var link in imageLinks)
                {
                    replaceString = replaceString.Replace(link.ToString(), "[REMOVED LINK]");
                }
                result = result.Replace(badBlock.ToString(), replaceString);
            }    
            var images = rxImages.Matches(result);
            int imageNum = 0;
            bool firstThumb = true;
            bool skipped = false;

            foreach (Match match in images)
            {
                if (imageNum == 0)
                {
                    imageNum++;
                    if(!extentions.Contains(Utility.imageExtention(match.Value)))
                    {
                        skipped = true;
                        continue;
                    }
                    threadImages.Add(new ThreadImage());
                    threadImages.Last().imagename = rxNames.Match(match.Value.Substring(9)).Value;
                    continue;
                }
                else if (imageNum == 1)
                {
                    imageNum++;
                    if (skipped) continue;
                    threadImages.Last().imageurl = match.Value;
                }
                else if (imageNum == 2)
                {
                    imageNum = 0;
                    if(skipped)
                    {
                        skipped = false;
                        continue;
                    }
                    threadImages.Last().thumburl = match.Value;
                    if(firstThumb && needsTeaserImage)
                    {
                        teaserThumb =
                            Utility.imageFromURL(match.Value, client, false);
                        firstThumb = false;
                    }
                }
            }
            //buildThreadImageInfo();
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
            stream.Flush();
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
            this.rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/" + board + @"\/[0-9]+s?\.(?i)[a-zA-Z0-9]+");
            this.rxBlock = new Regex(@"\<blockquote.*?\<\/blockquote\>");
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
                    threadImage.thumburl = Utility.pathToImage(board, threadId, threadImage.imagename, imageType.thumbnail);
                });
            }
        }

        public void setThreadContent(List<Image> thumbs)
        {
            if (!threadPanel.startProc(threadImages.Count())) return;
            for(int i = 0; i < threadImages.Count(); i++)
            {
                if (fromDisk)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        thumbs[i].Source = new BitmapImage(new Uri(threadImages[i].thumburl));
                    });
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
                    thumbs[i].Margin = new Thickness(10);
                    threadPanel.Add(thumbs[i], i);
                });
            }
            threadPanel.endProc();
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
            if (fromDisk) return;
            if (!threadPanel.startProc(threadImages.Count(), false)) return;

            string fullDirectory = Utility.pathToImageDirectory(board, threadId, imageType.fullImage);
            string thumbDirecotry = Utility.pathToImageDirectory(board, threadId, imageType.thumbnail);
            Directory.CreateDirectory(fullDirectory);
            Directory.CreateDirectory(thumbDirecotry);

            int image = 0;
            foreach(var thread in threadImages)
            {
                if(thread.imagename != null && 
                    !File.Exists(Utility.pathToImage(board, threadId, thread.imagename, imageType.fullImage)) ||
                    !File.Exists(Utility.pathToImage(board, threadId, thread.imagename, imageType.thumbnail)))
                {
                    Image toSave =
                    Utility.imageFromURL(
                        "https://" + thread.imageurl,
                        client,
                        thread.imageurl == null);
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        thread.width = ((BitmapImage)toSave.Source).PixelWidth;
                        thread.height = ((BitmapImage)toSave.Source).PixelHeight;
                    });
                    saveImage(fullDirectory, thread.imagename, toSave);
                    Image thumbToSave =
                    Utility.imageFromURL(
                        "https://" + thread.thumburl,
                        client,
                        thread.thumburl == null);
                    saveImage(thumbDirecotry, thread.imagename, thumbToSave);
                }
                threadPanel.Add(image);
                image++;
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

            threadPanel.endProc();
        }

        public async void saveThreadAsync()
        {
            await Task.Run(() => saveThread()); ;
        }
        public void saveImage(string path, string name, Image toSave)
        {
            BitmapImage imageBitmap = new BitmapImage();
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                imageBitmap = ((BitmapImage)toSave.Source);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                //Dont need to save if I already have it
                if(!File.Exists(Path.Combine(path, name + ".png")))
                {
                    FileStream stream = new FileStream(Path.Combine(path, name + ".png"), FileMode.Create);
                    encoder.Interlace = PngInterlaceOption.On;
                    encoder.Frames.Add(BitmapFrame.Create(imageBitmap));
                    encoder.Save(stream);
                    stream.Flush();
                    stream.Close();
                }
            });
        }
    }
}
