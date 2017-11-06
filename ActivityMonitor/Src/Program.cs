using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace ActivityMonitor
{
    /// <summary>
    /// Represents an executable that is monitored for activity
    /// </summary>
    public class Program
    {
        private List<ProgramSession> sessions = new List<ProgramSession>();

        public string ExeName { get; }
        public string Description { get; }
        public string Path { get; }
        public bool IsRunning { get; private set; }
        public TimeSpan TotalRunTime
        {
            get
            {
                TimeSpan Total = new TimeSpan();
                foreach (ProgramSession ps in Sessions)
                {
                    if (!ps.IsActive)
                    {
                        Total += ps.Duration;
                    }
                }
                return Total;
            }
        }
        public int TotalSessions { get { return sessions.Count; } }
        public List<ProgramSession> Sessions { get { return sessions; } }
        /// <summary>
        /// Process Id associated with exe when it was found by the RecordRunningProgramSnapshot method
        /// Only purpose is to supply the recorder with an id to create a new session with without having to
        /// break the law of demeter by doing: snapshottedProgram.Sessions.Last().InstanceSessions.First().SystemProcessId;
        /// </summary>
        public UInt32 InitialProcessId { get; }

        /// <summary>
        /// Constructor for a new program.
        /// </summary>
        public Program(string exeName, string description, string path, UInt32 currentProcessId, DateTime sessionCreationDate)
        {
            ExeName = exeName;
            Description = description;
            Path = path;
            InitialProcessId = currentProcessId;

            //Log the Initial Program Session
            Sessions.Add(new ProgramSession(currentProcessId, sessionCreationDate));
            IsRunning = true;
        }

        /// <summary>
        /// Logs a new exe instance session using DateTime.Now as the start time. Can be done at any time in theory
        /// </summary>
        public void RegisterNewInstanceSession(UInt32 processId, DateTime startTime)
        {
            // case: new instance is part of an existing user session
            if (IsRunning)
            {
                Sessions.Last().AddNewInstanceSession(processId, startTime);
            }
            else
            // case: new instance is also start of a new user session
            {
                Sessions.Add(new ProgramSession(processId, startTime));
                IsRunning = true;
            }
        }

        #region Logging when exe instances stop
        /// <summary>
        /// Logs the end of a exe instance session. This is called when the process stop trace event happens.
        /// </summary>
        public void LogInstanceStopped(UInt32 processId, DateTime endTime)
        {
            if (!IsRunning) //Guard condition
            {
                Console.WriteLine("Error: Tried to log end of {0} instance, but program was not flagged as running", ExeName);
                return;
            }

            Sessions.Last().EndInstanceSession(processId, endTime);

            if (!Sessions.Last().IsActive)
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// This method is called when the application is closed. It therefore 'closes' all open sessions at the datetime the application closes.
        /// </summary>
        public void LogAllInstanceStopped()
        {
            if (IsRunning)
            {
                Sessions.Last().EndAllInstanceSessions();
                IsRunning = false;
            }
            // TODO: add console log for error catching
        }
        #endregion
    }
}
