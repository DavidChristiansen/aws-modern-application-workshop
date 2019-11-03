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
