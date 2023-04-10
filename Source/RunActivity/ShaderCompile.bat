REM Effects compilation
dotnet tool restore
FOR %%i IN (*.fx) DO (
    echo Compiling %%~ni.mgfx
	dotnet mgfxc %%~nxi ..\Content\%%~ni.mgfx /Profile:DirectX_11
)
IF "%~1"=="" PAUSE
