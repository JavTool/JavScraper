using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper
{
    public class WebClientUtil
    {
        private readonly HttpClient _httpClient;
        public WebClientUtil(HttpClient httpClient)
        {
            this._httpClient = httpClient;


        }
        public async Task<Stream> GetAsync<T>(string requestUri)
        {
            var response = await _httpClient.GetAsync(requestUri);
            var rep = response;
            var content = await rep.Content.ReadAsStreamAsync(); //读取响应内容

            return content;
        }

        //public T Get<T>(string requestUri)
        //{
        //    var response = _httpClient.GetAsync(requestUri);
        //    var rep = response.Result;
        //    var content = rep.Content.ReadAsStringAsync(); //读取响应内容
        //    var result = content.Result;//在这里会等待task返回。
        //    var data = JsonUtil.ConvertToModel<T>(result);
        //    return data;
        //}

        //public string GetAsyncToString(string requestUri)
        //{
        //    var response = _httpClient.GetAsync(requestUri);
        //    var rep = response.Result;
        //    var content = rep.Content.ReadAsStringAsync(); //读取响应内容
        //    var result = content.Result;//在这里会等待task返回。          
        //    return result;
        //}
        //public T PostAsync<T, V>(string requestUri, V obj)
        //{
        //    var jsonContent = new StringContent(JsonUtil.ConvertToJson(obj));
        //    jsonContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
        //    {
        //        CharSet = "utf-8"
        //    };
        //    var response = _httpClient.PostAsync(requestUri, jsonContent);
        //    var rep = response.Result;
        //    var content = rep.Content.ReadAsStringAsync(); //读取响应内容
        //    var result = content.Result;//在这里会等待task返回。          
        //    var data = JsonUtil.ConvertToModel<T>(result);
        //    return data;
        //}
        //public string PostJsonAsyncToString(string requestUri, string json)
        //{
        //    var jsonContent = new StringContent(json);
        //    jsonContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        //    jsonContent.Headers.ContentType.CharSet = "utf-8";
        //    var response = _httpClient.PostAsync(requestUri, jsonContent);
        //    var rep = response.Result;
        //    var content = rep.Content.ReadAsStringAsync(); //读取响应内容
        //    var result = content.Result;//在这里会等待task返回。          
        //    return result;
        //}

        public void AddHeader(string key, string value)
        {
            _httpClient.DefaultRequestHeaders.Add(key, value);

        }

        public void AddHeaderJson()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static Stream DownloadFileToStream(string url, out string contentType)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            // req.Timeout = 10000;//设置超时(10秒) 
            HttpWebResponse rep = (HttpWebResponse)req.GetResponse();
            contentType = rep.ContentType;
            var stream = rep.GetResponseStream();
            return stream;
        }
    }

    public class WebClientConst
    {
        public static readonly string Apires = "apires";
        public static readonly string Idui1 = "idui1";

    }

    public enum WebClientEnum
    {
        Idui1 = 1, Apires = 2
    }
}
