using System;
using System.Threading;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Outbox.Internals;
using Rebus.Transport;
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.Outbox.Config
{
	/// <summary>
	/// Configuration extensions to use transactional outbox for transport
	/// </summary>
	public static class OutboxTransportConfigurationExtensions
	{
		/// <summary>
		/// Decorates transport to save messages into an outbox instead of sending them directly
		/// </summary>
		/// <param name="configurer"></param>
		/// <param name="outboxStorageConfigurer"></param>
		/// <param name="runOutboxMessagesProcessor">Whether to run a background task to send messages from the outbox through the transport</param>
		/// <exception cref="ArgumentNullException"></exception>
		public static void UseOutbox(this StandardConfigurer<ITransport> configurer,
			Action<StandardConfigurer<IOutboxStorage>> outboxStorageConfigurer, bool runOutboxMessagesProcessor = true)
		{
			if (configurer == null) 
				throw new ArgumentNullException(nameof(configurer));
			if (outboxStorageConfigurer == null)
				throw new ArgumentNullException(nameof(outboxStorageConfigurer));
			
			outboxStorageConfigurer(configurer.OtherService<IOutboxStorage>());
			
			configurer.Decorate(c =>
			{
				var transport = c.Get<ITransport>();
				var outboxStorage = c.Get<IOutboxStorage>();
				if (runOutboxMessagesProcessor)
				{
					var outboxMessagesProcessor = new OutboxMessagesProcessor(transport, outboxStorage, c.Get<IBackoffStrategy>(),
						c.Get<IRebusLoggerFactory>(), c.Get<CancellationToken>());
					outboxMessagesProcessor.Run();
				}
				return new OutboxTransportDecorator(transport, outboxStorage);
			});
		}
	}
}