using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;

namespace Cdk
{
    internal class DynamoDbStack : Stack
    {
        public Repository ecrRepository { get; }
        public Table table { get; }

        public DynamoDbStack(Construct parent, string id, DynamoDbStackProps props) : base(parent, id, props)
        {
            var dynamoDbEndpoint = props.Vpc.AddGatewayEndpoint("DynamoDbEndpoint", new GatewayVpcEndpointOptions
            {
                Service = GatewayVpcEndpointAwsService.DYNAMODB
            });

            var dynamoDbPolicy = new PolicyStatement();
            dynamoDbPolicy.AddAnyPrincipal();
            dynamoDbPolicy.AddActions("*");
            dynamoDbPolicy.AddAllResources();
            dynamoDbEndpoint.AddToPolicy(
                dynamoDbPolicy
            );

            this.table = new Table(this, "Table", new TableProps
            {
                TableName = "MysfitsTable",
                PartitionKey = new Attribute
                {
                    Name = "MysfitId",
                    Type = AttributeType.STRING
                }
            });
            this.table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "LawChaosIndex",
                PartitionKey = new Attribute
                {
                    Name = "LawChaos",
                    Type = AttributeType.STRING
                },
                SortKey = new Attribute
                {
                    Name = "MysfitId",
                    Type = AttributeType.STRING
                },
                ReadCapacity = 5,
                WriteCapacity = 5,
                ProjectionType = ProjectionType.ALL
            });
            this.table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "GoodEvilIndex",
                PartitionKey = new Attribute
                {
                    Name = "GoodEvil",
                    Type = AttributeType.STRING
                },
                SortKey = new Attribute
                {
                    Name = "MysfitId",
                    Type = AttributeType.STRING
                },
                ReadCapacity = 5,
                WriteCapacity = 5,
                ProjectionType = ProjectionType.ALL
            });

            var fargatePolicy = new PolicyStatement();
            fargatePolicy.AddActions(
                //  Allows the ECS tasks to interact with only the MysfitsTable in DynamoDB
                "dynamodb:Scan",
                "dynamodb:Query",
                "dynamodb:UpdateItem",
                "dynamodb:GetItem",
                "dynamodb:DescribeTable"
            );
            fargatePolicy.AddResources(
                "arn:aws:dynamodb:*:*:table/MysfitsTable*"
            );
            props.fargateService.TaskDefinition.AddToTaskRolePolicy(
                fargatePolicy
            );
        }
    }

    public class DynamoDbStackProps : StackProps
    {
        internal FargateService fargateService { get; set; }

        internal Vpc Vpc { get; set; }
    }
}