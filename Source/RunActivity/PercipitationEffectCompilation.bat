REM Effects compilation
CD ..\..\
dotnet tool restore
dotnet tool run mgfxc Source\RunActivity\Content\PrecipitationShader.fx Program\Content\PrecipitationShader.mgfx /Profile:DirectX_11
cd Source\Runactivity

