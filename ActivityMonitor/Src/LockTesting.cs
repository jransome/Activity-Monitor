using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ActivityMonitor
{

    /// <summary>
    /// Not part of the actual program, just for testing how lock works
    /// </summary>
    public class LockTesting
    {
        // Singleton implementation
        private static readonly LockTesting instance = new LockTesting();
        public static LockTesting Instance { get { return instance; } }

        public int count = 0;
        private Thread t1;
        private Thread t2;
        private readonly Object baton = new Object();

        public void StartThreads()
        {
            count = 0;
            t1 = new Thread(CountChanger1);
            t2 = new Thread(CountChanger2);

            t1.Start();
            Thread.Sleep(300);
            t2.Start();
        }

        void CountChanger1()
        {
            lock (baton)
            {
                for (int i = 0; i < 10; i++)
                {
                    count++;
                    Console.WriteLine("Thread {0} INCREASED count to {1}", Thread.CurrentThread.ManagedThreadId, count);
                    Thread.Sleep(500);
                }
            }
        }

        void CountChanger2()
        {
            lock (baton)
            {
                for (int i = 0; i < 10; i++)
                {
                    count--;
                    Console.WriteLine("Thread {0} REDUCED count to {1}", Thread.CurrentThread.ManagedThreadId, count);
                    Thread.Sleep(500);
                }
            }
        }

        void IncrementCount()
        {
            while (count < 20)
            {
                lock (baton)
                {
                    count++;
                    Console.WriteLine("Thread {0} incremented count to {1}", Thread.CurrentThread.ManagedThreadId, count);
                    Thread.Sleep(1000);
                }
            }
        }

    }
}
