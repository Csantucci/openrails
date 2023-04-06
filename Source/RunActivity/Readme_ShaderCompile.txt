
How to compile the Shader sources (rev. 1.1 of this readme)

This is neeeded only if you want to create your own version of the Shaders (*.fx files).

Starting from ORNYMG 140.1, you will find in folder ShaderSources (which is within OR_NewYear_MG folder) the original Shader sources (*.fx files).
The compiled shaders (*.mgfx files) are in folder Content, as in the official versions of OR.

Steps to be performed only once

1) Check if you have .Net 6.0 SDK installed (prerequisite): go to your System disk (usually C:) and to folder Program Files\dotnet\sdk and check
 if you have there a subfolder named 6.0 or higher.
If there is one subfolder like that, you have .Net 6.0 SDK installed. If this is the case, skip following step 2). 
2) If not, download .Net 6.0 SDK from here https://dotnet.microsoft.com/en-us/download and install it.
3) With Windows Explorer move to your ORNYMG installation, go to folder ShaderSources and double-click on mgfxinstall.bat. This will install the Shaders compiler.

Steps to be performed every time you want to modify the standard compiled shaders

1) Edit the Shader source files within folder ShaderSources according to your desire
2) Double-click on ShaderCompile.bat within folder ShaderSources. All .mgfx files will be generated and will replace the ones within the Content folder.

If you want to avoid to do these latter two steps for every new ORNYMG version you download and unzip, you can copy your modified .fx files and .mgfx files from the 
related folder within OR_NewYear_MG to backup folders (you can do this only once), and simply copy them back every time you have unzipped your new ORNYMG version.
If you use this faster procedure, previously verify that there haven't been official updates to the .fx and .mgfx files you have modified and saved. 
