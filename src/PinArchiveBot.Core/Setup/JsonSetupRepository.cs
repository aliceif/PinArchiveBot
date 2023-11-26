using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PinArchiveBot.Core.Setup
{
	public class JsonSetupRepository : ISetupRepository
	{
		private readonly SetupOptions options;
		private readonly ILogger<JsonSetupRepository> logger;

		private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

		public JsonSetupRepository(IOptions<SetupOptions> options, ILogger<JsonSetupRepository> logger)
		{
			this.options = options.Value;
			this.logger = logger;
		}

		public async Task<GuildSetup> ReadGuildSetup(ulong guildId)
		{
			var targetPath = Path.IsPathRooted(this.options.SetupFilePath)
				? this.options.SetupFilePath
				: Path.Combine(AppContext.BaseDirectory, this.options.SetupFilePath);
			try
			{
				using var fileStream = File.OpenRead(targetPath);
				using var reader = new StreamReader(fileStream);
				var content = await reader.ReadToEndAsync();

				var guildSetups = string.IsNullOrWhiteSpace(content) ? [] : JsonSerializer.Deserialize<Dictionary<ulong, GuildSetup>>(content, this.jsonSerializerOptions) ?? [];
				return guildSetups.TryGetValue(guildId, out var guildSetup) ? guildSetup : new GuildSetup(guildId, null, []);
			}
			catch (FileNotFoundException)
			{
				this.logger.LogWarning("Setup file did not exist yet?  {SetupFilePath}, effectively {TargetPath}", this.options.SetupFilePath, targetPath);
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
