namespace Mutia.FileMQ.Sample
{
    public class ConsoleSubscriber : ISubscriber<Message>
    {
        public async Task HandleMessage(Message message)
        {
            Console.WriteLine(message);

        }
    }
}
