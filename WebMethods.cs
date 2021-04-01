using System;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

[assembly: InternalsVisibleTo("SynchroFeed.AWS.Lambda.WebHook.Test")]
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace SynchroFeed.AWS.Lambda.WebHook
{
    public class WebMethods
    {
        internal Func<BasicAWSCredentials, IAmazonSimpleNotificationService> GetSnsClientFunc = credentials => new AmazonSimpleNotificationServiceClient(credentials);
        internal Func<HostBuilderContext, IConfigurationBuilder, IConfigurationBuilder> ConfigurationBuilderFunc = InitializeConfigOption;

        internal readonly AwsCredentials awsCredentials;
        internal readonly AwsSns awsSns;

        public const string SnsSubject = "Package Event";

        /// <summary>
        /// Initializes a new instance of the <see cref="WebMethods"/> class.
        /// </summary>
        public WebMethods()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebMethods"/> class.
        /// </summary>
        /// <param name="configurationBuilderFunc">The configuration builder function used for unit testing.</param>
        internal WebMethods(Func<HostBuilderContext, IConfigurationBuilder, IConfigurationBuilder> configurationBuilderFunc = null)
        {
            if (configurationBuilderFunc != null)
                ConfigurationBuilderFunc = configurationBuilderFunc;

            var builder = InitializeHostBuilder();
            var host = builder.Build();

            awsCredentials = host.Services.GetRequiredService<IOptions<AwsCredentials>>().Value;
            awsSns = host.Services.GetRequiredService<IOptions<AwsSns>>().Value;
        }


        private IHostBuilder InitializeHostBuilder()
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables(prefix: "ASPNETCORE_");
                    hostingContext.HostingEnvironment.EnvironmentName = GetEnvironmentName();
                    ConfigurationBuilderFunc(hostingContext, config);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddOptions()
                        .Configure<AwsCredentials>(options => hostContext.Configuration.GetSection("AwsCredentials").Bind(options))
                        .Configure<AwsSns>(options => hostContext.Configuration.GetSection("AwsSns").Bind(options));
                });

            return builder;
        }

        private string GetEnvironmentName()
        {
            var result = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrEmpty(result)) return result;
            result = Environment.GetEnvironmentVariable("Hosting:Environment");
            if (!string.IsNullOrEmpty(result)) return result;
            result = Environment.GetEnvironmentVariable("ASPNET_ENV");
            if (!string.IsNullOrEmpty(result)) return result;
            return "Development";
        }

        private static IConfigurationBuilder InitializeConfigOption(HostBuilderContext hostingContext, IConfigurationBuilder builder)
        {
            if (hostingContext.HostingEnvironment.IsDevelopment())
            {
                return builder.AddJsonFile("config.dev.json", false);
            }
            else
            {
                return builder.AddJsonFile($"config.{hostingContext.HostingEnvironment.EnvironmentName}.json", true)
                    .AddEnvironmentVariables("WEBHOOK_");
            }
        }

        /// <summary>
        /// A Lambda function to respond to HTTP POST methods from API Gateway
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>The list of blogs</returns>
        public APIGatewayProxyResponse Post(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("SynchroFeed.AWS.Lambda.WebHook Post Request");
            context.Logger.LogLine($"Body:{request.Body}");
            var credential = new BasicAWSCredentials(awsCredentials.AccessKey, awsCredentials.SecretKey);
            var client = GetSnsClientFunc(credential);
            var publishResponse = client.PublishAsync(new PublishRequest(awsSns.Topic, request.Body, SnsSubject));
            var result = publishResponse.Result;
            context.Logger.LogLine($"SNS Result:{result.HttpStatusCode}, {result.MessageId}");

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)publishResponse.Result.HttpStatusCode,
                Body = $"MessageId={publishResponse.Result.MessageId}",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };

            return response;
        }
    }
}
