using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using LinkClanBot3.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions;
using NuGet.Protocol;

namespace LinkClanBot3.Discord
{
	public class SettingVoiceChannel
	{
		public List<string> TemporaryMemberRole { get; set; } = [];
		public List<string> MemberRole { get; set; } = [];
		public List<string> AdminRole { get; set; } = [];
	}

	public class DiscordEventService : BackgroundService
	{
		private readonly IServiceScopeFactory _scopeFactory;

		private ILogger<DiscordEventService> Logger { get; set; }
		private DiscordSocketClient Client { set; get; }
		private IConfigurationRoot Configuration { set; get; }

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
			Logger = loggerFactory.CreateLogger<DiscordEventService>();


			Configuration = new ConfigurationBuilder()
			   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
			   .AddUserSecrets<Program>()
			   .Build();


			var _config = new DiscordSocketConfig { 
				MessageCacheSize = 100,
			};
			_config.GatewayIntents = GatewayIntents.Guilds;
            Client = new DiscordSocketClient(_config);
		}

		private T? getConfig<T>(string name)
		{
			return Configuration.GetSection(name).Get<T>();
		}

		public override void Dispose()
		{
			Client.Dispose();
		}


		private MemberRole? GetRole(ulong DiscordId)
		{
			var LoginRoleItem = getConfig<SettingVoiceChannel>("VoiceChannle");
			if (LoginRoleItem == null) return null;

			foreach (var role_item in Client.GetGuild(Convert.ToUInt64(getConfig<string>("Guild") ?? "")).GetUser(DiscordId).Roles)
			{
				foreach (var role in LoginRoleItem.AdminRole)
				{
					if (role_item.Id == Convert.ToUInt64(role))
					{
						return MemberRole.Admin;
					}
				}

				foreach (var role in LoginRoleItem.MemberRole)
				{
					if (role_item.Id == Convert.ToUInt64(role))
					{
						return MemberRole.Member;
					}
				}

				foreach (var role in LoginRoleItem.TemporaryMemberRole)
				{
					if (role_item.Id == Convert.ToUInt64(role))
					{
						return MemberRole.TemporaryMember;
					}
				}
			}
			return MemberRole.Withdrawal;
		}

		private void SendMessage(string message)
		{
            var MessageChannelId = getConfig<string>("MessageChannel");
            var test = Client.GetChannel(Convert.ToUInt64(MessageChannelId));
            if (test is SocketTextChannel channel)
            {
                channel.SendMessageAsync(message);
            }
        }

		/// <summary>
		/// 起動時処理
		/// </summary>
		/// <param name="stoppingToken"></param>
		/// <returns></returns>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Logger.LogInformation("DicordEventService Start");
			var token = getConfig<string>("DiscordToken");
			await Client.LoginAsync(TokenType.Bot, token);
			await Client.StartAsync();

			Client.MessageUpdated += MessageUpdated;
			Client.UserVoiceStateUpdated += UserVoiceStateUpdated;
			Client.GuildMemberUpdated += OnGuildMemberUpdated;

            Client.Disconnected += (ex) =>
            {
                SendMessage("接続を切ります！ありがとうございました！");
                return Task.CompletedTask;
            };
			Client.Connected += () =>
			{
				Logger.LogInformation("DicordEventService Connected");

				return Task.CompletedTask;
			};
			Client.Ready += () =>
            {
				SendMessage("出欠確認君Botの準備が出来ました！こんにちは！");
                Console.WriteLine("Bot is Ready!");
				
				return Task.CompletedTask;
			};

			await Task.CompletedTask;
		}

		private string? SendMessageWithRoleUpdate(MemberRole oldRole, MemberRole newRole)
		{
			if (oldRole == newRole)
			{
				return null;
            }
			switch(newRole)
			{
				case MemberRole.Admin:
					return "管理者に昇格しました。称えよ！！";
				case MemberRole.Member:
					return "正隊員に昇格しました。皆にあいさつは？";
				case MemberRole.TemporaryMember:
					return "仮入隊になりました。ようこそ！";
				case MemberRole.Withdrawal:
					return "脱退しました。さような！また逢う日まで！";
				default:
					return "ロールが更新されました。";
            }
        }

		private void MemberUpdate(LinkClanBot3Context db, SocketUser user, MemberRole? role)
		{
			var member = db.Member.FirstOrDefault(e=>e.DiscordID == user.Id.ToString());

			if(role == MemberRole.Withdrawal)
			{
				return;
			}

            // メンバーが存在しない場合は、追加する
            if (member == null)
			{
                db.Member.Add(new Member()
                {
                    CallName = "",
                    Role = role ?? MemberRole.TemporaryMember,
                    OriginID = "",
                    DiscordID = user.Id.ToString(),
                    DiscordDisplayName = user.Username,
                    DiscordName = user.GlobalName,
                    SteamID = "",
                    UplayID = "",
                    BATTEL_NET_BattleTag = "",
                    epicgamesID = "",
                    PlayStationID = "",
                    XboxID = "",
                    SNS_X_UserID = ""
                });
				return;
			}

			// ロールが変わった場合は、DBに保存する
			if (role.HasValue)
			{
				member.Role = role.Value;
			}
            db.Member.Update(member);
        }

        private Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> arg1, SocketGuildUser arg2)
		{
			if (!arg1.HasValue || arg1.Value.IsBot)
			{
				return Task.CompletedTask;
            }
			var oldRole = MemberRole.Withdrawal;
			var newRole = MemberRole.Withdrawal;
            foreach (var role in arg1.Value.Roles)
			{
				var sysRole = GetRole(role.Id);
				if(sysRole == null || sysRole.Value == MemberRole.Withdrawal)
				{
					continue;
				}
				oldRole = sysRole.Value;
				break;
            }
            foreach (var role in arg2.Roles)
            {
                var sysRole = GetRole(role.Id);
                if (sysRole == null || sysRole.Value == MemberRole.Withdrawal)
                {
                    continue;
                }
                newRole = sysRole.Value;
                break;
            }

			// ロールが変わった場合は、DBに保存する

			using (var scope = _scopeFactory.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<LinkClanBot3Context>();
                MemberUpdate(dbContext, arg2, newRole);
            }

			var message = SendMessageWithRoleUpdate(oldRole, newRole);
			if (message != null)
			{
				SendMessage(arg2.DisplayName + "さんが、" + message);
			}

            return Task.CompletedTask;
        }

        private Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
		{
			using (var scope = _scopeFactory.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<LinkClanBot3Context>();

				var userRole = GetRole(arg1.Id);
				if (userRole == null || userRole.Value == MemberRole.Withdrawal)
				{
					return Task.CompletedTask;
				}

				var user = dbContext.Member.FirstOrDefault(x => x.DiscordID == arg1.Id.ToString());
				if(user == null)
				{
					user = dbContext.Member.Add(new Member()
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

                    dbContext.SaveChanges();
				}

				// 入室
				if (arg2.VoiceChannel == null)
				{
					Console.WriteLine($"入室 , {user.DiscordDisplayName}");
                    dbContext.MemberTimeLine.Add(new MemberTimeLine
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
                    dbContext.MemberTimeLine.Add(new MemberTimeLine
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
                    dbContext.MemberTimeLine.Add(new MemberTimeLine
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
                dbContext.SaveChanges();
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
