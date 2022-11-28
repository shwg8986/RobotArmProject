@echo off

:loop1
    adb devices
    adb reverse tcp:8000 tcp:8000
    if %errorlevel% neq 0 (
        timeout 3
        goto :loop1
    ) else (
        goto :loopout1
    )
goto :loop1

:loopout1

:loop2
    adb devices
    adb reverse tcp:9001 tcp:9001
    if %errorlevel% neq 0 (
        timeout 3
        goto :loop2
    ) else (
        goto :loopout2
    ) 
goto :loop2

:loopout2

exit /b 0