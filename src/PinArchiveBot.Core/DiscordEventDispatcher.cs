using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mail;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using PinArchiveBot.Core.Setup;

namespace PinArchiveBot.Core
{
	public class DiscordEventDispatcher
	{
		private readonly IServiceProvider serviceProvider;
		private readonly ILogger<DiscordEventDispatcher> logger;
		private readonly DiscordSocketClient client;
		private readonly CommandService commandService;
		private readonly ISetupRepository setupRepository;

		public DiscordEventDispatcher(IServiceProvider serviceProvider, ILogger<DiscordEventDispatcher> logger, DiscordSocketClient client, CommandService commandService, ISetupRepository setupRepository)
		{
			this.serviceProvider = serviceProvider;

			this.logger = logger;
			this.client = client;

			this.client.Ready += this.Client_Ready;

			this.client.MessageReceived += this.Client_MessageReceived;
			this.client.ReactionAdded += this.Client_ReactionAdded;
			this.client.AuditLogCreated += this.Client_AuditLogCreated;

			this.commandService = commandService;

			this.setupRepository = setupRepository;
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
			if (message.Author.IsBot)
			{
				return;
			}

			if (!message.HasMentionPrefix(this.client.CurrentUser, ref argPos))
			{
				if (message.MentionedUsers.Any(m => m.Id == this.client.CurrentUser.Id))
				{
					await message.ReplyAsync($"Hello! If you want help getting set up, please ask me: {this.client.CurrentUser.Mention} setup help");
				}

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

		private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> cacheableUserMessage, Cacheable<IMessageChannel, ulong> cacheableMessagechannel, SocketReaction reaction)
		{
			var message = await cacheableUserMessage.GetOrDownloadAsync();
			var channel = await cacheableMessagechannel.GetOrDownloadAsync();
			var context = new CommandContext(this.client, message);
			if (context.IsPrivate)
			{
				return;
			}

			if (reaction.Emote.Equals(new Emoji("📌")))
			{
				var reactingGuildUser = await context.Guild.GetUserAsync(reaction.UserId);
				var guildChannel = await context.Guild.GetChannelAsync(channel.Id);

				var hasPinPermission = reactingGuildUser.GuildPermissions.ManageMessages || reactingGuildUser.GetPermissions(guildChannel).ManageMessages;
				if (hasPinPermission)
				{
					await this.CreatePinMessageForMessage(context.Guild, message);
				}
			}
		}

		private async Task Client_AuditLogCreated(SocketAuditLogEntry auditLog, SocketGuild guild)
		{
			if (auditLog.Data is SocketMessagePinAuditLogData pinAuditLogData)
			{
				var guildSetup = await this.setupRepository.ReadGuildSetup(guild.Id);
				if (!guildSetup.LivePin)
				{
					return;
				}

				var message = await guild.GetTextChannel(pinAuditLogData.ChannelId).GetMessageAsync(pinAuditLogData.MessageId);
				await this.CreatePinMessageForMessage(guild, message);
			}
		}

		private async Task CreatePinMessageForMessage(IGuild guild, IMessage message)
		{
			GuildSetup guildSetup = await this.setupRepository.ReadGuildSetup(guild.Id);

			if (!guildSetup.SingleTargetChannelId.HasValue)
			{
				return;
			}

			var pinChannel = await guild.GetTextChannelAsync(guildSetup.SingleTargetChannelId.Value);

			var contentEmbedBuilder = new EmbedBuilder()
					.WithAuthor(message.Author)
					.WithColor(Color.Gold)
					.WithUrl(message.GetJumpUrl());
			contentEmbedBuilder.Fields.Add(new EmbedFieldBuilder()
				.WithName("Message text")
				.WithValue((string?)(string.IsNullOrEmpty(message.Content) ? "(empty)" : message.Content)));
			var contentEmbed = contentEmbedBuilder.Build();

			var imageEmbeds = message.Attachments
				.Where(a => a is { Height: not null, Width: not null })
				.Select((attachment) => new EmbedBuilder().WithImageUrl(attachment.Url).WithColor(Color.Gold).Build());

			var embedEmbeds = message.Embeds.OfType<Embed>();

			var embeds = Enumerable.Empty<Embed>().Append(contentEmbed).Concat(imageEmbeds).Concat(embedEmbeds).ToArray();

			await pinChannel.SendMessageAsync($"pinned {message.GetJumpUrl()}", embeds: embeds);
		}
	}
}
