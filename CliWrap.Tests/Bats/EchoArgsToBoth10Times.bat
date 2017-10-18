@echo off

SHIFT

FOR /L %%A IN (1,1,10) DO (
  echo %*
)

exit /b 14