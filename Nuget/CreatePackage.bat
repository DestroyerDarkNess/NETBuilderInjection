@echo off
taskkill /F /IM MSBuild.exe
echo/
set ver=1.0.3.0

if "%ver%" == "" goto error

del NETBuilderInjection.1.0.3.nupkg

:pack
Nuget pack NETBuilderInjection.nuspec -Version %ver%
goto done

:error
echo Parameter version is required. Eg.: CreatePackage 1.0.3.0
goto done

:done
echo Done!
pause