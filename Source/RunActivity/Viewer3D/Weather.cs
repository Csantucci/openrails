// COPYRIGHT 2010, 2011, 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

// Debug compiler flag for test output for automatic weather
//#define DEBUG_AUTOWEATHER 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.MultiPlayer;
using Orts.Simulation;
using ORTS.Common;
using ORTS.Common.Input;
using Swan.Logging;
using Events = Orts.Common.Events;

namespace Orts.Viewer3D
{   
    // <----------------------------- ExRail ------------------------------>
    public class WeatherControl
    {
        public readonly Viewer Viewer;
        public readonly WeatherExtension Weather;
        public bool ReloadWeatherSwitch, SaveWeatherSwitch;
        BinaryWriter WeatherFileOut;
        BinaryReader WeatherFileIn;
        public readonly List<SoundSourceBase> ClearSound;
        public readonly List<SoundSourceBase> RainSound;
        public readonly List<SoundSourceBase> SnowSound;
        public readonly List<SoundSourceBase> FewSound;
        public readonly List<SoundSourceBase> CloudySound;
        public readonly List<SoundSourceBase> DesertSound;
        public readonly List<SoundSourceBase> SnowStormSound;
        public readonly List<SoundSourceBase> FoggySound;
        public readonly List<SoundSourceBase> PartlyCloudySound;

        public readonly List<SoundSourceBase> WeatherSounds = new List<SoundSourceBase>();

        public bool weatherChangeOn;
        public DynamicWeather dynamicWeather;
        public bool RandomizedWeather;
        public bool DesertZone; // We are in a desert zone, so no randomized weather change...
        private readonly float[,] DesertZones = { { 30, 45, -120, -105 } }; // minlat, maxlat, minlong, maxlong

        // Variables used for wind calculations
        Vector2 WindSpeedInternalMpS;
        readonly Vector2[] windSpeedMpS = new Vector2[2];
        public float Time;
        readonly float[] WindChangeMpSS = { 40, 5 }; // Flurry, steady
        const float WindSpeedMaxMpS = 4.5f;
        float WindUpdateTimer;
        readonly float WindGustUpdateTimeS = 1.0f;
        bool InitialWind = true;
        float BaseWindDirectionRad;
        readonly float WindDirectionVariationRad = MathHelper.ToRadians(45.0f); // Set at 45 Deg
        float calculatedWindDirection;

        private const float FogMinDistance = 10f;
        private const float FogMaxDistance = 200000f;

        public WeatherControl(Viewer viewer)
        {
            Viewer = viewer;
            Weather = Viewer.Simulator.Weather;
            ReloadWeatherSwitch = false;
            SaveWeatherSwitch = false;
   
            var pathArray = new[] {
                Program.Simulator.RoutePath + @"\SOUND",
                Program.Simulator.BasePath + @"\SOUND",
            };

            ClearSound = new List<SoundSourceBase> {
              
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "clear_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "clear_ex.sms"), false),
            };
            RainSound = new List<SoundSourceBase> {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "rain_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "rain_ex.sms"), false),
            };
            SnowSound = new List<SoundSourceBase> {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "snow_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "snow_ex.sms"), false),
            };
            // <----------------------------- ExRail ------------------------------>
            FewSound = new List<SoundSourceBase> {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "few_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "few_ex.sms"), false),
            };

            CloudySound = new List<SoundSourceBase> {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "cloudy_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "cloudy_ex.sms"), false),
            };

            DesertSound = new List<SoundSourceBase> {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "desert_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "desert_ex.sms"), false),
            };

            SnowStormSound = new List<SoundSourceBase> {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "SnowStorm_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "SnowStorm_ex.sms"), false),
            };
            FoggySound = new List<SoundSourceBase> {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "Foggy_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "Foggy_ex.sms"), false),
            };
            PartlyCloudySound = new List<SoundSourceBase> {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "PartlyCloudy_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "PartlyCloudy_ex.sms"), false),
            };
        
            WeatherSounds.AddRange(ClearSound);
            WeatherSounds.AddRange(RainSound);
            WeatherSounds.AddRange(SnowSound);

            WeatherSounds.AddRange(FewSound);
            WeatherSounds.AddRange(CloudySound);
            WeatherSounds.AddRange(DesertSound);
            WeatherSounds.AddRange(SnowStormSound);
            WeatherSounds.AddRange(FoggySound);
            WeatherSounds.AddRange(PartlyCloudySound);
        
            SetInitialWeatherParameters();
            UpdateWeatherParameters();

            // Add here randomized weather
            if (Viewer.Settings.ActWeatherRandomizationLevel > 0 && Viewer.Simulator.ActivityRun != null && !Viewer.Simulator.ActivityRun.WeatherChangesPresent)
            {
                RandomizedWeather = RandomizeInitialWeather();
                dynamicWeather = new DynamicWeather();
                if (RandomizedWeather)
                {
                    UpdateSoundSources();
                    UpdateVolume();
                    // We have a pause in weather change, depending from randomization level
                    dynamicWeather.stableWeatherTimer = ((4.0f - Viewer.Settings.ActWeatherRandomizationLevel) * 600) + Viewer.Random.Next(300) - 150;
                    weatherChangeOn = true;
                }
            }

            Viewer.Simulator.WeatherChanged += (object sender, EventArgs e) =>
            {
                SetInitialWeatherParameters();
                UpdateWeatherParameters();
            };
        }
        
        public virtual void SaveWeatherParameters(BinaryWriter outf)
        {
            outf.Write(0); // Set fixed weather
            outf.Write(RandomizedWeather);
            outf.Write(weatherChangeOn);
            if (weatherChangeOn) {
                dynamicWeather.Save(outf);
            }

        }

        public virtual void RestoreWeatherParameters(BinaryReader inf)
        {
            int weathercontroltype = inf.ReadInt32();
            // Restoring wrong type of weather - abort
            if (weathercontroltype != 0) {
                Trace.TraceError(Simulator.Catalog.GetString("Restoring wrong weather type : trying to restore dynamic weather but save contains user controlled weather"));
            }
            RandomizedWeather = inf.ReadBoolean();
            weatherChangeOn = inf.ReadBoolean();
            
            if (weatherChangeOn) {
                dynamicWeather = new DynamicWeather();
                dynamicWeather.Restore(inf);
            }
            UpdateVolume();
        }
        // <----------------------------- ExRail ------------------------------>
        public virtual void LoadWeatherType(BinaryReader wfile)
        {
            int weathercontroltype = wfile.ReadInt32();
            // Restoring wrong type of weather - abort
            if (weathercontroltype != 0) {
                Trace.TraceError(Simulator.Catalog.GetString("Restoring wrong weather type : trying to restore dynamic weather but save contains user controlled weather"));
            }
            RandomizedWeather = wfile.ReadBoolean();
            weatherChangeOn = wfile.ReadBoolean();
            
            if (weatherChangeOn) {
                dynamicWeather = new DynamicWeather();
                // dynamicWeather.Restore(wfile);
            }

            Weather.PrecipitationLiquidity = wfile.ReadSingle();
            Weather.PricipitationIntensityPPSPM2 = wfile.ReadSingle();
            // W I N D
            Weather.WindSpeed = wfile.ReadSingle();
            Weather.WindDirectionSky = wfile.ReadSingle();
            // R A I N / S N O W
            Weather.PrecipWind1.X = wfile.ReadSingle();                            
            Weather.PrecipWind1.Y = wfile.ReadSingle();                            
            Weather.PrecipWind1.Z = wfile.ReadSingle();                            
            Weather.PrecipWind2.X = wfile.ReadSingle();                            
            Weather.PrecipWind2.Y = wfile.ReadSingle();                            
            Weather.PrecipWind2.Z = wfile.ReadSingle(); 
            // P A R T I C L E  S I Z E
            Weather.ParticleSize1 = wfile.ReadSingle(); 
            Weather.ParticleSize2 = wfile.ReadSingle(); 
            // C L O U D S  O P A C I T Y 
            Weather.OvercastFactor = wfile.ReadSingle();
            Weather.OvercastFactor2 = wfile.ReadSingle();
            Weather.OvercastFactor3 = wfile.ReadSingle();
            // S U N
            Weather.SunSize_Sunrise = wfile.ReadSingle();
            Weather.SunSize_Noon    = wfile.ReadSingle();   
            Weather.SunSize_Sunset  = wfile.ReadSingle(); 
            // S K Y
            Weather.SkyFogDistance_Sunrise = wfile.ReadSingle();
            Weather.SkyFogDistance_Noon    = wfile.ReadSingle();   
            Weather.SkyFogDistance_Sunset  = wfile.ReadSingle(); 
            Weather.SkyFog_Sunrise.R = wfile.ReadByte();
            Weather.SkyFog_Sunrise.G = wfile.ReadByte();
            Weather.SkyFog_Sunrise.B = wfile.ReadByte();
            Weather.SkyFog_Noon.R = wfile.ReadByte();   
            Weather.SkyFog_Noon.G = wfile.ReadByte();
            Weather.SkyFog_Noon.B = wfile.ReadByte();
            Weather.SkyFog_Sunset.R = wfile.ReadByte(); 
            Weather.SkyFog_Sunset.G = wfile.ReadByte();
            Weather.SkyFog_Sunset.B = wfile.ReadByte();
            // S C E N E R Y
            Weather.SceneryFogDistance_Sunrise = wfile.ReadSingle();
            Weather.SceneryFogDistance_Noon    = wfile.ReadSingle();   
            Weather.SceneryFogDistance_Sunset  = wfile.ReadSingle(); 
            Weather.SceneryFog_Sunrise.R = wfile.ReadByte();
            Weather.SceneryFog_Sunrise.G = wfile.ReadByte();
            Weather.SceneryFog_Sunrise.B = wfile.ReadByte();
            Weather.SceneryFog_Noon.R = wfile.ReadByte();   
            Weather.SceneryFog_Noon.G = wfile.ReadByte();
            Weather.SceneryFog_Noon.B = wfile.ReadByte();
            Weather.SceneryFog_Sunset.R = wfile.ReadByte(); 
            Weather.SceneryFog_Sunset.G = wfile.ReadByte();
            Weather.SceneryFog_Sunset.B = wfile.ReadByte();
            // V E G E T A T I O N
            Weather.VegetationDesatuationModifier = wfile.ReadSingle();
            Weather.VegetationBrightnessModifier  = wfile.ReadSingle();
            Weather.VegetationContrastModifier    = wfile.ReadSingle();
            // T E R R A I N
            Weather.TerrainDesatuationModifier = wfile.ReadSingle();
            Weather.TerrainBrightnessModifier  = wfile.ReadSingle();
            Weather.TerrainContrastModifier    = wfile.ReadSingle();
            // 
            wfile.Close();
            UpdateVolume();
        }

        // <----------------------------- ExRail ------------------------------>
        public virtual void SaveWeatherType(BinaryWriter wfile )
        {
            wfile.Write(0); // Set fixed weather
            wfile.Write(RandomizedWeather);
            wfile.Write(weatherChangeOn);
            if (weatherChangeOn) {
                // dynamicWeather.Save(wfile);
            }
            wfile.Write(Weather.PrecipitationLiquidity);
            wfile.Write(Weather.PricipitationIntensityPPSPM2);
            // 
            wfile.Write(Weather.WindSpeed);
            wfile.Write(Weather.WindDirectionSky);
            // 
            wfile.Write(Weather.PrecipWind1.X);                            
            wfile.Write(Weather.PrecipWind1.Y);                            
            wfile.Write(Weather.PrecipWind1.Z);                            
            wfile.Write(Weather.PrecipWind2.X);                            
            wfile.Write(Weather.PrecipWind2.Y);                            
            wfile.Write(Weather.PrecipWind2.Z);
            // P A R T I C L E  S I Z E
            wfile.Write(Weather.ParticleSize1); 
            wfile.Write(Weather.ParticleSize2); 
            // O V E R C A S T 
            wfile.Write(Weather.OvercastFactor);
            wfile.Write(Weather.OvercastFactor2);
            wfile.Write(Weather.OvercastFactor3);
            // S U N
            wfile.Write(Weather.SunSize_Sunrise);
            wfile.Write(Weather.SunSize_Noon);   
            wfile.Write(Weather.SunSize_Sunset); 
            // S K Y
            wfile.Write(Weather.SkyFogDistance_Sunrise);
            wfile.Write(Weather.SkyFogDistance_Noon);   
            wfile.Write(Weather.SkyFogDistance_Sunset);
            wfile.Write(Weather.SkyFog_Sunrise.R);
            wfile.Write(Weather.SkyFog_Sunrise.G);
            wfile.Write(Weather.SkyFog_Sunrise.B);
            wfile.Write(Weather.SkyFog_Noon.R);   
            wfile.Write(Weather.SkyFog_Noon.G);
            wfile.Write(Weather.SkyFog_Noon.B);
            wfile.Write(Weather.SkyFog_Sunset.R); 
            wfile.Write(Weather.SkyFog_Sunset.G);
            wfile.Write(Weather.SkyFog_Sunset.B);
            // S C E N E R Y
            wfile.Write(Weather.SceneryFogDistance_Sunrise);
            wfile.Write(Weather.SceneryFogDistance_Noon);   
            wfile.Write(Weather.SceneryFogDistance_Sunset); 
            wfile.Write(Weather.SceneryFog_Sunrise.R);
            wfile.Write(Weather.SceneryFog_Sunrise.G);
            wfile.Write(Weather.SceneryFog_Sunrise.B);
            wfile.Write(Weather.SceneryFog_Noon.R);   
            wfile.Write(Weather.SceneryFog_Noon.G);
            wfile.Write(Weather.SceneryFog_Noon.B);
            wfile.Write(Weather.SceneryFog_Sunset.R); 
            wfile.Write(Weather.SceneryFog_Sunset.G);
            wfile.Write(Weather.SceneryFog_Sunset.B);
            // V E G E T A T I O N
            wfile.Write(Weather.VegetationDesatuationModifier);
            wfile.Write(Weather.VegetationBrightnessModifier);
            wfile.Write(Weather.VegetationContrastModifier);
            // T E R R A I N
            wfile.Write(Weather.TerrainDesatuationModifier);
            wfile.Write(Weather.TerrainBrightnessModifier);
            wfile.Write(Weather.TerrainContrastModifier);
            // 
            wfile.Flush();
            wfile.Close();
        }

        // <----------------------------- ExRail ------------------------------>
        // Load or Initilize Weather 
        string Weatherfilepath;
        FileInfo fileInfo;
        public void SetInitialWeatherParameters()
        {
            switch (Viewer.Simulator.Season)
            {
                case SeasonType.Spring:
                { 
                    switch (Viewer.Simulator.WeatherType)
                    {
                        case WeatherType.Clear:
                            Weatherfilepath = "Weather/Saves/WeatherType_SpringClear.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   // HUSK DEN LOADER FRA FIL !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                // --------------------------------------------                       
                                Weather.WindSpeed = 12.0f;
                                Weather.WindDirectionSky = 1.0f;
                                // --------------------------------------------  
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // --------------------------------------------
                                Weather.OvercastFactor  = 0.1f;
                                Weather.OvercastFactor2 = 0.0f; 
                                Weather.OvercastFactor3 = 0.0f;
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.3f;
                                Weather.SunSize_Sunset  = 1.5f;
                                // --------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 3940.0f;
                                Weather.SkyFogDistance_Noon    = 4780.0f;
                                Weather.SkyFogDistance_Sunset  = 3420.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(142, 172, 215, 255);
                                Weather.SkyFog_Sunset   = new Color(195, 163, 128, 255);
                                // --------------------------------------------
                                Weather.SceneryFogDistance_Sunrise =  710.0f;
                                Weather.SceneryFogDistance_Noon    =  1440.0f;
                                Weather.SceneryFogDistance_Sunset  =  830.0f;
                                Weather.SceneryFog_Sunrise  = new Color(185, 182, 196, 255);
                                Weather.SceneryFog_Noon     = new Color(149, 176, 218, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 1.0f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 1.0f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut ); 
                            }
                        break;

                        case WeatherType.Rain:

                            Weatherfilepath = "Weather/Saves/WeatherType_SpringRain.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {  
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 2.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.53f;
                                Weather.OvercastFactor2 = 0.28f; 
                                Weather.OvercastFactor3 = 0.56f;
                                // -----------------------------------------------                
                                Weather.ParticleSize1 = 0.88f;
                                Weather.ParticleSize2 = 0.88f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( -7.0f, 0.0f, -14.8f);
                                Weather.PrecipWind2 = new Vector3( -12.0f, 0.0f, -3.9f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 2950.0f;
                                Weather.SkyFogDistance_Noon    = 4700.0f;
                                Weather.SkyFogDistance_Sunset  = 3600.0f;
                                Weather.SkyFog_Sunrise  = new Color(117, 108, 105, 255);
                                Weather.SkyFog_Noon     = new Color(116,  111, 113, 255);
                                Weather.SkyFog_Sunset   = new Color(112, 105, 105, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 500.0f;
                                Weather.SceneryFogDistance_Noon    = 140.0f;
                                Weather.SceneryFogDistance_Sunset  = 500.0f;
                                Weather.SceneryFog_Sunrise  = new Color(99, 92, 92, 255);
                                Weather.SceneryFog_Noon     = new Color(140, 139, 140, 255);
                                Weather.SceneryFog_Sunset   = new Color(115, 107, 105, 205);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.0f;
                                Weather.SunSize_Sunset  = 1.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.5f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.7f;
                                Weather.TerrainBrightnessModifier  = 0.77f;
                                Weather.TerrainContrastModifier    = 0.98f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Snow:
                
                            Weatherfilepath = "Weather/Saves/WeatherType_SpringSnow.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.195f;
                                Weather.OvercastFactor2 = 0.20f; 
                                Weather.OvercastFactor3 = 0.20f; 
                                // -----------------------------------------------
                                Weather.ParticleSize1 = 1.8f;
                                Weather.ParticleSize2 = 1.5f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 2.0f, 0.0f, 4.4f);
                                Weather.PrecipWind2 = new Vector3( 8.0f, 0.0f, 8.8f);
                                // --------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 500.0f;
                                Weather.SkyFogDistance_Noon    = 500.0f;
                                Weather.SkyFogDistance_Sunset  = 500.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(183, 182, 182, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 200.0f;
                                Weather.SceneryFogDistance_Noon    = 350.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 176, 174, 255);
                                Weather.SceneryFog_Noon     = new Color(180, 176, 174, 255);
                                Weather.SceneryFog_Sunset   = new Color(180, 176, 174, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 2.0f;
                                Weather.SunSize_Noon    = 2.0f;
                                Weather.SunSize_Sunset  = 2.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.6f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.6f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Few:

                            Weatherfilepath = "Weather/Saves/WeatherType_SpringFew.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.10f; 
                                Weather.OvercastFactor2 = 0.16f; 
                                Weather.OvercastFactor3 = 0.18f;
                                // -----------------------------------------------                        
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 6600.0f;
                                Weather.SkyFogDistance_Noon    = 4900.0f;
                                Weather.SkyFogDistance_Sunset  = 5000.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(128, 164, 227, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 680.0f;
                                Weather.SceneryFogDistance_Noon    = 1000.0f;
                                Weather.SceneryFogDistance_Sunset  = 720.0f;
                                Weather.SceneryFog_Sunrise  = new Color(170, 172, 170, 255);
                                Weather.SceneryFog_Noon     = new Color(128, 164, 227, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.1f;
                                Weather.SunSize_Noon    = 2.0f;
                                Weather.SunSize_Sunset  = 1.5f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------                        
                                Weather.TerrainDesatuationModifier = 1.0f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        

                        break;

                        case WeatherType.Cloudy:

                            Weatherfilepath = "Weather/Saves/WeatherType_SpringCloudy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = -1.0f;
                                Weather.WindDirectionSky = 0.55f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.05f; 
                                Weather.OvercastFactor2 = 0.08f; 
                                Weather.OvercastFactor3 = 0.15f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 5300.0f;
                                Weather.SkyFogDistance_Noon    = 4480.0f;
                                Weather.SkyFogDistance_Sunset  = 3640.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(188, 177, 180, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 870.0f;
                                Weather.SceneryFogDistance_Noon    = 1070.0f;
                                Weather.SceneryFogDistance_Sunset  = 890.0f;
                                Weather.SceneryFog_Sunrise  = new Color(174, 175, 173, 255);
                                Weather.SceneryFog_Noon     = new Color(207, 191, 194, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.67f;
                                Weather.SunSize_Noon    = 1.7f;
                                Weather.SunSize_Sunset  = 2.2f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------                        
                                SaveWeatherType( WeatherFileOut );
                            }

                        break;

                        case WeatherType.Desert:

                            Weatherfilepath = "Weather/Saves/WeatherType_SpringDesert.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.3f;
                                Weather.WindDirectionSky = 2.40f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.07f; 
                                Weather.OvercastFactor2 = 0.18f; 
                                Weather.OvercastFactor3 = 0.14f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 5300.0f;
                                Weather.SkyFogDistance_Noon    = 4480.0f;
                                Weather.SkyFogDistance_Sunset  = 3640.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(188, 177, 180, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 100.0f;
                                Weather.SceneryFogDistance_Noon    = 1070.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(229, 194, 152, 255);
                                Weather.SceneryFog_Noon     = new Color(207, 191, 194, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.67f;
                                Weather.SunSize_Noon    = 1.7f;
                                Weather.SunSize_Sunset  = 2.2f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.SnowStorm:

                            Weatherfilepath = "Weather/Saves/WeatherType_SpringSnowStorm.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.14f;
                                Weather.WindDirectionSky = 0.0f;

                                Weather.OvercastFactor  = 0.10f;
                                Weather.OvercastFactor2 = 0.14f; 
                                Weather.OvercastFactor3 = 0.10f; 
                                // -----------------------------------------------
                                Weather.ParticleSize1  = 1.7f;
                                Weather.ParticleSize2  = 2.0f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 7.5f, 0.4f, 10.3f);
                                Weather.PrecipWind2 = new Vector3( 1.7f, 0.0f, 20.6f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 6650.0f;
                                Weather.SkyFogDistance_Noon    = 6400.0f;
                                Weather.SkyFogDistance_Sunset  = 1960.0f;
                                Weather.SkyFog_Sunrise  = new Color(164, 161, 163, 255);
                                Weather.SkyFog_Noon     = new Color(167, 167, 170, 255);
                                Weather.SkyFog_Sunset   = new Color(112, 105, 105, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 320.0f;
                                Weather.SceneryFogDistance_Noon    = 280.0f;
                                Weather.SceneryFogDistance_Sunset  = 500.0f;
                                Weather.SceneryFog_Sunrise  = new Color(115, 108, 106, 255);
                                Weather.SceneryFog_Noon     = new Color(161, 161, 160, 255);
                                Weather.SceneryFog_Sunset   = new Color(105, 106, 100, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.0f;
                                Weather.SunSize_Sunset  = 1.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.6f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.5f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Foggy:

                            Weatherfilepath = "Weather/Saves/WeatherType_SpringFoggy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.00f;
                                Weather.OvercastFactor2 = 0.031f; 
                                Weather.OvercastFactor3 = 0.081f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 3250.0f;
                                Weather.SkyFogDistance_Noon    = 3600.0f;
                                Weather.SkyFogDistance_Sunset  = 2800.0f;
                                Weather.SkyFog_Sunrise  = new Color(203, 204, 219, 255);
                                Weather.SkyFog_Noon     = new Color(176, 196, 206, 255);
                                Weather.SkyFog_Sunset   = new Color(233, 237, 245, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 100.0f;
                                Weather.SceneryFogDistance_Noon    = 140.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 181, 185, 255);
                                Weather.SceneryFog_Noon     = new Color(177, 199, 209, 255);
                                Weather.SceneryFog_Sunset   = new Color(178, 181, 184, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 0.9f;
                                Weather.SunSize_Sunset  = 0.8f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.5f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.PartlyCloudy:
                     
                            Weatherfilepath = "Weather/Saves/WeatherType_SpringPartlyCloudy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.00f;
                                Weather.OvercastFactor2 = 0.00f; 
                                Weather.OvercastFactor3 = 0.01f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 15000.0f;
                                Weather.SkyFogDistance_Noon    = 15000.0f;
                                Weather.SkyFogDistance_Sunset  = 15000.0f;
                                Weather.SkyFog_Sunrise  = new Color(203, 204, 219, 255);
                                Weather.SkyFog_Noon     = new Color(176, 196, 206, 255);
                                Weather.SkyFog_Sunset   = new Color(233, 237, 245, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 2530.0f;
                                Weather.SceneryFogDistance_Noon    = 2770.0f;
                                Weather.SceneryFogDistance_Sunset  = 3030.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 181, 185, 255);
                                Weather.SceneryFog_Noon     = new Color(155, 174, 200, 255);
                                Weather.SceneryFog_Sunset   = new Color(178, 181, 184, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 0.65f;
                                Weather.SunSize_Noon    = 0.9f;
                                Weather.SunSize_Sunset  = 0.8f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.81f;
                                Weather.VegetationBrightnessModifier  = 0.84f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.9f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;
                    }
                    break;
                }

                case SeasonType.Summer:
                {                     
                    switch (Viewer.Simulator.WeatherType)
                    {
                        case WeatherType.Clear:
                            Weatherfilepath = "Weather/Saves/WeatherType_SummerClear.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   // HUSK DEN LOADER FRA FIL !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                // --------------------------------------------                       
                                Weather.WindSpeed = 12.0f;
                                Weather.WindDirectionSky = 1.0f;
                                // --------------------------------------------  
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // --------------------------------------------
                                Weather.OvercastFactor  = 0.1f;
                                Weather.OvercastFactor2 = 0.0f; 
                                Weather.OvercastFactor3 = 0.0f;
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.3f;
                                Weather.SunSize_Sunset  = 1.5f;
                                // --------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 3940.0f;
                                Weather.SkyFogDistance_Noon    = 4780.0f;
                                Weather.SkyFogDistance_Sunset  = 3420.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(142, 172, 215, 255);
                                Weather.SkyFog_Sunset   = new Color(195, 163, 128, 255);
                                // --------------------------------------------
                                Weather.SceneryFogDistance_Sunrise =  710.0f;
                                Weather.SceneryFogDistance_Noon    =  1440.0f;
                                Weather.SceneryFogDistance_Sunset  =  830.0f;
                                Weather.SceneryFog_Sunrise  = new Color(185, 182, 196, 255);
                                Weather.SceneryFog_Noon     = new Color(149, 176, 218, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 1.0f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 1.0f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut ); 
                            }
                        break;

                        case WeatherType.Rain:

                            Weatherfilepath = "Weather/Saves/WeatherType_SummerRain.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {  
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 2.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.53f;
                                Weather.OvercastFactor2 = 0.28f; 
                                Weather.OvercastFactor3 = 0.56f;
                                // -----------------------------------------------                
                                Weather.ParticleSize1 = 0.88f;
                                Weather.ParticleSize2 = 0.88f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( -7.0f, 0.0f, -14.8f);
                                Weather.PrecipWind2 = new Vector3( -12.0f, 0.0f, -3.9f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 2950.0f;
                                Weather.SkyFogDistance_Noon    = 4700.0f;
                                Weather.SkyFogDistance_Sunset  = 3600.0f;
                                Weather.SkyFog_Sunrise  = new Color(117, 108, 105, 255);
                                Weather.SkyFog_Noon     = new Color(116,  111, 113, 255);
                                Weather.SkyFog_Sunset   = new Color(112, 105, 105, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 500.0f;
                                Weather.SceneryFogDistance_Noon    = 140.0f;
                                Weather.SceneryFogDistance_Sunset  = 500.0f;
                                Weather.SceneryFog_Sunrise  = new Color(99, 92, 92, 255);
                                Weather.SceneryFog_Noon     = new Color(140, 139, 140, 255);
                                Weather.SceneryFog_Sunset   = new Color(115, 107, 105, 205);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.0f;
                                Weather.SunSize_Sunset  = 1.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.5f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.7f;
                                Weather.TerrainBrightnessModifier  = 0.77f;
                                Weather.TerrainContrastModifier    = 0.98f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Snow:
                
                            Weatherfilepath = "Weather/Saves/WeatherType_SummerSnow.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.195f;
                                Weather.OvercastFactor2 = 0.20f; 
                                Weather.OvercastFactor3 = 0.20f; 
                                // -----------------------------------------------
                                Weather.ParticleSize1 = 1.8f;
                                Weather.ParticleSize2 = 1.5f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 2.0f, 0.0f, 4.4f);
                                Weather.PrecipWind2 = new Vector3( 8.0f, 0.0f, 8.8f);
                                // --------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 500.0f;
                                Weather.SkyFogDistance_Noon    = 500.0f;
                                Weather.SkyFogDistance_Sunset  = 500.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(183, 182, 182, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 200.0f;
                                Weather.SceneryFogDistance_Noon    = 350.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 176, 174, 255);
                                Weather.SceneryFog_Noon     = new Color(180, 176, 174, 255);
                                Weather.SceneryFog_Sunset   = new Color(180, 176, 174, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 2.0f;
                                Weather.SunSize_Noon    = 2.0f;
                                Weather.SunSize_Sunset  = 2.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.6f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.6f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Few:

                            Weatherfilepath = "Weather/Saves/WeatherType_SummerFew.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.10f; 
                                Weather.OvercastFactor2 = 0.16f; 
                                Weather.OvercastFactor3 = 0.18f;
                                // -----------------------------------------------                        
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 6600.0f;
                                Weather.SkyFogDistance_Noon    = 4900.0f;
                                Weather.SkyFogDistance_Sunset  = 5000.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(128, 164, 227, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 680.0f;
                                Weather.SceneryFogDistance_Noon    = 1000.0f;
                                Weather.SceneryFogDistance_Sunset  = 720.0f;
                                Weather.SceneryFog_Sunrise  = new Color(170, 172, 170, 255);
                                Weather.SceneryFog_Noon     = new Color(128, 164, 227, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.1f;
                                Weather.SunSize_Noon    = 2.0f;
                                Weather.SunSize_Sunset  = 1.5f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------                        
                                Weather.TerrainDesatuationModifier = 1.0f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        

                        break;

                        case WeatherType.Cloudy:

                            Weatherfilepath = "Weather/Saves/WeatherType_SummerCloudy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = -1.0f;
                                Weather.WindDirectionSky = 0.55f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.05f; 
                                Weather.OvercastFactor2 = 0.08f; 
                                Weather.OvercastFactor3 = 0.15f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 5300.0f;
                                Weather.SkyFogDistance_Noon    = 4480.0f;
                                Weather.SkyFogDistance_Sunset  = 3640.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(188, 177, 180, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 870.0f;
                                Weather.SceneryFogDistance_Noon    = 1070.0f;
                                Weather.SceneryFogDistance_Sunset  = 890.0f;
                                Weather.SceneryFog_Sunrise  = new Color(174, 175, 173, 255);
                                Weather.SceneryFog_Noon     = new Color(207, 191, 194, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.67f;
                                Weather.SunSize_Noon    = 1.7f;
                                Weather.SunSize_Sunset  = 2.2f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------                        
                                SaveWeatherType( WeatherFileOut );
                            }

                        break;

                        case WeatherType.Desert:

                            Weatherfilepath = "Weather/Saves/WeatherType_SummerDesert.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.3f;
                                Weather.WindDirectionSky = 2.40f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.07f; 
                                Weather.OvercastFactor2 = 0.18f; 
                                Weather.OvercastFactor3 = 0.14f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 5300.0f;
                                Weather.SkyFogDistance_Noon    = 4480.0f;
                                Weather.SkyFogDistance_Sunset  = 3640.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(188, 177, 180, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 100.0f;
                                Weather.SceneryFogDistance_Noon    = 1070.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(229, 194, 152, 255);
                                Weather.SceneryFog_Noon     = new Color(207, 191, 194, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.67f;
                                Weather.SunSize_Noon    = 1.7f;
                                Weather.SunSize_Sunset  = 2.2f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.SnowStorm:

                            Weatherfilepath = "Weather/Saves/WeatherType_SummerSnowStorm.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.14f;
                                Weather.WindDirectionSky = 0.0f;

                                Weather.OvercastFactor  = 0.10f;
                                Weather.OvercastFactor2 = 0.14f; 
                                Weather.OvercastFactor3 = 0.10f; 
                                // -----------------------------------------------
                                Weather.ParticleSize1  = 1.7f;
                                Weather.ParticleSize2  = 2.0f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 7.5f, 0.4f, 10.3f);
                                Weather.PrecipWind2 = new Vector3( 1.7f, 0.0f, 20.6f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 6650.0f;
                                Weather.SkyFogDistance_Noon    = 6400.0f;
                                Weather.SkyFogDistance_Sunset  = 1960.0f;
                                Weather.SkyFog_Sunrise  = new Color(164, 161, 163, 255);
                                Weather.SkyFog_Noon     = new Color(167, 167, 170, 255);
                                Weather.SkyFog_Sunset   = new Color(112, 105, 105, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 320.0f;
                                Weather.SceneryFogDistance_Noon    = 280.0f;
                                Weather.SceneryFogDistance_Sunset  = 500.0f;
                                Weather.SceneryFog_Sunrise  = new Color(115, 108, 106, 255);
                                Weather.SceneryFog_Noon     = new Color(161, 161, 160, 255);
                                Weather.SceneryFog_Sunset   = new Color(105, 106, 100, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.0f;
                                Weather.SunSize_Sunset  = 1.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.6f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.5f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Foggy:

                            Weatherfilepath = "Weather/Saves/WeatherType_SummerFoggy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.00f;
                                Weather.OvercastFactor2 = 0.031f; 
                                Weather.OvercastFactor3 = 0.081f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 3250.0f;
                                Weather.SkyFogDistance_Noon    = 3600.0f;
                                Weather.SkyFogDistance_Sunset  = 2800.0f;
                                Weather.SkyFog_Sunrise  = new Color(203, 204, 219, 255);
                                Weather.SkyFog_Noon     = new Color(176, 196, 206, 255);
                                Weather.SkyFog_Sunset   = new Color(233, 237, 245, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 100.0f;
                                Weather.SceneryFogDistance_Noon    = 140.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 181, 185, 255);
                                Weather.SceneryFog_Noon     = new Color(177, 199, 209, 255);
                                Weather.SceneryFog_Sunset   = new Color(178, 181, 184, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 0.9f;
                                Weather.SunSize_Sunset  = 0.8f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.5f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.PartlyCloudy:
                     
                            Weatherfilepath = "Weather/Saves/WeatherType_SummerPartlyCloudy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.00f;
                                Weather.OvercastFactor2 = 0.00f; 
                                Weather.OvercastFactor3 = 0.01f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 15000.0f;
                                Weather.SkyFogDistance_Noon    = 15000.0f;
                                Weather.SkyFogDistance_Sunset  = 15000.0f;
                                Weather.SkyFog_Sunrise  = new Color(203, 204, 219, 255);
                                Weather.SkyFog_Noon     = new Color(176, 196, 206, 255);
                                Weather.SkyFog_Sunset   = new Color(233, 237, 245, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 2530.0f;
                                Weather.SceneryFogDistance_Noon    = 2770.0f;
                                Weather.SceneryFogDistance_Sunset  = 3030.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 181, 185, 255);
                                Weather.SceneryFog_Noon     = new Color(155, 174, 200, 255);
                                Weather.SceneryFog_Sunset   = new Color(178, 181, 184, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 0.65f;
                                Weather.SunSize_Noon    = 0.9f;
                                Weather.SunSize_Sunset  = 0.8f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.81f;
                                Weather.VegetationBrightnessModifier  = 0.84f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.9f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;
                    }
                    break;
                }

                case SeasonType.Autumn:
                {
                    switch (Viewer.Simulator.WeatherType)
                    {
                        case WeatherType.Clear:
                            Weatherfilepath = "Weather/Saves/WeatherType_AutumnClear.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   // HUSK DEN LOADER FRA FIL !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                // --------------------------------------------                       
                                Weather.WindSpeed = 12.0f;
                                Weather.WindDirectionSky = 1.0f;
                                // --------------------------------------------  
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // --------------------------------------------
                                Weather.OvercastFactor  = 0.1f;
                                Weather.OvercastFactor2 = 0.0f; 
                                Weather.OvercastFactor3 = 0.0f;
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.3f;
                                Weather.SunSize_Sunset  = 1.5f;
                                // --------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 3940.0f;
                                Weather.SkyFogDistance_Noon    = 4780.0f;
                                Weather.SkyFogDistance_Sunset  = 3420.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(142, 172, 215, 255);
                                Weather.SkyFog_Sunset   = new Color(195, 163, 128, 255);
                                // --------------------------------------------
                                Weather.SceneryFogDistance_Sunrise =  710.0f;
                                Weather.SceneryFogDistance_Noon    =  1440.0f;
                                Weather.SceneryFogDistance_Sunset  =  830.0f;
                                Weather.SceneryFog_Sunrise  = new Color(185, 182, 196, 255);
                                Weather.SceneryFog_Noon     = new Color(149, 176, 218, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 1.0f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 1.0f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut ); 
                            }
                        break;

                        case WeatherType.Rain:

                            Weatherfilepath = "Weather/Saves/WeatherType_AutumnRain.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {  
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 2.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.53f;
                                Weather.OvercastFactor2 = 0.28f; 
                                Weather.OvercastFactor3 = 0.56f;
                                // -----------------------------------------------                
                                Weather.ParticleSize1 = 0.88f;
                                Weather.ParticleSize2 = 0.88f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( -7.0f, 0.0f, -14.8f);
                                Weather.PrecipWind2 = new Vector3( -12.0f, 0.0f, -3.9f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 2950.0f;
                                Weather.SkyFogDistance_Noon    = 4700.0f;
                                Weather.SkyFogDistance_Sunset  = 3600.0f;
                                Weather.SkyFog_Sunrise  = new Color(117, 108, 105, 255);
                                Weather.SkyFog_Noon     = new Color(116,  111, 113, 255);
                                Weather.SkyFog_Sunset   = new Color(112, 105, 105, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 500.0f;
                                Weather.SceneryFogDistance_Noon    = 140.0f;
                                Weather.SceneryFogDistance_Sunset  = 500.0f;
                                Weather.SceneryFog_Sunrise  = new Color(99, 92, 92, 255);
                                Weather.SceneryFog_Noon     = new Color(140, 139, 140, 255);
                                Weather.SceneryFog_Sunset   = new Color(115, 107, 105, 205);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.0f;
                                Weather.SunSize_Sunset  = 1.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.5f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.7f;
                                Weather.TerrainBrightnessModifier  = 0.77f;
                                Weather.TerrainContrastModifier    = 0.98f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Snow:
                
                            Weatherfilepath = "Weather/Saves/WeatherType_AutumnSnow.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.195f;
                                Weather.OvercastFactor2 = 0.20f; 
                                Weather.OvercastFactor3 = 0.20f; 
                                // -----------------------------------------------
                                Weather.ParticleSize1 = 1.8f;
                                Weather.ParticleSize2 = 1.5f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 2.0f, 0.0f, 4.4f);
                                Weather.PrecipWind2 = new Vector3( 8.0f, 0.0f, 8.8f);
                                // --------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 500.0f;
                                Weather.SkyFogDistance_Noon    = 500.0f;
                                Weather.SkyFogDistance_Sunset  = 500.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(183, 182, 182, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 200.0f;
                                Weather.SceneryFogDistance_Noon    = 350.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 176, 174, 255);
                                Weather.SceneryFog_Noon     = new Color(180, 176, 174, 255);
                                Weather.SceneryFog_Sunset   = new Color(180, 176, 174, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 2.0f;
                                Weather.SunSize_Noon    = 2.0f;
                                Weather.SunSize_Sunset  = 2.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.6f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.6f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Few:

                            Weatherfilepath = "Weather/Saves/WeatherType_AutumnFew.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.10f; 
                                Weather.OvercastFactor2 = 0.16f; 
                                Weather.OvercastFactor3 = 0.18f;
                                // -----------------------------------------------                        
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 6600.0f;
                                Weather.SkyFogDistance_Noon    = 4900.0f;
                                Weather.SkyFogDistance_Sunset  = 5000.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(128, 164, 227, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 680.0f;
                                Weather.SceneryFogDistance_Noon    = 1000.0f;
                                Weather.SceneryFogDistance_Sunset  = 720.0f;
                                Weather.SceneryFog_Sunrise  = new Color(170, 172, 170, 255);
                                Weather.SceneryFog_Noon     = new Color(128, 164, 227, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.1f;
                                Weather.SunSize_Noon    = 2.0f;
                                Weather.SunSize_Sunset  = 1.5f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------                        
                                Weather.TerrainDesatuationModifier = 1.0f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        

                        break;

                        case WeatherType.Cloudy:

                            Weatherfilepath = "Weather/Saves/WeatherType_AutumnCloudy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = -1.0f;
                                Weather.WindDirectionSky = 0.55f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.05f; 
                                Weather.OvercastFactor2 = 0.08f; 
                                Weather.OvercastFactor3 = 0.15f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 5300.0f;
                                Weather.SkyFogDistance_Noon    = 4480.0f;
                                Weather.SkyFogDistance_Sunset  = 3640.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(188, 177, 180, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 870.0f;
                                Weather.SceneryFogDistance_Noon    = 1070.0f;
                                Weather.SceneryFogDistance_Sunset  = 890.0f;
                                Weather.SceneryFog_Sunrise  = new Color(174, 175, 173, 255);
                                Weather.SceneryFog_Noon     = new Color(207, 191, 194, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.67f;
                                Weather.SunSize_Noon    = 1.7f;
                                Weather.SunSize_Sunset  = 2.2f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------                        
                                SaveWeatherType( WeatherFileOut );
                            }

                        break;

                        case WeatherType.Desert:

                            Weatherfilepath = "Weather/Saves/WeatherType_AutumnDesert.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.3f;
                                Weather.WindDirectionSky = 2.40f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.07f; 
                                Weather.OvercastFactor2 = 0.18f; 
                                Weather.OvercastFactor3 = 0.14f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 5300.0f;
                                Weather.SkyFogDistance_Noon    = 4480.0f;
                                Weather.SkyFogDistance_Sunset  = 3640.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(188, 177, 180, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 100.0f;
                                Weather.SceneryFogDistance_Noon    = 1070.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(229, 194, 152, 255);
                                Weather.SceneryFog_Noon     = new Color(207, 191, 194, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.67f;
                                Weather.SunSize_Noon    = 1.7f;
                                Weather.SunSize_Sunset  = 2.2f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.SnowStorm:

                            Weatherfilepath = "Weather/Saves/WeatherType_AutumnSnowStorm.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.14f;
                                Weather.WindDirectionSky = 0.0f;

                                Weather.OvercastFactor  = 0.10f;
                                Weather.OvercastFactor2 = 0.14f; 
                                Weather.OvercastFactor3 = 0.10f; 
                                // -----------------------------------------------
                                Weather.ParticleSize1  = 1.7f;
                                Weather.ParticleSize2  = 2.0f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 7.5f, 0.4f, 10.3f);
                                Weather.PrecipWind2 = new Vector3( 1.7f, 0.0f, 20.6f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 6650.0f;
                                Weather.SkyFogDistance_Noon    = 6400.0f;
                                Weather.SkyFogDistance_Sunset  = 1960.0f;
                                Weather.SkyFog_Sunrise  = new Color(164, 161, 163, 255);
                                Weather.SkyFog_Noon     = new Color(167, 167, 170, 255);
                                Weather.SkyFog_Sunset   = new Color(112, 105, 105, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 320.0f;
                                Weather.SceneryFogDistance_Noon    = 280.0f;
                                Weather.SceneryFogDistance_Sunset  = 500.0f;
                                Weather.SceneryFog_Sunrise  = new Color(115, 108, 106, 255);
                                Weather.SceneryFog_Noon     = new Color(161, 161, 160, 255);
                                Weather.SceneryFog_Sunset   = new Color(105, 106, 100, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.0f;
                                Weather.SunSize_Sunset  = 1.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.6f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.5f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Foggy:

                            Weatherfilepath = "Weather/Saves/WeatherType_AutumnFoggy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.00f;
                                Weather.OvercastFactor2 = 0.031f; 
                                Weather.OvercastFactor3 = 0.081f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 3250.0f;
                                Weather.SkyFogDistance_Noon    = 3600.0f;
                                Weather.SkyFogDistance_Sunset  = 2800.0f;
                                Weather.SkyFog_Sunrise  = new Color(203, 204, 219, 255);
                                Weather.SkyFog_Noon     = new Color(176, 196, 206, 255);
                                Weather.SkyFog_Sunset   = new Color(233, 237, 245, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 100.0f;
                                Weather.SceneryFogDistance_Noon    = 140.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 181, 185, 255);
                                Weather.SceneryFog_Noon     = new Color(177, 199, 209, 255);
                                Weather.SceneryFog_Sunset   = new Color(178, 181, 184, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 0.9f;
                                Weather.SunSize_Sunset  = 0.8f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.5f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;
                    
                        case WeatherType.PartlyCloudy:
                     
                            Weatherfilepath = "Weather/Saves/WeatherType_AutumnPartlyCloudy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.00f;
                                Weather.OvercastFactor2 = 0.00f; 
                                Weather.OvercastFactor3 = 0.01f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 15000.0f;
                                Weather.SkyFogDistance_Noon    = 15000.0f;
                                Weather.SkyFogDistance_Sunset  = 15000.0f;
                                Weather.SkyFog_Sunrise  = new Color(203, 204, 219, 255);
                                Weather.SkyFog_Noon     = new Color(176, 196, 206, 255);
                                Weather.SkyFog_Sunset   = new Color(233, 237, 245, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 2530.0f;
                                Weather.SceneryFogDistance_Noon    = 2770.0f;
                                Weather.SceneryFogDistance_Sunset  = 3030.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 181, 185, 255);
                                Weather.SceneryFog_Noon     = new Color(155, 174, 200, 255);
                                Weather.SceneryFog_Sunset   = new Color(178, 181, 184, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 0.65f;
                                Weather.SunSize_Noon    = 0.9f;
                                Weather.SunSize_Sunset  = 0.8f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.81f;
                                Weather.VegetationBrightnessModifier  = 0.84f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.9f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;



                    }
                    break;
                }

                case SeasonType.Winter: 
                    
                    switch (Viewer.Simulator.WeatherType)
                    {
                        case WeatherType.Clear:
                            Weatherfilepath = "Weather/Saves/WeatherType_WinterClear.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   // HUSK DEN LOADER FRA FIL !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                // --------------------------------------------                       
                                Weather.WindSpeed = 12.0f;
                                Weather.WindDirectionSky = 1.0f;
                                // --------------------------------------------  
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // --------------------------------------------
                                Weather.OvercastFactor  = 0.1f;
                                Weather.OvercastFactor2 = 0.0f; 
                                Weather.OvercastFactor3 = 0.0f;
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.3f;
                                Weather.SunSize_Sunset  = 1.5f;
                                // --------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 3940.0f;
                                Weather.SkyFogDistance_Noon    = 4780.0f;
                                Weather.SkyFogDistance_Sunset  = 3420.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(142, 172, 215, 255);
                                Weather.SkyFog_Sunset   = new Color(195, 163, 128, 255);
                                // --------------------------------------------
                                Weather.SceneryFogDistance_Sunrise =  710.0f;
                                Weather.SceneryFogDistance_Noon    =  1440.0f;
                                Weather.SceneryFogDistance_Sunset  =  830.0f;
                                Weather.SceneryFog_Sunrise  = new Color(185, 182, 196, 255);
                                Weather.SceneryFog_Noon     = new Color(149, 176, 218, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 1.0f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 1.0f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut ); 
                            }
                        break;

                        case WeatherType.Rain:

                            Weatherfilepath = "Weather/Saves/WeatherType_WinterRain.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {  
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 2.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.53f;
                                Weather.OvercastFactor2 = 0.28f; 
                                Weather.OvercastFactor3 = 0.56f;
                                // -----------------------------------------------                
                                Weather.ParticleSize1 = 0.88f;
                                Weather.ParticleSize2 = 0.88f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( -7.0f, 0.0f, -14.8f);
                                Weather.PrecipWind2 = new Vector3( -12.0f, 0.0f, -3.9f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 2950.0f;
                                Weather.SkyFogDistance_Noon    = 4700.0f;
                                Weather.SkyFogDistance_Sunset  = 3600.0f;
                                Weather.SkyFog_Sunrise  = new Color(117, 108, 105, 255);
                                Weather.SkyFog_Noon     = new Color(116,  111, 113, 255);
                                Weather.SkyFog_Sunset   = new Color(112, 105, 105, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 500.0f;
                                Weather.SceneryFogDistance_Noon    = 140.0f;
                                Weather.SceneryFogDistance_Sunset  = 500.0f;
                                Weather.SceneryFog_Sunrise  = new Color(99, 92, 92, 255);
                                Weather.SceneryFog_Noon     = new Color(140, 139, 140, 255);
                                Weather.SceneryFog_Sunset   = new Color(115, 107, 105, 205);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.0f;
                                Weather.SunSize_Sunset  = 1.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.5f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.7f;
                                Weather.TerrainBrightnessModifier  = 0.77f;
                                Weather.TerrainContrastModifier    = 0.98f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Snow:
                
                            Weatherfilepath = "Weather/Saves/WeatherType_WinterSnow.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.195f;
                                Weather.OvercastFactor2 = 0.20f; 
                                Weather.OvercastFactor3 = 0.20f; 
                                // -----------------------------------------------
                                Weather.ParticleSize1 = 1.8f;
                                Weather.ParticleSize2 = 1.5f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 2.0f, 0.0f, 4.4f);
                                Weather.PrecipWind2 = new Vector3( 8.0f, 0.0f, 8.8f);
                                // --------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 500.0f;
                                Weather.SkyFogDistance_Noon    = 500.0f;
                                Weather.SkyFogDistance_Sunset  = 500.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(183, 182, 182, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 200.0f;
                                Weather.SceneryFogDistance_Noon    = 350.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 176, 174, 255);
                                Weather.SceneryFog_Noon     = new Color(180, 176, 174, 255);
                                Weather.SceneryFog_Sunset   = new Color(180, 176, 174, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 2.0f;
                                Weather.SunSize_Noon    = 2.0f;
                                Weather.SunSize_Sunset  = 2.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.6f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.6f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Few:

                            Weatherfilepath = "Weather/Saves/WeatherType_WinterFew.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.10f; 
                                Weather.OvercastFactor2 = 0.16f; 
                                Weather.OvercastFactor3 = 0.18f;
                                // -----------------------------------------------                        
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 6600.0f;
                                Weather.SkyFogDistance_Noon    = 4900.0f;
                                Weather.SkyFogDistance_Sunset  = 5000.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(128, 164, 227, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 680.0f;
                                Weather.SceneryFogDistance_Noon    = 1000.0f;
                                Weather.SceneryFogDistance_Sunset  = 720.0f;
                                Weather.SceneryFog_Sunrise  = new Color(170, 172, 170, 255);
                                Weather.SceneryFog_Noon     = new Color(128, 164, 227, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.1f;
                                Weather.SunSize_Noon    = 2.0f;
                                Weather.SunSize_Sunset  = 1.5f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------                        
                                Weather.TerrainDesatuationModifier = 1.0f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        

                        break;

                        case WeatherType.Cloudy:

                            Weatherfilepath = "Weather/Saves/WeatherType_WinterCloudy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = -1.0f;
                                Weather.WindDirectionSky = 0.55f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.05f; 
                                Weather.OvercastFactor2 = 0.08f; 
                                Weather.OvercastFactor3 = 0.15f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 5300.0f;
                                Weather.SkyFogDistance_Noon    = 4480.0f;
                                Weather.SkyFogDistance_Sunset  = 3640.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(188, 177, 180, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 870.0f;
                                Weather.SceneryFogDistance_Noon    = 1070.0f;
                                Weather.SceneryFogDistance_Sunset  = 890.0f;
                                Weather.SceneryFog_Sunrise  = new Color(174, 175, 173, 255);
                                Weather.SceneryFog_Noon     = new Color(207, 191, 194, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.67f;
                                Weather.SunSize_Noon    = 1.7f;
                                Weather.SunSize_Sunset  = 2.2f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------                        
                                SaveWeatherType( WeatherFileOut );
                            }

                        break;

                        case WeatherType.Desert:

                            Weatherfilepath = "Weather/Saves/WeatherType_WinterDesert.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.3f;
                                Weather.WindDirectionSky = 2.40f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.07f; 
                                Weather.OvercastFactor2 = 0.18f; 
                                Weather.OvercastFactor3 = 0.14f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 5300.0f;
                                Weather.SkyFogDistance_Noon    = 4480.0f;
                                Weather.SkyFogDistance_Sunset  = 3640.0f;
                                Weather.SkyFog_Sunrise  = new Color(187, 187, 196, 255);
                                Weather.SkyFog_Noon     = new Color(188, 177, 180, 255);
                                Weather.SkyFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 100.0f;
                                Weather.SceneryFogDistance_Noon    = 1070.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(229, 194, 152, 255);
                                Weather.SceneryFog_Noon     = new Color(207, 191, 194, 255);
                                Weather.SceneryFog_Sunset   = new Color(200, 164, 128, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.67f;
                                Weather.SunSize_Noon    = 1.7f;
                                Weather.SunSize_Sunset  = 2.2f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.7f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.SnowStorm:

                            Weatherfilepath = "Weather/Saves/WeatherType_WinterSnowStorm.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 1.14f;
                                Weather.WindDirectionSky = 0.0f;

                                Weather.OvercastFactor  = 0.10f;
                                Weather.OvercastFactor2 = 0.14f; 
                                Weather.OvercastFactor3 = 0.10f; 
                                // -----------------------------------------------
                                Weather.ParticleSize1  = 1.7f;
                                Weather.ParticleSize2  = 2.0f;
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 7.5f, 0.4f, 10.3f);
                                Weather.PrecipWind2 = new Vector3( 1.7f, 0.0f, 20.6f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 6650.0f;
                                Weather.SkyFogDistance_Noon    = 6400.0f;
                                Weather.SkyFogDistance_Sunset  = 1960.0f;
                                Weather.SkyFog_Sunrise  = new Color(164, 161, 163, 255);
                                Weather.SkyFog_Noon     = new Color(167, 167, 170, 255);
                                Weather.SkyFog_Sunset   = new Color(112, 105, 105, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 320.0f;
                                Weather.SceneryFogDistance_Noon    = 280.0f;
                                Weather.SceneryFogDistance_Sunset  = 500.0f;
                                Weather.SceneryFog_Sunrise  = new Color(115, 108, 106, 255);
                                Weather.SceneryFog_Noon     = new Color(161, 161, 160, 255);
                                Weather.SceneryFog_Sunset   = new Color(105, 106, 100, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 1.0f;
                                Weather.SunSize_Sunset  = 1.0f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.6f;
                                Weather.VegetationBrightnessModifier  = 0.7f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.5f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                        case WeatherType.Foggy:

                            Weatherfilepath = "Weather/Saves/WeatherType_WinterFoggy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.00f;
                                Weather.OvercastFactor2 = 0.031f; 
                                Weather.OvercastFactor3 = 0.081f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 3250.0f;
                                Weather.SkyFogDistance_Noon    = 3600.0f;
                                Weather.SkyFogDistance_Sunset  = 2800.0f;
                                Weather.SkyFog_Sunrise  = new Color(203, 204, 219, 255);
                                Weather.SkyFog_Noon     = new Color(176, 196, 206, 255);
                                Weather.SkyFog_Sunset   = new Color(233, 237, 245, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 100.0f;
                                Weather.SceneryFogDistance_Noon    = 140.0f;
                                Weather.SceneryFogDistance_Sunset  = 400.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 181, 185, 255);
                                Weather.SceneryFog_Noon     = new Color(177, 199, 209, 255);
                                Weather.SceneryFog_Sunset   = new Color(178, 181, 184, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 1.0f;
                                Weather.SunSize_Noon    = 0.9f;
                                Weather.SunSize_Sunset  = 0.8f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.5f;
                                Weather.VegetationBrightnessModifier  = 1.0f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.8f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;
                        
                        case WeatherType.PartlyCloudy:
                     
                            Weatherfilepath = "Weather/Saves/WeatherType_WinterPartlyCloudy.bin";
                            fileInfo = new FileInfo( Path.Combine(Viewer.ContentPath, Weatherfilepath));
                    
                            if (fileInfo.Exists && fileInfo.Length >=64) 
                            {   
                                Console.WriteLine("\n" + Weatherfilepath + " exists ##");
                                WeatherFileIn = new BinaryReader(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Read));
                                Console.WriteLine("\n" + Weatherfilepath + " Open ##");
                                LoadWeatherType( WeatherFileIn );
                                Console.WriteLine("\n" + Weatherfilepath + " Loaded ##");
                                WeatherFileIn.Close();
                                Console.WriteLine("\n" + Weatherfilepath + " Closed ##");
                            } 
                            else     
                            {   
                                WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath ), FileMode.Create, FileAccess.Write));
                                Console.WriteLine("\n" + Weatherfilepath + " Created ##");
                                Weather.WindSpeed = 3.0f;
                                Weather.WindDirectionSky = 3.0f;
                                // -----------------------------------------------
                                Weather.OvercastFactor  = 0.00f;
                                Weather.OvercastFactor2 = 0.00f; 
                                Weather.OvercastFactor3 = 0.01f; 
                                // -----------------------------------------------
                                Weather.PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
                                Weather.PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
                                // -----------------------------------------------
                                Weather.SkyFogDistance_Sunrise = 15000.0f;
                                Weather.SkyFogDistance_Noon    = 15000.0f;
                                Weather.SkyFogDistance_Sunset  = 15000.0f;
                                Weather.SkyFog_Sunrise  = new Color(203, 204, 219, 255);
                                Weather.SkyFog_Noon     = new Color(176, 196, 206, 255);
                                Weather.SkyFog_Sunset   = new Color(233, 237, 245, 255);
                                // -----------------------------------------------
                                Weather.SceneryFogDistance_Sunrise = 2530.0f;
                                Weather.SceneryFogDistance_Noon    = 2770.0f;
                                Weather.SceneryFogDistance_Sunset  = 3030.0f;
                                Weather.SceneryFog_Sunrise  = new Color(180, 181, 185, 255);
                                Weather.SceneryFog_Noon     = new Color(155, 174, 200, 255);
                                Weather.SceneryFog_Sunset   = new Color(178, 181, 184, 255);
                                // -----------------------------------------------
                                Weather.SunSize_Sunrise = 0.65f;
                                Weather.SunSize_Noon    = 0.9f;
                                Weather.SunSize_Sunset  = 0.8f;
                                // -----------------------------------------------
                                Weather.VegetationDesatuationModifier = 0.81f;
                                Weather.VegetationBrightnessModifier  = 0.84f;
                                Weather.VegetationContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                Weather.TerrainDesatuationModifier = 0.9f;
                                Weather.TerrainBrightnessModifier  = 1.0f;
                                Weather.TerrainContrastModifier    = 1.0f;
                                // -----------------------------------------------
                                SaveWeatherType( WeatherFileOut );
                            }
                        break;

                    
                    }
                break;
            }
         
        }

        public void UpdateWeatherParameters()
        {
            Viewer.SoundProcess.RemoveSoundSources(this);
            switch (Viewer.Simulator.WeatherType)
            {
                case WeatherType.Clear:
                        Weather.PrecipitationLiquidity = 1;
                        Weather.PricipitationIntensityPPSPM2 = 0;
                        Viewer.SoundProcess.AddSoundSources(this, ClearSound);
                break;
                
                case WeatherType.Rain: 
                        Weather.PrecipitationLiquidity = 1;
                        Weather.PricipitationIntensityPPSPM2 = 0.25f;
                        Viewer.SoundProcess.AddSoundSources(this, RainSound);
                break;
                
                case WeatherType.Snow: 
                        Weather.PrecipitationLiquidity = 0;
                        Weather.PricipitationIntensityPPSPM2 = 0.05f;   
                        Viewer.SoundProcess.AddSoundSources(this, SnowSound); 
                break;
                
                case WeatherType.Few:
                        Weather.PrecipitationLiquidity = 1; 
                        Weather.PricipitationIntensityPPSPM2 = 0; 
                        Viewer.SoundProcess.AddSoundSources(this, FewSound);
                break;
                
                case WeatherType.Cloudy: 
                        Weather.PrecipitationLiquidity = 1; 
                        Weather.PricipitationIntensityPPSPM2 = 0;
                        Viewer.SoundProcess.AddSoundSources(this, CloudySound);
                break;
                
                case WeatherType.Desert: 
                        Weather.PrecipitationLiquidity = 1; 
                        Weather.PricipitationIntensityPPSPM2 = 0; 
                        Viewer.SoundProcess.AddSoundSources(this, DesertSound);
                break;
                
                case WeatherType.SnowStorm: 
                        Viewer.SoundProcess.AddSoundSources(this, SnowStormSound);
                        Weather.PrecipitationLiquidity = 0;
                        Weather.PricipitationIntensityPPSPM2 = 0.25f;
                break;

                case WeatherType.Foggy:
                        Weather.PrecipitationLiquidity = 0.0f;
                        Weather.PricipitationIntensityPPSPM2 = 0.0f;
                        Viewer.SoundProcess.AddSoundSources(this, FoggySound);
                break;

                case WeatherType.PartlyCloudy:
                        Weather.PrecipitationLiquidity = 0.0f;
                        Weather.PricipitationIntensityPPSPM2 = 0.0f;
                        Viewer.SoundProcess.AddSoundSources(this, PartlyCloudySound);
                break;
 
                default: break;
            }

        }

        void UpdateSoundSources()
        {
            Viewer.SoundProcess.RemoveSoundSources(this);
            switch (Viewer.Simulator.WeatherType)
            {
                case WeatherType.Clear:     Viewer.SoundProcess.AddSoundSources(this, ClearSound); break;
                case WeatherType.Rain:      Viewer.SoundProcess.AddSoundSources(this, RainSound); break;
                case WeatherType.Snow:      Viewer.SoundProcess.AddSoundSources(this, SnowSound); break;
                case WeatherType.Few:       Viewer.SoundProcess.AddSoundSources(this, FewSound); break;
                case WeatherType.Cloudy:    Viewer.SoundProcess.AddSoundSources(this, CloudySound); break;
                case WeatherType.Desert:    Viewer.SoundProcess.AddSoundSources(this, DesertSound); break;
                case WeatherType.SnowStorm: Viewer.SoundProcess.AddSoundSources(this, SnowStormSound); break;
                case WeatherType.Foggy:     Viewer.SoundProcess.AddSoundSources(this, FoggySound); break;
                case WeatherType.PartlyCloudy: Viewer.SoundProcess.AddSoundSources(this, PartlyCloudySound); break;
                default: break;
            }

        }

        void UpdateVolume()
        {
             foreach (var soundSource in RainSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
             foreach (var soundSource in SnowSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
             foreach (var soundSource in SnowStormSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
        }
        
        private void UpdateWind(ElapsedTime elapsedTime)
        {
            WindUpdateTimer += elapsedTime.ClockSeconds;

            if (WindUpdateTimer > WindGustUpdateTimeS)
            {
                WindSpeedInternalMpS = Vector2.Zero;
                for (var i = 0; i < windSpeedMpS.Length; i++)
                {
                    windSpeedMpS[i].X += (((float)Viewer.Random.NextDouble() * 2) - 1) * WindChangeMpSS[i] * WindUpdateTimer;
                    windSpeedMpS[i].Y += (((float)Viewer.Random.NextDouble() * 2) - 1) * WindChangeMpSS[i] * WindUpdateTimer;

                    var windMagnitude = windSpeedMpS[i].Length() / (i == 0 ? Weather.WindSpeedMpS.Length() * 0.4f : WindSpeedMaxMpS);

                    if (windMagnitude > 1) windSpeedMpS[i] /= windMagnitude;

                    WindSpeedInternalMpS += windSpeedMpS[i];
                }

                var TotalwindMagnitude = WindSpeedInternalMpS.Length() / WindSpeedMaxMpS;

                if (TotalwindMagnitude > 1)
                    WindSpeedInternalMpS /= TotalwindMagnitude;

                Weather.WindSpeedMpS = WindSpeedInternalMpS;
                WindUpdateTimer = 0.0f; // Reset wind gust timer

                if (InitialWind) // Record the initial wind direction.
                {
                    BaseWindDirectionRad = (float)Math.Atan2(Weather.WindSpeedMpS.X, Weather.WindSpeedMpS.Y);
                    InitialWind = false; // set false so that base wind is not changed
                }

                calculatedWindDirection = (float)Math.Atan2(Weather.WindSpeedMpS.X, Weather.WindSpeedMpS.Y);

                // Test to ensure wind direction stays within the direction bandwidth set, if out of bounds set new random direction
                if (calculatedWindDirection > (BaseWindDirectionRad + WindDirectionVariationRad))
                    calculatedWindDirection = BaseWindDirectionRad + (WindDirectionVariationRad * (float)Viewer.Random.NextDouble());

                if (calculatedWindDirection < (BaseWindDirectionRad - WindDirectionVariationRad))
                    calculatedWindDirection = BaseWindDirectionRad - (WindDirectionVariationRad * (float)Viewer.Random.NextDouble());

                Weather.CalculatedWindDirection = calculatedWindDirection;
            }
        }

        private bool RandomizeInitialWeather()
        {
            CheckDesertZone();
            if (DesertZone) return false;
            // First define overcast
            var randValue = Viewer.Random.Next(170);
            var intermValue = randValue >= 50 ? (float)(randValue - 50f) : randValue;
            Weather.OvercastFactor = intermValue >= 20 ? (float)(intermValue - 20f) / 100f : (float)intermValue / 100f; // give more probability to less overcast
            Viewer.Simulator.WeatherType = WeatherType.Clear;
            // Then check if we are in precipitation zone
            if (Weather.OvercastFactor > 0.5)
            {
                randValue = Viewer.Random.Next(75);
                if (randValue > 40)
                {
                    Weather.PricipitationIntensityPPSPM2 = (float)(randValue - 40f) / 1000f;
                    if (Viewer.GraphicsDevice.GraphicsProfile != GraphicsProfile.HiDef)
                        Weather.PricipitationIntensityPPSPM2 = Math.Min(Weather.PricipitationIntensityPPSPM2, 0.010f);
                    
                    if (Viewer.Simulator.Season == SeasonType.Winter) {
                        Viewer.Simulator.WeatherType = WeatherType.Snow;
                        Weather.PrecipitationLiquidity = 0;
                    }
                    else {
                        Viewer.Simulator.WeatherType = WeatherType.Rain;
                        Weather.PrecipitationLiquidity = 1;
                    }
                }
                else Weather.PricipitationIntensityPPSPM2 = 0;
            }
            else Weather.PricipitationIntensityPPSPM2 = 0;
            // And now define visibility
            randValue = Viewer.Random.Next(2000);
            if (Weather.PricipitationIntensityPPSPM2 > 0 || Weather.OvercastFactor > 0.7f)
                // Use first digit to define power of ten and the other three to define the multiplying number
                Weather.SceneryFogDistance_Mix = Math.Max(100, (float)Math.Pow(10, randValue / 1000 + 2) * (float)(((randValue % 1000) + 1) / 100f));
            else
                Weather.SceneryFogDistance_Mix = Math.Max(500, (float)Math.Pow(10, (randValue / 1000) + 3) * (float)(((randValue % 1000) + 1) / 100f));
            return true;
        }


        // This class will eventually be expanded to interpret dynamic weather scripts and
        // make game-time weather transitions.

 
        private void CheckDesertZone()
        {
            // Compute player train lat/lon in degrees 
            double latitude = 0;
            double longitude = 0;
            var location = Viewer.PlayerLocomotive.Train.FrontTDBTraveller;
            new Orts.Common.WorldLatLon().ConvertWTC(location.TileX, location.TileZ, location.Location, ref latitude, ref longitude);
            float LatitudeDeg = MathHelper.ToDegrees((float)latitude);
            float LongitudeDeg = MathHelper.ToDegrees((float)longitude);

            // Compare player train lat/lon with array of desert zones
            for (int i = 0; i < DesertZones.Length / 4; i++)
            {
                if (LatitudeDeg > DesertZones[i, 0] && LatitudeDeg < DesertZones[i, 1] && LongitudeDeg > DesertZones[i, 2] && LongitudeDeg < DesertZones[i, 3]
                     && Viewer.PlayerLocomotive.Train.FrontTDBTraveller.Location.Y < 1000 ||
                     LatitudeDeg > DesertZones[i, 0] + 1 && LatitudeDeg < DesertZones[i, 1] - 1 && LongitudeDeg > DesertZones[i, 2] + 1 && LongitudeDeg < DesertZones[i, 3] - 1)
                {
                    DesertZone = true;
                    return;
                }
            }
        }

                
        // <----------------------------- ExRail ------------------------------>
        // Update labels stats
        public void GuiUpdateStats()
        {
            Viewer.WeatherEditorWindow.ScFCSunrise_R.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.R.ToString();
            Viewer.WeatherEditorWindow.ScFCSunrise_G.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.G.ToString();
            Viewer.WeatherEditorWindow.ScFCSunrise_B.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.B.ToString();

            Viewer.WeatherEditorWindow.ScFCNoon_R.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Noon.R.ToString();
            Viewer.WeatherEditorWindow.ScFCNoon_G.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Noon.G.ToString();
            Viewer.WeatherEditorWindow.ScFCNoon_B.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Noon.B.ToString();

            Viewer.WeatherEditorWindow.ScFCSunset_R.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.R.ToString();
            Viewer.WeatherEditorWindow.ScFCSunset_G.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.G.ToString();
            Viewer.WeatherEditorWindow.ScFCSunset_B.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.B.ToString();

            Viewer.WeatherEditorWindow.SkyFCSunrise_R.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.R.ToString();
            Viewer.WeatherEditorWindow.SkyFCSunrise_G.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.G.ToString();
            Viewer.WeatherEditorWindow.SkyFCSunrise_B.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.B.ToString();

            Viewer.WeatherEditorWindow.SkyFCNoon_R.Text = Viewer.World.WeatherControl.Weather.SkyFog_Noon.R.ToString();
            Viewer.WeatherEditorWindow.SkyFCNoon_G.Text = Viewer.World.WeatherControl.Weather.SkyFog_Noon.G.ToString();
            Viewer.WeatherEditorWindow.SkyFCNoon_B.Text = Viewer.World.WeatherControl.Weather.SkyFog_Noon.B.ToString();

            Viewer.WeatherEditorWindow.SkyFCSunset_R.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunset.R.ToString();
            Viewer.WeatherEditorWindow.SkyFCSunset_G.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunset.G.ToString();
            Viewer.WeatherEditorWindow.SkyFCSunset_B.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunset.B.ToString();

            Viewer.WeatherEditorWindow.WindSpeed.Text = Viewer.World.WeatherControl.Weather.WindSpeed.ToString("0.00");
            Viewer.WeatherEditorWindow.WindDirSky.Text = Viewer.World.WeatherControl.Weather.WindDirectionSky.ToString("0.00");

            Viewer.WeatherEditorWindow.PrecipWind1X.Text = Viewer.World.WeatherControl.Weather.PrecipWind1.X.ToString("0.00");
            Viewer.WeatherEditorWindow.PrecipWind1Y.Text = Viewer.World.WeatherControl.Weather.PrecipWind1.Y.ToString("0.00");
            Viewer.WeatherEditorWindow.PrecipWind1Z.Text = Viewer.World.WeatherControl.Weather.PrecipWind1.Z.ToString("0.00");
            Viewer.WeatherEditorWindow.PrecipWind2X.Text = Viewer.World.WeatherControl.Weather.PrecipWind2.X.ToString("0.00");
            Viewer.WeatherEditorWindow.PrecipWind2Y.Text = Viewer.World.WeatherControl.Weather.PrecipWind2.Y.ToString("0.00");
            Viewer.WeatherEditorWindow.PrecipWind2Z.Text = Viewer.World.WeatherControl.Weather.PrecipWind2.Z.ToString("0.00");

            Viewer.WeatherEditorWindow.PrecipLiquid.Text = Viewer.World.WeatherControl.Weather.PrecipitationLiquidity.ToString("0.00");
            Viewer.WeatherEditorWindow.PricipIntPPSPM2.Text = Viewer.World.WeatherControl.Weather.PricipitationIntensityPPSPM2.ToString("0.00");
            
            Viewer.WeatherEditorWindow.PrecipParSize1.Text = Viewer.World.WeatherControl.Weather.ParticleSize1.ToString("0.00");
            //Viewer.WeatherEditorWindow.PrecipParSize2.Text = Viewer.World.WeatherControl.Weather.ParticleSize2.ToString("0.00");

            Viewer.WeatherEditorWindow.Overcast1.Text = Viewer.World.WeatherControl.Weather.OvercastFactor.ToString("0.00");
            Viewer.WeatherEditorWindow.Overcast2.Text = Viewer.World.WeatherControl.Weather.OvercastFactor2.ToString("0.00");
            Viewer.WeatherEditorWindow.Overcast3.Text = Viewer.World.WeatherControl.Weather.OvercastFactor3.ToString("0.00");
        
            Viewer.WeatherEditorWindow.ScVDSunrise.Text = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunrise.ToString("0.00");
            Viewer.WeatherEditorWindow.ScVDNoon.Text    = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Noon.ToString("0.00");
            Viewer.WeatherEditorWindow.ScVDSunset.Text  = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunset.ToString("0.00");
            Viewer.WeatherEditorWindow.ScVDPrecentMix.Text = Viewer.MaterialManager.SceneryFogDistanceMix.MixOut.ToString("0.00");

            Viewer.WeatherEditorWindow.SkyVDSunrise.Text = Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunrise.ToString("0.00");
            Viewer.WeatherEditorWindow.SkyVDNoon.Text    = Viewer.World.WeatherControl.Weather.SkyFogDistance_Noon.ToString("0.00");
            Viewer.WeatherEditorWindow.SkyVDSunset.Text  = Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunset.ToString("0.00");
            Viewer.WeatherEditorWindow.SkyVDPrecentMix.Text = Viewer.MaterialManager.SkyFogDistanceMix.MixOut.ToString("0.00");
            
            Viewer.WeatherEditorWindow.SkySunSizeSunrise.Text = Viewer.World.WeatherControl.Weather.SunSize_Sunrise.ToString("0.00");
            Viewer.WeatherEditorWindow.SkySunSizeNoon.Text    = Viewer.World.WeatherControl.Weather.SunSize_Noon.ToString("0.00");
            Viewer.WeatherEditorWindow.SkySunSizeSunset.Text  = Viewer.World.WeatherControl.Weather.SunSize_Sunset.ToString("0.00");
            Viewer.WeatherEditorWindow.SkySunSizeCurrent.Text = Viewer.World.WeatherControl.Weather.SunSize_Mix.ToString("0.00");

            Viewer.WeatherEditorWindow.VegDesatMod.Text  = Viewer.World.WeatherControl.Weather.VegetationDesatuationModifier.ToString("0.00");
            Viewer.WeatherEditorWindow.VegBrightMod.Text = Viewer.World.WeatherControl.Weather.VegetationBrightnessModifier.ToString("0.00");
            Viewer.WeatherEditorWindow.VegContMod.Text   = Viewer.World.WeatherControl.Weather.VegetationContrastModifier.ToString("0.00");

            Viewer.WeatherEditorWindow.TerrDesatMod.Text  = Viewer.World.WeatherControl.Weather.TerrainDesatuationModifier.ToString("0.00");
            Viewer.WeatherEditorWindow.TerrBrightMod.Text = Viewer.World.WeatherControl.Weather.TerrainBrightnessModifier.ToString("0.00");
            Viewer.WeatherEditorWindow.TerrContMod.Text   = Viewer.World.WeatherControl.Weather.TerrainContrastModifier.ToString("0.00");

            Viewer.WeatherEditorWindow.SkyFCCurrent.Color = Viewer.World.WeatherControl.Weather.SkyFogMix;
            Viewer.WeatherEditorWindow.ScFCCurrent.Color  = Viewer.World.WeatherControl.Weather.SceneryFogMix;

            Color col = new Color(0xFF,0xFA,0x79);
            Viewer.WeatherEditorWindow.WeatherType.Color = col;
            Viewer.WeatherEditorWindow.Season.Color      = col;
            Viewer.WeatherEditorWindow.Time.Color        = col;
            Viewer.WeatherEditorWindow.SunriseTime.Color = col;
            Viewer.WeatherEditorWindow.SunsetTime.Color  = col;
            Viewer.WeatherEditorWindow.CurrentTime.Color = col;

            switch (Viewer.Simulator.WeatherType)
            {   
                case Orts.Formats.Msts.WeatherType.Clear:     Viewer.WeatherEditorWindow.WeatherType.Text = "Clear"; break;
                case Orts.Formats.Msts.WeatherType.Snow:      Viewer.WeatherEditorWindow.WeatherType.Text = "Snow"; break;
                case Orts.Formats.Msts.WeatherType.Rain:      Viewer.WeatherEditorWindow.WeatherType.Text = "Rain"; break;
                case Orts.Formats.Msts.WeatherType.Few:       Viewer.WeatherEditorWindow.WeatherType.Text = "Few"; break;
                case Orts.Formats.Msts.WeatherType.Cloudy:    Viewer.WeatherEditorWindow.WeatherType.Text = "Cloudy"; break;
                case Orts.Formats.Msts.WeatherType.Desert:    Viewer.WeatherEditorWindow.WeatherType.Text = "Desert"; break;
                case Orts.Formats.Msts.WeatherType.SnowStorm: Viewer.WeatherEditorWindow.WeatherType.Text = "Snowstorm"; break;
                case Orts.Formats.Msts.WeatherType.Foggy:     Viewer.WeatherEditorWindow.WeatherType.Text = "Foggy"; break;
                case Orts.Formats.Msts.WeatherType.PartlyCloudy: Viewer.WeatherEditorWindow.WeatherType.Text = "PartlyCloudy"; break;
            }
            switch (Viewer.Simulator.Season)
            {
                case Orts.Formats.Msts.SeasonType.Spring: Viewer.WeatherEditorWindow.Season.Text = "Spring"; break;
                case Orts.Formats.Msts.SeasonType.Summer: Viewer.WeatherEditorWindow.Season.Text = "Summer"; break;
                case Orts.Formats.Msts.SeasonType.Autumn: Viewer.WeatherEditorWindow.Season.Text = "Autumn"; break;
                case Orts.Formats.Msts.SeasonType.Winter: Viewer.WeatherEditorWindow.Season.Text = "Winter"; break;
            }
            

        }



        public void GuiSelection()
        { 
            // Update Time & labels with currrent Sky/Scnenery Fog Color Mix
            Viewer.WeatherEditorWindow.Time.Text          = ORTS.Common.FormatStrings.FormatTime( Viewer.Simulator.ClockTime);
            Viewer.WeatherEditorWindow.SunriseTime.Text   = ORTS.Common.FormatStrings.FormatTime( Viewer.ENVFile.SkySatellites[0].RiseTime);
            Viewer.WeatherEditorWindow.SunsetTime.Text    = ORTS.Common.FormatStrings.FormatTime( Viewer.ENVFile.SkySatellites[0].SetTime);
            Viewer.WeatherEditorWindow.SkyFCCurrent.Color = Viewer.World.WeatherControl.Weather.SkyFogMix;
            Viewer.WeatherEditorWindow.ScFCCurrent.Color  = Viewer.World.WeatherControl.Weather.SceneryFogMix;
            var Extime = Viewer.Simulator.GameTime ;
            Viewer.WeatherEditorWindow.CurrentTime.Text   = ORTS.Common.FormatStrings.FormatTime(Extime);

            switch (Viewer.Simulator.WeatherType)
            {   
                case Orts.Formats.Msts.WeatherType.Clear:     Viewer.WeatherEditorWindow.WeatherType.Text = "Clear"; break;
                case Orts.Formats.Msts.WeatherType.Snow:      Viewer.WeatherEditorWindow.WeatherType.Text = "Snow"; break;
                case Orts.Formats.Msts.WeatherType.Rain:      Viewer.WeatherEditorWindow.WeatherType.Text = "Rain"; break;
                case Orts.Formats.Msts.WeatherType.Few:       Viewer.WeatherEditorWindow.WeatherType.Text = "Few"; break;
                case Orts.Formats.Msts.WeatherType.Cloudy:    Viewer.WeatherEditorWindow.WeatherType.Text = "Cloudy"; break;
                case Orts.Formats.Msts.WeatherType.Desert:    Viewer.WeatherEditorWindow.WeatherType.Text = "Desert"; break;
                case Orts.Formats.Msts.WeatherType.SnowStorm: Viewer.WeatherEditorWindow.WeatherType.Text = "Snowstorm"; break;
                case Orts.Formats.Msts.WeatherType.Foggy:     Viewer.WeatherEditorWindow.WeatherType.Text = "Foggy"; break;
                case Orts.Formats.Msts.WeatherType.PartlyCloudy: Viewer.WeatherEditorWindow.WeatherType.Text = "PartlyCloudy"; break;
            }
            
            // Swich Gui selected item
            switch (Viewer.WeatherEditorWindow.EnmSelect)
            {
                // Scenery Fog Color Sunrise
                case Popups.WeatherEditorWindow.EnuSelection.ScFC_Sunrise_Sel:
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.R += 1;
                        Viewer.WeatherEditorWindow.ScFCSunrise_R.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.R.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.R -= 1;
                        Viewer.WeatherEditorWindow.ScFCSunrise_R.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.R.ToString();
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.G += 1;
                        Viewer.WeatherEditorWindow.ScFCSunrise_G.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.G.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                       Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.G -= 1;
                        Viewer.WeatherEditorWindow.ScFCSunrise_G.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.G.ToString();
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.B += 1;
                        Viewer.WeatherEditorWindow.ScFCSunrise_B.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.B.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.B -= 1;
                        Viewer.WeatherEditorWindow.ScFCSunrise_B.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunrise.B.ToString();
                    }        
                break;
                
                // Scenery Fog Color Noon
                case Popups.WeatherEditorWindow.EnuSelection.ScFC_Noon_Sel:

                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Noon.R += 1;
                        Viewer.WeatherEditorWindow.ScFCNoon_R.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Noon.R.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Noon.R -= 1;
                        Viewer.WeatherEditorWindow.ScFCNoon_R.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Noon.R.ToString();
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Noon.G += 1;
                        Viewer.WeatherEditorWindow.ScFCNoon_G.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Noon.G.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Noon.G -= 1;
                        Viewer.WeatherEditorWindow.ScFCNoon_G.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Noon.G.ToString();
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Noon.B += 1;
                        Viewer.WeatherEditorWindow.ScFCNoon_B.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Noon.B.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Noon.B -= 1;
                        Viewer.WeatherEditorWindow.ScFCNoon_B.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Noon.B.ToString();
                    }        
                break;

                // Scenery Fog Color Sunset
                case Popups.WeatherEditorWindow.EnuSelection.ScFC_Sunset_Sel:
                                    
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.R += 1;
                        Viewer.WeatherEditorWindow.ScFCSunset_R.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.R.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.R -= 1;
                        Viewer.WeatherEditorWindow.ScFCSunset_R.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.R.ToString();
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.G += 1;
                        Viewer.WeatherEditorWindow.ScFCSunset_G.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.G.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.G -= 1;
                        Viewer.WeatherEditorWindow.ScFCSunset_G.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.G.ToString();
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.B += 1;
                        Viewer.WeatherEditorWindow.ScFCSunset_B.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.B.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.B -= 1;
                        Viewer.WeatherEditorWindow.ScFCSunset_B.Text = Viewer.World.WeatherControl.Weather.SceneryFog_Sunset.B.ToString();
                    }        
                break;
                 
                // Sky Fog Color Sunrise
                case Popups.WeatherEditorWindow.EnuSelection.SkyFC_Sunrise_Sel:
                    
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.R += 1;
                        Viewer.WeatherEditorWindow.SkyFCSunrise_R.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.R.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.R -= 1;
                        Viewer.WeatherEditorWindow.SkyFCSunrise_R.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.R.ToString();
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.G += 1;
                        Viewer.WeatherEditorWindow.SkyFCSunrise_G.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.G.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                       Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.G -= 1;
                        Viewer.WeatherEditorWindow.SkyFCSunrise_G.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.G.ToString();
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.B += 1;
                        Viewer.WeatherEditorWindow.SkyFCSunrise_B.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.B.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.B -= 1;
                        Viewer.WeatherEditorWindow.SkyFCSunrise_B.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunrise.B.ToString();
                    }        
                break;

                // Sky Fog Color Noon
                case Popups.WeatherEditorWindow.EnuSelection.SkyFC_Noon_Sel:

                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Noon.R += 1;
                        Viewer.WeatherEditorWindow.SkyFCNoon_R.Text = Viewer.World.WeatherControl.Weather.SkyFog_Noon.R.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Noon.R -= 1;
                        Viewer.WeatherEditorWindow.SkyFCNoon_R.Text = Viewer.World.WeatherControl.Weather.SkyFog_Noon.R.ToString();
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Noon.G += 1;
                        Viewer.WeatherEditorWindow.SkyFCNoon_G.Text = Viewer.World.WeatherControl.Weather.SkyFog_Noon.G.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Noon.G -= 1;
                        Viewer.WeatherEditorWindow.SkyFCNoon_G.Text = Viewer.World.WeatherControl.Weather.SkyFog_Noon.G.ToString();
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Noon.B += 1;
                        Viewer.WeatherEditorWindow.SkyFCNoon_B.Text = Viewer.World.WeatherControl.Weather.SkyFog_Noon.B.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Noon.B -= 1;
                        Viewer.WeatherEditorWindow.SkyFCNoon_B.Text = Viewer.World.WeatherControl.Weather.SkyFog_Noon.B.ToString();
                    }        
                break;

                // Sky Fog Color Sunset
                case Popups.WeatherEditorWindow.EnuSelection.SkyFC_Sunset_Sel:
                                    
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunset.R += 1;
                        Viewer.WeatherEditorWindow.SkyFCSunset_R.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunset.R.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunset.R -= 1;
                        Viewer.WeatherEditorWindow.SkyFCSunset_R.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunset.R.ToString();
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunset.G += 1;
                        Viewer.WeatherEditorWindow.SkyFCSunset_G.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunset.G.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunset.G -= 1;
                        Viewer.WeatherEditorWindow.SkyFCSunset_G.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunset.G.ToString();
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunset.B += 1;
                        Viewer.WeatherEditorWindow.SkyFCSunset_B.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunset.B.ToString();
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.SkyFog_Sunset.B -= 1;
                        Viewer.WeatherEditorWindow.SkyFCSunset_B.Text = Viewer.World.WeatherControl.Weather.SkyFog_Sunset.B.ToString();
                    } 
                    Viewer.WeatherEditorWindow.SkyVDPrecentMix.Text = Viewer.World.WeatherControl.Weather.SkyFogDistance_Mix.ToString("0.00");
                break;

                // Scenery View Distance - Sunrise/Noon/sunset
                case Popups.WeatherEditorWindow.EnuSelection.ScViewDistance_Sel:
                                    
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunrise += 10;
                        Viewer.WeatherEditorWindow.ScVDSunrise.Text = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunrise.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunrise -= 10;
                        Viewer.WeatherEditorWindow.ScVDSunrise.Text = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunrise.ToString("0.00");
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.SceneryFogDistance_Noon += 10;
                        Viewer.WeatherEditorWindow.ScVDNoon.Text = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Noon.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.SceneryFogDistance_Noon -= 10;
                        Viewer.WeatherEditorWindow.ScVDNoon.Text = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Noon.ToString("0.00");
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunset += 10;
                        Viewer.WeatherEditorWindow.ScVDSunset.Text = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunset.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunset -= 10;
                        Viewer.WeatherEditorWindow.ScVDSunset.Text = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Sunset.ToString("0.00");
                    }
                    Viewer.WeatherEditorWindow.ScVDPrecentMix.Text = Viewer.World.WeatherControl.Weather.SceneryFogDistance_Mix.ToString("0.00");
                break;

                // Sky View Distance - Sunrise/Noon/sunset
                case Popups.WeatherEditorWindow.EnuSelection.SkyViewDistance_Sel:
           
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunrise += 10;
                        Viewer.WeatherEditorWindow.SkyVDSunrise.Text = Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunrise.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunrise -= 10;
                        Viewer.WeatherEditorWindow.SkyVDSunrise.Text = Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunrise.ToString("0.00");
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.SkyFogDistance_Noon += 10;
                        Viewer.WeatherEditorWindow.SkyVDNoon.Text = Viewer.World.WeatherControl.Weather.SkyFogDistance_Noon.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.SkyFogDistance_Noon -= 10;
                        Viewer.WeatherEditorWindow.SkyVDNoon.Text = Viewer.World.WeatherControl.Weather.SkyFogDistance_Noon.ToString("0.00");
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunset += 10;
                        Viewer.WeatherEditorWindow.SkyVDSunset.Text = Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunset.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunset -= 10;
                        Viewer.WeatherEditorWindow.SkyVDSunset.Text = Viewer.World.WeatherControl.Weather.SkyFogDistance_Sunset.ToString("0.00");
                    } 
                    Viewer.WeatherEditorWindow.SkyVDPrecentMix.Text = Viewer.World.WeatherControl.Weather.SkyFogDistance_Mix.ToString("0.00");
                break;
                
                // Wind speed & Direction
                case Popups.WeatherEditorWindow.EnuSelection.Wind_Sel:
           
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.WindSpeed += .1f;
                        Viewer.WeatherEditorWindow.WindSpeed.Text = Viewer.World.WeatherControl.Weather.WindSpeed.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.WindSpeed -= .1f;
                        Viewer.WeatherEditorWindow.WindSpeed.Text = Viewer.World.WeatherControl.Weather.WindSpeed.ToString("0.00");
                    }        

                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.WindDirectionSky += .01f;
                        Viewer.WeatherEditorWindow.WindDirSky.Text = Viewer.World.WeatherControl.Weather.WindDirectionSky.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.WindDirectionSky -= .01f;
                        Viewer.WeatherEditorWindow.WindDirSky.Text = Viewer.World.WeatherControl.Weather.WindDirectionSky.ToString("0.00");
                    }        
                break;

                // Overcast 1,2,3
                case Popups.WeatherEditorWindow.EnuSelection.Overcast:
           
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.OvercastFactor +=  .01f;
                        Viewer.WeatherEditorWindow.Overcast1.Text = Viewer.World.WeatherControl.Weather.OvercastFactor.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.OvercastFactor -=  .01f;
                        Viewer.WeatherEditorWindow.Overcast1.Text = Viewer.World.WeatherControl.Weather.OvercastFactor.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.OvercastFactor2 += .01f;
                        Viewer.WeatherEditorWindow.Overcast2.Text = Viewer.World.WeatherControl.Weather.OvercastFactor2.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.OvercastFactor2 -= .01f;
                        Viewer.WeatherEditorWindow.Overcast2.Text = Viewer.World.WeatherControl.Weather.OvercastFactor2.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.OvercastFactor3 += .01f;
                        Viewer.WeatherEditorWindow.Overcast3.Text = Viewer.World.WeatherControl.Weather.OvercastFactor3.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.OvercastFactor3 -= .01f;
                        Viewer.WeatherEditorWindow.Overcast3.Text = Viewer.World.WeatherControl.Weather.OvercastFactor3.ToString("0.00");
                    }   
                break;

                // Sun Size
                case Popups.WeatherEditorWindow.EnuSelection.SkySunSize_Sel:

                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.SunSize_Sunrise +=  .025f;
                        Viewer.WeatherEditorWindow.SkySunSizeSunrise.Text = Viewer.World.WeatherControl.Weather.SunSize_Sunrise.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.SunSize_Sunrise -=  .025f;
                        Viewer.WeatherEditorWindow.SkySunSizeSunrise.Text = Viewer.World.WeatherControl.Weather.SunSize_Sunrise.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.SunSize_Noon += .025f;
                        Viewer.WeatherEditorWindow.SkySunSizeNoon.Text = Viewer.World.WeatherControl.Weather.SunSize_Noon.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.SunSize_Noon -= .025f;
                        Viewer.WeatherEditorWindow.SkySunSizeNoon.Text = Viewer.World.WeatherControl.Weather.SunSize_Noon.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.SunSize_Sunset += .025f;
                        Viewer.WeatherEditorWindow.SkySunSizeSunset.Text = Viewer.World.WeatherControl.Weather.SunSize_Sunset.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.SunSize_Sunset -= .025f;
                        Viewer.WeatherEditorWindow.SkySunSizeSunset.Text = Viewer.World.WeatherControl.Weather.SunSize_Sunset.ToString("0.00");
                    } 
                    Viewer.WeatherEditorWindow.SkySunSizeCurrent.Text = Viewer.World.WeatherControl.Weather.SunSize_Mix.ToString("0.00");
                    

                break;
                
                // Precipitation Wind 1
                case Popups.WeatherEditorWindow.EnuSelection.PrecipWind1_Sel:
                        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind1.X +=  .1f;
                        Viewer.WeatherEditorWindow.PrecipWind1X.Text = Viewer.World.WeatherControl.Weather.PrecipWind1.X.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind1.X -=  .1f;
                        Viewer.WeatherEditorWindow.PrecipWind1X.Text = Viewer.World.WeatherControl.Weather.PrecipWind1.X.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                      Viewer.World.WeatherControl.Weather.PrecipWind1.Y += .1f;
                        Viewer.WeatherEditorWindow.PrecipWind1Y.Text = Viewer.World.WeatherControl.Weather.PrecipWind1.Y.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind1.Y -= .1f;
                        Viewer.WeatherEditorWindow.PrecipWind1Y.Text = Viewer.World.WeatherControl.Weather.PrecipWind1.Y.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind1.Z += .1f;
                        Viewer.WeatherEditorWindow.PrecipWind1Z.Text = Viewer.World.WeatherControl.Weather.PrecipWind1.Z.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind1.Z -= .1f;
                        Viewer.WeatherEditorWindow.PrecipWind1Z.Text = Viewer.World.WeatherControl.Weather.PrecipWind1.Z.ToString("0.00");
                    }  
                break;

                // Precipitation Wind 1
                case Popups.WeatherEditorWindow.EnuSelection.PrecipWind2_Sel:
                        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind2.X +=  .1f;
                        Viewer.WeatherEditorWindow.PrecipWind2X.Text = Viewer.World.WeatherControl.Weather.PrecipWind2.X.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind2.X -=  .1f;
                        Viewer.WeatherEditorWindow.PrecipWind2X.Text = Viewer.World.WeatherControl.Weather.PrecipWind2.X.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                      Viewer.World.WeatherControl.Weather.PrecipWind2.Y += .1f;
                        Viewer.WeatherEditorWindow.PrecipWind2Y.Text = Viewer.World.WeatherControl.Weather.PrecipWind2.Y.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind2.Y -= .1f;
                        Viewer.WeatherEditorWindow.PrecipWind2Y.Text = Viewer.World.WeatherControl.Weather.PrecipWind2.Y.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind2.Z += .1f;
                        Viewer.WeatherEditorWindow.PrecipWind2Z.Text = Viewer.World.WeatherControl.Weather.PrecipWind2.Z.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.PrecipWind2.Z -= .1f;
                        Viewer.WeatherEditorWindow.PrecipWind2Z.Text = Viewer.World.WeatherControl.Weather.PrecipWind2.Z.ToString("0.00");
                    }  
                break;

                // Vegetation Color Control
                case Popups.WeatherEditorWindow.EnuSelection.VegMod_Sel:
                        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.VegetationDesatuationModifier += .02f;
                        Viewer.WeatherEditorWindow.VegDesatMod.Text = Viewer.World.WeatherControl.Weather.VegetationDesatuationModifier.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.VegetationDesatuationModifier -= .02f;
                        Viewer.WeatherEditorWindow.VegDesatMod.Text = Viewer.World.WeatherControl.Weather.VegetationDesatuationModifier.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.VegetationBrightnessModifier += .02f;
                        Viewer.WeatherEditorWindow.VegBrightMod.Text = Viewer.World.WeatherControl.Weather.VegetationBrightnessModifier.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.VegetationBrightnessModifier -= .02f;
                        Viewer.WeatherEditorWindow.VegBrightMod.Text = Viewer.World.WeatherControl.Weather.VegetationBrightnessModifier.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.VegetationContrastModifier += .02f;
                        Viewer.WeatherEditorWindow.VegContMod.Text = Viewer.World.WeatherControl.Weather.VegetationContrastModifier.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.VegetationContrastModifier -= .02f;
                        Viewer.WeatherEditorWindow.VegContMod.Text = Viewer.World.WeatherControl.Weather.VegetationContrastModifier.ToString("0.00");
                    }  
                break;
                
                // Vegetation Color Control
                case Popups.WeatherEditorWindow.EnuSelection.TerrMod_Sel:
                        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.TerrainDesatuationModifier += .02f;
                        Viewer.WeatherEditorWindow.TerrDesatMod.Text = Viewer.World.WeatherControl.Weather.TerrainDesatuationModifier.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.TerrainDesatuationModifier -= .02f;
                        Viewer.WeatherEditorWindow.TerrDesatMod.Text = Viewer.World.WeatherControl.Weather.TerrainDesatuationModifier.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                      Viewer.World.WeatherControl.Weather.TerrainBrightnessModifier += .02f;
                        Viewer.WeatherEditorWindow.TerrBrightMod.Text = Viewer.World.WeatherControl.Weather.TerrainBrightnessModifier.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.TerrainBrightnessModifier -= .02f;
                        Viewer.WeatherEditorWindow.TerrBrightMod.Text = Viewer.World.WeatherControl.Weather.TerrainBrightnessModifier.ToString("0.00");
                    }        
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgUp)) {
                        Viewer.World.WeatherControl.Weather.TerrainContrastModifier += .02f;
                        Viewer.WeatherEditorWindow.TerrContMod.Text = Viewer.World.WeatherControl.Weather.TerrainContrastModifier.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_PgDn)) {
                        Viewer.World.WeatherControl.Weather.TerrainContrastModifier -= .02f;
                        Viewer.WeatherEditorWindow.TerrContMod.Text = Viewer.World.WeatherControl.Weather.TerrainContrastModifier.ToString("0.00");
                    }  
                break;

                // Precipitation particle size 1
                case Popups.WeatherEditorWindow.EnuSelection.PrecipParSize_Sel:
                        
                    if (UserInput.IsDown(UserCommand.Weather_Control_Insert)) {
                        Viewer.World.WeatherControl.Weather.ParticleSize1 += .02f;
                        Viewer.WeatherEditorWindow.PrecipParSize1.Text = Viewer.World.WeatherControl.Weather.ParticleSize1.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_Delete)) {
                        Viewer.World.WeatherControl.Weather.ParticleSize1 -= .02f;
                        Viewer.WeatherEditorWindow.PrecipParSize1.Text = Viewer.World.WeatherControl.Weather.ParticleSize1.ToString("0.00");
                    }        
                    /* Only one shader, todo: create two or options?
                     * if (UserInput.IsDown(UserCommand.Weather_Control_Home)) {
                        Viewer.World.WeatherControl.Weather.ParticleSize2 += .02f;
                        Viewer.WeatherEditorWindow.PrecipParSize2.Text = Viewer.World.WeatherControl.Weather.ParticleSize2.ToString("0.00");
                    }
                    if (UserInput.IsDown(UserCommand.Weather_Control_End)) {
                        Viewer.World.WeatherControl.Weather.ParticleSize2 -= .02f;
                        Viewer.WeatherEditorWindow.PrecipParSize2.Text = Viewer.World.WeatherControl.Weather.ParticleSize2.ToString("0.00");
                    }
                     */       
                break;
                
                // 
                case Popups.WeatherEditorWindow.EnuSelection.Precip_Sel:
                    Viewer.WeatherEditorWindow.PrecipLiquid.Text = Viewer.World.WeatherControl.Weather.PrecipitationLiquidity.ToString("0.00");
                    Viewer.WeatherEditorWindow.PricipIntPPSPM2.Text = Viewer.World.WeatherControl.Weather.PricipitationIntensityPPSPM2.ToString("0.00");   
                break;

                case Popups.WeatherEditorWindow.EnuSelection.Notselected:
                break;

                default:
                break;
            }

        }

        void ReloadWeather()
        {
            if(Viewer.MaterialManager.Materials.ContainsKey("Sky::0:0:0"))
            {
                Material bdata;
                // Get runtime SkyMaterial class
                Viewer.MaterialManager.Materials.TryGetValue("Sky::0:0:0", out bdata);
                // Load weather settings file or create new
                SetInitialWeatherParameters();
                var x = bdata as SkyMaterial;
                // Reload weather textures
                x.SetWeather();
            }

        }

        [CallOnThread("Updater")]
        public virtual void Update(ElapsedTime elapsedTime)
        {
            Time += elapsedTime.ClockSeconds;
            var manager = MPManager.Instance();

            
            if (MPManager.IsClient() && manager.weatherChanged)
            {
                // Multiplayer weather has changed so we need to update our state to match weather, overcastFactor, pricipitationIntensity and fogDistance.
                if (manager.weather >= 0 && manager.weather != (int)Viewer.Simulator.WeatherType)
                {
                    Viewer.Simulator.WeatherType = (Orts.Formats.Msts.WeatherType)manager.weather;
                    UpdateWeatherParameters();
                }
                if (manager.overcastFactor >= 0)
                    Weather.OvercastFactor = manager.overcastFactor;
                if (manager.pricipitationIntensity >= 0)
                {
                    Weather.PricipitationIntensityPPSPM2 = manager.pricipitationIntensity;
                    UpdateVolume();
                }
                if (manager.fogDistance >= 0)
                    Weather.SceneryFogDistance_Mix = manager.fogDistance;

                // Reset the message now that we've applied all the changes.
                if ((manager.weather >= 0 && manager.weather != (int)Viewer.Simulator.WeatherType) || manager.overcastFactor >= 0 || manager.pricipitationIntensity >= 0 || manager.fogDistance >= 0)
                {
                    manager.weatherChanged = false;
                    manager.weather = -1;
                    manager.overcastFactor = -1;
                    manager.pricipitationIntensity = -1;
                    manager.fogDistance = -1;
                }
            }
            else if (!MPManager.IsClient())
            {   
                if(Viewer.WeatherEditorWindow.Visible) 
                { 
                    // Update Weather Editor Window labels
                    GuiSelection();
                }
                
                // The user is able to change the weather for debugging. This will cycle through clear, rain and snow.
                if (UserInput.IsPressed(UserCommand.DebugWeatherChange))
                {
                    switch (Viewer.Simulator.WeatherType)
                    { 
                        // { Clear, Snow, Rain, Few, Cloudy, Desert, SnowStorm, Foggy, PartlyCloudy }
                        case WeatherType.Clear:
                            Viewer.Simulator.WeatherType = WeatherType.Rain;
                            ReloadWeather();
                            break;
                        case WeatherType.Rain:
                            Viewer.Simulator.WeatherType = WeatherType.Snow;
                            ReloadWeather();
                            break;
                        case WeatherType.Snow:
                            Viewer.Simulator.WeatherType = WeatherType.SnowStorm;
                            ReloadWeather();
                            break;
                        case WeatherType.Few:
                            Viewer.Simulator.WeatherType = WeatherType.Cloudy;
                            ReloadWeather();
                            break;
                        case WeatherType.Cloudy:
                            Viewer.Simulator.WeatherType = WeatherType.PartlyCloudy;
                            ReloadWeather();
                            break;
                        case WeatherType.Desert:
                            Viewer.Simulator.WeatherType = WeatherType.Few;
                            ReloadWeather();
                            break;
                        case WeatherType.SnowStorm:
                            Viewer.Simulator.WeatherType = WeatherType.Foggy;
                            ReloadWeather();
                            break;
                        case WeatherType.Foggy:
                            Viewer.Simulator.WeatherType = WeatherType.Desert;
                            ReloadWeather();
                            break;
                        case WeatherType.PartlyCloudy:
                            Viewer.Simulator.WeatherType = WeatherType.Clear;
                            ReloadWeather();
                            break;
                    }
                    // Block dynamic weather change after a manual weather change operation
                    weatherChangeOn = false;
                    dynamicWeather?.ResetWeatherTargets();
                    UpdateWeatherParameters();

                    // If we're a multiplayer server, send out the new weather to all clients.
                    if (MPManager.IsServer())
                        MPManager.Notify(new MSGWeather((int)Viewer.Simulator.WeatherType, -1, -1, -1).ToString());

                        
                }

                // Pricipitation ranges from 0 to max PrecipitationViewer.MaxIntensityPPSPM2 if 32bit.
                // 16bit uses PrecipitationViewer.MaxIntensityPPSPM2_16
                // 0xFFFF represents 65535 which is the max for 16bit devices.
                if (UserInput.IsDown(UserCommand.DebugPrecipitationIncrease))
                {
                    if (Viewer.Simulator.WeatherType == WeatherType.Clear)
                    {
                        Viewer.SoundProcess.RemoveSoundSources(this);
                        if (Weather.PrecipitationLiquidity > DynamicWeather.RainSnowLiquidityThreshold)
                        {
                            Viewer.Simulator.WeatherType = WeatherType.Rain;
                            Viewer.SoundProcess.AddSoundSources(this, RainSound);
                        }
                        else
                        {
                            Viewer.Simulator.WeatherType = WeatherType.Snow;
                            Viewer.SoundProcess.AddSoundSources(this, SnowSound);
                        }
                    }
                    Weather.PricipitationIntensityPPSPM2 = MathHelper.Clamp(Weather.PricipitationIntensityPPSPM2 * 1.05f
                        , PrecipitationViewer.MinIntensityPPSPM2 + 0.0000001f, PrecipitationViewer.MaxIntensityPPSPM2 );
                    weatherChangeOn = false;
                    if (dynamicWeather != null) dynamicWeather.ORTSPrecipitationIntensity = -1;
                }
                if (UserInput.IsDown(UserCommand.DebugPrecipitationDecrease))
                {
                    Weather.PricipitationIntensityPPSPM2 = MathHelper.Clamp(Weather.PricipitationIntensityPPSPM2 / 1.05f
                        , PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
                    if (Weather.PricipitationIntensityPPSPM2 < PrecipitationViewer.MinIntensityPPSPM2 + 0.00001f)
                    {
                        Weather.PricipitationIntensityPPSPM2 = 0;
                        if (Viewer.Simulator.WeatherType != WeatherType.Clear)
                        {
                            Viewer.SoundProcess.RemoveSoundSources(this);
                            Viewer.SoundProcess.AddSoundSources(this, ClearSound);
                        }
                    }
                    weatherChangeOn = false;
                    if (dynamicWeather != null) dynamicWeather.ORTSPrecipitationIntensity = -1;
                }
                if (UserInput.IsDown(UserCommand.DebugPrecipitationIncrease) || UserInput.IsDown(UserCommand.DebugPrecipitationDecrease)) UpdateVolume();

                // Change in precipitation liquidity, passing from rain to snow and vice-versa
                if (UserInput.IsDown(UserCommand.DebugPrecipitationLiquidityIncrease))
                {
                    Weather.PrecipitationLiquidity = MathHelper.Clamp(Weather.PrecipitationLiquidity + 0.01f, 0, 1);
                    weatherChangeOn = false;
                    if (dynamicWeather != null) dynamicWeather.ORTSPrecipitationLiquidity = -1;
                    if (Weather.PrecipitationLiquidity > DynamicWeather.RainSnowLiquidityThreshold && Viewer.Simulator.WeatherType != WeatherType.Rain
                        && Weather.PricipitationIntensityPPSPM2 > 0)
                    {
                        Viewer.Simulator.WeatherType = WeatherType.Rain;
                        Viewer.SoundProcess.RemoveSoundSources(this);
                        Viewer.SoundProcess.AddSoundSources(this, RainSound);
                    }
                }
                if (UserInput.IsDown(UserCommand.DebugPrecipitationLiquidityDecrease))
                {
                    Weather.PrecipitationLiquidity = MathHelper.Clamp(Weather.PrecipitationLiquidity - 0.01f, 0, 1);
                    weatherChangeOn = false;
                    if (dynamicWeather != null) dynamicWeather.ORTSPrecipitationLiquidity = -1;
                    if (Weather.PrecipitationLiquidity <= DynamicWeather.RainSnowLiquidityThreshold && Viewer.Simulator.WeatherType != WeatherType.Snow
                        && Weather.PricipitationIntensityPPSPM2 > 0)
                    {
                        Viewer.Simulator.WeatherType = WeatherType.Snow;
                        Viewer.SoundProcess.RemoveSoundSources(this);
                        Viewer.SoundProcess.AddSoundSources(this, SnowSound);
                    }
                }
                if (UserInput.IsDown(UserCommand.DebugPrecipitationLiquidityIncrease) || UserInput.IsDown(UserCommand.DebugPrecipitationLiquidityDecrease)) UpdateVolume();

                // Daylight offset is useful for debugging night running timetables; it ranges from -12h to +12h
                string FormatDaylightOffsetHour(int h) => h <= 0 ? h.ToString() : $"+{h}";
                if (UserInput.IsPressed(UserCommand.DebugDaylightOffsetIncrease) && Weather.DaylightOffset < 12)
                {
                    Weather.DaylightOffset += 1;
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Increased daylight offset to {0} h", FormatDaylightOffsetHour(Weather.DaylightOffset)));
                }
                if (UserInput.IsPressed(UserCommand.DebugDaylightOffsetDecrease) && Weather.DaylightOffset > -12)
                {
                    Weather.DaylightOffset -= 1;
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Decreased daylight offset to {0} h", FormatDaylightOffsetHour(Weather.DaylightOffset)));
                }

                // Save Weather Parameters to Log File
                if (UserInput.IsPressed(UserCommand.DebugWeatherSave) || SaveWeatherSwitch) {
                    SaveWeatherSwitch = false;
                    Viewer.WeatherEditorWindow.Bnt_SaveWeathertype_Sel.Text = " Saving Weather";
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Saving Weather Parameters"));
                    Console.WriteLine("\n##########################################");
                    Console.WriteLine("## Exrail Weather Extension V3.2        ##");
                    Console.WriteLine("## Saved Weather Parameters             ##");
                    Console.Write(    "## WeatherType = "); 
                    Console.Write( (int)Viewer.Simulator.WeatherType );
                    Console.Write(    "                      ##\n");
                    Console.WriteLine("##########################################");
                    Console.WriteLine("Weather.PrecipitationLiquidity = " + Weather.PrecipitationLiquidity);
                    Console.WriteLine("Weather.PricipitationIntensityPPSPM2 = " + Weather.PricipitationIntensityPPSPM2);
                    
                    Console.WriteLine("Weather.WindSpeed = " + Weather.WindSpeed);
                    Console.WriteLine("Weather.WindDirectionSky = " + Weather.WindDirectionSky);
                    
                    Console.WriteLine("Weather.PrecipWind1.X = " + Weather.PrecipWind1.X);                            
                    Console.WriteLine("Weather.PrecipWind1.Y = " + Weather.PrecipWind1.Y);                            
                    Console.WriteLine("Weather.PrecipWind1.Z = " + Weather.PrecipWind1.Z);                            
                    Console.WriteLine("Weather.PrecipWind2.X = " + Weather.PrecipWind2.X);                            
                    Console.WriteLine("Weather.PrecipWind2.Y = " + Weather.PrecipWind2.Y);                            
                    Console.WriteLine("Weather.PrecipWind2.Z = " + Weather.PrecipWind2.Z);
                    
                    Console.WriteLine("Weather.ParticleSize1 = " + Weather.ParticleSize1);  
                    Console.WriteLine("Weather.ParticleSize2 = " + Weather.ParticleSize2);  
                   
                    Console.WriteLine("Weather.OvercastFactor  = "  + Weather.OvercastFactor);
                    Console.WriteLine("Weather.OvercastFactor2 = " + Weather.OvercastFactor2);
                    Console.WriteLine("Weather.OvercastFactor3 = " + Weather.OvercastFactor3);
                    
                    Console.WriteLine("Weather.SunSize_Sunrise = " + Weather.SunSize_Sunrise);
                    Console.WriteLine("Weather.SunSize_Noon    = "    + Weather.SunSize_Noon);   
                    Console.WriteLine("Weather.SunSize_Sunset  = "  + Weather.SunSize_Sunset); 
                    
                    Console.WriteLine("Weather.SkyFogDistance_Sunrise = " + Weather.SkyFogDistance_Sunrise);
                    Console.WriteLine("Weather.SkyFogDistance_Noon    = "    + Weather.SkyFogDistance_Noon);   
                    Console.WriteLine("Weather.SkyFogDistance_Sunset  = "  + Weather.SkyFogDistance_Sunset);
                    Console.WriteLine("Weather.SkyFog_Sunrise.R = " + Weather.SkyFog_Sunrise.R);
                    Console.WriteLine("Weather.SkyFog_Sunrise.G = " + Weather.SkyFog_Sunrise.G);
                    Console.WriteLine("Weather.SkyFog_Sunrise.B = " + Weather.SkyFog_Sunrise.B);
                    Console.WriteLine("Weather.SkyFog_Noon.R = " + Weather.SkyFog_Noon.R);  
                    Console.WriteLine("Weather.SkyFog_Noon.G = " + Weather.SkyFog_Noon.G);   
                    Console.WriteLine("Weather.SkyFog_Noon.B = " + Weather.SkyFog_Noon.B);
                    Console.WriteLine("Weather.SkyFog_Sunset.R = " + Weather.SkyFog_Sunset.R); 
                    Console.WriteLine("Weather.SkyFog_Sunset.G = " + Weather.SkyFog_Sunset.G);
                    Console.WriteLine("Weather.SkyFog_Sunset.B = " + Weather.SkyFog_Sunset.B);
                  
                    Console.WriteLine("Weather.SceneryFogDistance_Sunrise = " + Weather.SceneryFogDistance_Sunrise);
                    Console.WriteLine("Weather.SceneryFogDistance_Noon    = "    + Weather.SceneryFogDistance_Noon);   
                    Console.WriteLine("Weather.SceneryFogDistance_Sunset  = "  + Weather.SceneryFogDistance_Sunset); 
                    Console.WriteLine("Weather.SceneryFog_Sunrise.R = " + Weather.SceneryFog_Sunrise.R);
                    Console.WriteLine("Weather.SceneryFog_Sunrise.G = " + Weather.SceneryFog_Sunrise.G);
                    Console.WriteLine("Weather.SceneryFog_Sunrise.B = " + Weather.SceneryFog_Sunrise.B);
                    Console.WriteLine("Weather.SceneryFog_Noon.R = " + Weather.SceneryFog_Noon.R);   
                    Console.WriteLine("Weather.SceneryFog_Noon.G = " + Weather.SceneryFog_Noon.G);
                    Console.WriteLine("Weather.SceneryFog_Noon.B = " + Weather.SceneryFog_Noon.B);
                    Console.WriteLine("Weather.SceneryFog_Sunset.R = " + Weather.SceneryFog_Sunset.R); 
                    Console.WriteLine("Weather.SceneryFog_Sunset.G = " + Weather.SceneryFog_Sunset.G);
                    Console.WriteLine("Weather.SceneryFog_Sunset.B = " + Weather.SceneryFog_Sunset.B);
                    
                    Console.WriteLine("Weather.VegetationDesatuationModifier = " + Weather.VegetationDesatuationModifier);
                    Console.WriteLine("Weather.VegetationBrightnessModifier  = "  + Weather.VegetationBrightnessModifier);
                    Console.WriteLine("Weather.VegetationContrastModifier    = "    + Weather.VegetationContrastModifier);
                    
                    Console.WriteLine("Weather.TerrainDesatuationModifier = " + Weather.TerrainDesatuationModifier);
                    Console.WriteLine("Weather.TerrainBrightnessModifier  = "  + Weather.TerrainBrightnessModifier);
                    Console.WriteLine("Weather.TerrainContrastModifier    = "    + Weather.TerrainContrastModifier);
                    WeatherFileOut = new BinaryWriter(new FileStream(Path.Combine(Viewer.ContentPath, Weatherfilepath), FileMode.Open, FileAccess.Write));
                    Console.WriteLine("########### Saving WeatherType ###########");
                    SaveWeatherType( WeatherFileOut );
                    Console.WriteLine("########### Weather File Saved ###########");
                    Console.WriteLine("##########################################");
                    // ===================================/\============================================
                    Viewer.WeatherEditorWindow.Bnt_SaveWeathertype_Sel.Color = Viewer.WeatherEditorWindow.Color_ReLoaded;
                    Viewer.WeatherEditorWindow.Bnt_SaveWeathertype_Sel.Text = " ◄Save Weather►";
                }
                UpdateWind(elapsedTime);
            }

            if (!MPManager.IsMultiPlayer())
            {
                // Shift the clock forwards or backwards at 1/2 h-per-second.
                if (UserInput.IsDown(UserCommand.DebugClockForwards)) Viewer.Simulator.ClockTime += elapsedTime.RealSeconds * 1800;
                if (UserInput.IsDown(UserCommand.DebugClockBackwards)) Viewer.Simulator.ClockTime -= elapsedTime.RealSeconds * 1800;
            }

            // If we're a multiplayer server, send out the new overcastFactor, pricipitationIntensity and fogDistance to all clients.
            if (MPManager.IsServer())
            {
                if (UserInput.IsReleased(UserCommand.DebugOvercastIncrease) || UserInput.IsReleased(UserCommand.DebugOvercastDecrease)
                    || UserInput.IsReleased(UserCommand.DebugPrecipitationIncrease) || UserInput.IsReleased(UserCommand.DebugPrecipitationDecrease)
                    || UserInput.IsReleased(UserCommand.DebugFogIncrease) || UserInput.IsReleased(UserCommand.DebugFogDecrease))
                {
                    manager.SetEnvInfo(Weather.OvercastFactor, Weather.SceneryFogDistance_Mix);
                    MPManager.Notify(new MSGWeather(-1, Weather.OvercastFactor, Weather.PricipitationIntensityPPSPM2, Weather.SceneryFogDistance_Mix).ToString());
                }
            }
            if (Program.Simulator != null && Program.Simulator.ActivityRun != null && Program.Simulator.ActivityRun.triggeredEventWrapper != null &&
               (Program.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.ORTSWeatherChange != null || Program.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.Outcomes.ORTSWeatherChange != null))
            // Start a weather change sequence in activity mode
            {
                // If not yet weather changes, create the instance
                if (dynamicWeather == null)
                {
                    dynamicWeather = new DynamicWeather();
                }
                var weatherChange = Program.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.ORTSWeatherChange ?? Program.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.Outcomes.ORTSWeatherChange;
                dynamicWeather.WeatherChange_Init(weatherChange, this);
                Program.Simulator.ActivityRun.triggeredEventWrapper = null;
            }
            if (weatherChangeOn)
            // Manage the weather change sequence
            {
                dynamicWeather.WeatherChange_Update(elapsedTime, this);
            }
            if (RandomizedWeather && !weatherChangeOn) // Time to prepare a new weather change
                dynamicWeather.WeatherChange_NextRandomization(elapsedTime, this);
            
            if (Weather.PricipitationIntensityPPSPM2 == 0 && Viewer.Simulator.WeatherType != WeatherType.Clear)
            {
                // ExRail - Disable setting the weather to Clear | Viewer.Simulator.WeatherType = WeatherType.Clear;
                UpdateWeatherParameters();
            }
            else if (Weather.PricipitationIntensityPPSPM2 > 0 && Viewer.Simulator.WeatherType == WeatherType.Clear)
            {
                Viewer.Simulator.WeatherType = Weather.PrecipitationLiquidity > DynamicWeather.RainSnowLiquidityThreshold ? WeatherType.Rain : WeatherType.Snow;
                UpdateWeatherParameters();
            }
        }

        public class DynamicWeather
        {
            public const float RainSnowLiquidityThreshold = 0.3f;
            public float overcastChangeRate;
            public float overcastTimer;
            public float fogChangeRate;
            public float fogTimer;
            public float stableWeatherTimer;
            public float precipitationIntensityChangeRate;
            public float precipitationIntensityTimer;
            public float precipitationIntensityDelayTimer = -1;
            public float precipitationLiquidityChangeRate;
            public float precipitationLiquidityTimer;
            public float ORTSOvercast = -1;
            public int ORTSOvercastTransitionTimeS = -1;
            public float ORTSFog = -1;
            public int ORTSFogTransitionTimeS = -1;
            public float ORTSPrecipitationIntensity = -1;
            public int ORTSPrecipitationIntensityTransitionTimeS = -1;
            public float ORTSPrecipitationLiquidity = -1;
            public int ORTSPrecipitationLiquidityTransitionTimeS = -1;
            public bool fogDistanceIncreasing;
            public DynamicWeather()
            {
            }

            public void Save(BinaryWriter outf)
            {
                outf.Write(overcastTimer);
                outf.Write(overcastChangeRate);
                outf.Write(fogTimer);
                outf.Write(fogChangeRate);
                outf.Write(precipitationIntensityTimer);
                outf.Write(precipitationIntensityChangeRate);
                outf.Write(precipitationLiquidityTimer);
                outf.Write(precipitationLiquidityChangeRate);
                outf.Write(ORTSOvercast);
                outf.Write(ORTSFog);
                outf.Write(ORTSPrecipitationIntensity);
                outf.Write(ORTSPrecipitationLiquidity);
                outf.Write(fogDistanceIncreasing);
                outf.Write(ORTSFogTransitionTimeS);
                outf.Write(stableWeatherTimer);
                outf.Write(precipitationIntensityDelayTimer);
            }

            public void Restore(BinaryReader inf)
            {
                overcastTimer = inf.ReadSingle();
                overcastChangeRate = inf.ReadSingle();
                fogTimer = inf.ReadSingle();
                fogChangeRate = inf.ReadSingle();
                precipitationIntensityTimer = inf.ReadSingle();
                precipitationIntensityChangeRate = inf.ReadSingle();
                precipitationLiquidityTimer = inf.ReadSingle();
                precipitationLiquidityChangeRate = inf.ReadSingle();
                ORTSOvercast = inf.ReadSingle();
                ORTSFog = inf.ReadSingle();
                ORTSPrecipitationIntensity = inf.ReadSingle();
                ORTSPrecipitationLiquidity = inf.ReadSingle();
                fogDistanceIncreasing = inf.ReadBoolean();
                ORTSFogTransitionTimeS = inf.ReadInt32();
                stableWeatherTimer = inf.ReadSingle();
                precipitationIntensityDelayTimer = inf.ReadSingle();
            }

            public void ResetWeatherTargets()
            {
                ORTSOvercast = -1;
                ORTSFog = -1;
                ORTSPrecipitationIntensity = -1;
                ORTSPrecipitationLiquidity = -1;
            }

            // Check for correctness of parameters and initialize rates of change

            public void WeatherChange_Init(ORTSWeatherChange eventWeatherChange, WeatherControl weatherControl)
            {
                var wChangeOn = false;
                if (eventWeatherChange.ORTSOvercast >= 0 && eventWeatherChange.ORTSOvercastTransitionTimeS >= 0)
                {
                    ORTSOvercast = eventWeatherChange.ORTSOvercast;
                    ORTSOvercastTransitionTimeS = eventWeatherChange.ORTSOvercastTransitionTimeS;
                    overcastTimer = ORTSOvercastTransitionTimeS;
                    overcastChangeRate = overcastTimer > 0 ? (MathHelper.Clamp(ORTSOvercast, 0, 1.0f) - weatherControl.Weather.OvercastFactor) / ORTSOvercastTransitionTimeS : 0;
                    wChangeOn = true;
                }
                if (eventWeatherChange.ORTSFog >= 0 && eventWeatherChange.ORTSFogTransitionTimeS >= 0)
                {
                    ORTSFog = eventWeatherChange.ORTSFog;
                    ORTSFogTransitionTimeS = eventWeatherChange.ORTSFogTransitionTimeS;
                    fogTimer = ORTSFogTransitionTimeS;
                    var fogFinalValue = MathHelper.Clamp(ORTSFog, FogMinDistance, FogMaxDistance);
                    fogDistanceIncreasing = false;
                    fogChangeRate = fogTimer > 0 ? (fogFinalValue - weatherControl.Weather.SceneryFogDistance_Mix) / (ORTSFogTransitionTimeS * ORTSFogTransitionTimeS) : 0;
                    if (fogFinalValue > weatherControl.Weather.SceneryFogDistance_Mix)
                    {
                        fogDistanceIncreasing = true;
                        fogChangeRate = -fogChangeRate;
                        if (fogTimer > 0) ORTSFog = weatherControl.Weather.SceneryFogDistance_Mix;
                    }
                    wChangeOn = true;
                }
                if (eventWeatherChange.ORTSPrecipitationIntensity >= 0 && eventWeatherChange.ORTSPrecipitationIntensityTransitionTimeS >= 0)
                {
                    ORTSPrecipitationIntensity = eventWeatherChange.ORTSPrecipitationIntensity;
                    ORTSPrecipitationIntensityTransitionTimeS = eventWeatherChange.ORTSPrecipitationIntensityTransitionTimeS;
                    precipitationIntensityTimer = ORTSPrecipitationIntensityTransitionTimeS;
                    // Pricipitation ranges from 0 to max PrecipitationViewer.MaxIntensityPPSPM2 if 32bit.
                    // 16bit uses PrecipitationViewer.MaxIntensityPPSPM2_16
                    if (weatherControl.Viewer.GraphicsDevice.GraphicsProfile == GraphicsProfile.HiDef)
                        precipitationIntensityChangeRate = precipitationIntensityTimer > 0 ? (MathHelper.Clamp(ORTSPrecipitationIntensity, 0, PrecipitationViewer.MaxIntensityPPSPM2)
                            - weatherControl.Weather.PricipitationIntensityPPSPM2) / ORTSPrecipitationIntensityTransitionTimeS : 0;
                    wChangeOn = true;
                }
                if (eventWeatherChange.ORTSPrecipitationLiquidity >= 0 && eventWeatherChange.ORTSPrecipitationLiquidityTransitionTimeS >= 0)
                {
                    ORTSPrecipitationLiquidity = eventWeatherChange.ORTSPrecipitationLiquidity;
                    ORTSPrecipitationLiquidityTransitionTimeS = eventWeatherChange.ORTSPrecipitationLiquidityTransitionTimeS;
                    precipitationLiquidityTimer = ORTSPrecipitationLiquidityTransitionTimeS;
                    precipitationLiquidityChangeRate = precipitationLiquidityTimer > 0 ? (MathHelper.Clamp(ORTSPrecipitationLiquidity, 0, 1.0f)
                        - weatherControl.Weather.PrecipitationLiquidity) / ORTSPrecipitationLiquidityTransitionTimeS : 0;
                    wChangeOn = true;
                }
                weatherControl.weatherChangeOn = wChangeOn;
            }

            public void WeatherChange_Update(ElapsedTime elapsedTime, WeatherControl weatherControl)
            {
                var wChangeOn = false;
                if (ORTSOvercast >= 0)
                {
                    overcastTimer -= elapsedTime.ClockSeconds;
                    if (overcastTimer <= 0) overcastTimer = 0;
                    else wChangeOn = true;
                    weatherControl.Weather.OvercastFactor = ORTSOvercast - (overcastTimer * overcastChangeRate);
                    if (overcastTimer == 0) ORTSOvercast = -1;
                }
                if (ORTSFog >= 0)
                {
                    fogTimer -= elapsedTime.ClockSeconds;
                    if (fogTimer <= 0) fogTimer = 0;
                    else wChangeOn = true;
                    if (!fogDistanceIncreasing)
                        weatherControl.Weather.SceneryFogDistance_Mix = ORTSFog - (fogTimer * fogTimer * fogChangeRate);
                    else
                    {
                        var fogTimerDifference = ORTSFogTransitionTimeS - fogTimer;
                        weatherControl.Weather.SceneryFogDistance_Mix = ORTSFog - (fogTimerDifference * fogTimerDifference * fogChangeRate);
                    }
                    if (fogTimer == 0) ORTSFog = -1;
                }
                if (ORTSPrecipitationIntensity >= 0 && precipitationIntensityDelayTimer == -1)
                {
                    precipitationIntensityTimer -= elapsedTime.ClockSeconds;
                    if (precipitationIntensityTimer <= 0) precipitationIntensityTimer = 0;
                    else if (weatherControl.RandomizedWeather == false) wChangeOn = true;
                    var oldPricipitationIntensityPPSPM2 = weatherControl.Weather.PricipitationIntensityPPSPM2;
                    weatherControl.Weather.PricipitationIntensityPPSPM2 = ORTSPrecipitationIntensity - (precipitationIntensityTimer * precipitationIntensityChangeRate);
                    if (weatherControl.Weather.PricipitationIntensityPPSPM2 > 0)
                    {
                        if (oldPricipitationIntensityPPSPM2 == 0)
                        {
                            weatherControl.Viewer.Simulator.WeatherType = weatherControl.Weather.PrecipitationLiquidity > RainSnowLiquidityThreshold ? WeatherType.Rain : WeatherType.Snow;
                            weatherControl.UpdateSoundSources();
                        }
                        weatherControl.UpdateVolume();
                    }
                    if (weatherControl.Weather.PricipitationIntensityPPSPM2 == 0)
                    {
                        if (oldPricipitationIntensityPPSPM2 > 0)
                        {
                            weatherControl.Viewer.Simulator.WeatherType = WeatherType.Clear;
                            weatherControl.UpdateSoundSources();
                        }
                    }
                    if (precipitationIntensityTimer == 0) ORTSPrecipitationIntensity = -1;
                }
                else if (ORTSPrecipitationIntensity >= 0 && precipitationIntensityDelayTimer > 0)
                {
                    precipitationIntensityDelayTimer -= elapsedTime.ClockSeconds;
                    if (precipitationIntensityDelayTimer <= 0)
                    {
                        precipitationIntensityDelayTimer = -1; // OK, now rain/snow can start
                        precipitationIntensityTimer = overcastTimer; // Going in parallel now
                    }
                }
                if (ORTSPrecipitationLiquidity >= 0)
                {
                    precipitationLiquidityTimer -= elapsedTime.ClockSeconds;
                    if (precipitationLiquidityTimer <= 0) precipitationLiquidityTimer = 0;
                    else wChangeOn = true;
                    var oldPrecipitationLiquidity = weatherControl.Weather.PrecipitationLiquidity;
                    weatherControl.Weather.PrecipitationLiquidity = ORTSPrecipitationLiquidity - (precipitationLiquidityTimer * precipitationLiquidityChangeRate);
                    if (weatherControl.Weather.PrecipitationLiquidity > RainSnowLiquidityThreshold)
                    {
                        if (oldPrecipitationLiquidity <= RainSnowLiquidityThreshold)
                        {
                            weatherControl.Viewer.Simulator.WeatherType = WeatherType.Rain;
                            weatherControl.UpdateSoundSources();
                            weatherControl.UpdateVolume();
                        }
                    }
                    if (weatherControl.Weather.PrecipitationLiquidity <= RainSnowLiquidityThreshold)
                    {
                        if (oldPrecipitationLiquidity > RainSnowLiquidityThreshold)
                        {
                            weatherControl.Viewer.Simulator.WeatherType = WeatherType.Snow;
                            weatherControl.UpdateSoundSources();
                            weatherControl.UpdateVolume();
                        }
                    }
                    if (precipitationLiquidityTimer == 0) ORTSPrecipitationLiquidity = -1;
                }
                if (stableWeatherTimer > 0)
                {
                    stableWeatherTimer -= elapsedTime.ClockSeconds;
                    if (stableWeatherTimer <= 0)
                        stableWeatherTimer = 0;
                    else wChangeOn = true;
                }
                weatherControl.weatherChangeOn = wChangeOn;
            }

            public void WeatherChange_NextRandomization(ElapsedTime elapsedTime, WeatherControl weatherControl) // start next randomization
            {
                // Define how much time transition will last
                var weatherChangeTimer = ((4 - weatherControl.Viewer.Settings.ActWeatherRandomizationLevel) * 600) +
                    Viewer.Random.Next((4 - weatherControl.Viewer.Settings.ActWeatherRandomizationLevel) * 600);
                // Begin with overcast
                var randValue = Viewer.Random.Next(170);
                var intermValue = randValue >= 50 ? (float)(randValue - 50f) : randValue;
                ORTSOvercast = intermValue >= 20 ? (float)(intermValue - 20f) / 100f : (float)intermValue / 100f; // give more probability to less overcast
                ORTSOvercastTransitionTimeS = weatherChangeTimer;
                overcastTimer = ORTSOvercastTransitionTimeS;
                overcastChangeRate = overcastTimer > 0 ? (MathHelper.Clamp(ORTSOvercast, 0, 1.0f) - weatherControl.Weather.OvercastFactor) / ORTSOvercastTransitionTimeS : 0;
                // Then check if we are in precipitation zone
                if (ORTSOvercast > 0.5)
                {
                    randValue = Viewer.Random.Next(75);
                    if (randValue > 40)
                    {
                        ORTSPrecipitationIntensity = (float)(randValue - 40f) / 1000f;
                        if (weatherControl.Viewer.GraphicsDevice.GraphicsProfile != GraphicsProfile.HiDef)
                            ORTSPrecipitationIntensity = Math.Min(ORTSPrecipitationIntensity, 0.010f);
                        if (weatherControl.Viewer.Simulator.Season == SeasonType.Winter)
                        {
                            weatherControl.Weather.PrecipitationLiquidity = 0;
                        }
                        else
                        {
                            weatherControl.Weather.PrecipitationLiquidity = 1;
                        }
                    }
                }
                if (weatherControl.Weather.PricipitationIntensityPPSPM2 > 0 && ORTSPrecipitationIntensity == -1)
                {
                    ORTSPrecipitationIntensity = 0;
                    // Must return to zero before overcast < 0.5
                    ORTSPrecipitationIntensityTransitionTimeS = (int)((0.5 - weatherControl.Weather.OvercastFactor) / overcastChangeRate);
                }
                if (weatherControl.Weather.PricipitationIntensityPPSPM2 == 0 && ORTSPrecipitationIntensity > 0 && weatherControl.Weather.OvercastFactor < 0.5)
                {
                    // We will have precipitation now, but it must start after overcast is over 0.5
                    precipitationIntensityDelayTimer = (0.5f - weatherControl.Weather.OvercastFactor) / overcastChangeRate;
                }

                if (ORTSPrecipitationIntensity > 0)
                {
                    ORTSPrecipitationIntensityTransitionTimeS = weatherChangeTimer;
                }
                if (ORTSPrecipitationIntensity >= 0)
                {
                    precipitationIntensityTimer = ORTSPrecipitationIntensityTransitionTimeS;
                    // Pricipitation ranges from 0 to max PrecipitationViewer.MaxIntensityPPSPM2 
                    precipitationIntensityChangeRate = precipitationIntensityTimer > 0 ? (MathHelper.Clamp(ORTSPrecipitationIntensity, 0, PrecipitationViewer.MaxIntensityPPSPM2)
                       - weatherControl.Weather.PricipitationIntensityPPSPM2) / ORTSPrecipitationIntensityTransitionTimeS : 0;
       
                }

                // And now define visibility
                randValue = Viewer.Random.Next(2000);
                if (ORTSPrecipitationIntensity > 0 || ORTSOvercast > 0.7f)
                    // Use first digit to define power of ten and the other three to define the multiplying number
                    ORTSFog = Math.Max(100, (float)Math.Pow(10, randValue / 1000 + 2) * (float)(((randValue % 1000) + 1) / 100f));
                else
                    ORTSFog = Math.Max(500, (float)Math.Pow(10, (randValue / 1000) + 3) * (float)(((randValue % 1000) + 1) / 100f));
                ORTSFogTransitionTimeS = weatherChangeTimer;
                fogTimer = ORTSFogTransitionTimeS;
                var fogFinalValue = MathHelper.Clamp(ORTSFog, 10, 100000);
                fogDistanceIncreasing = false;
                fogChangeRate = fogTimer > 0 ? (fogFinalValue - weatherControl.Weather.SceneryFogDistance_Mix) / (ORTSFogTransitionTimeS * ORTSFogTransitionTimeS) : 0;
                if (fogFinalValue > weatherControl.Weather.SceneryFogDistance_Mix)
                {
                    fogDistanceIncreasing = true;
                    fogChangeRate = -fogChangeRate;
                    ORTSFog = weatherControl.Weather.SceneryFogDistance_Mix;
                }

                weatherControl.weatherChangeOn = true;
            }
        }
    }


    public class AutomaticWeather : WeatherControl
    {
        /* Variables used for auto weather control */
        // settings
        readonly List<WeatherSetting> weatherDetails = new List<WeatherSetting>();

        // running values
        // general
        public int AWActiveIndex;                                        // active active index in waether list
        public float AWNextChangeTime;                                   // time for next change
        public float AWLastVisibility;                                   // visibility at end of previous weather
        public Orts.Formats.Msts.WeatherType AWPrecipitationActiveType;  // actual active precipitation

        // cloud
        public float AWOvercastCloudcover;                               // actual cloudcover
        public float AWOvercastCloudRateOfChangepS;                      // rate of change of cloudcover

        // precipitation
        public Orts.Formats.Msts.WeatherType AWPrecipitationRequiredType;// actual active precipitation
        public float AWPrecipitationTotalDuration;                       // actual total duration (seconds)
        public int AWPrecipitationTotalSpread;                           // actual number of periods with precipitation
        public float AWPrecipitationActualPPSPM2;                        // actual rate of precipitation (particals per second per square meter)
        public float AWPrecipitationRequiredPPSPM2;                      // required rate of precipitation
        public float AWPrecipitationRateOfChangePPSPM2PS;                // rate of change for rate of precipitation (particals per second per square meter per second)
        public float AWPrecipitationEndSpell;                            // end of present spell of precipitation (time in seconds)
        public float AWPrecipitationNextSpell;                           // start of next spell (time in seconds) (-1 if no further spells)
        public float AWPrecipitationStartRate;                           // rate of change at start of spell
        public float AWPrecipitationEndRate;                             // rate of change at end of spell

        // fog
        public float AWActualVisibility;                                 // actual fog visibility
        public float AWFogChangeRateMpS;                                 // required rate of change for fog
        public float AWFogLiftTime;                                      // start time of fog lifting to be clear at required time

        // wind
        public float AWPreviousWindSpeed;                                // windspeed at end of previous weather
        public float AWRequiredWindSpeed;                                // required wind speed at end of weather
        public float AWAverageWindSpeed;                                 // required average windspeed
        public float AWAverageWindGust;                                  // required average additional wind gust
        public float AWWindGustTime;                                     // time of next wind gust
        public float AWActualWindSpeed;                                  // actual wind speed
        public float AWWindSpeedChange;                                  // required change of wind speed
        public float AWRequiredWindDirection;                            // required wind direction at end of weather
        public float AWAverageWindDirection;                             // required average wind direction
        public float AWActualWindDirection;                              // actual wind direction
        public float AWWindDirectionChange;                              // required wind direction change

        public AutomaticWeather(Viewer viewer, string weatherFile, double realTime)
            : base(viewer)
        {
            // Read weather details from file
            var WeatherFile = new WeatherFile(weatherFile);
            weatherDetails = WeatherFile.Changes;

            if (weatherDetails.Count == 0)
            {
                Trace.TraceWarning("Weather file contains no settings {0}", weatherFile);
            }
            else
            {
                CheckWeatherDetails();
            }

            // Set initial weather parameters
            SetInitialWeatherParameters(realTime);
        }

        // Dummy constructor for restore
        public AutomaticWeather(Viewer viewer)
            : base(viewer)
        {
        }

        // Check weather details, set auto variables
        void CheckWeatherDetails()
        {
            float prevTime = 0;

            foreach (WeatherSetting weatherSet in weatherDetails)
            {
                TimeSpan acttime = new TimeSpan((long)(weatherSet.Time * 10000000));

                // Check if time is in sequence
                if (weatherSet.Time < prevTime)
                {
                    Trace.TraceInformation("Invalid time value : time out of sequence : {0}", acttime.ToString());
                    weatherSet.Time = prevTime + 1;
                }
                prevTime = weatherSet.Time;

                // Check settings
                if (weatherSet is WeatherSettingOvercast)
                {
                    WeatherSettingOvercast thisOvercast = weatherSet as WeatherSettingOvercast;
                    CheckValue(ref thisOvercast.Overcast, true, 0, 100, acttime, "Overcast");
                    CheckValue(ref thisOvercast.OvercastVariation, true, 0, 100, acttime, "Overcast Variation");
                    CheckValue(ref thisOvercast.OvercastRateOfChange, true, 0, 1, acttime, "Overcast Rate of Change");
                    CheckValue(ref thisOvercast.OvercastVisibilityM, false, 1000, 60000, acttime, "Overcast Visibility");
                }
                else if (weatherSet is WeatherSettingPrecipitation)
                {
                    WeatherSettingPrecipitation thisPrecipitation = weatherSet as WeatherSettingPrecipitation;

                    // Clear spell
                    CheckValue(ref thisPrecipitation.Overcast, true, 0, 100, acttime, "Overcast");
                    CheckValue(ref thisPrecipitation.OvercastVariation, true, 0, 100, acttime, "Overcast Variation");
                    CheckValue(ref thisPrecipitation.OvercastRateOfChange, true, 0, 1, acttime, "Overcast Rate of Change");
                    CheckValue(ref thisPrecipitation.OvercastVisibilityM, false, 1000, 60000, acttime, "Overcast Visibility");

                    // Precipitation
                    CheckValue(ref thisPrecipitation.PrecipitationDensity, true, 0, 1, acttime, "Precipitation Density");
                    CheckValue(ref thisPrecipitation.PrecipitationVariation, true, 0, 1, acttime, "Precipitation Variation");
                    CheckValue(ref thisPrecipitation.PrecipitationRateOfChange, true, 0, 1, acttime, "Precipitation Rate Of Change");
                    CheckValue(ref thisPrecipitation.PrecipitationProbability, true, 0, 100, acttime, "Precipitation Probability");
                    CheckValue(ref thisPrecipitation.PrecipitationSpread, false, 1, 1000, acttime, "Precipitation Spread");
                    CheckValue(ref thisPrecipitation.PrecipitationVisibilityAtMinDensityM, false, 100, thisPrecipitation.OvercastVisibilityM, acttime, "Precipitation Visibility At Min Density");
                    CheckValue(ref thisPrecipitation.PrecipitationVisibilityAtMaxDensityM, false, 100, thisPrecipitation.PrecipitationVisibilityAtMinDensityM, acttime, "Precipitation Visibility At Max Density");

                    // Build up
                    CheckValue(ref thisPrecipitation.OvercastPrecipitationStart, true, thisPrecipitation.Overcast, 100, acttime, "Overcast Precipitation Start");
                    CheckValue(ref thisPrecipitation.OvercastBuildUp, true, 0, 1, acttime, "Overcast Build Up");
                    CheckValue(ref thisPrecipitation.PrecipitationStartPhaseS, false, 30, 240, acttime, "Precipitation Start Phase");

                    // Dispersion
                    CheckValue(ref thisPrecipitation.OvercastDispersion, true, 0, 1, acttime, "Overcast Dispersion");
                    CheckValue(ref thisPrecipitation.PrecipitationEndPhaseS, false, 30, 360, acttime, "Precipitation End Phase");
                }
                else if (weatherSet is WeatherSettingFog)
                {
                    WeatherSettingFog thisFog = weatherSet as WeatherSettingFog;
                    CheckValue(ref thisFog.FogOvercast, true, 0, 100, acttime, "Fog Overcast");
                    CheckValue(ref thisFog.FogSetTimeS, false, 300, 3600, acttime, "Fog Set Time");
                    CheckValue(ref thisFog.FogLiftTimeS, false, 300, 3600, acttime, "Fog Lift Time");
                    CheckValue(ref thisFog.FogVisibilityM, false, 10, 20000, acttime, "Fog Visibility");
                }
            }
        }

        // Check value, set random value if allowed and value not set
        void CheckValue(ref float setValue, bool randomize, float minValue, float maxValue, TimeSpan acttime, string description)
        {
            // Overcast
            if (setValue < 0 && randomize)
            {
                setValue = Viewer.Random.Next((int)maxValue * 100) / 100;  // ensure there is a value if range is 0 - 1
            }
            else
            {
                float correctedValue = MathHelper.Clamp(setValue, minValue, maxValue);
                if (correctedValue != setValue)
                {
                    Trace.TraceInformation("Invalid value for {0} for weather at {1} : {2}; value must be between {3} and {4}, clamped to {5}",
                        description, acttime.ToString(), setValue, minValue, maxValue, correctedValue);
                    setValue = correctedValue;
                }
            }
        }

        // Set initial weather parameters
        void SetInitialWeatherParameters(double realTime)
        {
            Time = (float)realTime;

            // Find last valid weather change
            AWActiveIndex = 0;
            var passedTime = false;

            if (weatherDetails.Count == 0)
                return;

            for (var iIndex = 1; iIndex < weatherDetails.Count && !passedTime; iIndex++)
            {
                if (weatherDetails[iIndex].Time > Time)
                {
                    passedTime = true;
                    AWActiveIndex = iIndex - 1;
                }
            }

            // Get last weather
#if DEBUG_AUTOWEATHER
            Trace.TraceInformation("Initial active weather : {0}", AWActiveIndex);
#endif
            WeatherSetting lastWeather = weatherDetails[AWActiveIndex];

            AWNextChangeTime = AWActiveIndex < (weatherDetails.Count - 1) ? weatherDetails[AWActiveIndex + 1].Time : (24 * 3600);
            int nextIndex = AWActiveIndex < (weatherDetails.Count - 1) ? AWActiveIndex + 1 : -1;

            // Fog
            if (lastWeather is WeatherSettingFog)
            {
                WeatherSettingFog lastWeatherFog = lastWeather as WeatherSettingFog;
                float actualLiftingTime = (0.9f * lastWeatherFog.FogLiftTimeS) + ((float)Viewer.Random.Next(10) / 100 * lastWeatherFog.FogLiftTimeS); // defined time +- 10%
                AWFogLiftTime = AWNextChangeTime - actualLiftingTime;

                // Check if fog is allready lifting
                if ((float)realTime > AWFogLiftTime && nextIndex > 1)
                {
                    float reqVisibility = GetWeatherVisibility(weatherDetails[nextIndex]);
                    float remainingFactor = ((float)realTime - AWNextChangeTime + actualLiftingTime) / actualLiftingTime;
                    AWActualVisibility = lastWeatherFog.FogVisibilityM + (remainingFactor * remainingFactor * (reqVisibility - lastWeatherFog.FogVisibilityM));
                    AWOvercastCloudcover = lastWeatherFog.FogOvercast / 100;
                }
                else
                {
                    StartFog(lastWeatherFog, (float)realTime, AWActiveIndex);
                }
            }

            // Precipitation
            else if (lastWeather is WeatherSettingPrecipitation)
            {
                WeatherSettingPrecipitation lastWeatherPrecipitation = lastWeather as WeatherSettingPrecipitation;
                StartPrecipitation(lastWeatherPrecipitation, (float)realTime, true);
            }

            // Cloudcover
            else if (lastWeather is WeatherSettingOvercast)
            {
                WeatherSettingOvercast lastWeatherOvercast = lastWeather as WeatherSettingOvercast;
                AWOvercastCloudcover = Math.Max(0, Math.Min(1, (lastWeatherOvercast.Overcast / 100) +
                    ((float)Viewer.Random.Next((int)(-0.5f * lastWeatherOvercast.OvercastVariation), (int)(0.5f * lastWeatherOvercast.OvercastVariation)) / 100)));
                AWActualVisibility = Weather.SceneryFogDistance_Mix = lastWeatherOvercast.OvercastVisibilityM;

#if DEBUG_AUTOWEATHER
                Trace.TraceInformation("Visibility : {0}", Weather.FogDistance);
#endif
            }

            // Set system weather parameters
            Viewer.SoundProcess.RemoveSoundSources(this);
            Viewer.Simulator.WeatherType = AWPrecipitationActiveType;

            switch (AWPrecipitationActiveType)
            {
                case WeatherType.Rain:
                    Weather.PricipitationIntensityPPSPM2 = AWPrecipitationActualPPSPM2;
                    Weather.OvercastFactor = AWOvercastCloudcover;
                    Weather.SceneryFogDistance_Mix = AWActualVisibility;
                    Viewer.SoundProcess.AddSoundSources(this, RainSound);
                    foreach (var soundSource in RainSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
#if DEBUG_AUTOWEATHER
                    Trace.TraceInformation("Weather type RAIN");
#endif
                    break;

                case WeatherType.Snow:
                    Weather.PricipitationIntensityPPSPM2 = AWPrecipitationActualPPSPM2;
                    Weather.OvercastFactor = AWOvercastCloudcover;
                    Weather.SceneryFogDistance_Mix = AWActualVisibility;
                    Viewer.SoundProcess.AddSoundSources(this, SnowSound);
                    foreach (var soundSource in SnowSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
#if DEBUG_AUTOWEATHER
                    Trace.TraceInformation("Weather type SNOW");
#endif
                    break;

                default:
                    Weather.PricipitationIntensityPPSPM2 = 0;
                    Viewer.SoundProcess.AddSoundSources(this, ClearSound);
                    Weather.OvercastFactor = AWOvercastCloudcover;
                    Weather.SceneryFogDistance_Mix = AWActualVisibility;
#if DEBUG_AUTOWEATHER
                    Trace.TraceInformation("Weather type CLEAR");
#endif
                    break;
            }

#if DEBUG_AUTOWEATHER
            Trace.TraceInformation("Overcast : {0}\nPrecipitation : {1}\n Visibility : {2}",
                Weather.OvercastFactor, Weather.PricipitationIntensityPPSPM2, Weather.FogDistance);
#endif
        }

        [CallOnThread("Updater")]
        public override void Update(ElapsedTime elapsedTime)
        {
            // Not client and weather auto mode
            Time += elapsedTime.ClockSeconds;
            var fogActive = false;

            if (weatherDetails.Count == 0)
                return;

            WeatherSetting lastWeather = weatherDetails[AWActiveIndex];
            int nextIndex = AWActiveIndex < (weatherDetails.Count - 1) ? AWActiveIndex + 1 : -1;
            fogActive = false;

            // Check for fog
            if (lastWeather is WeatherSettingFog)
            {
                WeatherSettingFog lastWeatherFog = lastWeather as WeatherSettingFog;
                CalculateFog(lastWeatherFog, nextIndex);
                fogActive = true;

                // If fog has lifted, change to next sequence
                if (Time > (AWNextChangeTime - lastWeatherFog.FogLiftTimeS) && AWActualVisibility >= 19999 && AWActiveIndex < (weatherDetails.Count - 1))
                {
                    fogActive = false;
                    AWNextChangeTime = Time - 1;  // Force change to next weather
                }
            }

            // Check for precipitation
            else if (lastWeather is WeatherSettingPrecipitation)
            {
                WeatherSettingPrecipitation lastWeatherPrecipitation = lastWeather as WeatherSettingPrecipitation;

                // Precipitation not active
                if (AWPrecipitationActiveType == WeatherType.Clear)
                {
                    // If beyond start of next spell start precipitation
                    if (Time > AWPrecipitationNextSpell)
                    {
                        // If cloud has build up
                        if (AWOvercastCloudcover >= (lastWeatherPrecipitation.OvercastPrecipitationStart / 100))
                        {
                            StartPrecipitationSpell(lastWeatherPrecipitation, AWNextChangeTime);
                            CalculatePrecipitation(lastWeatherPrecipitation, elapsedTime);
                        }
                        // Build up cloud
                        else
                        {
                            AWOvercastCloudcover = CalculateOvercast(lastWeatherPrecipitation.OvercastPrecipitationStart, 0, lastWeatherPrecipitation.OvercastBuildUp, elapsedTime);
                        }
                    }
                    // Set overcast and visibility
                    else
                    {
                        AWOvercastCloudcover = CalculateOvercast(lastWeatherPrecipitation.Overcast, lastWeatherPrecipitation.OvercastVariation, lastWeatherPrecipitation.OvercastRateOfChange, elapsedTime);
                        if (Weather.SceneryFogDistance_Mix > lastWeatherPrecipitation.OvercastVisibilityM)
                        {
                            AWActualVisibility = Weather.SceneryFogDistance_Mix - (40 * elapsedTime.RealSeconds); // reduce visibility by 40 m/s
                        }
                        else if (Weather.SceneryFogDistance_Mix < lastWeatherPrecipitation.OvercastVisibilityM)
                        {
                            AWActualVisibility = Weather.SceneryFogDistance_Mix + (40 * elapsedTime.RealSeconds); // increase visibility by 40 m/s
                        }
                    }
                }
                // Active precipitation
                // If beyond end of spell: decrease densitity, if density below minimum threshold stop precipitation
                else if (Time > AWPrecipitationEndSpell)
                {
                    StopPrecipitationSpell(lastWeatherPrecipitation, elapsedTime);
                    // if density dropped under min threshold precipitation has ended
                    if (AWPrecipitationActualPPSPM2 <= PrecipitationViewer.MinIntensityPPSPM2)
                    {
                        AWPrecipitationActiveType = WeatherType.Clear;
#if DEBUG_AUTOWEATHER
                        Trace.TraceInformation("Start of clear spell, duration : {0}", (AWPrecipitationNextSpell - Time));
                        TimeSpan wt = new TimeSpan((long)(AWPrecipitationNextSpell * 10000000));
                        Trace.TraceInformation("Next spell : {0}", wt.ToString());
#endif                    
                    }
                }
                // Active precipitation: set density and related visibility
                else
                {
                    CalculatePrecipitation(lastWeatherPrecipitation, elapsedTime);
                }
            }
            // Clear
            else if (lastWeather is WeatherSettingOvercast)
            {
                WeatherSettingOvercast lastWeatherOvercast = lastWeather as WeatherSettingOvercast;
                AWOvercastCloudcover = CalculateOvercast(lastWeatherOvercast.Overcast, lastWeatherOvercast.OvercastVariation, lastWeatherOvercast.OvercastRateOfChange, elapsedTime);
                if (AWActualVisibility > lastWeatherOvercast.OvercastVisibilityM)
                {
                    AWActualVisibility = Math.Max(lastWeatherOvercast.OvercastVisibilityM, AWActualVisibility - (40 * elapsedTime.RealSeconds)); // reduce visibility by 40 m/s
                }
                else if (AWActualVisibility < lastWeatherOvercast.OvercastVisibilityM)
                {
                    AWActualVisibility = Math.Min(lastWeatherOvercast.OvercastVisibilityM, AWActualVisibility + (40 * elapsedTime.RealSeconds)); // increase visibility by 40 m/s
                }
            }

            // Set weather parameters
            Viewer.SoundProcess.RemoveSoundSources(this);
            Viewer.Simulator.WeatherType = AWPrecipitationActiveType;

            switch (AWPrecipitationActiveType)
            {
                case WeatherType.Rain:
                    Weather.PricipitationIntensityPPSPM2 = AWPrecipitationActualPPSPM2;
                    Weather.OvercastFactor = AWOvercastCloudcover;
                    Weather.SceneryFogDistance_Mix = AWActualVisibility;
                    Viewer.SoundProcess.AddSoundSources(this, RainSound);
                    foreach (var soundSource in RainSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
                    break;

                case WeatherType.Snow:
                    Weather.PricipitationIntensityPPSPM2 = AWPrecipitationActualPPSPM2;
                    Weather.OvercastFactor = AWOvercastCloudcover;
                    Weather.SceneryFogDistance_Mix = AWActualVisibility;
                    Viewer.SoundProcess.AddSoundSources(this, SnowSound);
                    foreach (var soundSource in SnowSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
                    break;

                default:
                    Weather.PricipitationIntensityPPSPM2 = 0;
                    Viewer.SoundProcess.AddSoundSources(this, ClearSound);
                    Weather.OvercastFactor = AWOvercastCloudcover;
                    Weather.SceneryFogDistance_Mix = AWActualVisibility;
                    break;
            }

            // Check for change in required weather
            // Time to change but no change after midnight and further weather available
            if (Time < 24 * 3600 && Time > AWNextChangeTime && AWActiveIndex < (weatherDetails.Count - 1))
            {
                // If precipitation still active or fog not lifted, postpone change by one minute
                if (AWPrecipitationActiveType != WeatherType.Clear || fogActive)
                {
                    AWNextChangeTime += 60;
                }
                else
                {
                    // Set final values of last weather
                    lastWeather.GenOvercast = Weather.OvercastFactor;
                    lastWeather.GenVisibility = AWLastVisibility = Weather.SceneryFogDistance_Mix;
                    //lastWeater.AWGenWind = ?? // TODO

                    AWActiveIndex++;
                    AWNextChangeTime = AWActiveIndex < (weatherDetails.Count - 2) ? weatherDetails[AWActiveIndex + 1].Time : 24 * 3600;

#if DEBUG_AUTOWEATHER
                    Trace.TraceInformation("Weather change : index {0}, type {1}", AWActiveIndex, weatherDetails[AWActiveIndex].GetType().ToString());
#endif                    

                    WeatherSetting nextWeather = weatherDetails[AWActiveIndex];
                    if (nextWeather is WeatherSettingFog)
                    {
                        StartFog(nextWeather as WeatherSettingFog, Time, AWActiveIndex);
                    }
                    else if (nextWeather is WeatherSettingPrecipitation)
                    {
                        StartPrecipitation(nextWeather as WeatherSettingPrecipitation, Time, false);
                    }
                }
            }
        }

        float GetWeatherVisibility(WeatherSetting weatherDetail)
        {
            float nextVisibility = Weather.SceneryFogDistance_Mix; // Present visibility
            if (weatherDetail is WeatherSettingFog)
            {
                WeatherSettingFog weatherFog = weatherDetail as WeatherSettingFog;
                nextVisibility = weatherFog.FogVisibilityM;
            }
            else if (weatherDetail is WeatherSettingOvercast)
            {
                WeatherSettingOvercast weatherOvercast = weatherDetail as WeatherSettingOvercast;
                nextVisibility = weatherOvercast.OvercastVisibilityM;
            }
            else if (weatherDetail is WeatherSettingPrecipitation)
            {
                WeatherSettingPrecipitation weatherPrecipitation = weatherDetail as WeatherSettingPrecipitation;
                nextVisibility = weatherPrecipitation.OvercastVisibilityM;
            }
            return nextVisibility;
        }

        void StartFog(WeatherSettingFog lastWeatherFog, float startTime, int activeIndex)
        {
            // Fog fully set or fog at start of day
            if (startTime > (lastWeatherFog.Time + lastWeatherFog.FogSetTimeS) || activeIndex == 0)
            {
                AWActualVisibility = lastWeatherFog.FogVisibilityM;
            }
            // Fog still setting
            else
            {
                float remainingFactor = (startTime - lastWeatherFog.Time + lastWeatherFog.FogSetTimeS) / lastWeatherFog.FogSetTimeS;
                AWActualVisibility = MathHelper.Clamp(AWActualVisibility - (remainingFactor * remainingFactor * (AWActualVisibility - lastWeatherFog.FogVisibilityM)), lastWeatherFog.FogVisibilityM, AWActualVisibility);
            }
        }

        void CalculateFog(WeatherSettingFog lastWeatherFog, int nextIndex)
        {
            if (AWFogLiftTime > 0 && Time > AWFogLiftTime && nextIndex > 0) // fog is lifting
            {
                float reqVisibility = GetWeatherVisibility(weatherDetails[nextIndex]);
                float remainingFactor = (Time - weatherDetails[nextIndex].Time + lastWeatherFog.FogLiftTimeS) / lastWeatherFog.FogLiftTimeS;
                AWActualVisibility = lastWeatherFog.FogVisibilityM + (remainingFactor * remainingFactor * (reqVisibility - lastWeatherFog.FogVisibilityM));
                AWOvercastCloudcover = lastWeatherFog.FogOvercast / 100;
            }
            else if (AWActualVisibility > lastWeatherFog.FogVisibilityM)
            {
                float remainingFactor = (Time - lastWeatherFog.Time + lastWeatherFog.FogSetTimeS) / lastWeatherFog.FogSetTimeS;
                AWActualVisibility = MathHelper.Clamp(AWLastVisibility - (remainingFactor * remainingFactor * (AWLastVisibility - lastWeatherFog.FogVisibilityM)), lastWeatherFog.FogVisibilityM, AWLastVisibility);
            }
        }

        void StartPrecipitation(WeatherSettingPrecipitation lastWeatherPrecipitation, float startTime, bool allowImmediateStart)
        {
            AWPrecipitationRequiredType = lastWeatherPrecipitation.PrecipitationType;

            // Determine actual duration of precipitation
            float maxDuration = AWNextChangeTime - weatherDetails[AWActiveIndex].Time;
            AWPrecipitationTotalDuration = (float)maxDuration * (lastWeatherPrecipitation.PrecipitationProbability / 100f);  // nominal value
            AWPrecipitationTotalDuration = (0.9f + ((float)Viewer.Random.Next(20) / 20)) * AWPrecipitationTotalDuration; // randomized value, +- 10% 
            AWPrecipitationTotalDuration = Math.Min(AWPrecipitationTotalDuration, maxDuration); // but never exceeding maximum duration
            AWPrecipitationNextSpell = lastWeatherPrecipitation.Time; // set start of spell to start of weather change

            // Determine spread: no. of periods with precipitation (no. of showers)
            if (lastWeatherPrecipitation.PrecipitationSpread == 1)
            {
                AWPrecipitationTotalSpread = 1;
            }
            else
            {
                AWPrecipitationTotalSpread = Math.Max(1, (int)((0.9f + ((float)Viewer.Random.Next(20) / 20)) * lastWeatherPrecipitation.PrecipitationSpread));
                if ((AWPrecipitationTotalDuration / AWPrecipitationTotalSpread) < 900) // Length of spell at least 15 mins
                {
                    AWPrecipitationTotalSpread = (int)(AWPrecipitationTotalDuration / 900);
                }
            }

            // Determine actual precipitation state - only if immediate start allowed
            bool precipitationActive = allowImmediateStart && Viewer.Random.Next(100) >= lastWeatherPrecipitation.PrecipitationProbability;

#if DEBUG_AUTOWEATHER
            Trace.TraceInformation("Precipitation active on start : {0}", precipitationActive.ToString());
#endif                    

            // Determine total remaining time as well as remaining periods, based on start/end time and present time
            // This is independent from actual precipitation state

            if (AWPrecipitationTotalSpread > 1)
            {
                AWPrecipitationTotalDuration = ((float)((AWNextChangeTime - startTime) / (AWNextChangeTime - weatherDetails[AWActiveIndex].Time))) * AWPrecipitationTotalDuration;
                AWPrecipitationTotalSpread = (int)(((float)((AWNextChangeTime - startTime) / (AWNextChangeTime - weatherDetails[AWActiveIndex].Time))) * AWPrecipitationTotalSpread);
            }

            // Set actual details
            if (precipitationActive)
            {
                // Precipitation active: set actual details, calculate end of present spell
                int precvariation = (int)(lastWeatherPrecipitation.PrecipitationVariation * 100);
                float baseDensitiy = PrecipitationViewer.MaxIntensityPPSPM2 * lastWeatherPrecipitation.PrecipitationDensity;
                AWPrecipitationActualPPSPM2 = MathHelper.Clamp((1.0f + ((float)Viewer.Random.Next(-precvariation, precvariation) / 100)) * baseDensitiy,
                                               PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
                AWPrecipitationRequiredPPSPM2 = MathHelper.Clamp((1.0f + ((float)Viewer.Random.Next(-precvariation, precvariation) / 100)) * baseDensitiy,
                                               PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);

                // Rate of change is max. difference over random timespan between 1 and 10 mins.
                // Startphase
                float startrate = (1.75f * lastWeatherPrecipitation.PrecipitationRateOfChange) +
                                           (0.5F * Viewer.Random.Next((int)(lastWeatherPrecipitation.PrecipitationRateOfChange * 100)) / 100f);
                float spellStartPhase = Math.Min(60f + (300f * startrate), 600);
                AWPrecipitationStartRate = (AWPrecipitationRequiredPPSPM2 - AWPrecipitationActualPPSPM2) / spellStartPhase;

                // Endphase
                float endrate = (1.75f * lastWeatherPrecipitation.PrecipitationRateOfChange) +
                               (0.5F * Viewer.Random.Next((int)(lastWeatherPrecipitation.PrecipitationRateOfChange * 100)) / 100f);
                float spellEndPhase = Math.Min(60f + (300f * endrate), 600);

                float avduration = AWPrecipitationTotalDuration / AWPrecipitationTotalSpread;
                float actduration = (0.5f + ((float)Viewer.Random.Next(100) / 100)) * avduration;
                float spellEndTime = Math.Min(startTime + actduration, AWNextChangeTime);
                AWPrecipitationEndSpell = Math.Max(startTime, spellEndTime - spellEndPhase);
                // For end rate, use minimum precipitation
                AWPrecipitationEndRate = (AWPrecipitationActualPPSPM2 - PrecipitationViewer.MinIntensityPPSPM2) / spellEndPhase;
                AWPrecipitationTotalDuration -= actduration;
                AWPrecipitationTotalSpread -= 1;

                // Calculate length of clear period and start of next spell
                if (AWPrecipitationTotalDuration > 0 && AWPrecipitationTotalSpread > 0)
                {
                    float avclearspell = (AWNextChangeTime - startTime - AWPrecipitationTotalDuration) / AWPrecipitationTotalSpread;
                    AWPrecipitationNextSpell = spellEndTime + ((0.9f + (Viewer.Random.Next(200) / 1000f)) * avclearspell);
                }
                else
                {
                    AWPrecipitationNextSpell = AWNextChangeTime + 1; // Set beyond next weather such that it never occurs
                }

                // set active values
                AWPrecipitationActiveType = lastWeatherPrecipitation.PrecipitationType;
                AWOvercastCloudcover = lastWeatherPrecipitation.OvercastPrecipitationStart / 100;  // Fixed cloudcover during precipitation
                AWActualVisibility = lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM + (float)(Math.Sqrt(AWPrecipitationActualPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2) *
                    (lastWeatherPrecipitation.PrecipitationVisibilityAtMaxDensityM - lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM));
                AWLastVisibility = lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM; // Fix last visibility to visibility at minimum density
            }
            else
            // If presently not active, set start of next spell
            {
                if (AWPrecipitationTotalSpread < 1)
                {
                    AWPrecipitationNextSpell = -1;
                }
                else
                {
                    int clearSpell = (int)((AWNextChangeTime - startTime - AWPrecipitationTotalDuration) / AWPrecipitationTotalSpread);
                    AWPrecipitationNextSpell = clearSpell > 0 ? startTime + Viewer.Random.Next(clearSpell) : startTime;

                    if (allowImmediateStart)
                    {
                        AWOvercastCloudcover = lastWeatherPrecipitation.Overcast / 100;
                        AWActualVisibility = lastWeatherPrecipitation.OvercastVisibilityM;
                    }

#if DEBUG_AUTOWEATHER
                    TimeSpan wt = new TimeSpan((long)(AWPrecipitationNextSpell * 10000000));
                    Trace.TraceInformation("Next spell : {0}", wt.ToString());
#endif                    
                }

                AWPrecipitationActiveType = WeatherType.Clear;
            }
        }

        void StartPrecipitationSpell(WeatherSettingPrecipitation lastWeatherPrecipitation, float nextWeatherTime)
        {
            int precvariation = (int)(lastWeatherPrecipitation.PrecipitationVariation * 100);
            float baseDensitiy = PrecipitationViewer.MaxIntensityPPSPM2 * lastWeatherPrecipitation.PrecipitationDensity;
            AWPrecipitationActiveType = AWPrecipitationRequiredType;
            AWPrecipitationActualPPSPM2 = PrecipitationViewer.MinIntensityPPSPM2;
            AWPrecipitationRequiredPPSPM2 = MathHelper.Clamp((1.0f + ((float)Viewer.Random.Next(-precvariation, precvariation) / 100)) * baseDensitiy,
                                           PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
            AWLastVisibility = Weather.SceneryFogDistance_Mix;

            // Rate of change at start is max. difference over defined time span +- 10%, scaled between 1/2 and 4 mins
            float startphase = MathHelper.Clamp(lastWeatherPrecipitation.PrecipitationStartPhaseS * (0.9f + (Viewer.Random.Next(100) / 1000)), 30, 240);
            AWPrecipitationStartRate = (AWPrecipitationRequiredPPSPM2 - AWPrecipitationActualPPSPM2) / startphase;
            AWPrecipitationRateOfChangePPSPM2PS = AWPrecipitationStartRate;

            // Rate of change at end is max. difference over defined time span +- 10%, scaled between 1/2 and 6 mins
            float endphase = MathHelper.Clamp(lastWeatherPrecipitation.PrecipitationEndPhaseS * (0.9f + (Viewer.Random.Next(100) / 1000)), 30, 360);
            AWPrecipitationEndRate = (AWPrecipitationRequiredPPSPM2 - AWPrecipitationActualPPSPM2) / endphase;

            // Calculate end of spell and start of next spell
            if (AWPrecipitationTotalSpread > 1)
            {
                float avduration = AWPrecipitationTotalDuration / AWPrecipitationTotalSpread;
                float actduration = (0.5f + ((float)Viewer.Random.Next(100) / 100)) * avduration;
                float spellEndTime = Math.Min(Time + actduration, AWNextChangeTime);
                AWPrecipitationEndSpell = Math.Max(Time, spellEndTime - endphase);

                AWPrecipitationTotalDuration -= actduration;
                AWPrecipitationTotalSpread -= 1;

                int clearSpell = (int)((nextWeatherTime - spellEndTime - AWPrecipitationTotalDuration) / AWPrecipitationTotalSpread);
                AWPrecipitationNextSpell = spellEndTime + 60f; // Always a minute between spells
                AWPrecipitationNextSpell = clearSpell > 0 ? AWPrecipitationNextSpell + Viewer.Random.Next(clearSpell) : AWPrecipitationNextSpell;
            }
            else
            {
                AWPrecipitationEndSpell = Math.Max(Time, nextWeatherTime - endphase);
            }

#if DEBUG_AUTOWEATHER
            Trace.TraceInformation("Start next spell, duration : {0} , start phase : {1} , end phase {2}, density {3} (of max. {4}) , rate of change : {5} - {6} - {7}",
                                    (AWPrecipitationEndSpell - Time), startphase, endphase, AWPrecipitationRequiredPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2, 
                                    AWPrecipitationRateOfChangePPSPM2PS, AWPrecipitationStartRate, AWPrecipitationEndRate);
#endif
        }

        void CalculatePrecipitation(WeatherSettingPrecipitation lastWeatherPrecipitation, ElapsedTime elapsedTime)
        {
            if (AWPrecipitationActualPPSPM2 < AWPrecipitationRequiredPPSPM2)
            {
                AWPrecipitationActualPPSPM2 = Math.Min(AWPrecipitationRequiredPPSPM2, AWPrecipitationActualPPSPM2 + (AWPrecipitationRateOfChangePPSPM2PS * elapsedTime.RealSeconds));
            }
            else if (AWPrecipitationActualPPSPM2 > AWPrecipitationRequiredPPSPM2)
            {
                AWPrecipitationActualPPSPM2 = Math.Max(AWPrecipitationRequiredPPSPM2, AWPrecipitationActualPPSPM2 - (AWPrecipitationRateOfChangePPSPM2PS * elapsedTime.RealSeconds));
            }
            else
            {
                AWPrecipitationRateOfChangePPSPM2PS = lastWeatherPrecipitation.PrecipitationRateOfChange / 120 * (PrecipitationViewer.MaxIntensityPPSPM2 - PrecipitationViewer.MinIntensityPPSPM2);
                int precvariation = (int)(lastWeatherPrecipitation.PrecipitationVariation * 100);
                float baseDensitiy = PrecipitationViewer.MaxIntensityPPSPM2 * lastWeatherPrecipitation.PrecipitationDensity;
                AWPrecipitationRequiredPPSPM2 = MathHelper.Clamp((1.0f + ((float)Viewer.Random.Next(-precvariation, precvariation) / 100)) * baseDensitiy,
                                               PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
#if DEBUG_AUTOWEATHER
                Trace.TraceInformation("New density : {0}", AWPrecipitationRequiredPPSPM2);
#endif

                AWLastVisibility = lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM; // reach required density, so from now on visibility is determined by density
            }

            // Calculate visibility: use last visibility which is either visibility at start of precipitation (at start of spell) or visibility at minimum density (after reaching required density)
            float reqVisibility = lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM + ((float)Math.Sqrt(AWPrecipitationRequiredPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2) *
                (lastWeatherPrecipitation.PrecipitationVisibilityAtMaxDensityM - lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM));
            AWActualVisibility = AWLastVisibility + (float)(Math.Sqrt(AWPrecipitationActualPPSPM2 / AWPrecipitationRequiredPPSPM2) *
                (reqVisibility - AWLastVisibility));
        }

        void StopPrecipitationSpell(WeatherSettingPrecipitation lastWeatherPrecipitation, ElapsedTime elapsedTime)
        {
            AWPrecipitationActualPPSPM2 = Math.Max(PrecipitationViewer.MinIntensityPPSPM2, AWPrecipitationActualPPSPM2 - (AWPrecipitationEndRate * elapsedTime.RealSeconds));
            AWActualVisibility = AWLastVisibility +
                (float)(Math.Sqrt(AWPrecipitationActualPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2) * (lastWeatherPrecipitation.PrecipitationVisibilityAtMaxDensityM - AWLastVisibility));
            AWOvercastCloudcover = CalculateOvercast(lastWeatherPrecipitation.Overcast, 0, lastWeatherPrecipitation.OvercastDispersion, elapsedTime);
        }

        float CalculateOvercast(float requiredOvercast, float overcastVariation, float overcastRateOfChange, ElapsedTime elapsedTime)
        {
            float requiredOvercastFactor = requiredOvercast / 100f;
            AWOvercastCloudRateOfChangepS = overcastRateOfChange == 0
                ? (float)Viewer.Random.Next(50) / (100 * 300) * (0.8f + ((float)Viewer.Random.Next(100) / 250))
                : overcastRateOfChange / 300 * (0.8f + ((float)Viewer.Random.Next(100) / 250));

            if (AWOvercastCloudcover < requiredOvercastFactor)
            {
                float newOvercast = Math.Min(requiredOvercastFactor, Weather.OvercastFactor + (AWOvercastCloudRateOfChangepS * elapsedTime.RealSeconds));
                return newOvercast;
            }
            else if (Weather.OvercastFactor > requiredOvercastFactor)
            {
                float newOvercast = Math.Max(requiredOvercastFactor, Weather.OvercastFactor - (AWOvercastCloudRateOfChangepS * elapsedTime.RealSeconds));
                return newOvercast;
            }
            else
            {
                float newOvercast = Math.Max(0, Math.Min(1, requiredOvercastFactor + ((float)Viewer.Random.Next((int)(-0.5f * overcastVariation), (int)(0.5f * overcastVariation)) / 100)));
                return newOvercast;
            }
        }

        public override void SaveWeatherParameters(BinaryWriter outf)
        {
            // Set indication to automatic weather
            outf.Write(1);

            // Save input details
            foreach (WeatherSetting autoweather in weatherDetails)
            {
                if (autoweather is WeatherSettingFog)
                {
                    WeatherSettingFog autofog = autoweather as WeatherSettingFog;
                    autofog.Save(outf);
                }
                else if (autoweather is WeatherSettingPrecipitation)
                {
                    WeatherSettingPrecipitation autoprec = autoweather as WeatherSettingPrecipitation;
                    autoprec.Save(outf);
                }
                else if (autoweather is WeatherSettingOvercast)
                {
                    WeatherSettingOvercast autoovercast = autoweather as WeatherSettingOvercast;
                    autoovercast.Save(outf);
                }
            }
            outf.Write("end");

            outf.Write(AWActiveIndex);
            outf.Write(AWNextChangeTime);

            outf.Write(AWActualVisibility);
            outf.Write(AWLastVisibility);
            outf.Write(AWFogLiftTime);
            outf.Write(AWFogChangeRateMpS);

            outf.Write((int)AWPrecipitationActiveType);
            outf.Write(AWPrecipitationActualPPSPM2);
            outf.Write(AWPrecipitationRequiredPPSPM2);
            outf.Write(AWPrecipitationRateOfChangePPSPM2PS);
            outf.Write(AWPrecipitationTotalDuration);
            outf.Write(AWPrecipitationTotalSpread);
            outf.Write(AWPrecipitationEndSpell);
            outf.Write(AWPrecipitationNextSpell);
            outf.Write(AWPrecipitationStartRate);
            outf.Write(AWPrecipitationEndRate);

            outf.Write(AWOvercastCloudcover);
            outf.Write(AWOvercastCloudRateOfChangepS);

            outf.Write(Weather.OvercastFactor);
            outf.Write(Weather.SceneryFogDistance_Mix);
            outf.Write(Weather.PricipitationIntensityPPSPM2);
        }

        public override void RestoreWeatherParameters(BinaryReader inf)
        {
            int weathercontroltype = inf.ReadInt32();

            // Restoring wrong type of weather - abort
            if (weathercontroltype != 1)
            {
                Trace.TraceError(Simulator.Catalog.GetString("Restoring wrong weather type : trying to restore user controlled weather but save contains dynamic weather"));
            }

            weatherDetails.Clear();

            string readtype = inf.ReadString();
            bool endread = false;

            while (!endread)
            {
                if (String.Equals(readtype, "fog"))
                {
                    WeatherSettingFog autofog = new WeatherSettingFog(inf);
                    weatherDetails.Add(autofog);
                }
                else if (String.Equals(readtype, "precipitation"))
                {
                    WeatherSettingPrecipitation autoprec = new WeatherSettingPrecipitation(inf);
                    weatherDetails.Add(autoprec);
                }
                else if (String.Equals(readtype, "overcast"))
                {
                    WeatherSettingOvercast autoovercast = new WeatherSettingOvercast(inf);
                    weatherDetails.Add(autoovercast);
                }
                else if (String.Equals(readtype, "end"))
                {
                    endread = true;
                }
                else
                {
                    // Error - nothing to report it here
                    endread = true;
                }

                if (!endread)
                {
                    readtype = inf.ReadString();
                }
            }

            AWActiveIndex = inf.ReadInt32();
            AWNextChangeTime = inf.ReadSingle();

            AWActualVisibility = inf.ReadSingle();
            AWLastVisibility = inf.ReadSingle();
            AWFogLiftTime = inf.ReadSingle();
            AWFogChangeRateMpS = inf.ReadSingle();

            AWPrecipitationActiveType = (WeatherType)inf.ReadInt32();
            AWPrecipitationActualPPSPM2 = inf.ReadSingle();
            AWPrecipitationRequiredPPSPM2 = inf.ReadSingle();
            AWPrecipitationRateOfChangePPSPM2PS = inf.ReadSingle();
            AWPrecipitationTotalDuration = inf.ReadSingle();
            AWPrecipitationTotalSpread = inf.ReadInt32();
            AWPrecipitationEndSpell = inf.ReadSingle();
            AWPrecipitationNextSpell = inf.ReadSingle();
            AWPrecipitationStartRate = inf.ReadSingle();
            AWPrecipitationEndRate = inf.ReadSingle();

            AWOvercastCloudcover = inf.ReadSingle();
            AWOvercastCloudRateOfChangepS = inf.ReadSingle();

            Weather.OvercastFactor = inf.ReadSingle();

            Weather.PricipitationIntensityPPSPM2 = inf.ReadSingle();

            Time = (float)Viewer.Simulator.ClockTime;
        }
    }
}
