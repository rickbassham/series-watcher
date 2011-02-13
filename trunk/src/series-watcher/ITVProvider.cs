using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeriesWatcher
{
    public interface ITVProvider
    {
        void Update();
        Episode FindEpisode(int seriesId, int season, int episode);
        Episode FindEpisode(int seriesId, DateTime dateTime);
        Episode FindEpisode(string seriesName, int season, int episode);
        Episode FindEpisode(string seriesName, DateTime dateTime);
        Series GetFullSeries(int seriesId);
    }
}
