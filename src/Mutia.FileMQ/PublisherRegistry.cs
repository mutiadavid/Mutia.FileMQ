using System;

namespace Mutia.FileMQ
{
    internal class PublisherRegistry
    {
        public PublisherRegistry()
        {

        }
        public PublisherRegistry(Guid id, DateTime creationTime)
        {
            PublisherId = id;
            LastHeartBeatTime = DateTime.UtcNow;
            CreationTime = creationTime;
        }

        public Guid PublisherId { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastHeartBeatTime { get; set; }
    }
}
