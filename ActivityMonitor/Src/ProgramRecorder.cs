using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ActivityMonitor
{
    public sealed class ProgramRecorder
    {
        // Singleton implementation
        private static readonly ProgramRecorder instance = new ProgramRecorder();
        public static ProgramRecorder Instance { get { return instance; } }

        private readonly Object baton = new Object(); // The one and only 'key' for accessing code within the lock blocks below
        private List<KeyValuePair<string, uint>> unsavedNewStartTraces = new List<KeyValuePair<string, uint>>(); // Where start traces for untracked exes are unsuccessfully saved

        /// <summary>
        /// Constructor.
        /// </summary>
        private ProgramRecorder()
        {
            ProgramListener.Instance.ProcessWasStartedStopped += RecordProcessStartStop;
        }

        /// <summary>
        /// This function is to be run on startup. Asks the listener to scrape all running processes and stores them, with
        /// the associated description from FileVersionInfo.
        /// </summary>
        public void RecordRunningProgramSnapshot()
        {
            lock (baton)
            {
                Console.WriteLine("Recording snapshot");
                ManagementObjectCollection runningPrograms = ProgramListener.Instance.GetRunningPrograms();
                foreach (ManagementObject snapshottedProgram in runningPrograms)
                {
                    if (IsAlreadyTrackedExe(snapshottedProgram))
                    {
                        // Already tracked so add new session
                        UInt32 processId = (UInt32)snapshottedProgram["ProcessId"];
                        DateTime processCreationDate = ManagementDateTimeConverter.ToDateTime((string)snapshottedProgram["CreationDate"]);
                        Program tp = GetStoredProgram(snapshottedProgram);
                        tp.RegisterNewInstanceSession(processId, processCreationDate);
                    }
                    else
                    {
                        // New exe so add to tracked programs
                        RecordNewProgram(snapshottedProgram);
                    }
                }
                Console.WriteLine("Snapshot recorded");
            }
        }

        private void RecordProcessStartStop(ProcessStartStopEventArgs processEventDetails)
        {
            lock (baton)
            {
                Console.WriteLine("Start/stop event occuring");
                ManagementBaseObject processDetails = processEventDetails.WmiQueryEvent;
                if (processEventDetails.IsProcessStartEvent)
                {
                    if (IsAlreadyTrackedExe(processDetails))
                    {
                        // New process detected on a tracked exe
                        Console.WriteLine("NEW INSTANCE OF TRACKED EXE {0} DETECTED!", GetExeName(processDetails));

                        UInt32 processId = (UInt32)processDetails["ProcessId"];
                        Program sp = GetStoredProgram(processDetails);
                        sp.RegisterNewInstanceSession(processId, processEventDetails.TimeStamp);
                    }
                    else
                    {
                        // New process detected on a never before seen exe
                        Console.WriteLine("NEW INSTANCE OF UNTRACKED EXE {0} DETECTED!", GetExeName(processDetails));

                        ManagementBaseObject newProgramDetails = SupplementStartTraceInfo(processDetails);
                        if (newProgramDetails == null)
                        {
                            string name = GetExeName(processDetails);
                            UInt32 processId = (UInt32)processDetails["ProcessId"];
                            unsavedNewStartTraces.Add(new KeyValuePair<string, uint>(name, processId));
                            Console.WriteLine($"PROGRAM RECORDER: New untracked program {name} detected, but attempt to query running processes for it has returned no results");
                        }
                        else
                        {
                            RecordNewProgram(newProgramDetails);
                        }
                    }
                }
                else
                {
                    if (IsAlreadyTrackedExe(processDetails))
                    {
                        // End of process detected on a tracked exe
                        Console.WriteLine("END OF PROCESS FOR TRACKED EXE {0} DETECTED!", GetExeName(processDetails));

                        Program sp = GetStoredProgram(processDetails);
                        UInt32 processId = (UInt32)processDetails["ProcessId"];
                        sp.LogInstanceStopped(processId, processEventDetails.TimeStamp);
                    }
                    else
                    {
                        // End of process detected on a never before seen exe
                        // Probably shouldn't be possible... unless it's the end of a 'ghost' process - one where we detected the start trace
                        // but could not find it on querying running processes (stored in unsavedNewStartTraces), which we chack for below.
                        string name = GetExeName(processDetails);
                        UInt32 processId = (UInt32)processDetails["ProcessId"];
                        var kvp = new KeyValuePair<string, uint>(name, processId);
                        if (unsavedNewStartTraces.Exists(unst => unst.Equals(kvp)))
                        {
                            Console.WriteLine("PROGRAM RECORDER: END OF PROCESS FOR UNSAVED UNTRACKED EXE {0} DETECTED! (THIS IS OKAY)", GetExeName(processDetails));
                            unsavedNewStartTraces.Remove(kvp);
                        }
                        else
                        {
                            Console.WriteLine("PROGRAM RECORDER: END OF PROCESS FOR UNTRACKED EXE {0} DETECTED! (THIS SHOULDN'T BE HAPPENING!!)", GetExeName(processDetails));
                        }
                    }
                }
                Console.WriteLine("Start/stop event dealt with");
            }
        }

        #region Saving helpers
        /// <summary>
        /// Saves a new program to TrackedPrograms. Has to check if this new program comes from the scraper method, or
        /// from a start stop trace event as the latter does not include a exe path variable, which therefore must be sourced
        /// from a further Win32_Process query.
        /// </summary>
        private void RecordNewProgram(ManagementBaseObject programDetails)
        {
            string exeName = (string)programDetails["Name"];
            string path = (string)programDetails["ExecutablePath"];
            UInt32 processId = (UInt32)programDetails["ProcessId"];
            DateTime processCreationDate = ManagementDateTimeConverter.ToDateTime((string)programDetails["CreationDate"]);

            string exeDescription = "None available";
            try { exeDescription = FileVersionInfo.GetVersionInfo(path).FileDescription; } catch (Exception) { } // TODO: do something with exception

            Program newExeProgram = new Program(exeName, exeDescription, path, processId, processCreationDate);
            bool saved = TrackedPrograms.Instance.TrySaveProgram(newExeProgram);

            if (!saved) // failed to save means we have a problem TODO: revisit error catching?
            {
                Console.WriteLine("PROGRAM RECORDER: NEW INSTANCE OF UNTRACKED EXE {0} WAS SNAPSHOTTED, BUT FAILED TO SAVE BECAUSE IT WASN'T UNTRACKED AFTER ALL?!!", exeName);
            }
        }

        /// <summary>
        /// Takes StartTrace event details and queries Win32_Process for further information (exe path info)
        /// so it can be saved by RecordNewProgram. This is only for when an untacked exe is recorded as a start trace
        /// event and needs to be saved in 'entirety'.
        /// </summary>
        private ManagementBaseObject SupplementStartTraceInfo(ManagementBaseObject startTraceDetails)
        {
            uint newProcessId = (UInt32)startTraceDetails["ProcessId"];
            string newProcessName = (string)startTraceDetails["ProcessName"];
            string wmiQueryString = "SELECT ProcessId, Name, ExecutablePath, CreationDate FROM Win32_Process " +
                                    $"WHERE ProcessId = '{newProcessId}' AND Name = '{newProcessName}'";
            ManagementObjectCollection results = new ManagementObjectSearcher(wmiQueryString).Get();

            if (results.Count == 0)
            {
                return null;
            }
            else
            {
                ManagementBaseObject win32Process = results.OfType<ManagementObject>().FirstOrDefault();
                return win32Process;
            }
        }

        private void RecordNewInstanceSession(ManagementBaseObject programDetails)
        {

        }
        #endregion

        /// <summary>
        /// Methods that correspond to methods in the TrakedPrograms class.
        /// Just saves having to type TrackedPrograms.Instance... every time
        /// </summary>
        #region Query helpers
        private Program GetStoredProgram(ManagementBaseObject exeDetails)
        {
            return TrackedPrograms.Instance.GetStoredProgramByName(GetExeName(exeDetails));
        }

        private bool IsAlreadyTrackedExe(ManagementBaseObject exeDetails)
        {
            return TrackedPrograms.Instance.IsAlreadyTracked(GetExeName(exeDetails));
        }

        /// <summary>
        /// Win32_Process and Win32_ProcessTrace store the exe name under differently named properties.
        /// </summary>
        private string GetExeName(ManagementBaseObject exeDetails)
        {
            string name;
            if (exeDetails.ClassPath.ClassName == "Win32_Process")
            {
                name = (string)exeDetails["Name"];
            }
            else
            {
                name = (string)exeDetails["ProcessName"];
            }
            return name;
        }
        #endregion
    }
}
