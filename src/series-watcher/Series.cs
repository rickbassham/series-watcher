using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace SeriesWatcher
{
    public class Series
    {
        public int Id;
        public string Name;

        public List<Banner> Banners = new List<Banner>();
        public List<Episode> Episodes = new List<Episode>();
    }

    public enum BannerType
    {
        Series,
        Season,
        FanArt,
        Poster
    }

    public class Banner
    {
        public int Id;
        public string Path;
        public string ThumbnailPath;
        public string VignettePath;
        public BannerType BannerType;
        public Color[] Colors;
    }

    public class Episode
    {
        public int Id;
        public string Name;
        public int? SeasonNumber;
        public int? EpisodeNumber;
        public DateTime? OriginalAirDate;

        public Series Series;
    }
}
