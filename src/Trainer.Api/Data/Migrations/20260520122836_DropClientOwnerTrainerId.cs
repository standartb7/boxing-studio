using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trainer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropClientOwnerTrainerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_OwnerTrainerId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "OwnerTrainerId",
                table: "Clients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerTrainerId",
                table: "Clients",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Clients_OwnerTrainerId",
                table: "Clients",
                column: "OwnerTrainerId");
        }
    }
}
