@echo off

SHIFT

:loop
  echo %*
goto loop

exit /b 14