@echo off
echo Starting ASP.NET Development Server...
echo.

REM Kill any existing processes
taskkill /f /im "WebDev.WebServer40.exe" 2>nul
taskkill /f /im "iisexpress.exe" 2>nul

REM Start Visual Studio Development Server
"C:\Program Files (x86)\Common Files\Microsoft Shared\DevServer\11.0\WebDev.WebServer40.exe" /port:51779 /path:"%~dp0" /vpath:"/"

echo.
echo Server started at: http://localhost:51779/
echo Press Ctrl+C to stop the server
pause
