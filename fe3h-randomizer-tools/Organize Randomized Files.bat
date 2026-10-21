@echo off
rem ===================================================================
rem  FE3H Randomizer - automatic output organizer
rem
rem  Takes the files written by "Fire Emblem Three Houses Randomizer"
rem  and places them in the romfs patch layout the game expects,
rem  inside one single output folder.
rem
rem  Usage:
rem    "Organize Randomized Files.bat"
rem        Sorts the files found beside this .bat into .\output.
rem        If there are none, the folder above is checked as well.
rem
rem    "Organize Randomized Files.bat" "D:\where\you\saved"
rem        Sorts the files from that folder into .\output
rem
rem    "Organize Randomized Files.bat" "D:\saved\files" "D:\mod\romfs"
rem        Sorts into a specific destination folder instead
rem
rem    /c  copy instead of move, originals are kept
rem    /n  preview only, nothing is changed
rem
rem  You can also drag and drop the randomized files onto this .bat.
rem
rem  Folder map (identical to the old hand-made .bat, but every file is
rem  handled in one single pass instead of one file per run):
rem    BGM.ktsl2stbin          goes to  nx\sound
rem    gwscrdata.bin           goes to  patch1\common\common
rem    fixed_calendardata.bin  goes to  patch3\nx\data
rem    btlscrdata.bin          goes to  patch4\common\common
rem    msgdata.bin             goes to  patch4\common\common
rem    scrdata.bin             goes to  patch4\common\common
rem    tuscrdata.bin           goes to  patch4\common\common
rem    INFO0.bin               goes to  patch4
rem    INFO2.bin               goes to  patch4
rem    fixed_*.bin  (all)      goes to  patch4\nx\data
rem ===================================================================

setlocal EnableExtensions
title FE3H Randomizer - Output Organizer

set "BASE=%~dp0"
if "%BASE:~-1%"=="\" set "BASE=%BASE:~0,-1%"

set "SOURCE=%BASE%"
set "TARGET=%BASE%\output"
set "COPYMODE="
set "DRYRUN="
set "HASFILES="
set "DIRS=0"
set "MOVED=0"
set "REPLACED=0"
set "FAILED=0"
set "SEEN=0"
set "LIST=%TEMP%\fe3h_sort_%RANDOM%%RANDOM%.txt"
del /q "%LIST%" >nul 2>&1

:parse
if "%~1"=="" goto parsed
if /i "%~1"=="/?" goto usage
if /i "%~1"=="/h" goto usage
if /i "%~1"=="/help" goto usage
if /i "%~1"=="/c" (
    set "COPYMODE=1"
    shift
    goto parse
)
if /i "%~1"=="/n" (
    set "DRYRUN=1"
    shift
    goto parse
)
if not exist "%~1\" goto parse_file
if "%DIRS%"=="0" (
    set "SOURCE=%~f1"
    set "DIRS=1"
    shift
    goto parse
)
if "%DIRS%"=="1" (
    set "TARGET=%~f1"
    set "DIRS=2"
    shift
    goto parse
)
echo    WARNING: ignored extra folder argument: %~1
shift
goto parse

:parse_file
if not exist "%~1" (
    echo    WARNING: not found, ignored: %~1
    shift
    goto parse
)
echo "%~f1" >> "%LIST%"
set "HASFILES=1"
shift
goto parse

:parsed
if "%SOURCE:~-1%"=="\" set "SOURCE=%SOURCE:~0,-1%"
if "%TARGET:~-1%"=="\" set "TARGET=%TARGET:~0,-1%"

echo.
echo  ==================================================================
echo    FE3H Randomizer - output organizer
echo  ==================================================================
echo.
if defined DRYRUN echo    /n preview mode - no file will be changed.
if defined COPYMODE echo    /c copy mode - your original files are kept.
if defined HASFILES echo    Sorting  : the files you dropped on this .bat
if not defined HASFILES echo    Scanning : %SOURCE%
echo    Output   : %TARGET%
if /i "%SOURCE%"=="%TARGET%" echo    The files are sorted in place, inside the output folder.
echo.

if exist "%TARGET%" if not exist "%TARGET%\" (
    echo    ERROR: "%TARGET%" is a file, not a folder.
    echo.
    if not defined NOPAUSE pause
    exit /b 1
)

if defined DRYRUN goto :make_done
if not exist "%TARGET%\" mkdir "%TARGET%" >nul 2>&1
if not exist "%TARGET%\nx\sound\" mkdir "%TARGET%\nx\sound" >nul 2>&1
if not exist "%TARGET%\patch1\common\common\" mkdir "%TARGET%\patch1\common\common" >nul 2>&1
if not exist "%TARGET%\patch3\nx\data\" mkdir "%TARGET%\patch3\nx\data" >nul 2>&1
if not exist "%TARGET%\patch4\common\common\" mkdir "%TARGET%\patch4\common\common" >nul 2>&1
if not exist "%TARGET%\patch4\nx\data\" mkdir "%TARGET%\patch4\nx\data" >nul 2>&1
:make_done

if defined HASFILES goto :do_files
call :scan
if not "%SEEN%"=="0" goto :scanned
if /i not "%SOURCE%"=="%BASE%" goto :scanned
echo.
echo    Nothing found in the folder this .bat sits in, checking the folder above...
set "SOURCE=%BASE%\.."
call :scan
goto :scanned

:do_files
for /f "usebackq delims=" %%F in ("%LIST%") do call :handle "%%~F"

:scanned
echo.
if "%SEEN%"=="0" (
    echo    No randomized files were found.
    echo.
    echo    Save the randomizer output beside this .bat, drag the files onto
    echo    it, or pass the folder they are in as an argument.
) else (
    echo    ---------------------------------------------------------------
    echo     Files seen    : %SEEN%
    echo     Moved         : %MOVED%
    echo     Replaced      : %REPLACED%
    echo     Failed        : %FAILED%
    echo    ---------------------------------------------------------------
    if not "%FAILED%"=="0" echo.
    if not "%FAILED%"=="0" echo    WARNING: %FAILED% file(s) could not be written. Is one of them
    if not "%FAILED%"=="0" echo             still open in another program?
)

if defined DRYRUN goto :no_tree
if "%MOVED%"=="0" goto :no_tree
echo.
echo    Resulting layout:
echo.
tree "%TARGET%" /f
:no_tree

echo.
if not "%SEEN%"=="0" echo    Next step: copy the nx, patch1, patch3 and patch4 folders from
if not "%SEEN%"=="0" echo    the output folder into the romfs folder of your Three Houses mod.
if not "%SEEN%"=="0" echo.
del /q "%LIST%" >nul 2>&1
if not defined NOPAUSE pause
if "%SEEN%"=="0" exit /b 2
exit /b 0

rem ===================================================================
rem  :scan  -  hand every file of the source folder to :handle
rem ===================================================================
:scan
for /f "delims=" %%F in ('dir /b "%SOURCE%\*.bin" 2^>nul') do call :handle "%SOURCE%\%%F"
for /f "delims=" %%F in ('dir /b "%SOURCE%\*.ktsl2stbin" 2^>nul') do call :handle "%SOURCE%\%%F"
for /f "delims=" %%F in ('dir /b "%SOURCE%\*.ktsl2asbin" 2^>nul') do call :handle "%SOURCE%\%%F"
exit /b

rem ===================================================================
rem  :handle  -  work out the destination of one file
rem ===================================================================
:handle
set "SRC=%~f1"
set "NAME=%~nx1"
if not exist "%SRC%" exit /b 0
dir /b "%SRC%" >nul 2>&1
if errorlevel 1 exit /b 0
set /a SEEN+=1

if /i "%NAME%"=="DATA1.bin" (
    echo    --   %NAME% is patched in place by the randomizer, nothing to move.
    exit /b 0
)

if /i "%NAME%"=="BGM.ktsl2stbin" goto :d_sound
if /i "%NAME%"=="gwscrdata.bin" goto :d_patch1
if /i "%NAME%"=="fixed_calendardata.bin" goto :d_patch3
if /i "%NAME%"=="btlscrdata.bin" goto :d_common
if /i "%NAME%"=="msgdata.bin" goto :d_common
if /i "%NAME%"=="scrdata.bin" goto :d_common
if /i "%NAME%"=="tuscrdata.bin" goto :d_common
if /i "%NAME%"=="INFO0.bin" goto :d_patch4
if /i "%NAME%"=="INFO2.bin" goto :d_patch4
if /i "%NAME%"=="fixed_persondata.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_classdata.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_data.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_lobby_common.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_act_data.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_lobby_activity.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_groupworkdata.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_presentdata.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_shopdata.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_lobby_quest.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_scenario.bin" goto :d_nxdata
if /i "%NAME%"=="fixed_growthdata.bin" goto :d_nxdata

rem -- any other fixed_*.bin belongs in patch4\nx\data
set "HEAD=%NAME:~0,6%"
set "TAIL=%NAME:~-4%"
if /i "%HEAD%"=="fixed_" if /i "%TAIL%"==".bin" goto :d_nxdata

rem -- any other sound archive belongs in nx\sound
set "TAIL=%NAME:~-12%"
if /i "%TAIL%"==".ktsl2stbin" goto :d_sound
set "TAIL=%NAME:~-11%"
if /i "%TAIL%"==".ktsl2asbin" goto :d_sound

echo    --   %NAME% is not a randomized output file, left alone.
exit /b 0

:d_sound
call :moveto "%SRC%" "nx\sound"
exit /b

:d_patch1
call :moveto "%SRC%" "patch1\common\common"
exit /b

:d_patch3
call :moveto "%SRC%" "patch3\nx\data"
exit /b

:d_common
call :moveto "%SRC%" "patch4\common\common"
exit /b

:d_patch4
call :moveto "%SRC%" "patch4"
exit /b

:d_nxdata
call :moveto "%SRC%" "patch4\nx\data"
exit /b

rem ===================================================================
rem  :moveto  -  move (or copy) one file into a folder of the output
rem ===================================================================
:moveto
set "REL=%~2"
set "DESTDIR=%TARGET%\%~2"
if /i "%~dp1"=="%DESTDIR%\" (
    echo    ok   %~nx1 is already in %REL%
    exit /b 0
)
if defined DRYRUN (
    if exist "%DESTDIR%\%~nx1" (
        echo    dry  would replace  %~nx1  in  %REL%
    ) else (
        echo    dry  would move     %~nx1  in  %REL%
    )
    exit /b 0
)
if not exist "%DESTDIR%\" mkdir "%DESTDIR%" >nul 2>&1
set "EXISTED="
if exist "%DESTDIR%\%~nx1" set "EXISTED=1"
if defined COPYMODE (
    copy /y "%~1" "%DESTDIR%\%~nx1" >nul 2>&1
) else (
    move /y "%~1" "%DESTDIR%\%~nx1" >nul 2>&1
)
if errorlevel 1 (
    set /a FAILED+=1
    echo    FAIL %~nx1  could not be written to  %REL%
    exit /b 0
)
set /a MOVED+=1
if defined EXISTED (
    set /a REPLACED+=1
    echo    ok   %~nx1  to  %REL%  [replaced older copy]
) else (
    echo    ok   %~nx1  to  %REL%
)
exit /b 0

rem ===================================================================
:usage
echo.
echo  ==================================================================
echo    FE3H Randomizer - output organizer
echo  ==================================================================
echo.
echo    "Organize Randomized Files.bat"
echo        Sorts the files beside this .bat into .\output
echo.
echo    "Organize Randomized Files.bat" "D:\where\you\saved"
echo        Sorts the files from that folder into .\output
echo.
echo    "Organize Randomized Files.bat" "D:\saved\files" "D:\mod\romfs"
echo        Sorts into the second folder instead
echo.
echo    /c   copy instead of move
echo    /n   preview only, nothing is changed
echo.
echo    You can also drag and drop the randomized files onto this .bat.
echo.
if not defined NOPAUSE pause
exit /b 0
