using System;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Logs a given message to the console.
    /// </summary>
    public class Log : Do
    {
        /// <summary>
        /// Create an instance of a <see cref="Log"/> task.
        /// </summary>
        /// <param name="message">A message to print.</param>
        public Log(string message) : base(() => UnityEngine.Debug.Log(message))
        {
        }

        /// <summary>
        /// Create an instance of a <see cref="Log"/> task.
        /// </summary>
        /// <param name="message">A delegate that returns a string to print.</param>
        public Log(Action<string> message) : base(() => UnityEngine.Debug.Log(message))
        {
        }
    }
}