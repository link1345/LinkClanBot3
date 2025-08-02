using Discord.WebSocket;
using Discord;
using LinkClanBot3.Data;
using Microsoft.Extensions;
using NuGet.Protocol;

namespace LinkClanBot3.Discord
{
	public class SettingVoiceChannel
	{
		public List<string> LoginRole { get; set; } = [];
		public List<string> AdminRole { get; set; } = [];
	}


	public class DiscordEventService : BackgroundService
	{
		private readonly IServiceScopeFactory _scopeFactory;

		private ILogger<DiscordEventService> logger { get; set; }
		private DiscordSocketClient _client { set; get; }
		private IConfigurationRoot _configuration { set; get; }

		public DiscordEventService(IServiceScopeFactory scopeFactory)
		{
			_scopeFactory = scopeFactory;

			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				//   builder.AddConsole();  // Plain
				builder.AddJsonConsole(
					 options => options.IncludeScopes = true   // Enable Scope
				);
			});
			logger = loggerFactory.CreateLogger<DiscordEventService>();


			_configuration = new ConfigurationBuilder()
			   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
			   .AddUserSecrets<Program>()
			   .Build();


			var _config = new DiscordSocketConfig { MessageCacheSize = 100 };
			_client = new DiscordSocketClient(_config);
		}

		private T? getConfig<T>(string name)
		{
			return _configuration.GetSection(name).Get<T>();
		}

		public override void Dispose()
		{
			_client.Dispose();
		}


		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			logger.LogInformation("DicordEventService Start");
			var token = getConfig<string>("DiscordToken");
			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();

			_client.MessageUpdated += MessageUpdated;
			_client.UserVoiceStateUpdated += UserVoiceStateUpdated;
			
			_client.Connected += () =>
			{
				logger.LogInformation("DicordEventService Connected");

				return Task.CompletedTask;
			};
			_client.Ready += () =>
			{
				var test = _client.GetChannel(626015039681200130);
				if (test is SocketTextChannel channel)
				{
					channel.SendMessageAsync("Bot is Ready!");
				}
				else
				{
					Console.WriteLine("Bot is Ready!");
				}
				return Task.CompletedTask;
			};

			await Task.CompletedTask;
		}

		private Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
		{
			using (var scope = _scopeFactory.CreateScope())
			{
				var _dbContext = scope.ServiceProvider.GetRequiredService<LinkClanBot3Context>();
				
				var roleFlag = false;
				var LoginRoleItem = getConfig<SettingVoiceChannel>("VoiceChannle");
				if (LoginRoleItem == null) return Task.CompletedTask;

				foreach (var role_item in _client.GetGuild(Convert.ToUInt64(getConfig<string>("Guild") ?? "")).GetUser(arg1.Id).Roles)
				{
					
					foreach(var role in LoginRoleItem.LoginRole)
					{
						if (role_item.Id == Convert.ToUInt64(role))
						{
							roleFlag = true;
							break;
						}
					}
				}
				if(roleFlag == false) {
					return Task.CompletedTask;
				}

				var user = _dbContext.Member.FirstOrDefault(x => x.DiscordID == arg1.Id.ToString());
				if(user == null)
				{
					user = _dbContext.Member.Add(new Member()
					{
						CallName = "",
						Role = MemberRole.Member,
						OriginID = "",
						DiscordID = arg1.Id.ToString(),
						DiscordDisplayName = arg1.Username,
						DiscordName = arg1.GlobalName,
						SteamID = "",
						UplayID = "",
						BATTEL_NET_BattleTag = "",
						epicgamesID = "",
						PlayStationID = "",
						XboxID = "",
						SNS_X_UserID = ""
					}).Entity;

					_dbContext.SaveChanges();
				}

				// 入室
				if (arg2.VoiceChannel == null)
				{
					Console.WriteLine($"入室 , {user.DiscordDisplayName}");
					_dbContext.MemberTimeLine.Add(new MemberTimeLine
					{
						MemberData = user,
						EnteringRoom = EnteringRoom.Entry,
						before_channel_id = null,
						before_channel_name = null,
						after_channel_id = arg3.VoiceChannel?.Id.ToString() ?? "",
						after_channel_name = arg3.VoiceChannel?.Name ?? "",
						EventDate = DateTime.Now
					});
				}
				// 退出
				else if (arg3.VoiceChannel == null)
				{
					Console.WriteLine($"退出 , {user.DiscordDisplayName}");
					_dbContext.MemberTimeLine.Add(new MemberTimeLine
					{
						MemberData = user,
						EnteringRoom = EnteringRoom.Exit,
						before_channel_id = arg2.VoiceChannel?.Id.ToString() ?? "",
						before_channel_name = arg2.VoiceChannel?.Name ?? "",
						after_channel_id = null,
						after_channel_name = null,
						EventDate = DateTime.Now
					});
				}
				// 移動
				else
				{
					Console.WriteLine($"移動 , {user.DiscordDisplayName}");
					_dbContext.MemberTimeLine.Add(new MemberTimeLine
					{
						MemberData = user,
						EnteringRoom = EnteringRoom.Move,
						before_channel_id = arg2.VoiceChannel?.Id.ToString() ?? "",
						before_channel_name = arg2.VoiceChannel?.Name ?? "",
						after_channel_id = arg3.VoiceChannel?.Id.ToString() ?? "",
						after_channel_name = arg3.VoiceChannel?.Name ?? "",
						EventDate = DateTime.Now
					});
				}
                _dbContext.SaveChanges();
            }
			return Task.CompletedTask;
		}

		private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
		{
			var message = await before.GetOrDownloadAsync();
			Console.WriteLine($"{message} -> {after}");
		}


	}
}
