using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.App
{


    public class IniParser
    {
        private Dictionary<string, Dictionary<string, string>> sections;

        public IniParser()
        {
            sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        public void Parse(string filePath)
        {
            sections.Clear();

            string currentSection = "";

            foreach (string line in File.ReadLines(filePath))
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else if (!string.IsNullOrEmpty(currentSection) && trimmedLine.Contains("="))
                {
                    string[] parts = trimmedLine.Split(new[] { '=' }, 2);
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    sections[currentSection][key] = value;
                }
            }
        }

        public string GetValue(string section, string key)
        {
            if (sections.ContainsKey(section) && sections[section].ContainsKey(key))
            {
                return sections[section][key];
            }

            return null;
        }
    }

    //class Program
    //{
    //    static void Main()
    //    {
    //        string filePath = "path/to/your/file.ini";
    //        IniParser iniParser = new IniParser();
    //        iniParser.Parse(filePath);

    //        // Example: Reading a value from the SysInfo section
    //        string proxyType = iniParser.GetValue("SysInfo", "ProxyType");
    //        Console.WriteLine($"ProxyType: {proxyType}");
    //    }
    //}

}
