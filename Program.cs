using Dapper;
using DotNetEnv;
using HappyCraftEvent.Contracts.Enums;
using HappyCraftEvent.Contracts.Scopes;
using HappyCraftEvent.DataAccess.IRepository;
using HappyCraftEvent.DataAccess.Repository;
using HappyCraftEvent.Helper.IService;
using HappyCraftEvent.Helper.Service;
using HappyCraftEvent.Helper.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
    Env.Load(envPath);

// Register Dapper type handlers so TEXT columns map to enum types.
SqlMapper.AddTypeHandler(new EnumTypeHandler<UserRole>());
SqlMapper.AddTypeHandler(new EnumTypeHandler<UserStatus>());
SqlMapper.AddTypeHandler(new EnumTypeHandler<GendersEnum>());

var builder = WebApplication.CreateBuilder(args);

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HappyCraftEvent";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HappyCraftEvent-API";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey))
    };
});

// Authorization policies for scopes
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Scope:UsersRead", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("scope", HappyCraftScopes.UsersRead)))
    .AddPolicy("Scope:UsersWrite", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("scope", HappyCraftScopes.UsersWrite)))
    .AddPolicy("Scope:EventsRead", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("scope", HappyCraftScopes.EventsRead)))
    .AddPolicy("Scope:EventsWrite", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("scope", HappyCraftScopes.EventsWrite)))
    .AddPolicy("Scope:EventsAssignAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("scope", HappyCraftScopes.EventsAssignAdmin)))
    .AddPolicy("Scope:GuestsRead", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("scope", HappyCraftScopes.GuestsRead)))
    .AddPolicy("Scope:GuestsWrite", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("scope", HappyCraftScopes.GuestsWrite)))
    .AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.FindFirst("role")?.Value == UserRole.ADMIN.ToString()));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Dependency injection
builder.Services.AddScoped<IUsersDal, UsersDal>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthDal, AuthDal>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtTokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
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
