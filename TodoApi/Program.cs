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
        // databaseUrl example: postgres://user:pass@host:5432/dbname
        databaseUrl = databaseUrl.Replace("postgresql://", "postgres://");
        var uri = new Uri(databaseUrl);
        var uri = new Uri(databaseUrl);
        var userInfoParts = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfoParts.ElementAtOrDefault(0) ?? string.Empty);
        var password = Uri.UnescapeDataString(userInfoParts.ElementAtOrDefault(1) ?? string.Empty);

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = username,
            Password = password,
            Database = uri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Require,
            TrustServerCertificate = true,
            IncludeErrorDetail = true
        };

        return csb.ConnectionString;
    }

    throw new Exception("DATABASE_URL não encontrado no ambiente.");
}

builder.Services.AddDbContext<TodoContext>(opt =>
{
    var connString = BuildConnectionString();

    // LOG seguro (não mostra senha)
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