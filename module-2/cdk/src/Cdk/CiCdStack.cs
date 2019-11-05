using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.CodeCommit;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.IAM;

namespace Cdk
{
    internal class CiCdStack : Stack
    {
        public CiCdStack(Construct parent, string id, CiCdStackProps props) : base(parent, id, props)
        {
            var apiRepository = Amazon.CDK.AWS.CodeCommit.Repository.FromRepositoryArn(this, "Repository", props.apiRepositoryArn);
            var environmentVariables = new Dictionary<string, IBuildEnvironmentVariable>();
            environmentVariables.Add("AWS_ACCOUNT_ID", new BuildEnvironmentVariable()
            {
                Type = BuildEnvironmentVariableType.PLAINTEXT,
                Value = Aws.ACCOUNT_ID
            });
            environmentVariables.Add("AWS_DEFAULT_REGION", new BuildEnvironmentVariable()
            {
                Type = BuildEnvironmentVariableType.PLAINTEXT,
                Value = Aws.REGION
            });
            var codebuildProject = new PipelineProject(this, "BuildProject", new PipelineProjectProps
            {
                Environment = new BuildEnvironment
                {
                    ComputeType = ComputeType.SMALL,
                    BuildImage = LinuxBuildImage.UBUNTU_14_04_PYTHON_3_5_2,
                    Privileged = true,
                    EnvironmentVariables = environmentVariables
                }
            });
            var codeBuildPolicy = new PolicyStatement();
            codeBuildPolicy.AddResources(apiRepository.RepositoryArn);
            codeBuildPolicy.AddActions(
                "codecommit:ListBranches",
                "codecommit:ListRepositories",
                "codecommit:BatchGetRepositories",
                "codecommit:GitPull"
            );
            codebuildProject.AddToRolePolicy(
                codeBuildPolicy
            );
            props.ecrRepository.GrantPullPush(codebuildProject.GrantPrincipal);

            var sourceOutput = new Artifact_();
            var sourceAction = new Amazon.CDK.AWS.CodePipeline.Actions.CodeCommitSourceAction(
                new Amazon.CDK.AWS.CodePipeline.Actions.CodeCommitSourceActionProps
                {
                    ActionName = "CodeCommit-Source",
                    Branch = "master",
                    Trigger = CodeCommitTrigger.POLL,
                    Repository = apiRepository,
                    Output = sourceOutput
                });

            var buildOutput = new Artifact_();
            var buildAction = new CodeBuildAction(new CodeBuildActionProps
            {
                ActionName = "Build",
                Input = sourceOutput,
                Outputs = new Artifact_[]
                {
                    buildOutput
                },
                Project = codebuildProject
            });

            var deployAction = new EcsDeployAction(new EcsDeployActionProps
            {
                ActionName = "DeployAction",
                Input = buildOutput,
                Service = props.ecsService,
            });

            var pipeline = new Pipeline(this, "Pipeline");
            pipeline.AddStage(new StageOptions
            {
                StageName = "Source",
                Actions = new Action[] {sourceAction}
            });
            pipeline.AddStage(new StageOptions
            {
                StageName = "Build",
                Actions = new Action[] {buildAction}
            });
            pipeline.AddStage(new StageOptions
            {
                StageName = "Deploy",
                Actions = new Action[] {deployAction}
            });
        }
    }

    public class CiCdStackProps : StackProps
    {
        public Amazon.CDK.AWS.ECR.Repository ecrRepository { get; internal set; }
        public FargateService ecsService { get; internal set; }
        public string apiRepositoryArn { get; internal set; }
    }
}