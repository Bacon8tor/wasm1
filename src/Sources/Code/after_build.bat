@echo off

SET FS_DRIVE=D:
SET FS_PATH=%FS_DRIVE%\FS2020\Packages\Community
SET ADDON_PATH=%FS_PATH%\mobiflight-event-module-swm
SET MODULE_PATH=%ADDON_PATH%\modules
SET BUILD2_PATH=%ADDON_PATH%\build2.py
SET MANIFEST_PATH=%ADDON_PATH%\manifest.json
SET WASM_PATH=MSFS\Debug\StandaloneModule.wasm

IF EXIST "%MODULE_PATH%" GOTO begin

mkdir %ADDON_PATH%
mkdir %MODULE_PATH%

:begin
copy /Y %WASM_PATH% %MODULE_PATH%
copy /Y events.txt %MODULE_PATH%
copy /Y wasm_vars.txt %MODULE_PATH%
copy /Y build2.py %ADDON_PATH%

echo { >%MANIFEST_PATH%
echo   "dependencies": [], >>%MANIFEST_PATH%
echo   "content_type": "MISC", >>%MANIFEST_PATH%
echo   "title": "Event Module", >>%MANIFEST_PATH%
echo   "manufacturer": "", >>%MANIFEST_PATH%
echo   "creator": "MobiFlight", >>%MANIFEST_PATH%
echo   "package_version": "0.2.50", >>%MANIFEST_PATH%
echo   "minimum_game_version": "1.12.13", >>%MANIFEST_PATH%
echo   "release_notes": { >>%MANIFEST_PATH%
echo     "neutral": { >>%MANIFEST_PATH%
echo       "LastUpdate": "", >>%MANIFEST_PATH%
echo       "OlderHistory": "" >>%MANIFEST_PATH%
echo     } >>%MANIFEST_PATH%
echo   } >>%MANIFEST_PATH%
echo } >>%MANIFEST_PATH%

%FS_DRIVE%
cd %ADDON_PATH%
python %BUILD2_PATH%

del /F /Q %BUILD2_PATH%
