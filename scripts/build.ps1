$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
$baseDir = $scriptPath + "\.."

properties { 
  $buildNuGet = $true
  $treatWarningsAsErrors = $false
  
  $readmeFile = "$baseDir\README.md"
  $licenseFile = "$baseDir\LICENSE"
  $buildDir = "$baseDir\build"
  $sourceDir = "$baseDir\src"
  $toolsDir = "$baseDir\tools"
  
  $builds = @{   
    "WebApi.HALight" = @{ 
      UnitTestSuffix = ".UnitTests"
      BuildConfigurations = 
        @(
          @{
            # No typo... -> 4.5.1 sets the right tools.
            Framework = '4.5.1';
            TargetFrameworkVersion = "v4.5";
            NuGetDir = "net45";       
          },
          @{
            Framework = '4.5.1';
            TargetFrameworkVersion = "v4.5.1";
            NuGetDir = "net451";       
          },
          @{
            # No typo... -> 4.5.1 sets the right tools.
            Framework = '4.5.1';
            TargetFrameworkVersion = "v4.5.2";
            NuGetDir = "net452";       
          }
        )
    }
  }
}

task default -depends Package

task Clean {
  if ( Test-Path $buildDir) {
    Remove-Item -Recurse -Force $buildDir
  }
}

task VeryClean -depends Clean {
  if (-Not (GetUserConfirmation("All unversioned files will be deleted. All modfified files will be reverted. Sure to continue?"))) {
    throw "Aborting build..."
  }

  pushd
  cd $baseDir
  & git.exe clean -fdx
  & git.exe reset --hard
  popd
}

task Build -depends Clean {   
  $nuget = Resolve-Path "$toolsDir\nuget\NuGet.exe"

  foreach ($name in $builds.Keys)
  {
    $sln = $sourceDir + '\' + $name + '.sln'

    Write-Host -ForegroundColor Green "Restoring NuGet Packages"
    & $nuget restore $sln
  
    Write-Host -ForegroundColor Green "Updating assembly version to $version"
    $version = Get-Version
    Update-AssemblyInfoFiles $sourceDir $version
    $buildConfigurations = $builds.Item($name).BuildConfigurations

    foreach ($buildConfig in $buildConfigurations) 
    {
      $targetVersion = $buildConfig.TargetFrameworkVersion
      $nugetdir = $buildConfig.NuGetDir

      # Set PSAKE to the right Framework Config      
      Framework $buildConfig.Framework

      Write-Host
      Write-Host -ForegroundColor Green "Building $name for Framework Version $targetVersion"       
      exec { msbuild "/fileLoggerParameters:LogFile=MsBuild.log;Append; Verbosity=detailed;Encoding=UTF-8" "/consoleloggerparameters:ShowTimestamp;Summary" "/v:m" "/t:Clean;Rebuild" "/p:Configuration=Release" "/p:Platform=Any CPU" "/p:TargetFrameworkVersion=$targetVersion" "/p:OutputPath=$buildDir\$name\lib\$nugetdir" $sln | Out-Default } "Error building $name"
    }        
  }
}

task Test -depends Build {
  $nunitConsole = Resolve-Path "$sourceDir\packages\NUnit.Runners.2.6.4\tools\nunit-console.exe"
  foreach ($name in $builds.Keys)
  {
    $buildConfigurations = $builds.Item($name).BuildConfigurations
    $unitTestSuffix = $builds.Item($name).UnitTestSuffix
    foreach ($buildConfig in $buildConfigurations) 
    {
      $targetVersion = $buildConfig.TargetFrameworkVersion
      $framework = "net-" + $buildConfig.Framework
      $targetAssembly = $buildDir + "\" + $name + "\lib\" + $buildConfig.NuGetDir + "\" + $name + $unitTestSuffix + ".dll"
      $targetAssembly = Resolve-Path $targetAssembly

      Write-Host
      Write-Host -ForegroundColor Green "Testing $name for Framework Version $targetVersion"       

      & $nunitConsole $targetAssembly /nologo /noshadow /framework:$framework
    }
  }
}

task Package -depends Test {
  $nuget = Resolve-Path "$toolsDir\nuget\NuGet.exe"
  foreach ($name in $builds.Keys)
  {
    $nugetRootDirStr = $buildDir + "\" + $name + "\"
    $nugetRootDir = Resolve-Path $nugetRootDirStr
    $version = Get-Version + ".0"
    
    Copy-Item -Force "$sourceDir\NuGet-Package\$name.nuspec" "$nugetRootDir\"
    md -Force "$nugetRootDir\tools" | Out-Null
    Copy-Item -Force "$sourceDir\NuGet-Package\$name.install.ps1" "$nugetRootDir\tools\install.ps1"

    # Clean up all dependent assemblies, we just want to package the original assembly.    
    $buildConfigurations = $builds.Item($name).BuildConfigurations
    foreach ($buildConfig in $buildConfigurations) 
    {
      $nugetLibPath = Resolve-Path ($nugetRootDirStr + "\lib\" + $buildConfig.NuGetDir)
      Write-Host $nugetLibPath
      Get-ChildItem -Path  $nugetLibPath -Recurse -exclude "$name.dll" | Remove-Item -force 
    }

    Write-Host
    Write-Host -ForegroundColor Green "Packaging $name for all configured Framework Versions"
    $spec = "$nugetRootDir\$name.nuspec"    
    & $nuget pack -OutPutDirectory $nugetRootDir -Version $version $spec  
  }
}

function Get-Version()
{    
    return (Get-Content -Raw "$sourceDir\VERSION").Trim()
}

function Update-AssemblyInfoFiles ([string] $sourceDir, [string] $version)
{
    $assemblyVersionPattern = 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
    $fileVersionPattern = 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
    $assemblyVersion = 'AssemblyVersion("' + $version + '.0")';
    $fileVersion = 'AssemblyFileVersion("' + $version + '.0")';
    
    Get-ChildItem -Path $sourceDir -r -filter AssemblyInfo.cs | ForEach-Object {
        $filename = $_.Directory.ToString() + '\' + $_.Name
        Write-Host $filename
        $filename + ' -> ' + $version
    
        (Get-Content -encoding UTF8 $filename) | ForEach-Object {
            % {$_ -replace $assemblyVersionPattern, $assemblyVersion } |
            % {$_ -replace $fileVersionPattern, $fileVersion }
        } | Set-Content -encoding UTF8 $filename
    }
}

function GetUserConfirmation($msg) {
  $caption = "Confirm"
  $message = $msg
  $yes = new-Object System.Management.Automation.Host.ChoiceDescription "&Yes","help"
  $no = new-Object System.Management.Automation.Host.ChoiceDescription "&No","help"
  $choices = [System.Management.Automation.Host.ChoiceDescription[]]($yes,$no)
  $answer = $host.ui.PromptForChoice($caption,$message,$choices,0)  

  switch ($answer){
    0 {return $true}
    1 {return $false}
  }  
}