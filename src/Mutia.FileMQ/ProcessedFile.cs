using System;

namespace Mutia.FileMQ
{
    internal class ProcessedFile
    {
        public ProcessedFile()
        {

        }
        public ProcessedFile(string name)
        {
            Name = name;
            ProcessedTime = DateTime.UtcNow;
        }
        public string Name { get; set; }
        public DateTime ProcessedTime { get; set; }
    }
}
