@echo off

set "SOURCE_DIR=scripts\git-hooks\windows"
set "DEST_DIR=.git\hooks"

for %%f in (%SOURCE_DIR%\*) do (
    echo Installing %%~nxf
    mklink /H "%DEST_DIR%\%%~nxf" "%%~ff"
)

if %errorlevel% equ 0 (
    echo Hooks installed successfully.
) else (
    echo Error installing hooks.
    exit /b 1
)
