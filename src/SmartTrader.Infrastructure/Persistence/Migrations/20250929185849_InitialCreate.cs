using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTrader.Infrastructure.src.SmartTrader.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "market");

            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.CreateTable(
                name: "candles_1m",
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
                    table.PrimaryKey("PK_candles_1m", x => new { x.SymbolId, x.ts_open });
                });

            migrationBuilder.CreateTable(
                name: "indicators_cache",
                schema: "market",
                columns: table => new
                {
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timeframe = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    candle_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Values = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_indicators_cache", x => new { x.SymbolId, x.Timeframe, x.Name, x.candle_ts });
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "signals",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timeframe = table.Column<string>(type: "text", nullable: false),
                    Strategy = table.Column<string>(type: "text", nullable: false),
                    candle_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Side = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(38,10)", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "symbols",
                schema: "market",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BaseAsset = table.Column<string>(type: "text", nullable: false),
                    QuoteAsset = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_symbols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timeframe = table.Column<string>(type: "text", nullable: false),
                    Strategy = table.Column<string>(type: "text", nullable: false),
                    @params = table.Column<string>(name: "params", type: "jsonb", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "core",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_candles_1m_symbol_tsopen_desc",
                schema: "market",
                table: "candles_1m",
                columns: new[] { "SymbolId", "ts_open" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_status_created_at",
                schema: "core",
                table: "outbox",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_signals_SymbolId_Timeframe_Strategy_candle_ts",
                schema: "core",
                table: "signals",
                columns: new[] { "SymbolId", "Timeframe", "Strategy", "candle_ts" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId_SymbolId_Timeframe_Strategy",
                schema: "core",
                table: "subscriptions",
                columns: new[] { "UserId", "SymbolId", "Timeframe", "Strategy" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_symbols_Name",
                schema: "market",
                table: "symbols",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_ChatId",
                schema: "core",
                table: "users",
                column: "ChatId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candles_1m",
                schema: "market");

            migrationBuilder.DropTable(
                name: "indicators_cache",
                schema: "market");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "core");

            migrationBuilder.DropTable(
                name: "signals",
                schema: "core");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "symbols",
                schema: "market");

            migrationBuilder.DropTable(
                name: "users",
                schema: "core");
        }
    }
}
