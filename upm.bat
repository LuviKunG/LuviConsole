@echo off
set /p version=Enter Version: 
echo %version%
git subtree split --prefix="Assets/LuviConsole" --branch upm
git tag %version% upm
pause