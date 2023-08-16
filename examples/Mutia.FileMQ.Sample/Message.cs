using System.Text.Json;

namespace Mutia.FileMQ.Sample
{
    public class Message
    {
        public Message(string waboWaba)
        {
            WaboWaba = waboWaba;
        }
        public string WaboWaba
        {
            get; set;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
