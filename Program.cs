using Dapper;
using DotNetEnv;
using HappyCraftEvent.Contracts.Enums;
using HappyCraftEvent.DataAccess.IRepository;
using HappyCraftEvent.DataAccess.Repository;
using HappyCraftEvent.Helper.IService;
using HappyCraftEvent.Helper.Service;
using System.Text.Json.Serialization;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
    Env.Load(envPath);

// Register Dapper type handlers so TEXT columns map to enum types.
SqlMapper.AddTypeHandler(new EnumTypeHandler<UserRole>());
SqlMapper.AddTypeHandler(new EnumTypeHandler<UserStatus>());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency injection
builder.Services.AddScoped<IUsersDal, UsersDal>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Converts enum values to/from their string names when Dapper reads TEXT columns.
sealed class EnumTypeHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
{
    public override T Parse(object value) =>
        Enum.Parse<T>(value.ToString()!, ignoreCase: true);

    public override void SetValue(System.Data.IDbDataParameter parameter, T value) =>
        parameter.Value = value.ToString();
}
