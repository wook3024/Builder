@echo off
echo Building Protocol Generator...
echo ==============================

dotnet restore
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet build --configuration Release
if %errorlevel% neq 0 exit /b %errorlevel%

echo.
echo Build completed successfully!
echo.
echo To run the generator:
echo   cd src\ProtocolGenerator.CLI\bin\Release\net10.0
echo   ProtocolGenerator.CLI.exe [xml-file] [output-dir]
echo.
echo Example:
echo   ProtocolGenerator.CLI.exe ..\..\..\..\samples\sample_protocol.xml ..\..\..\..\output
