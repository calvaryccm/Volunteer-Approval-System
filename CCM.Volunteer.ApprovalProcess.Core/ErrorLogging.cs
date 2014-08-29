using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core
{
    public class ErrorLogging
    {
        public static void Log(Exception e)
        {

            if (ConfigurationManager.AppSettings["UseErrorLogging"] != null)
            {
                if (ConfigurationManager.AppSettings["UseErrorLogging"].CompareTo("true") == 0)
                    Elmah.ErrorSignal.FromCurrentContext().Raise(e);
            }
        }

        public static Exception GetLatestError()
        {
            System.Collections.IList errorList = new System.Collections.ArrayList();
            Elmah.ErrorLog.Default.GetErrors(0, 1, errorList);

            if(errorList.Count > 0)
            {
             Elmah.ErrorLogEntry entry = errorList[0] as Elmah.ErrorLogEntry;
             // do what you like with 'entry'
             return entry.Error.Exception;
            }

            return null;

        }

    }
}
