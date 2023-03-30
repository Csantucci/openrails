REM Effects compilation
FOR %%i IN (*.fx) DO (
    echo Compiling %%~ni.mgfx
	dotnet mgfxc %%~nxi ..\Content\%%~ni.mgfx /Profile:DirectX_11
)
