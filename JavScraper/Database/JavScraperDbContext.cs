using JavScraper.Domain;
using LiteDB;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JavScraper.Database
{
    /// <summary>
    /// 数据库访问实体。
    /// </summary>
    public class JavScraperDbContext : LiteDatabase
    {
        /// <summary>
        /// 影片情节信息。
        /// </summary>
        public ILiteCollection<Plot> Plots { get; }

        /// <summary>
        /// 元数据。
        /// </summary>
        public ILiteCollection<Metadata> Metadata { get; }

        /// <summary>
        /// 翻译。
        /// </summary>
        public ILiteCollection<Translation> Translations { get; }

        /// <summary>
        /// 图片人脸中心点位置。
        /// </summary>
        public ILiteCollection<ImageFaceCenterPoint> ImageFaceCenterPoints { get; }

        /// <summary>
        /// 构造。
        /// </summary>
        /// <param name="connectionString">连接字符串。</param>
        public JavScraperDbContext(string connectionString) : base(connectionString)
        {
            Plots = GetCollection<Plot>("Plots");
            Metadata = GetCollection<Metadata>("Metadata");
            Translations = GetCollection<Translation>("Translations");
            ImageFaceCenterPoints = GetCollection<ImageFaceCenterPoint>("ImageFaceCenterPoints");

            Plots.EnsureIndex(o => o.Num);
            Plots.EnsureIndex(o => o.Provider);

            Metadata.EnsureIndex(o => o.Num);
            Metadata.EnsureIndex(o => o.Provider);
            Metadata.EnsureIndex(o => o.Url);

            Translations.EnsureIndex(o => o.hash);
            Translations.EnsureIndex(o => o.lang);
        }

        /// <summary>
        /// 创建数据库实体。
        /// </summary>
        /// <param name="hostEnvironment">提供有关其中正在运行应用程序的宿主环境的信息。</param>
        /// <returns></returns>
        public static JavScraperDbContext Create(IHostEnvironment hostEnvironment)
        {
            var path = Path.Combine(hostEnvironment.ContentRootPath, "JavScraper.db");

            try
            {
                return new JavScraperDbContext(path);
            }
            catch { }

            return default;
        }

        /// <summary>
        /// 保存视频元数据。
        /// </summary>
        /// <returns></returns>
        public bool SaveJavVideo(JavVideo video)
        {
            try
            {
                var metadata = Metadata.FindOne(o => o.Url == video.Url && o.Provider == video.Provider);
                var datetime = DateTime.Now;
                if (metadata == null)
                {
                    metadata = new Metadata()
                    {
                        Created = datetime,
                        Data = video,
                        Modified = datetime,
                        Num = video.Num,
                        Provider = video.Provider,
                        Url = video.Url,
                        Selected = datetime
                    };
                    Metadata.Insert(metadata);
                }
                else
                {
                    metadata.Modified = datetime;
                    metadata.Selected = datetime;
                    metadata.Num = video.Num;
                    metadata.Data = video;
                    Metadata.Update(metadata);
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// 查找视频元数据。
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public JavVideo FindJavVideo(string provider, string url)
        {
            if (string.IsNullOrWhiteSpace(provider))
                return Metadata.FindOne(o => o.Url == url)?.Data;
            else
                return Metadata.FindOne(o => o.Url == url && o.Provider == provider)?.Data;
        }

        /// <summary>
        /// 查找视频元数据。
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public Metadata FindMetadata(string provider, string url)
        {
            if (string.IsNullOrWhiteSpace(provider))
                return Metadata.FindOne(o => o.Url == url);
            else
                return Metadata.FindOne(o => o.Url == url && o.Provider == provider);
        }
    }
}
