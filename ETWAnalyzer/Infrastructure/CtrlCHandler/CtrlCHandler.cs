using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.ProcessTools
{
    /// <summary>
    /// Allows to register callbacks when the process is stopped via Ctrl-C or Ctrl-Break
    /// </summary>
    class CtrlCHandler
    {
        private delegate bool ConsoleCtrlHandlerDelegate(int sig);

        readonly ConsoleCtrlHandlerDelegate myHandler;

        public static CtrlCHandler Instance = new CtrlCHandler();
        readonly object myLock = new object();

        List<WeakReference<Action>> myCallbacks = new List<WeakReference<Action>>();

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate handler, bool add);

        CtrlCHandler()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            myHandler += CtrlCCallback;
            SetConsoleCtrlHandler(myHandler, true);
        }

        bool CtrlCCallback(int signal)
        {
            lock (myLock)
            {
                foreach (var callback in myCallbacks)
                {
                    if (callback.TryGetTarget(out Action existingAction))
                    {
                        existingAction();
                    }
                }
            }
            return false;
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            // kill child processes also when parent process exits 
            CtrlCCallback(0);
        }


        /// <summary>
        /// Register a callback when the process exits or 
        /// </summary>
        /// <param name="acc"></param>
        public void Register(Action acc)
        {
            lock (myLock)
            {
                myCallbacks.Add(new WeakReference<Action>(acc) );

                // cleanup delegates
                myCallbacks = myCallbacks.Where(x => x.TryGetTarget(out Action _)).ToList();
            }
        }

    }
}
