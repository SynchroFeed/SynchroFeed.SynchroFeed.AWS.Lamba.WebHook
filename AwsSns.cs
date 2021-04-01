using System;

namespace SynchroFeed.AWS.Lambda.WebHook
{
    /// <summary>
    /// The AwsSns class is used to encapsulate the AWS SNS settings.
    /// </summary>
    public class AwsSns
    {
        /// <summary>
        /// Gets or sets the AWS SNS topic.
        /// </summary>
        /// <value>The AWS SNS topic.</value>
        public string Topic { get; set; } = "";

    }
}
