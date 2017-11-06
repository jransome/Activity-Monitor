using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Windows;

namespace ActivityMonitor
{
    /// <summary>
    /// Singleton responsible for listening out for process start and stop events, as well as getting snapshots of all running processes
    /// TODO: look into making it a static class?
    /// </summary>
    public class ProgramListener
    {
        private static readonly ProgramListener instance = new ProgramListener();
        public static ProgramListener Instance { get { return instance; } }

        /// <summary>
        /// Private constructor. Sets up the event listeners.
        /// </summary>
        private ProgramListener()
        {
            SetupProcessEventWatchers();
        }

        public delegate void ProcessStartStopEventHandler(ProcessStartStopEventArgs e);
        public event ProcessStartStopEventHandler ProcessWasStartedStopped; // The event that triggers the above delegate
        protected virtual void OnProcessStartStop(ProcessStartStopEventArgs e) // protected and virtual by convention TODO: make this class sealed and work out the virtual/protected keywords
        {
            if (ProcessWasStartedStopped != null)
                ProcessWasStartedStopped(e);
        }

        public void SetupProcessEventWatchers()
        {
            ManagementEventWatcher processStartWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            processStartWatcher.EventArrived += new EventArrivedEventHandler(ProcessStartStopHandler); // Subscribing directly to the method seems to work just the same???
            processStartWatcher.Start();

            ManagementEventWatcher processStopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            processStopWatcher.EventArrived += new EventArrivedEventHandler(ProcessStartStopHandler); // Subscribing directly to the method seems to work just the same???
            processStopWatcher.Start();
        }

        /// <summary>
        /// Handles events raised by the process start and stop watchers, determines whether it was a start or stop trace and
        /// raises the ProcessWasStartedStopped event.
        /// </summary>
        private void ProcessStartStopHandler(object sender, EventArrivedEventArgs e)
        {
            string classQueried = e.NewEvent.SystemProperties["__Class"].Value.ToString();
            bool isStartTrace = (classQueried == "Win32_ProcessStartTrace");

            ProcessStartStopEventArgs args = new ProcessStartStopEventArgs(isStartTrace, e);
            OnProcessStartStop(args);
        }

        /// <summary>
        /// Returns a snapshot of running processes as a ManagementObjectCollection
        /// </summary>
        public ManagementObjectCollection GetRunningPrograms()
        {
            string wmiQueryString = "SELECT ProcessId, Name, ExecutablePath, CreationDate FROM Win32_Process";
            ManagementObjectCollection win32ProcessResults = new ManagementObjectSearcher(wmiQueryString).Get();
            return win32ProcessResults;
        }

        #region Helpers

        #endregion
    }
}
