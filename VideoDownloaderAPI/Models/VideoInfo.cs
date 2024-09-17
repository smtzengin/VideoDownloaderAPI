using System.Collections.Generic;

namespace VideoDownloaderAPI.Models
{
    public class VideoInfo
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public double? Duration { get; set; } // Nullable double
        public int? ViewCount { get; set; }    // Nullable int
        public List<DownloadOption> DownloadOptions { get; set; }
    }
}
