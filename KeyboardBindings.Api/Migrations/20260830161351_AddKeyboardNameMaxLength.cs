using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeyboardBindings.Api.Migrations;

/// <inheritdoc />
public partial class AddKeyboardNameMaxLength : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "CK_KeyMappings_KeyboardName_MaxLength",
            table: "KeyMappings",
            sql: "length(\"KeyboardName\") <= 100");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_KeyMappings_KeyboardName_MaxLength",
            table: "KeyMappings");
    }
}
