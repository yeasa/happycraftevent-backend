using DbUp;
using HappyCraftEvent.Migrator;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));

var logger = loggerFactory.CreateLogger("Migrator");

// ── Locate solution root and load .env ───────────────────────────────────────
string solutionRoot;
try
{
    solutionRoot = MigrationConfiguration.FindSolutionRoot();
    MigrationConfiguration.LoadDotEnv(solutionRoot, logger);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to locate solution root or load .env.");
    return 1;
}

// ── Resolve connection string ────────────────────────────────────────────────
var connectionString = MigrationConfiguration.GetConnectionString(logger);
if (connectionString is null)
{
    logger.LogError("Aborting: no valid connection string found.");
    return 1;
}

// ── Ensure database exists ───────────────────────────────────────────────────
if (!MigrationConfiguration.EnsureDatabase(connectionString, logger))
    return 1;

// ── Locate scripts folder ────────────────────────────────────────────────────
var scriptsPath = Path.Combine(solutionRoot, "happycraftevent.Migrator", "Scripts");
if (!Directory.Exists(scriptsPath))
{
    logger.LogError("Scripts directory not found: {ScriptsPath}", scriptsPath);
    return 1;
}

// ── Run migrations ───────────────────────────────────────────────────────────
logger.LogInformation("Running migrations from: {ScriptsPath}", scriptsPath);

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsFromFileSystem(scriptsPath)
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();
if (!result.Successful)
{
    logger.LogError(result.Error, "Migration failed.");
    return 1;
}

logger.LogInformation("All migrations completed successfully.");
return 0;
