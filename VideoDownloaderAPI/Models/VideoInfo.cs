using System.Collections.Generic;

namespace VideoDownloaderAPI.Models
{
    public class VideoInfo
    {
        public string? ChannelName { get; set; }
        public uint? ChannelFollowerCount { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? Thumbnail { get; set; }
        public string? DurationString { get; set; }
        public uint? LikeCount { get; set; }
        public int? ViewCount { get; set; }
        public List<DownloadOption> DownloadOptions { get; set; }
    }

}
