using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Data;

namespace ActivityMonitor
{
    /// <summary>
    /// Singleton class for managing and storing a list of tracked programs. Sealed prevents creation of classes derriving from this one (not necessary in this project)
    /// </summary>
    public sealed class TrackedPrograms
    {
        // Singleton implementation TODO: what about making it static?
        private static readonly TrackedPrograms instance = new TrackedPrograms();
        public static TrackedPrograms Instance { get { return instance; } }

        // Main index property
        private ObservableCollection<Program> index = new ObservableCollection<Program>();
        public ObservableCollection<Program> Index { get { return index; } private set { index = value; } }

        /// <summary>
        /// This makes the observable Index collection thread safe. Without it, we were getting an error
        /// owing to a non UI thread trying to change the collection. Note that it works even though we
        /// haven't used a lock with uiBaton at all.
        /// </summary>
        private readonly Object uiBaton = new Object();
        private TrackedPrograms()
        {
            BindingOperations.EnableCollectionSynchronization(Index, uiBaton);
        }

        /// <summary>
        /// Saves a program only if it's not already tracked by looking at exe name. Returns a boolean indicating success.
        /// </summary>
        public bool TrySaveProgram(Program program)
        {
            bool succeeded = false;
            if (!IsAlreadyTracked(program.ExeName)) // guard condition
            {
                Index.Add(program);
                succeeded = true;
            }
            return succeeded;
        }

        public Program GetStoredProgramByName(string exeName)
        {
            // FOR FUTURE REF: the Exists method takes a predicate (method that returns a bool) which is specified
            // here as a lambda expression. it looks a bit like the es6 js function declarations, where tp is the
            // parameter (in this case each tracked process in the list), and the logic is on the right side of the => sign.

            List<Program> list = new List<Program>(Index);
            return list.Find(tp => tp.ExeName == exeName);
        }

        public bool IsAlreadyTracked(string exeName)
        {
            List<Program> list = new List<Program>(Index);
            return list.Exists(tp => tp.ExeName == exeName);
        }
    }
}
