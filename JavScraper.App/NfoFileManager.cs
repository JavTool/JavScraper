using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JavScraper.App
{
    public class NfoFileManager
    {
        private string _filePath;
        private XElement _nfoContent;

        public NfoFileManager(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new ArgumentException("Invalid file path.");
            }

            _filePath = filePath;
            _nfoContent = LoadNfoFile();
        }

        public override string ToString()
        {
            return _nfoContent?.ToString();
        }

        private XElement LoadNfoFile()
        {
            try
            {
                return XElement.Load(_filePath);
            }
            catch (Exception ex)
            {
                //throw new Exception("Failed to load NFO file.", ex);

                return null;
            }
        }

        private void SaveNfoFile()
        {
            try
            {
                _nfoContent.Save(_filePath);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save NFO file.", ex);
            }
        }

        private XElement GetElement(string elementName)
        {
            XElement element = _nfoContent.Element(elementName);

            if (element == null)
            {
                element = new XElement(elementName);
                _nfoContent.Add(element);
            }

            return element;
        }

        public string GetTitle()
        {
            return GetElement("title").Value;
        }

        public void SetTitle(string value)
        {
            GetElement("title").Value = value;
            SaveNfoFile();
        }

        public string GetSortTitle()
        {
            return GetElement("sorttitle").Value;
        }

        public void SetSortTitle(string value)
        {
            GetElement("sorttitle").Value = value;
            SaveNfoFile();
        }

        public string GetOriginalTitle()
        {
            return GetElement("originaltitle").Value;
        }

        public void SetOriginalTitle(string value)
        {
            GetElement("originaltitle").Value = value;
            SaveNfoFile();
        }




        public List<string> GetGenres()
        {
            return _nfoContent.Elements("genre").Select(e => e.Value).ToList();
        }

        public void SetGenres(List<string> genres)
        {
            // Remove existing genres
            _nfoContent.Elements("genre").Remove();

            // Add new genres
            foreach (var genre in genres)
            {
                _nfoContent.Add(new XElement("genre", genre));
            }

            SaveNfoFile();
        }

        public List<string> GetTags()
        {
            return _nfoContent.Elements("tag").Select(e => e.Value).ToList();
        }

        public void SetTags(List<string> tags)
        {
            // Remove existing tags
            _nfoContent.Elements("tag").Remove();

            // Add new tags
            foreach (var tag in tags)
            {
                _nfoContent.Add(new XElement("tag", tag));
            }

            SaveNfoFile();
        }

        public List<(string Name, string Type)> GetActors()
        {
            return _nfoContent.Elements("actor")
                              .Select(e => (
                                  Name: e.Element("name")?.Value,
                                  Type: e.Element("type")?.Value))
                              .ToList();
        }

        public void SetActors(List<(string Name, string Type)> actors)
        {
            _nfoContent.Elements("actor").Remove();
            foreach (var actor in actors)
            {
                var actorElement = new XElement("actor",
                    new XElement("name", actor.Name),
                    new XElement("type", actor.Type)
                );
                _nfoContent.Add(actorElement);
            }
            SaveNfoFile();
        }

        public void SetActors(List<string> actors)
        {
            _nfoContent.Elements("actor").Remove();
            foreach (var actor in actors)
            {
                var actorElement = new XElement("actor",
                    new XElement("name", actor),
                    new XElement("type", "Actor")
                );
                _nfoContent.Add(actorElement);
            }
            SaveNfoFile();
        }
        public void SaveMetadata(string title, string originalTitle, string sortTitle, List<string> genres, List<string> tags)
        {
            GetElement("title").Value = title;
            GetElement("originaltitle").Value = originalTitle;
            GetElement("sorttitle").Value = sortTitle;

            // 处理分类
            _nfoContent.Elements("genre").Remove();
            foreach (var genre in genres)
            {
                _nfoContent.Add(new XElement("genre", genre));
            }

            // 处理标签
            _nfoContent.Elements("tag").Remove();
            foreach (var tag in tags)
            {
                _nfoContent.Add(new XElement("tag", tag));
            }


            SaveNfoFile();
        }
    }
}
