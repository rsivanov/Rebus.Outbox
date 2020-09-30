using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Outbox.Headers;
using Rebus.Transport;

namespace Rebus.Outbox.Internals
{
	internal class OutboxTransportDecorator : ITransport
	{
		internal const string OutgoingMessagesItemsKey = "outbox-outgoing-messages";
		private readonly ITransport _transport;
		private readonly IOutboxStorage _outboxStorage;

		public OutboxTransportDecorator(ITransport transport, IOutboxStorage outboxStorage)
		{
			_transport = transport;
			_outboxStorage = outboxStorage;
		}
		
		public void CreateQueue(string address)
		{
			_transport.CreateQueue(address);
		}

		public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
		{
			var outgoingMessages = context.GetOrAdd(OutgoingMessagesItemsKey, () =>
			{
				var messages = new ConcurrentQueue<TransportMessage>();

				context.OnCommitted(tc => StoreOutgoingMessages(messages));

				return messages;
			});
			
			message.Headers.Add(OutboxHeaders.Recipient, destinationAddress);

			outgoingMessages.Enqueue(message);
			
			return Task.CompletedTask;
		}

		private async Task StoreOutgoingMessages(ConcurrentQueue<TransportMessage> messages)
		{
			foreach (var message in messages)
			{
				await _outboxStorage.Store(message);
			}
		}

		public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
		{
			return _transport.Receive(context, cancellationToken);
		}

		public string Address => _transport.Address;
	}
}