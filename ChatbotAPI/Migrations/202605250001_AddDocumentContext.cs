using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatbotAPI.Migrations
{
    public partial class AddDocumentContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentContext",
                table: "ChatSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentName",
                table: "ChatSessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentContext",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "DocumentName",
                table: "ChatSessions");
        }
    }
}
