using DotNetEnv;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.RegularExpressions;

namespace HappyCraftEvent.Migrator;

public static class MigrationConfiguration
{
    public static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var slnPath = Path.Combine(current.FullName, "happycraftevent.sln");
            if (File.Exists(slnPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate solution root.");
    }

    public static void LoadDotEnv(string solutionRoot, ILogger logger)
    {
        var envPath = Path.Combine(solutionRoot, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            logger.LogInformation(".env loaded from: {EnvPath}", envPath);
        }
        else
        {
            logger.LogWarning(".env file not found at {EnvPath}.", envPath);
        }
    }

    /// <summary>
    /// Returns the connection string from .env, or null if missing. Caller decides how to handle the missing case.
    /// </summary>
    public static string? GetConnectionString(ILogger logger)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("Connection string resolved from .env.");
            return connectionString;
        }

        logger.LogError("ConnectionStrings__Default is missing or empty in .env.");
        return null;
    }

    /// <summary>
    /// Ensures the target database exists, creating it if necessary.
    /// Returns false if the operation fails.
    /// </summary>
    public static bool EnsureDatabase(string connectionString, ILogger logger)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var targetDatabase = builder.Database;

            if (string.IsNullOrWhiteSpace(targetDatabase))
            {
                logger.LogError("Connection string is missing database name.");
                return false;
            }

            if (!Regex.IsMatch(targetDatabase, "^[a-zA-Z0-9_]+$"))
            {
                logger.LogError("Invalid target database name format: {DatabaseName}", targetDatabase);
                return false;
            }

            logger.LogInformation("Checking target database existence: {DatabaseName}", targetDatabase);

            // PostgreSQL requires connecting to an existing system database to create/check another database.
            builder.Database = "postgres";
            var systemConnectionString = builder.ToString();

            using var connection = new NpgsqlConnection(systemConnectionString);
            connection.Open();

            const string checkSql = "SELECT 1 FROM pg_database WHERE LOWER(datname) = LOWER(@databaseName)";
            var exists = connection.QueryFirstOrDefault<int?>(checkSql, new { databaseName = targetDatabase }) is not null;

            if (exists)
            {
                logger.LogInformation("Target database already exists: {DatabaseName}", targetDatabase);
            }
            else
            {
                logger.LogWarning("Target database does not exist. Creating: {DatabaseName}", targetDatabase);
                var createSql = $"CREATE DATABASE \"{targetDatabase}\" ENCODING = 'UTF8'";
                connection.Execute(createSql);
                logger.LogInformation("Target database created successfully: {DatabaseName}", targetDatabase);
            }

            logger.LogInformation("Database check passed.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure database exists. Check the connection string and PostgreSQL availability.");
            return false;
        }
    }
}
