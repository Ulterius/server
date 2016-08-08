@echo off
taskkill /f /im "Ulterius Server.exe"
:exit
start "" "%~dp0Ulterius Server.exe"

