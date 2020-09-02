using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Outbox
{
	/// <summary>
	/// Abstraction that handles how transport messages are stored in an outbox
	/// </summary>
	public interface IOutboxStorage
	{
		/// <summary>
		/// Saves transport message to the outbox storage instead of sending it.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		Task Store(TransportMessage message);

		/// <summary>
		/// Removes top next available messages from the outbox storage
		/// This should be called in a transaction with send
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <param name="topMessages"></param>
		/// <returns></returns>
		Task<TransportMessage[]> Retrieve(CancellationToken cancellationToken, int topMessages);
	}
}