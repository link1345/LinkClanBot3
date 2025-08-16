using LinkClanBot3.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System.Net;

namespace LinkClanBot3.Data
{
	public class ViewMember
	{
		public string 名前 { get; set; } = "";

		public string 役職 { get; set; } = "";

		public string OriginID { get; set; } = "";

		public string Xアカウント { get; set; } = "";
	}

	[ApiController]
	[Route("api/[controller]")]
	public class MemberController : Controller
	{
        private readonly LinkClanBot3Context dbContext;
        private readonly ILogger<MemberController> Logger;

        public MemberController(LinkClanBot3Context db, ILogger<MemberController> logger)
        {
            dbContext = db;
            Logger = logger;
        }
        private string getRoleString(MemberRole role)
		{
			switch (role)
			{
				case MemberRole.Admin:
					return "代表";
				case MemberRole.Leader:
					return "幹部";
                case MemberRole.Member:
					return "";
				case MemberRole.TemporaryMember:
					return "仮入隊";
				case MemberRole.Withdrawal:
					return "脱退者";
				default:
					return "脱退者";
			}
		}

		[HttpGet]
		/// <summary>
		/// ユーザを取得
		/// </summary>
		/// <returns></returns>
		public IEnumerable<ViewMember> Index()
		{
			return dbContext.Member
				.Where(m => m.Role != MemberRole.Withdrawal)
				.OrderBy(e=>(int)e.Role)
				.ToList()
				.Select(e=>new ViewMember() {
					名前 = e.DiscordName,
					役職 = getRoleString(e.Role),
					OriginID = e.OriginID ?? "",
					Xアカウント = e.SNS_X_UserID ?? ""
				}).ToList();
		}        
	}

}
