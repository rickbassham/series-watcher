using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using theTvDb;
using System.IO;

namespace SeriesWatcher
{
    class Program
    {
        static readonly string MOVE_FOLDER = System.Configuration.ConfigurationManager.AppSettings["MOVE_FOLDER"];
        static readonly string WATCH_FOLDER = System.Configuration.ConfigurationManager.AppSettings["WATCH_FOLDER"];

        static RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;

        static Regex[] regularExpressions = new Regex[] {
            new Regex(@".*\\(?<SeriesId>\d+)\\(?<Title>.*?)S(?<Season>\d+)E(?<Episode>\d+)", options),
            new Regex(@".*\\(?<Title>.*?)S(?<Season>\d+)E(?<Episode>\d+)", options),
            new Regex(@".*\\(?<SeriesId>\d+)\\(?<Title>.*?)(?<Date>\d+-\d+-\d+)", options),
            new Regex(@".*\\(?<Title>.*?)(?<Date>\d+-\d+-\d+)", options),
            new Regex(@".*\\(?<SeriesId>\d+)\\(?<Title>.*?)(?<Date>\d+\.\d+\.\d+)", options),
            new Regex(@".*\\(?<Title>.*?)(?<Date>\d+\.\d+\.\d+)", options),
        };

        static void Main(string[] args)
        {
            theTvDbProvider provider = new theTvDbProvider();
            provider.Update();

            string folder = WATCH_FOLDER;

            if (Directory.Exists(folder))
            {
                foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                {

                    bool matchFound = false;

                    foreach (Regex r in regularExpressions)
                    {
                        if (matchFound)
                        {
                            break;
                        }

                        Match m = r.Match(file);

                        if (m != null && m.Success)
                        {
                            matchFound = true;

                            Episode e = null;

                            if (m.Groups["SeriesId"].Success)
                            {
                                if (m.Groups["Season"].Success)
                                {
                                    Console.WriteLine("Found SeriesId: {0} Series: {1} Season: {2} Episode {3}",
                                        m.Groups["SeriesId"].Value,
                                        CleanTitle(m.Groups["Title"].Value), m.Groups["Season"].Value, m.Groups["Episode"].Value);

                                    e = provider.FindEpisode(
                                        Convert.ToInt32(m.Groups["SeriesId"].Value),
                                        Convert.ToInt32(m.Groups["Season"].Value),
                                        Convert.ToInt32(m.Groups["Episode"].Value));
                                }
                                else
                                {
                                    Console.WriteLine("Found Series: {0} Date: {1}",
                                     CleanTitle(m.Groups["Title"].Value), m.Groups["Date"].Value);

                                    e = provider.FindEpisode(
                                        Convert.ToInt32(m.Groups["SeriesId"].Value),
                                        Convert.ToDateTime(m.Groups["Date"].Value));
                                }
                            }
                            else if (m.Groups["Season"].Success)
                            {
                                Console.WriteLine("Found Series: {0} Season: {1} Episode {2}",
                                    CleanTitle(m.Groups["Title"].Value), m.Groups["Season"].Value, m.Groups["Episode"].Value);

                                e = provider.FindEpisode(
                                    CleanTitle(m.Groups["Title"].Value),
                                    Convert.ToInt32(m.Groups["Season"].Value),
                                    Convert.ToInt32(m.Groups["Episode"].Value));
                            }
                            else if (m.Groups["Date"].Success)
                            {
                                Console.WriteLine("Found Series: {0} Date: {1}",
                                   CleanTitle(m.Groups["Title"].Value), m.Groups["Date"].Value);

                                e = provider.FindEpisode(
                                    CleanTitle(m.Groups["Title"].Value),
                                    Convert.ToDateTime(m.Groups["Date"].Value));
                            }

                            if (e != null)
                            {
                                string episodeName = e.Name;

                                foreach (char c in Path.GetInvalidFileNameChars())
                                {
                                    episodeName = episodeName.Replace(c.ToString(), string.Empty);
                                }

                                string newPath = Path.Combine(MOVE_FOLDER, string.Format(@"{0}\Season {1}\{0} - S{1}E{2} - {3}{4}",
                                    e.Series.Name,
                                    e.SeasonNumber.Value.ToString("00"),
                                    e.EpisodeNumber.Value.ToString("00"),
                                    episodeName,
                                    Path.GetExtension(file)));


                                if (!Directory.Exists(Path.GetDirectoryName(newPath)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                                }

                                Console.WriteLine("Moving to {0}", newPath);

                                if (!File.Exists(newPath))
                                {
                                    File.Move(file, newPath);
                                }
                            }
                        }
                    }
                }
            }
        }

        static string CleanTitle(string title)
        {
            Regex r2 = new Regex(@"(?<!\.)\.", options);

            return r2.Replace(title, " ").Trim();
        }
    }
}
