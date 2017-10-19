@echo off

:loop
	timeout /T 10 /NOBREAK > NUL
goto loop

exit /b 14