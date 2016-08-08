@echo off
taskkill /f /IM "Ulterius Server.exe"
pushd %~dp0
"Ulterius Server.exe"
popd