using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Configure server URL when running on platforms like Render (uses PORT env var)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add Entity Framework
// Prefer DATABASE_URL when available (e.g., Render), otherwise fall back to appsettings
string? BuildConnectionString()
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        // garante compatibilidade com "postgresql://"
        databaseUrl = databaseUrl.Replace("postgresql://", "postgres://");

        var dbUri = new Uri(databaseUrl);

        var userInfoParts = dbUri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfoParts.ElementAtOrDefault(0) ?? string.Empty);
        var password = Uri.UnescapeDataString(userInfoParts.ElementAtOrDefault(1) ?? string.Empty);

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = dbUri.Host,
            Port = dbUri.Port > 0 ? dbUri.Port : 5432,
            Username = username,
            Password = password,
            Database = dbUri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Require,
            IncludeErrorDetail = true
        };

        return csb.ConnectionString;
    }

    return builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<TodoContext>(opt =>
{
    var connString = BuildConnectionString();

    var safe = new NpgsqlConnectionStringBuilder(connString) { Password = "*****" }.ToString();
    Console.WriteLine($"[DB] Using connection: {safe}");

    opt.UseNpgsql(connString);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
    context.Database.Migrate();
}

app.Run();