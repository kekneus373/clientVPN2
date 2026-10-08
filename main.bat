@echo off
SET WAIT_TIME=5
SET EXE="C:\Program Files\OpenVPN\bin\openvpn-gui.exe"
SET CONFIG=client.ovpn

echo Stopping existing OpenVPN process...
taskkill /IM openvpn-gui.exe
timeout /t %WAIT_TIME% /nobreak >nul

REM Force kill if still running (optional fallback)
taskkill /IM openvpn-gui.exe /F 2>nul

echo Starting OpenVPN with %CONFIG%...
start "" %EXE% --connect %CONFIG%

exit 0