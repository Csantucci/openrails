
How to compile the Shader sources

This is neeeded only if you want to create your own version of the Shaders (*.fx files).
Keep your own version in a separate folder, let's call it MyShaders.

Steps to be performed only once

1) Check if you have .Net 6.0 SDK installed: go to your System disk (usually C:) and to folder Program Files\dotnet\sdk ahd check if you have 6.0 or later installed.
If yes, skip following step. 
2) Download .Net 6.0 SDK from here https://dotnet.microsoft.com/en-us/download and install it.
3) With Windows Explorer move to your ORNYMG installation, go to folder ShaderSources and double-click on mgfxinstall.bat. This will install the Shaders compiler.

Step to be performed every time you want to modify the standard compiled shaders

1) Copy your shaders from MyShaders to folder Content of your ORNYMG installation. You may copy only the files you have modified from the default ones.
2) With Windows Explorer move to your ORNYMG installation, go to folder ShaderSources and double-click on ShaderCompile.bat. This will create the *.mgfx files 
(that is the compiled shaders) in folder Content of your ORNYMG installation.