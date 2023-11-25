using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PinArchiveBot.Core.Setup
{
	public class JsonSetupRepository : ISetupRepository
	{
		private readonly SetupOptions options;
		private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

		public JsonSetupRepository(IOptions<SetupOptions> options)
		{
			this.options = options.Value;
		}

		public async Task<GuildSetup> ReadGuildSetup(ulong guildId)
		{
			try
			{
				var targetPath = Path.IsPathRooted(this.options.SetupFilePath)
					? this.options.SetupFilePath
					: Path.Combine(AppContext.BaseDirectory, this.options.SetupFilePath);
				using var fileStream = File.OpenRead(targetPath);
				using var reader = new StreamReader(fileStream);
				var content = await reader.ReadToEndAsync();

				var guildSetups = string.IsNullOrWhiteSpace(content) ? [] : JsonSerializer.Deserialize<Dictionary<ulong, GuildSetup>>(content, this.jsonSerializerOptions) ?? [];
				return guildSetups.TryGetValue(guildId, out var guildSetup) ? guildSetup : new GuildSetup(guildId, null, []);
			}
			catch (FileNotFoundException)
			{
				return new GuildSetup(guildId, null, []);
			}
		}

		public async Task WriteGuildSetup(GuildSetup guildSetup)
		{
			var targetPath = Path.IsPathRooted(this.options.SetupFilePath)
				? this.options.SetupFilePath
				: Path.Combine(AppContext.BaseDirectory, this.options.SetupFilePath);

			using var fileStream = File.Open(targetPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			using var reader = new StreamReader(fileStream);
			var content = await reader.ReadToEndAsync();

			var guildSetups = string.IsNullOrWhiteSpace(content) ? [] : JsonSerializer.Deserialize<Dictionary<ulong, GuildSetup>>(content) ?? [];
			guildSetups[guildSetup.GuildId] = guildSetup;

			fileStream.Seek(0, SeekOrigin.Begin);
			await JsonSerializer.SerializeAsync(fileStream, guildSetups, this.jsonSerializerOptions);
		}
	}
}
