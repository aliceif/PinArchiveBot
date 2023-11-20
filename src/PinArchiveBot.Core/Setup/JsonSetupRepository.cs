using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PinArchiveBot.Core.Setup
{
	public class JsonSetupRepository : ISetupRepository
	{
		private readonly SetupOptions options;

		public JsonSetupRepository(IOptions<SetupOptions> options)
		{
			this.options = options.Value;
		}

		public async Task<GuildSetup> ReadGuildSetup(ulong guildId)
		{
			using var fileStream = File.OpenRead(this.options.SetupFilePath);
			var guildSetups = await JsonSerializer
				.DeserializeAsync<Dictionary<ulong, GuildSetup>>(fileStream) ?? [];
			return guildSetups.TryGetValue(guildId, out var guildSetup) ? guildSetup : new GuildSetup(guildId, null, []);
		}

		public async Task WriteGuildSetup(GuildSetup guildSetup)
		{
			using var fileStream = File.Open(this.options.SetupFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			var guildSetups = await JsonSerializer
				.DeserializeAsync<Dictionary<ulong, GuildSetup>>(fileStream) ?? [];
			guildSetups[guildSetup.GuildId] = guildSetup;
			await JsonSerializer.SerializeAsync(fileStream, guildSetups, new JsonSerializerOptions { WriteIndented = true });
		}
	}
}
