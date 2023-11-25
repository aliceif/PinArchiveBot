using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using PinArchiveBot.Core;
using PinArchiveBot.Core.Setup;
using PinArchiveBot.Service;

IHost host = Host.CreateDefaultBuilder(args)
	.UseSystemd()
	.ConfigureAppConfiguration((hostContext, builder) =>
	{
		// Add other providers for JSON, etc.

		// only use user secrets when debugging.
		if (Debugger.IsAttached)
		{
			builder.AddUserSecrets<Program>();
		}
	})
	.ConfigureLogging((hostContext, builder) =>
	{
		// if we're in fact using systemd, throw out the default console logger and only use the systemd journald
		if (Microsoft.Extensions.Hosting.Systemd.SystemdHelpers.IsSystemdService())
		{
			builder.ClearProviders();
			builder.AddJournal(options => options.SyslogIdentifier = hostContext.Configuration["SyslogIdentifier"]);
		}
	})
	.ConfigureServices(services =>
	{
		services.AddHostedService<Worker>();

		var discordSocketConfig = new DiscordSocketConfig
		{
			// request all unprivileged but unrequest the ones that keep causing log spam
			GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildInvites & ~GatewayIntents.GuildScheduledEvents | GatewayIntents.MessageContent,
		};
		var client = new DiscordSocketClient(config: discordSocketConfig);

		services.AddSingleton(client);
		services.AddSingleton<DiscordEventDispatcher>();

		services.AddSingleton<ISetupRepository, JsonSetupRepository>();

		services.AddOptions<SetupOptions>(nameof(SetupOptions));
	})
	.Build();

await host.RunAsync();
