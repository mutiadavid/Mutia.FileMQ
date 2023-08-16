using System;
using System.Collections.Generic;

namespace Mutia.FileMQ
{
    internal class ActivePublishers
    {
        public List<PublisherRegistry> Publishers { get; set; } = new List<PublisherRegistry>();
        public DateTime? LastRefresh { get; set; }
    }
}
