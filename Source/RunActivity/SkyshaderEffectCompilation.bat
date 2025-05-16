REM Effects compilation
CD ..\..\
dotnet tool restore
dotnet tool run mgfxc Source\RunActivity\Content\SkyShader.fx Program\Content\Skyshader.mgfx /Profile:DirectX_11
cd Source\Runactivity

