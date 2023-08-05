@ECHO OFF

SET BASEDIR=%~dp0
SET DUMMYNAME=CliWrap.Tests.Dummy

IF EXIST "%BASEDIR%/%DUMMYNAME%.exe" (
    "%BASEDIR%/%DUMMYNAME%.exe" %*
) ELSE (
    dotnet "%BASEDIR%/%DUMMYNAME%.dll" %*
)