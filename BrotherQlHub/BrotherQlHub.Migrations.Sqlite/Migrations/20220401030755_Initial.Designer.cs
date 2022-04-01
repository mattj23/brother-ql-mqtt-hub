﻿// <auto-generated />
using System;
using BrotherQlHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BrotherQlHub.Migrations.Sqlite.Migrations
{
    [DbContext(typeof(HubContext))]
    [Migration("20220401030755_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.3");

            modelBuilder.Entity("BrotherQlHub.Data.Printer", b =>
                {
                    b.Property<string>("Serial")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastSeen")
                        .HasColumnType("TEXT");

                    b.Property<string>("Model")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Serial");

                    b.ToTable("Printers");
                });

            modelBuilder.Entity("BrotherQlHub.Data.PrinterTag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("PrinterSerial")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("TagCategoryId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TagId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("PrinterSerial", "TagCategoryId")
                        .IsUnique();

                    b.ToTable("PrinterTags");
                });

            modelBuilder.Entity("BrotherQlHub.Data.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CategoryId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("BrotherQlHub.Data.TagCategory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

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