using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Rebus.Logging;
using Rebus.Outbox.Headers;
using Rebus.Transport;
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.Outbox.Internals
{
	public class OutboxMessagesProcessor
	{
		private readonly int _topMessagesToRetrieve;
		private readonly ITransport _transport;
		private readonly IOutboxStorage _outboxStorage;
		private readonly IBackoffStrategy _backoffStrategy;
		private readonly CancellationToken _busDisposalCancellationToken;
		private readonly ILog _log;

		public OutboxMessagesProcessor(int topMessagesToRetrieve, ITransport transport, IOutboxStorage outboxStorage, IBackoffStrategy backoffStrategy, IRebusLoggerFactory rebusLoggerFactory, CancellationToken busDisposalCancellationToken)
		{
			_topMessagesToRetrieve = topMessagesToRetrieve;
			_transport = transport;
			_outboxStorage = outboxStorage;
			_backoffStrategy = backoffStrategy;
			_busDisposalCancellationToken = busDisposalCancellationToken;
			_log = rebusLoggerFactory.GetLogger<OutboxMessagesProcessor>();
		}

		private async Task ProcessOutboxMessages()
		{
			_log.Debug("Starting outbox messages processor");

			while (!_busDisposalCancellationToken.IsCancellationRequested)
			{
				try
				{
					bool waitForMessages;
					using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
					{
						var messages = await _outboxStorage.Retrieve(_busDisposalCancellationToken, _topMessagesToRetrieve);
						if (messages.Length > 0)
						{
							using (var rebusTransactionScope = new RebusTransactionScope())
							{
								foreach (var message in messages)
								{
									var destinationAddress = message.Headers[OutboxHeaders.Recipient];
									message.Headers.Remove(OutboxHeaders.Recipient);
									await _transport.Send(destinationAddress, message,
										rebusTransactionScope.TransactionContext);
								}
								await rebusTransactionScope.CompleteAsync();
							}
							waitForMessages = false;
						}
						else
						{
							waitForMessages = true;
						}

						transactionScope.Complete();
					}

					if (waitForMessages)
					{
						await _backoffStrategy.WaitNoMessageAsync(_busDisposalCancellationToken);
					}
					else
					{
						_backoffStrategy.Reset();
					}
				}
				catch (OperationCanceledException) when (_busDisposalCancellationToken.IsCancellationRequested)
				{
					// we're shutting down
				}
				catch (Exception exception)
				{
					_log.Error(exception, "Unhandled exception in outbox messages processor");
				}
			}
			_log.Debug("Outbox messages processor stopped");
		}

		public Task Run() => Task.Run(ProcessOutboxMessages);
	}
}