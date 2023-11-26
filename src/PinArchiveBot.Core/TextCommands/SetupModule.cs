using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PinArchiveBot.Core.Setup;

namespace PinArchiveBot.Core.TextCommands
{// Create a module with no prefix
	public class SetupModule : ModuleBase<SocketCommandContext>
	{
		private readonly ISetupRepository setupRepository;

		public SetupModule(ISetupRepository setupRepository)
		{
			this.setupRepository = setupRepository;
		}

		[Command("setup")]
		[Summary("Gives setup help text overview.")]
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
		public Task SetupHelpAsync([Remainder] string _)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
		{
			var sb = new StringBuilder();

			sb.AppendLine("The following commands are available for setting up:");
			sb.AppendLine("`channel` [channel link or id]: the channel to post pins for this guild into");
			sb.AppendLine("`livepin` [true or false]: automatically copy all newly made pins from now on (or don't)");
			sb.AppendLine("You can check your configuration:");
			sb.AppendLine("`querysetup`");
			return this.ReplyAsync(sb.ToString());
		}

		[Command("channel")]
		[Summary("Sets up the pins channel for this guild.")]
		public async Task ChannelAsync(IChannel channel)
		{
			if (channel is null)
			{
				await this.ReplyAsync("Something was wrong with the given channel.");
				return;
			}

			var storedSetup = await this.setupRepository.ReadGuildSetup(this.Context.Guild.Id);
			await this.setupRepository.WriteGuildSetup(storedSetup with { SingleTargetChannelId = channel.Id });
			await this.ReplyAsync($"{channel.Name} is set as the pins channel for this guild.");
		}

		[Command("livepin")]
		[Summary("Copies pins as they happen.")]
		public async Task LivePinAsync(bool value)
		{
			var storedSetup = await this.setupRepository.ReadGuildSetup(this.Context.Guild.Id);
			await this.setupRepository.WriteGuildSetup(storedSetup with { LivePin = value });
			if (value)
			{
				await this.ReplyAsync("New pins will now automatically be sent to the configured channel.");
			}
			else
			{
				await this.ReplyAsync("New pins will not be automatically processed, only manual pinning by reaction is available.");
			}
		}

		[Command("querysetup")]
		[Summary("Queries the current setup.")]
		public async Task QuerySetupAsync()
		{
			var storedSetup = await this.setupRepository.ReadGuildSetup(this.Context.Guild.Id);
			var sb = new StringBuilder();
			sb.AppendLine($"Setup for guild \"{this.Context.Guild.Name}\" ({this.Context.Guild.Id}):");
			if (storedSetup.SingleTargetChannelId.HasValue)
			{
				sb.AppendLine($"channel: <#{storedSetup.SingleTargetChannelId.Value}> ({storedSetup.SingleTargetChannelId})");
			}
			else
			{
				sb.AppendLine($"channel: not set");
			}

			sb.AppendLine($"livepin: {storedSetup.LivePin}");
			await this.ReplyAsync(sb.ToString());
		}
	}
}
