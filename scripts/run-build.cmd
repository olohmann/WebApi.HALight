powershell -Command "& { Start-Transcript runbuild.txt; Import-Module ..\Tools\PSake\psake.psm1; Invoke-psake .\build.ps1 %*; Stop-Transcript; }"
pause