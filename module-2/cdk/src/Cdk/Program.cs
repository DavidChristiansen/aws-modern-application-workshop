using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cdk
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new App(null);
            var developerToolStack = new DeveloperToolsStack(app, "MythicalMysfits-DeveloperTools");
            new WebApplicationStack(app, "MythicalMysfits-WebApplication");
            var networkStack = new NetworkStack(app, "MythicalMysfits-Network");
            var ecrStack = new EcrStack(app, "MythicalMysfits-ECR");
            var ecsStack = new EcsStack(app, "MythicalMysfits-ECS", new EcsStackProps
            {
                Vpc = networkStack.vpc,
                ecrRepository = ecrStack.ecrRepository
            });
            new CiCdStack(app, "MythicalMysfits", new CiCdStackProps
            {
                ecrRepository = ecrStack.ecrRepository,
                ecsService = ecsStack.ecsService.Service,
                apiRepositoryArn = developerToolStack.apiRepository.RepositoryArn
            });
            app.Synth();
        }
    }
}
