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

