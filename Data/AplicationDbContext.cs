using Microsoft.AspNetCore.Identity; // IdentityUser uchun qo‘shildi
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<Review> Reviews { get; set; }
    // public IEnumerable<object> Images { get; internal set; }
    public DbSet<Image> Images { get; set; } 
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
}
