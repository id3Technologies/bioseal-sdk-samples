param($language = "all")

function BuildCppx64 {
    try
    {
        Write-Output "==============================================================================="
        Write-Output "Build cpp x64"
        Write-Output "==============================================================================="
        # c/c++ build
        Push-Location
        Set-Location cpp
        mkdir -Force build
        Set-Location build
        Remove-Item -Recurse -Force *
        # build x64
        & "C:\Program Files\CMake\bin\cmake" -G "Visual Studio 15 2017 Win64" -DWINDOWS_BUILD=ON ..
        & "C:\Program Files\CMake\bin\cmake" --build . --config Release
        if (-not $?)
        {
            throw
        }
        # run sample
        Set-Location .\Release\
        .\id3BiosealSample.exe
        if ($LastExitCode -ne 0)
        {
            throw
        }
    }
    catch
    {
        Write-Output "Error in cpp x64"
        throw
    }
    finally
    {
        Pop-Location
    }
}

function BuildDotnet {
    try
    {
        Write-Output "==============================================================================="
        Write-Output "Build dotnet"
        Write-Output "==============================================================================="
        Push-Location
        Set-Location dotnet
        & "C:\\nuget\\nuget" restore id3.BioSeal.Samples.sln
        & "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Professional\\MSBuild\\15.0\\Bin\\msbuild.exe" id3.Bioseal.Samples.sln /t:Build /p:Configuration=Release
        if (-not $?) 
        {
            throw
        }
        # test x64
        Set-Location id3.BioSeal.Samples\bin\Release
        if (-not $?) 
        {
            throw
        }
        ./id3.BioSeal.Samples.exe
    }
    catch
    {
        Write-Output "Error in dotnet"
        throw
    }
    finally
    {
        Pop-Location
    }
}

function BuildDart {
    try
    {
        Write-Output "==============================================================================="
        Write-Output "Build dart"
        Write-Output "==============================================================================="
        Push-Location
        Set-Location .\dart\id3_bioseal_sample
        dart pub get
        dart analyze --no-fatal-warnings
        dart compile exe .\bin\id3_bioseal_sample.dart -o bin/runme.exe
    }
    catch
    {
        Write-Output "Error in dart"
        throw
    }
    finally
    {
        Pop-Location
    }
}

function BuildJava {
    try
    {
        Write-Output "==============================================================================="
        Write-Output "Build java"
        Write-Output "==============================================================================="
        Push-Location
        # build
        Copy-Item sdk\bin\windows\x64\id3Bioseal.dll java\id3Bioseal.dll
        Set-Location java
        java -cp ".;../sdk/java/eu.id3.bioseal.jar" BiosealSdkSampleCLI.java
        if (-not $?) 
        {
            throw
        }
    }
    catch
    {
        Write-Output "Error in java"
        throw
    }
    finally
    {
        Pop-Location
    }
}

function BuildPython {
    try
    {
        Write-Output "==============================================================================="
        Write-Output "Build python"
        Write-Output "==============================================================================="
        Push-Location
        Set-Location python
        python -m venv sample-env
        sample-env\Scripts\activate
        $pakname = Get-ChildItem -Name ..\sdk\python\id3bioseal-*-cp311-cp311-win_amd64.whl
        python -m pip install ..\sdk\python\$pakname
        python BiosealSdkSampleCLI.py
        deactivate
    }
    catch
    {
        Write-Output "Error in python"
        throw
    }
    finally
    {
        Pop-Location
    }
}

function BuildAndroid {
    try
    {
        Write-Output "==============================================================================="
        Write-Output "Build Android"
        Write-Output "==============================================================================="
        Push-Location
        Set-Location .\android
        .\gradlew.bat --no-daemon assembleRelease
    }
    catch
    {
        Write-Output "Error in Android"
        throw
    }
    finally
    {
        Pop-Location
    }
}

try
{
    switch  ($language)
    {
        "all" {
            # cpp
            BuildCppx64

            # dotnet
            BuildDotnet

            # dart
            BuildDart

            # java
            BuildJava

            # python
            BuildPython

            # android
            BuildAndroid
        }
        "dart" {
            BuildDart
        }
        "dotnet" {
            BuildDotnet
        }
        "java" {
            BuildJava
        }
        "python" {
            BuildPython
        }
        "cpp" {
            BuildCppx64
        }
        "android" {
            BuildAndroid
        }
        default { Write-Output "Unknown language $($language)" }
    }
    Pause
}
catch
{
}