try
{
    Write-Output "==============================================================================="
    Write-Output "Build cpp x64"
    Write-Output "==============================================================================="
    # c/c++ build
    mkdir -Force build
    Set-Location build
    Remove-Item -Recurse -Force *
    # build x64 cpu version lib & tests
    & "C:\Program Files\CMake\bin\cmake" -G "Visual Studio 15 2017 Win64" -DWINDOWS_BUILD=ON ..
    & "C:\Program Files\CMake\bin\cmake" --build . --config Release
    if (-not $?)
    {
        throw
    }
    # test
    Push-Location
    Set-Location Release
    .\id3BiosealSample.exe
    if ($LastExitCode -ne 0)
    {
        throw
    }
    Pop-Location
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
