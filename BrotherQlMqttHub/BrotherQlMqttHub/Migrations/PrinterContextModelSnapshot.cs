﻿// <auto-generated />
using System;
using BrotherQlMqttHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BrotherQlMqttHub.Migrations
{
    [DbContext(typeof(PrinterContext))]
    partial class PrinterContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.9");

            modelBuilder.Entity("BrotherQlMqttHub.Data.Printer", b =>
                {
                    b.Property<string>("Serial")
                        .HasColumnType("varchar(255)");

                    b.Property<DateTime?>("LastSeen")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Model")
                        .HasColumnType("longtext");

                    b.HasKey("Serial");

                    b.ToTable("Printers");
                });

            modelBuilder.Entity("BrotherQlMqttHub.Data.PrinterTag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("PrinterSerial")
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

            modelBuilder.Entity("BrotherQlMqttHub.Data.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("CategoryId")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("BrotherQlMqttHub.Data.TagCategory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("BrotherQlMqttHub.Data.PrinterTag", b =>
                {
                    b.HasOne("BrotherQlMqttHub.Data.Printer", null)
                        .WithMany("Tags")
                        .HasForeignKey("PrinterSerial");
                });

            modelBuilder.Entity("BrotherQlMqttHub.Data.Tag", b =>
                {
                    b.HasOne("BrotherQlMqttHub.Data.TagCategory", "Category")
                        .WithMany("Tags")
                        .HasForeignKey("CategoryId");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("BrotherQlMqttHub.Data.Printer", b =>
                {
                    b.Navigation("Tags");
                });

            modelBuilder.Entity("BrotherQlMqttHub.Data.TagCategory", b =>
                {
                    b.Navigation("Tags");
                });
#pragma warning restore 612, 618
        }
    }
}
