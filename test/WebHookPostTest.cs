#region header

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Robert Vandehey" file="WebHookPostTest.cs">
// MIT License
//
// Copyright(c) 2018 Robert Vandehey
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion header

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using System;
using System.Net;
using System.Threading;
using Xunit;

namespace SynchroFeed.AWS.Lambda.WebHook.Test
{
    public class WebHookPostTest
    {
        private WebMethods WebMethods { get; set; }
        private Mock<IAmazonSimpleNotificationService> SnsMock { get; }
        private Mock<APIGatewayProxyRequest> RequestMock { get; }
        private Mock<ILambdaLogger> LoggerMock { get; }
        private Mock<ILambdaContext> ContextMock { get; }

        public WebHookPostTest()
        {
            SnsMock = new Mock<IAmazonSimpleNotificationService>(MockBehavior.Strict);
            SnsMock
                .Setup(a => a.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                .Callback((PublishRequest req, CancellationToken token) => CheckPublishRequest(req))
                .ReturnsAsync(new PublishResponse
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    MessageId = Guid.NewGuid().ToString()
                })
                .Verifiable();
            RequestMock = new Mock<APIGatewayProxyRequest>();
            LoggerMock = new Mock<ILambdaLogger>();
            LoggerMock.Setup(a => a.Log(It.IsAny<string>()));
            LoggerMock.Setup(a => a.LogLine(It.IsAny<string>()));
            ContextMock = new Mock<ILambdaContext>();
            ContextMock.Setup(a => a.Logger).Returns(LoggerMock.Object);

            WebMethods.GetSnsClientFunc = credentials => SnsMock.Object;
        }

        [Fact]
        public void Test_WebMethod_Post_With_Environment_Config()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            Environment.SetEnvironmentVariable("WEBHOOK_AWSCREDENTIALS:ACCESSKEY", "TestAccessKey");
            Environment.SetEnvironmentVariable("WEBHOOK_AWSCREDENTIALS:SECRETKEY", "TestSecretKey");
            Environment.SetEnvironmentVariable("WEBHOOK_AWSSNS:TOPIC", "TestSnsTopic");

            WebMethods.ConfigurationBuilderFunc = (context, builder) => builder.AddEnvironmentVariables("WEBHOOK_");

            WebMethods = new WebMethods();

            Assert.Equal("TestAccessKey", WebMethods.awsCredentials.AccessKey);
            Assert.Equal("TestSecretKey", WebMethods.awsCredentials.SecretKey);
            Assert.Equal("TestSnsTopic", WebMethods.awsSns.Topic);

            var response = WebMethods.Post(RequestMock.Object, ContextMock.Object);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            SnsMock.Verify();
        }

        [Fact]
        public void Test_WebMethod_Post_With_Json_Config()
        {
            WebMethods.ConfigurationBuilderFunc = (context, builder) => builder.AddJsonFile("testconfig.json", false);

            WebMethods = new WebMethods();

            Assert.Equal("TESTACCESSKEY", WebMethods.awsCredentials.AccessKey);
            Assert.Equal("TESTSECRETKEY", WebMethods.awsCredentials.SecretKey);
            Assert.Equal("arn:aws:sns:us-east-1:TEST:TestFeed", WebMethods.awsSns.Topic);

            var response = WebMethods.Post(RequestMock.Object, ContextMock.Object);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            SnsMock.Verify();
        }

        [Fact]
        public void Test_WebMethod_Default_Create()
        {
            var called = false;

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

            WebMethods.ConfigurationBuilderFunc = (hostingContext, builder) =>
            {
                called = true;
                Assert.True(hostingContext.HostingEnvironment.IsDevelopment());
                return builder;
            };

            WebMethods = new WebMethods();

            Assert.True(called);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void CheckPublishRequest(PublishRequest request)
        {
            Assert.Equal(WebMethods.SnsSubject, request.Subject);
            Assert.Equal(WebMethods.awsSns.Topic, request.TopicArn);
        }
    }
}