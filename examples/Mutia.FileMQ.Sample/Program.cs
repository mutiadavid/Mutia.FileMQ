namespace Mutia.FileMQ.Sample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var id = new Guid("a3ceece7-bdc3-408f-b195-b56924933a1f");

            var sharedDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MyTestFileBusApp");


            var publisher = new FilePublisher<Message>(sharedDirectory, id);
            var subscriber = new ConsoleSubscriber();

            await publisher.Subscribe(subscriber);

            int n = 1;
            while (true)
            {
                await publisher.Publish(new Message($"Message {n}"));

                await Task.Delay(1000);

                if (n > 100) break;

                n++;
            }

            Console.ReadKey();
        }
    }
}