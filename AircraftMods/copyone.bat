@echo off

SET FS_PATH=D:\FS2020\Packages\Community
SET BASE_PATH=Mods
SET MOD_PATH=%BASE_PATH%\%1

IF EXIST "%MOD_PATH%" GOTO begin
echo Mod path not found - MOD_PATH
goto finis


:begin
xcopy /S /I /Y "%MOD_PATH%" "%FS_PATH%\%1"

:finis
echo. 
echo. 
