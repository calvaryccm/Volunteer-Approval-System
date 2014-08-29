using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace CCM.Volunteer.ApprovalProcess.Automation
{

    public class CyclicLogTraceListener : TraceListener
    {
        private const int _StackFrameSkipCount = 5;
        private const char _IndentCharacter = ' ';
        private long _FileIndex = 0;
        private bool _FirstLogFound = false;
        private bool _FileNameTemplateHasFormatting = false;
        private long _FileLength = 0;
        private DateTime _FileCreationDate = DateTime.MinValue;
        #region "  Properties"
        private StreamWriter _sw;
        private string _FolderName;
        private string _FieldSeparator;
        private long _FileSizeThreshold;
        private SizeUnit _FileSizeUnit;
        private long _FileCountThreshold;
        private string _FileName;
        private string _FileNameTemplate;
        private string _TimeStampFormat;
        private bool _AddMethod;
        private bool _AddPidTid;
        private bool _AutoFlush;
        private long _FileAgeThreshold;
        private AgeUnit _FileAgeUnit;
        /// <summary>
        /// Indicates what unit of time FileAgeThreshold represents
        /// </summary>
        public enum AgeUnit
        {
            Minutes,
            Hours,
            Days,
            Weeks,
            Months
        }
        /// <summary>
        /// Indicates what unit of size FileBytesThreshold represents
        /// </summary>
        public enum SizeUnit
        {
            Gigabytes,
            Megabytes,
            Kilobytes,
            Bytes
        }
        /// <summary>
        /// If true, log file is flushed after every write.
        /// Can also be set via trace="autoflush" in the
        /// system.diagnostics .config file section
        /// </summary>
        public bool AutoFlush
        {
            get { return _AutoFlush; }
            set { _AutoFlush = value; }
        }
        /// <summary>
        /// Folder that log files will be written to.
        /// Defaults to current folder.
        /// </summary>
        public string FolderName
        {
            get { return _FolderName; }
            set
            {
                _FolderName = value;
                if (!_FolderName.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    _FolderName = _FolderName + Path.DirectorySeparatorChar;
                }
                if (!Directory.Exists(_FolderName))
                {
                    throw new DirectoryNotFoundException("Requested trace logging directory '" + _FolderName + "' does not exist");
                }
            }
        }
        /// <summary>
        /// Seperator used between log field entries.
        /// Defaults to comma.
        /// </summary>
        public string FieldSeparator
        {
            get { return _FieldSeparator; }
            set { _FieldSeparator = value; }
        }
        /// <summary>
        /// Template used to generate log filenames
        /// supports standard String.Format for two values: file index {0:} and current date {1:}
        /// using the standard String.Format conventions
        /// Defaults to "{0:0000}.log"
        /// </summary>
        public string FileNameTemplate
        {
            get { return _FileNameTemplate; }
            set
            {
                _FileNameTemplate = value;
                _FileNameTemplateHasFormatting = Regex.IsMatch(_FileNameTemplate, "{(0|1):.*}");
            }
        }
        /// <summary>
        /// Add the method name of the calling function to the log.
        /// Defaults to True.
        /// </summary>
        public bool AddMethod
        {
            get { return _AddMethod; }
            set { _AddMethod = value; }
        }
        /// <summary>
        /// Add the process and thread ID to the log.
        /// Defaults to False.
        /// </summary>
        public bool AddPidTid
        {
            get { return _AddPidTid; }
            set { _AddPidTid = value; }
        }
        /// <summary>
        /// If a format string is provided, the time will be added to each log entry.
        /// Defaults to "yyyy-MM-dd hh:mm:ss". Set to empty string to disable.
        /// </summary>
        public string TimeStampFormat
        {
            get { return _TimeStampFormat; }
            set { _TimeStampFormat = value; }
        }
        /// <summary>
        /// Maximum number of log files to create.
        /// Defaults to 10000.
        /// </summary>
        public long FileCountThreshold
        {
            get { return _FileCountThreshold; }
            set { _FileCountThreshold = value; }
        }
        /// <summary>
        /// Maximum age, in FileAgeUnits, of log files before a new log file will be created
        /// Defaults to 0, infinite
        /// </summary>
        public long FileAgeThreshold
        {
            get { return _FileAgeThreshold; }
            set { _FileAgeThreshold = value; }
        }
        /// <summary>
        /// Determines what time unit is represented in FileAgeThreshold.
        /// Defaults to AgeUnit.Days
        /// </summary>
        public AgeUnit FileAgeUnit
        {
            get { return _FileAgeUnit; }
            set { _FileAgeUnit = value; }
        }
        /// <summary>
        /// Maximum file size each log is allowed to grow to before a new log is created.
        /// Defaults to 512kb.
        /// </summary>
        public long FileSizeThreshold
        {
            get { return (_FileSizeThreshold); }
            set { _FileSizeThreshold = value; }
        }
        /// <summary>
        /// Determines what size unit is represented in FileSizeThreshold.
        /// Defaults to Bytes.
        /// </summary>
        public SizeUnit FileSizeUnit
        {
            get { return _FileSizeUnit; }
            set { _FileSizeUnit = value; }
        }
        #endregion
        #region "  Public Methods"
        /// <summary>
        /// constructor contains defaults if values aren't specified
        /// </summary>
        public CyclicLogTraceListener()
        {
            this.FileNameTemplate = "{0:0000}.log";
            _FolderName = ".";
            _FileSizeThreshold = 1;
            _FileSizeUnit = SizeUnit.Megabytes;
            _FileCountThreshold = 10000;
            _TimeStampFormat = "yyyy-dd-MM hh:mm:ss";
            _AddMethod = false;
            _AddPidTid = false;
            _FieldSeparator = ", ";
            _FileAgeUnit = AgeUnit.Days;
            _FileAgeThreshold = 0;
            _AutoFlush = true;
        }
        /// <summary>
        /// this method is used when trace configured via the system.diagnostics section of the .config file
        /// all the parameters are set via a single initializeData string in this format:
        ///   "booleanValue=true, stringValue='string', longValue=567"
        /// </summary>
        public CyclicLogTraceListener(string initializeData)
            : this()
        {
            FolderName = ParseString(initializeData, "folderName", _FolderName);
            _FileSizeThreshold = ParseLong(initializeData, "fileSizeThreshold", _FileSizeThreshold);
            _FileSizeUnit = (SizeUnit)ParseEnum(initializeData, "fileSizeUnit", _FileSizeUnit, typeof(SizeUnit));
            _FileCountThreshold = ParseLong(initializeData, "fileCountThreshold", _FileCountThreshold);
            _FileAgeThreshold = ParseLong(initializeData, "fileAgeThreshold", _FileAgeThreshold);
            _FileAgeUnit = (AgeUnit)ParseEnum(initializeData, "fileAgeUnit", _FileAgeUnit, typeof(AgeUnit));
            _FileNameTemplate = ParseString(initializeData, "fileNameTemplate", _FileNameTemplate);
            _TimeStampFormat = ParseString(initializeData, "timeStampFormat", _TimeStampFormat);
            _AddPidTid = ParseBoolean(initializeData, "addPidTid", _AddPidTid);
            _AddMethod = ParseBoolean(initializeData, "addMethod", _AddMethod);
            _FieldSeparator = ParseString(initializeData, "fieldSeparator", _FieldSeparator);
        }
        #region "  Initialization Parsing"
        private object ParseEnum(string initializeData, string name, object defaultValue, Type t)
        {
            string s = ParseString(initializeData, name, defaultValue.ToString());
            if (string.IsNullOrEmpty(s))
            {
                return defaultValue;
            }
            object o = null;
            try
            {
                o = System.Enum.Parse(t, s, true);
            }
            catch (System.ArgumentException ex)
            {
                //-- if the string representation provided doesn't match
                //-- any known enum (case, we'll get this exception
            }
            if (o == null)
            {
                return defaultValue;
            }
            else
            {
                return o;
            }
        }
        /// <summary>
        /// parses values of the form
        /// name=true, name=false
        /// </summary>
        private bool ParseBoolean(string initializeData, string name, bool defaultValue)
        {
            Match m = Regex.Match(initializeData, "(?<=" + name + "=)false|true", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                return bool.Parse(m.Value);
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// parses values of the form
        /// name=3, name=28932
        /// </summary>
        private long ParseLong(string initializeData, string name, long defaultValue)
        {
            Match m = Regex.Match(initializeData, "(?<=" + name + "=)d+", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                return long.Parse(m.Value);
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// parses values of the form
        /// name='data', name="data", name=data
        /// </summary>
        private string ParseString(string initializeData, string name, string defaultValue)
        {
            Match m = Regex.Match(initializeData, "(?<=" + name + "=('|\")*)[^'\",]+", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                return m.Value;
            }
            else
            {
                //-- check for the ='' ="" =, case (empty string)
                if (Regex.IsMatch(initializeData, name + "=['\",]['\"]*", RegexOptions.IgnoreCase))
                {
                    return "";
                }
                else
                {
                    return defaultValue;
                }
            }
        }
        #endregion
        public override void Write(object o)
        {
            WriteMessage(FormatMessage(o.ToString(), "", false));
        }
        public override void Write(string message)
        {
            WriteMessage(FormatMessage(message, "", false));
        }
        public override void Write(string message, string category)
        {
            WriteMessage(FormatMessage(message, category, false));
        }
        public override void Write(object o, string category)
        {
            WriteMessage(FormatMessage(o.ToString(), category, false));
        }
        public override void WriteLine(object o)
        {
            WriteMessage(FormatMessage(o.ToString(), "", true));
        }
        public override void WriteLine(string message)
        {
            WriteMessage(FormatMessage(message, "", true));
        }
        public override void WriteLine(string message, string category)
        {
            WriteMessage(FormatMessage(message, category, true));
        }
        public override void WriteLine(object o, string category)
        {
            WriteMessage(FormatMessage(o.ToString(), category, true));
        }
        public override void Close()
        {
            lock (this)
            {
                CloseLogFile();
            }
        }
        public override void Flush()
        {
            lock (this)
            {
                if ((_sw != null))
                {
                    _sw.Flush();
                }
            }
        }
        #endregion
        #region "  Private Methods"
        private string[] FormatMessage(string message, string category, bool includeNewLine)
        {
            return new string[] {
			GetIndent(),
			GetTimeStamp(),
			GetPidTid(),
			GetMethodName(),
			GetCategory(category),
			message,
			GetNewLine(includeNewLine)
		};
        }
        /// <summary>
        /// creates a new log filename in this format
        ///   "Directory  FileNameTemplate"
        /// </summary>
        private string CreateLogFileName(long fileIndex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_FolderName);
            sb.Append(string.Format(_FileNameTemplate, fileIndex, DateTime.Now));
            return sb.ToString();
        }
        /// <summary>
        /// Check that no more than (n) log files will exist at any given time;
        /// if more than (n) do exist, the oldest one is deleted
        /// </summary>
        private void EnforceFileThreshold()
        {
            if (_FileCountThreshold == 0)
                return;
            //-- get all the files in the current folder..
            string[] FileNames = null;
            if (string.IsNullOrEmpty(Path.GetExtension(_FileNameTemplate)))
            {
                FileNames = Directory.GetFiles(_FolderName);
            }
            else
            {
                //-- ..that end with whatever log extension was specified
                FileNames = Directory.GetFiles(_FolderName, "*" + Path.GetExtension(_FileNameTemplate));
            }
            if (FileNames.Length == 0)
                return;
            int FilesMatched = 0;
            DateTime OldestFileDate = DateTime.MinValue;
            string OldestFileName = "";
            //-- find all the files that match our specific log pattern
            //-- (extension isn't specific enough
            string FilePattern = Regex.Replace(_FileNameTemplate, "{[^}]+?}", ".*?") + "$";
            Regex r = new Regex(FilePattern);
            FileInfo fi = default(FileInfo);
            foreach (string FileName in FileNames)
            {
                if (r.IsMatch(FileName))
                {
                    FilesMatched += 1;
                    fi = new FileInfo(FileName);
                    if (fi.CreationTimeUtc > OldestFileDate)
                    {
                        OldestFileDate = fi.CreationTimeUtc;
                        OldestFileName = FileName;
                    }
                }
            }
            if (FilesMatched > _FileCountThreshold)
            {
                File.Delete(OldestFileName);
            }
        }
        /// <summary>
        /// Opens the "current" log file; this can be either an
        /// existing incomplete log file or a brand new log file
        /// </summary>
        private void OpenLogFile(long messageLength)
        {
            //-- close any currently open log file, if any
            CloseLogFile();
            string FileName = null;
            int LoopCount = 0;
            while (true)
            {
                LoopCount += 1;
                //-- generate next log name in sequence (by date, index, etc)
                if (_FileCountThreshold == 0)
                {
                    _FileIndex = 1;
                }
                else
                {
                    _FileIndex += 1;
                    if (_FileIndex > _FileCountThreshold)
                    {
                        _FileIndex = 1;
                    }
                }
                FileName = CreateLogFileName(_FileIndex - 1);
                //-- see if next log file already exists
                if (!File.Exists(FileName))
                {
                    //-- this will be a new log file
                    _FileLength = 0;
                    _FileCreationDate = DateTime.MinValue;
                    //-- if creating a new file, we need to make ABSOLUTELY
                    //-- sure we haven't exceeded total allowed file count
                    EnforceFileThreshold();
                    break; // TODO: might not be correct. Was : Exit Do
                }
                else
                {
                    //-- existing log file; retrieve length and creation time
                    FileInfo fi = new FileInfo(FileName);
                    _FileLength = fi.Length;
                    _FileCreationDate = fi.CreationTimeUtc;
                    //-- has this log file exceeded valid length or age?
                    if (LogFileSizeMaxReached(messageLength) | LogFileAgeMaxReached())
                    {
                        if (_FirstLogFound | (LoopCount > _FileCountThreshold))
                        {
                            File.Delete(FileName);
                            _FileLength = 0;
                            break; // TODO: might not be correct. Was : Exit Do
                        }
                    }
                    else
                    {
                        break; // TODO: might not be correct. Was : Exit Do
                    }
                }
            }
            //-- this is an optimization for subsequent passes through the loop
            _FirstLogFound = true;
            //-- at this point we're either..
            //-- A) opening a brand new logfile
            //-- B) appending to an existing logfile
            _sw = File.AppendText(FileName);
            _sw.AutoFlush = _AutoFlush;
        }
        private void CloseLogFile()
        {
            lock (this)
            {
                if (((_sw != null)))
                {
                    _sw.Close();
                    _sw = null;
                }
            }
        }
        private long StringArrayLength(string[] message)
        {
            long ml = 0;
            for (int i = 0; i <= message.Length - 1; i++)
            {
                ml += message[i].Length;
            }
            return ml;
        }
        private void WriteMessage(string[] message)
        {
            long ml = StringArrayLength(message);
            lock (this)
            {
                if (_sw == null)
                {
                    OpenLogFile(ml);
                }
                else
                {
                    if (LogFileSizeMaxReached(ml) | LogFileAgeMaxReached())
                    {
                        OpenLogFile(ml);
                    }
                }
                for (int i = 0; i <= message.Length - 1; i++)
                {
                    _sw.Write(message[i]);
                }
                _FileLength += ml;
            }
        }
        private string GetMethodName()
        {
            if (_AddMethod)
            {
                StackFrame sf = new StackFrame(_StackFrameSkipCount);
                MethodBase mb = sf.GetMethod();
                StringBuilder sb = new StringBuilder();
                sb.Append(mb.ReflectedType.FullName);
                sb.Append(".");
                sb.Append(mb.Name);
                sb.Append(_FieldSeparator);
                return sb.ToString();
            }
            else
            {
                return "";
            }
        }
        private string GetIndent()
        {
            return new string(_IndentCharacter, (this.IndentLevel * this.IndentSize));
        }
        private string GetCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return "";
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(category);
                sb.Append(_FieldSeparator);
                return sb.ToString();
            }
        }
        private string GetNewLine(bool includeNewLine)
        {
            if (includeNewLine)
            {
                return Environment.NewLine;
            }
            else
            {
                return "";
            }
        }
        private string GetPidTid()
        {
            if (_AddPidTid)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Process.GetCurrentProcess().Id);
                sb.Append("/");
                sb.Append(Thread.CurrentThread.ManagedThreadId);
                sb.Append(_FieldSeparator);
                return sb.ToString();
            }
            else
            {
                return "";
            }
        }
        private string GetTimeStamp()
        {
            if (string.IsNullOrEmpty(_TimeStampFormat))
            {
                return "";
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString(_TimeStampFormat));
                sb.Append(_FieldSeparator);
                return sb.ToString();
            }
        }
        private bool LogFileAgeMaxReached()
        {
            if (_FileAgeThreshold == 0)
            {
                return false;
            }
            else
            {
                if (_FileCreationDate == DateTime.MinValue)
                {
                    return false;
                }
                switch (_FileAgeUnit)
                {
                    case AgeUnit.Hours:
                        return _FileCreationDate < DateTime.UtcNow.AddHours(-_FileAgeThreshold);
                    case AgeUnit.Minutes:
                        return _FileCreationDate < DateTime.UtcNow.AddMinutes(-_FileAgeThreshold);
                    case AgeUnit.Months:
                        return _FileCreationDate < DateTime.UtcNow.AddMonths(-Convert.ToInt32(_FileAgeThreshold));
                    case AgeUnit.Weeks:
                        return _FileCreationDate < DateTime.UtcNow.AddDays(-(_FileAgeThreshold * 7));
                    default:
                        //-- default to days
                        return _FileCreationDate < DateTime.UtcNow.AddDays(-_FileAgeThreshold);
                }
            }
        }
        private bool LogFileSizeMaxReached(long messageLength)
        {
            if (_FileSizeThreshold == 0)
            {
                return false;
            }
            else
            {
                long l = messageLength + _FileLength;
                switch (_FileSizeUnit)
                {
                    case SizeUnit.Kilobytes:
                        return l > (_FileSizeThreshold * 1024);
                    case SizeUnit.Megabytes:
                        return l > (_FileSizeThreshold * 1048576);
                    case SizeUnit.Gigabytes:
                        return l > (_FileSizeThreshold * 1073741824);
                    default:
                        //-- default to bytes
                        return l >= _FileSizeThreshold;
                }
            }
        }
        #endregion

    }

    //=======================================================
    //Service provided by Telerik (www.telerik.com)
    //Conversion powered by NRefactory.
    //Twitter: @telerik
    //Facebook: facebook.com/telerik
    //=======================================================

}
