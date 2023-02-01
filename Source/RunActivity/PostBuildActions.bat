CD ..\..\
IF EXIST "Program\Content\Web" RMDIR "Program\Content\Web" /S /Q
IF NOT EXIST "Program\Content\Web" MKDIR "Program\Content\Web"
XCOPY "Source\RunActivity\Viewer3D\WebServices\Web" "Program\Content\Web" /S /Y
DEL "Program\RunActivityLAA.*"
DEL "Program\RunActivity32.*"
DEL "Program\*.nlp"
XCOPY "Program\Runactivity.exe"
XCOPY "Program\Runactivity.pdb"
XCOPY "Program\Runactivity.exe.config"
REN Runactivity.exe Runactivity32.exe
REN Runactivity.pdb Runactivity32.pdb
REN Runactivity.exe.config Runactivity32.exe.config
XCOPY "Runactivity32.*" "Program"
DEL "Runactivity32.*"

REM Effects compilation

dotnet tool restore

FOR %%i IN (Source\RunActivity\Content\*.fx) DO (
    echo Compiling Source\RunActivity\Content\%%~ni.mgfx
	dotnet tool run mgfxc Source\RunActivity\Content\%%~nxi Program\Content\%%~ni.mgfx /Profile:DirectX_11
)
