using System;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Configuration;

namespace LocalBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DateHeader.SyncDateTimer();
   
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddCommandLine(args)
                .Build();

            var host = new WebHostBuilder()
                .UseKestrel((context, options) =>
                {
                    IPEndPoint endPoint = context.Configuration.CreateIPEndPoint();

                    options.Listen(endPoint, builder =>
                    {
                        builder.UseHttpApplication<BenchmarkApplication>();
                    });
                })
                .UseStartup<Startup>()
                .Build();

            return host;
        }
    }

    public static class HttpApplicationConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseHttpApplication<TConnection>(this IConnectionBuilder builder) where TConnection : IHttpConnection, new()
        {
            return builder.Use(next => new HttpApplication<TConnection>().ExecuteAsync);
        }
    }

    public class HttpApplication<TConnection> where TConnection : IHttpConnection, new()
    {
        public Task ExecuteAsync(ConnectionContext connection)
        {
            var httpConnection = new TConnection
            {
                Reader = connection.Transport.Input,
                Writer = connection.Transport.Output
            };
            return httpConnection.ExecuteAsync();
        }
    }

    public interface IHttpConnection : IHttpHeadersHandler, IHttpRequestLineHandler
    {
        PipeReader Reader { get; set; }
        PipeWriter Writer { get; set; }
        Task ExecuteAsync();
        ValueTask OnReadCompletedAsync();
    }
}
