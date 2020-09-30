using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Rebus.Messages;
using Rebus.Outbox.Headers;
using Rebus.Outbox.Internals;
using Rebus.Transport;
using Xunit;

namespace Rebus.Outbox.Tests
{
	public class OutboxTransportDecoratorTests
	{
		private readonly OutboxTransportDecorator _decorator;
		private readonly ITransport _transport;
		private readonly IOutboxStorage _outboxStorage;

		public OutboxTransportDecoratorTests()
		{
			_transport = Substitute.For<ITransport>();
			_outboxStorage = Substitute.For<IOutboxStorage>();
			_decorator = new OutboxTransportDecorator(_transport, _outboxStorage);
		}
		
		[Fact]
		public void CreateQueue_DelegatesToTransport()
		{
			const string queue = "TestQueue";
			_decorator.CreateQueue(queue);
			_transport.Received().CreateQueue(queue);
		}
		
		[Fact]
		public void Address_ReturnsTransportAddress()
		{
			const string address = "TestAddress";
			_transport.Address.Returns(address);
			Assert.Equal(address, _decorator.Address);
		}

		[Fact]
		public async Task Receive_DelegatesToTransport()
		{
			var transactionContext = Substitute.For<ITransactionContext>();
			var cancellationToken = new CancellationToken();
			var transportMessage = new TransportMessage(new Dictionary<string, string>(), new byte[0]);

			_transport.Receive(transactionContext, cancellationToken).Returns(Task.FromResult(transportMessage));
			var result = await _decorator.Receive(transactionContext, cancellationToken);
			Assert.Same(transportMessage, result);
		}

		[Fact]
		public async Task Send_StoresMessagesToOutboxOnCommit()
		{
			using var rebusTransactionScope = new RebusTransactionScope();
			
			var transportMessage1 = new TransportMessage(new Dictionary<string, string>(), new byte[0]);
			const string destinationAddress1 = "TestAddress1";
			var transportMessage2 = new TransportMessage(new Dictionary<string, string>(), new byte[0]);
			const string destinationAddress2 = "TestAddress2";
			
			await _decorator.Send(destinationAddress1, transportMessage1, rebusTransactionScope.TransactionContext);
			await _decorator.Send(destinationAddress2, transportMessage2, rebusTransactionScope.TransactionContext);

			Assert.True(rebusTransactionScope.TransactionContext.Items.TryGetValue(
				OutboxTransportDecorator.OutgoingMessagesItemsKey, out var outgoingMessagesObject));

			var outgoingMessages = ((IEnumerable<TransportMessage>) outgoingMessagesObject).ToArray();
			Assert.Contains(transportMessage1, outgoingMessages);
			Assert.Contains(transportMessage2, outgoingMessages);

			var recipient1 = Assert.Contains(OutboxHeaders.Recipient, transportMessage1.Headers as IDictionary<string, string>);
			Assert.Equal(destinationAddress1, recipient1);
			
			var recipient2 = Assert.Contains(OutboxHeaders.Recipient, transportMessage2.Headers as IDictionary<string, string>);
			Assert.Equal(destinationAddress2, recipient2);
			
			await rebusTransactionScope.CompleteAsync();

			await _outboxStorage.Received().Store(transportMessage1);
			await _outboxStorage.Received().Store(transportMessage2);
		}
	}
}