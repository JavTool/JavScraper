using System.Xml;
using JavScraper.Tools.Entities;

public class NfoBuilder
{
    public XmlDocument GenerateNfo(JavVideo javVideo)
    {
        var xmlDoc = new XmlDocument();
        var root = xmlDoc.CreateElement("movie");
        xmlDoc.AppendChild(root);

        // 添加影片信息
        AddElement(xmlDoc, root, "title", javVideo.Title);
        AddElement(xmlDoc, root, "originalTitle", javVideo.OriginalTitle);
        AddElement(xmlDoc, root, "sortTitle", javVideo.Number);
        AddElement(xmlDoc, root, "plot", javVideo.Plot);
        AddElement(xmlDoc, root, "director", javVideo.Director);
        AddElement(xmlDoc, root, "runtime", javVideo.Runtime);
        AddElement(xmlDoc, root, "studio", javVideo.Studio);
        AddElement(xmlDoc, root, "maker", javVideo.Maker);
        AddElement(xmlDoc, root, "date", javVideo.Date);
        
        // 添加演员信息
        if (javVideo.Actors != null && javVideo.Actors.Count > 0)
        {
            var actorsElement = xmlDoc.CreateElement("actors");
            foreach (var actor in javVideo.Actors)
            {
                var actorElement = xmlDoc.CreateElement("actor");
                actorElement.InnerText = actor; // 假设演员名称是字符串
                actorsElement.AppendChild(actorElement);
            }
            root.AppendChild(actorsElement);
        }

        return xmlDoc;
    }

    private void AddElement(XmlDocument xmlDoc, XmlElement parent, string name, string value)
    {
        var element = xmlDoc.CreateElement(name);
        element.InnerText = value;
        parent.AppendChild(element);
    }
} 