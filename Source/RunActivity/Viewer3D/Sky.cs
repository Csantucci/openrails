// COPYRIGHT 2009 - 2023 by the Open Rails project.
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using ORTS.Common;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Processes;
using Orts.Viewer3D.RollingStock.Subsystems.ETCS;

namespace Orts.Viewer3D
{
    public class SkyViewer
    {
        internal readonly SkyPrimitive   Primitive;
        internal readonly float WindSpeed;
        internal readonly float WindDirection;
        internal int MoonPhase;
        internal Vector3 SolarDirection;
        internal Vector3 LunarDirection;
        internal double Latitude; // Latitude of current route in radians. -pi/2 = south pole, 0 = equator, pi/2 = north pole.
        internal double Longitude; // Longitude of current route in radians. -pi = west of prime, 0 = prime, pi = east of prime.
        static readonly WorldLatLon WorldLatLon = new WorldLatLon();
        readonly Viewer Viewer;
        public readonly Material Material;
        readonly Vector3[] SolarPositionCache = new Vector3[72];
        readonly Vector3[] LunarPositionCache = new Vector3[72];
        readonly SkyInterpolation SkyInterpolation = new SkyInterpolation();

        public SkyViewer(Viewer viewer)
        {
            Viewer = viewer;
            Material = viewer.MaterialManager.Load("Sky");
            // Instantiate classes
            Primitive   = new SkyPrimitive(Viewer.RenderProcess);
            // Default wind speed and direction
            // TODO: We should be using Viewer.Simulator.Weather instead of our own local weather fields
            WindSpeed = 0.0f; // m/s (approx 11 mph)
            WindDirection = 14.7f; // radians (approx 270 deg, i.e. westerly)
     
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            SkyInterpolation.SetSunAndMoonDirection(ref SolarDirection, ref LunarDirection, SolarPositionCache, LunarPositionCache, Viewer.Simulator.ClockTime);

            var xnaSkyWorldLocation = Matrix.CreateTranslation(Viewer.Camera.Location * new Vector3(1, 1, -1));
            frame.AddPrimitive(Material, Primitive, RenderPrimitiveGroup.Sky, ref xnaSkyWorldLocation);
        }

        public void LoadPrep()
        {
            WorldLatLon.ConvertWTC(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location, ref Latitude, ref Longitude);

            // First time around, initialize the following items:
            SkyInterpolation.OldClockTime = Viewer.Simulator.ClockTime % 86400;
            while (SkyInterpolation.OldClockTime < 0)
            {
                SkyInterpolation.OldClockTime += 86400;
            }

            SkyInterpolation.Step1 = SkyInterpolation.Step2 = (int)(SkyInterpolation.OldClockTime / 1200);
            SkyInterpolation.Step2 = SkyInterpolation.Step2 < SkyInterpolation.MaxSteps - 1 ? SkyInterpolation.Step2 + 1 : 0; // limit to max. steps in case activity starts near midnight

            // And the rest depends on the weather (which is changeable)
            Viewer.Simulator.WeatherChanged += (sender, e) => WeatherChanged();
            WeatherChanged();
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Material.Mark();
        }

        void WeatherChanged()
        {
            // TODO: Allow setting the date from route files?
            var seasonType = (int)Viewer.Simulator.Season;
            var date = new SkyDate { OrdinalDate = Latitude >= 0 ? 82 + (seasonType * 91) : (82 + ((seasonType + 2) * 91)) % 365 };
            date.Month = 1 + (date.OrdinalDate / 30);
            date.Day = 21;
            date.Year = 2017;

            // Fill in the sun- and moon-position lookup tables
            for (var i = 0; i < SkyInterpolation.MaxSteps; i++)
            {
                SolarPositionCache[i] = SunMoonPos.SolarAngle(Latitude, Longitude, Viewer.ENVFile.Sun, (float)i / SkyInterpolation.MaxSteps, date);
                LunarPositionCache[i] = SunMoonPos.LunarAngle(Latitude, Longitude, (float)i / SkyInterpolation.MaxSteps, date);
            }

            // Phase of the moon is generated at random, but moon dog only occurs in winter
            MoonPhase = Viewer.Random.Next(8);
            if (MoonPhase == 6 && date.OrdinalDate > 45 && date.OrdinalDate < 330)
            {
                MoonPhase = 3;
            }
        }

        public struct SkyDate
        {
            public int Year;
            public int Month;
            public int Day;
            public int OrdinalDate; // Ordinal date. Range: 0 to 366.
        }
    }

    public class SkyPrimitive : RenderPrimitive
    {
        public const float RadiusM = 16000;
        public const float CloudsAltitudeM = 2000;

        public SkyElement Element;

        /*
         * The sky is formed of 3 layers (back to front):
         * - Cloud-less sky and night sky textures, blended according to time of day, and with sun effect added in (in the shader)
         * - Moon textures (phase is random)
         * - Clouds blended by overcast factor and animated by wind speed and direction
         *
         * Both the cloud-less sky and clouds use sky domes; the sky is
         * perfectly spherical, while the cloud dome is flattened and offset
         * so that it passes closer over the camera but still extends beyond
         * the horizon by the same amount.
         *
         * The sky dome is the top hemisphere of a globe, plus an extension
         * below the horizon to ensure we never get to see the edge. Both the
         * rotational (sides) and horizontal/vertical (steps) segments are
         * split so that the center angles are `DomeComponentDegrees`.
         *
         * It is important that there are enough sides for the texture mapping
         * to look good; otherwise, smooth curves will render as wavy lines.
         * Currently, testing shows 6° is the maximum reasonable angle.
         */
        const int TuneDomeComponentDegrees = 6;

        const int DomeSides = 360 / TuneDomeComponentDegrees;
        const int DomeStepsMain = 90 / TuneDomeComponentDegrees;
        const int DomeStepsExtra = 1;
        const int DomeSteps = DomeStepsMain + DomeStepsExtra;
        const int DomePrimitives = (2 * DomeSides * DomeSteps) - DomeSides;
        const int DomeVertices = 1 + (DomeSides * DomeSteps);
        const int DomeIndexes = 3 * DomePrimitives;

        const int MoonPrimitives = 2;
        const int MoonVertices = 4;
        const int MoonIndexes = 3 * MoonPrimitives;

        const int VertexCount = (2 * DomeVertices) + MoonVertices;
        const int IndexCount = DomeIndexes + MoonIndexes;

        // Calculate the height of the dome from top to bottom of extra steps (below horizon)
        static readonly float DomeHeightM = RadiusM * (float)(1 + Math.Sin(MathHelper.ToRadians(DomeStepsExtra * TuneDomeComponentDegrees)));
        static readonly float CloudsFlatness = 1 - ((RadiusM - CloudsAltitudeM) / DomeHeightM);
        static readonly float CloudsOffsetM = CloudsAltitudeM - (RadiusM * CloudsFlatness);

        readonly VertexPositionNormalTexture[] VertexList;
        readonly short[] IndexList;

        VertexBuffer VertexBuffer;
        IndexBuffer IndexBuffer;

        public SkyPrimitive(RenderProcess renderProcess)
        {
            // Initialize the vertex and index lists
            VertexList = new VertexPositionNormalTexture[VertexCount];
            IndexList = new short[IndexCount];
            var vertexIndex = 0;
            var indexIndex = 0;
            InitializeDomeVertexList(ref vertexIndex, RadiusM);
            InitializeDomeVertexList(ref vertexIndex, RadiusM, CloudsFlatness, CloudsOffsetM);
            InitializeDomeIndexList(ref indexIndex);
            InitializeMoonLists(ref vertexIndex, ref indexIndex);
            Debug.Assert(vertexIndex == VertexCount, $"Did not initialize all verticies; expected {VertexCount}, got {vertexIndex}");
            Debug.Assert(indexIndex == IndexCount, $"Did not initialize all indexes; expected {IndexCount}, got {indexIndex}");

            // Meshes have now been assembled, so put everything into vertex and index buffers
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
        }

        public enum SkyElement
        {
            Sky,
            Moon,
            Clouds,
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.Indices = IndexBuffer;

            switch (Element)
            {
                case SkyElement.Sky:
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: 0,
                        startIndex: 0,
                        primitiveCount: DomePrimitives);
                    break;
                case SkyElement.Moon:
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: DomeVertices * 2,
                        startIndex: DomeIndexes,
                        primitiveCount: MoonPrimitives);
                    break;
                case SkyElement.Clouds:
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: DomeVertices,
                        startIndex: 0,
                        primitiveCount: DomePrimitives);
                    break;
            }
        }

        void InitializeDomeVertexList(ref int index, float radius, float flatness = 1, float offset = 0)
        {
            // Single vertex at zenith
            VertexList[index].Position = new Vector3(0, (radius * flatness) + offset, 0);
            VertexList[index].Normal = Vector3.Normalize(VertexList[index].Position);
            VertexList[index].TextureCoordinate = new Vector2(0.5f, 0.5f);
            index++;

            for (var step = 1; step <= DomeSteps; step++)
            {
                var stepCos = (float)Math.Cos(MathHelper.ToRadians(90f * step / DomeStepsMain));
                var stepSin = (float)Math.Sin(MathHelper.ToRadians(90f * step / DomeStepsMain));

                var y = radius * stepCos;
                var d = radius * stepSin;

                for (var side = 0; side < DomeSides; side++)
                {
                    var sideCos = (float)Math.Cos(MathHelper.ToRadians(360f * side / DomeSides));
                    var sideSin = (float)Math.Sin(MathHelper.ToRadians(360f * side / DomeSides));

                    var x = d * sideCos;
                    var z = d * sideSin;

                    var u = 0.5f + ((float)step / DomeStepsMain * sideCos / 2);
                    var v = 0.5f + ((float)step / DomeStepsMain * sideSin / 2);

                    // Store the position, texture coordinates and normal (normalized position vector) for the current vertex
                    VertexList[index].Position = new Vector3(x, (y * flatness) + offset, z);
                    VertexList[index].Normal = Vector3.Normalize(VertexList[index].Position);
                    VertexList[index].TextureCoordinate = new Vector2(u, v);
                    index++;
                }
            }
        }

        void InitializeDomeIndexList(ref int index)
        {
            // Zenith triangles
            for (var side = 0; side < DomeSides; side++)
            {
                IndexList[index++] = 0;
                IndexList[index++] = (short)(1 + ((side + 1) % DomeSides));
                IndexList[index++] = (short)(1 + ((side + 0) % DomeSides));
            }

            for (var step = 1; step < DomeSteps; step++)
            {
                for (var side = 0; side < DomeSides; side++)
                {
                    IndexList[index++] = (short)(1 + ((step - 1) * DomeSides) + ((side + 0) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 0) * DomeSides) + ((side + 1) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 0) * DomeSides) + ((side + 0) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 1) * DomeSides) + ((side + 0) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 1) * DomeSides) + ((side + 1) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 0) * DomeSides) + ((side + 1) % DomeSides));
                }
            }
        }

        void InitializeMoonLists(ref int vertexIndex, ref int indexIndex)
        {
            // Moon vertices
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    VertexList[vertexIndex].Position = new Vector3(i, j, 0);
                    VertexList[vertexIndex].Normal = new Vector3(0, 0, 1);
                    VertexList[vertexIndex].TextureCoordinate = new Vector2(i, j);
                    vertexIndex++;
                }
            }

            // Moon indices - clockwise winding
            IndexList[indexIndex++] = 0;
            IndexList[indexIndex++] = 1;
            IndexList[indexIndex++] = 2;
            IndexList[indexIndex++] = 1;
            IndexList[indexIndex++] = 3;
            IndexList[indexIndex++] = 2;
        }

        void InitializeVertexBuffers(GraphicsDevice graphicsDevice)
        {
            VertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), VertexList.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(VertexList);
            IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), IndexCount, BufferUsage.WriteOnly);
            IndexBuffer.SetData(IndexList);
        }
    }

    // Exrail: Mix two texture2D's into one
    public class SkyMix2Texture2D 
    {
        readonly Viewer Viewer;
        public Texture2D T1,T2,T3,ReturnSkydomeTexture;
        const int Sun_NoonTime = 43200;
        readonly int Sun_RiseTime = 0, Sun_SetTime = 0;
        int NoonTimeNulled = 0, Sun_SetTimeNulled = 0;
        int CurrentTime  = 0, CurrentTimeNulled = 0;
        public float Precentage_Mix = 0;

        Color[] SunriseA;
        Color[] NoonA;
        Color[] SunsetA;

        Vector3[] MixBitmapSunrise;
        Vector3[] MixBitmapNoon;
        Vector3[] MixBitmapSunset;
        Color[]   MixReturn; 
        int Exsize = 0;

        public SkyMix2Texture2D ( Viewer viewer)
        {
            Viewer = viewer;
            Sun_RiseTime = viewer.ENVFile.SkySatellites[0].RiseTime;
            Sun_SetTime  = viewer.ENVFile.SkySatellites[0].SetTime;
            CurrentTime  = (int)Viewer.Simulator.ClockTime;

            if( CurrentTime >= 43200) 
            {   
                // After 12:00
                Sun_SetTimeNulled  = Sun_SetTime - Sun_NoonTime;
                CurrentTimeNulled  = CurrentTime - Sun_NoonTime;
                Precentage_Mix = (float) CurrentTimeNulled / Sun_SetTimeNulled * 100;
	        }
	        else 
            { 
                // Before 12:00
                CurrentTimeNulled = CurrentTime  - Sun_RiseTime; 
                NoonTimeNulled    = Sun_NoonTime - Sun_RiseTime; 
                Precentage_Mix = (float) CurrentTimeNulled / NoonTimeNulled * 100;
	        }
        }

        public double ExResetTime(double clockTimeSeconds)
        {
            var hour = (int)(clockTimeSeconds / (60 * 60));
            clockTimeSeconds -= hour * 60 * 60;
            var minute = (int)(clockTimeSeconds / 60);
            clockTimeSeconds -= minute * 60;
            var seconds = (int)clockTimeSeconds;

            // Reset clock before and after midnight
            if (hour >= 24)  hour %= 24;
            if (hour < 0)    hour += 24;
            if (minute < 0)  minute += 60;
            if (seconds < 0) seconds += 60;
            
            clockTimeSeconds = hour * 3600;
            clockTimeSeconds += minute * 60;
            clockTimeSeconds += seconds;
            
            return clockTimeSeconds; 
        }

        public void setTexture(Texture2D Ext1, Texture2D Ext2, Texture2D Ext3) 
        { 
            T1= Ext1; T2= Ext2; T3= Ext3;
            Exsize = T1.Width * T1.Height; 
            ReturnSkydomeTexture = new Texture2D(T1.GraphicsDevice, T1.Width, T1.Height); 
            
            // initialize arrays with the size of skydome_Sunrise
            SunriseA = new Color[Exsize]; 
            NoonA    = new Color[Exsize];
            SunsetA  = new Color[Exsize];
            MixBitmapSunrise = new Vector3[Exsize];
            MixBitmapNoon    = new Vector3[Exsize];
            MixBitmapSunset  = new Vector3[Exsize];
            MixReturn = new Color[Exsize];
            
            // grap skydomes images
            T1.GetData(SunriseA); T2.GetData(NoonA); T3.GetData(SunsetA);
            
            // Convert to floats
            for (int i=0; i < Exsize; i++) 
            {    
			    MixBitmapSunrise[i].X = (float) SunriseA[i].R /255;
			    MixBitmapSunrise[i].Y = (float) SunriseA[i].G /255;
			    MixBitmapSunrise[i].Z = (float) SunriseA[i].B /255;

                MixBitmapNoon[i].X = (float) NoonA[i].R /255;
			    MixBitmapNoon[i].Y = (float) NoonA[i].G /255;
			    MixBitmapNoon[i].Z = (float) NoonA[i].B /255;

                MixBitmapSunset[i].X = (float) SunsetA[i].R /255;
			    MixBitmapSunset[i].Y = (float) SunsetA[i].G /255;
			    MixBitmapSunset[i].Z = (float) SunsetA[i].B /255;
		    }
        }

        int Count = 1;
        public void WeatherTimeUpdate ()
        { 
            Count--;
            if(Count == 0) Count = 240;
            {   // Get time 00:00-23:59
                CurrentTime = (int)ExResetTime((int)Viewer.Simulator.ClockTime);
                // Before or after Noon
                if( CurrentTime >= 43200) 
                {   // After 
                    Sun_SetTimeNulled  = Sun_SetTime - Sun_NoonTime;
                    CurrentTimeNulled  = CurrentTime - Sun_NoonTime;
                    Precentage_Mix = (float)CurrentTimeNulled / Sun_SetTimeNulled * 100;
                    MixNoon( Precentage_Mix );
	            }
	            else 
                {   // Before
                    CurrentTimeNulled = CurrentTime - Sun_RiseTime; 
                    NoonTimeNulled    = Sun_NoonTime - Sun_RiseTime; 
                    Precentage_Mix = (float)CurrentTimeNulled / NoonTimeNulled * 100;
                    MixRise( Precentage_Mix );
	            }
            }
        }
        
        void MixNoon( float procent)
        {
            // 12:00 - 18:00  
            if(procent > 100) procent = 100.0f;
            if(procent < 0) procent = 0.0f;

            float InvPro = 100.0f - procent;
            for (int i = 0; i < Exsize; i++) {
                MixReturn[i].R = (byte) ((MixBitmapSunset[i].X * procent /100 + MixBitmapNoon[i].X*InvPro /100) *255.0f);
                MixReturn[i].G = (byte) ((MixBitmapSunset[i].Y * procent /100 + MixBitmapNoon[i].Y*InvPro /100) *255.0f);
                MixReturn[i].B = (byte) ((MixBitmapSunset[i].Z * procent /100 + MixBitmapNoon[i].Z*InvPro /100) *255.0f);;
            }
            ReturnSkydomeTexture.SetData( MixReturn );
        }

        void MixRise( float procent)
        {
            // 08:00 - 12:00
            if(procent > 100) procent = 100.0f;
            if(procent < 0) procent = 0.0f;

            float InvPro = 100.0f - procent;
            for (int i = 0; i < Exsize; i++) {
                MixReturn[i].R = (byte) ((MixBitmapNoon[i].X * procent /100 + MixBitmapSunrise[i].X*InvPro /100) *255.0f) ;
                MixReturn[i].G = (byte) ((MixBitmapNoon[i].Y * procent /100 + MixBitmapSunrise[i].Y*InvPro /100) *255.0f) ;
                MixReturn[i].B = (byte) ((MixBitmapNoon[i].Z * procent /100 + MixBitmapSunrise[i].Z*InvPro /100) *255.0f) ;
            }
            ReturnSkydomeTexture.SetData( MixReturn );
        }
    }


    public class SkyMaterial : Material
    {   
        new readonly Viewer Viewer;
        readonly SkyShader SkyShader;
        public   Texture2D SkyTextureSunrise;
        public   Texture2D SkyTextureNoon;
        public   Texture2D SkyTextureSunset;
        public   Texture2D SunTexture;
        readonly Texture2D StarTextureN;
        readonly Texture2D StarTextureS;
        readonly Texture2D MoonTexture;
        readonly Texture2D MoonMask;
        public   Texture2D CloudTexture1;
        public   Texture2D CloudTexture2;
        public   Texture2D CloudTexture3;
        public   SkyMix2Texture2D SkydomeTextureMix;

        readonly IEnumerator<EffectPass> ShaderPassesSky;
        readonly IEnumerator<EffectPass> ShaderPassesMoon;
        readonly IEnumerator<EffectPass> ShaderPassesClouds;

        public SkyMaterial(Viewer viewer) : base(viewer, null)
        {
            Viewer = viewer;
            SkyShader = Viewer.MaterialManager.SkyShader;
            // TODO: This should happen on the loader thread.
            var VRG = viewer.RenderProcess.GraphicsDevice;
            StarTextureN = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Starmap_N.png"));
            StarTextureS = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Starmap_S.png"));
            MoonTexture  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "MoonMap.png"));
            MoonMask     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "MoonMask.png"));

            SkyShader.StarMapTexture  = StarTextureN;
            SkyShader.MoonMapTexture  = MoonTexture;
            SkyShader.MoonMaskTexture = MoonMask;
            
            SkydomeTextureMix = new SkyMix2Texture2D(viewer);
            
            // ExRail Weather texture selector
            SetWeather();

            ShaderPassesSky    = SkyShader.Techniques["Sky"].Passes.GetEnumerator();
            ShaderPassesMoon   = SkyShader.Techniques["Moon"].Passes.GetEnumerator();
            ShaderPassesClouds = SkyShader.Techniques["Clouds"].Passes.GetEnumerator();

        }
        
        // ExRail Weather texture selector
        public void SetWeather()
        {
            // ExRail Weather texture selector
            Console.WriteLine("\n##########################################");
            Console.WriteLine("## Exrail Weather Extension V3.2        ##");
            Console.WriteLine("## Load Sky & Clouds Textures           ##");
            Console.Write("## WeatherType = ");
            Console.Write( (int)Viewer.Simulator.WeatherType );
            Console.Write("                      ##\n");
            Console.WriteLine("##########################################");

            var VRG = Viewer.RenderProcess.GraphicsDevice;

            switch (Viewer.Simulator.WeatherType)
            {
                case Orts.Formats.Msts.WeatherType.Clear:
                    SkyTextureSunrise = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Clear_SkyDome_Sunrise.png"));
                    SkyTextureNoon    = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Clear_SkyDome_Noon.png"));
                    SkyTextureSunset  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Clear_SkyDome_Sunset.png"));
                    CloudTexture1     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Clear_Clouds1.png"));
                    CloudTexture2     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Clear_Clouds2.png"));
                    CloudTexture3     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Clear_Clouds3.png"));
                    break;
                case Orts.Formats.Msts.WeatherType.Rain:
                    SkyTextureSunrise = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Rain_SkyDome_Sunrise.png"));
                    SkyTextureNoon    = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Rain_SkyDome_Noon.png"));
                    SkyTextureSunset  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Rain_SkyDome_Sunset.png"));
                    CloudTexture1     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Rain_Clouds1.png"));
                    CloudTexture2     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Rain_Clouds2.png"));
                    CloudTexture3     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Rain_Clouds3.png"));
                    break;
                case Orts.Formats.Msts.WeatherType.Snow:
                    SkyTextureSunrise = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Snow_SkyDome_Sunrise.png"));
                    SkyTextureNoon    = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Snow_SkyDome_Noon.png"));
                    SkyTextureSunset  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Snow_SkyDome_Sunset.png"));
                    CloudTexture1     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Snow_Clouds1.png"));
                    CloudTexture2     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Snow_Clouds2.png"));
                    CloudTexture3     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Snow_Clouds3.png"));
                    break;
                case Orts.Formats.Msts.WeatherType.Few:
                    SkyTextureSunrise = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Few_SkyDome_Sunrise.png"));
                    SkyTextureNoon    = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Few_SkyDome_Noon.png"));
                    SkyTextureSunset  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Few_SkyDome_Sunset.png"));
                    CloudTexture1     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Few_Clouds1.png"));
                    CloudTexture2     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Few_Clouds2.png"));
                    CloudTexture3     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Few_Clouds3.png"));
                    break;
                case Orts.Formats.Msts.WeatherType.Cloudy:
                    SkyTextureSunrise = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Cloudy_SkyDome_Sunrise.png"));
                    SkyTextureNoon    = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Cloudy_SkyDome_Noon.png"));
                    SkyTextureSunset  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Cloudy_SkyDome_Sunset.png"));
                    CloudTexture1     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Cloudy_Clouds1.png"));
                    CloudTexture2     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Cloudy_Clouds2.png"));
                    CloudTexture3     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Cloudy_Clouds3.png"));
                    break;
                case Orts.Formats.Msts.WeatherType.Desert:
                    SkyTextureSunrise = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Desert_SkyDome_Sunrise.png"));
                    SkyTextureNoon    = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Desert_SkyDome_Noon.png"));
                    SkyTextureSunset  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Desert_SkyDome_Sunset.png"));
                    CloudTexture1     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Desert_Clouds1.png"));
                    CloudTexture2     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Desert_Clouds2.png"));
                    CloudTexture3     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Desert_Clouds3.png"));
                    break;
                case Orts.Formats.Msts.WeatherType.SnowStorm:
                    SkyTextureSunrise = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/SnowStorm_SkyDome_Sunrise.png"));
                    SkyTextureNoon    = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/SnowStorm_SkyDome_Noon.png"));
                    SkyTextureSunset  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/SnowStorm_SkyDome_Sunset.png"));
                    CloudTexture1     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/SnowStorm_Clouds1.png"));
                    CloudTexture2     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/SnowStorm_Clouds2.png"));
                    CloudTexture3     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/SnowStorm_Clouds3.png"));
                    break;
                case Orts.Formats.Msts.WeatherType.Foggy:
                    SkyTextureSunrise = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Foggy_SkyDome_Sunrise.png"));
                    SkyTextureNoon    = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Foggy_SkyDome_Noon.png"));
                    SkyTextureSunset  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Foggy_SkyDome_Sunset.png"));
                    CloudTexture1     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Foggy_Clouds1.png"));
                    CloudTexture2     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Foggy_Clouds2.png"));
                    CloudTexture3     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Foggy_Clouds3.png"));
                    break;
                case Orts.Formats.Msts.WeatherType.PartlyCloudy:
                    SkyTextureSunrise = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/PartlyCloudy_SkyDome_Sunrise.png"));
                    SkyTextureNoon    = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/PartlyCloudy_SkyDome_Noon.png"));
                    SkyTextureSunset  = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/PartlyCloudy_SkyDome_Sunset.png"));
                    CloudTexture1     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/PartlyCloudy_Clouds1.png"));
                    CloudTexture2     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/PartlyCloudy_Clouds2.png"));
                    CloudTexture3     = SharedTextureManager.Get(VRG, System.IO.Path.Combine(Viewer.ContentPath, "Weather/PartlyCloudy_Clouds3.png"));
                break;
                
            }
            // Skydomes x 3
            SkydomeTextureMix.setTexture(SkyTextureSunrise, SkyTextureNoon, SkyTextureSunset );
            SkydomeTextureMix.WeatherTimeUpdate();
                                   
            // Clouds x 3
            SkyShader.CloudMapTexture1 = CloudTexture1;
            SkyShader.CloudMapTexture2 = CloudTexture2;
            SkyShader.CloudMapTexture3 = CloudTexture3;

        }



        int count =0;
        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            // Check Reload Weather texturres 

            if( Viewer.World.WeatherControl.ReloadWeatherSwitch ) 
            {   
                Viewer.WeatherEditorWindow.Bnt_ReloadWeathertype_Sel.Color = Viewer.WeatherEditorWindow.Color_Selected;
                Viewer.WeatherEditorWindow.Bnt_ReloadWeathertype_Sel.Text = " ◄Reloading Texture►";
                count++;
            }
                
            if(Viewer.World.WeatherControl.ReloadWeatherSwitch && count > 10) 
            {   
                SetWeather();
                Viewer.World.WeatherControl.ReloadWeatherSwitch = false;
                count = 0;
                Viewer.WeatherEditorWindow.Bnt_ReloadWeathertype_Sel.Color = Viewer.WeatherEditorWindow.Color_ReLoaded;
                Viewer.WeatherEditorWindow.Bnt_ReloadWeathertype_Sel.Text = " ◄Texture Reloaded►";
            }
        

            SkyShader.StarMapTexture = Viewer.World.Sky.Latitude > 0 ? StarTextureN : StarTextureS;

            SkydomeTextureMix.WeatherTimeUpdate();
            Viewer.MaterialManager.SkyShader.SkyMapTexture = SkydomeTextureMix.ReturnSkydomeTexture; 

            for (var i = 0; i < 5; i++) {
                graphicsDevice.SamplerStates[i] = SamplerState.LinearWrap;
            }
            var xnaSkyView = XNAViewMatrix * Camera.XNASkyProjection;

            var xnaMoonMatrix = Matrix.CreateTranslation(Viewer.World.Sky.LunarDirection * SkyPrimitive.RadiusM);
            var xnaMoonView = xnaMoonMatrix * xnaSkyView;
            SkyShader.SetViewMatrixMoon(ref XNAViewMatrix);

            // Sky dome
            SkyShader.CurrentTechnique = SkyShader.Techniques["Sky"];
            Viewer.World.Sky.Primitive.Element = SkyPrimitive.SkyElement.Sky;  
        
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.None;

            ShaderPassesSky.Reset();
            while (ShaderPassesSky.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    var wvp = item.XNAMatrix * xnaSkyView;
                    SkyShader.SetMatrix(ref wvp);
                    ShaderPassesSky.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }

            // Moon
            SkyShader.CurrentTechnique = SkyShader.Techniques["Moon"];
            Viewer.World.Sky.Primitive.Element = SkyPrimitive.SkyElement.Moon;
            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            ShaderPassesMoon.Reset();
            while (ShaderPassesMoon.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    var wvp = item.XNAMatrix * xnaMoonView;
                    SkyShader.SetMatrix(ref wvp);
                    ShaderPassesMoon.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }

            // Clouds
            SkyShader.CurrentTechnique = SkyShader.Techniques["Clouds"];
            Viewer.World.Sky.Primitive.Element = SkyPrimitive.SkyElement.Clouds;
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            ShaderPassesClouds.Reset();
            while (ShaderPassesClouds.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    var wvp = item.XNAMatrix * xnaSkyView;
                    SkyShader.SetMatrix(ref wvp);
                    ShaderPassesClouds.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override bool GetBlending()
        {
            return false;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(SkyTextureSunrise);
            Viewer.TextureManager.Mark(SkyTextureNoon);
            Viewer.TextureManager.Mark(SkyTextureSunset);
            Viewer.TextureManager.Mark(StarTextureN);
            Viewer.TextureManager.Mark(StarTextureS);
            Viewer.TextureManager.Mark(MoonTexture);
            Viewer.TextureManager.Mark(MoonMask);
            Viewer.TextureManager.Mark(CloudTexture1);
            Viewer.TextureManager.Mark(CloudTexture2);
            Viewer.TextureManager.Mark(CloudTexture3);
            base.Mark();
        }
    }
}
