// <auto-generated />
using System;
using BrotherQlHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BrotherQlHub.Migrations.Mysql.Migrations
{
    [DbContext(typeof(HubContext))]
    partial class HubContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("BrotherQlHub.Data.Printer", b =>
                {
                    b.Property<string>("Serial")
                        .HasColumnType("varchar(255)");

                    b.Property<DateTime?>("LastSeen")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Model")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Serial");

                    b.ToTable("Printers");
                });

            modelBuilder.Entity("BrotherQlHub.Data.PrinterTag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("PrinterSerial")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("TagCategoryId")
                        .HasColumnType("int");

                    b.Property<int>("TagId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("PrinterSerial", "TagCategoryId")
                        .IsUnique();

                    b.ToTable("PrinterTags");
                });

            modelBuilder.Entity("BrotherQlHub.Data.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("BrotherQlHub.Data.TagCategory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("BrotherQlHub.Data.PrinterTag", b =>
                {
                    b.HasOne("BrotherQlHub.Data.Printer", null)
                        .WithMany("Tags")
                        .HasForeignKey("PrinterSerial")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BrotherQlHub.Data.Tag", b =>
                {
                    b.HasOne("BrotherQlHub.Data.TagCategory", "Category")
                        .WithMany("Tags")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");
                });

            modelBuilder.Entity("BrotherQlHub.Data.Printer", b =>
                {
                    b.Navigation("Tags");
                });

            modelBuilder.Entity("BrotherQlHub.Data.TagCategory", b =>
                {
                    b.Navigation("Tags");
                });
#pragma warning restore 612, 618
        }
    }
}
