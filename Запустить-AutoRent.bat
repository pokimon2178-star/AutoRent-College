@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ========================================
echo          AutoRent - запуск сайта
echo ========================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ОШИБКА: .NET не установлен.
    echo Установите .NET 8 SDK с сайта:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Проверка и восстановление пакетов...
dotnet restore
if errorlevel 1 (
    echo Ошибка восстановления пакетов.
    pause
    exit /b 1
)

echo.
echo Запуск AutoRent...
echo После запуска откройте http://localhost:5016
echo Для остановки нажмите Ctrl+C.
echo.

dotnet run --urls http://localhost:5016

pause
