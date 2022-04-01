using Microsoft.EntityFrameworkCore;

namespace BrotherQlHub.Data
{
    public class HubContext : DbContext
    {   
        public HubContext(DbContextOptions<HubContext> options) : base(options) {}

        public DbSet<Printer> Printers { get; set; } = null!;

        public DbSet<TagCategory> Categories { get; set; } = null!;

        public DbSet<Tag> Tags { get; set; } = null!;

        public DbSet<PrinterTag> PrinterTags { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TagCategory>()
                .HasMany(c => c.Tags)
                .WithOne(t => t.Category);

            modelBuilder.Entity<PrinterTag>()
                .HasIndex(pt => new {pt.PrinterSerial, pt.TagCategoryId})
                .IsUnique();

            modelBuilder.Entity<Printer>()
                .HasMany(p => p.Tags)
                .WithOne();

        }
    }
}