using Amazon.CDK;
using Amazon.CDK.AWS.EC2;

namespace Cdk
{
    internal class NetworkStack : Stack
    {
        public Vpc vpc { get; }
        public NetworkStack(Construct parent, string id) : base(parent, id)
        {
            this.vpc = new Vpc(this, "VPC", new VpcProps
            {
                NatGateways = 1,
                MaxAzs = 2
            });
        }
    }
}