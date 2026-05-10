@echo off
setlocal

pushd "%~dp0"

echo Publishing to Thunderstore...
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0PublishThunderstore.ps1"
set THUNDERSTORE_EXITCODE=%ERRORLEVEL%

if not "%THUNDERSTORE_EXITCODE%"=="0" (
    echo.
    echo Thunderstore publish failed. Exit code: %THUNDERSTORE_EXITCODE%
    choice /C YN /M "Continue to Nexus anyway"
    if errorlevel 2 (
        echo.
        echo Publishing stopped after Thunderstore failure.
        pause
        exit /b %THUNDERSTORE_EXITCODE%
    )
)

echo.
echo Publishing to Nexus...
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0PublishNexus.ps1"
set NEXUS_EXITCODE=%ERRORLEVEL%

if not "%NEXUS_EXITCODE%"=="0" (
    echo.
    echo Nexus publish failed. Exit code: %NEXUS_EXITCODE%
    pause
    exit /b %NEXUS_EXITCODE%
)

echo.
echo Publish finished.
pause
exit /b 0
