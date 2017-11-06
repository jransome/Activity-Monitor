using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivityMonitor
{
    public class ProgramInstanceSession
    {
        public UInt32 SystemProcessId { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; set; }
        public TimeSpan Duration { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ProgramInstanceSession(UInt32 processId, DateTime startDate)
        {
            StartDate = startDate;
            SystemProcessId = processId;
            IsActive = true;
        }

        public void EndSession(DateTime timeStamp)
        {
            if (IsActive)
            {
                EndDate = timeStamp;
                Duration = EndDate - StartDate;
                IsActive = false;
            }
            // TODO: add console log for error catching
        }
    }
}
