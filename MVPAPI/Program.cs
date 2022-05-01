using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using LazyCache;
using MVPAPI;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped(typeof(IConnectionStringProvider), typeof(ConnectionStringProvider));
builder.Services.AddLazyCache();

var app = builder.Build();

app.Use((context, next) =>
{
    context.Request.EnableBuffering();
    return next();
});

InitDatabaseCache(app);

app.UseMiddleware(typeof(RestApiMiddleware));

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

void InitDatabaseCache(WebApplication app)
{
    var cache = app.Services.GetService<IAppCache>();
    
    cache.GetOrAdd("DatabaseTableNames", () =>
    {
        using var connection = new NpgsqlConnection(new ConnectionStringProvider().ConnectionString);
        var names = connection.Query<string>(@"
            SELECT tablename
            FROM pg_catalog.pg_tables
            WHERE schemaname != 'pg_catalog' 
            AND schemaname != 'information_schema';
            ").ToList();
        return names;
    });
}