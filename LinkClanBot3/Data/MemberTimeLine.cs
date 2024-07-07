using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkClanBot3.Data
{
	public enum EnteringRoom
	{
		Entry = 0,
		Exit = 1,
		Move = 2,
	}

    public class MemberTimeLine
    {

		[Key]
		[MaxLength(450)]
		[Comment("[PK] TimeLineID")]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public string TimeLineID { get; set; }

		[Required]
		[Comment("MemberID")]
		[ForeignKey("Member")]
		[MaxLength(450)]
		public string MemberID { get; set; }
		public virtual Member MemberData { get; set; }

		[Required]
		public EnteringRoom EnteringRoom { set; get; }

		public string? before_channel_id { set; get; }

		public string? before_channel_name { set; get; }

		public string? after_channel_id { set; get; }

		public string? after_channel_name { set; get; }


		[Required]
		public DateTime EventDate { get; set; }
	}
}
