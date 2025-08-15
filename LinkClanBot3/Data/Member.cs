using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkClanBot3.Data
{

    public enum MemberRole
    {
        // 管理者
        Admin,
		// 一般メンバー
        Member,
		// 仮入隊
        TemporaryMember,
        // 脱退者
        Withdrawal
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

		public virtual List<MemberTimeLine>? MemberTimeLine { get; set; }
	}
}
