using AngleSharp.Dom;
using JavScraper.Tools;
using JavScraper.Tools.Entities;
using JavScraper.Tools.Http;
using JavScraper.Tools.Scrapers;
using JavScraper.Tools.Services;
using JavScraper.Tools.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class VideoInfoFetcher
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly MultiScraperService _multiScraperService;

    public VideoInfoFetcher(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _multiScraperService = new MultiScraperService(loggerFactory);
        }

    public async Task<JavVideo> FetchCensoredVideo(string javId)
    {
        var videoInfo = await TryFetchFromDMM(javId)
                     ?? await TryFetchFromJav123(javId)
                     ?? await TryFetchFromJavBus(javId);
        return videoInfo;
    }

    public async Task<JavVideo> FetchUncensoredVideo(JavId javId)
    {
        var videoInfo = await TryFetchFromJavUncensoredScraper(javId);
        return videoInfo;
    }

    private async Task<JavVideo> TryFetchFromDMM(string javId)
    {
        var dmmScraper = new DMM(_loggerFactory);
        try
        {
            return await dmmScraper.SearchAndParseJavVideo(javId);
        }
        catch (Exception ex)
        {
            LogError("DMM", javId, ex);
            return null;
        }
    }

    private async Task<JavVideo> TryFetchFromJav123(string javId)
    {
        var jav123Scraper = new Jav123Scraper(_loggerFactory);
        try
        {
            return await jav123Scraper.SearchAndParseJavVideo(javId);
        }
        catch (Exception ex)
        {
            LogError("Jav123", javId, ex);
            return null;
        }
    }

    private async Task<JavVideo> TryFetchFromJavBus(string javId)
    {
        var javBusScraper = new JavBus(_loggerFactory);
        try
        {
            return await javBusScraper.SearchAndParseJavVideo(javId);
        }
        catch (Exception ex)
        {
            LogError("JavBus", javId, ex);
            return null;
        }
    }

    private async Task<JavVideo> TryFetchFromJavBusUncensored(string javId)
    {
        var javBusScraper = new JavBusUncensored(_loggerFactory);
        try
        {
            return await javBusScraper.SearchAndParseJavVideo(javId);
        }
        catch (Exception ex)
        {
            LogError("JavBus", javId, ex);
            return null;
        }
    }

    public async Task<JavVideo> TryGetMetadataAsync(JavId javId)
    {
        var makers = await HttpUtils.CheckMakerAsync(javId);
        
        // 使用多刮削器服务获取最佳结果
        return await _multiScraperService.GetBestMetadataAsync(javId, makers);
    }
    private async Task<JavVideo> TryFetchFromJavUncensoredScraper(JavId javId)
    {
        var javDBScraper = new JavUncensoredScraper(_loggerFactory);
        try
        {
            JavVideo javVideo = await javDBScraper.SearchAndParseJavVideo(javId);

            switch (javId.Matcher)
            {
                case "AVE":
                    javVideo = await javDBScraper.GetAVEMetadata(javId);
                    Console.WriteLine($"成功从 AVE 获取番号 {javId.Id} 的数据");
                    break;
                case "Caribbean":
                    var makers = await HttpUtils.CheckMakerAsync(javId);
                    if (makers == null || makers.Count == 0)
                    {
                        return null;
                    }
                    foreach (var maker in makers)
                    {
                        try
                        {
                            var video = maker switch
                            {
                                "Caribbeancom" => await javDBScraper.GetCaribbeanMetadata(javId),
                                "CaribbeancomPR" => await javDBScraper.GetCaribbeanPRMetadata(javId),
                                "1Pondo" => await javDBScraper.Get1PondoMetadata(javId),
                                "Pacopacomama" => await javDBScraper.GetPacopacomamaMetadata(javId),
                                _ => null
                            };

                            // 如果获取成功，立即返回
                            if (video != null)
                            {
                                Console.WriteLine($"成功从 {maker} 获取番号 {javId.Id} 的数据");
                                return video;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"尝试从 {maker} 获取数据失败：{ex.Message}");
                        }
                    }
                    //int count = 0;
                    //while (makers.Count > 0)
                    //{
                    //    var maker = makers[0];

                    //    javVideo = maker switch
                    //    {
                    //        "Caribbeancom" => await javDBScraper.GetCaribbeanMetadata(javId),
                    //        "CaribbeancomPR" => await javDBScraper.GetCaribbeanPRMetadata(javId),
                    //        "1Pondo" => await javDBScraper.Get1PondoMetadata(javId),
                    //        "Pacopacomama" => await javDBScraper.GetPacopacomamaMetadata(javId),
                    //        _ => throw new NotSupportedException($"不支持的 maker：{maker}")
                    //    };
                    //    makers.RemoveAt(0);
                    //    count++;
                    //}
                    break;
                case "Heyzo":
                    javVideo = await javDBScraper.GetHeyzoMetadata(javId);
                    Console.WriteLine($"成功从 Heyzo 获取番号 {javId.Id} 的数据");
                    break;
                case "FC2":
                    //javVideo = await javDBScraper.GetFC2Metadata(javId);
                    break;
                default:
                    break;
            }
            return javVideo;
        }
        catch (Exception ex)
        {
            LogError("JavBus", javId, ex);
            return null;
        }
    }

    private void LogError(string source, string javId, Exception ex)
    {
        var logger = _loggerFactory.CreateLogger<VideoInfoFetcher>();
        Console.WriteLine($"Error fetching video info from {source} for ID {javId}: {ex.Message}");
        logger.LogError($"Error fetching video info from {source} for ID {javId}: {ex.Message}");
    }
}