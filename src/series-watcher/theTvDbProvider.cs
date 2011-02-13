using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Net;
using System.Xml.Serialization;

using SeriesWatcher;

namespace theTvDb
{
    public class theTvDbProvider
    {
        private static readonly string API_KEY = "E171DB33C88FA779";
        private static readonly string LOCAL_ROOT = System.Configuration.ConfigurationManager.AppSettings["LOCAL_ROOT"];

        public theTvDbProvider()
        {
            if (!Directory.Exists(LOCAL_ROOT))
            {
                Directory.CreateDirectory(LOCAL_ROOT);
            }

            string seriesFolder = Path.Combine(LOCAL_ROOT, "series");

            if (!Directory.Exists(seriesFolder))
            {
                Directory.CreateDirectory(seriesFolder);
            }
        }

        /// <summary>
        /// Updates the local cache for all series that have updated since the last check.
        /// </summary>
        public void Update()
        {
            string updateFilePath = Path.Combine(LOCAL_ROOT, "update.log");

            string lastUpdate = string.Empty;

            if (File.Exists(updateFilePath))
            {
                // Only force updates every 60 minutes.
                if (File.GetLastWriteTime(updateFilePath) > (DateTime.Now - TimeSpan.FromMinutes(60)))
                {
                    return;
                }

                using (StreamReader rdr = new StreamReader(updateFilePath))
                {
                    lastUpdate = rdr.ReadToEnd();
                }
            }

            if (lastUpdate.Length == 0)
            {
                XmlDocument serverTimeDoc = GetXml(new Uri("http://www.thetvdb.com/api/Updates.php?type=none"));

                lastUpdate = serverTimeDoc.SelectSingleNode("/Items/Time").InnerText;
            }

            XmlDocument doc = GetXml(new Uri(string.Format("http://thetvdb.com/api/Updates.php?type=all&time={0}", lastUpdate)));

            foreach (XmlNode seriesNode in doc.SelectNodes("/Items/Series"))
            {
                if (Directory.Exists(Path.Combine(LOCAL_ROOT, string.Concat("series/", seriesNode.InnerText))))
                {
                    DownloadSeries(Convert.ToInt32(seriesNode.InnerText), true);
                }
            }

            lastUpdate = doc.SelectSingleNode("/Items/Time").InnerText;
            using (StreamWriter w = new StreamWriter(updateFilePath, false))
            {
                w.Write(lastUpdate);
            }
        }

        public Episode FindEpisode(int seriesId, int season, int episode)
        {
            Series s = GetFullSeries(seriesId);

            foreach (Episode e in s.Episodes)
            {
                if (e.SeasonNumber == season && e.EpisodeNumber == episode)
                {
                    return e;
                }
            }

            return null;
        }

        public Episode FindEpisode(int seriesId, DateTime dateTime)
        {
            Series s = GetFullSeries(seriesId);

            foreach (Episode e in s.Episodes)
            {
                if (e.OriginalAirDate == dateTime)
                {
                    return e;
                }
            }

            return null;
        }

        public Episode FindEpisode(string seriesName, int season, int episode)
        {
            List<DataSeries> seriesList = GetSeries(seriesName);

            if (seriesList.Count > 0)
            {
                Series s = GetFullSeries(Convert.ToInt32(seriesList[0].id));

                foreach (Episode e in s.Episodes)
                {
                    if (e.SeasonNumber == season && e.EpisodeNumber == episode)
                    {
                        return e;
                    }
                }
            }

            return null;
        }

        public Episode FindEpisode(string seriesName, DateTime dateTime)
        {
            List<DataSeries> seriesList = GetSeries(seriesName);

            if (seriesList.Count > 0)
            {
                Series s = GetFullSeries(Convert.ToInt32(seriesList[0].id));

                foreach (Episode e in s.Episodes)
                {
                    if (e.OriginalAirDate == dateTime)
                    {
                        return e;
                    }
                }
            }

            return null;
        }

        public Series GetFullSeries(int seriesId)
        {
            XmlDocument doc = GetXml(string.Format("series/{0}/en.xml", seriesId));

            Series s = new Series();

            s.Id = Convert.ToInt32(doc.SelectSingleNode("/Data/Series/id").InnerText);
            s.Name = doc.SelectSingleNode("/Data/Series/SeriesName").InnerText;

            doc = GetXml(string.Format("series/{0}/banners.xml", seriesId));

            foreach (XmlNode bannerNode in doc.SelectNodes("/Banners/Banner"))
            {
                Banner b = new Banner();

                b.Id = Convert.ToInt32(bannerNode.SelectSingleNode("id").InnerText);
                b.Path = bannerNode.SelectSingleNode("BannerPath").InnerText;
                /*
                b.ThumbnailPath = bannerNode.SelectSingleNode("ThumbnailPath").InnerText;
                b.VignettePath = bannerNode.SelectSingleNode("VignettePath").InnerText;
                */
                b.BannerType = (BannerType)Enum.Parse(typeof(BannerType), bannerNode.SelectSingleNode("BannerType").InnerText, true);

                /*
                if (bannerNode.SelectSingleNode("Colors").InnerText.Trim().Length > 0)
                {
                    e.OriginalAirDate = Convert.ToDateTime(bannerNode.SelectSingleNode("FirstAired").InnerText);
                }
                */

                s.Banners.Add(b);
            }

            doc = GetXml(string.Format("series/{0}/all/en.xml", seriesId));

            foreach (XmlNode episodeNode in doc.SelectNodes("/Data/Episode"))
            {
                Episode e = new Episode();

                e.Series = s;

                e.Id = Convert.ToInt32(episodeNode.SelectSingleNode("id").InnerText);
                e.EpisodeNumber = Convert.ToInt32(episodeNode.SelectSingleNode("EpisodeNumber").InnerText);
                e.Name = episodeNode.SelectSingleNode("EpisodeName").InnerText;

                if (episodeNode.SelectSingleNode("FirstAired").InnerText.Trim().Length > 0)
                {
                    e.OriginalAirDate = Convert.ToDateTime(episodeNode.SelectSingleNode("FirstAired").InnerText);
                }

                e.SeasonNumber = Convert.ToInt32(episodeNode.SelectSingleNode("SeasonNumber").InnerText);

                s.Episodes.Add(e);
            }

            return s;
        }

        private List<DataSeries> GetSeries(string name)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Data));

            List<DataSeries> series = new List<DataSeries>();

            DataSeries dsLocal = GetSeriesLocal(name);

            if (dsLocal != null)
            {
                series.Add(dsLocal);
            }
            else
            {
                XmlDocument doc = GetXml(new Uri(string.Format("http://thetvdb.com/api/GetSeries.php?seriesname={0}", name)));

                using (XmlNodeReader reader = new XmlNodeReader(doc))
                {
                    Data d = serializer.Deserialize(reader) as Data;

                    foreach (DataSeries ds in d.Items)
                    {
                        DownloadSeries(Convert.ToInt32(ds.id), false);
                        series.Add(ds);
                    }
                }
            }

            return series;
        }

        private void DownloadSeries(int seriesId, bool forceDownload)
        {
            GetXml(string.Format("series/{0}/en.xml", seriesId), forceDownload);
            GetXml(string.Format("series/{0}/banners.xml", seriesId), forceDownload);
            GetXml(string.Format("series/{0}/all/en.xml", seriesId), forceDownload);
        }

        private DataSeries GetSeriesLocal(string name)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Data));

            foreach (string seriesFolder in Directory.GetDirectories(Path.Combine(LOCAL_ROOT, "series")))
            {
                using (StreamReader sr = new StreamReader(Path.Combine(seriesFolder, "en.xml")))
                {
                    Data ds = serializer.Deserialize(sr) as Data;

                    if (ds != null && ds.Items.Length > 0)
                    {
                        if (ds.Items[0].SeriesName == name)
                        {
                            return ds.Items[0];
                        }
                    }
                }
            }

            return null;
        }

        private XmlDocument GetXml(string path)
        {
            return GetXml(path, false);
        }

        private XmlDocument GetXml(string path, bool forceDownload)
        {
            XmlDocument doc = new XmlDocument();

            string localPath = Path.Combine(LOCAL_ROOT, path.Replace('/', '\\'));

            if (File.Exists(localPath) && !forceDownload)
            {
                doc.Load(localPath);
            }
            else
            {
                doc = GetXml(new Uri(string.Format("http://www.thetvdb.com/api/{0}/{1}", API_KEY, path)));

                if (!Directory.Exists(Path.GetDirectoryName(localPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                }

                doc.Save(localPath);
            }

            return doc;
        }

        private XmlDocument GetXml(Uri u)
        {
            XmlDocument doc = new XmlDocument();

            HttpWebRequest request = WebRequest.Create(u) as HttpWebRequest;

            request.UserAgent = "theTvDbProvider/1.0 Microsoft.Net/4.0";

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                doc.Load(response.GetResponseStream());
            }

            doc.Save("temp.xml");

            return doc;
        }
    }
}
