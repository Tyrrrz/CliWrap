@echo off

:loop

echo Hello world
echo Hello world 1>&2

goto loop

exit /b 14