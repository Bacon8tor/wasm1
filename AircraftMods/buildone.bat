@echo off

SET FS_PATH=D:\FS2020\Packages\Community
SET BASE_PATH=Mods
SET MOD_PATH=%BASE_PATH%\%1
SET BUILD2_PATH=%MOD_PATH%\build2.py
SET MANIFEST_PATH=%MOD_PATH%\manifest.json

IF EXIST "%MOD_PATH%" GOTO begin
echo Mod path not found - MOD_PATH
goto finis


:begin
copy /Y build2.py %MOD_PATH%

echo { >%MANIFEST_PATH%
echo   "dependencies": [], >>%MANIFEST_PATH%
echo   "content_type": "AIRCRAFT", >>%MANIFEST_PATH%
echo   "title": "%2", >>%MANIFEST_PATH%
echo   "manufacturer": "Various", >>%MANIFEST_PATH%
echo   "creator": "MobiFlight", >>%MANIFEST_PATH%
echo   "package_version": "%3", >>%MANIFEST_PATH%
echo   "minimum_game_version": "1.12.13", >>%MANIFEST_PATH%
echo   "release_notes": { >>%MANIFEST_PATH%
echo     "neutral": { >>%MANIFEST_PATH%
echo       "LastUpdate": "", >>%MANIFEST_PATH%
echo       "OlderHistory": "" >>%MANIFEST_PATH%
echo     } >>%MANIFEST_PATH%
echo   } >>%MANIFEST_PATH%
echo } >>%MANIFEST_PATH%

pushd %MOD_PATH%
python build2.py
popd
del /F /Q %BUILD2_PATH%


:finis
echo. 
echo. 
