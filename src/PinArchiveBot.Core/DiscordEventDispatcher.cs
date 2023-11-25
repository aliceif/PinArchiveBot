using System.Globalization;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace PinArchiveBot.Core
{
	public class DiscordEventDispatcher
	{
		private readonly IServiceProvider serviceProvider;
		private readonly ILogger<DiscordEventDispatcher> logger;
		private readonly DiscordSocketClient client;
		private readonly CommandService commandService;

		public DiscordEventDispatcher(IServiceProvider serviceProvider, ILogger<DiscordEventDispatcher> logger, DiscordSocketClient client, CommandService commandService)
		{
			this.serviceProvider = serviceProvider;

			this.logger = logger;
			this.client = client;

			this.client.Ready += this.Client_Ready;

			this.client.MessageReceived += this.Client_MessageReceived;

			this.commandService = commandService;
		}

		private async Task Client_Ready()
		{
			await this.client.SetGameAsync("helping with your pins", type: ActivityType.CustomStatus);

			// Here we discover all of the command modules in the entry
			// assembly and load them. Starting from Discord.NET 2.0, a
			// service provider is required to be passed into the
			// module registration method to inject the
			// required dependencies.
			//
			// If you do not use Dependency Injection, pass null.
			// See Dependency Injection guide for more information.
			await this.commandService.AddModulesAsync(assembly: Assembly.GetAssembly(typeof(DiscordEventDispatcher)), services: this.serviceProvider);
		}

		private async Task Client_MessageReceived(SocketMessage messageParam)
		{
			// Don't process the command if it was a system message
			if (messageParam is not SocketUserMessage message)
			{
				return;
			}

			var context = new SocketCommandContext(this.client, message);

			// Create a number to track where the prefix ends and the command begins
			int argPos = 0;

			// Determine if the message is a command based on the prefix and make sure no bots trigger commands
			if (message.Author.IsBot || !message.HasMentionPrefix(this.client.CurrentUser, ref argPos))
			{
				return;
			}

			// Execute the command with the command context we just
			// created, along with the service provider for precondition checks.
			await this.commandService.ExecuteAsync(
				context: context,
				argPos: argPos,
				services: this.serviceProvider);

			return;
		}
	}
}
