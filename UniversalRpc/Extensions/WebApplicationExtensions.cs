#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
#endif
using UniversalRPC.RPC.Model;
using System.IO;
using UniversalRPC.RPC.Services;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UniversalRPC.Extensions;


namespace UniversalRPC.RPC.Extensions
{
#if NET6_0_OR_GREATER
    public static class WebApplicationExtensions
    {
        private async static Task ToExcuteRPC(HttpContext context, WebApplication app)
        {
            var request = await context.Request.ReadFromJsonAsync<Request>();
            if (request != null)
            {
                var serviceFactory = app.Services.GetService<RPCServiceFactory>();
                var serviceType = serviceFactory.GetServiceType(request.ServiceName);
                if (serviceType != null)
                {
                using(var scope=app.Services.CreateScope()){
                var service = scope.ServiceProvider.GetService(serviceType);
                    if (request?.MethodName != null)
                    {
                        var method = serviceType.GetMethod(request.MethodName);
                        var result = method?.Invoke(service, request.Parameters);
                         if (result != null && result.GetType().IsTask(out var retType))
                        {
                             var task=(Task)result;
                        await task.ConfigureAwait(false);
                        var resultProperty = task.GetType().GetProperty("Result");
                            result = resultProperty.GetValue(task);
                        }

                        context.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                        if (result != null)
                        {
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(result, RPC.JsonSerializerSettings));
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                    }
                }
                    
                }
                else
                {
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                }
            }
            else
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            }
        }

        private async static Task ToExcuteRPC(HttpContext context, IEndpointRouteBuilder app)
        {
        var body=context.Request.Body;
        var read = new StreamReader(body);
        var request=JsonConvert.DeserializeObject<Request>(await read.ReadToEndAsync());
            //var request = await context.Request.ReadFromJsonAsync<Request>();
            if (request != null)
            {
                var serviceFactory = app.ServiceProvider.GetService<RPCServiceFactory>();
                var serviceType = serviceFactory.GetServiceType(request.ServiceName);
                if (serviceType != null)
                {
                using(var scope=app.ServiceProvider.CreateScope()){
                var service = scope.ServiceProvider.GetService(serviceType);
                    if (request?.MethodName != null)
                    {
                        var method = serviceType.GetMethod(request.MethodName);
                        var result = method?.Invoke(service, request.Parameters);
                        if (result != null && result.GetType().IsTask(out var retType))
                        {
                        var task=(Task)result;
                        await task.ConfigureAwait(false);
                        var resultProperty = task.GetType().GetProperty("Result");
                            result = resultProperty.GetValue(task);
                        }

                        context.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                        if (result != null)
                        {
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(result, RPC.JsonSerializerSettings));
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                    }
                }
                    
                }
                else
                {
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                }
            }
            else
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <returns></returns>
        public static WebApplication UseRPCService(this WebApplication app)
        {
            _ = app.MapPost("/rpc", async (context)=>await ToExcuteRPC(context,app));
            _ = app.MapGet("/rpc", async (context) => await ToExcuteRPC(context, app));
            return app;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IEndpointRouteBuilder UseRPCService(this IEndpointRouteBuilder app)
        {
            _ = app.MapPost("/rpc", async (context) => await ToExcuteRPC(context, app));
            _ = app.MapGet("/rpc", async (context) => await ToExcuteRPC(context, app));
            return app;
        }

    }
#endif
}
