param([string]$target)

Try 
{   
    Import-Module .\Tools\PSake\psake.psm1

    Invoke-Psake .\scripts\build.ps1 $target    
    Remove-Module psake
}
Catch 
{
    Remove-Module psake
    Write-Error $_    
}