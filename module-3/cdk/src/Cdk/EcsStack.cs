using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;

namespace Cdk
{
    internal class EcsStack : Stack
    {
        public Cluster ecsCluster { get; }
        public NetworkLoadBalancedFargateService ecsService { get; }

        public EcsStack(Construct parent, string id, EcsStackProps props) : base(parent, id)
        {
            this.ecsCluster = new Cluster(this, "Cluster", new ClusterProps
            {
                Vpc = props.Vpc,
            });
            this.ecsCluster.Connections.AllowFromAnyIpv4(Port.Tcp(8080));
            this.ecsService = new NetworkLoadBalancedFargateService(this, "Service", new NetworkLoadBalancedFargateServiceProps()
                {
                    Cluster = this.ecsCluster,
                    DesiredCount = 1,
                    PublicLoadBalancer = true,
                    TaskImageOptions = new NetworkLoadBalancedTaskImageOptions
                    {
                        EnableLogging = true,
                        ContainerPort = 8080,
                        Image = ContainerImage.FromEcrRepository(props.ecrRepository),
                    }
                }
            );
            this.ecsService.Service.Connections.AllowFrom(Peer.Ipv4(props.Vpc.VpcCidrBlock), Port.Tcp(8080));

            var taskDefinitionPolicy = new PolicyStatement();
            taskDefinitionPolicy.AddActions(
                // Rules which allow ECS to attach network interfaces to instances
                // on your behalf in order for awsvpc networking mode to work right
                "ec2:AttachNetworkInterface",
                "ec2:CreateNetworkInterface",
                "ec2:CreateNetworkInterfacePermission",
                "ec2:DeleteNetworkInterface",
                "ec2:DeleteNetworkInterfacePermission",
                "ec2:Describe*",
                "ec2:DetachNetworkInterface",

                // Rules which allow ECS to update load balancers on your behalf
                //  with the information sabout how to send traffic to your containers
                "elasticloadbalancing:DeregisterInstancesFromLoadBalancer",
                "elasticloadbalancing:DeregisterTargets",
                "elasticloadbalancing:Describe*",
                "elasticloadbalancing:RegisterInstancesWithLoadBalancer",
                "elasticloadbalancing:RegisterTargets",

                //  Rules which allow ECS to run tasks that have IAM roles assigned to them.
                "iam:PassRole",

                //  Rules that let ECS create and push logs to CloudWatch.
                "logs:DescribeLogStreams",
                "logs:CreateLogGroup");
            taskDefinitionPolicy.AddAllResources();

            this.ecsService.Service.TaskDefinition.AddToExecutionRolePolicy(
                taskDefinitionPolicy
            );

            var taskRolePolicy = new PolicyStatement();
            taskRolePolicy.AddActions(
                // Allow the ECS Tasks to download images from ECR
                "ecr:GetAuthorizationToken",
                "ecr:BatchCheckLayerAvailability",
                "ecr:GetDownloadUrlForLayer",
                "ecr:BatchGetImage",
                // Allow the ECS tasks to upload logs to CloudWatch
                "logs:CreateLogStream",
                "logs:CreateLogGroup",
                "logs:PutLogEvents"
            );
            taskRolePolicy.AddAllResources();

            this.ecsService.Service.TaskDefinition.AddToTaskRolePolicy(
                taskRolePolicy
            );
        }
    }

    public class EcsStackProps
    {
        public Repository ecrRepository { get; set; }
        public Vpc Vpc { get; set; }
    }
}