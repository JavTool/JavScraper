using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.App
{
   

    public class IniReader
    {
        public static (JavToolConfig, NfoOptionConfig) ReadIni(string filePath)
        {
            IniParser iniParser = new IniParser();
            iniParser.Parse(filePath);

            JavToolConfig javToolConfig = new JavToolConfig
            {
                ProxyType = int.Parse(iniParser.GetValue("SysInfo", "ProxyType")),
                HttpProxyUrl = iniParser.GetValue("SysInfo", "HttpProxyUrl"),
                Port = int.Parse(iniParser.GetValue("SysInfo", "Port")),
                Username = iniParser.GetValue("SysInfo", "Username"),
                Password = iniParser.GetValue("SysInfo", "Password"),
                IsFormAlwaysOnTop = int.Parse(iniParser.GetValue("SysInfo", "IsFormAlwaysOnTop")),
                // 继续添加其他属性...
            };

            NfoOptionConfig nfoOptionConfig = new NfoOptionConfig
            {
                NfoOptionActorRuleIsWrite = int.Parse(iniParser.GetValue("Nfo_Option", "NfoOptionActorRuleIsWrite")),
                NfoOptionIsHasSubtitleYesText = iniParser.GetValue("Nfo_Option", "NfoOptionIsHasSubtitleYesText"),
                NfoOptionTitleRenameRule = iniParser.GetValue("Nfo_Option", "NfoOptionTitleRenameRule"),
                NfoOptionMaxTitleLen = int.Parse(iniParser.GetValue("Nfo_Option", "NfoOptionMaxTitleLen")),
            };

            return (javToolConfig, nfoOptionConfig);
        }

        //static void Main()
        //{
        //    string filePath = "path/to/your/file.ini";
        //    var (sysInfoConfig, nfoOptionConfig) = ReadIni(filePath);

        //    // 示例：输出读取的配置信息
        //    Console.WriteLine($"SysInfoConfig - ProxyType: {sysInfoConfig.ProxyType}");
        //    Console.WriteLine($"NfoOptionConfig - NfoOptionActorRuleIsWrite: {nfoOptionConfig.NfoOptionActorRuleIsWrite}");
        //}
    }

}
