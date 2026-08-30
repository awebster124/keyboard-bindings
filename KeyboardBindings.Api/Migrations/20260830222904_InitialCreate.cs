using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KeyboardBindings.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeyMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KeyboardName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PhysicalCode = table.Column<byte>(type: "INTEGER", nullable: false),
                    MappedCode = table.Column<byte>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyMappings", x => x.Id);
                    table.CheckConstraint("CK_KeyMappings_KeyboardName_MaxLength", "length(\"KeyboardName\") <= 100");
                });

            migrationBuilder.InsertData(
                table: "KeyMappings",
                columns: new[] { "Id", "KeyboardName", "MappedCode", "PhysicalCode", "Version" },
                values: new object[,]
                {
                    { 5, "Apex Pro Gen 3", (byte)4, (byte)4, new Guid("00000000-0000-0000-0000-000000000004") },
                    { 6, "Apex Pro Gen 3", (byte)5, (byte)5, new Guid("00000000-0000-0000-0000-000000000005") },
                    { 7, "Apex Pro Gen 3", (byte)6, (byte)6, new Guid("00000000-0000-0000-0000-000000000006") },
                    { 8, "Apex Pro Gen 3", (byte)7, (byte)7, new Guid("00000000-0000-0000-0000-000000000007") },
                    { 9, "Apex Pro Gen 3", (byte)8, (byte)8, new Guid("00000000-0000-0000-0000-000000000008") },
                    { 10, "Apex Pro Gen 3", (byte)9, (byte)9, new Guid("00000000-0000-0000-0000-000000000009") },
                    { 11, "Apex Pro Gen 3", (byte)10, (byte)10, new Guid("00000000-0000-0000-0000-00000000000a") },
                    { 12, "Apex Pro Gen 3", (byte)11, (byte)11, new Guid("00000000-0000-0000-0000-00000000000b") },
                    { 13, "Apex Pro Gen 3", (byte)12, (byte)12, new Guid("00000000-0000-0000-0000-00000000000c") },
                    { 14, "Apex Pro Gen 3", (byte)13, (byte)13, new Guid("00000000-0000-0000-0000-00000000000d") },
                    { 15, "Apex Pro Gen 3", (byte)14, (byte)14, new Guid("00000000-0000-0000-0000-00000000000e") },
                    { 16, "Apex Pro Gen 3", (byte)15, (byte)15, new Guid("00000000-0000-0000-0000-00000000000f") },
                    { 17, "Apex Pro Gen 3", (byte)16, (byte)16, new Guid("00000000-0000-0000-0000-000000000010") },
                    { 18, "Apex Pro Gen 3", (byte)17, (byte)17, new Guid("00000000-0000-0000-0000-000000000011") },
                    { 19, "Apex Pro Gen 3", (byte)18, (byte)18, new Guid("00000000-0000-0000-0000-000000000012") },
                    { 20, "Apex Pro Gen 3", (byte)19, (byte)19, new Guid("00000000-0000-0000-0000-000000000013") },
                    { 21, "Apex Pro Gen 3", (byte)20, (byte)20, new Guid("00000000-0000-0000-0000-000000000014") },
                    { 22, "Apex Pro Gen 3", (byte)21, (byte)21, new Guid("00000000-0000-0000-0000-000000000015") },
                    { 23, "Apex Pro Gen 3", (byte)22, (byte)22, new Guid("00000000-0000-0000-0000-000000000016") },
                    { 24, "Apex Pro Gen 3", (byte)23, (byte)23, new Guid("00000000-0000-0000-0000-000000000017") },
                    { 25, "Apex Pro Gen 3", (byte)24, (byte)24, new Guid("00000000-0000-0000-0000-000000000018") },
                    { 26, "Apex Pro Gen 3", (byte)25, (byte)25, new Guid("00000000-0000-0000-0000-000000000019") },
                    { 27, "Apex Pro Gen 3", (byte)26, (byte)26, new Guid("00000000-0000-0000-0000-00000000001a") },
                    { 28, "Apex Pro Gen 3", (byte)27, (byte)27, new Guid("00000000-0000-0000-0000-00000000001b") },
                    { 29, "Apex Pro Gen 3", (byte)28, (byte)28, new Guid("00000000-0000-0000-0000-00000000001c") },
                    { 30, "Apex Pro Gen 3", (byte)29, (byte)29, new Guid("00000000-0000-0000-0000-00000000001d") },
                    { 31, "Apex Pro Gen 3", (byte)30, (byte)30, new Guid("00000000-0000-0000-0000-00000000001e") },
                    { 32, "Apex Pro Gen 3", (byte)31, (byte)31, new Guid("00000000-0000-0000-0000-00000000001f") },
                    { 33, "Apex Pro Gen 3", (byte)32, (byte)32, new Guid("00000000-0000-0000-0000-000000000020") },
                    { 34, "Apex Pro Gen 3", (byte)33, (byte)33, new Guid("00000000-0000-0000-0000-000000000021") },
                    { 35, "Apex Pro Gen 3", (byte)34, (byte)34, new Guid("00000000-0000-0000-0000-000000000022") },
                    { 36, "Apex Pro Gen 3", (byte)35, (byte)35, new Guid("00000000-0000-0000-0000-000000000023") },
                    { 37, "Apex Pro Gen 3", (byte)36, (byte)36, new Guid("00000000-0000-0000-0000-000000000024") },
                    { 38, "Apex Pro Gen 3", (byte)37, (byte)37, new Guid("00000000-0000-0000-0000-000000000025") },
                    { 39, "Apex Pro Gen 3", (byte)38, (byte)38, new Guid("00000000-0000-0000-0000-000000000026") },
                    { 40, "Apex Pro Gen 3", (byte)39, (byte)39, new Guid("00000000-0000-0000-0000-000000000027") },
                    { 41, "Apex Pro Gen 3", (byte)40, (byte)40, new Guid("00000000-0000-0000-0000-000000000028") },
                    { 42, "Apex Pro Gen 3", (byte)41, (byte)41, new Guid("00000000-0000-0000-0000-000000000029") },
                    { 43, "Apex Pro Gen 3", (byte)42, (byte)42, new Guid("00000000-0000-0000-0000-00000000002a") },
                    { 44, "Apex Pro Gen 3", (byte)43, (byte)43, new Guid("00000000-0000-0000-0000-00000000002b") },
                    { 45, "Apex Pro Gen 3", (byte)44, (byte)44, new Guid("00000000-0000-0000-0000-00000000002c") },
                    { 58, "Apex Pro Gen 3", (byte)57, (byte)57, new Guid("00000000-0000-0000-0000-000000000039") },
                    { 59, "Apex Pro Gen 3", (byte)58, (byte)58, new Guid("00000000-0000-0000-0000-00000000003a") },
                    { 60, "Apex Pro Gen 3", (byte)59, (byte)59, new Guid("00000000-0000-0000-0000-00000000003b") },
                    { 61, "Apex Pro Gen 3", (byte)60, (byte)60, new Guid("00000000-0000-0000-0000-00000000003c") },
                    { 62, "Apex Pro Gen 3", (byte)61, (byte)61, new Guid("00000000-0000-0000-0000-00000000003d") },
                    { 63, "Apex Pro Gen 3", (byte)62, (byte)62, new Guid("00000000-0000-0000-0000-00000000003e") },
                    { 64, "Apex Pro Gen 3", (byte)63, (byte)63, new Guid("00000000-0000-0000-0000-00000000003f") },
                    { 65, "Apex Pro Gen 3", (byte)64, (byte)64, new Guid("00000000-0000-0000-0000-000000000040") },
                    { 66, "Apex Pro Gen 3", (byte)65, (byte)65, new Guid("00000000-0000-0000-0000-000000000041") },
                    { 67, "Apex Pro Gen 3", (byte)66, (byte)66, new Guid("00000000-0000-0000-0000-000000000042") },
                    { 68, "Apex Pro Gen 3", (byte)67, (byte)67, new Guid("00000000-0000-0000-0000-000000000043") },
                    { 69, "Apex Pro Gen 3", (byte)68, (byte)68, new Guid("00000000-0000-0000-0000-000000000044") },
                    { 70, "Apex Pro Gen 3", (byte)69, (byte)69, new Guid("00000000-0000-0000-0000-000000000045") },
                    { 71, "Apex Pro Gen 3", (byte)70, (byte)70, new Guid("00000000-0000-0000-0000-000000000046") },
                    { 72, "Apex Pro Gen 3", (byte)71, (byte)71, new Guid("00000000-0000-0000-0000-000000000047") },
                    { 73, "Apex Pro Gen 3", (byte)72, (byte)72, new Guid("00000000-0000-0000-0000-000000000048") },
                    { 74, "Apex Pro Gen 3", (byte)73, (byte)73, new Guid("00000000-0000-0000-0000-000000000049") },
                    { 75, "Apex Pro Gen 3", (byte)74, (byte)74, new Guid("00000000-0000-0000-0000-00000000004a") },
                    { 76, "Apex Pro Gen 3", (byte)75, (byte)75, new Guid("00000000-0000-0000-0000-00000000004b") },
                    { 77, "Apex Pro Gen 3", (byte)76, (byte)76, new Guid("00000000-0000-0000-0000-00000000004c") },
                    { 78, "Apex Pro Gen 3", (byte)77, (byte)77, new Guid("00000000-0000-0000-0000-00000000004d") },
                    { 79, "Apex Pro Gen 3", (byte)78, (byte)78, new Guid("00000000-0000-0000-0000-00000000004e") },
                    { 80, "Apex Pro Gen 3", (byte)79, (byte)79, new Guid("00000000-0000-0000-0000-00000000004f") },
                    { 81, "Apex Pro Gen 3", (byte)80, (byte)80, new Guid("00000000-0000-0000-0000-000000000050") },
                    { 82, "Apex Pro Gen 3", (byte)81, (byte)81, new Guid("00000000-0000-0000-0000-000000000051") },
                    { 83, "Apex Pro Gen 3", (byte)82, (byte)82, new Guid("00000000-0000-0000-0000-000000000052") },
                    { 84, "Apex Pro Gen 3", (byte)83, (byte)83, new Guid("00000000-0000-0000-0000-000000000053") },
                    { 85, "Apex Pro Gen 3", (byte)84, (byte)84, new Guid("00000000-0000-0000-0000-000000000054") },
                    { 86, "Apex Pro Gen 3", (byte)85, (byte)85, new Guid("00000000-0000-0000-0000-000000000055") },
                    { 87, "Apex Pro Gen 3", (byte)86, (byte)86, new Guid("00000000-0000-0000-0000-000000000056") },
                    { 88, "Apex Pro Gen 3", (byte)87, (byte)87, new Guid("00000000-0000-0000-0000-000000000057") },
                    { 89, "Apex Pro Gen 3", (byte)88, (byte)88, new Guid("00000000-0000-0000-0000-000000000058") },
                    { 90, "Apex Pro Gen 3", (byte)89, (byte)89, new Guid("00000000-0000-0000-0000-000000000059") },
                    { 91, "Apex Pro Gen 3", (byte)90, (byte)90, new Guid("00000000-0000-0000-0000-00000000005a") },
                    { 92, "Apex Pro Gen 3", (byte)91, (byte)91, new Guid("00000000-0000-0000-0000-00000000005b") },
                    { 93, "Apex Pro Gen 3", (byte)92, (byte)92, new Guid("00000000-0000-0000-0000-00000000005c") },
                    { 94, "Apex Pro Gen 3", (byte)93, (byte)93, new Guid("00000000-0000-0000-0000-00000000005d") },
                    { 95, "Apex Pro Gen 3", (byte)94, (byte)94, new Guid("00000000-0000-0000-0000-00000000005e") },
                    { 96, "Apex Pro Gen 3", (byte)95, (byte)95, new Guid("00000000-0000-0000-0000-00000000005f") },
                    { 97, "Apex Pro Gen 3", (byte)96, (byte)96, new Guid("00000000-0000-0000-0000-000000000060") },
                    { 98, "Apex Pro Gen 3", (byte)97, (byte)97, new Guid("00000000-0000-0000-0000-000000000061") },
                    { 99, "Apex Pro Gen 3", (byte)98, (byte)98, new Guid("00000000-0000-0000-0000-000000000062") },
                    { 100, "Apex Pro Gen 3", (byte)99, (byte)99, new Guid("00000000-0000-0000-0000-000000000063") },
                    { 225, "Apex Pro Gen 3", (byte)224, (byte)224, new Guid("00000000-0000-0000-0000-0000000000e0") },
                    { 226, "Apex Pro Gen 3", (byte)225, (byte)225, new Guid("00000000-0000-0000-0000-0000000000e1") },
                    { 227, "Apex Pro Gen 3", (byte)226, (byte)226, new Guid("00000000-0000-0000-0000-0000000000e2") },
                    { 228, "Apex Pro Gen 3", (byte)227, (byte)227, new Guid("00000000-0000-0000-0000-0000000000e3") },
                    { 229, "Apex Pro Gen 3", (byte)228, (byte)228, new Guid("00000000-0000-0000-0000-0000000000e4") },
                    { 230, "Apex Pro Gen 3", (byte)229, (byte)229, new Guid("00000000-0000-0000-0000-0000000000e5") },
                    { 231, "Apex Pro Gen 3", (byte)230, (byte)230, new Guid("00000000-0000-0000-0000-0000000000e6") },
                    { 232, "Apex Pro Gen 3", (byte)231, (byte)231, new Guid("00000000-0000-0000-0000-0000000000e7") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeyMappings_KeyboardName_PhysicalCode",
                table: "KeyMappings",
                columns: new[] { "KeyboardName", "PhysicalCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyMappings");
        }
    }
}
