using System;
using System.Collections.Generic;
using System.Windows;

namespace ActivityMonitor
{
    /// <summary>
    /// This is the thing that controls all objects in the app. There might be a better architectural implementation... must look at MVVM
    /// </summary>
    public sealed class MainController
    {
        private static readonly MainController instance = new MainController();
        public static MainController Instance { get { return instance; } }

        /// <summary>
        /// Private constructor. Subscribes to Application lifetime events to do cleanup.
        /// </summary>
        private MainController()
        {
            Application.Current.Startup += CheckForStraySessions;
            Application.Current.Exit += CloseAllSessions;
        }

        //public List<Program> ScrapeRunningPrograms()
        //{
        //    ProgramRecorder.Instance.RecordRunningProgramSnapshot();
        //    return GetStoredPrograms();
        //}

        //public List<Program> GetStoredPrograms()
        //{
        //    return TrackedPrograms.Instance.Index;
        //}

        /// <summary>
        /// Called at startup to check all tracked programs for open sessions, which they shouldn't have, as
        /// they should all be closed when the application is closed. For example, this deals with the
        /// possibility that CloseAllSessions might not be called if the computer crashes or something.
        /// </summary>
        private void CheckForStraySessions(object sender, StartupEventArgs e)
        {
            MessageBox.Show("startup"); // TODO: fix this
            foreach (Program trackedProgram in TrackedPrograms.Instance.Index)
            {
                if (trackedProgram.IsRunning)
                {
                    trackedProgram.LogAllInstanceStopped();
                    string message = $"{trackedProgram.ExeName}'s sessions were not closed then Activity Monitor was last shut down! The session has now been closed but this means {trackedProgram.ExeName}'s total run time is greater than what it should be!";
                    MessageBox.Show(message, "Oh no!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        /// <summary>
        /// Closes all sessions on the shutdown of the application.
        /// </summary>
        private void CloseAllSessions(object sender, ExitEventArgs e)
        {
            foreach (Program trackedProgram in TrackedPrograms.Instance.Index)
            {
                trackedProgram.LogAllInstanceStopped();
            }
        }

    }
}
