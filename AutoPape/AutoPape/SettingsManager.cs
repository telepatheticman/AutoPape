using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace AutoPape
{
    enum archiveMode
    {
        everything,
        whitelist,
        current,
        none
    }

    enum threadSaveMode
    {
        everything,
        fit
    }

    public class BlackList
    {
        [XmlArrayAttribute("Images")]
        [XmlArrayItem("Image")]
        public List<string> images;
        [XmlArrayAttribute("Threads")]
        [XmlArrayItem("Thread")]
        public List<string> threads;
        [XmlArrayAttribute("KeyWords")]
        [XmlArrayItem("KeyWord")]
        public List<string> keyWords;

        public BlackList()
        {
            images = new List<string>();
            threads = new List<string>();
            keyWords = new List<string>();
        }
    }
    
    [XmlRoot("Settings", Namespace = "")]
    public class SettingsManager
    {
        [XmlIgnore]
        private XmlSerializer serializer;
        [XmlElement("BlackList")]
        public BlackList blackList;
        [XmlArrayAttribute("WhiteListedKeyWords")]
        [XmlArrayItem("KeyWord")]
        public List<string> keyWords;
        [XmlElement("Monitors")]
        public WallpaperManager wallpaperManager;
        public SettingsManager(WallpaperManager wallpaperManager) : this()
        {
            this.wallpaperManager = wallpaperManager;

        }
        public SettingsManager()
        {
            serializer = new XmlSerializer(this.GetType());
            blackList = new BlackList();
            keyWords = new List<string>();
            wallpaperManager = new WallpaperManager();
        }



        public bool validThread(Thread check)
        {
            bool valid = false;

            foreach (var keyword in keyWords)
            {
                if (check.sub.Contains(keyword) || check.teaser.Contains(keyword))
                {
                    valid = true;
                    break;
                }
            }

            foreach (var keyword in blackList.keyWords)
            {
                if (check.sub.Contains(keyword) || check.teaser.Contains(keyword))
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

        public void saveSettings()
        {
            FileStream stream = new FileStream(
                Path.Combine(Utility.pathToParent(), "Settings.xml"), 
                FileMode.Create);
            serializer.Serialize(stream, this);
        }

        public bool loadSettings()
        {
            FileStream stream = new FileStream(
                Path.Combine(Utility.pathToParent(), "Settings.xml"),
                FileMode.Open);
            SettingsManager loaded;
            try
            {
                loaded = (SettingsManager)serializer.Deserialize(stream);
                loaded.wallpaperManager.refreshScreens();
            }
            catch(Exception ex)
            {
                wallpaperManager.getScreenSpace();
                return false;
            }
            copyFromLoaded(loaded);
            return true;
        }

        private void copyFromLoaded(SettingsManager loaded)
        {
            serializer = loaded.serializer;
            blackList = loaded.blackList;
            keyWords = loaded.keyWords;
            wallpaperManager = loaded.wallpaperManager;
        }
    }
}
