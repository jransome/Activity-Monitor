using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace ActivityMonitor
{
    public class ProcessStartStopEventArgs : EventArgs
    {
        public bool IsProcessStartEvent { get; }
        public ManagementBaseObject WmiQueryEvent { get; }
        public DateTime TimeStamp { get; }

        public ProcessStartStopEventArgs(bool isProcessStartEvent, EventArrivedEventArgs wmiQueryEventArgs)
        {
            IsProcessStartEvent = isProcessStartEvent;
            WmiQueryEvent = wmiQueryEventArgs.NewEvent;
            TimeStamp = DateTime.Now; // TODO: had issues converting Properties["TIME_CREATED"].Value to a datetime. maybe revisit?
        }
    }
}
