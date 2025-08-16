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
		public string DiscordName { get; set; } = "";

		public string Role { get; set; } = "";

		public string OriginID { get; set; } = "";

		public string Xaccount { get; set; } = "";
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
					return "��\";
				case MemberRole.Leader:
					return "����";
                case MemberRole.Member:
					return "";
				case MemberRole.TemporaryMember:
					return "������";
				case MemberRole.Withdrawal:
					return "�E�ގ�";
				default:
					return "�E�ގ�";
			}
		}

		[HttpGet]
		/// <summary>
		/// ���[�U���擾
		/// </summary>
		/// <returns></returns>
		public IEnumerable<ViewMember> Index()
		{
			return dbContext.Member
				.Where(m => m.Role != MemberRole.Withdrawal)
				.OrderBy(e=>(int)e.Role)
				.ToList()
				.Select(e=>new ViewMember() {
					DiscordName = e.DiscordName,
					Role = getRoleString(e.Role),
					OriginID = e.OriginID ?? "",
					Xaccount = e.SNS_X_UserID ?? ""
				}).ToList();
		}        
	}

}
