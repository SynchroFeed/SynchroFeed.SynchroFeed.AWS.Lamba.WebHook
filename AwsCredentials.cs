using System;

namespace SynchroFeed.AWS.Lambda.WebHook
{
    /// <summary>
    /// An enumeration for the Amazon CredentialsType
    /// </summary>
    public enum AwsCredentialsType
    {
        /// <summary>
        /// The credential is an Amazon Basic credential
        /// </summary>
        Basic
    }

    /// <summary>
    /// The AwsCredentials class encapsulates the AWS credentials.
    /// </summary>
    public class AwsCredentials
    {
        /// <summary>
        /// Gets or sets the type of the credentials.
        /// </summary>
        /// <value>The type of the credentials.</value>
        public AwsCredentialsType CredentialsType { get; set; } = AwsCredentialsType.Basic;

        /// <summary>
        /// Gets or sets the AWS credentials access key.
        /// </summary>
        /// <value>The AWS credentials access key.</value>
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the AWS credentials secret key.
        /// </summary>
        /// <value>The AWS credentials secret key.</value>
        public string SecretKey { get; set; }
    }
}
