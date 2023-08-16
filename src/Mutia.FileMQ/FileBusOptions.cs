namespace Mutia.FileMQ
{
    public class FileBusOptions
    {

        public int FilesWatcherRefreshDelay { get; set; } = 100;
        public int PublisherHeartBeatDelay { get; set; } = 5000;
        public int ActivePublishersRefreshDelay { get; set; } = 10000;
        public int MaxRetry { get; set; } = 5;

    }
}
