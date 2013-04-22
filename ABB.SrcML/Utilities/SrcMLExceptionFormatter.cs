using System;

namespace ABB.SrcML.Utilities
{
    /// <summary>
    /// Format the exception message
    /// </summary>
	public class SrcMLExceptionFormatter
	{
        /// <summary>
        /// Return a formatted message string.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string CreateMessage(Exception ex)
        {
            return String.Format("Message: {0}\nStackTrace: {1}", ex.Message, ex.StackTrace);
        }

        /// <summary>
        /// Return a formatted message string.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string CreateMessage(Exception ex, string message)
        {
            return String.Format("{0}\nMessage: {1}\nStackTrace: {2}", message, ex.Message, ex.StackTrace);
        }
	}
}
