REM Effects compilation
CD ..\..\
dotnet tool restore
dotnet tool run mgfxc Source\RunActivity\Content\SceneryShader.fx Program\Content\SceneryShader.mgfx /Profile:DirectX_11
cd Source\Runactivity

