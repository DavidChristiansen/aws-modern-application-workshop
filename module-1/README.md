# Module 1: IDE Setup and Static Website Hosting

![Architecture](/images/module-1/architecture-module-1.png)

**Time to complete:** 20 minutes

**Services used:**

* [AWS Cloud9](https://aws.amazon.com/cloud9/)
* [Amazon Simple Storage Service (S3)](https://aws.amazon.com/s3/)
* [Amazon CloudFront](https://aws.amazon.com/cloudfront/)

In this module, follow the instructions to create your cloud-based IDE on [AWS Cloud9](https://aws.amazon.com/cloud9/) and deploy the initial version of our Mythical Mysfits website.  [Amazon S3](https://aws.amazon.com/s3/) is a highly durable, highly available, and inexpensive object storage service that can serve stored objects directly via HTTP.  Amazon CloudFront is a highly-secure CDN that provides both network and application level protection. Your traffic and applications benefit through a variety of built-in protections such as AWS Shield Standard, at no additional cost. You can also use configurable features such as AWS Certificate Manager (ACM) to create and manage custom SSL certificates at no extra cost.

The combination of S3 and CloudFront makes for a wonderfully useful capability for serving static web content (html, js, css, media content, etc.) directly to web browsers for sites on the Internet.  We will utilize S3 to host the content and the fast content delivery network (CDN) service, CloudFront, to securely deliver our Mythical Mysfits website to customers globally with low latency, high transfer speeds.

## Getting Started: Configuring your environment for Mythical Mysfits

### Sign In to the AWS Console

To begin, sign in to the [AWS Console](https://console.aws.amazon.com) for the AWS account you will be using in this workshop.

This web application can be deployed in any AWS region that supports all the services used in this application. The supported regions include:

* us-east-1 (N. Virginia)
* us-east-2 (Ohio)
* us-west-2 (Oregon)
* ap-southeast-1 (Singapore)
* ap-northeast-1 (Tokyo)
* eu-central-1 (Frankfurt)
* eu-west-1 (Ireland)

Select a region from the dropdown in the upper right corner of the AWS Management Console.

### Creating your Mythical Mysifts IDE

#### Create a new AWS Cloud9 Environment

On the AWS Console home page, type **Cloud9** into the service search bar and select it:
![aws-console-home](/images/module-1/cloud9-service.png)

Click **Create Environment** on the Cloud9 home page:
![cloud9-home](/images/module-1/cloud9-home.png)

Name your environment **MythicalMysfitsIDE** with any description you'd like, and click **Next Step**:
![cloud9-name](/images/module-1/cloud9-name-ide.png)

Leave the Environment settings as their defaults and click **Next Step**:
![cloud9-configure](/images/module-1/cloud9-configure-env.png)

Click **Create Environment**:
![cloud9-review](/images/module-1/cloud9-review.png)

When the IDE has finished being created for you, you'll be presented with a welcome screen that looks like this:
![cloud9-welcome](/images/module-1/cloud9-welcome.png)

#### Cloning the Mythical Mysfits Workshop Repository

In the bottom panel of your new Cloud9 IDE, you will see a terminal command line terminal open and ready to use. First, let's create a directory within which we will store of the files created and used with this workshop:

```sh
mkdir workshop && cd workshop
```

Run the following git command in the terminal to clone the necessary code to complete this tutorial:

```sh
git clone -b dotnet-cdk-csharp https://github.com/aws-samples/aws-modern-application-workshop.git source
```

After cloning the repository, you'll see that your project explorer now includes the files cloned:
![cloud9-explorer](/images/module-1/cloud9-explorer.png)

### Install required tools

* [AWS CLI](https://aws.amazon.com/cli/)
* [Node.js and NPM](https://nodejs.org/en/) - v12 or greater
* [.NET Core 2.1](https://www.microsoft.com/net/download)
* [AWS CDK](https://docs.aws.amazon.com/CDK/latest/userguide/getting_started.html)
  * npm install -g aws-cdk
* [Angular CLI](https://cli.angular.io/)
  * npm install -g @angular/cli

Now, let's prepare the Single Page Web Application we want to host.

## Single Page Web Application (SPA)

In the module-1 folder, the `frontend` folder includes a full-featured [Angular](https://angular.io/) application. This application was generated using `ng new`, and we added the basic features for the Mythical Mysfits web app. Included is the [Bootstrap framework](https://getbootstrap.com/) and a popular Angular library [ng-bootstrap](https://ng-bootstrap.github.io/#/home). Both give pre-built UX features and layout options.

### Overview of the `frontend`

This version of the frontend has all of the Mysfits data hardcoded into the `MythicalMysfitProfileService` (located at `frontend/src/app/services/mythical-mysfit-profile.service.ts`). This is an injectable [Angular service](https://angular.io/tutorial/toh-pt4) created to work with Mysfit profile data. In the following modules, we'll update this service to pull the Mysfits data from an API we create.

### Copy the Web Application

**Action:** Copy the web application source code from the module 1 directory

```sh
mkdir ~/environment/workshop/frontend
```

```sh
cd ~/environment/workshop/frontend
```

```sh
git init
```

```sh
cp -r ~/environment/workshop/source/module-1/frontend/* ~/environment/workshop/frontend/
```

```sh
git add .
```

```sh
git commit -m 'Initial commit of frontend code'
```

### Configure the Web Application

Before you can build and publish your Angular app, you will need to create a production Angular environment file located in your `~/environment/workshop/frontend/src/environments/` folder. Make sure the file is named `environment.prod.ts`.

**Action:** Create a file `~/environment/workshop/frontend/src/environments/environment.prod.ts`

```sh
touch ~/environment/workshop/frontend/src/environments/environment.prod.ts
```

**Action:** Open the `environment.prod.ts` file in Cloud9 and copy the properties from the `environment.ts` file located in the same folder. The property at this point should only be the following:

```sh
code ~/environment/workshop/frontend/src/environments/environment.prod.ts
```

**Action:** In `environment.prod.ts`, copy the following code.

```js
export const environment = {
  production: false
};
```

**Action:** In `environment.prod.ts`, change `production` to `true`.

**Action:** Add this environment file to the git repo.

```sh
git add ~/environment/workshop/frontend/src/environments/environment.prod.ts
```

```sh
git commit -m 'Addition of environment.prod.ts'
```

### Build your Web Application

**Action:** Ensure you are in the `~/environment/workshop/frontend/src` folder

```sh
cd ~/environment/workshop/frontend/
```

**Action:** Run `npm install` to install all the prerequisites for your web application

```sh
npm install
```

**Action:** Run `npm run build -- --prod` to create a `production` build of your Angular application.

```sh
npm run build -- --prod
```

Your web application is now ready to deploy.  We will use the code  you just generated in the next step.

## Infrastructure As Code

Next, we will create the infrastructure components needed for creating a GIT repository for your web application code, the hosting of a static website in Amazon S3 and delivering that content to your customers via the CloudFront Content Delivery Network (CDN).  To achieve this we will generate our Infrastructure as Code using a tool called AWS CloudFormation.

### AWS CloudFormation

AWS CloudFormation is a service that can programmatically provision AWS resources that you declare within JSON or YAML files called *CloudFormation Templates*, enabling the common best practice of *Infrastructure as Code*.  AWS CloudFormation enables you to:

* Create and provision AWS infrastructure deployments predictably and repeatedly.
* Leverage AWS products such as Amazon EC2, Amazon Elastic Block Store, Amazon SNS, Elastic Load Balancing, and Auto Scaling.
* Build highly reliable, highly scalable, cost-effective applications in the cloud without worrying about creating and configuring the underlying AWS infrastructure.
* Use a template file to create and delete a collection of resources together as a single unit (a stack).

### AWS Cloud Development Kit ([AWS CDK](https://aws.amazon.com/cdk/))

To generate our CloudFormation, we will utilise the [AWS Cloud Development Kit](https://aws.amazon.com/cdk/) (also known as AWS CDK).  The AWS CDK is an open-source software development framework to define cloud infrastructure in code and provision it through AWS CloudFormation. The CDK integrates fully with AWS services and offers a higher level object-oriented abstraction to define AWS resources imperatively. Using the CDK’s library of infrastructure constructs, you can easily encapsulate AWS best practices in your infrastructure definition and share it without worrying about boilerplate logic. The CDK improves the end-to-end development experience because you get to use the power of modern programming languages to define your AWS infrastructure in a predictable and efficient manner.

The CDK can be used to define your cloud resources using one of the supported programming languages: C#/.NET, Java, JavaScript, Python, or TypeScript.  Developers can use one of the supported programming languages to define reusable cloud components known as Constructs. You compose these together into Stacks and Apps.

One of the biggest benefits from AWS CDK is the principal of reusability - Being able to write, reuse and share components throughout your application and team.  These components are referred to as Constructs within AWS CDK.  To this end, the code we will write in Module 1 will be reused throughout all remaining modules.

### Initialise CDK App folder

**Action:** Switch back to your `Root Folder`

```sh
cd ~/environment/workshop
```

**Action:** Create a new folder to contain your AWS CDK application

```sh
mkdir ./cdk
```

**Action:** Switch to your AWS CDK application directory

```sh
cd ./cdk
```

#### Install AWS CDK

**Action:** If you haven't already, install the AWS CDK using the following command.

```sh
npm install -g aws-cdk
```

**Action:** Run the following command to see the version number of the CDK.

```sh
cdk --version
```

In the cdk folder, lets now initialize a cdk app, where LANGUAGE is one of the supported programming languages: csharp (C#), java (Java), python (Python), or typescript (TypeScript) and TEMPLATE is an optional template that creates an app with different resources than the default app that cdk init creates for the language.  For the purposes of this workshop we will use _CSharp_ as our language.

**Action:** Execute the following command:

```sh
cdk init app --language=csharp
```

This command has now initialised a new CDK app in your `~/environment/workshop/cdk` folder.  Part of the initialisation process also establishes the given directory as a new git repository.

#### AWS CDK folder structure

Now, let's implement the code to host our web application.  

Notice the standard structure of an C# AWS CDK app, that consists of a `src` folder.

* The `src` folder is where CDK defines the default application structure.

### Code the GIT repositories for our CDK and our Web applications (DeveloperToolsStack)

Next, within the src/Cdk folder, create a file called `DeveloperToolsStack.cs`.  

**Action:** Execute the following command:

```sh
touch src/Cdk/DeveloperToolsStack.cs
```

Open this file in Cloud9 and define a new construct called `DeveloperToolsStack`, as illustrated below.

**Action:** Write/Copy the following code:

```c#
using Amazon.CDK;

namespace Cdk
{
    internal class DeveloperToolsStack : Stack
    {
        public DeveloperToolsStack(Construct parent, string id) : base(parent, id)
        {
        }
    }
}
```

Following the change of filename and classname, you should now update the references in the `Program.cs` file, as such.

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
            app.Synth();
        }
    }
}
```

### Source Version Control

AWS CodeCommit is a version control service that enables you to privately store and manage Git repositories in the AWS cloud. For further information on CodeCommit, see the [AWS CodeCommit documentation](https://docs.aws.amazon.com/codecommit).

Next, we will define the code that generates the CodeCommit repository.  But before we do that, we must add a reference to the appropriate `@aws-cdk/aws-codecommit` npm package.

**Action:** Execute the following command from the `~/environment/workshop/cdk/src/Cdk` directory

```sh
dotnet add package Amazon.CDK.AWS.CodeCommit
```

and add the _using_ statement to the DeveloperTooksStack.cs file

```c#
using Amazon.CDK.AWS.CodeCommit;
```

Once that has completed, let's proceed with defining our AWS CodeCommit repositories.  The AWS CDK consists of a comprehensive array of high level abstractions that both simplify the implementation of your CloudFormation templates as well as providing you with granular control over the resources you generate.

So, to define the CodeCommit repositories we need for this workshop, write the following in the `developer-tools-stack.ts` file.

**Action:** Write/Copy the following code:

```c#
var cdkRepository = new Repository(this, "CDKRepository", new RepositoryProps {
    RepositoryName = Amazon.CDK.Aws.ACCOUNT_ID + "-MythicalMysfitsService-Repository-CDK"
});

var webRepository = new Repository(this, "WebRepository", new RepositoryProps {
    RepositoryName = Amazon.CDK.Aws.ACCOUNT_ID + "-MythicalMysfitsService-Repository-Web"
});

this.apiRepository = new Amazon.CDK.AWS.CodeCommit.Repository(this, "APIRepository", new RepositoryProps {
    RepositoryName = Amazon.CDK.Aws.ACCOUNT_ID + "-MythicalMysfitsService-Repository-API"
});

this.lambdaRepository = new Amazon.CDK.AWS.CodeCommit.Repository(this, "LambdaRepository", new RepositoryProps {
    RepositoryName = Amazon.CDK.Aws.ACCOUNT_ID + "-MythicalMysfitsService-Repository-Lambda"
});
```

Just before the constructor of the construct, type the following statements which will allow other constructs to reference the repositories:

```c#
public Repository apiRepository { get; }
public Repository lambdaRepository { get; }
```

We can have the generated CloudFormation template provide the clone urls for the generated CodeCommit respositories by defining custom output properties defining `cdk.CfnOutput` constructs, as demonstrated in the code snippet below:

```c#
new CfnOutput(this, "CDKRepositoryCloneUrlHttp", new CfnOutputProps()
{
    Description = "CDK Repository CloneUrl HTTP",
    Value = cdkRepository.RepositoryCloneUrlHttp
});
```

Declare `CfnOutput` for the HTTP and SSH clone URLs for each of your repositories.  Once done, your file should look something list the code block bellow.

```c#
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
```

### Commit your CDK Application to source control

```sh
cd ~/environment/workshop/cdk/
```

```sh
git add .
```

```sh
git commit -m 'Initial commit of CDK code'
```

### Deploy the GIT repositories

Before we deploy our GIT repositories we can view the CloudFormation template that will be generated by executing the `cdk synth` command.  Do this now

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/cdk
dotnet build src
cdk synth MythicalMysfits-DeveloperTools
```

We can deploy the `MythicalMysfits-DeveloperToolsStack` by executing the `cdk deploy` command from within the `cdk` folder and defining the stack we wish to deploy:

**Action:** Execute the following command:

```sh
cdk deploy MythicalMysfits-DeveloperTools
```

You may be prompted with a messages such as `Do you wish to deploy these changes (y/n)?` to which you should respond by typing `y`

### Bind to your new GIT Repository remotes

Now that you have created your CodeCommit repositories, we want to now connect them to your local repositories.  This involves selecting a method by which you wish to communicate (HTTPS or GIT/SSH) and then lastly adding the CodeCommit repository as a remote and pushing your changes.

#### Choose connection method

Choose between connecting to your AWS CodeCommit repositories via HTTPS or SSH.  The easiest way to set up CodeCommit is to configure HTTPS Git credentials for AWS CodeCommit.

The HTTPS authentication method:

* Uses a static user name and password.
* Works with all operating systems supported by CodeCommit.
* Is also compatible with integrated development environments (IDEs) and other development tools that support Git credentials.

Refer to [https://docs.aws.amazon.com/codecommit/latest/userguide/setting-up.html](https://docs.aws.amazon.com/codecommit/latest/userguide/setting-up.html) for details on how to configure for connections using HTTP.

Refer to [https://docs.aws.amazon.com/codecommit/latest/userguide/setting-up.html#setting-up-standard](https://docs.aws.amazon.com/codecommit/latest/userguide/setting-up.html#setting-up-standard) for details on how to configure connections via GIT Credentials (SSH)

### Add your CodeCommit Repo to ~/environment/workshop/frontend

```sh
cd ~/environment/workshop/frontend
```

Execute ONE of the following two commands, based on your chosen method of connection.

_Note:_ If using HTTPS connection method, execute this command

```sh
git remote add origin <<Your Web CodeCommit HTTPS Repository Clone URL>
```

_Note:_ If using SSH connection method, execute this command

```sh
git remote add origin <<Your Web CodeCommit SSH Repository Clone URL>
```

```sh
git push --set-upstream origin master
```

### Add your CodeCommit Repo to ~/environment/workshop/cdk

```sh
cd ~/environment/workshop/cdk
```

Execute ONE of the following two commands, based on your chosen method of connection.

_Note:_ If using HTTPS connection method, execute this command

```sh
git remote add origin <<Your CDK CodeCommit HTTPS Repository Clone URL>
```

_Note:_ If using SSH connection method, execute this command

```sh
git remote add origin <<Your CDK CodeCommit HTTPS Repository Clone URL>
```

```sh
git push --set-upstream origin master
```

## Code the Web Application Infrastructure

Now, let's define the infrastructure needed to host our website.  

Create a new file called `WebApplicationStack.cs` in the `lib` folder and define the skeleton class structure, by writing/copying the following code:

**Action:** Write/Copy the following code:

```c#
using System.IO;
using Amazon.CDK;

namespace Cdk
{
    internal class WebApplicationStack : Stack
    {
        public WebApplicationStack(Construct parent, string id) : base(parent, id)
        {

        }
    }
}
```

Add an import statement for the `WebApplicationStack` to the `Program.cs` file.

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
            app.Synth();
        }
    }
}
```

Now we have the required files, let's go through defining the S3 and CloudFront infrastructure.  But before we do that, we must add references to the appropriate npm packages that we will be using.

Execute the following commands from the `~/environment/workshop/cdk/src/Cdk` directory

**Action:** Execute the following command:

```sh
dotnet add package Amazon.CDK.AWS.IAM;
dotnet add package Amazon.CDK.AWS.S3;
dotnet add package Amazon.CDK.AWS.S3.Deployment;
dotnet add package Amazon.CDK.AWS.CloudFront;
```

In the `WebApplicationStack.cs` file, import the AWS CDK libraries we will be using.  At the top of the file

**Action:** Write/Copy the following code:

```c#
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Amazon.CDK.AWS.CloudFront;
```

### Define the S3 bucket

We are going to define our S3 bucket and define the web index document as 'index.html'

**Action:** Write/Copy the following code:

```c#
// Create a S3 bucket, with the given name and define the web index document as 'index.html'
var bucket = new Bucket(this, "Bucket", new BucketProps
{
    WebsiteIndexDocument = "index.html"
});
```

### Restrict access to the S3 bucket

We want to restrict access to our S3 bucket, and only allow access from the CloudFront distribution. We'll use an [Origin Access Identity (OAI)](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/private-content-restricting-access-to-s3.html) to allow CloudFront to access and serve files to our users.

**Action:** Within the `WebApplicationStack.cs` constructor write the folllowing code:

```c#
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
```

### CloudFront Distribution

Next, Write the definition for a new CloudFront web distribution:

**Action:** Write/Copy the following code:

```c#
var cdn = new CloudFrontWebDistribution(this, "CloudFront", new CloudFrontWebDistributionProps{
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
```

### Upload the website content to the S3 bucket

Now we want to use a handy CDK helper that takes the defined source directory, compresses it, and uploads it to the destination s3 bucket:

```c#
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
```

### CloudFormation Outputs

Finally, we want to define a cloudformation output for the domain name assigned to our CloudFront distribution:

**Action:** Write/Copy the following code:

```c#
// Create a CDK Output which details the URL for the CloudFront Distribtion URL.
new CfnOutput(this, "CloudFrontURL", new CfnOutputProps
{
    Description = "The CloudFront distribution URL",
    Value = "http://" + cdn.DomainName
});
```

With that, we have completed writing the components of our module 1 stack.  Your `cdk` folder should resemble like the reference implementation, which can be found in the `~/environment/workshop/source/module-1/cdk/` directory.

### View the synthesized CloudFormation template

From within the `~/environment/workshop/cdk` folder run the `cdk synth` command to display the CloudFormation template that is generated based on the code you have just written.

```c#
cdk synth MythicalMysfits-WebApplication
```

### Commit your changes

Commit the changes to your CDK application to GIT by executing the following commands:

**Action:** Execute the following commands:

```sh
cd ~/environment/workshop/cdk
```

```sh
git add .
```

```sh
git commit -m 'Addition of Web Application stack'
```

```sh
git push
```

### Deploy the Web Application Infrastructure

The first time you deploy an AWS CDK app that deploys content into a S3 environment you’ll need to install a “bootstrap stack”. This function creates the resources required for the CDK toolkit’s operation. For example, the stack includes an S3 bucket that is used to store templates and assets during the deployment process.

### Deploy the Website and Infrastructure

The first time you deploy an AWS CDK app that deploys content into a S3 environment you’ll need to install a “bootstrap stack”. This function creates the resources required for the CDK toolkit’s operation. Currently the bootstrap command creates only an Amazon S3 bucket.

**Note:** You incur any charges for what the AWS CDK stores in the bucket. Because the AWS CDK does not remove any objects from the bucket, the bucket can accumulate objects as you use the AWS CDK. You can get rid of the bucket by deleting the MythicalMysfits-WebApplication stack from your account.

```sh
cdk bootstrap
```

We can deploy the `MythicalMysfits-WebApplication` by executing the `cdk deploy` command from within the `cdk` folder and defining the stack we wish to deploy.

**Action:** Execute the following command:

```sh
cd ~/environment/workshop/cdk
dotnet build src
cdk deploy MythicalMysfits-WebApplication
```

You will be prompted with a messages such as `Do you wish to deploy these changes (y/n)?` to which you should respond by typing `y`

The AWS CDK will then perform the following actions:

* Generate the AWS CodeCommit repositories for us to store our `web` and `CDK` code
* Creates an S3 Bucket
* Creates a CloudFront distribution to deliver the website code hosted in S3
* Enables access for CloudFront to access the S3 Bucket
* Removes any existing files in the bucket.
* Copies the local files from the Angular build directory located at `frontend/dist`.
* Prints the URL where you can visit your site.

Try to navigate to the URL displayed and see you website.

![mysfits-welcome](/images/module-1/mysfits-welcome.png)

Congratulations, you have created the basic static Mythical Mysfits Website!

That concludes Module 1.

[Proceed to Module 2](/module-2)
