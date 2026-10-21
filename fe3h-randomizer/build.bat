@echo off
rem ===================================================================
rem  Builds "Fire Emblem Three Houses Randomizer V2.exe" from source.
rem
rem    build.bat            build Release into bin\Release
rem    build.bat /install   build, then copy the result over
rem                         ..\Fire Emblem Three Houses Randomizer.exe
rem                         (the old exe is kept as .original.bak)
rem
rem  Needs a C# compiler: Visual Studio 2019/2022 (any edition,
rem  "Desktop development with C#" or the .NET desktop build tools),
rem  or the .NET SDK 6+ with the .NET Framework 4.7.2 targeting pack.
rem ===================================================================

setlocal EnableExtensions EnableDelayedExpansion
title FE3H Randomizer - build

cd /d "%~dp0"

set "PROJECT=Fire Emblem Three Houses Randomizer.csproj"
set "OUTEXE=%CD%\bin\Release\Fire Emblem Three Houses Randomizer V2.exe"
set "INSTALL="
if /i "%~1"=="/install" set "INSTALL=1"

echo.
echo  ==================================================================
echo    Building the FE3H randomizer
echo  ==================================================================
echo.

if not exist "%PROJECT%" (
    echo    "%PROJECT%" is missing. Run this .bat from the folder that
    echo    contains it.
    echo.
    pause
    exit /b 1
)

rem ------------------------------------------------- locate MSBuild
set "MSBUILD="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" set "VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
    for /f "usebackq tokens=*" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do (
        if not defined MSBUILD set "MSBUILD=%%I"
    )
)

if not defined MSBUILD call :try_msbuild "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :try_msbuild "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :try_msbuild "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :try_msbuild "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :try_msbuild "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :try_msbuild "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :try_msbuild "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :try_msbuild "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :try_msbuild "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"

if not defined MSBUILD goto :try_dotnet

echo    MSBuild   : !MSBUILD!
echo    Project   : %PROJECT%
echo.
echo    Building (Release)...
echo.
"!MSBUILD!" "%PROJECT%" /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /v:minimal /nologo
if errorlevel 1 goto :build_failed
goto :build_done

rem ------------------------------------------------- dotnet fallback
:try_dotnet
where dotnet >nul 2>&1
if errorlevel 1 goto :no_toolchain

echo    MSBuild was not found, trying "dotnet build".
echo.
dotnet build "%PROJECT%" -c Release -v minimal
if errorlevel 1 goto :build_failed
goto :build_done

rem ------------------------------------------------- outcomes
:no_toolchain
echo    No C# compiler was found.
echo.
echo    Install one of these, then run build.bat again:
echo.
echo      * Visual Studio 2022 Community  -  tick
echo        "Desktop development with C#" (or ".NET desktop build tools")
echo        https://visualstudio.microsoft.com/vs/community/
echo.
echo      * or the .NET SDK 8 plus the ".NET Framework 4.7.2 targeting pack"
echo        https://dotnet.microsoft.com/download
echo.
pause
exit /b 1

:build_failed
echo.
echo  ------------------------------------------------------------------
echo    The build failed. The error lines above say what went wrong.
echo.
echo    Most common causes:
echo      * reference assemblies for .NET Framework 4.7.2 are missing
echo        (Visual Studio: modify installation, tick .NET Framework 4.7.2)
echo      * an old MSBuild (pre-2017) was picked up - install VS 2019/2022
echo  ------------------------------------------------------------------
echo.
pause
exit /b 1

:build_done
echo.
if not exist "%OUTEXE%" (
    echo    The build reported success but the exe was not found at:
    echo      %OUTEXE%
    echo    Check bin\Release for the output name.
    echo.
    pause
    exit /b 1
)

echo  ==================================================================
echo    Built: %OUTEXE%
echo  ==================================================================
echo.

if not defined INSTALL goto :done

set "TARGET=..\Fire Emblem Three Houses Randomizer.exe"
if exist "%TARGET%" (
    if not exist "%TARGET%.original.bak" (
        echo    Keeping a backup of your current exe as:
        echo      Fire Emblem Three Houses Randomizer.exe.original.bak
        copy /y "%TARGET%" "%TARGET%.original.bak" >nul
    )
)
copy /y "%OUTEXE%" "%TARGET%" >nul
if errorlevel 1 (
    echo    Could not copy the new exe over "%TARGET%".
    echo    Copy it by hand from the path above.
    echo.
    pause
    exit /b 1
)
echo    Installed: %TARGET%
echo    (your previous exe is the .original.bak next to it)
echo.
timeout /t 5 >nul
exit /b 0

:done
echo    Next: copy
echo      "bin\Release\Fire Emblem Three Houses Randomizer V2.exe"
echo    next to your game dump / over your old randomizer exe.
echo.
echo    Or run  "build.bat /install"  to do that copy for you.
echo.
pause
exit /b 0

rem -------------------------------------------------
:try_msbuild
if defined MSBUILD exit /b 0
if exist "%~1" set "MSBUILD=%~1"
exit /b 0
