using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
#nullable disable
namespace VideoGen.Infrastructure.Persistence.Migrations;
[DbContext(typeof(VideoGenDbContext))]
[Migration("202606230001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "VideoJobs", columns: table => new { Id = table.Column<Guid>(nullable: false), ProductImagePath = table.Column<string>(maxLength: 500, nullable: false), ReferenceImagePath = table.Column<string>(maxLength: 500, nullable: false), UserDescription = table.Column<string>(nullable: false), Style = table.Column<string>(nullable: false), PositivePrompt = table.Column<string>(type: "text", nullable: true), NegativePrompt = table.Column<string>(type: "text", nullable: true), Status = table.Column<string>(maxLength: 32, nullable: false), OutputVideoPath = table.Column<string>(nullable: true), ErrorMessage = table.Column<string>(nullable: true), CreatedAt = table.Column<DateTime>(nullable: false), UpdatedAt = table.Column<DateTime>(nullable: false) }, constraints: table => table.PrimaryKey("PK_VideoJobs", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_VideoJobs_Status_CreatedAt", table: "VideoJobs", columns: new[] { "Status", "CreatedAt" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "VideoJobs");
}
