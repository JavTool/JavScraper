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
    /// <summary>
    /// 表示并操作单个 NFO 文档的工具类。封装对 NFO （XML）文件的读取、写入和常用字段访问。
    /// 
    /// 设计要点：
    /// - 构造时加载指定路径的 NFO 文件（如果文件不存在会抛出异常）；
    /// - 提供对常见元素（title、originaltitle、sorttitle、genre、tag、actor 等）的读取与写入方法；
    /// - 写操作会立即保存回磁盘（调用 <see cref="SaveNfoFile"/>），以保证数据持久性；
    /// - 内部使用 LINQ to XML（<see cref="XElement"/>）表示和修改文档内容。
    /// </summary>
    public class NfoDocument
    {
        private string _filePath;
        private XElement _nfoContent;

        /// <summary>
        /// 使用指定文件路径创建一个 <see cref="NfoDocument"/> 实例并加载 NFO 内容。
        /// </summary>
        /// <param name="filePath">NFO 文件的完整路径。若路径为空或文件不存在则会抛出 <see cref="ArgumentException"/>。</param>
        /// <exception cref="ArgumentException">当 <paramref name="filePath"/> 为空或文件不存在时抛出。</exception>
        public NfoDocument(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new ArgumentException("Invalid file path.");
            }

            _filePath = filePath;
            _nfoContent = LoadNfoFile();
        }

        /// <summary>
        /// 返回当前 NFO 文档的 XML 字符串表示，用于调试或简单查看内容。
        /// </summary>
        /// <returns>若文档已加载则返回其 XML 文本，否则返回 <c>null</c>。</returns>
        public override string ToString()
        {
            return _nfoContent?.ToString();
        }

        /// <summary>
        /// 从磁盘加载并解析 NFO 文件为 <see cref="XElement"/> 表示。
        /// </summary>
        /// <returns>解析后的根元素；加载失败时返回 <c>null</c>（调用方可据此判断）。</returns>
        /// <remarks>此方法在解析异常时不会抛出，而是返回 <c>null</c>，以便上层代码决定如何处理空文档。</remarks>
        private XElement LoadNfoFile()
        {
            try
            {
                return XElement.Load(_filePath);
            }
            catch (Exception)
            {
                // 解析失败时返回 null，调用方会进行空值检查并处理
                return null;
            }
        }

        /// <summary>
        /// 将内存中的 NFO XML 内容保存回源文件路径。
        /// </summary>
        /// <exception cref="Exception">若保存失败会抛出异常以通知调用者。</exception>
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

        /// <summary>
        /// 获取指定名称的元素，如果不存在则创建并添加到根节点后返回。
        /// </summary>
        /// <param name="elementName">元素名称（区分大小写，匹配 NFO 的节点名，如 "title"、"genre" 等）。</param>
        /// <returns>目标元素实例（保证非 null）。</returns>
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

        /// <summary>
        /// 获取 <c>plot</c> 元素的文本内容（剧情/简介）。
        /// </summary>
        /// <returns>剧情文本，若元素为空则返回空字符串。</returns>
        public string GetPlot()
        {
            return GetElement("plot").Value;
        }

        /// <summary>
        /// 获取 NFO 中的标题（<c>title</c>）。
        /// </summary>
        /// <returns>标题文本。</returns>
        public string GetTitle()
        {
            return GetElement("title").Value;
        }

        /// <summary>
        /// 设置 NFO 中的 <c>title</c> 元素并立即保存文件。
        /// </summary>
        /// <param name="value">要写入的标题文本。</param>
        public void SetTitle(string value)
        {
            GetElement("title").Value = value;
            SaveNfoFile();
        }

        /// <summary>
        /// 获取 NFO 中的排序标题（<c>sorttitle</c>），通常用于按番号或自定义标识排序。
        /// </summary>
        /// <returns>排序标题文本。</returns>
        public string GetSortTitle()
        {
            return GetElement("sorttitle").Value;
        }

        /// <summary>
        /// 设置 <c>sorttitle</c> 并保存 NFO 文件。
        /// </summary>
        /// <param name="value">排序标题文本。</param>
        public void SetSortTitle(string value)
        {
            GetElement("sorttitle").Value = value;
            SaveNfoFile();
        }

        /// <summary>
        /// 获取 NFO 中的原始标题（<c>originaltitle</c>），通常为抓取到的站点原始名称。
        /// </summary>
        /// <returns>原始标题文本。</returns>
        public string GetOriginalTitle()
        {
            return GetElement("originaltitle").Value;
        }

        /// <summary>
        /// 设置 <c>originaltitle</c> 并保存 NFO 文件。
        /// </summary>
        /// <param name="value">原始标题文本。</param>
        public void SetOriginalTitle(string value)
        {
            GetElement("originaltitle").Value = value;
            SaveNfoFile();
        }




        /// <summary>
        /// 获取 NFO 中所有的 <c>genre</c> 节点值，作为类型/分类列表返回。
        /// </summary>
        /// <returns>分类名称的列表（可能为空）。</returns>
        public List<string> GetGenres()
        {
            return _nfoContent.Elements("genre").Select(e => e.Value).ToList();
        }

        /// <summary>
        /// 用给定的分类列表替换 NFO 中现有的 <c>genre</c> 节点，并保存文件。
        /// </summary>
        /// <param name="genres">要写入的分类名称列表。</param>
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

        /// <summary>
        /// 获取 NFO 中所有 <c>tag</c> 节点的值（标签列表）。
        /// </summary>
        /// <returns>标签字符串列表。</returns>
        public List<string> GetTags()
        {
            return _nfoContent.Elements("tag").Select(e => e.Value).ToList();
        }

        /// <summary>
        /// 用指定标签列表替换 NFO 中的 <c>tag</c> 节点并保存文件。
        /// </summary>
        /// <param name="tags">要写入的标签列表。</param>
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

        /// <summary>
        /// 获取 NFO 中的演员列表，每个演员以元组 (Name, Type) 表示。
        /// </summary>
        /// <returns>演员元组列表，若无演员则返回空列表。</returns>
        public List<(string Name, string Type)> GetActors()
        {
            return _nfoContent.Elements("actor")
                              .Select(e => (
                                  Name: e.Element("name")?.Value,
                                  Type: e.Element("type")?.Value))
                              .ToList();
        }

        /// <summary>
        /// 使用带类型信息的演员元组列表替换当前 NFO 中的演员节点并保存文件。
        /// </summary>
        /// <param name="actors">包含演员名与类型的元组列表。</param>
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

        /// <summary>
        /// 使用仅包含演员名称的列表替换当前演员节点，类型默认为 "Actor"，并保存文件。
        /// </summary>
        /// <param name="actors">演员名称字符串列表。</param>
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
        /// 将一组元数据批量写入 NFO，包括标题、原始标题、排序标题、剧情、演员、分类、标签、年份与发布日期等字段。
        /// </summary>
        /// <param name="title">视频显示标题。</param>
        /// <param name="originalTitle">站点抓取到的原始标题。</param>
        /// <param name="sortTitle">排序标题，通常为番号。</param>
        /// <param name="plot">剧情或简介文本。</param>
        /// <param name="metatubeId">外部来源 ID（可选），若提供会写入 metatubeid 节点。</param>
        /// <param name="actors">演员名称列表（将被写入 actor 节点）。若传入 null 或空则跳过演员写入。</param>
        /// <param name="genres">分类列表，将替换现有 genre 节点。</param>
        /// <param name="tags">标签列表，将替换现有 tag 节点。</param>
        /// <param name="year">可选年份信息。</param>
        /// <param name="date">可选发布日期，若提供会写入 releasedate 与 premiered 节点。</param>
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
        /// 将 NFO 中的相关字段组合为一个 <see cref="JavVideo"/> 对象，便于上层业务逻辑使用。
        /// </summary>
        /// <returns>包含从 NFO 中读取到的标题、演员、类型与标签等信息的 <see cref="JavVideo"/> 实例。</returns>
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
