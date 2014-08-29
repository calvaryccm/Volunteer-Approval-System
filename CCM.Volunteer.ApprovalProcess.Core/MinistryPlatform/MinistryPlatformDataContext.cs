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
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Collections.Concurrent;
using CCM.Volunteer.ApprovalProcess.Core.Models;
using CCM.Volunteer.ApprovalProcess.Core.MinistryPlatform;
using CCM.Volunteer.ApprovalProcess.Core.Cryptography;

namespace CCM.Volunteer.ApprovalProcess.Core.MinistryPlatform
{
    public class MinistryPlatformDataContext : IDisposable
    {
        private static string connString = ConfigurationManager.ConnectionStrings["MinistryPlatform"].ConnectionString;
        private readonly SqlConnection connection = new SqlConnection(connString);

        //api specific stuffs
        private static string domainguid = ConfigurationManager.AppSettings["mpguid"];
        private static string password = ConfigurationManager.AppSettings["mppw"];
        private static int domainID = Convert.ToInt32(ConfigurationManager.AppSettings["mp_DomainID"]);

        public MinistryPlatformDataContext()
        {
            //SqlMapper.SetTypeMap(typeof(Address), new RemoveSlashesMap());
            Dapper.SqlMapper.SetTypeMap(
            typeof(Address),
            new CustomPropertyTypeMap(
                typeof(Address),
                (type, columnName) =>
                    type.GetProperties().FirstOrDefault(prop =>
                        prop.GetCustomAttributes(false)
                            .OfType<ColumnAttribute>()
                            .Any(attr => attr.Name == columnName))));
        }

        /// <summary>
        /// Gets the current Domain ID
        /// </summary>
        public int DomainID
        {
            get
            {
                return domainID;
            }
        }

        public IEnumerable<T> GetRecords<T>(object query = null)
        {
            if (query == null)
            {
                return connection.GetList<T>();
            }

            return connection.GetList<T>(query);
        }

        public IEnumerable<T> GetRecordsWithImages<T>(object query = null)
            where T : BaseEntity
        {
            if (query == null)
            {
                return connection.GetListWithImageInfo<T>();
            }

            return connection.GetListWithImageInfo<T>(query);
        }

        public T GetRecord<T>(int id)
        {
            return connection.Get<T>(id);
        }

        public T GetRecordWithImage<T>(int id)
           where T : BaseEntity
        {
            return connection.GetWithImageInfo<T>(id);
        }

        public bool UpdateRecord<T>(T obj, int UserID = 0, string UserName = "WebUser")
        {
            bool result = connection.Update(obj) == 1;

            connection.Insert(new dpAuditLog
                                {
                                    Table_Name = typeof(T).GetAttributeValue((System.ComponentModel.DataAnnotations.Schema.TableAttribute attr) => attr.Name),
                                    Date_Time = DateTime.Now,
                                    Audit_Description = "Updated",
                                    Record_ID = GetRecordID(typeof(T), obj),
                                    User_ID = UserID,
                                    User_Name = UserName
                                });

            return result;
            //return false;
        }

        /// <summary>
        /// Update a record in the DB
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool UpdateRecord<T>(int id, dynamic data, int UserID = 0, string UserName = "WebUser")
        {
            List<string> paramNames = GetParamNames((object)data);

            //if there's nothing to update, then just return with true
            if (paramNames.Count < 1)
                return true;

            var TableName = typeof(T).GetAttributeValue((System.ComponentModel.DataAnnotations.Schema.TableAttribute attr) => attr.Name);
            var KeyName = GetRecordName(typeof(T));


            var builder = new StringBuilder();
            builder.Append("update ").Append(TableName).Append(" set ");
            builder.AppendLine(string.Join(",", paramNames.Select(p => p + "= @" + p)));
            builder.Append("where " + KeyName + " = @Id");

            DynamicParameters parameters = new DynamicParameters(data);
            parameters.Add("Id", id);

            int result = connection.Execute(builder.ToString(), parameters);

            connection.Insert(new dpAuditLog
            {
                Table_Name = TableName,
                Date_Time = DateTime.Now,
                Audit_Description = "Updated",
                Record_ID = id,
                User_ID = UserID,
                User_Name = UserName
            });

            return result == 1;
        }

        public int InsertRecord<T>(T newObj, int UserID = 0, string UserName = "WebUser")
        {
            int? result = connection.Insert(newObj);

            if (!result.HasValue)
                return 0;

            connection.Insert(new dpAuditLog
            {
                Table_Name = typeof(T).GetAttributeValue((System.ComponentModel.DataAnnotations.Schema.TableAttribute attr) => attr.Name),
                Date_Time = DateTime.Now,
                Audit_Description = "Created",
                Record_ID = result.Value,
                User_ID = UserID,
                User_Name = UserName
            });

            return result.Value;
        }

        public int InsertRecordDynamic(string tableName, object data, int UserID = 0, string UserName = "WebUser")
        {
            List<string> paramNames = GetParamNames((object)data);
            var TableName = tableName; //typeof(T).GetAttributeValue((System.ComponentModel.DataAnnotations.Schema.TableAttribute attr) => attr.Name);

            var builder = new StringBuilder();
            builder.Append("insert into ").Append(TableName).Append(" (");
            builder.Append(string.Join(", ", paramNames.Select(p => "[" + p + "]"))).AppendLine(")");
            builder.Append(" values (").Append(string.Join(", ", paramNames.Select(p => "@" + p.Replace("/", "_")))).AppendLine(");");
            builder.Append(" SELECT CAST(SCOPE_IDENTITY() as int)");


            DynamicParameters parameters = new DynamicParameters();
            parameters.AddDynamicParams(data);
            
            DynamicParameters newParams = new DynamicParameters();
            //remove any weird characters
            foreach (var item in parameters.ParameterNames)
            {
                var param = parameters.Get(item);
                if (param != null)
                    newParams.Add(item.Replace("/", "_"), param);
            }

            int result = connection.Query<int>(builder.ToString(), newParams).First();

            connection.Insert(new dpAuditLog
            {
                Table_Name = TableName,
                Date_Time = DateTime.Now,
                Audit_Description = "Created",
                Record_ID = result,
                User_ID = UserID,
                User_Name = UserName
            });

            return result;
        }

        public IEnumerable<T> ExecuteStoredProcedure<T>(string storedProcedureName)
        {
            return connection.Query<T>(storedProcedureName, commandType: System.Data.CommandType.StoredProcedure);
        }

        public IEnumerable<T> ExecuteStoredProcedure<T>(string storedProcedureName, object parameters)
        {
            return connection.Query<T>(storedProcedureName, parameters, commandType: System.Data.CommandType.StoredProcedure);
        }

        public bool ExecuteStoredProcedure(string storedProcedureName)
        {
            return connection.Execute(storedProcedureName, commandType: System.Data.CommandType.StoredProcedure) > 0;
        }

        public bool ExecuteStoredProcedure(string storedProcedureName, object parameters)
        {
            return connection.Execute(storedProcedureName, parameters, commandType: System.Data.CommandType.StoredProcedure) > 0;
        }

        //public IEnumerable<Congregation> GetCurrentCampuses()
        //{
        //    return connection.Query<Congregation>("SELECT * FROM Congregations WHERE (End_Date IS Null Or End_Date > GETDATE()) AND Location_ID IS NOT Null");
        //}

        public int? FindMatchingUser(string firstName, string email)
        {
            return connection.Query<int?>(@"SELECT TOP 1 dp_Users.[User_ID]
	            FROM dp_Users 
		            INNER JOIN dp_Domains ON dp_Domains.Domain_ID = dp_Users.Domain_ID
		            INNER JOIN Contacts ON Contacts.Contact_ID = dp_Users.Contact_ID
		
	            WHERE 
		            dp_Domains.Domain_ID = @DomainID
		            AND (Contacts.Email_Address LIKE @EmailAddress OR dp_Users.User_Email LIKE @EmailAddress)
		            AND (Contacts.First_Name LIKE @FirstName OR Contacts.Nickname LIKE @FirstName)", new { EmailAddress = email, FirstName = firstName, DomainID = domainID }).SingleOrDefault();
        }

        public IEnumerable<ParticipantMilestone> GetMilestones(int contactID)
        {
            return connection.Query<ParticipantMilestone>(@"SELECT * 
	                FROM Participant_Milestones PM
                    INNER JOIN Contacts C ON C.Participant_Record = PM.Participant_ID
	                INNER JOIN Milestones M ON M.Milestone_ID = PM.Milestone_ID
	                WHERE C.Contact_ID = @ContactID
	                ORDER BY Date_Accomplished DESC", new { ContactID = contactID });
        }

        public IEnumerable<Group> GetCurrentGroups(int contactID)
        {
            return connection.Query<Group>(@"SELECT G.* FROM Group_Participants GP
                                                INNER JOIN Participants P ON GP.Participant_ID = P.Participant_ID
                                                INNER JOIN Contacts C ON P.Contact_ID = C.Contact_ID
                                                INNER JOIN Groups G ON GP.Group_ID = G.Group_ID
                                                WHERE C.Contact_ID = @ContactID
                                                AND (GP.End_Date IS NULL OR GP.End_Date > GETDATE())
                                                ORDER BY Start_Date DESC", new { ContactID = contactID });
        }

        public IEnumerable<MinistryQuestionaire> GetVolunteerAppsForRedFlag()
        {
            var result = connection.Query<MinistryQuestionaire>(@"SELECT * FROM Ministry_Questionaires MQ
                                                                INNER JOIN Contacts C ON C.Contact_ID = MQ.Contact_ID
	                                                            WHERE Red_Flag_Submitted IS NULL
	                                                            AND ISNULL(MQ_Status_ID, 1) <> 3
                                                                ORDER BY C.Last_Name, C.First_Name");

            foreach(var item in result)
            {
                item.ReferenceList = connection.GetList<MQReference>(new { Ministry_Questionaire_ID = item.Ministry_Questionaire_ID });
                item.MinistryToVolunteerFor = connection.Query<Program>(@"SELECT TOP 1 P.*
                                                                          FROM MQ_Programs MQP
                                                                          INNER JOIN Programs P ON MQP.Program_ID = P.Program_ID
                                                                          WHERE MQP.Ministry_Questionaire_ID = @ID
                                                                          ORDER BY MQP.Start_Date DESC",
                                                                                                       new { ID = item.Ministry_Questionaire_ID })
                                                                        .SingleOrDefault();
                item.Contact = connection.GetWithImageInfo<Contact>(item.Contact_ID);

                if (item.Contact.Marital_Status_ID.HasValue)
                    item.Contact.MaritalStatus = connection.Get<MaritalStatus>(item.Contact.Marital_Status_ID);

                if (item.Volunteer_Campus.HasValue)
                    item.Campus = connection.Get<Congregation>(item.Volunteer_Campus.Value);

                if(item.How_Often.HasValue)
                    item.HowOftenDoYouAttend = connection.Get<Frequency>(item.How_Often);
            }

            return result;
        }

        public IEnumerable<MinistryQuestionaire> GetVolunteerAppsReadyForApproval()
        {
            var result = connection.Query<MinistryQuestionaire>(@"SELECT *
                                                                    FROM Ministry_Questionaires MQ
                                                                    INNER JOIN Contacts C ON C.Contact_ID = MQ.Contact_ID
                                                                    WHERE (DATEADD(HOUR, 70, ISNULL(Red_Flag_Submitted, GETDATE())) < GETDATE()) -- 3 days after red flag was submitted
                                                                    AND MQ_Status_ID = 6 -- this app is in review
                                                                    ORDER BY C.Last_Name, C.First_Name");

            foreach (var item in result)
            {
                item.ReferenceList = connection.GetList<MQReference>(new { Ministry_Questionaire_ID = item.Ministry_Questionaire_ID });
                item.MinistryToVolunteerFor = connection.Query<Program>(@"SELECT TOP 1 P.*
                                                                          FROM MQ_Programs MQP
                                                                          INNER JOIN [Programs] P ON MQP.Program_ID = P.Program_ID
                                                                          WHERE MQP.Ministry_Questionaire_ID = @ID
                                                                          ORDER BY MQP.Start_Date DESC",
                                                                                                       new { ID = item.Ministry_Questionaire_ID })
                                                                        .SingleOrDefault();
                item.Contact = connection.GetWithImageInfo<Contact>(item.Contact_ID);

                if (item.Contact.Marital_Status_ID.HasValue)
                    item.Contact.MaritalStatus = connection.Get<MaritalStatus>(item.Contact.Marital_Status_ID);

                if (item.Volunteer_Campus.HasValue)
                    item.Campus = connection.Get<Congregation>(item.Volunteer_Campus.Value);

                if (item.How_Often.HasValue)
                    item.HowOftenDoYouAttend = connection.Get<Frequency>(item.How_Often);
            }

            return result;
        }

        public IEnumerable<MinistryQuestionaire> GetVolunteerAppsNeedingFollowUp()
        {
            var result = connection.Query<MinistryQuestionaire>(@"SELECT *
                                                            FROM Ministry_Questionaires MQ
                                                            INNER JOIN Contacts C ON C.Contact_ID = MQ.Contact_ID
                                                            WHERE MQ_Follow_Up < GETDATE()
                                                            ORDER BY C.Last_Name, C.First_Name");


            foreach (var item in result)
            {
                item.ReferenceList = connection.GetList<MQReference>(new { Ministry_Questionaire_ID = item.Ministry_Questionaire_ID });
                item.MinistryToVolunteerFor = connection.Query<Program>(@"SELECT TOP 1 P.*
                                                                          FROM MQ_Programs MQP
                                                                          INNER JOIN [Programs] P ON MQP.Program_ID = P.Program_ID
                                                                          WHERE MQP.Ministry_Questionaire_ID = @ID
                                                                          ORDER BY MQP.Start_Date DESC",
                                                                                                       new { ID = item.Ministry_Questionaire_ID })
                                                                        .SingleOrDefault();
                item.Contact = connection.GetWithImageInfo<Contact>(item.Contact_ID);

                if(item.Contact.Marital_Status_ID.HasValue)
                    item.Contact.MaritalStatus = connection.Get<MaritalStatus>(item.Contact.Marital_Status_ID);

                if (item.Volunteer_Campus.HasValue)
                    item.Campus = connection.Get<Congregation>(item.Volunteer_Campus.Value);

                if (item.How_Often.HasValue)
                    item.HowOftenDoYouAttend = connection.Get<Frequency>(item.How_Often);
            }

            return result;
        }



        public IEnumerable<Contact> GetContactsByGroupID(int groupID)
        {
            return connection.Query<Contact, dpFile, Contact>(@" DECLARE @PageID int = (SELECT Page_ID FROM dp_Pages WHERE Display_Name='Contacts')
                                                SELECT * FROM Group_Participants GP
                                                INNER JOIN Participants P ON GP.Participant_ID = P.Participant_ID
                                                INNER JOIN Contacts C ON P.Contact_ID = C.Contact_ID
                                                INNER JOIN Groups G ON GP.Group_ID = G.Group_ID
                                                LEFT JOIN dp_Files as cFiles ON cFiles.Record_ID = C.Contact_ID  AND cFiles.Page_ID = @PageID AND cFiles.Default_Image = 1
                                                WHERE G.Group_ID = @GroupID
                                                AND (GP.End_Date IS NULL OR GP.End_Date > GETDATE())",
                                                                                        (obj, file) => { 
                                                                                    obj.DefaultImage = file;
                                                                                    if (file != null)
                                                                                        obj.DefaultImageUrl = String.Format(Constants.GeneralImageBaseAppUrl, file.Table_Name.ToLower(), obj.Contact_ID, file.File_ID);
                                                                                    else
                                                                                        obj.DefaultImageUrl = String.Format(Constants.GeneralImageBaseAppUrl, "contacts", obj.Contact_ID, 0);
                                                                                    return obj; },             
                                                                                                     param: new { GroupID = groupID },
                                                                                                     splitOn: "File_ID");
        }

        public MQProgram GetLatestMinistryAppliedFor(int appID, int programID)
        {
            return connection.Query<MQProgram>(@"SELECT TOP 1 * 
                                                FROM MQ_Programs MQP 
                                                WHERE MQP.Ministry_Questionaire_ID = @ID 
                                                AND   MQP.Program_ID = @programID
                                                ORDER BY MQP.[Start_Date] DESC", new { ID = appID, programID = programID }).SingleOrDefault();
        }

        public Program GetMinistryToVolunteerFor(int appID)
        {
            return connection.Query<Program>(@"SELECT TOP 1 P.*
                                                    FROM MQ_Programs MQP
                                                    INNER JOIN Programs P ON MQP.Program_ID = P.Program_ID
                                                    WHERE MQP.Ministry_Questionaire_ID = @ID
                                                    ORDER BY MQP.Start_Date DESC, MQP.MQ_Program_ID DESC",
                                                                                new { ID = appID })
                                                .SingleOrDefault();
        }

        public bool AttachImageToContact(byte[] image, int contactID, string fileType)
        {
            int pageID = connection.Query<int>("SELECT Page_ID FROM dp_Pages WHERE Display_Name='contacts'").SingleOrDefault();

            using (apiSoapClient mp = new apiSoapClient())
            {
                string result = mp.AttachFile(domainguid, password, image, contactID + "." + fileType, pageID, contactID, "Uploaded from CCM web on " + DateTime.Now.ToString(), true, 0);
                string[] results = result.Split('|');

                string[] sptGuid = results[0].Split('.');
                string UniqueName = "";
                for (int g = 0; g < sptGuid.Length - 1; g++)
                    UniqueName += sptGuid[g] + ".";

                UniqueName = UniqueName.Substring(0, UniqueName.Length - 1);
                string defResults = mp.UpdateDefaultImage(domainguid, password, pageID, contactID, UniqueName);
                string defResult = defResults.Split('|')[1];

                if (Int32.Parse(results[1]) != 0 || Int32.Parse(defResult) != 0)
                    return false;
            }

            return true;
        }

        public bool UpdateUserPassword(int userID, string newPassword)
        {
            return ExecuteStoredProcedure("dp_UpdateUserPassword", new { UserID = userID, NewPassword = newPassword.MD5Encode() });
        }

        //        public SignUpLog GetSignUpLogInfo(Guid token)
        //        {


        //                return connection.Query<SignUpLog>(@"SELECT *
        //	                                    FROM Sign_Up_Log as S
        //		                                INNER JOIN dp_Domains as D ON D.Domain_ID = S.Domain_ID
        //	                                    WHERE S.Sign_Up_Attempt_Guid = @Token
        //	                                    AND S.Domain_ID = @DomainID",
        //                                                                    new
        //                                                                    {
        //                                                                        Token = token.ToString(),
        //                                                                        DomainID = DomainID
        //                                                                    }).SingleOrDefault();

        //        }

        public void GetUserInfo(int UserID, out string UserGuid, out string DomainGuid)
        {
            //get password
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["MinistryPlatform"].ConnectionString))
            {
                connection.Open();

                //get login result
                var loginresult = connection.Query(@"SELECT [User_ID], [Contact_ID], [User_Guid], [Domain_GUID]
	                                    FROM dp_Users as U
		                                INNER JOIN dp_Domains as D ON D.Domain_ID = U.Domain_ID
	                                    WHERE U.User_ID = @UserID
	                                    AND U.Domain_ID = @DomainID",
                                        new
                                        {
                                            UserID = UserID,
                                            DomainID = domainID
                                        });

                //if there's a result, populate the out parameters
                if (loginresult.Count() > 0)
                {
                    //ContactID = loginresult.ElementAt(0).Contact_ID;
                    UserGuid = loginresult.ElementAt(0).User_Guid.ToString();
                    // UserID = loginresult.ElementAt(0).User_ID;
                    DomainGuid = loginresult.ElementAt(0).Domain_GUID.ToString();
                }
                else
                {
                    UserGuid = string.Empty;
                    DomainGuid = string.Empty;
                }
            }
        }

        private int GetRecordID(Type type, object obj)
        {
            foreach (var property in type.GetProperties())
            {
                System.ComponentModel.DataAnnotations.KeyAttribute keyAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>();

                if (keyAttribute != null)
                {
                    return (int)property.GetValue(obj);
                }
            }

            return -1;
        }

        private string GetRecordName(Type type)
        {
            foreach (var property in type.GetProperties())
            {
                System.ComponentModel.DataAnnotations.KeyAttribute keyAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>();

                if (keyAttribute != null)
                {
                    return property.Name;
                }
            }

            return string.Empty;
        }

        static ConcurrentDictionary<Type, List<string>> paramNameCache = new ConcurrentDictionary<Type, List<string>>();

        internal static List<string> GetParamNames(object o)
        {
            if (o is DynamicParameters)
            {
                return (o as DynamicParameters).ParameterNames.ToList();
            }

            List<string> paramNames;
            if (!paramNameCache.TryGetValue(o.GetType(), out paramNames))
            {
                paramNames = new List<string>();
                foreach (var prop in o.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public).Where(n => n.GetCustomAttribute<KeyAttribute>() == null))
                {
                    ColumnAttribute item = prop.GetCustomAttribute<ColumnAttribute>();
                    if (item != null)
                    {
                        paramNames.Add(item.Name);
                        continue;
                    }

                    paramNames.Add(prop.Name);
                }
                paramNameCache[o.GetType()] = paramNames;
            }
            return paramNames;
        }

        //internal static bool TableExists(string tableName)
        //{
        //    Assembly.GetAssembly(typeof(Contact)).;
        //}

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
