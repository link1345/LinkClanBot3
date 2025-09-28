using CronSTD;
using Discord;
using Discord.WebSocket;
using LinkClanBot3.Data;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace LinkClanBot3.Discord
{
	public class SettingVoiceChannel
	{
		public List<string> TemporaryMemberRole { get; set; } = [];
		public List<string> MemberRole { get; set; } = [];
		public List<string> LeaderRole { get; set; } = [];
        public List<string> AdminRole { get; set; } = [];
    }

	public class DiscordEventService : BackgroundService
	{
		private readonly IServiceScopeFactory _scopeFactory;

		private ILogger<DiscordEventService> Logger { get; set; }
		private DiscordSocketClient Client { set; get; }
		private IConfigurationRoot Configuration { set; get; }

		private readonly CronDaemon TemporaryMemberAlertCron = new CronDaemon();

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
			   .AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true)
               .Build();

			TemporaryMemberAlertCron.AddJob("0 0 * * *", TemporaryMemberAlertTask);
			TemporaryMemberAlertCron.Start();
		}

		private async void TemporaryMemberAlertTask()
		{
			foreach (var guild in Client.Guilds)
			{
				var users = await guild.GetUsersAsync().FlattenAsync();
				foreach (var user in users)
				{
					var role = GetRole(user.RoleIds);
					if (role == MemberRole.TemporaryMember)
					{
						using (var scope = _scopeFactory.CreateScope())
						{
							var dbContext = scope.ServiceProvider.GetRequiredService<LinkClanBot3Context>();
							var member = dbContext.Member.Include(e=>e.MemberTimeLine).FirstOrDefault(e => e.DiscordID == user.Id.ToString());
							if (member != null)
							{
								var updateDate = (DateTime.UtcNow - member.RoleChangedDate).TotalDays;
								if (updateDate == 14)
								{
									continue;
								}

								var elapsedDays = member.ElapsedDays();
								var totalJoinTime = member.GetTotalJoinTime();

								SendMessage($"{user.DisplayName}さん、仮入隊から{updateDate}日経過しました。正隊員への昇格をお忘れなく！(最終参加:{elapsedDays}日前、合計参加時間:{totalJoinTime}時間)");
							}
						}
					}
				}
			}
		}

		private T? getConfig<T>(string name)
		{
			return Configuration.GetSection(name).Get<T>();
		}

		public override void Dispose()
		{
			Client.Dispose();
		}

		private MemberRole GetRole(IReadOnlyCollection<ulong> roleIds)
		{
			var LoginRoleItem = getConfig<SettingVoiceChannel>("VoiceChannle");
			if (LoginRoleItem == null) return MemberRole.Withdrawal;            
			
			foreach (var role_item in roleIds)
			{
				foreach (var role in LoginRoleItem.AdminRole)
				{
					if (role_item == Convert.ToUInt64(role))
					{
						return MemberRole.Admin;
					}
				}

                foreach (var role in LoginRoleItem.LeaderRole)
                {
                    if (role_item == Convert.ToUInt64(role))
                    {
                        return MemberRole.Leader;
                    }
                }

                foreach (var role in LoginRoleItem.MemberRole)
				{
					if (role_item == Convert.ToUInt64(role))
					{
						return MemberRole.Member;
					}
				}

				foreach (var role in LoginRoleItem.TemporaryMemberRole)
				{
					if (role_item == Convert.ToUInt64(role))
					{
						return MemberRole.TemporaryMember;
					}
				}
			}
			return MemberRole.Withdrawal;
		}

		private MemberRole GetRole(ulong DiscordId)
		{
			var LoginRoleItem = getConfig<SettingVoiceChannel>("VoiceChannle");
			if (LoginRoleItem == null) return MemberRole.Withdrawal;
			var guild = Client.Guilds.FirstOrDefault(e => {
				if (e.Users.FirstOrDefault(e=>e.Id == DiscordId) != null) {
					return true;
				} 
				return false;
			});
			if( guild == null)
			{
				Logger.LogWarning($"Guild not found for DiscordId: {DiscordId}");
				return MemberRole.Withdrawal;
			}
			var user = guild.GetUser(DiscordId);
			if (user == null)
			{
				Logger.LogWarning($"User not found for DiscordId: {DiscordId}");
				return MemberRole.Withdrawal;
			}
			if (user.IsBot)
			{
                return MemberRole.Withdrawal;
            }
            foreach (var role_item in user.Roles)
			{
				foreach (var role in LoginRoleItem.AdminRole)
				{
					if (role_item.Id == Convert.ToUInt64(role))
					{
						return MemberRole.Admin;
					}
				}

                foreach (var role in LoginRoleItem.LeaderRole)
                {
                    if (role_item.Id == Convert.ToUInt64(role))
                    {
                        return MemberRole.Leader;
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

		private MemberRole GetRole(IReadOnlyCollection<SocketRole> roles)
		{
			var LoginRoleItem = getConfig<SettingVoiceChannel>("VoiceChannle");
			if (LoginRoleItem == null) return MemberRole.Withdrawal;
			foreach (var role_item in roles)
			{
				foreach (var role in LoginRoleItem.AdminRole)
				{
					if (role_item.Id == Convert.ToUInt64(role))
					{
						return MemberRole.Admin;
					}
				}

                foreach (var role in LoginRoleItem.LeaderRole)
                {
                    if (role_item.Id == Convert.ToUInt64(role))
                    {
                        return MemberRole.Leader;
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

		private async Task GlobalDelete(string commandID)
		{
			using (HttpClient client = new HttpClient())
			{
				var token = getConfig<string>("DiscordToken");
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"https://discord.com/api/v10/applications/618379894639951872/commands/{commandID}");
				request.Headers.Add("Authorization", $"Bot {token}");
				HttpResponseMessage response = await client.SendAsync(request);
				Console.WriteLine(response.Content.ReadAsStringAsync().Result);//成功すれば出力なし
			}
		}

		private async Task GuildDelete(string commandID)
		{
			using (HttpClient client = new HttpClient())
			{
				var token = getConfig<string>("DiscordToken");
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"https://discord.com/api/v10/applications/618379894639951872/guilds/1323098619452063885/commands/{commandID}");
				request.Headers.Add("Authorization", $"Bot {token}");
				HttpResponseMessage response = await client.SendAsync(request);
				Console.WriteLine(response.Content.ReadAsStringAsync().Result);//成功すれば出力なし
			}
		}

		private async Task CommandsReset()
		{
			var commands = await Client.GetGlobalApplicationCommandsAsync();
			foreach (var command in commands)
			{
				await GlobalDelete(command.Id.ToString());
			}
			foreach (var guild in Client.Guilds)
			{
				var guildCommands = guild.GetApplicationCommandsAsync();
				foreach (var command in commands)
				{
					await GuildDelete(command.Id.ToString());
				}

				//await guild.BulkOverwriteApplicationCommandAsync(applicationCommandProperties.ToArray());
			}
			using (var scope = _scopeFactory.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<LinkClanBot3Context>();
				await MembersUpdate(db);
			}
			Logger.LogInformation("Commands Reset");
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
			var _config = new DiscordSocketConfig
			{
				MessageCacheSize = 100,
			};
			_config.GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildVoiceStates;
			Client = new DiscordSocketClient(_config);
			Client.UserVoiceStateUpdated += UserVoiceStateUpdated;
			Client.GuildMemberUpdated += OnGuildMemberUpdated;
			Client.SlashCommandExecuted += OnSlashCommandExecuted;

			Client.Disconnected += async(ex) =>
			{
				foreach (var guild in Client.Guilds)
				{
					await guild.DeleteApplicationCommandsAsync();
				}
				SendMessage("接続を切ります！ありがとうございました！");
				return;
			};
			Client.Connected += () =>
			{
				Logger.LogInformation("DicordEventService Connected");

				return Task.CompletedTask;
			};
			Client.Ready += async () =>
            {
                Logger.LogInformation("Ready init run!");
                SlashCommandBuilder globalCommandHelp = new SlashCommandBuilder();
				globalCommandHelp.WithName("help");
				globalCommandHelp.WithDescription("ヘルプです。");

                SlashCommandBuilder globalCommandProfileClear = new SlashCommandBuilder();
                globalCommandProfileClear.WithName("clear-profile");
                globalCommandProfileClear.WithDescription("プロフィールに設定した内容を全て消します");

                // Slash command with name as its parameter.
                SlashCommandOptionBuilder slashCommandOptionName = new();
				slashCommandOptionName.WithName("call-name");
				slashCommandOptionName.WithType(ApplicationCommandOptionType.String);
				slashCommandOptionName.WithDescription("呼ばれたい名前");
                slashCommandOptionName.WithRequired(false);

                SlashCommandOptionBuilder slashCommandOptionSnsX = new();
				slashCommandOptionSnsX.WithName("sns-x");
				slashCommandOptionSnsX.WithType(ApplicationCommandOptionType.String);
				slashCommandOptionSnsX.WithDescription("Xアカウント(@から始まる形式で書いてください)");
				slashCommandOptionSnsX.WithRequired(false);

				SlashCommandOptionBuilder slashCommandOptionOriginID = new();
				slashCommandOptionOriginID.WithName("origin-id");
				slashCommandOptionOriginID.WithType(ApplicationCommandOptionType.String);
				slashCommandOptionOriginID.WithDescription("Origin ID");
				slashCommandOptionOriginID.WithRequired(false);

				SlashCommandOptionBuilder slashCommandOptionSteamID = new();
				slashCommandOptionSteamID.WithName("steam-id");
				slashCommandOptionSteamID.WithType(ApplicationCommandOptionType.String);
				slashCommandOptionSteamID.WithDescription("Steam ID");
				slashCommandOptionSteamID.WithRequired(false);

				SlashCommandOptionBuilder slashCommandOptionUplayID = new();
				slashCommandOptionUplayID.WithName("uplay-id");
				slashCommandOptionUplayID.WithType(ApplicationCommandOptionType.String);
				slashCommandOptionUplayID.WithDescription("Steam ID");
				slashCommandOptionUplayID.WithRequired(false);

				SlashCommandOptionBuilder slashCommandOptionBattleTag = new();
				slashCommandOptionBattleTag.WithName("battle-tag");
				slashCommandOptionBattleTag.WithType(ApplicationCommandOptionType.String);
				slashCommandOptionBattleTag.WithDescription("BATTEL.NET BattleTag");
				slashCommandOptionBattleTag.WithRequired(false);

				SlashCommandOptionBuilder slashCommandOptionEpicgamesID = new();
				slashCommandOptionEpicgamesID.WithName("epicgames-id");
				slashCommandOptionEpicgamesID.WithType(ApplicationCommandOptionType.String);
				slashCommandOptionEpicgamesID.WithDescription("Epicgames ID");
				slashCommandOptionEpicgamesID.WithRequired(false);

				SlashCommandOptionBuilder slashCommandOptionPlayStationID = new();
				slashCommandOptionPlayStationID.WithName("playstation-id");
				slashCommandOptionPlayStationID.WithType(ApplicationCommandOptionType.String);
				slashCommandOptionPlayStationID.WithDescription("PlayStation ID");
				slashCommandOptionPlayStationID.WithRequired(false);

				SlashCommandOptionBuilder slashCommandOptionXboxID = new();
				slashCommandOptionXboxID.WithName("xbox-id");
				slashCommandOptionXboxID.WithType(ApplicationCommandOptionType.String);
				slashCommandOptionXboxID.WithDescription("XBOX ID");
				slashCommandOptionXboxID.WithRequired(false);


				SlashCommandBuilder globalCommandEditProfile = new SlashCommandBuilder();
				globalCommandEditProfile.WithName("edit-profile");
				globalCommandEditProfile.WithDescription("自分のプロフィールを編集します");
				globalCommandEditProfile.AddOptions(slashCommandOptionName);
				globalCommandEditProfile.AddOptions(slashCommandOptionSnsX);
				globalCommandEditProfile.AddOptions(slashCommandOptionOriginID);
				globalCommandEditProfile.AddOptions(slashCommandOptionSteamID);
				globalCommandEditProfile.AddOptions(slashCommandOptionUplayID);
				globalCommandEditProfile.AddOptions(slashCommandOptionBattleTag);
				globalCommandEditProfile.AddOptions(slashCommandOptionEpicgamesID);
				globalCommandEditProfile.AddOptions(slashCommandOptionPlayStationID);
				globalCommandEditProfile.AddOptions(slashCommandOptionXboxID);

				//await CommandsReset();
				await Client.CreateGlobalApplicationCommandAsync(globalCommandHelp.Build());
                await Client.CreateGlobalApplicationCommandAsync(globalCommandProfileClear.Build());
                await Client.CreateGlobalApplicationCommandAsync(globalCommandEditProfile.Build());

				SendMessage("出欠確認君Botの準備が出来ました！こんにちは！");
				Logger.LogInformation("Bot is Ready!");
				return;
			};

            Logger.LogInformation($"Bot run! {token}");
            await Client.LoginAsync(TokenType.Bot, token);
			_ = Client.StartAsync();
            Logger.LogInformation("Bot run!");

            await Task.CompletedTask;
		}

		private string NowAdress()
		{
			IPAddress[] lIp = Dns.GetHostAddresses(Dns.GetHostName());

			// IPv4を抽出する必要がある
			foreach (var iIp in lIp)
			{
				if (iIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				{
					return iIp.ToString();
				}
			}
			return "";
		}

		private async Task OnSlashCommandExecuted(SocketSlashCommand command)
		{
			switch (command.CommandName)
			{
				case "help":					
					await command.RespondAsync("出欠確認君Botのヘルプです。\n" +
						$"確認ページ : http://{NowAdress()}:8080\n" +
						"`/edit-profile` - プロフィールを変更します。\n" +
						"`/help` - ヘルプを表示します。", ephemeral: true);
					break;		
				case "clear-profile":
					MemberProfileClear(command.User);
					await command.RespondAsync($"{command.User.GlobalName}さんのプロフィールをリセットしました！\n http://{NowAdress()}:8080 で確認できます。", ephemeral: true);
                    break;
                case "edit-profile": 
					var profile = new Member()
					{
						CallName = null,
                        Role = MemberRole.TemporaryMember,
						OriginID = null,
						DiscordID = "",
						DiscordDisplayName = "",
						DiscordName = "",
						SteamID = null,
                        UplayID = null,
                        BATTEL_NET_BattleTag = null,
                        epicgamesID = null,
                        PlayStationID = null,
                        XboxID = null,
                        SNS_X_UserID = null,
                    };

					foreach (var option in command.Data.Options)
					{
						switch (option.Name)
						{
							case "call-name":
								profile.CallName = option.Value?.ToString() ?? null;
                                break;
							case "sns-x":
								profile.SNS_X_UserID = option.Value?.ToString() ?? null;
                                break;
							case "origin-id":
								profile.OriginID = option.Value?.ToString() ?? null;
								break;
							case "steam-id":
								profile.SteamID = option.Value?.ToString() ?? null;
                                break;
							case "uplay-id":
								profile.UplayID = option.Value?.ToString() ?? null;
                                break;
							case "battle-tag":
								profile.BATTEL_NET_BattleTag = option.Value?.ToString() ?? null;
                                break;
							case "epicgames-id":
								profile.epicgamesID = option.Value?.ToString() ?? null;
                                break;
							case "playstation-id":
								profile.PlayStationID = option.Value?.ToString() ?? null;
                                break;
							case "xbox-id":
								profile.XboxID = option.Value?.ToString() ?? null;
                                break;
						}
					}

					MemberProfileUpdate(command.User, profile);

					await command.RespondAsync($"{command.User.GlobalName}さんのプロフィールを設定しました！\n http://{NowAdress()}:8080 で確認できます。", ephemeral:true);
					break;
			}
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
					return "代表に昇格しました。称えよ！！";
                case MemberRole.Leader:
                    return "幹部に昇格しました。称えよ！！";
                case MemberRole.Member:
					return "正隊員に昇格しました。皆にあいさつは？";
				case MemberRole.TemporaryMember:
					return "仮入隊になりました。ようこそ！";
				case MemberRole.Withdrawal:
					return "脱退しました。さようなら！また逢う日まで！";
				default:
					return "ロールが更新されました。";
			}
		}

		private async Task MembersUpdate(LinkClanBot3Context db)
		{
			foreach (var guild in Client.Guilds)
			{
				var users = await guild.GetUsersAsync().FlattenAsync();

				foreach (var user in users) 
				{
					MemberUpdate(db, user); 
				}
			}
			return;
		}

        private void MemberProfileClear(SocketUser user)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LinkClanBot3Context>();
                var dbMember = dbContext.Member.FirstOrDefault(e => e.DiscordID == user.Id.ToString());

                // メンバーが存在しない場合は、何もしない
                if (dbMember == null)
                {
                    return;
                }

                dbMember.CallName = null;
                dbMember.OriginID = null;
                dbMember.SteamID = null;
                dbMember.UplayID = null;
                dbMember.BATTEL_NET_BattleTag = null;
                dbMember.epicgamesID = null;
                dbMember.PlayStationID = null;
                dbMember.XboxID = null;
                dbMember.SNS_X_UserID = null;
                // DBに保存する
                dbContext.Member.Update(dbMember);
                dbContext.SaveChanges();
            }
        }

        private void MemberProfileUpdate(SocketUser user, Member member)
		{
			using (var scope = _scopeFactory.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<LinkClanBot3Context>();
				var dbMember = dbContext.Member.FirstOrDefault(e => e.DiscordID == user.Id.ToString());

				// メンバーが存在しない場合は、何もしない
				if (dbMember == null)
				{
					return;
				}

				dbMember.CallName = member.CallName ?? dbMember.CallName;
				dbMember.OriginID = member.OriginID ?? dbMember.OriginID;
				dbMember.SteamID = member.SteamID ?? dbMember.SteamID;
				dbMember.UplayID = member.UplayID ?? dbMember.UplayID;
				dbMember.BATTEL_NET_BattleTag = member.BATTEL_NET_BattleTag ?? dbMember.BATTEL_NET_BattleTag;
				dbMember.epicgamesID = member.epicgamesID ?? dbMember.epicgamesID;
				dbMember.PlayStationID = member.PlayStationID ?? dbMember.PlayStationID;
				dbMember.XboxID = member.XboxID ?? dbMember.XboxID;
				dbMember.SNS_X_UserID = member.SNS_X_UserID ?? dbMember.SNS_X_UserID;
				// DBに保存する
				dbContext.Member.Update(dbMember);
				dbContext.SaveChanges();
			}
		}


		private void MemberUpdate(LinkClanBot3Context db, IGuildUser user)
		{
			var role = GetRole(user.RoleIds);
			MemberUpdate(db, user, role);
		}

		private void MemberUpdate(LinkClanBot3Context db, IGuildUser user, MemberRole? role)
		{
			var member = db.Member.FirstOrDefault(e=>e.DiscordID == user.Id.ToString());

			// メンバーが存在しない場合は、追加する
			if (member == null)
			{
				db.Member.Add(new Member()
				{
					CallName = "",
					Role = role ?? MemberRole.TemporaryMember,
					OriginID = "",
					DiscordID = user.Id.ToString(),
					DiscordDisplayName = user.GlobalName ?? "",
					DiscordName = user.Nickname ?? user.DisplayName,
					SteamID = "",
					UplayID = "",
					BATTEL_NET_BattleTag = "",
					epicgamesID = "",
					PlayStationID = "",
					XboxID = "",
					SNS_X_UserID = "",
					RoleChangedDate = DateTime.UtcNow
				});
				db.SaveChanges();
				return;
			}

			// ロールが変わった場合は、DBに保存する
			if (role.HasValue)
			{
				member.Role = role.Value;
				member.RoleChangedDate = DateTime.UtcNow;
			}
			db.Member.Update(member);
			db.SaveChanges();
		}

		private Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> arg1, SocketGuildUser arg2)
		{
			if (!arg1.HasValue)
			{
				return Task.CompletedTask;
			}
			var oldRole = GetRole(arg1.Value.Roles);
			var newRole = GetRole(arg2.Roles);

			//ロールが変わった場合は、DBに保存する
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
				if (userRole == MemberRole.Withdrawal)
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
						EventDate = DateTime.UtcNow
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
						EventDate = DateTime.UtcNow
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
						EventDate = DateTime.UtcNow
					});
				}
				dbContext.SaveChanges();
			}
			return Task.CompletedTask;
		}

	}
}
