using System;

namespace Rebus.Outbox.Config
{
	/// <summary>
	/// Outbox configuration options
	/// </summary>
	public class OutboxOptions
	{
		/// <summary>
		/// Whether to run a background task to send messages from the outbox through the transport
		/// </summary>
		public bool RunMessagesProcessor { get; set; } = true;

		/// <summary>
		/// Max number of messages to retrieve from the outbox and send through the transport in a single transaction
		/// </summary>
		public int MaxMessagesToRetrieve { get; set; } = 10;
	}
}