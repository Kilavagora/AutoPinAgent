using System;
using System.Diagnostics;

namespace AutoPinAgent;

public static class Logger
{
	private const string SourceName = "AutoPinAgent";
	private const string LogName = "Application";

	public static void LogException(Exception ex)
	{
		if (!EventLog.SourceExists(SourceName))
		{
			EventLog.CreateEventSource(SourceName, LogName);
		}

		var message = $"Message: {ex.Message}\n\nStackTrace: {ex.StackTrace}";

		EventLog.WriteEntry(SourceName, message, EventLogEntryType.Error, 1001);
	}

	public static void LogInfo(string info)
	{
		if (EventLog.SourceExists(SourceName))
		{
			EventLog.WriteEntry(SourceName, info, EventLogEntryType.Information);
		}
	}
}