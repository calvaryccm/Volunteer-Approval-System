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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCM.Volunteer.ApprovalProcess.Web
{
    public static class Globals
    {
        public static string VolunteerEmail { get; set; }
        public static int WelcomeNewVolunteerTemplate { get; set; }
        public static int AskForReferenceTemplate { get; set; }
        public static int RedFlagTeamTemplate { get; set; }
        public static int RedFlagTeamTemplateNOSOF { get; set; }
        public static int NewVolunteerApprovedTemplate { get; set; }
        public static int NewVolunteerApprovedTemplateNOSOF { get; set; }
        public static int ClearingAndFollowUpTemplate { get; set; }
        public static int ReturnToVolunteerDirectorTemplate { get; set; }
        public static int WelcomeToTeamTemplate { get; set; }
        public static int PlacedTeamMemberTemplate { get; set; }
    }
}