using System;

namespace ABB.SrcML.VisualStudio.SrcMLService
{
	public class ExceptionFormatter
	{
        public static string CreateMessage(Exception ex)
        {
            return String.Format("Message: {0}\nStackTrace: {1}", ex.Message, ex.StackTrace);
        }

        public static string CreateMessage(Exception ex, string message)
        {
            return String.Format("{0}\nMessage: {1}\nStackTrace: {2}", message, ex.Message, ex.StackTrace);
        }
	}
}
