# clientVPN v2

A Windows Forms launcher for selecting the primary or backup Crystal Finance VPN connection. It runs the configured batch file, can hide the corresponding RDP shortcut, and stays in the system tray after connecting.

## Build

Install the .NET 10 SDK on Windows, then run from the repository root:

```powershell
dotnet build app.slnx
dotnet publish app\app.csproj -c Release -r win-x64 --self-contained true
```

The publish output is `app\bin\Release\net10.0-windows\win-x64\publish\`. The executable is self-contained and single-file, but `appsettings.json`, the batch files, and configured icon files are distributed as external files alongside it.

## Configuration

Edit `app\appsettings.json` before building or publishing. The project copies it to the output directory; at runtime the launcher reads `appsettings.json` beside the executable. JSON property names are case-insensitive.

- `title`: text shown above the connection buttons.
- `primaryButton` and `secondaryButton`: each requires a `label` and `batchPath`; optional `iconPath` sets a button icon. Relative paths are resolved from the directory containing `appsettings.json`.
- `hideRdp`: when `true`, selecting a connection hides the RDP files assigned to that profile and unhides the others. Refreshing the RDP icons in the app makes all configured files visible.
- `rdpFiles`: array of RDP shortcuts. Each entry has a `path` and `hideWhen` (`primary` or `secondary`), indicating which selection hides that shortcut. Paths can be absolute or relative to `appsettings.json`.

The supplied batch files expect OpenVPN GUI at `C:\Program Files\OpenVPN\bin\openvpn-gui.exe` and the corresponding `client.ovpn` or `reserve.ovpn` in OpenVPN config folder (detected automatically - check documentation). Update the scripts if your installation or VPN profile names differ. The launcher needs permission to change the hidden attribute of the configured RDP files.

## License

Under MIT. The `app/ic_launcher.ico` is pulled from [OpenVPN Branding Assets](https://openvpn.net/branding/) - change if needed, left for clarity.