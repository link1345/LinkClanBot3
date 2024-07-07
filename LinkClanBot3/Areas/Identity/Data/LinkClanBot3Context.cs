using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinkClanBot3.Data;

public class LinkClanBot3Context : IdentityDbContext<IdentityUser>
{
    public LinkClanBot3Context(DbContextOptions<LinkClanBot3Context> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<MemberTimeLine>(e => { 
            e.HasOne(m => m.MemberData).WithMany(m => m.MemberTimeLine).HasForeignKey(m => m.MemberID).OnDelete(DeleteBehavior.SetNull);        
        });
	}


	public DbSet<Member> Member { get; set; }
	public DbSet<MemberTimeLine> MemberTimeLine { get; set; }
}
