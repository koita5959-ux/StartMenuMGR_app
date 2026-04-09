@echo off
chcp 65001 >nul
setlocal

rem ============================================================
rem  StartMenuMGR ビルド～インストーラー～ZIP 一括作成
rem  プロジェクトルート（StartMenuMGR_app）で実行すること
rem ============================================================

set APP_NAME=StartMenuMGR
set APP_VERSION=01.02
set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo.
echo === %APP_NAME% v%APP_VERSION% ビルド開始 ===
echo.

rem --- 1. dotnet publish ---
echo [1/3] dotnet publish ...
dotnet publish %APP_NAME% -c Release -r win-x64 --self-contained false -o %APP_NAME%\bin\Publish
if errorlevel 1 (
    echo [エラー] dotnet publish に失敗しました。
    exit /b 1
)
echo       OK
echo.

rem --- 2. Inno Setup ---
echo [2/3] Inno Setup コンパイル ...
%ISCC% setup.iss
if errorlevel 1 (
    echo [エラー] Inno Setup コンパイルに失敗しました。
    exit /b 1
)
echo       OK
echo.

rem --- 3. 配布ZIP作成 ---
echo [3/3] 配布ZIP作成 ...
powershell -Command "Compress-Archive -Path '%APP_NAME%_setup_v%APP_VERSION%.exe', 'ご利用にあたって.txt' -DestinationPath '%APP_NAME%_v%APP_VERSION%.zip' -Force"
if errorlevel 1 (
    echo [エラー] ZIP作成に失敗しました。
    exit /b 1
)
echo       OK
echo.

echo === 完了 ===
echo  インストーラー: %APP_NAME%_setup_v%APP_VERSION%.exe
echo  配布ZIP:        %APP_NAME%_v%APP_VERSION%.zip
echo.

endlocal
