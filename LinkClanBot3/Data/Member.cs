using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkClanBot3.Data
{

    public enum MemberRole : int
    {
        // 管理者
        Admin = 0,
		// 幹部
        Leader = 1,
        // 一般メンバー
        Member = 2,
		// 仮入隊
        TemporaryMember = 3,
        // 脱退者
        Withdrawal = 4
    }

    public class Member
    {
		public Member()
		{
			DiscordID = "";
			DiscordDisplayName = "";
			DiscordName = "";
		}

		[Key]
		[MaxLength(450)]
		[Comment("[PK] MemberID")]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public string MemberID { get; set; }

		[MaxLength(450)]
		public string? CallName { get; set; }
		[MaxLength(450)]
		public MemberRole Role { get; set; }
		[MaxLength(450)]
		public string? OriginID { get; set; }
		[Required]
		[MaxLength(450)]
		public string DiscordID { get; set; }
		[Required]
		[MaxLength(450)]
		public string DiscordDisplayName { get; set; }
		[Required]
		[MaxLength(450)]
		public string DiscordName { get; set; }
		[MaxLength(450)]
		public string? SteamID { get; set; }
		[MaxLength(450)]
		public string? UplayID { get; set; }
		[MaxLength(450)]
		public string? BATTEL_NET_BattleTag { get; set; }
		[MaxLength(450)]
		public string? epicgamesID { get; set; }
		[MaxLength(450)]
		public string? PlayStationID { get; set; }
		[MaxLength(450)]
		public string? XboxID { get; set; }
		[MaxLength(450)]
		public string? SNS_X_UserID { get; set; }

		public DateTime RoleChangedDate { get; set; } = DateTime.UtcNow;

        public string GetRoleString()
        {
            switch (this.Role)
            {
                case MemberRole.Admin:
                    return "代表";
				case MemberRole.Leader:
					return "幹部";
                case MemberRole.Member:
                    return "正隊員";
                case MemberRole.TemporaryMember:
                    return "仮隊員";
                case MemberRole.Withdrawal:
                    return "除隊";
            }
            return "";
        }


		public static int MaxDisplayDay = 30;

		/// <summary>
		/// 最後にJoinしてからの経過日数を返す
		/// </summary>
		/// <returns></returns>
		public int ElapsedDays()
		{
			// 1度も入室履歴がない場合は、ステータスが変更された日からの経過日数を返す
			var lastDays = (DateTime.UtcNow - this.RoleChangedDate).Days;
			if (this.MemberTimeLine == null || this.MemberTimeLine.Count == 0)
			{
				return MaxDisplayDay;
			}

			// MAXDisplayDay以上に経過している場合は、MaxDisplayDayを返す
			var lastJoinWithMaxDays = this.MemberTimeLine.Where(e=>e.EventDate >= DateTime.UtcNow.AddDays(-MaxDisplayDay));
			if (lastJoinWithMaxDays == null)
			{
				return MaxDisplayDay;
			}
			// 最後にJoinした履歴を取得する
			var lastJoin = lastJoinWithMaxDays.OrderByDescending(e => e.EventDate).FirstOrDefault(e => e.EnteringRoom == EnteringRoom.Entry);
			if (lastJoin == null)
			{
				return MaxDisplayDay;
			}

			// MaxDisplayDays以上の日にちが経った場合は、MaxDisplayDaysを返す
			var days = Math.Min((int)(DateTime.UtcNow - lastJoin.EventDate).TotalDays , MaxDisplayDay);
			return days;
		}

		public double GetTotalJoinTime()
		{
			if (this.MemberTimeLine == null || this.MemberTimeLine.Count == 0)
			{
				return 0.0;
			}
			var timeLines = this.MemberTimeLine
				.OrderBy(e => e.EventDate)
				.Take(10000)
				.ToList();
			double sum = 0.0;
			var startIndex = 0;
			foreach (var line in timeLines.Select((value, index) => new { index, value }))
			{
				if (line.value.EnteringRoom == EnteringRoom.Exit)
				{
					var startItem = timeLines.Skip(startIndex).Take(line.index - startIndex).FirstOrDefault(e => e.EnteringRoom == EnteringRoom.Entry);
					if (startItem != null)
					{
						// 退出があれば、退出時間と入室時間の差分を計算する
						sum += (line.value.EventDate - startItem.EventDate).TotalHours;
						startIndex = line.index;
					}
				}
			}
			return Math.Round(sum, 2);
		}

		/// <summary>
		/// 今月の参加時間を返す
		/// </summary>
		/// <param name="MonthsAgo">何か月前か</param>
		/// <returns></returns>
		public double GetJoinTime(int MonthsAgo)
		{
			DateTime dtToday = DateTime.Today.AddMonths(MonthsAgo);
			var from = new DateTime(dtToday.Year, dtToday.Month, 1);
			var to = new DateTime(dtToday.Year, dtToday.Month,
				DateTime.DaysInMonth(dtToday.Year, dtToday.Month), 12, 59, 59);

			if(this.MemberTimeLine == null || this.MemberTimeLine.Count == 0)
			{
				return 0.0;
			}

			var timeLines = this.MemberTimeLine
				.Where(e => from <= e.EventDate && to >= e.EventDate)
				.OrderBy(e => e.EventDate)
				.Take(10000)
				.ToList();

			double sum = 0.0;
			var startIndex = 0;
			foreach (var line in timeLines.Select((value, index) => new { index, value }))
			{
				if (line.value.EnteringRoom == EnteringRoom.Exit)
				{
					var startItem = timeLines.Skip(startIndex).Take(line.index - startIndex).FirstOrDefault(e => e.EnteringRoom == EnteringRoom.Entry);
					if (startItem != null)
					{
						// 退出があれば、退出時間と入室時間の差分を計算する
						sum += (line.value.EventDate - startItem.EventDate).TotalHours;
						startIndex = line.index;
					}
				}
			}
			return Math.Round(sum, 2);
		}

		public virtual List<MemberTimeLine>? MemberTimeLine { get; set; }
	}
}
