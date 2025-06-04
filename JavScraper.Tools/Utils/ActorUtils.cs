using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JavScraper.Tools.Utils
{
    public static class ActorUtils
    {
        public static void CleanupActorNames(List<string> actors)
        {
            if (actors == null || actors.Count == 0) return;

            for (int i = 0; i < actors.Count; i++)
            {
                Match match = Regex.Match(actors[i], @"（([^）]*)）");
                if (match.Success)
                {
                    actors[i] = match.Groups[1].Value;
                }
            }
        }
    }
}