using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Superbr4in.ThreadUtility
{
    /// <summary>
    /// Provides methods for accessing threads the user has actually no control of.
    /// </summary>
    public static class ThreadLens
    {
        #region Constants

        /// <remarks>
        /// <a href="https://docs.microsoft.com/en-us/windows/desktop/ProcThread/thread-security-and-access-rights">
        /// Microsoft documentation</a>
        /// </remarks>
        private const uint THREAD_SUSPEND_RESUME = 0x2;

        #endregion

        /// <summary>
        /// Executes code while suspending any other thread running in the current process.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to be executed.</param>
        public static void Focus(Action action)
        {
            // Create array of current process' thread handles
            var threadHandles = Process.GetCurrentProcess().Threads
                .Cast<ProcessThread>()
                // Exclude the current thread
                .Where(pt => pt.ThreadState != ThreadState.Running)
                .Select(pt => OpenThread(THREAD_SUSPEND_RESUME, false, (uint)pt.Id))
                .ToArray();

            // Suspend other threads
            foreach (var hThread in threadHandles)
                SuspendThread(hThread);

            // Execute the specified action
            action();

            // Resume suspended threads
            foreach (var hThread in threadHandles)
                ResumeThread(hThread);
        }

        /// <summary>
        /// Retrieves user input using the <see cref="Console"/> while suspending any other thread running in the
        /// current process.
        /// </summary>
        /// <param name="prompt">The text that prompts the user to make an input.</param>
        /// <returns>An input string retrieved from the user.</returns>
        public static string FocusConsoleInput(string prompt = null)
        {
            string input = null;
            Focus(() =>
            {
                // Prompt user input
                Console.WriteLine(prompt);
                input = Console.ReadLine();
            });

            return input;
        }

        #region DLL imports

        /// <remarks>
        /// <a href="https://docs.microsoft.com/en-us/windows/desktop/api/processthreadsapi/nf-processthreadsapi-openthread">
        /// Microsoft documentation</a>
        /// </remarks>
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        /// <remarks>
        /// <a href="https://docs.microsoft.com/en-us/windows/desktop/api/processthreadsapi/nf-processthreadsapi-resumethread">
        /// Microsoft documentation</a>
        /// </remarks>
        [DllImport("kernel32.dll")]
        private static extern uint ResumeThread(IntPtr hThread);

        /// <remarks>
        /// <a href="https://docs.microsoft.com/en-us/windows/desktop/api/processthreadsapi/nf-processthreadsapi-suspendthread">
        /// Microsoft documentation</a>
        /// </remarks>
        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        #endregion
    }
}
