// COPYRIGHT 2010, 2011, 2014, 2015 by the Open Rails project.
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

using Microsoft.Xna.Framework;

//using SharpDX;
using System;
using System.Diagnostics;

namespace Orts.Simulation
{
    public class WeatherExtension
    {
        // SunSize
        public float SunSize_Sunrise = 1.0f;
        public float SunSize_Noon    = 1.0f;
        public float SunSize_Sunset  = 1.0f;
        public float SunSize_Mix  = 1.0f;
        
        // Wind Speed and direction
        public float WindSpeed  = 0;  
        public float WindDirectionSky = 0.0f;
        
        // Overcast factor 1,2,3
        public float OvercastFactor  = 0;
        public float OvercastFactor2 = 0;
        public float OvercastFactor3 = 0;
        
        // Precipitation vectors
        public Vector3 PrecipWind1 = new Vector3( 0.0f, 0.0f, 0.0f);
        public Vector3 PrecipWind2 = new Vector3( 0.0f, 0.0f, 0.0f);
       
        // Precipitation Particlesize 
        public float ParticleSize1  = 1.5f;
        public float ParticleSize2  = 2.0f;
        
        // Sky Fog/visibility distance 
        public float SkyFogDistance_Mix     = 200.0f;
        public float SkyFogDistance_Sunrise = 500.0f;
        public float SkyFogDistance_Noon    = 500.0f;
        public float SkyFogDistance_Sunset  = 500.0f;
        public Color SkyFog_Sunrise = new Color(112,120,120,255);
        public Color SkyFog_Noon    = new Color(111,111,111,255);
        public Color SkyFog_Sunset  = new Color(120,120,110,255);
        public Color SkyFogMix      = new Color(120,120,110,255);
        
        // Scenery Fog/visibility distance 
        public float SceneryFogDistance_Mix = 210.0f;
        public float SceneryFogDistance_Sunrise = 500.0f;
        public float SceneryFogDistance_Noon    = 500.0f;
        public float SceneryFogDistance_Sunset  = 500.0f;
        public Color SceneryFogMix = new Color(120,120,110,255);
        public Color SceneryFog_Sunrise = new Color(112,120,120,255);
        public Color SceneryFog_Noon    = new Color(111,111,111,255);
        public Color SceneryFog_Sunset  = new Color(120,120,110,255);
        
        // Vegetation color control
        public float VegetationDesatuationModifier = 0;
        public float VegetationBrightnessModifier  = 0; 
        public float VegetationContrastModifier    = 0; 
        
        // Terrain Color control
        public float TerrainBrightnessModifier  = 0;
        public float TerrainDesatuationModifier = 0; 
        public float TerrainContrastModifier    = 0;

        // Daylight offset (-12h to +12h)
        public int DaylightOffset = 0;
        
        // Precipitation liquidity; =1 for rain, =0 for snow; intermediate values possible with dynamic weather;
        public float PrecipitationLiquidity  = 0;
        public float CalculatedWindDirection = 0;
        public Vector2 WindSpeedMpS = new Vector2();
        
        // Pricipitation intensity in particles per second per meter^2 (PPSPM2).
        public float PricipitationIntensityPPSPM2;
        public float WindDirection { get { return CalculatedWindDirection; } }

    }
}
