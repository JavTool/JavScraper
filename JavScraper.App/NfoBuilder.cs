using JavScraper.App.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace JavScraper.App
{
    public class NfoBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title"></param>
        /// <param name="plot"></param>
        /// <param name="url"></param>
        /// <param name="imageUrl"></param>
        /// <param name="date"></param>
        /// <param name="actorList"></param>
        /// <param name="age"></param>
        /// <param name="bust"></param>
        /// <param name="height"></param>
        /// <param name="tagList"></param>
        /// <param name="descPath"></param>
        public static void GenerateNFOFile(string id, string title, string plot, string url, string imageUrl, string date, List<string> actorList, string age, string bust, string height, List<string> tagList, string descPath)
        {
            XmlDocument xmlDocument = new XmlDocument();
            // 定义 xml 声明
            XmlDeclaration xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", "yes");

            xmlDocument.AppendChild(xmlDeclaration);

            var movieElement = xmlDocument.CreateElement("movie");

            var plotElement = xmlDocument.CreateElement("plot");

            plot = string.Format("<p itemprop=\"plot\">{0}</p><p itemprop=\"age\">年龄: <span>{1}</span></p><p itemprop=\"bust\">罩杯: <span>{2}</span></p><p itemprop=\"height\">身高: <span>{3}</span></p>", plot, age, bust, height);

            XmlCDataSection xmlCDataSection = xmlDocument.CreateCDataSection(plot);

            plotElement.AppendChild(xmlCDataSection);
            movieElement.AppendChild(plotElement);

            var outlineElement = xmlDocument.CreateElement("outline");
            movieElement.AppendChild(outlineElement);

            var lockdataElement = xmlDocument.CreateElement("lockdata");
            lockdataElement.InnerText = string.Format("{0}", "false");
            movieElement.AppendChild(lockdataElement);

            var dateaddedElement = xmlDocument.CreateElement("dateadded");
            dateaddedElement.InnerText = string.Format("{0}", date);
            movieElement.AppendChild(dateaddedElement);

            //<title>淫新年</title>
            var titleElement = xmlDocument.CreateElement("title");
            titleElement.InnerText = string.Format("{0}", title);
            movieElement.AppendChild(titleElement);

            //<originaltitle>淫新年</originaltitle>
            var originaltitleElement = xmlDocument.CreateElement("originaltitle");
            originaltitleElement.InnerText = string.Format("{0}", title);
            movieElement.AppendChild(originaltitleElement);

            //<actor>
            //  <name>董小宛</name>
            //  <type>Actor</type>
            //</actor>
            //<actor>
            //  <name>聂小倩</name>
            //  <type>Actor</type>
            //</actor>
            //string actors = "董小宛,聂小倩";
            //var actorList = actors.Split(',').ToList();
            foreach (var actor in actorList)
            {
                var actorElement = xmlDocument.CreateElement("actor");

                var nameElement = xmlDocument.CreateElement("name");
                nameElement.InnerText = string.Format("{0}", actor);
                actorElement.AppendChild(nameElement);

                var typeElement = xmlDocument.CreateElement("type");
                typeElement.InnerText = string.Format("{0}", "Actor");
                actorElement.AppendChild(typeElement);

                movieElement.AppendChild(actorElement);
            }


            var actorageElement = xmlDocument.CreateElement("actorage");
            actorageElement.InnerText = string.Format("{0}", age);
            actorageElement.SetAttribute("type", "JavScraper");
            movieElement.AppendChild(actorageElement);

            var actorbustElement = xmlDocument.CreateElement("actorbust");
            actorbustElement.InnerText = string.Format("{0}", bust);
            actorbustElement.SetAttribute("type", "JavScraper");
            movieElement.AppendChild(actorbustElement);

            //<actorheight>2021</actorheight>
            var actorheightElement = xmlDocument.CreateElement("actorheight");
            actorheightElement.InnerText = string.Format("{0}", height);
            actorheightElement.SetAttribute("type", "JavScraper");
            movieElement.AppendChild(actorheightElement);


            //<year>2021</year>
            var yearElement = xmlDocument.CreateElement("year");
            yearElement.InnerText = string.Format("{0}", date.Substring(0, 4));
            movieElement.AppendChild(yearElement);

            //<sorttitle>91CM-100</sorttitle>
            var sorttitleElement = xmlDocument.CreateElement("sorttitle");
            sorttitleElement.InnerText = string.Format("{0}", id);
            movieElement.AppendChild(sorttitleElement);

            //<mpaa>XXX</mpaa>
            var mpaaElement = xmlDocument.CreateElement("mpaa");
            mpaaElement.InnerText = string.Format("{0}", "XXX");
            movieElement.AppendChild(mpaaElement);

            //<premiered>2021-02-11</premiered>
            var premieredElement = xmlDocument.CreateElement("dateadded");
            premieredElement.InnerText = string.Format("{0}", date);
            movieElement.AppendChild(premieredElement);

            //<releasedate>2021-02-11</releasedate>
            var releasedateElement = xmlDocument.CreateElement("releasedate");
            releasedateElement.InnerText = string.Format("{0}", date);
            movieElement.AppendChild(releasedateElement);

            //<runtime>34</runtime>
            var runtimeElement = xmlDocument.CreateElement("runtime");
            runtimeElement.InnerText = string.Format("{0}", "30");
            movieElement.AppendChild(runtimeElement);

            //<genre>多人群交</genre>
            //<genre>家庭乱伦</genre>
            //<genre>人妻少妇</genre>
            //<genre>顔值在线</genre>
            //string genres = "多人群交,家庭乱伦,人妻少妇,顔值在线";
            //var genresList = genres.Split(',').ToList();
            var genresList = tagList;
            foreach (var genre in genresList)
            {
                var genreElement = xmlDocument.CreateElement("genre");
                genreElement.InnerText = string.Format("{0}", genre);
                movieElement.AppendChild(genreElement);
            }
            //<studio>91 制片厂</studio>
            var studioElement = xmlDocument.CreateElement("studio");
            studioElement.InnerText = string.Format("{0}", "91 制片厂");
            movieElement.AppendChild(studioElement);


            //<uniqueid type="JavScraper">OKB-023</uniqueid>
            var uniqueidElement = xmlDocument.CreateElement("uniqueid");
            uniqueidElement.SetAttribute("type", "JavScraper");
            uniqueidElement.InnerText = string.Format("{0}", id);
            movieElement.AppendChild(uniqueidElement);

            //<javscraperid>91CM-100</javscraperid>
            var javscraperidElement = xmlDocument.CreateElement("uniqueid");
            javscraperidElement.InnerText = string.Format("{0}", id);
            movieElement.AppendChild(javscraperidElement);

            //<uniqueid type="JavScraper-Json">{"OriginalTitle":"算命先生","Cover":"https://www.91mv.org//uploads/20210520/2021052011505861231.png","Date":"2017-09-07"}</uniqueid>
            var jsonElement = xmlDocument.CreateElement("uniqueid");
            jsonElement.SetAttribute("type", "JavScraper-Json");
            jsonElement.InnerText = string.Format("{0}", "{\"OriginalTitle\":\"" + title + "\",\"Cover\":\"" + imageUrl + "\",\"Date\":\"" + date + "\"}");
            movieElement.AppendChild(jsonElement);

            //<javscraper-jsonid>{"OriginalTitle":"算命先生","Cover":"https://www.91mv.org//uploads/20210520/2021052011505861231.png","Date":"2017-09-07"}</javscraper-jsonid>
            var jsonidElement = xmlDocument.CreateElement("javscraper-jsonid");
            jsonidElement.InnerText = string.Format("{0}", "{\"OriginalTitle\":\"" + title + "\",\"Cover\":\"" + imageUrl + "\",\"Date\":\"" + date + "\"}");
            movieElement.AppendChild(jsonidElement);

            //<uniqueid type="JavScraper-Url">https://91mv.org/index/detail?id=80JAvM</uniqueid>
            var urlElement = xmlDocument.CreateElement("uniqueid");
            urlElement.SetAttribute("type", "JavScraper-Url");
            urlElement.InnerText = string.Format("{0}", url);
            movieElement.AppendChild(urlElement);

            //<javscraper-urlid>https://91mv.org/index/detail?id=80JAvM</javscraper-urlid>
            var urlidElement = xmlDocument.CreateElement("javscraper-urlid");
            urlidElement.InnerText = string.Format("{0}", url);
            movieElement.AppendChild(urlidElement);

            #region fileinfo

            var fileinfoElement = xmlDocument.CreateElement("fileinfo");

            var streamdetailsElement = xmlDocument.CreateElement("streamdetails");

            var videoElement = xmlDocument.CreateElement("video");
            var videoInfo = "video";
            if (!string.IsNullOrEmpty(videoInfo))
            {
                //<codec>h264</codec>
                var codecElement = xmlDocument.CreateElement("codec");
                codecElement.InnerText = string.Format("{0}", "h264");
                videoElement.AppendChild(codecElement);

                //<micodec>h264</micodec>
                var micodecElement = xmlDocument.CreateElement("micodec");
                micodecElement.InnerText = string.Format("{0}", "h264");
                videoElement.AppendChild(micodecElement);

                //<bitrate>2909267</bitrate>
                var bitrateElement = xmlDocument.CreateElement("bitrate");
                bitrateElement.InnerText = string.Format("{0}", "2909267");
                videoElement.AppendChild(bitrateElement);

                //<width>1280</width>
                var widthElement = xmlDocument.CreateElement("width");
                widthElement.InnerText = string.Format("{0}", "1280");
                videoElement.AppendChild(widthElement);

                //<height>720</height>
                var heightElement = xmlDocument.CreateElement("height");
                heightElement.InnerText = string.Format("{0}", "720");
                videoElement.AppendChild(heightElement);

                //<aspect>16:9</aspect>
                var aspectElement = xmlDocument.CreateElement("aspect");
                aspectElement.InnerText = string.Format("{0}", "16:9");
                videoElement.AppendChild(aspectElement);

                //<aspectratio>16:9</aspectratio>
                var aspectratioElement = xmlDocument.CreateElement("aspectratio");
                aspectratioElement.InnerText = string.Format("{0}", "16:9");
                videoElement.AppendChild(aspectratioElement);

                //<framerate>29.97003</framerate>
                var framerateElement = xmlDocument.CreateElement("framerate");
                framerateElement.InnerText = string.Format("{0}", "29.97003");
                videoElement.AppendChild(framerateElement);

                //<scantype>progressive</scantype>
                var scantypeElement = xmlDocument.CreateElement("scantype");
                scantypeElement.InnerText = string.Format("{0}", "progressive");
                videoElement.AppendChild(scantypeElement);

                //<default>False</default>
                var defaultElement = xmlDocument.CreateElement("default");
                defaultElement.InnerText = string.Format("{0}", "False");
                videoElement.AppendChild(defaultElement);

                //<forced>False</forced>
                var forcedElement = xmlDocument.CreateElement("forced");
                forcedElement.InnerText = string.Format("{0}", "False");
                videoElement.AppendChild(forcedElement);

                //<duration>33</duration>
                var durationElement = xmlDocument.CreateElement("duration");
                durationElement.InnerText = string.Format("{0}", "33");
                videoElement.AppendChild(durationElement);

                //<durationinseconds>2031</durationinseconds>
                var durationinsecondsElement = xmlDocument.CreateElement("durationinseconds");
                durationinsecondsElement.InnerText = string.Format("{0}", "2031");
                videoElement.AppendChild(durationinsecondsElement);

                streamdetailsElement.AppendChild(videoElement);
            }

            var audioElement = xmlDocument.CreateElement("audio");
            var audioInfo = "audio";
            if (!string.IsNullOrEmpty(audioInfo))
            {
                //<codec>aac</codec>
                var codecElement = xmlDocument.CreateElement("codec");
                codecElement.InnerText = string.Format("{0}", "aac");
                audioElement.AppendChild(codecElement);

                //<micodec>aac</micodec>
                var micodecElement = xmlDocument.CreateElement("micodec");
                micodecElement.InnerText = string.Format("{0}", "aac");
                audioElement.AppendChild(micodecElement);

                //<bitrate>319940</bitrate>
                var bitrateElement = xmlDocument.CreateElement("bitrate");
                bitrateElement.InnerText = string.Format("{0}", "319940");
                audioElement.AppendChild(bitrateElement);

                //<language>eng</language>
                var languageElement = xmlDocument.CreateElement("language");
                languageElement.InnerText = string.Format("{0}", "eng");
                audioElement.AppendChild(languageElement);

                //<scantype>progressive</scantype>
                var scantypeElement = xmlDocument.CreateElement("scantype");
                scantypeElement.InnerText = string.Format("{0}", "progressive");
                audioElement.AppendChild(scantypeElement);

                //<channels>2</channels>
                var channelsElement = xmlDocument.CreateElement("channels");
                channelsElement.InnerText = string.Format("{0}", "2");
                audioElement.AppendChild(channelsElement);

                //<samplingrate>48000</samplingrate>
                var samplingrateElement = xmlDocument.CreateElement("samplingrate");
                samplingrateElement.InnerText = string.Format("{0}", "48000");
                audioElement.AppendChild(samplingrateElement);

                //<default>False</default>
                var defaultElement = xmlDocument.CreateElement("default");
                defaultElement.InnerText = string.Format("{0}", "False");
                audioElement.AppendChild(defaultElement);

                //<forced>False</forced>
                var forcedElement = xmlDocument.CreateElement("forced");
                forcedElement.InnerText = string.Format("{0}", "False");
                audioElement.AppendChild(forcedElement);

                streamdetailsElement.AppendChild(audioElement);
            }
            fileinfoElement.AppendChild(streamdetailsElement);
            movieElement.AppendChild(fileinfoElement);

            #endregion


            xmlDocument.AppendChild(movieElement);

            xmlDocument.Save(string.Format("{0}.nfo", descPath));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title"></param>
        /// <param name="plot"></param>
        /// <param name="url"></param>
        /// <param name="imageUrl"></param>
        /// <param name="date"></param>
        /// <param name="actorList"></param>
        /// <param name="age"></param>
        /// <param name="bust"></param>
        /// <param name="height"></param>
        /// <param name="tagList"></param>
        /// <param name="descPath"></param>
        public static XmlDocument GenerateNfo(JavVideo javVideo, bool lockdata = false)
        {
            XmlDocument xmlDocument = new XmlDocument();
            // 定义 xml 声明
            XmlDeclaration xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", "yes");

            xmlDocument.AppendChild(xmlDeclaration);

            var movieElement = xmlDocument.CreateElement("movie");

            var plotElement = xmlDocument.CreateElement("plot");

            var plot = string.Format("<p itemprop=\"plot\">{0}</p>", javVideo.Plot);

            //var lockdataElement = xmlDocument.CreateElement("lockdata"); "<lockdata>true</lockdata>";

            XmlCDataSection xmlCDataSection = xmlDocument.CreateCDataSection(plot);

            plotElement.AppendChild(xmlCDataSection);

            movieElement.AppendChild(plotElement);

            var outlineElement = xmlDocument.CreateElement("outline");
            movieElement.AppendChild(outlineElement);

            var lockdataElement = xmlDocument.CreateElement("lockdata");
            lockdataElement.InnerText = string.Format("{0}", lockdata ? "true" : "false");
            movieElement.AppendChild(lockdataElement);

            var dateaddedElement = xmlDocument.CreateElement("dateadded");
            dateaddedElement.InnerText = string.Format("{0}", javVideo.Date);
            movieElement.AppendChild(dateaddedElement);

            //<title>淫新年</title>
            var titleElement = xmlDocument.CreateElement("title");
            titleElement.InnerText = string.Format("{0}", javVideo.Title);
            movieElement.AppendChild(titleElement);

            //<originaltitle>淫新年</originaltitle>
            var originaltitleElement = xmlDocument.CreateElement("originaltitle");
            originaltitleElement.InnerText = string.Format("{0}", javVideo.OriginalTitle);
            movieElement.AppendChild(originaltitleElement);

            //<actor>
            //  <name>董小宛</name>
            //  <type>Actor</type>
            //</actor>
            //<actor>
            //  <name>聂小倩</name>
            //  <type>Actor</type>
            //</actor>
            //string actors = "董小宛,聂小倩";
            //var actorList = actors.Split(',').ToList();
            foreach (var actor in javVideo.Actors)
            {
                var actorElement = xmlDocument.CreateElement("actor");

                var nameElement = xmlDocument.CreateElement("name");
                nameElement.InnerText = string.Format("{0}", actor);
                actorElement.AppendChild(nameElement);

                var typeElement = xmlDocument.CreateElement("type");
                typeElement.InnerText = string.Format("{0}", "Actor");
                actorElement.AppendChild(typeElement);

                movieElement.AppendChild(actorElement);
            }


            //var actorageElement = xmlDocument.CreateElement("actorage");
            //actorageElement.InnerText = string.Format("{0}", age);
            //actorageElement.SetAttribute("type", "JavScraper");
            //movieElement.AppendChild(actorageElement);

            //var actorbustElement = xmlDocument.CreateElement("actorbust");
            //actorbustElement.InnerText = string.Format("{0}", bust);
            //actorbustElement.SetAttribute("type", "JavScraper");
            //movieElement.AppendChild(actorbustElement);

            ////<actorheight>2021</actorheight>
            //var actorheightElement = xmlDocument.CreateElement("actorheight");
            //actorheightElement.InnerText = string.Format("{0}", height);
            //actorheightElement.SetAttribute("type", "JavScraper");
            //movieElement.AppendChild(actorheightElement);


            //<year>2021</year>
            var yearElement = xmlDocument.CreateElement("year");
            yearElement.InnerText = string.Format("{0}", javVideo?.Date?[..4]);
            movieElement.AppendChild(yearElement);

            //<sorttitle>91CM-100</sorttitle>
            var sorttitleElement = xmlDocument.CreateElement("sorttitle");
            sorttitleElement.InnerText = string.Format("{0}", javVideo.Number);
            movieElement.AppendChild(sorttitleElement);

            //<mpaa>XXX</mpaa>
            var mpaaElement = xmlDocument.CreateElement("mpaa");
            mpaaElement.InnerText = string.Format("{0}", "XXX");
            movieElement.AppendChild(mpaaElement);

            //<premiered>2021-02-11</premiered>
            var premieredElement = xmlDocument.CreateElement("dateadded");
            premieredElement.InnerText = string.Format("{0}", javVideo?.Date);
            movieElement.AppendChild(premieredElement);

            //<releasedate>2021-02-11</releasedate>
            var releasedateElement = xmlDocument.CreateElement("releasedate");
            releasedateElement.InnerText = string.Format("{0}", javVideo?.Date);
            movieElement.AppendChild(releasedateElement);

            //<runtime>34</runtime>
            var runtimeElement = xmlDocument.CreateElement("runtime");
            runtimeElement.InnerText = string.Format("{0}", javVideo?.Runtime);
            movieElement.AppendChild(runtimeElement);

            //<genre>多人群交</genre>
            //<genre>家庭乱伦</genre>
            //<genre>人妻少妇</genre>
            //<genre>顔值在线</genre>
            //string genres = "多人群交,家庭乱伦,人妻少妇,顔值在线";
            //var genresList = genres.Split(',').ToList();
            var genresList = javVideo.Genres;
            foreach (var genre in genresList)
            {
                var genreElement = xmlDocument.CreateElement("genre");
                genreElement.InnerText = string.Format("{0}", genre);
                movieElement.AppendChild(genreElement);
            }
            //<studio>91 制片厂</studio>
            var studioElement = xmlDocument.CreateElement("studio");
            studioElement.InnerText = string.Format("{0}", javVideo.Studio);
            movieElement.AppendChild(studioElement);


            //<uniqueid type="JavScraper">OKB-023</uniqueid>
            var uniqueidElement = xmlDocument.CreateElement("uniqueid");
            uniqueidElement.SetAttribute("type", "JavScraper");
            uniqueidElement.InnerText = string.Format("{0}", javVideo.Number);
            movieElement.AppendChild(uniqueidElement);

            //<javscraperid>91CM-100</javscraperid>
            var javscraperidElement = xmlDocument.CreateElement("uniqueid");
            javscraperidElement.InnerText = string.Format("{0}", javVideo.Number);
            movieElement.AppendChild(javscraperidElement);

            //<uniqueid type="JavScraper-Json">{"OriginalTitle":"算命先生","Cover":"https://www.91mv.org//uploads/20210520/2021052011505861231.png","Date":"2017-09-07"}</uniqueid>
            var jsonElement = xmlDocument.CreateElement("uniqueid");
            jsonElement.SetAttribute("type", "JavScraper-Json");
            jsonElement.InnerText = string.Format("{0}", "{\"OriginalTitle\":\"" + javVideo.OriginalTitle + "\",\"Cover\":\"" + javVideo.Cover + "\",\"Date\":\"" + javVideo.Date + "\"}");
            movieElement.AppendChild(jsonElement);

            //<javscraper-jsonid>{"OriginalTitle":"算命先生","Cover":"https://www.91mv.org//uploads/20210520/2021052011505861231.png","Date":"2017-09-07"}</javscraper-jsonid>
            var jsonidElement = xmlDocument.CreateElement("javscraper-jsonid");
            jsonidElement.InnerText = string.Format("{0}", "{\"OriginalTitle\":\"" + javVideo.OriginalTitle + "\",\"Cover\":\"" + javVideo.Cover + "\",\"Date\":\"" + javVideo.Date + "\"}");
            movieElement.AppendChild(jsonidElement);

            //<uniqueid type="JavScraper-Url">https://91mv.org/index/detail?id=80JAvM</uniqueid>
            var urlElement = xmlDocument.CreateElement("uniqueid");
            urlElement.SetAttribute("type", "JavScraper-Url");
            urlElement.InnerText = string.Format("{0}", javVideo.Url);
            movieElement.AppendChild(urlElement);

            //<javscraper-urlid>https://91mv.org/index/detail?id=80JAvM</javscraper-urlid>
            var urlidElement = xmlDocument.CreateElement("javscraper-urlid");
            urlidElement.InnerText = string.Format("{0}", javVideo.Url);
            movieElement.AppendChild(urlidElement);

            #region fileinfo

            var fileinfoElement = xmlDocument.CreateElement("fileinfo");

            var streamdetailsElement = xmlDocument.CreateElement("streamdetails");

            var videoElement = xmlDocument.CreateElement("video");
            var videoInfo = "video";
            if (!string.IsNullOrEmpty(videoInfo))
            {
                //<codec>h264</codec>
                var codecElement = xmlDocument.CreateElement("codec");
                codecElement.InnerText = string.Format("{0}", "h264");
                videoElement.AppendChild(codecElement);

                //<micodec>h264</micodec>
                var micodecElement = xmlDocument.CreateElement("micodec");
                micodecElement.InnerText = string.Format("{0}", "h264");
                videoElement.AppendChild(micodecElement);

                //<bitrate>2909267</bitrate>
                var bitrateElement = xmlDocument.CreateElement("bitrate");
                bitrateElement.InnerText = string.Format("{0}", "2909267");
                videoElement.AppendChild(bitrateElement);

                //<width>1280</width>
                var widthElement = xmlDocument.CreateElement("width");
                widthElement.InnerText = string.Format("{0}", "1280");
                videoElement.AppendChild(widthElement);

                //<height>720</height>
                var heightElement = xmlDocument.CreateElement("height");
                heightElement.InnerText = string.Format("{0}", "720");
                videoElement.AppendChild(heightElement);

                //<aspect>16:9</aspect>
                var aspectElement = xmlDocument.CreateElement("aspect");
                aspectElement.InnerText = string.Format("{0}", "16:9");
                videoElement.AppendChild(aspectElement);

                //<aspectratio>16:9</aspectratio>
                var aspectratioElement = xmlDocument.CreateElement("aspectratio");
                aspectratioElement.InnerText = string.Format("{0}", "16:9");
                videoElement.AppendChild(aspectratioElement);

                //<framerate>29.97003</framerate>
                var framerateElement = xmlDocument.CreateElement("framerate");
                framerateElement.InnerText = string.Format("{0}", "29.97003");
                videoElement.AppendChild(framerateElement);

                //<scantype>progressive</scantype>
                var scantypeElement = xmlDocument.CreateElement("scantype");
                scantypeElement.InnerText = string.Format("{0}", "progressive");
                videoElement.AppendChild(scantypeElement);

                //<default>False</default>
                var defaultElement = xmlDocument.CreateElement("default");
                defaultElement.InnerText = string.Format("{0}", "False");
                videoElement.AppendChild(defaultElement);

                //<forced>False</forced>
                var forcedElement = xmlDocument.CreateElement("forced");
                forcedElement.InnerText = string.Format("{0}", "False");
                videoElement.AppendChild(forcedElement);

                //<duration>33</duration>
                var durationElement = xmlDocument.CreateElement("duration");
                durationElement.InnerText = string.Format("{0}", "33");
                videoElement.AppendChild(durationElement);

                //<durationinseconds>2031</durationinseconds>
                var durationinsecondsElement = xmlDocument.CreateElement("durationinseconds");
                durationinsecondsElement.InnerText = string.Format("{0}", "2031");
                videoElement.AppendChild(durationinsecondsElement);

                streamdetailsElement.AppendChild(videoElement);
            }

            var audioElement = xmlDocument.CreateElement("audio");
            var audioInfo = "audio";
            if (!string.IsNullOrEmpty(audioInfo))
            {
                //<codec>aac</codec>
                var codecElement = xmlDocument.CreateElement("codec");
                codecElement.InnerText = string.Format("{0}", "aac");
                audioElement.AppendChild(codecElement);

                //<micodec>aac</micodec>
                var micodecElement = xmlDocument.CreateElement("micodec");
                micodecElement.InnerText = string.Format("{0}", "aac");
                audioElement.AppendChild(micodecElement);

                //<bitrate>319940</bitrate>
                var bitrateElement = xmlDocument.CreateElement("bitrate");
                bitrateElement.InnerText = string.Format("{0}", "319940");
                audioElement.AppendChild(bitrateElement);

                //<language>eng</language>
                var languageElement = xmlDocument.CreateElement("language");
                languageElement.InnerText = string.Format("{0}", "eng");
                audioElement.AppendChild(languageElement);

                //<scantype>progressive</scantype>
                var scantypeElement = xmlDocument.CreateElement("scantype");
                scantypeElement.InnerText = string.Format("{0}", "progressive");
                audioElement.AppendChild(scantypeElement);

                //<channels>2</channels>
                var channelsElement = xmlDocument.CreateElement("channels");
                channelsElement.InnerText = string.Format("{0}", "2");
                audioElement.AppendChild(channelsElement);

                //<samplingrate>48000</samplingrate>
                var samplingrateElement = xmlDocument.CreateElement("samplingrate");
                samplingrateElement.InnerText = string.Format("{0}", "48000");
                audioElement.AppendChild(samplingrateElement);

                //<default>False</default>
                var defaultElement = xmlDocument.CreateElement("default");
                defaultElement.InnerText = string.Format("{0}", "False");
                audioElement.AppendChild(defaultElement);

                //<forced>False</forced>
                var forcedElement = xmlDocument.CreateElement("forced");
                forcedElement.InnerText = string.Format("{0}", "False");
                audioElement.AppendChild(forcedElement);

                streamdetailsElement.AppendChild(audioElement);
            }
            fileinfoElement.AppendChild(streamdetailsElement);
            movieElement.AppendChild(fileinfoElement);

            #endregion


            xmlDocument.AppendChild(movieElement);

            //xmlDocument.Save(string.Format("{0}.nfo", descPath));
            return xmlDocument;
        }
    }
}
