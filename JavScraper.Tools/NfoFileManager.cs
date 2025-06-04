using JavScraper.Tools.Entities;
using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JavScraper.Tools
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

        public string GetPlot()
        {
            return GetElement("plot").Value;
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

        /// <summary>
        /// 保存元数据。
        /// </summary>
        /// <param name="title">视频标题。</param>
        /// <param name="originalTitle">原始标题。</param>
        /// <param name="sortTitle">排序标题。</param>
        /// <param name="date">日期。</param>
        /// <param name="metatubeId">metatubeId</param>
        /// <param name="actors">演员。</param>
        /// <param name="genres">分类。</param>
        /// <param name="tags">标签。</param>
        public void SaveMetadata(string title, string originalTitle, string sortTitle, string plot, string metatubeId, List<string> actors, List<string> genres, List<string> tags, int? year = null, string date = null)
        {
            GetElement("title").Value = title;
            GetElement("originaltitle").Value = originalTitle;
            GetElement("sorttitle").Value = sortTitle;
            if (!String.IsNullOrEmpty(metatubeId))
            {
                GetElement("metatubeid").Value = $"JavBus:{metatubeId}";
            }
            if (!String.IsNullOrEmpty(plot))
            {
                GetElement("plot").Value = plot;
            }
            if (year.HasValue)
            {
                GetElement("year").Value = year.ToString();
            }
            if (!String.IsNullOrEmpty(date))
            {
                GetElement("releasedate").Value = date;
                GetElement("premiered").Value = date;
            }
            // 演员
            if (actors != null && actors.Count > 0)
            {
                SetActors(actors);
            }
            // 分类
            _nfoContent.Elements("genre").Remove();
            foreach (var genre in genres)
            {
                _nfoContent.Add(new XElement("genre", genre));
            }

            // 标签
            _nfoContent.Elements("tag").Remove();
            foreach (var tag in tags)
            {
                _nfoContent.Add(new XElement("tag", tag));
            }

            SaveNfoFile();
        }

        /// <summary>
        /// 获取视频信息。
        /// </summary>
        /// <returns></returns>
        public JavVideo GetJavVideo()
        {
            return new JavVideo()
            {
                Title = GetTitle(),
                OriginalTitle = GetOriginalTitle(),
                SortTitle = GetSortTitle(),
                Actors = GetActors().Select(e => (e.Name)).ToList(),
                Genres = GetGenres(),
                Tags = GetTags()
            };
        }

    }
}
