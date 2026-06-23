using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using VideoGen.Infrastructure.Persistence;
#nullable disable
namespace VideoGen.Infrastructure.Persistence.Migrations;
[DbContext(typeof(VideoGenDbContext))]
partial class VideoGenDbContextModelSnapshot : ModelSnapshot
{
 protected override void BuildModel(ModelBuilder modelBuilder) { modelBuilder.HasAnnotation("ProductVersion", "8.0.11").HasAnnotation("Relational:MaxIdentifierLength", 63); modelBuilder.Entity("VideoGen.Domain.Entities.VideoJob", b => { b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid"); b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone"); b.Property<string>("ErrorMessage").HasColumnType("text"); b.Property<string>("NegativePrompt").HasColumnType("text"); b.Property<string>("OutputVideoPath").HasColumnType("text"); b.Property<string>("PositivePrompt").HasColumnType("text"); b.Property<string>("ProductImagePath").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)"); b.Property<string>("ReferenceImagePath").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)"); b.Property<string>("Status").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)"); b.Property<string>("Style").IsRequired().HasColumnType("text"); b.Property<DateTime>("UpdatedAt").HasColumnType("timestamp with time zone"); b.Property<string>("UserDescription").IsRequired().HasColumnType("text"); b.HasKey("Id"); b.HasIndex("Status", "CreatedAt"); b.ToTable("VideoJobs"); }); }
}
