using System.Collections.Generic;
using System;

namespace ActivityMonitor
{
    /// <summary>
    /// Represents a session of a program usage, that is, a period of time where at least one instance of the program (program instance session) was running.
    /// Essentially an aggregation of overlapping program instance sessions.
    /// </summary>
    public class ProgramSession
    {
        #region Private fields
        private List<ProgramInstanceSession> instanceSessions = new List<ProgramInstanceSession>();
        private int NumActiveInstances; // The number of concurrently running exe process instances associated with this particular program session.
        #endregion

        #region Properties
        /// <summary>
        /// List of instance sessions that comprise this ProgramSession.
        /// </summary>
        public List<ProgramInstanceSession> InstanceSessions { get { return instanceSessions; } private set { instanceSessions = value; } }

        /// <summary>
        /// Returns whether there are any active instances of the exe in progress
        /// </summary>
        public bool IsActive { get { return (NumActiveInstances > 0); } }

        /// <summary>
        /// Returns the duration of the session
        /// </summary>
        public TimeSpan Duration { get; private set; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Consructor. Creates new instance session and sets IsActive flag to true.
        /// </summary>
        public ProgramSession(UInt32 processId, DateTime startDate)
        {
            StartDate = startDate;

            // New program session will always start with a new instance session
            AddNewInstanceSession(processId, startDate);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new instance session
        /// </summary>
        public void AddNewInstanceSession(UInt32 processId, DateTime startDate)
        {
            if (InstanceSessions.Exists(ins => (ins.IsActive) && (ins.SystemProcessId == processId))) // Guard condition
            {
                // Theoretically this block should never be entered. If it is it means that there is more than one instance of the
                // exe with the SAME processId running concurrently. This would probably mean that the first one was never closed.
                Console.WriteLine("Problem: Tried to register a new exe instance session, but another with the same processId was already running?!");
                return;
            }
            InstanceSessions.Add(new ProgramInstanceSession(processId, startDate));
            NumActiveInstances++;
        }

        /// <summary>
        /// Ends an instance session
        /// </summary>
        public void EndInstanceSession(UInt32 processId, DateTime endTime)
        {
            ProgramInstanceSession pis = InstanceSessions.Find(ins => (ins.IsActive) && (ins.SystemProcessId == processId));
            if (pis == null) // Guard condition
            {
                Console.WriteLine("Problem: Tried to close an exe instance session, but it couldn't find it!");
                return;
            }
            pis.EndSession(endTime);
            NumActiveInstances--;

            if (!IsActive)
                RegisterEndOfSession();
        }

        /// <summary>
        /// Is run on application exit
        /// </summary>
        public void EndAllInstanceSessions()
        {
            foreach (ProgramInstanceSession pis in InstanceSessions)
            {
                pis.EndSession(DateTime.Now);
            }
            RegisterEndOfSession();
        }

        /// <summary>
        /// Closes THIS session
        /// </summary>
        private void RegisterEndOfSession()
        {
            EndDate = DateTime.Now;
            Duration = EndDate - StartDate;
            NumActiveInstances = 0;
        }
        #endregion
    }
}
