I need you to generate a complete, modern C# WinForms application (.NET 10) for a lightweight VPN launcher. 

### 1. Project Structure & Paths
- **App Directory:** `app/`
- **Scripts are in the Project Root.**
- **Config File:** `app/appsettings.json` (Never embed this in the build; read it at runtime)
- **Output:** A single-file, self-contained executable that can be run from a desktop shortcut or the app folder.

### 2. Core Requirements
- **Single Instance:** The app must check if an instance is already running. If so, bring the existing instance to focus. If not, run normally.
- **System Tray:** When the user clicks the "Close" (X) button, the window should **not** terminate. Instead, minimize the app to the System Tray (NotifyIcon) with a right-click context menu (Exit).
- **Startup:** On initial load, call a "Refresh Icons" action automatically.

### 3. UI Design (Modern & Lightweight)
- **Look & Feel:** Modern, clean, and minimal. Use a **light-blue linear gradient** for the background (from very light sky blue to a slightly deeper light blue). If gradients are tricky, use a solid, pleasant light blue (#E0F7FA or similar).
- **Window Size:** Fixed dimensions (e.g., 320x450) optimized for 1366x768 screens. No resizing allowed. Only show the Close button in the title bar.
- **Centering:** All elements should be vertically and horizontally centered within the form.
- **Font:** Use a modern system font (e.g., Segoe UI Variable) with bold weights for readability.

### 4. UI Layout & Elements
1. **Top Label:** A large, bold, centered text label read from `appsettings.json` at the top.
2. **Main Actions Button:** A large, prominent button in the center.
   - **Content:** Icon (top) + Label (bottom), loaded dynamically from `appsettings.json`.
   - **Action:** When clicked, runs the primary batch file defined in the JSON.
   - **Style:** Rounded corners, subtle shadow, hover effect (change background color slightly).
3. **Secondary Button:** A smaller, text-only button below the main button.
   - **Content:** Label loaded from `appsettings.json`.
   - **Action:** Runs the secondary batch file defined in the JSON.
4. **Refresh Button:** A small button with a refresh icon (🗘) in the top-left corner.
   - **Action:** Calls the `RefreshIcons()` method to unhide RDP files and refresh the UI.
5. **Help Tooltip:** A small "❓" icon in the top-right corner.
   - **Behavior:** On hover or click, a tooltip appears reminding the user where the `appsettings.json` file is located.

### 5. Logic & Functionality
- **Config Parsing:** Read `appsettings.json` to determine:
  - Primary/Secondary batch file paths.
  - Primary/Secondary button labels and icon paths.
  - A list of `.rdp` file paths and a "hidden attribute" status that changes based on the connected VPN.
  - RDP icon hiding feature use (true/false).
- **RDP Icon Management:** 
  - Implement a `RefreshIcons()` method that unhides icons on first run / refresh clicked.
  - Implement a `SetIcons()` method for managing the hide attribute:
    1. Invokes when any of 2 VPN connection buttons is pressed.
    2. Sets the `FileAttributes.Hidden` property on `.rdp` files accordingly (e.g., if VPN A is connected, hide VPN B's RDP files).
    3. Forces a Windows Explorer refresh (`SHChangeNotify`) so the desktop icons update immediately without a reboot.
- **Process Execution:** 
  - Use `System.Diagnostics.Process` to run the batch files.
  - Exit (to systray) after succesful Batch file run - do not trick users into thinking they aren't connected yet.

### 6. Code Quality & Deliverables
- Provide the full `Program.cs`, `MainForm.cs`, and `MainForm.Designer.cs` (or use code-only layout if preferred).
- Utilize the `appsettings.json` sample structure.
- Ensure all paths are handled robustly (use `Path.Combine` and verify file existence before binding). Throw error window dialog if a required file is missing, don't die quietly.
- Add comments explaining the logic and tray management.
- Use `dotnet` for evaluating and bulding.
- Make sure the UI scales correctly if DPI settings are not 100%.
- Ask me any clarifying questions beforehand so we don't start from scratch 10 times.