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

    public class ArchiveSettings
    {
        [XmlElement("AutoArchive")]
        public bool autoArchive { get; set; }
        [XmlElement("ArchiveBadFit")]
        public bool archiveBadFit { get; set; }
        [XmlElement("ArchiveBlacklist")]
        public bool archiveBlacklist { get; set; }
        [XmlElement("ArchiveNonWhitelist")]
        public bool archiveNonWhitelist { get; set; }
        [XmlElement("SaveBadFit")]
        public bool saveBadFit { get; set; }
        [XmlElement("SaveBlacklist")]
        public bool saveBlacklist { get; set; }
        [XmlElement("SaveNonWhitelist")]
        public bool saveNonWhitelist { get; set; }
        [XmlElement("LimitSpace")]
        public bool limitSpace { get; set; }
        [XmlElement("LimitBytes")]
        public ulong limitBytes { get; set; }
        [XmlElement("Units")]
        public string limitUnits { get; set; }

        public void buildDefault()
        {
            autoArchive = true;
            archiveBadFit = true;
            archiveBlacklist = true;
            archiveNonWhitelist = true;

            saveBadFit = true;
            saveBlacklist = true;
            saveNonWhitelist = true;
            limitSpace = false;
            limitBytes = 10;
            limitBytes *= 1024;
            limitBytes *= 1024;
            limitBytes *= 1024;

            limitUnits = "GB";

        }

    }


    //TODO: Make whitelist and blacklist either the same object or inharit from same object
    public class WhiteList
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
        [XmlAttribute("Enabled")]
        public bool enabeld;

        public WhiteList()
        {
            images = new List<string>();
            threads = new List<string>();
            keyWords = new List<string>();
            enabeld = false;
        }
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
        [XmlAttribute("Enabled")]
        public bool enabeld;

        public BlackList()
        {
            images = new List<string>();
            threads = new List<string>();
            keyWords = new List<string>();
            enabeld = true;
        }
    }
    
    [XmlRoot("Settings", Namespace = "")]
    public class SettingsManager
    {
        [XmlIgnore]
        private XmlSerializer serializer;
        [XmlElement("ArchiveSettings")]
        public ArchiveSettings archiveSettings { get; set; }
        [XmlElement("BlackList")]
        public BlackList blackList;
        [XmlElement("WhiteList")]
        public WhiteList whiteList;
        [XmlElement("Monitors")]
        public WallpaperManager wallpaperManager;
        [XmlElement("SaveDirectory")]
        public string saveDirectory;
        [XmlElement("OldSaveDirectory")]
        public string oldSaveDirectory;
        [XmlElement("TimeInterval")]
        public int interval;
        [XmlElement("WG")]
        public bool usingWG;
        [XmlElement("W")]
        public bool usingW;
        [XmlElement("WhiteOverBlack")]
        public bool whiteOverBlack;

        [XmlIgnore]
        public string customDirectory
        {
            get
            {
                return Path.Combine(saveDirectory, "Custom");
            }
        }
        public SettingsManager(WallpaperManager wallpaperManager) : this()
        {
            this.wallpaperManager = wallpaperManager;

        }
        public SettingsManager()
        {
            serializer = new XmlSerializer(this.GetType());
            blackList = new BlackList();
            whiteList = new WhiteList();
            wallpaperManager = new WallpaperManager();
            archiveSettings = new ArchiveSettings();
            interval = 60;
            usingWG = true;
            usingW = false;
            whiteOverBlack = false;
        }


        //TODO: This can be written much better. Split later.
        public bool validThread(Thread check)
        {
            bool valid = true;
            if(!whiteOverBlack)
            {
                if (whiteList.enabeld)
                {
                    foreach (var keyword in whiteList.keyWords)
                    {
                        if (!check.sub.ToLower().Contains(keyword.ToLower()) ||
                            !check.teaser.ToLower().Contains(keyword.ToLower()))
                        {
                            valid = false;
                            break;
                        }
                    }
                }

                if (blackList.enabeld)
                {
                    foreach (var keyword in blackList.keyWords)
                    {
                        if (check.sub.ToLower().Contains(keyword.ToLower()) ||
                            check.teaser.ToLower().Contains(keyword.ToLower()))
                        {
                            valid = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (blackList.enabeld)
                {
                    foreach (var keyword in blackList.keyWords)
                    {
                        if (check.sub.ToLower().Contains(keyword.ToLower()) ||
                            check.teaser.ToLower().Contains(keyword.ToLower()))
                        {
                            valid = false;
                            break;
                        }
                    }
                }

                if (whiteList.enabeld)
                {
                    foreach (var keyword in whiteList.keyWords)
                    {
                        if (!check.sub.ToLower().Contains(keyword.ToLower()) ||
                            !check.teaser.ToLower().Contains(keyword.ToLower()))
                        {
                            valid = false;
                            break;
                        }
                    }
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
            stream.Close();
        }

        public bool loadSettings()
        {
            SettingsManager loaded;
            try
            {
                FileStream stream = new FileStream(
                    Path.Combine(Utility.pathToParent(), "Settings.xml"),
                    FileMode.Open);
                loaded = (SettingsManager)serializer.Deserialize(stream);
                stream.Close();
                loaded.wallpaperManager.refreshScreens();
            }
            catch(Exception ex)
            {
                buildDefaults();
                saveSettings();
                return false;
            }
            copyFromLoaded(loaded);
            return true;
        }

        private void copyFromLoaded(SettingsManager loaded)
        {
            serializer = loaded.serializer;
            blackList = loaded.blackList;
            whiteList = loaded.whiteList;
            wallpaperManager = loaded.wallpaperManager;
            saveDirectory = loaded.saveDirectory;
            oldSaveDirectory = loaded.oldSaveDirectory;
            archiveSettings = loaded.archiveSettings;
            interval = loaded.interval;
            usingW = loaded.usingW;
            usingWG = loaded.usingWG;
            whiteOverBlack = loaded.whiteOverBlack;
            if (string.IsNullOrEmpty(saveDirectory) || !Directory.Exists(saveDirectory))
            {
                saveDirectory = Utility.pathToParent();
            }
            if (!string.IsNullOrEmpty(oldSaveDirectory) && Directory.Exists(oldSaveDirectory))
            {
                Utility.deleteOld(oldSaveDirectory);
                oldSaveDirectory = "";
            }
            Directory.CreateDirectory(customDirectory);
            saveSettings();
        }

        private void buildDefaults()
        {
            blackList.keyWords.Add("NSFW");
            blackList.keyWords.Add("Not Safe For Work");
            blackList.keyWords.Add("Lewd");
            blackList.keyWords.Add("Nude");
            blackList.keyWords.Add("Naked");
            blackList.keyWords.Add("Hentai");
            blackList.keyWords.Add("Desktop");
            blackList.keyWords.Add("Homescreen");
            wallpaperManager.monitorSettings = new List<MonitorSetting>();
            wallpaperManager.getScreenSpace();
            saveDirectory = Utility.pathToParent();
            Directory.CreateDirectory(customDirectory);
        }

        #region Path Stuff
        public string pathToImage(string board, string thread, string image, imageType type)
        {
            string path = pathToImageDirectory(board, thread, type);
            path = Path.Combine(path, image + ".png");
            return path;
        }

        public string pathToImageDirectory(string board, string thread, imageType type)
        {
            string path = pathToThreadDirectory(board, thread);
            switch (type)
            {
                case imageType.thumbnail:
                    path = Path.Combine(path, Utility.thumbnailPath);
                    break;
                case imageType.fullImage:
                    path = Path.Combine(path, Utility.fullImagePath);
                    break;
            }
            return path;
        }

        public string pathToThreadDirectory(string board, string thread)
        {
            string path = pathToBoardDirectory(board);
            path = Path.Combine(path, thread);
            return path;
        }

        public string pathToBoardDirectory(string board)
        {
            string path = saveDirectory;
            path = Path.Combine(path, board);
            return path;
        }
        #endregion
    }
}
