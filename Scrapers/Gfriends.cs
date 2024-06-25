﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JavScraper.Http;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 头像
    /// </summary>
    public class Gfriends
    {
        protected JavHttpClient client;
        protected ILogger log;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// 适配器名称
        /// </summary>
        public string Name => "gfriends";

        private FileTreeModel tree;
        private DateTime last = DateTime.Now.AddDays(-1);
        private readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        private const string base_url = "https://raw.githubusercontent.com/xinxin8816/gfriends/master/";

        public Gfriends(ILoggerFactory logManager, IJsonSerializer jsonSerializer)
        {
            client = new JavHttpClient(client => client.BaseAddress = new Uri(base_url));
            this.log = logManager.CreateLogger<Gfriends>();
            this._jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// 查找女优的头像地址
        /// </summary>
        /// <param name="name">女优姓名</param>
        /// <param name="cancelationToken"></param>
        /// <returns></returns>
        public async Task<string> FindAsync(string name, CancellationToken cancelationToken)
        {
            await locker.WaitAsync(cancelationToken);
            try
            {
                if (tree == null || (DateTime.Now - last).TotalHours > 1)
                {
                    var json = await client.GetStringAsync("Filetree.json");
                    tree = _jsonSerializer.DeserializeFromString<FileTreeModel>(json);
                    last = DateTime.Now;
                    tree.Content = tree.Content.OrderBy(o => o.Key).ToDictionary(o => o.Key, o => o.Value);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
            finally
            {
                locker.Release();
            }

            if (tree?.Content?.Any() != true)
                return null;

            return tree.Find(name);
        }

        /// <summary>
        /// 树模型
        /// </summary>
        public class FileTreeModel
        {
            /// <summary>
            /// 内容
            /// </summary>
            public Dictionary<string, Dictionary<string, string>> Content { get; set; }

            /// <summary>
            /// 查找图片
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public string Find(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var key = $"{name.Trim()}.";

                foreach (var dd in Content)
                {
                    foreach (var d in dd.Value)
                    {
                        if (d.Key.StartsWith(key))
                            return $"{base_url}Content/{dd.Key}/{d.Value}";
                    }
                }

                return null;
            }
        }
    }
}
