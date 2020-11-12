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
	public static class OutboxConfigurationExtensions
	{
		/// <summary>
		/// Decorates transport to save messages into an outbox instead of sending them directly
		/// </summary>
		/// <param name="configurer"></param>
		/// <param name="outboxStorageConfigurer"></param>
		/// <param name="configureOptions"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static RebusConfigurer Outbox(this RebusConfigurer configurer,
			Action<StandardConfigurer<IOutboxStorage>> outboxStorageConfigurer, Action<OutboxOptions> configureOptions = null)
		{
			configurer.Transport(t =>
			{
				if (outboxStorageConfigurer == null)
					throw new ArgumentNullException(nameof(outboxStorageConfigurer));
			
				outboxStorageConfigurer(t.OtherService<IOutboxStorage>());
				var outboxOptions = new OutboxOptions();
				configureOptions?.Invoke(outboxOptions);

				t.Decorate(c =>
				{
					var transport = c.Get<ITransport>();
					var outboxStorage = c.Get<IOutboxStorage>();
					if (outboxOptions.RunMessagesProcessor)
					{
						var outboxMessagesProcessor = new OutboxMessagesProcessor(
							outboxOptions.MaxMessagesToRetrieve,
							transport,
							outboxStorage,
							c.Get<IBackoffStrategy>(),
							c.Get<IRebusLoggerFactory>(),
							c.Get<CancellationToken>());

						outboxMessagesProcessor.Run();
					}
					return new OutboxTransportDecorator(transport, outboxStorage);
				});

			});
			return configurer;
		}
	}
}