﻿using System;
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
        [XmlElement("ArchiveSettings")]
        public ArchiveSettings archiveSettings { get; set; }
        [XmlElement("BlackList")]
        public BlackList blackList;
        [XmlArrayAttribute("WhiteListedKeyWords")]
        [XmlArrayItem("KeyWord")]
        public List<string> keyWords;
        [XmlElement("Monitors")]
        public WallpaperManager wallpaperManager;
        [XmlElement("SaveDirectory")]
        public string saveDirectory;
        [XmlElement("OldSaveDirectory")]
        public string oldSaveDirectory;
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
            archiveSettings = new ArchiveSettings();
        }



        public bool validThread(Thread check)
        {
            bool valid = true;

            /*foreach (var keyword in keyWords)
            {
                if (check.sub.Contains(keyword) || check.teaser.Contains(keyword))
                {
                    valid = true;
                    break;
                }
            }*/

            foreach (var keyword in blackList.keyWords)
            {
                if (check.sub.ToLower().Contains(keyword.ToLower()) ||
                    check.teaser.ToLower().Contains(keyword.ToLower()))
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
            keyWords = loaded.keyWords;
            wallpaperManager = loaded.wallpaperManager;
            saveDirectory = loaded.saveDirectory;
            oldSaveDirectory = loaded.oldSaveDirectory;
            archiveSettings = loaded.archiveSettings;
            if (string.IsNullOrEmpty(saveDirectory) || !Directory.Exists(saveDirectory))
            {
                saveDirectory = Utility.pathToParent();
            }
            if (!string.IsNullOrEmpty(oldSaveDirectory) && Directory.Exists(oldSaveDirectory))
            {
                Utility.deleteOld(oldSaveDirectory);
                oldSaveDirectory = "";
            }
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
