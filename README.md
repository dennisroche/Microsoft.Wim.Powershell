Microsoft.Wim.Powershell
========================

PowerShell CmdLets for the native Windows Imaging API (WIMGAPI).

Uses [Managed WimgApi](https://managedwimgapi.codeplex.com/) - a thin managed wrapper for the native Windows Imaging API (WIMGAPI).

#CmdLets


###WriteWimImage

Writes a Windows Image (`*.wim`) to a location.

```ps
Import-Module .\Microsoft.Wim.Powershell.dll-Verbose 
Write-WimImage -WimPath G:\sources\install.wim -TargetPath E:\
```

#Installing

Available via [NuGet](https://www.nuget.org/packages/Microsoft.Wim.Powershell/).

```ps
Install-Package Microsoft.Wim.Powershell
```