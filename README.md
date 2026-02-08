# Auto-PIN Agent

Auto-PIN Agent is a lightweight Windows tray application designed to streamline your workflow when using security keys (like YubiKey) or other services that frequently prompt for a PIN in Windows Security dialogs.

## How it Works

The application monitors for Windows Security credential dialogs. When such a window is detected, the agent automatically:
1.  Identifies the PIN input field using UI Automation.
2.  Fills in your pre-configured PIN.
3.  Automatically "clicks" the OK button.

Your PIN is requested only once when the application starts and is stored securely.

## Requirements

-   Windows OS
-   .NET Framework 4.8

## Usage

1.  **Launch the application:** Run `AutoPinAgent.exe`.
2.  **Enter PIN:** A dialog will appear asking for your PIN. This PIN will be used for all subsequent Windows Security prompts during this session.
3.  **Tray Icon:** Look for the Shield icon in your system tray. 
    -   **Status:** Right-click the icon and select "Status" to see how many windows have been processed.
    -   **Exit:** Right-click and select "Exit" to close the application and clear the PIN from memory.
4.  **Automatic Filling:** Whenever a Windows Security PIN prompt appears, the agent will detect it, show a balloon notification, and automatically fill/submit the PIN.

## Credits

This app was inspired by [AutoInsertPin](https://github.com/joseangelmt/AutoInsertPin)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.
