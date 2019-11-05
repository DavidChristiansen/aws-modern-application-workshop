# Module 2: Creating a Service with AWS Fargate

![Architecture](/images/module-2/architecture-module-2.png)

**Time to complete:** 60 minutes

---
**Short of time?:** If you are short of time, refer to the completed AWS CDK code in `~/environment/workshop/source/module-2/cdk/`

---

**Services used:**

* [AWS CDK](https://docs.aws.amazon.com/CDK/latest/userguide/getting_started.html)
* [AWS Identity and Access Management (IAM)](https://aws.amazon.com/iam/)
* [Amazon Virtual Private Cloud (VPC)](https://aws.amazon.com/vpc/)
* [Amazon Elastic Load Balancing](https://aws.amazon.com/elasticloadbalancing/)
* [Amazon Elastic Container Service (ECS)](https://aws.amazon.com/ecs/)
* [AWS Fargate](https://aws.amazon.com/fargate/)
* [AWS Elastic Container Registry (ECR)](https://aws.amazon.com/ecr/)
* [AWS CodeCommit](https://aws.amazon.com/codecommit/)
* [AWS CodePipeline](https://aws.amazon.com/codepipeline/)
* [AWS CodeDeploy](https://aws.amazon.com/codedeploy/)
* [AWS CodeBuild](https://aws.amazon.com/codebuild/)

## Overview

In Module 2, using [AWS CDK](https://aws.amazon.com/cdk/), you will create a new microservice hosted with [AWS Fargate](https://aws.amazon.com/fargate/) on [Amazon Elastic Container Service](https://aws.amazon.com/ecs/) so that your Mythical Mysfits website can have an application backend to integrate with. AWS Fargate is a deployment option in Amazon ECS that allows you to deploy containers without having to manage any clusters or servers. For our Mythical Mysfits backend, we will use [.NET Core 2.1](https://docs.microsoft.com/en-us/dotnet/core/) and create a [Web API app](https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.1) in a [Docker container](https://www.docker.com/) behind a [Network Load Balancer](https://docs.aws.amazon.com/elasticloadbalancing/latest/network/introduction.html). These will form the microservice backend for the frontend website to integrate with.

## Module 2a: Creating a .NET Web API Container

Next, you will create a Docker container image that contains all of the code and configuration required to run the Mythical Mysfits backend as a microservice API created with .NET Core. We will build the docker container image within our local terminal and then push it to the Amazon Elastic Container Registry, where it will be available to pull when we create our service using Fargate.

All of the code required to run our service backend is stored within the `~/environment/workshop/source/module-2/webapi/` directory of the repository you've cloned into your local dev environment.  If you would like to review the .NET Core code that is used to create the service API, view the `~/environment/workshop/source/module-2/webapi/Controllers/MysfitsController.cs` file.

If you do not have Docker installed on your machine, you will need to install it. If you have it aleady installed, we can build the image by running the following commands.

**Action:** Create the directory `~/environment/workshop/webapi/`

```sh
mkdir ~/environment/workshop/webapi/
```

```sh
cd ~/environment/workshop/webapi/
```

```sh
git init
```

**Action:** Copy the API code to your local directory.

```sh
cp -r ~/environment/workshop/source/module-2/webapi/* ~/environment/workshop/webapi/
```

```sh
git add .
```

```sh
git commit -m 'Initial commit of WebAPI'
```

### Using Git with AWS CodeCommit

We need to configure git and integrate it with your CodeCommit repository.

[See this documentation for instructions on generating Git credentials for CodeCommit](https://docs.aws.amazon.com/codecommit/latest/userguide/setting-up-gc.html).

With the generated Git credentials downloaded, we are ready to clone our repository using the following terminal command:

#### Add the WebAPI CodeCommit repository as the remote for `~/environment/workshop/webapi`

Execute ONE of the following two commands, based on your chosen method of connection.

```sh
cd ~/environment/workshop/webapi/
```

_Note:_ If using HTTPS connection method, execute this command

```sh
git remote add origin _The HTTPS value for your API Respository_
```

_Note:_ If using SSH connection method, execute this command

```sh
git remote add origin _The SSH value for your API Respository_
```

## Building A Docker Image

Now, build the Docker image. This will use the file in the current directory called `Dockerfile` that tells Docker all of the instructions that should take place when the build command is executed.  Build the docker image using the conmand as follows:

```sh
docker build . -t $(aws sts get-caller-identity --query Account --output text).dkr.ecr.$(aws configure get region).amazonaws.com/mythicalmysfits/service:latest
```

Upon execution, you will now see docker download and install all of the necessary dependency packages that our application needs, and output the tag for the built image.  

**Action:** Copy the image tag for later reference. Below the example tag shown is: 111111111111.dkr.ecr.us-east-1.amazonaws.com/mythicalmysfits/service:latest**

```sh
Successfully built 8bxxxxxxxxab
Successfully tagged 111111111111.dkr.ecr.us-east-1.amazonaws.com/mythicalmysfits/service:latest
```

### Testing the Service Locally

Let's test our image locally to make sure everything is operating as expected. Copy the image tag that resulted from the previous command and run the following command to deploy the container locally:

```sh
docker run -p 8080:8080 REPLACE_ME_WITH_DOCKER_IMAGE_TAG
```

As a result you will see docker reporting that your container is up and running locally:

```sh
 * Running on http://0.0.0.0:8080/ (Press CTRL+C to quit)
```

To test our service with a local request, open up the above ip address in your browser of choice. Append /api/mysfits to the end of the URI in the address bar of the preview browser and hit enter:

If successful you will see a response from the service that returns the JSON document stored at `~/environment/workshop/webapi/mysfits-response.json`

When done testing the service you can stop it by pressing CTRL-C on PC or Mac.

## Module 2b: Deploying a Service with AWS Fargate

[AWS Fargate](https://aws.amazon.com/fargate/) is a compute engine for Amazon ECS that allows you to run containers without having to manage servers or clusters. With AWS Fargate, you no longer have to provision, configure, and scale clusters of virtual machines to run containers. This removes the need to choose server types, decide when to scale your clusters, or optimize cluster packing. AWS Fargate removes the need for you to interact with or think about servers or clusters. Fargate lets you focus on designing and building your applications instead of managing the infrastructure that runs them.

Amazon ECS has two launch types:

* Fargate launch type
* EC2 launch type

With Fargate launch type, all you have to do is package your application in containers, specify the CPU and memory requirements, define networking and IAM policies, and launch the application. For brevity in this workshop, we will use the Fargate launch type.

## Create Network Stack

Before we can create our service, we need to create the core infrastructure environment that the service will use, including the networking infrastructure in [Amazon VPC](https://aws.amazon.com/vpc/), and the AWS Identity and Access Management Roles that will define the permissions that ECS and our containers will have on top of AWS.  

The AWS CDK application you are about to write will create the following resources:

* **An Amazon VPC** - a network environment that contains four subnets (two public and two private) in the 10.0.0.0/16 private IP space, as well as all the needed Route Table configurations.  For brevity, a maximum of two AZs will be deployed to.
* **One NAT Gateway** - allows the containers we will eventually deploy into our private subnets to communicate out to the Internet to download necessary packages, etc.  For cost efficiency, only one NAT gateway will be deployed.
* **Security Groups** - Allows your docker containers to receive traffic on port 8080 from the Internet through the Network Load Balancer.
* **IAM Roles** - Identity and Access Management Roles are created. These will be used throughout the workshop to give AWS services or resources you create access to other AWS services like DynamoDB, S3, and more.

Let's start off by switching once again to our Workshop's CDK folder, and opening it in our editor:

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/cdk
```

```sh
code .
```

Create a new file in the `cdk/src/Cdk/` folder called `NetworkStack.cs`.

```sh
touch ~/environment/workshop/cdk/src/Cdk/NetworkStack.cs
```

### .NET Core References

We need to add a install the CDK NPM package for AWS EC2, doing so like below;

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/cdk/src/Cdk
```

```sh
dotnet add package Amazon.CDK.AWS.EC2
```

## Defining the Network Stack

Within the file you just created, define the Network Stack by typing/copying the code below.  We will step through the main parts shortly.

**Action:** Write/Copy the following code:

```c#
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
```

and update the `Program.cs` to reference the file

```c#
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
            app.Synth();
        }
    }
}

```

### Network Stack Walkthrough - VPC

Defining a VPC is super easy and one of the best ways to demonstrate the power and productivity gains achieved through using AWS CDK.  

AWS CDK makes the implementation of AWS Components and Services a breeze by providing you with high level abstractions.  Let's demonstrate this now.

A VPC can be defined in a simple one line statement.  Have a look at the code below:

```c#
this.vpc = new Vpc(this, "VPC");
```

Seriously!, that is all you need to define a VPC!  

Let's now run the `cdk synth` command to observe what this single line generates.  In a terminal window, navigate to `~/environment/workshop/cdk/` and run the following command:

__Note__: We are assigning the instance of our VPC to a local property so that it may be referenced by other stacks.

**Action:** Execute the following commands:

```sh
dotnet build src
```

```sh
cdk synth MythicalMysfits-NetworkStack -o templates
```

This command will generate the AWS CloudFormation template for the NetworkStack and place it in a folder called templates.  Open the generated file now and review the contents.  

In the generated file, you will find that one line of code generated a huge amount of AWS CloudFormation, including:

* A VPC construct;
* Public, private and isolated subnets in each of the availability zones in your region
* Routing tables for each of the subnets
* NAT and Internet Gateways for each AZ

But we want to customise the VPC created by adding some property overrides.  We change the VPC definiton to reflect the following:

**Action:** Change your VPC definition to reflect the following code:

```c#
this.vpc = new Vpc(this, "VPC", new VpcProps
{
    NatGateways = 1,
    MaxAzs = 2
});
```

Here we are defining the maximum number of NAT Gateways we want to establish and the maximum number of AZs we want to deploy to.

## Defining the Amazon Elastic Container Registry (ECR) Stack

Later in this module we will be generating a docker image that contains our .NET Web API, but first we need somewhere to put it.  The place for our image is within an Amazon Elastic Container Registry which we will now create using AWS CDK.

As before, let's create a new file within the `cdk/src/Cdk` folder, this time called `EcrStack.cs`.  

**Action:** Execute the following command:

```sh
touch ~/environment/workshop/cdk/lib/EcrStack.cs
```

We need to add a install the CDK NPM package for AWS ECR, doing so like below;

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/cdk/src/Cdk
```

```sh
dotnet add package Amazon.CDK.AWS.ECR
```

Write/Copy the following code to define the ECR Stack.

**Action:** Write/Copy the following code:

```c#
using Amazon.CDK;
using Amazon.CDK.AWS.ECR;

namespace Cdk
{
    internal class EcrStack : Stack
    {
        public Repository ecrRepository { get; }

        public EcrStack(Construct parent, string id) : base(parent, id)
        {
            this.ecrRepository = new Repository(this, "Repository", new RepositoryProps
            {
                RepositoryName = "mythicalmysfits/service"
            });
        }
    }
}
```

Then, add the ECRStack to our CDK application definition in `cdk/src/Cdk/program.cs`, as we have done before.  When done, your `cdk/src/Cdk/program.cs` should look like this;

**Action:** Write/Copy the following code:

```c#
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
            app.Synth();
        }
    }
}

```

#### ECR Stack Walkthrough

Our definition of the Elastic Container Registry is relatively simple:

**Action:** Write/Copy the following code:

```c#
this.ecrRepository = new Repository(this, "Repository", new RepositoryProps
{
    RepositoryName = "mythicalmysfits/service"
});
```

We assign the ECR Repository to a local read-only property so that other stacks can reference it.

#### Deployment the ECR Stack

Now, deploy your ECR using the following command.

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/cdk
```

```sh
dotnet build src/
```

And now deploy the ECR stack

**Action:** Execute the following command:

```sh
cdk deploy MythicalMysfits-ECR
```

In your browser, go to the ECR Dashboard and verify you can see the ECR you just created in the list.

#### Pushing the Docker Image to Amazon ECR

Earlier in this module we successful tested our WebAPI docker image locally, so now we are ready to push our container image to the Amazon Elastic Container Registry (Amazon ECR) we have just created.

Run the below command using either the AWS CLI command:

_Note:_ If you use `Bash`, execute the following command:

```sh
$(aws ecr get-login --no-include-email)
```

Next, push the image you created to the ECR repository using the copied tag from above. Using this command, docker will push your image and all the images it depends on to Amazon ECR:

**Action:** Execute the following command:

```sh
docker push $(aws sts get-caller-identity --query Account --output text).dkr.ecr.$(aws configure get region).amazonaws.com/mythicalmysfits/service:latest
```

Run the following command to see your newly pushed docker image stored inside the ECR repository using either the AWS CLI command:

```sh
aws ecr describe-images --repository-name mythicalmysfits/service
```

Now,  we have an image available in ECR that we can create and deploy to a service hosted on Amazon ECS using AWS Fargate.  The same service you tested locally via the terminal in Visual Studio Code as part of the last module will be deployed in the cloud and publicly available behind a Network Load Balancer.

## Module 2c - Create an AWS Fargate Elastic Container Service (ECS) Stack

First, we will create a **Cluster** in the **Amazon Elastic Container Service (ECS)**. This represents the cluster of “servers” that your service containers will be deployed to. Servers is in "quotations" because you will be using **AWS Fargate**. Fargate allows you to specify that your containers be deployed to a cluster without having to actually provision or manage any servers yourself.

Now, let's define our ECS instance.  But first, we need to add a install the CDK NPM packages for AWS ECS, doing so like below;

**Action:** Execute the following commands:

```sh
cd ~/environment/workshop/cdk/src/Cdk
```

```sh
dotnet add package Amazon.CDK.AWS.ECS
```

```sh
dotnet add package Amazon.CDK.AWS.ECS.Patterns
```

As before, let's create a new file within the `Cdk` folder, this time called `EcsStack.cs`.  

**Action:** Execute the following command:

```sh
touch ~/environment/workshop/cdk/lib/EcsStack.cs
```

Define the skeleton structure of a CDK Stack.

**Action:** Write/Copy the following code:

```c#
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
        public EcrStack(Construct parent, string id) : base(parent, id)
        {
        }
    }
}
```

This time, the stack we are creating depends on two previous stacks that we have created.  The recommended approach for importing dependencies and properties into a stack is via a properties construct.  Let's define that now.  

Above your EcsStack definition, import the following modules:

**Action:** Write/Copy the following code:

```c#
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
```

And define the following properties object.

**Action:** Write/Copy the following code:

```c#
public class EcsStackProps
{
    public Repository ecrRepository { get; set; }
    public Vpc Vpc { get; set; }
}
```

Now change the constructor of your EcsStack to require your properties object.

**Action:** Write/Copy the following code:

```c#
    public EcsStack(Construct parent, string id, EcsStackProps props) : base(parent, id)
```

Define two properties at the top of your EcsStack that expose the ecsCluster and ecsService for other stacks to utilise later in the workshop.

**Action:** Write/Copy the following code:

```c#
    public Cluster ecsCluster { get; }
    public NetworkLoadBalancedFargateService ecsService { get; }
```

Now we define our ECS Cluster object.

**Action:** Write/Copy the following code:

```c#
this.ecsCluster = new Cluster(this, "Cluster", new ClusterProps {
    Vpc = props.Vpc,
});
this.ecsCluster.Connections.AllowFromAnyIpv4(Port.Tcp(8080));
```

Notice how we reference the VPC (`props.vpc`) defined in the `EcsStackProps`.  [AWS CDK](https://aws.amazon.com/cdk/) will automatically create a reference here between the CloudFormation objects.  Also notice that we assign the instance of the ecs.Cluster created to a local property so that it can be referenced by this and other stacks.

**Action:** Write/Copy the following code:

```c#
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
```

Notice here the definition of container ports and the customisation of the EC2 SecurityGroups rules created by AWS CDK to limit permitted requests to the cidr block of the VPC we created.

Now we will add some additional IAM Policy Statements to the Execution and Task Roles.

**Action:** Write/Copy the following code:

```c#
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
```

Then, add the EcsStack to our CDK application definition in `cdk/src/Cdk/Program.cs`, as we have done before.  When done, your `cdk/src/Cdk/Program.cs` should look like this;

**Action:** Write/Copy the following code:

```c#
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
            app.Synth();
        }
    }
}
```

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/cdk
```

```sh
cd ~/environment/workshop/cdk/src/
```

```sh
dotnet build
```

#### Creating a Service Linked Role for ECS

If you have already used ECS in the past you can skip over this step and move on to the next step. If you have never used ECS before, we need to create an service linked role in IAM that grants the ECS service itself permissions to make ECS API requests within your account. This is required because when you create a service in ECS, the service will call APIs within your account to perform actions like pulling docker images, creating new tasks, etc.

Without creating this role, the ECS service would not be granted permissions to perform the actions required. To create the role, execute the following command in the terminal:

```sh
aws iam create-service-linked-role --aws-service-name ecs.amazonaws.com
```

If the above returns an error about the role existing already, you can ignore it, as it would indicate the role has automatically been created in your account in the past.

**Action:** Execute the following command:

```sh
cdk deploy MythicalMysfits-ECS
```

You will be prompted with a messages such as `Do you wish to deploy these changes (y/n)?` to which you should respond by typing `y`

After your service is created, ECS will provision a new task that's running the container you've pushed to ECR and register it to the created NLB.  

### Test the Service

Copy the DNS name you saved when creating the NLB and send a request to it using your browser of choice. Try sending a request to the mysfits resource using either the AWS CLI command:

_Note:_ If you use `Bash`, execute the following command:

```sh
curl http://<replace-with-your-nlb-address>/api/mysfits
```

A response showing the same JSON response we received earlier when testing the docker container locally in our browser means your .NET Web API is up and running on AWS Fargate.

>Note: This Network Load Balancer only supports HTTP (http://) requests since no SSL/TLS certificates are installed on it. For this tutorial, be sure to submit requests using http:// only, https:// requests will not work properly.

## Module 2d - Update Mythical Mysfits to Call the NLB

Next, we need to integrate our Angular app with your new API backend instead of using the hardcoded Mysfit profile data. We need to edit our `enviroment` file in our Angular app to include a `mysfitsApiUrl` property and re-publish it to our S3 bucket so the endpoint will now point to our NLB.  

### Update the web application from module 2 source

**Action:** Execute the following command:

```sh
cp -r ~/environment/workshop/source/module-2/frontend/* ~/environment/workshop/frontend/
```

### Replace the API Endpoint and Upload to S3

Navigate to the `environments` folder in the Angular app.

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/frontend/src/environments
```

We'll copy the existing local `environment.ts` file to create our `prod` version.

**Action:** Execute the following command:

```sh
cp ./environment.ts ./environment.prod.ts
```

Open the `environment.prod.ts` file in Visual Studio Code and replace the `mysfitsApiUrl` value with your NLB endpoint and add `/api` to end of it. Also change `production` to be `true`. Remember do not include the `/mysfits` path.

![angular-update](/images/module-2/replace-mysfits-api-url.png)

After replacing the endpoint to point at your NLB and adding `/api`, deploy your updated angular app:

Since we use `npm run build -- --prod` to build the Angular app, we'll need to create a `prod` version of the `environment` file.

**Action:** Execute the following commands:

```sh
cd ~/environment/workshop/frontend
```

```sh
npm install
npm run build -- --prod
```

```sh
cd ~/environment/workshop/cdk
```

```sh
npm run build
```

And now deploy the updated WebApplication stack

```sh
cdk deploy MythicalMysfits-WebApplication
```

## Module 2e: Automating Deployments using AWS Code Services

![Architecture](/images/module-2/architecture-module-2b.png)

### Creating the CI/CD Pipeline

Now that you have a service up and running, you may think of code changes that you'd like to make to your .NET service.  It would be a bottleneck for your development speed if you had to go through all of the same steps above every time you wanted to deploy a new feature to your service. That's where Continuous Integration and Continuous Delivery or CI/CD come in!

In this module, you will create a fully managed CI/CD stack that will automatically deliver all of the code changes that you make to your code base to the service you created during the last module.  

Let's start off by switching once again to our Workshop's CDK folder, and opening it in our editor:

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/cdk/src/Cdk/
```

Create a new file in the `cdk/src/Cdk` folder called `CiCdStack.cs`.

```sh
touch ~/environment/workshop/cdk/lib/CiCdStack.cs
```

__Note__ As before, you may find it helpful to run the command `npm run watch` from within the CDK folder to provide compile time error reporting whilst you develop your AWS CDK constructs.  We recommend running this from the terminal window within VS Code.

First, we need to add a install the CDK NPM package for AWS EC2, doing so like below;

**Action:** Execute the following commands:

```sh
cd ~/environment/workshop/cdk/src/Cdk
```

```sh
dotnet add package Amazon.CDK.AWS.CodeBuild
```

```sh
dotnet add package Amazon.CDK.AWS.CodePipeline
```

```sh
dotnet add package Amazon.CDK.AWS.CodePipeline
```

Within the file you just created, write/copy the following code to define the class  `CiCdStack`.  We will step through the main aspects in a moment:

**Action:** Write/Copy the following code:

```c#
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
```

With the CiCdStack defined, let's step through what we have written.

### Imports

We imported the assemblies that we require; 

```c#
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.CodeCommit;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.IAM;
```

### Interface

We defined the interface for the properties our stack will require

```c#
public class CiCdStackProps : StackProps
{
    public Amazon.CDK.AWS.ECR.Repository ecrRepository { get; internal set; }
    public FargateService ecsService { get; internal set; }
    public string apiRepositoryArn { get; internal set; }
}
```

### Constructor

Change the constructor of your EcsStack to require your properties object.

```c#
public CiCdStack(Construct parent, string id, CiCdStackProps props) : base(parent, id, props)
```

### Defining the CICD pipeline

Now, we define our CiCd pipeline using AWS CDK.  Once again, AWS CDK makes the implementation of AWS Components and Services a breeze by providing you with high level abstractions.  Let's demonstrate this now.

Within the `CiCdStack` file, we have the following code to import the CodeCommit repository we use for our API code

```c#
var apiRepository = Amazon.CDK.AWS.CodeCommit.Repository.FromRepositoryArn(this, "Repository", props.apiRepositoryArn);
```

The following code defines our CodeBuild project to compile our .NET Core WebAPI upon change

```c#
const codebuildProject = new codebuild.PipelineProject(this, "BuildProject", {
  projectName: "MythicalMysfitsServiceCodeBuildProject",
  environment: {
    computeType: codebuild.ComputeType.SMALL,
    buildImage: codebuild.LinuxBuildImage.UBUNTU_14_04_PYTHON_3_5_2,
    privileged: true,
    environmentVariables: {
      AWS_ACCOUNT_ID: {
        type: codebuild.BuildEnvironmentVariableType.PLAINTEXT,
        value: cdk.Aws.ACCOUNT_ID
      },
      AWS_DEFAULT_REGION: {
        type: codebuild.BuildEnvironmentVariableType.PLAINTEXT,
        value: cdk.Aws.REGION
      }
    }
  }
});
```

We then provide the codebuild project with permissions to query the codecommit repository.

```c#
const codeBuildPolicy = new iam.PolicyStatement();
codeBuildPolicy.addResources(apiRepository.repositoryArn)
codeBuildPolicy.addActions(
  "codecommit:ListBranches",
  "codecommit:ListRepositories",
  "codecommit:BatchGetRepositories",
  "codecommit:GitPull"
)
codebuildProject.addToRolePolicy(
  codeBuildPolicy
);
```

And provide the codebuild project with permissions to pull and push images to the ECR repository

```c#
props.ecrRepository.grantPullPush(codebuildProject.grantPrincipal);
```

We define the Pipeline Source action which tells CodePipeline where to obtain its source from.

```c#
const sourceOutput = new codepipeline.Artifact();
const sourceAction = new actions.CodeCommitSourceAction({
  actionName: "CodeCommit-Source",
  branch: "master",
  trigger: actions.CodeCommitTrigger.POLL,
  repository: apiRepository,
  output: sourceOutput
});
```

We define the Pipeline Build action to inform CodePipeline and CodeBuild how to build the WebAPI code and docker image.

```c#
const buildOutput = new codepipeline.Artifact();
const buildAction = new actions.CodeBuildAction({
  actionName: "Build",
  input: sourceOutput,
  outputs: [
    buildOutput
  ],
  project: codebuildProject
});
```

We define the ECS deployment action to instruct the CodePipeline how to deploy the output of the BuildAction to the CodePipeline instance.

```c#
const deployAction = new actions.EcsDeployAction({
  actionName: "DeployAction",
  service: props.ecsService,
  input: buildOutput
});
```

Finally we define the CodePipeline and stich all the stages/actions together.

```c#
const pipeline = new codepipeline.Pipeline(this, "Pipeline", {
  pipelineName: "MythicalMysfitsPipeline"
});
pipeline.addStage({
  stageName: "Source",
  actions: [sourceAction]
});
pipeline.addStage({
  stageName: "Build",
  actions: [buildAction]
});
pipeline.addStage({
  stageName: "Deploy",
  actions: [deployAction]
});
```

Then, add the CiCdStack to our CDK application definition in `cdk/src/Cdk/Program.cs`, when done, your `cdk/src/Cdk/Program.cs` should look like this;

**Action:** Write/Copy the following code:

```c#
#!/usr/bin/env node

import cdk = require('@aws-cdk/core');
import "source-map-support/register";
import { DeveloperToolsStack } from "../lib/developer-tools-stack";
import { WebApplicationStack } from "../lib/web-application-stack";
import { EcrStack } from "../lib/ecr-stack";
import { EcsStack } from "../lib/ecs-stack";
import { NetworkStack } from "../lib/network-stack";

const app = new cdk.App();
const developerToolStack = new DeveloperToolsStack(app, 'MythicalMysfits-DeveloperTools');
new WebApplicationStack(app, "MythicalMysfits-WebApplication");
const networkStack = new NetworkStack(app, "MythicalMysfits-Network");
const ecrStack = new EcrStack(app, "MythicalMysfits-ECR");
const ecsStack = new EcsStack(app, "MythicalMysfits-ECS", {
    vpc: networkStack.vpc,
    ecrRepository: ecrStack.ecrRepository,
});
new CiCdStack(app, "MythicalMysfits-CICD", {
    ecrRepository: ecrStack.ecrRepository,
    ecsService: ecsStack.ecsService.service,
    apiRepositoryArn: developerToolStack.apiRepository.repositoryArn,
});
```

### Deploy the CI/CD Pipeline

Make sure our TypeScript has been compiled.

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/cdk
```

```sh
npm run build
```

And now deploy the CICD stack

**Action:** Execute the following command:

```sh
cdk deploy MythicalMysfits-CICD
```

### Test the CI/CD Pipeline

Now the completed service code that we used to create our Fargate service in Module 2 is stored in the local repository that we just cloned from AWS CodeCommit.  Let's make a change to the .NET service before committing our changes to demonstrate that the CI/CD pipeline we've created is working. In Visual Studio Code, open the file stored at `~/environment/workshop/api/service/mysfits-response.json` and change the age of one of the mysfits to another value and save the file.

After saving the file, change directories to the new repository directory:

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/webapi
```

Then, run the following git commands to push in your code changes.  

**Action:** Execute the following commands:

```sh
git add .
```

```sh
git commit -m "I changed the age of one of the mysfits."
```

```sh
git push
```

After the change is pushed into the repository, you can open the CodePipeline service in the AWS Console to view your changes as they progress through the CI/CD pipeline. After committing your code change, it will take about 5 to 10 minutes for the changes to be deployed to your live service running in Fargate. Refresh your Mythical Mysfits website in the browser to see that the changes have taken effect.

You can view the progress of your code change through the [AWS CodePipeline](https://console.aws.amazon.com/codepipeline/home) console -- no actions needed, just watch the automation in action!

This concludes Module 2.

[Proceed to Module 3](/module-3)
