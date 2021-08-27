using Microsoft.EntityFrameworkCore;

namespace BrotherQlMqttHub.Data
{
    public class PrinterContext : DbContext
    {   
        public PrinterContext(DbContextOptions<PrinterContext> options) : base(options) {}

        public DbSet<Printer> Printers { get; set; }

        public DbSet<TagCategory> Categories { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public DbSet<PrinterTag> PrinterTags { get; set; }

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