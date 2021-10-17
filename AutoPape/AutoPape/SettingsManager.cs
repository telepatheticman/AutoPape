﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AutoPape
{

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
        public SettingsManager()
        {
            serializer = new XmlSerializer(this.GetType());
            blackList = new BlackList();
            keyWords = new List<string>();
        }

        public void saveSettings()
        {
            FileStream stream = new FileStream(
                Path.Combine(Utility.pathToParent(), "Settings.xml"), 
                FileMode.Create);
            serializer.Serialize(stream, this);
        }

        public void loadSettings()
        {
            FileStream stream = new FileStream(
                Path.Combine(Utility.pathToParent(), "Settings.xml"),
                FileMode.Open);
            SettingsManager loaded = (SettingsManager)serializer.Deserialize(stream);
            copyFromLoaded(loaded);
        }

        private void copyFromLoaded(SettingsManager loaded)
        {
            serializer = loaded.serializer;
            blackList = loaded.blackList;
            keyWords = loaded.keyWords;
        }
    }
}