using System;

namespace lms
{
	public class RemoteException : Exception
	{
		readonly private string stackTrace;
		internal RemoteException(string type, string message, string source, string stackTrace) : base(message) {
			Source = source;
			this.stackTrace = stackTrace;
		}
		public override string StackTrace => stackTrace;
	}
}
