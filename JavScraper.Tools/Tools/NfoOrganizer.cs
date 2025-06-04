using System.Collections.Generic;
using System.IO;

public class NfoOrganizer
{
    public  void OrganizeNfoFiles(string directoryPath)
    {
        var nfoFiles = Directory.GetFiles(directoryPath, "*.nfo");
        string movieNfoPath = Path.Combine(directoryPath, "movie.nfo");

        if (File.Exists(movieNfoPath))
        {
            foreach (var nfoFile in nfoFiles)
            {
                if (nfoFile != movieNfoPath)
                {
                    File.Copy(movieNfoPath, nfoFile, true);
                }
            }
        }
    }
} 