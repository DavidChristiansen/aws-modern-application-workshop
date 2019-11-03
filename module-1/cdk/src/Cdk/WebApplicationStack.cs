using System.IO;
using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Amazon.CDK.AWS.CloudFront;

namespace Cdk
{
    internal class WebApplicationStack : Stack
    {
        public WebApplicationStack(Construct parent, string id) : base(parent, id)
        {

            // Create a S3 bucket, with the given name and define the web index document as 'index.html'
            var bucket = new Bucket(this, "Bucket", new BucketProps
            {
                WebsiteIndexDocument = "index.html"
            });

            // Obtain the cloudfront origin access identity so that the s3 bucket may be restricted to it.
            var origin = new CfnCloudFrontOriginAccessIdentity(this, "BucketOrigin", new CfnCloudFrontOriginAccessIdentityProps{
              CloudFrontOriginAccessIdentityConfig= new {
                comment="mysfits-workshop"
              }
            });

            // Restrict the S3 bucket via a bucket policy that only allows our CloudFront distribution
            bucket.GrantRead(new CanonicalUserPrincipal(
              origin.AttrS3CanonicalUserId
            ));

            // Definition for a new CloudFront web distribution, which enforces traffic over HTTPS
            var cdn = new CloudFrontWebDistribution(this, "CloudFront", new CloudFrontWebDistributionProps
            {
                ViewerProtocolPolicy = ViewerProtocolPolicy.ALLOW_ALL,
                PriceClass = PriceClass.PRICE_CLASS_ALL,
                OriginConfigs = new SourceConfiguration[] {
                  new SourceConfiguration {
                      Behaviors = new Behavior[] {
                        new Behavior {
                            IsDefaultBehavior = true,
                            MaxTtl = null,
                            AllowedMethods = CloudFrontAllowedMethods.GET_HEAD_OPTIONS
                        }
                      },
                      OriginPath="/web",
                      S3OriginSource = new S3OriginConfig {
                        S3BucketSource=bucket,
                        OriginAccessIdentityId=origin.Ref
                      }
                  }
                }
            });

            string currentPath = Directory.GetCurrentDirectory();

            // A CDK helper that takes the defined source directory, compresses it, and uploads it to the destination s3 bucket.
            new BucketDeployment(this, "DeployWebsite", new BucketDeploymentProps
            {
                Sources = new ISource[]{
                  Source.Asset(Path.Combine(currentPath, "../Web"))
                },
                DestinationBucket = bucket,
                DestinationKeyPrefix = "web/",
                Distribution = cdn,
                RetainOnDelete = false,
            });

            // Create a CDK Output which details the URL for the CloudFront Distribtion URL.
            new CfnOutput(this, "CloudFrontURL", new CfnOutputProps
            {
                Description = "The CloudFront distribution URL",
                Value = "http://" + cdn.DomainName
            });
        }
    }
}