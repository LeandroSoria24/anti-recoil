@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set WPF=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF
set FX=C:\Windows\Microsoft.NET\Framework64\v4.0.30319

"%CSC%" /nologo /target:winexe /optimize+ /platform:x64 /codepage:65001 /utf8output ^
  /out:RecoilStudio.exe ^
  /reference:"%FX%\System.dll" ^
  /reference:"%FX%\System.Core.dll" ^
  /reference:"%FX%\System.Web.Extensions.dll" ^
  /reference:"%FX%\System.Xaml.dll" ^
  /reference:"%WPF%\WindowsBase.dll" ^
  /reference:"%WPF%\PresentationCore.dll" ^
  /reference:"%WPF%\PresentationFramework.dll" ^
  Program.cs src\*.cs

if errorlevel 1 (
    echo [ERROR] Fallo la compilacion de RecoilStudio.exe
    exit /b 1
)

echo [OK] RecoilStudio.exe compilado correctamente.
exit /b 0
