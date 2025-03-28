using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace Quests.Utils
{
    internal class SteamProfile
    {
        public static Dictionary<string, string> CachedPictures = new Dictionary<string, string>();
        public static async Task<string> GetProfilePictureUrlAsync(string steamId)
        {
            if (CachedPictures.TryGetValue(steamId, out string cachedUrl))
            {
                return cachedUrl;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"http://steamcommunity.com/profiles/{steamId}?xml=1";
                    using (HttpResponseMessage response = await client.GetAsync(url))
                    using (HttpContent content = response.Content)
                    {
                        string xmlString = await content.ReadAsStringAsync();
                        XmlDocument xmlDocument = new XmlDocument();
                        await Task.Run(() => xmlDocument.LoadXml(xmlString));

                        XmlElement avatarElement = xmlDocument["profile"]["avatarFull"];
                        string avatarUrl = avatarElement.InnerText;
                        CachedPictures[steamId] = avatarUrl;
                        return avatarUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while getting profile picture async: {ex.Message}");
                return "";
            }
        }
        /*
        public static string GetProfilePictureUrl(string steamId)
        {
            if (CachedPictures.TryGetValue(steamId, out string cachedUrl))
            {
                return cachedUrl;
            }
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(new WebClient().DownloadString($"http://steamcommunity.com/profiles/{steamId}?xml=1")); ;
                XmlElement xmlElement8 = xmlDocument["profile"]["avatarFull"];
                CachedPictures.Add(steamId, xmlElement8.InnerText);
                return xmlElement8.InnerText;
            } catch (Exception ex)
            {
                Console.WriteLine($"Error while getting profile picture sync: {ex.Message}");
                return "";
            }
        }*/
    }
}
