using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;

namespace AutoPinAgent;

public class TrayApplicationContext : ApplicationContext
{
	private readonly NotifyIcon _trayIcon;
	private readonly SecureString? _savedPassword;
	private readonly WindowObserver? _observer;
	private int _processedWindows;
	private const string TARGET_WINDOW_CLASS = "Credential Dialog Xaml Host";
	private readonly HashSet<IntPtr> _inProcessWindows = new();


	public TrayApplicationContext()
	{
		Logger.LogInfo("TrayApplicationContext initializing");
		_trayIcon = InitializeTrayIcon();

		var pwd = ShowPasswordDialog();

		if (string.IsNullOrEmpty(pwd))
		{
			MessageBox.Show("No PIN entered. Exiting.");
			Logger.LogInfo("No PIN entered. Exiting.");
			Exit();
			return;
		}

		_savedPassword = new SecureString();

		foreach (var c in pwd)
		{
			_savedPassword.AppendChar(c);
		}

		_savedPassword.MakeReadOnly();

		_observer = new WindowObserver(TARGET_WINDOW_CLASS);
		_observer.Start(HandleCredentialDialog);

		if (!_observer.IsActive())
		{
			MessageBox.Show("Failed to set up window event hook!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			Exit();
			return;
		}

		_trayIcon.ShowBalloonTip(3000, "Auto-PIN Agent",
			"Monitoring for PIN widows",
			ToolTipIcon.Info);
		Logger.LogInfo("TrayApplicationContext initialized");
	}

	private NotifyIcon InitializeTrayIcon()
	{
		var trayIcon = new NotifyIcon
		{
			Icon = SystemIcons.Shield, // Use any .ico file here
			ContextMenuStrip = new ContextMenuStrip(),
			Visible = true,
			Text = "Auto-PIN Agent"
		};
		trayIcon.ContextMenuStrip.Items.Add("Status", null, OnStatus);
		trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
		trayIcon.ContextMenuStrip.Items.Add("Exit", null, OnExit);
		return trayIcon;
	}

	private static string ShowPasswordDialog()
	{
		var prompt = new Form
		{
			Width = 300,
			Height = 150,
			FormBorderStyle = FormBorderStyle.FixedDialog,
			Text = "PIN Required",
			StartPosition = FormStartPosition.CenterScreen
		};

		var textLabel = new Label { Left = 20, Top = 20, Text = "Enter PIN:" };
		var textBox = new TextBox { Left = 20, Top = 45, Width = 240, PasswordChar = '*' };
		var confirmation = new Button
			{ Text = "Ok", Left = 185, Width = 75, Top = 80, DialogResult = DialogResult.OK };

		confirmation.Click += (sender, e) => { prompt.Close(); };
		prompt.Controls.Add(textBox);
		prompt.Controls.Add(confirmation);
		prompt.Controls.Add(textLabel);
		prompt.AcceptButton = confirmation;

		return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
	}

	private void HandleCredentialDialog(IntPtr hWnd)
	{
		Task.Run(() =>
		{
			try
			{
				if (!_inProcessWindows.Add(hWnd))
				{
					return;
				}

				var message = $"PIN Window Detected: {hWnd.ToString("X8")}";
				_trayIcon.ShowBalloonTip(3000, "Window Detected", message, ToolTipIcon.Info);
				var result = WaitForWindowReady(hWnd, "OkButton", TimeSpan.FromSeconds(2));
				if (!result)
				{
					_trayIcon.ShowBalloonTip(3000, "Error", "Window failed to initialize in time.", ToolTipIcon.Error);
					return;
				}

				ProcessCredentialDialog(hWnd);
			}
			catch (Exception ex)
			{
				_trayIcon.ShowBalloonTip(3000, "Error", ex.Message, ToolTipIcon.Error);
				Logger.LogException(ex);
			}
			finally
			{
				_inProcessWindows.Remove(hWnd);
			}
		});
	}

	private bool WaitForWindowReady(IntPtr hWnd, string target, TimeSpan timeout)
	{
		if (hWnd == IntPtr.Zero)
		{
			return false;
		}

		var startTime = DateTime.Now;

		while (DateTime.Now - startTime < timeout)
		{
			var window = AutomationElement.FromHandle(hWnd);
			var control = window?.FindFirst(TreeScope.Descendants,
				new AndCondition(Automation.RawViewCondition,
					new PropertyCondition(AutomationElement.AutomationIdProperty, target)));

			if (control != null)
			{
				return true;
			}

			Thread.Sleep(250);
		}

		return false;
	}

	private void ProcessCredentialDialog(IntPtr hWnd)
	{
		if (hWnd == IntPtr.Zero)
		{
			return;
		}

		var securityWindow = AutomationElement.FromHandle(hWnd);

		var editControl = securityWindow?.FindFirst(TreeScope.Descendants,
			new AndCondition(Automation.RawViewCondition,
				new PropertyCondition(AutomationElement.AutomationIdProperty, "PasswordField_3"),
				new PropertyCondition(AutomationElement.NameProperty, "Security Key PIN")));
		if (editControl is null)
		{
			return;
		}

		var okControl = securityWindow?.FindFirst(TreeScope.Descendants,
			new AndCondition(Automation.RawViewCondition,
				new PropertyCondition(AutomationElement.AutomationIdProperty, "OkButton")));
		if (okControl is null)
		{
			return;
		}

		// Enter PIN
		editControl.SetFocus();
		var valuePattern = editControl.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
		var pwd = new NetworkCredential(string.Empty, _savedPassword).Password;
		valuePattern?.SetValue(pwd);

		// Click OK
		var invokePattern = okControl.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
		invokePattern?.Invoke();
		_processedWindows++;
		Logger.LogInfo("PIN entered and OK button clicked");
	}

	private void OnStatus(object sender, EventArgs e)
	{
		var hookStatus = _observer?.IsActive() ?? false ? "Active" : "Inactive";
		MessageBox.Show(
			$"Auto-PIN Agent is running.\n\nTarget window class: {TARGET_WINDOW_CLASS}\nHook status: {hookStatus}\nPIN Windows processed: {_processedWindows}",
			"Status",
			MessageBoxButtons.OK,
			MessageBoxIcon.Information);
	}

	private void Exit()
	{
		_trayIcon.Visible = false;
		_trayIcon.Dispose();
		_observer?.Stop();
		Application.Exit();
	}

	private void OnExit(object sender, EventArgs e) => Exit();
}