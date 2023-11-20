using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace PinArchiveBot.Core
{
	public class DiscordEventDispatcher
	{
		private readonly ILogger<DiscordEventDispatcher> logger;
		private readonly DiscordSocketClient client;

		public DiscordEventDispatcher(ILogger<DiscordEventDispatcher> logger, DiscordSocketClient client)
		{
			this.logger = logger;
			this.client = client;

			this.client.Ready += this.Client_Ready;

			this.client.MessageReceived += this.Client_MessageReceived;
		}

		private async Task Client_Ready()
		{
			await this.client.SetGameAsync("helping with your pins", type: ActivityType.CustomStatus);
		}

		private Task Client_MessageReceived(SocketMessage messageParam)
		{
			// Don't process the command if it was a system message
			if (messageParam is not SocketUserMessage message)
			{
				return Task.CompletedTask;
			}

			var context = new SocketCommandContext(this.client, message);

			return Task.CompletedTask;
		}
	}
}
