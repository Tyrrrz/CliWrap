@echo off

FOR /L %%A IN (1,1,1000) DO (
	echo stdout %%A
	echo stderr %%A 1>&2
)