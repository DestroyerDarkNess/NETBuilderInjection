@echo off
echo/
set ver=1.0.0.0

if "%ver%" == "" goto error

:pack
Nuget pack NETBuilderInjection.nuspec -Version %ver%
goto done

:error
echo Parameter version is required. Eg.: CreatePackage 1.0.0.0
goto done

:done
echo Done!
pause