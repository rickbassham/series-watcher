# Introduction #

This program watches a folder for new files, uses [TheTVDB.com](http://www.thetvdb.com) to lookup information about them, and renames the files to a destination folder of your choice.


# Details #

To build, you will need Visual C# 2010 Express edition (available for free from Microsoft), or just download the pre-built files.

To run, edit the values of the series-watcher.exe.config file in your favorite text editor. Don't change the keys, just the values.

  * `LOCAL_ROOT` = the directory you want to use to cache info from theTvDb.com (don't change unless necessary).
  * `WATCH_FOLDER` = the folder you want series-watcher to watch (should probably only contain files for TV series).
  * `MOVE_FOLDER` = the root folder you want to move your files to after renaming.
  * `MKVMERGE` = the full path to mkvmerge.exe
  * `CONVERT_TO_MKV` = `true` or `false` to convert the file to mkv format when it moves to the `MOVE_FOLDER`

series-watcher supports the following formats for TV shows in the WATCH\_FOLDER:
```
<SeriesId>\<Title> S<Season>E<Episode>
<Title> S<Season>E<Episode>
<SeriesId>\<Title> <Date>
<Title> <Date>
```
Where `<SeriesId>` is the [TheTVDB.com](http://www.thetvdb.com) series ID, `<Season>` is a number, `<Episode>` is a number, and `<Date>` is in the following format dd/MM/yy or dd.mm.yy or yyyy.mm.dd or yyyy/mm/dd

Examples:
  * 71256\The.Daily.Show.2011.02.17.blah blah blah.avi
  * Smallville.S10E14.720p.HDTV.blah blah blah.mkv

The application will try to find a match on [TheTVDB.com](http://www.thetvdb.com) and rename the file as follows:

```
MOVE_FOLDER\Series Name\Season XX\Series Name - SXXEXX - Episode Name.extension
```

The SeriesId is useful if the application can't reliably choose the correct series from [TheTVDB.com](http://www.thetvdb.com).