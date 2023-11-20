namespace PinArchiveBot.Core.Setup
{
	public interface ISetupRepository
	{
		public Task<GuildSetup> ReadGuildSetup(ulong guildId);

		public Task WriteGuildSetup(GuildSetup guildSetup);
	}
}
