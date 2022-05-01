using System.Net;
using System.Text.Json;
using System.Web;
using Dapper;
using LazyCache;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Npgsql;

namespace MVPAPI;

public class RestApiMiddleware
{
    private readonly RequestDelegate _next;
    private static ILogger<RestApiMiddleware> _logger;
    private readonly IAppCache _cache;

    public RestApiMiddleware(RequestDelegate next, ILogger<RestApiMiddleware> logger, IAppCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path.ToString().Contains("swagger"))
        {
            await _next(context);
        }
        else
        {
            context.Response.ContentType = "application/json";

            switch (context.Request.Method)
            {
                case "POST":
                    break;
                case "GET":
                    var response = await HandleGet(context);
                    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions() {WriteIndented = true});

                    await context.Response.WriteAsync(json);
                    break;
            }
        }
    }

    private async Task<RestApiResponse> HandleGet(HttpContext context)
    {
        var headers = context.Request.HttpContext.Request.Headers;
        var queryString =
            HttpUtility.ParseQueryString(context.Request.HttpContext.Request.QueryString.Value ?? string.Empty);
        var path = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var obj = path.FirstOrDefault();
        
        //test

        if (string.IsNullOrEmpty(obj))
        {
            return new RestApiResponse()
            {
                Information = new Information()
                {
                    Message = "Cannot GET at path '/'. Please provide a DataObject to GET."
                }
            };
        }

        var info = new Information()
        {
            DataObjectName = obj.Split('/').First()
        };

        if (_cache.Get<List<string>>("DatabaseTableNames").Contains(info.DataObjectName))
        {
            dynamic dbObjects = null;
            if (path.Length > 1)
            {
                dbObjects = await SelectWithIdQuery(context, info, path[1]);
            }
            else
            {
                dbObjects = await SelectAllQuery(context, info);
            }

            return new RestApiResponse()
            {
                Information = info,
                Result = new Result()
                {
                    DataObjects = dbObjects
                }
            };
        }
        else
        {
            context.Response.StatusCode = (int) HttpStatusCode.NotFound;
            info.Message = $"Table for DatabObject with name {info.DataObjectName} does not yet exist.";

            return new RestApiResponse()
            {
                Information = info,
                Result = new Result()
            };
        }
    }

    private async Task<object> SelectWithIdQuery(HttpContext context, Information info, string id)
    {
        Guid guid;
        if (!Guid.TryParse(id, out guid))
        {
            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            info.Message = $"Id must be of type uuid.";
            info.Parameters = new {Id = id};
            return null;
        }
        
        await using var connection = new NpgsqlConnection(new ConnectionStringProvider().ConnectionString);

        var query = $"SELECT * FROM \"{info.DataObjectName}\" WHERE \"Id\" = @Id";
        var parameters = new {Id = guid};
        var dbObject = await connection.QueryFirstOrDefaultAsync(query, parameters);

        info.Query = query;
        info.Parameters = parameters;

        if (dbObject == null)
        {
            context.Response.StatusCode = (int) HttpStatusCode.NotFound;
            info.Message = $"DataObject not found";
        }
        else
        {
            context.Response.StatusCode = (int) HttpStatusCode.OK;
            info.Message = $"Found 1 DataObject.";
        }
        
        return dbObject;
    }

    private static async Task<dynamic> SelectAllQuery(HttpContext context, Information info)
    {
        await using var connection = new NpgsqlConnection(new ConnectionStringProvider().ConnectionString);

        var query = $"SELECT * FROM \"{info.DataObjectName}\"";
        var dbObjects = (await connection.QueryAsync(query))
            .ToList();

        context.Response.StatusCode = (int) HttpStatusCode.OK;
        info.Message = $"Found {dbObjects.Count} DataObjects.";
        info.Query = query;
        return dbObjects;
    }
}

public class RestApiResponse 
{
    public Result Result { get; set; }
    public Information Information { get; set; }
}

public class Information
{
    public string Message { get; set; }                             
    public string DataObjectName { get; set; }  
    public string Query { get; set; }
    public object Parameters { get; set; }
}

public class Result
{
    public dynamic DataObjects { get; set; }
}