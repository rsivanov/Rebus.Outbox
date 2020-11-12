using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Outbox.Headers;
using Rebus.Outbox.Internals;
using Rebus.Transport;
using Rebus.Workers.ThreadPoolBased;
using Xunit;

namespace Rebus.Outbox.Tests
{
	public class OutboxMessageProcessorTests
	{
		private readonly OutboxMessagesProcessor _processor;
		private readonly ITransport _transport;
		private readonly IOutboxStorage _outboxStorage;
		private readonly IBackoffStrategy _backoffStrategy;
		private readonly CancellationTokenSource _busDisposalCancellationTokenSource;
		private const int TopMessagesToRetrieve = 5;
		
		public OutboxMessageProcessorTests()
		{
			_transport = Substitute.For<ITransport>();
			_outboxStorage = Substitute.For<IOutboxStorage>();
			_backoffStrategy = Substitute.For<IBackoffStrategy>();
			_busDisposalCancellationTokenSource = new CancellationTokenSource();
			_processor = new OutboxMessagesProcessor(
				TopMessagesToRetrieve,
				_transport,
				_outboxStorage,
				_backoffStrategy,
				new NullLoggerFactory(),
				_busDisposalCancellationTokenSource.Token);
		}
		
		[Fact]
		public async Task ProcessOutboxMessages_WhenNoMessages_WaitsAccordingToBackoffStrategy()
		{
			_outboxStorage.When(o => o.Retrieve(_busDisposalCancellationTokenSource.Token, TopMessagesToRetrieve)).Do(
				c =>
				{
					_busDisposalCancellationTokenSource.Cancel();
				});
			await _processor.Run();
			await _backoffStrategy.Received().WaitNoMessageAsync(_busDisposalCancellationTokenSource.Token);
		}
		
		[Fact]
		public async Task ProcessOutboxMessages_WhenMessagesExist_SendsThemThroughTransportAndResetsBackoffStrategy()
		{
			var transportMessage1 = new TransportMessage(new Dictionary<string, string>(), new byte[0]);
			const string destinationAddress1 = "TestAddress1";
			transportMessage1.Headers.Add(OutboxHeaders.Recipient, destinationAddress1);
			
			const string destinationAddress2 = "TestAddress2";	
			var transportMessage2 = new TransportMessage(new Dictionary<string, string>(), new byte[0]);
			transportMessage2.Headers.Add(OutboxHeaders.Recipient, destinationAddress2);

			var transportMessages = new[] {transportMessage1, transportMessage2};
			_outboxStorage.Retrieve(_busDisposalCancellationTokenSource.Token, TopMessagesToRetrieve)
				.Returns(Task.FromResult(transportMessages));
			
			_outboxStorage.When(o => o.Retrieve(_busDisposalCancellationTokenSource.Token, TopMessagesToRetrieve)).Do(
				c =>
				{
					_busDisposalCancellationTokenSource.Cancel();
				});
			
			await _processor.Run();

			await _transport.Received().Send(destinationAddress1, transportMessage1, Arg.Any<ITransactionContext>());
			await _transport.Received().Send(destinationAddress2, transportMessage2, Arg.Any<ITransactionContext>());
			_backoffStrategy.Received().Reset();
		}		
	}
}