@ECHO OFF
ECHO.Usage: DevInstall.cmd [/u] ^<output directory^>
ECHO.This script requires Administrative privileges to run properly.
ECHO.Start ^> All Programs ^> Accessories ^> Right-Click Command Prompt ^> Select 'Run As Administrator'

set GACUtilPath=%ProgramFilesPath%
if "%GACUtilPath%"=="" set GACUtilPath=%ProgramFiles%

set GACUtil=%GACUtilPath%\Microsoft SDKs\Windows\v7.0A\bin\gacutil.exe

ECHO.Determine whether we are on an 32 or 64 bit machine
if "%PROCESSOR_ARCHITECTURE%"=="x86" if "%PROCESSOR_ARCHITEW6432%"=="" goto x86
set ProgramFilesPath=%ProgramFiles(x86)%

ECHO.Using %GACUtil%

goto unregister

:x86

    ECHO.On an x86 machine
    set ProgramFilesPath=%ProgramFiles%

:unregister
    ECHO.Remove the DLL from the Global Assembly cache
    "%GACUtil%" /u EasyHook
    "%GACUtil%" /u PeachHooker.Network
    "%GACUtil%" /u PeachHooker.File

    REM Exit out if the /u uninstall argument is provided, leaving no trace of program files.
    if "%1"=="/u" goto exit

:releasetype

    if "%1"=="" goto error
    set ReleaseType=%1
    goto checkbin

:checkbin

    if not exist "%ReleaseType%EasyHook.dll" goto missing
    if not exist "%ReleaseType%PeachHooker.Network.dll" goto missing
    if not exist "%ReleaseType%PeachHooker.File.dll" goto missing
    goto register

:missing

    ECHO.Cannot find %ReleaseType% binaries.
    ECHO.Build solution as %ReleaseType% and run script again. 
    goto exit

:register

    ECHO.Register the DLL with the global assembly cache
    ECHO.
    ECHO.Registering "%ReleaseType%EasyHook.dll"
    ECHO.
    "%GACUtil%" /if "%ReleaseType%EasyHook.dll"

    ECHO.
    ECHO.Registering "%ReleaseType%EasyHook.dll"
    ECHO.
    "%GACUtil%" /if "%ReleaseType%PeachHooker.Network.dll"

    ECHO.
    ECHO.Registering "%ReleaseType%PeachHooker.File.dll"
    ECHO.
    "%GACUtil%" /if "%ReleaseType%PeachHooker.File.dll"

    goto exit

:error

    ECHO.
    ECHO.Missing build output folder path

:exit