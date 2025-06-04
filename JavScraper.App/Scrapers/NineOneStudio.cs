using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JavScraper.App.Scrapers
{
    public class NineOneStudio
    {


        public static void ScraperDetailList(string savePath)
        {
            string baseUrl = "https://91mv.org";

            var web = new HtmlWeb();

            string listUrl = "https://91mv.org/index/search?keywords=%E5%87%BA%E8%BD%A8";

            var doc = web.Load(listUrl);

            var listNodes = doc.DocumentNode.SelectNodes("//*[@class='video-list']");

            foreach (var listNode in listNodes)
            {
                var imagesNode = listNode.ChildNodes[1].ChildNodes[1];
                var imageUrl = imagesNode.Attributes["src"].Value.Trim();
                var videoTitle = listNode.ChildNodes[3].InnerText.Trim();
                var videoId = listNode.ChildNodes[3].InnerText.Substring(videoTitle.Length - 8, 8).Trim();
                var title = listNode.ChildNodes[3].InnerText.Substring(0, videoTitle.Length - 8).Trim();
                var newTitle = string.Format("{0} {1}", listNode.ChildNodes[3].InnerText.Substring(videoTitle.Length - 8, 8), listNode.ChildNodes[3].InnerText.Substring(0, videoTitle.Length - 8).Trim());

                var descDir = string.Format("{0}/{1}", savePath, newTitle);

                if (!Directory.Exists(descDir))
                {
                    Directory.CreateDirectory(descDir);
                    var saveFileName = Downloader.Download(imageUrl, descDir, videoId);
                    var posterFileName = string.Format("{0}/{1}{2}", descDir, "poster", ".jpg");
                    ImageUtils.ConvertImage(saveFileName, posterFileName);
                    var fanartFileName = string.Format("{0}/{1}{2}", descDir, "fanart", ".jpg");
                    ImageUtils.ConvertImage(saveFileName, fanartFileName);
                    var landscapeFileName = string.Format("{0}/{1}{2}", descDir, "landscape", ".jpg");
                    ImageUtils.ConvertImage(saveFileName, landscapeFileName);
                }

                var detailUrl = baseUrl + listNode.Attributes["href"].Value.Trim();

                #region 获取详细页信息...

                doc = web.Load(detailUrl);

                var plotText = doc.DocumentNode.SelectSingleNode("//*[@class='play-text']").InnerText.Replace("剧情简介：", "");
                Console.WriteLine(plotText);

                List<string> actorsList = new List<string>();
                string actorName = "";
                string age = "";
                string bust = "";
                string height = "";
                var actorText = doc.DocumentNode.SelectSingleNode("//*[@class='player-name']").InnerText.Trim();
                if (!string.IsNullOrEmpty(actorText) && actorText.Split('/').Length > 0)
                {
                    actorName = actorText.Split('/')[0].Substring(0, actorText.Split('/')[0].Length - 2).Replace("主演：", "");

                    if (actorText.Split('/').Length > 1)
                    {
                        age = actorText.Split('/')[0].Substring(actorText.Split('/')[0].Length - 2, 2);

                        bust = actorText.Split('/')[1];

                        height = actorText.Split('/')[2];
                    }
                    actorsList = actorName.Split(',').ToList();
                }


                var dateText = doc.DocumentNode.SelectSingleNode("//*[@class='date']").InnerText.Trim();
                var date = dateText.Replace("日期：", "").Replace("/", "-");
                Console.WriteLine(date);

                var tagNodes = doc.DocumentNode.SelectNodes("//*[@class='player-tag']");
                List<string> tagList = new List<string>();
                if (tagNodes != null && tagNodes.Count > 0)
                {
                    foreach (var tagNode in tagNodes)
                    {
                        var tag = tagNode.InnerText.Replace("SM调教", "虐恋调教").Replace("多P群交", "多人群交");
                        tagList.Add(tag);
                        Console.WriteLine(tag);
                    }
                }

                var nfoFileName = string.Format(@"{0}\{1}", descDir, newTitle);
                NfoBuilder.GenerateNFOFile(videoId, title, plotText, detailUrl, imageUrl, date, actorsList, age, bust, height, tagList, nfoFileName);

                #endregion

                Console.WriteLine(detailUrl);
            }
        }


        public static void ScraperList(string savePath)
        {
            string baseUrl = "https://91mv.org";

            int listCount = 5;

            var web = new HtmlWeb();

            for (int i = 1; i <= listCount; i++)
            {
                string listUrl = string.Format("https://91mv.org/index/list?page={0}", i);

                var doc = web.Load(listUrl);

                var listNodes = doc.DocumentNode.SelectNodes("//*[@class='video-list']");

                foreach (var listNode in listNodes)
                {
                    var imagesNode = listNode.ChildNodes[1].ChildNodes[1];
                    var imageUrl = imagesNode.Attributes["src"].Value.Trim();
                    var videoTitle = listNode.ChildNodes[3].InnerText.Trim();
                    var videoId = listNode.ChildNodes[3].InnerText.Substring(videoTitle.Length - 8, 8).Trim();
                    var title = listNode.ChildNodes[3].InnerText.Substring(0, videoTitle.Length - 8).Trim();
                    var newTitle = string.Format("{0} {1}", listNode.ChildNodes[3].InnerText.Substring(videoTitle.Length - 8, 8), listNode.ChildNodes[3].InnerText.Substring(0, videoTitle.Length - 8).Trim());

                    var descDir = string.Format("{0}/{1}", savePath, newTitle);

                    if (!Directory.Exists(descDir))
                    {
                        Directory.CreateDirectory(descDir);
                        var saveFileName = Downloader.Download(imageUrl, descDir, videoId);
                        var posterFileName = string.Format("{0}/{1}{2}", descDir, "poster", ".jpg");
                        ImageUtils.ConvertImage(saveFileName, posterFileName);
                        var fanartFileName = string.Format("{0}/{1}{2}", descDir, "fanart", ".jpg");
                        ImageUtils.ConvertImage(saveFileName, fanartFileName);
                        var landscapeFileName = string.Format("{0}/{1}{2}", descDir, "landscape", ".jpg");
                        ImageUtils.ConvertImage(saveFileName, landscapeFileName);
                    }

                    var detailUrl = baseUrl + listNode.Attributes["href"].Value.Trim();

                    #region 获取详细页信息...

                    doc = web.Load(detailUrl);

                    var plotText = doc.DocumentNode.SelectSingleNode("//*[@class='play-text']").InnerText.Replace("剧情简介：", "");
                    Console.WriteLine(plotText);

                    List<string> actorsList = new List<string>();
                    string actorName = "";
                    string age = "";
                    string bust = "";
                    string height = "";
                    var actorText = doc.DocumentNode.SelectSingleNode("//*[@class='player-name']").InnerText.Trim();
                    if (!string.IsNullOrEmpty(actorText) && actorText.Split('/').Length > 0)
                    {
                        actorName = actorText.Split('/')[0].Substring(0, actorText.Split('/')[0].Length - 2).Replace("主演：", "");

                        if (actorText.Split('/').Length > 1)
                        {
                            age = actorText.Split('/')[0].Substring(actorText.Split('/')[0].Length - 2, 2);

                            bust = actorText.Split('/')[1];

                            height = actorText.Split('/')[2];
                        }
                        actorsList = actorName.Split(',').ToList();
                    }


                    var dateText = doc.DocumentNode.SelectSingleNode("//*[@class='date']").InnerText.Trim();
                    var date = dateText.Replace("日期：", "").Replace("/", "-");
                    Console.WriteLine(date);

                    var tagNodes = doc.DocumentNode.SelectNodes("//*[@class='player-tag']");
                    List<string> tagList = new List<string>();
                    if (tagNodes != null && tagNodes.Count > 0)
                    {
                        foreach (var tagNode in tagNodes)
                        {
                            var tag = tagNode.InnerText.Replace("SM调教", "虐恋调教").Replace("多P群交", "多人群交");
                            tagList.Add(tag);
                            Console.WriteLine(tag);
                        }
                    }

                    var nfoFileName = string.Format(@"{0}\{1}", descDir, newTitle);

                    NfoBuilder.GenerateNFOFile(videoId, title, plotText, detailUrl, imageUrl, date, actorsList, age, bust, height, tagList, nfoFileName);

                    #endregion

                    Console.WriteLine(detailUrl);
                }

            }

            Console.WriteLine("完成数据采集!");

            //return product;
        }

    }
}
