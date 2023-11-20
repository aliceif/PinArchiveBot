namespace PinArchiveBot.Core.Setup
{
	public record class GuildSetup(ulong GuildId, ulong? SingleTargetChannelId, Dictionary<ulong, ulong> PerChannelTargetIds);
}
