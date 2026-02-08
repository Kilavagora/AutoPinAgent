using System;
using System.Runtime.InteropServices;
using System.Text;


namespace AutoPinAgent;

public class WindowObserver(string targetClassname)
{
	// Constants for the hook
	private const uint EVENT_OBJECT_SHOW = 0x8002;
	private const uint WINEVENT_OUTOFCONTEXT = 0;

	// We must store this in a variable so it isn't garbage collected!
	private WinEventDelegate _delegate;
	private IntPtr _hookId;

	private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
		IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

	[DllImport("user32.dll")]
	static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
		WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

	[DllImport("user32.dll")]
	static extern bool UnhookWinEvent(IntPtr hWinEventHook);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

	public void Start(Action<IntPtr> onWindowDetected)
	{
		_delegate = (hWinEventHook, eventType, hwnd, idObject, idChild, dwEventThread, dwmsEventTime) =>
		{
			if (idObject != 0 || idChild != 0 || hwnd == IntPtr.Zero)
			{
				return;
			}

			var className = new StringBuilder(256);
			var length = GetClassName(hwnd, className, className.Capacity);
			if (length == 0)
			{
				return;
			}

			if (className.ToString().Equals(targetClassname, StringComparison.OrdinalIgnoreCase))
			{
				onWindowDetected(hwnd);
			}
		};

		_hookId = SetWinEventHook(EVENT_OBJECT_SHOW, EVENT_OBJECT_SHOW,
			IntPtr.Zero, _delegate, 0, 0, WINEVENT_OUTOFCONTEXT);
	}

	public bool IsActive()
	{
		return _hookId != IntPtr.Zero;
	}

	public void Stop()
	{
		UnhookWinEvent(_hookId);
		_hookId = IntPtr.Zero;
	}
}