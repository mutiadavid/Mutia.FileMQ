using System.Threading.Tasks;

namespace Mutia.FileMQ
{
    public interface ISubscriber<TMessage>
    {
        /// <summary>
        /// Handles a received message of type TMessage.
        /// </summary>
        /// <param name="message">The received message.</param>
        Task HandleMessage(TMessage message);
    }
}
