CD ..\..\
IF EXIST "Program\Content\Web" RMDIR "Program\Content\Web" /S /Q
IF NOT EXIST "Program\Content\Web" MKDIR "Program\Content\Web"
XCOPY "Source\RunActivity\Viewer3D\WebServices\Web" "Program\Content\Web" /S /Y
DEL "Program\RunActivityLAA.*"
DEL "Program\RunActivity32.*"
DEL "Program\*.nlp"
XCOPY "Program\RunActivity.exe"
XCOPY "Program\RunActivity.pdb"
XCOPY "Program\RunActivity.exe.config"
REN RunActivity.exe RunActivity32.exe
REN RunActivity.pdb RunActivity32.pdb
REN RunActivity.exe.config RunActivity32.exe.config
XCOPY "RunActivity32.*" "Program"
DEL "RunActivity32.*"

REM Effects compilation

dotnet tool restore

FOR %%i IN (Source\RunActivity\Content\*.fx) DO (
    echo Compiling Source\RunActivity\Content\%%~ni.mgfx
	dotnet tool run mgfxc Source\RunActivity\Content\%%~nxi Program\Content\%%~ni.mgfx /Profile:DirectX_11
)
REM Copy source effects
IF EXIST "Program\ShaderSources" RMDIR "Program\ShaderSources" /S /Q
IF NOT EXIST "Program\ShaderSources" MKDIR "Program\ShaderSources"
XCOPY "Source\RunActivity\Content\*.fx" "Program\ShaderSources"
XCOPY "Source\RunActivity\ShaderCompile.bat" "Program\ShaderSources"
XCOPY "Source\RunActivity\Readme_ShaderCompile.txt" "Program\ShaderSources"
IF NOT EXIST "Program\ShaderSources\.config" MKDIR "Program\ShaderSources\.config"
XCOPY ".config\dotnet-tools.json" "Program\ShaderSources\.config"
