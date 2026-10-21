@echo off
rem ===================================================================
rem  FE3H Randomizer - "run the randomizer, then sort the output"
rem
rem  Starts "Fire Emblem Three Houses Randomizer.exe" with its working
rem  folder already pointing at .\output, waits for you to close it,
rem  and then runs "Organize Randomized Files.bat" on that folder.
rem
rem  Simply save every randomized file into the folder that opens, and
rem  everything ends up in the correct patch structure by the time you
rem  are done - no .bat to write by hand, no re-running a script once
rem  per file.
rem ===================================================================

setlocal EnableExtensions
title FE3H Randomizer - Run and Sort

set "ME=%~dp0"
if "%ME:~-1%"=="\" set "ME=%ME:~0,-1%"
set "OUTDIR=%ME%\output"

set "EXE="
if exist "%ME%\Fire Emblem Three Houses Randomizer.exe" set "EXE=%ME%\Fire Emblem Three Houses Randomizer.exe"
if not defined EXE if exist "%ME%\Fire Emblem Three Houses Randomizer V2.exe" set "EXE=%ME%\Fire Emblem Three Houses Randomizer V2.exe"
if not defined EXE if exist "%ME%\..\Fire Emblem Three Houses Randomizer.exe" set "EXE=%ME%\..\Fire Emblem Three Houses Randomizer.exe"
if not defined EXE if exist "%ME%\..\Fire Emblem Three Houses Randomizer V2.exe" set "EXE=%ME%\..\Fire Emblem Three Houses Randomizer V2.exe"

if not defined EXE (
    echo.
    echo    The randomizer .exe was not found next to this .bat.
    echo    Drag "Fire Emblem Three Houses Randomizer.exe" onto this .bat,
    echo    or put this .bat in the same folder as the .exe.
    echo.
    pause
    exit /b 1
)

for %%A in ("%EXE%") do set "EXE=%%~fA"
for %%A in ("%EXE%") do set "EXEDIR=%%~dpA"

if not exist "%ME%\Organize Randomized Files.bat" (
    echo.
    echo    "Organize Randomized Files.bat" is missing from this folder.
    echo.
    pause
    exit /b 1
)

if not exist "%OUTDIR%\" mkdir "%OUTDIR%" >nul 2>&1

echo.
echo  ==================================================================
echo    FE3H Randomizer
echo  ==================================================================
echo.
echo    Randomizer : %EXE%
echo    Save into  : %OUTDIR%
echo.
echo    The randomizer is starting now. Save every randomized file into
echo    the folder that opens. When you close the randomizer, the files
echo    are sorted into the patch folders automatically.
echo.
pause

start "" /wait /d "%OUTDIR%" "%EXE%"

echo.
echo    Randomizer closed - sorting the files it wrote...
echo.

set "NOPAUSE=1"
call "%ME%\Organize Randomized Files.bat" "%OUTDIR%"
if not errorlevel 2 goto :sorter_done
echo.
echo    Nothing was found in the output folder. Checking the folder the
echo    randomizer runs from, in case the files were saved there...
echo.
call "%ME%\Organize Randomized Files.bat" "%EXEDIR%"
:sorter_done
set "NOPAUSE="

echo.
pause
exit /b 0
