using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reec.Inspection.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddLogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogAudit",
                columns: table => new
                {
                    IdLogAudit = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationName = table.Column<string>(type: "varchar(100)", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Duration = table.Column<TimeSpan>(type: "time(7)", nullable: true),
                    Protocol = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    IsHttps = table.Column<bool>(type: "bit", nullable: false),
                    Method = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Scheme = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Host = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    Port = table.Column<int>(type: "int", nullable: false),
                    HostPort = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Path = table.Column<string>(type: "varchar(max)", nullable: true),
                    QueryString = table.Column<string>(type: "varchar(2500)", maxLength: 2500, nullable: true),
                    TraceIdentifier = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    RequestHeader = table.Column<string>(type: "varchar(max)", nullable: true),
                    RequestBody = table.Column<string>(type: "varchar(max)", nullable: true),
                    ResponseHeader = table.Column<string>(type: "varchar(max)", nullable: true),
                    ResponseBody = table.Column<string>(type: "varchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CreateDateOnly = table.Column<DateOnly>(type: "Date", nullable: true),
                    CreateUser = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "DateTime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogAudit", x => x.IdLogAudit);
                });

            migrationBuilder.CreateTable(
                name: "LogEndpoint",
                columns: table => new
                {
                    IdLogEndpoint = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationName = table.Column<string>(type: "varchar(100)", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time(7)", nullable: false),
                    TraceIdentifier = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Retry = table.Column<byte>(type: "tinyint", nullable: false),
                    Method = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Schema = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Host = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    HostPort = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Path = table.Column<string>(type: "varchar(800)", maxLength: 800, nullable: true),
                    QueryString = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    RequestHeader = table.Column<string>(type: "varchar(max)", nullable: true),
                    RequestBody = table.Column<string>(type: "varchar(max)", nullable: true),
                    ResponseHeader = table.Column<string>(type: "varchar(max)", nullable: true),
                    ResponseBody = table.Column<string>(type: "varchar(max)", nullable: true),
                    CreateDateOnly = table.Column<DateOnly>(type: "Date", nullable: false),
                    CreateUser = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "DateTime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEndpoint", x => x.IdLogEndpoint);
                });

            migrationBuilder.CreateTable(
                name: "LogJob",
                columns: table => new
                {
                    IdLogJob = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationName = table.Column<string>(type: "varchar(100)", nullable: true),
                    NameJob = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    StateJob = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true),
                    TraceIdentifier = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Duration = table.Column<TimeSpan>(type: "time(7)", nullable: true),
                    Exception = table.Column<string>(type: "varchar(max)", nullable: true),
                    InnerException = table.Column<string>(type: "varchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "varchar(max)", nullable: true),
                    Data = table.Column<string>(type: "varchar(max)", nullable: true),
                    Message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    CreateDateOnly = table.Column<DateOnly>(type: "Date", nullable: false),
                    CreateUser = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "DateTime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogJob", x => x.IdLogJob);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogAudit");

            migrationBuilder.DropTable(
                name: "LogEndpoint");

            migrationBuilder.DropTable(
                name: "LogJob");
        }
    }
}
