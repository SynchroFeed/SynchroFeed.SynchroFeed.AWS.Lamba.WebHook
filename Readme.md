# SynchroFeed.AWS.Lambda.WebHook
[![Slack](https://a.slack-edge.com/66f9/img/landing/header_logo_sprite.png)](https://synchrofeed.slack.com/)

*SynchroFeed* is an extensible framework written in Microsoft .NET C# for integrating with a Nuget-like feed to perform 
actions against the packages in the feed.

The *SynchroFeed.AWS.Lambda.WebHook* package is an AWS Lambda function written in .NET Core 2.1 that supports an event posted to it
that represents an action against a package on a feed. That event is passed on to the configured AWS SNS queue for handling by some
other process like the *SynchroFeed.Listener*. 

For example, Proget supports registering a webhook when a package has been added,
deleted, deployed, promoted or purged on a feed. By adding the following configuration to the Content section of a webhook,
a message will be POSTed to the URL associated with the Proget webhook. 

```
$ToJson(%(
    packageUrl: https://myprogetserver.mycompany.com/feeds/$FeedName/$PackageId/$PackageVersion,
    feed: myprogetserver.$FeedName,
    package: $PackageId,
    version: $PackageVersion,
    hash: $PackageHash,
    packageType: $FeedType,
    event: $WebhookEvent,
    user: $UserName
))
```

The names preceded by $ are Proget variables that are replaced with actual values from the affected feed and package.
More details are available in Inedo's Proget WebHook [documentation](https://inedo.com/support/documentation/proget/advanced/webhooks).

### Message Format
This library doesn't validate the message posted to it. It just passes the message body on to the configured AWS SNS queue. Whatever is listening
to the SNS queue, will parse and validate the message and determine how to handle it. The *SynchroFeed.Listener* supports handling messages with 
the following format.

```
{
    "packageUrl": "https://myprogetserver.mycompany.com/feeds/MyFeedName/MyPackageId/1.0.0",
    "feed": "MyFeedName",
    "package": "MyPackageId",
    "version": "1.0.0",
    "hash": "a0f99da1bff470df3cd5f98d1853429a4d1f978c",
    "packageType": "nuget",
    "event": "added",
    "user": "MyUsername"
}
```

## Deploying to AWS
While installing a webhook might be different for each use, this section documents a common configuration. It consists of deploying the
artifacts of the WebHook project as an AWS Lambda. Since a Lambda can't get POST requests directly from the web, it is necessary to
create an AWS API Gateway endpoint that will pass the message on to the Lambda.

The ```serverless.template``` file is an AWS CloudFormation Serverless Application Model template file for that has declared 
the necessary Serverless functions and other AWS resources.


* Function.cs - class file containing the C# method mapped to the single function declared in the template file
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS

You may also have a test project depending on the options selected.

The generated project contains a Serverless template declaration for a single AWS Lambda function that will be exposed through Amazon API Gateway as a HTTP *Get* operation. Edit the template to customize the function or add more functions and other resources needed by your application, and edit the function code in Function.cs. You can then deploy your Serverless application.

## Here are some steps to follow from Visual Studio:

To deploy your Serverless application, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed application open the Stack View window by double-clicking the stack name shown beneath the AWS CloudFormation node in the AWS Explorer tree. The Stack View also displays the root URL to your published application.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can use the following command lines to deploy your application from the command line (these examples assume the project name is *EmptyServerless*):

Restore dependencies
```
    cd "AlkamiFeedWebAPI"
    dotnet restore
```

Execute unit tests
```
    cd "AlkamiFeedWebAPI/test/AlkamiFeedWebAPI.Tests"
    dotnet test
```

Deploy application
```
    cd "AlkamiFeedWebAPI/src/AlkamiFeedWebAPI"
    dotnet lambda deploy-serverless
```
