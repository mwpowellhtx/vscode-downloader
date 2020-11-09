@echo off

rem The target platforms are:
rem     darwin
rem     linux-arm64
rem     linux-armhf
rem     linux-deb-arm64
rem     linux-deb-armhf
rem     linux-deb-x64
rem     linux-rpm-arm64
rem     linux-rpm-armhf
rem     linux-rpm-x64
rem     linux-snap-x64
rem     linux-x64
rem     win32
rem     win32-archive
rem     win32-arm64
rem     win32-arm64-archive
rem     win32-arm64-user
rem     win32-user
rem     win32-x64
rem     win32-x64-archive
rem     win32-x64-user

rem And may be composed as follows, for either stable builds, or (nightly? we presume...) insider builds:
rem https://update.code.visualstudio.com/1.51.0-insider/win32-x64-user/insider
rem https://update.code.visualstudio.com/1.50.0/win32-x64-user/stable
rem https://update.code.visualstudio.com/1.50.0/win32/stable
rem https://update.code.visualstudio.com/1.50.0/win32-archive/stable
rem https://update.code.visualstudio.com/1.50.0/win32-system/stable

rem Assumes we have "wget.exe" available an in the path.
rem See: http://download.wsusoffline.net for download packages including "wget".
set wget_exe=wget.exe
set uri_base=https://update.code.visualstudio.com/
set uri_suffix=/stable

rem Any valid version number, or "latest" when unspecified.
set version_latest=1.50.1
set latest=latest
set version=%latest%

set macos_version_min=10.10

set dir_macos=macOS
set dir_win=Windows
set dir_x64=x64
set dir_x86=x86
set dir_arm=arm
set dir_arm64=arm64

set target_macos=darwin
set target_linux=linux
set target_win=win32

set arch_x64=x64
set arch_x86=x86
set arch_ios=ios
set arch_arm64=arm64
set arch_arm=arm

set build_user=user
set build_system=system
set build_archive=archive

:args_begin

if /i "%1" equ "--no-pause" (
    set no_pause=1
    goto :args_next
)

rem In generally the order in which you would review the downloads page.
rem https://code.visualstudio.com/Download
:args_target
if /i "%1" equ "-t" (
    if /i "%2" equ "%target_macos%" set target=%target_macos%
    if /i "%2" equ "%target_linux%" set target=%target_linux%
    if /i "%2" equ "%target_win%" set target=%target_win%
    shift
    goto :args_next
)
if /i "%1" equ "--target" (
    if /i "%2" equ "%target_macos%" set target=%target_macos%
    if /i "%2" equ "%target_linux%" set target=%target_linux%
    if /i "%2" equ "%target_win%" set target=%target_win%
    shift
    goto :args_next
)

:args_build
if /i "%1" equ "-b" (
    if /i "%2" equ "%build_user%" set build=%build_user%
    if /i "%2" equ "%build_system%" set build=%build_system%
    if /i "%2" equ "%build_archive%" set build=%build_archive%
    shift
    goto :args_next
)
if /i "%1" equ "--build" (
    if /i "%2" equ "%build_user%" set build=%build_user%
    if /i "%2" equ "%build_system%" set build=%build_system%
    if /i "%2" equ "%build_archive%" set build=%build_archive%
    shift
    goto :args_next
)

:args_arch
if /i "%1" equ "-a" (
    if /i "%2" equ "%arch_x64%" set arch=%arch_x64%
    if /i "%2" equ "%arch_x86%" set arch=%arch_x86%
    if /i "%2" equ "%arch_ios%" set arch=%arch_ios%
    if /i "%2" equ "%arch_arm%" set arch=%arch_arm%
    if /i "%2" equ "%arch_arm64%" set arch=%arch_arm64%
    shift
    goto :args_next
)
if /i "%1" equ "--arch" (
    if /i "%2" equ "%arch_x64%" set arch=%arch_x64%
    if /i "%2" equ "%arch_x86%" set arch=%arch_x86%
    if /i "%2" equ "%arch_ios%" set arch=%arch_ios%
    if /i "%2" equ "%arch_arm%" set arch=%arch_arm%
    if /i "%2" equ "%arch_arm64%" set arch=%arch_arm64%
    shift
    goto :args_next
)

:args_all
if /i "%1" equ "--all" (
    set all=1
    rem Everything else, besides perhaps version, is a do not care.
    rem However we should allow the remainder of the args to parse anyway.
    goto :args_next
)

:args_dry
if /i "%1" equ "--dry" (
    set dry=1
    goto :args_next
)

:args_version
if /i "%1" equ "-v" (
    set version=%2
    shift
    goto :args_next
)
if /i "%1" equ "--version" (
    set version=%2
    shift
    goto :args_next
)

:args_next

shift

if "%1" equ "" goto :args_fini

goto :args_begin

:args_fini

:prep_args

rem Assume "Windows x64 System Installer" by default.
if "%target%" equ "" set target=%target_win%
if "%arch%" equ "" set arch=%arch_x64%
if "%build%" equ "" set build=%build_system%

if "%all%" equ "1" (
    rem Get all of the valid combinations.
    call :on_get %target_macos% 0 0 %version%

    call :on_get %target_win% %arch_x64% %build_user% %version%
    call :on_get %target_win% %arch_x64% %build_system% %version%
    call :on_get %target_win% %arch_x64% %build_archive% %version%

    call :on_get %target_win% %arch_x86% %build_user% %version%
    call :on_get %target_win% %arch_x86% %build_system% %version%
    call :on_get %target_win% %arch_x86% %build_archive% %version%

    call :on_get %target_win% %arch_arm64% %build_user% %version%
    call :on_get %target_win% %arch_arm64% %build_system% %version%
    call :on_get %target_win% %arch_arm64% %build_archive% %version%
) else (
    rem Get in one specific combination.
    call :on_get %target% %arch% %build% %version%
)

goto :fini_eval

:on_get

setlocal

set t=%1
set a=%2
set b=%3

set v=%4

if "%dry%" equ "1" echo Dry: t=%t% a=%a% b=%b% v=%v%

if /i "%v%" equ "%latest%" set v=%version_latest%

set selector=
set get_dir=

if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_macos%00" ...
if "%t%%a%%b%%selector%" == "%target_macos%00" (
    set selector=%t%
    set get_dir=%v%\%dir_macos%\%macos_version_min%+
    echo line 214
)

rem Windows user|system|archive x64|x86|arm64 scenarios
if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_win%%arch_x64%%build_user%" ...
if "%t%%a%%b%%selector%" == "%target_win%%arch_x64%%build_user%" (
    set selector=%t%-%a%-%b%
    set get_dir=%v%\%dir_win%\%dir_x64%
    echo line 222
)

if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_win%%arch_x86%%build_user%" ...
if "%t%%a%%b%%selector%" == "%target_win%%arch_x86%%build_user%" (
    set selector=%t%-%b%
    set get_dir=%v%\%dir_win%\%dir_x86%
    echo line 229
)

if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_win%%arch_arm64%%build_user%" ...
if "%t%%a%%b%%selector%" == "%target_win%%arch_arm64%%build_user%" (
    set selector=%t%-%a%-%b%
    set get_dir=%v%\%dir_win%\%dir_arm64%
    echo line 236
)

rem System scenarios leave off the %build% component
if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_win%%arch_x64%%build_system%" ...
if "%t%%a%%b%%selector%" == "%target_win%%arch_x64%%build_system%" (
    set selector=%t%-%a%
    set get_dir=%v%\%dir_win%\%dir_x64%
    echo line 244
)

if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_win%%arch_x86%%build_system%" ...
if "%t%%a%%b%%selector%" == "%target_win%%arch_x86%%build_system%" (
    set selector=%t%
    set get_dir=%v%\%dir_win%\%dir_x86%
    echo line 251
)

if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_win%%arch_arm64%%build_system%" ...
if "%t%%a%%b%%selector%" == "%target_win%%arch_arm64%%build_system%" (
    set selector=%t%-%a%
    set get_dir=%v%\%dir_win%\%dir_arm64%
    echo line 258
)

rem And several archive scenarios
if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_win%%arch_x64%%build_archive%" ...
if "%t%%a%%b%%selector%" == "%target_win%%arch_x64%%build_archive%" (
    set selector=%t%-%a%-%b%
    set get_dir=%v%\%dir_win%\%dir_x64%
    echo line 266
)

if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_win%%arch_x86%%build_archive%" ...
if "%t%%a%%b%%selector%" == "%target_win%%arch_x86%%build_archive%" (
    set selector=%t%-%b%
    set get_dir=%v%\%dir_win%\%dir_x86%
    echo line 273
)

if "%dry%" == "1" echo Dry: if "%t%%a%%b%%selector%" == "%target_win%%arch_arm64%%build_archive%" ...
if "%t%%a%%b%%selector%" == "%target_win%%arch_arm64%%build_archive%" (
    set selector=%t%-%a%-%b%
    set get_dir=%v%\%dir_win%\%dir_arm64%
    echo line 280
)

rem Make one last verification of the combinations.
if "%selector%" == "" (
    echo Invalid combination, ^(t, a, b^) = ^(%t%, %a%, %b%^)
) else (

    if not exist %get_dir% mkdir %get_dir%

    rem TODO: TBD: then get the %get_uri% to the %get_dir% ...
    set get_uri=%uri_base%%v%/%selector%%uri_suffix%

    if "%dry%" equ "1" echo get %get_uri% to %get_dir% ...
    if "%dry%" neq "1" echo do the %get_uri% ...
)

endlocal

exit /b 0

rem     linux-arm64
rem     linux-armhf
rem     linux-deb-arm64
rem     linux-deb-armhf
rem     linux-deb-x64
rem     linux-rpm-arm64
rem     linux-rpm-armhf
rem     linux-rpm-x64
rem     linux-snap-x64
rem     linux-x64
rem     win32
rem     win32-archive
rem     win32-arm64
rem     win32-arm64-archive
rem     win32-arm64-user
rem     win32-user
rem     win32-x64
rem     win32-x64-archive
rem     win32-x64-user

rem https://update.code.visualstudio.com/1.50.0/win32/stable
rem https://update.code.visualstudio.com/1.50.0/win32-archive/stable
rem https://update.code.visualstudio.com/1.50.0/win32-system/stable
rem https://update.code.visualstudio.com/1.50.0/win32-x64-user/stable



:fini_eval

if "%no_pause%" equ "1" goto :fini
pause

:fini

endlocal
