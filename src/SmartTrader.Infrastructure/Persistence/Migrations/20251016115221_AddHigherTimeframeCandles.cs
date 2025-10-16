using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTrader.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHigherTimeframeCandles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "candles_15m",
                schema: "market",
                columns: table => new
                {
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ts_open = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(38,10)", nullable: false),
                    High = table.Column<decimal>(type: "numeric(38,10)", nullable: false),
                    Low = table.Column<decimal>(type: "numeric(38,10)", nullable: false),
                    Close = table.Column<decimal>(type: "numeric(38,10)", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(38,10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candles_15m", x => new { x.SymbolId, x.ts_open });
                });

            migrationBuilder.CreateTable(
                name: "candles_5m",
                schema: "market",
                columns: table => new
                {
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ts_open = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(38,10)", nullable: false),
                    High = table.Column<decimal>(type: "numeric(38,10)", nullable: false),
                    Low = table.Column<decimal>(type: "numeric(38,10)", nullable: false),
                    Close = table.Column<decimal>(type: "numeric(38,10)", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(38,10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candles_5m", x => new { x.SymbolId, x.ts_open });
                });

            migrationBuilder.CreateIndex(
                name: "ix_candles_15m_symbol_tsopen_desc",
                schema: "market",
                table: "candles_15m",
                columns: new[] { "SymbolId", "ts_open" });

            migrationBuilder.CreateIndex(
                name: "ix_candles_5m_symbol_tsopen_desc",
                schema: "market",
                table: "candles_5m",
                columns: new[] { "SymbolId", "ts_open" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candles_15m",
                schema: "market");

            migrationBuilder.DropTable(
                name: "candles_5m",
                schema: "market");
        }
    }
}
