using System.Collections.Generic;

namespace VideoDownloaderAPI.Models
{
    public class VideoInfo
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Thumbnail { get; set; }
        public double? Duration { get; set; }
        public int? ViewCount { get; set; }
        public List<DownloadOption> DownloadOptions { get; set; }
    }

}
