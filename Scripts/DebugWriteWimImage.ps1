# Project > Properties > Debug
# Start External Program: C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe
# Command Line Arguments: -NoExit -Command "&{ . .\DebugWriteWimImage  }"
$VerbosePreference = 'Continue'
$DebugPreference = 'Continue'

Import-Module .\Microsoft.Wim.Powershell.dll -Force -Verbose 
Write-WimImage -WimPath G:\sources\install.wim -TargetPath E:\Test -Verbose -Debug