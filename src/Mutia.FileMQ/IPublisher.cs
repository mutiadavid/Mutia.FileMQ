using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace Mutia.FileMQ
{

    public interface IPublisher<TMessage>
    {
        /// <summary>
        /// Subscribes a subscriber to receive messages of type TMessage.
        /// </summary>
        /// <param name="subscriber">The subscriber to be added.</param>
        Task Subscribe(ISubscriber<TMessage> subscriber);

        /// <summary>
        /// Unsubscribes a subscriber from receiving messages of type TMessage.
        /// </summary>
        /// <param name="subscriber">The subscriber to be removed.</param>
        Task Unsubscribe(ISubscriber<TMessage> subscriber);

        /// <summary>
        /// Publishes a message to all subscribed subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        Task Publish(TMessage message);
    }
}
