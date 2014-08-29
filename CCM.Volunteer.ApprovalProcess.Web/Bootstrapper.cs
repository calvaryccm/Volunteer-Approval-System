/*
Copyright 2014 Calvary Chapel of Melbourne, Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using CCM.Volunteer.ApprovalProcess.Core;
using Nancy;
using Nancy.Diagnostics;
using Nancy.Elmah;
using System;
using System.Configuration;

namespace CCM.Volunteer.ApprovalProcess.Web
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // The bootstrapper enables you to reconfigure the composition of the framework,
        // by overriding the various methods and properties.
        // For more information https://github.com/NancyFx/Nancy/wiki/Bootstrapper

        protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container,
          Nancy.Bootstrapper.IPipelines pipelines)
        {
            //SqlServerTypes.Utilities.LoadNativeAssemblies(System.IO.Path.Combine(RootPathProvider.GetRootPath(), ""));

            Globals.VolunteerEmail = ConfigurationManager.AppSettings["volunteerEmail"];
            Globals.WelcomeNewVolunteerTemplate = Int32.Parse(ConfigurationManager.AppSettings["WelcomeNewVolunteerTemplate"]);
            Globals.AskForReferenceTemplate = Int32.Parse(ConfigurationManager.AppSettings["AskForReferenceTemplate"]);
            Globals.RedFlagTeamTemplate = Int32.Parse(ConfigurationManager.AppSettings["RedFlagTeamTemplate"]);
            Globals.RedFlagTeamTemplateNOSOF = Int32.Parse(ConfigurationManager.AppSettings["RedFlagTeamTemplateNOSOF"]);
            Globals.NewVolunteerApprovedTemplate = Int32.Parse(ConfigurationManager.AppSettings["NewVolunteerApprovedTemplate"]);
            Globals.NewVolunteerApprovedTemplateNOSOF = Int32.Parse(ConfigurationManager.AppSettings["NewVolunteerApprovedTemplateNOSOF"]);
            Globals.ReturnToVolunteerDirectorTemplate = Int32.Parse(ConfigurationManager.AppSettings["ReturnToVolunteerDirectorTemplate"]);
            Globals.WelcomeToTeamTemplate = Int32.Parse(ConfigurationManager.AppSettings["WelcomeToTeamTemplate"]);
            Globals.PlacedTeamMemberTemplate = Int32.Parse(ConfigurationManager.AppSettings["PlacedTeamMemberTemplate"]);

            Elmahlogging.Enable(pipelines, "elmah");

            StaticConfiguration.DisableErrorTraces = false;

        }

        protected override DiagnosticsConfiguration DiagnosticsConfiguration
        {
            get { return new DiagnosticsConfiguration { Password = @"HELP" }; }
        }
    }
}