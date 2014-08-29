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
using CCM.Volunteer.ApprovalProcess.Core.Interfaces;
using CCM.Volunteer.ApprovalProcess.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.MinistryPlatform
{
    public class GroupsRepository : IGroupsRepository
    {
        private MinistryPlatformDataContext dataContext;

        public GroupsRepository()
        {
            dataContext = new MinistryPlatformDataContext();
        }

        public IEnumerable<Group> GetMinistryTeamsByProgramID(int programID)
        {
            return dataContext.GetRecords<ProgramGroup>(new { Program_ID = programID }).Select<ProgramGroup, Group>(f => dataContext.GetRecord<Group>(f.Group_ID));
        }

        public IEnumerable<Group> GetSmallGroupsByContactID(int contactID)
        {
            return dataContext.GetCurrentGroups(contactID);
        }
    }
}
