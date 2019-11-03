using Amazon.CDK;
using Amazon.CDK.AWS.CodeCommit;

namespace Cdk
{
    internal class DeveloperToolsStack : Stack
    {
        public Repository apiRepository { get; }
        public Repository lambdaRepository { get; }

        public DeveloperToolsStack(Construct parent, string id) : base(parent, id)
        {
            var cdkRepository = new Repository(this, "CDKRepository", new RepositoryProps
            {
                RepositoryName = Amazon.CDK.Aws.ACCOUNT_ID + "-MythicalMysfitsService-Repository-CDK"
            });

            var webRepository = new Repository(this, "WebRepository", new RepositoryProps
            {
                RepositoryName = Amazon.CDK.Aws.ACCOUNT_ID + "-MythicalMysfitsService-Repository-Web"
            });

            this.apiRepository = new Amazon.CDK.AWS.CodeCommit.Repository(this, "APIRepository", new RepositoryProps
            {
                RepositoryName = Amazon.CDK.Aws.ACCOUNT_ID + "-MythicalMysfitsService-Repository-API"
            });

            this.lambdaRepository = new Amazon.CDK.AWS.CodeCommit.Repository(this, "LambdaRepository",
                new RepositoryProps
                {
                    RepositoryName = Amazon.CDK.Aws.ACCOUNT_ID + "-MythicalMysfitsService-Repository-Lambda"
                });

            new CfnOutput(this, "CDKRepositoryCloneUrlHttp", new CfnOutputProps()
            {
                Description = "CDK Repository CloneUrl HTTP",
                Value = cdkRepository.RepositoryCloneUrlHttp
            });
            new CfnOutput(this, "CDKRepositoryCloneUrlSsh", new CfnOutputProps()
            {
                Description = "CDK Repository CloneUrl SSH",
                Value = cdkRepository.RepositoryCloneUrlHttp
            });

            new CfnOutput(this, "WebRepositoryCloneUrlHttp", new CfnOutputProps()
            {
                Description = "Web Repository CloneUrl HTTP",
                Value = webRepository.RepositoryCloneUrlHttp
            });
            new CfnOutput(this, "WebRepositoryCloneUrlSsh", new CfnOutputProps()
            {
                Description = "Web Repository CloneUrl SSH",
                Value = webRepository.RepositoryCloneUrlSsh
            });

            new CfnOutput(this, "APIRepositoryCloneUrlHttp", new CfnOutputProps()
            {
                Description = "API Repository CloneUrl HTTP",
                Value = apiRepository.RepositoryCloneUrlHttp
            });
            new CfnOutput(this, "APIRepositoryCloneUrlSsh", new CfnOutputProps()
            {
                Description = "API Repository CloneUrl HTTP",
                Value = apiRepository.RepositoryCloneUrlSsh
            });

            new CfnOutput(this, "lambdaRepositoryCloneUrlHttp", new CfnOutputProps()
            {
                Description = "Lambda Repository CloneUrl HTTP",
                Value = lambdaRepository.RepositoryCloneUrlHttp
            });
            new CfnOutput(this, "lambdaRepositoryCloneUrlSsh", new CfnOutputProps()
            {
                Description = "Lambda Repository CloneUrl HTTP",
                Value = lambdaRepository.RepositoryCloneUrlSsh
            });
        }
    }
}