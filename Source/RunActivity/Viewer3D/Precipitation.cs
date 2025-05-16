// COPYRIGHT 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Orts.Simulation;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Orts.Viewer3D
{
    public class PrecipitationViewer
    {
        public const float MinIntensityPPSPM2 = 0;
        public const float MaxIntensityPPSPM2 = 0.5f;
        readonly Viewer Viewer;
        readonly Material Material;
        readonly WeatherControl InternalWeatherControl;
        readonly PrecipitationPrimitive Pricipitation;
        readonly PrecipitationPrimitive PricipitationEx;
        public enum PrecipNumber { Precip1=0, Precip2};

        public PrecipitationViewer(Viewer viewer, WeatherControl weather_Control )
        {
            this.Viewer = viewer;
            InternalWeatherControl = weather_Control;
            Material = viewer.MaterialManager.Load("Precipitation");
            Pricipitation   = new PrecipitationPrimitive(this.Viewer.GraphicsDevice, PrecipNumber.Precip1);
            PricipitationEx = new PrecipitationPrimitive(this.Viewer.GraphicsDevice, PrecipNumber.Precip2);
            Reset();
        }
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var gameTime = (float)Viewer.Simulator.GameTime;
            Pricipitation.DynamicUpdate(   InternalWeatherControl.Weather, Viewer, ref InternalWeatherControl.Weather.PrecipWind1);
            PricipitationEx.DynamicUpdate( InternalWeatherControl.Weather, Viewer, ref InternalWeatherControl.Weather.PrecipWind2);
            
            Pricipitation.Update(gameTime, elapsedTime,   InternalWeatherControl.Weather.PricipitationIntensityPPSPM2, Viewer,  ref InternalWeatherControl.Weather.PrecipWind1);
            PricipitationEx.Update(gameTime, elapsedTime, InternalWeatherControl.Weather.PricipitationIntensityPPSPM2, Viewer,  ref InternalWeatherControl.Weather.PrecipWind2);
        
            // Note: This is quite a hack. We ideally should be able to pass this through RenderItem somehow.
            var XNAWorldLocation = Matrix.Identity;
            XNAWorldLocation.M11 = gameTime;
            XNAWorldLocation.M21 = Viewer.Camera.TileX;
            XNAWorldLocation.M22 = Viewer.Camera.TileZ;

            frame.AddPrimitive(Material, Pricipitation, RenderPrimitiveGroup.Precipitation, ref XNAWorldLocation);
            frame.AddPrimitive(Material, PricipitationEx, RenderPrimitiveGroup.Precipitation, ref XNAWorldLocation);
        }
        public void Reset()
        {
            // This procedure is only called once at the start of an activity.

            var gameTime = (float)Viewer.Simulator.GameTime;
            
            Pricipitation.Initialize(   Viewer.Simulator.WeatherType,  InternalWeatherControl.Weather.PrecipWind1, Viewer); 
            PricipitationEx.Initialize( Viewer.Simulator.WeatherType , InternalWeatherControl.Weather.PrecipWind2, Viewer);
            
            Console.WriteLine("Reset() function" );
            Console.Write("Gametime = " );
            Console.Write( (float) gameTime);Console.Write("\n");
        
            // Camera is null during first initialisation.
            if (Viewer.Camera != null) { 
                Console.WriteLine("Reset() Cam !=null" );
                Console.Write("Gametime = " );
                Console.Write( (float) gameTime);Console.Write("\n");
                //Pricipitation.Update(gameTime, null, Viewer.World.WeatherControl.Weather.PricipitationIntensityPPSPM2, Viewer, ref Viewer.World.WeatherControl.Weather.PrecipWind1);
                //PricipitationEx.Update(gameTime, null, Viewer.World.WeatherControl.Weather.PricipitationIntensityPPSPM2, Viewer, ref Viewer.World.WeatherControl.Weather.PrecipWind2 );
            }
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Material.Mark();
        }
    }

    public class PrecipitationPrimitive : RenderPrimitive
    {
        // http://www-das.uwyo.edu/~geerts/cwx/notes/chap09/hydrometeor.html
        // "Rain  1.8 - 2.2mm  6.1 - 6.9m/s"
        const float RainVelocityMpS = 3.0f;
        // "Snow flakes of any size falls at about 1 m/s"
        const float SnowVelocityMpS = 1.5f;
        // This is a fiddle factor because the above values feel too slow. Alternative suggestions welcome.
        const float ParticleVelocityFactor = 7.7f;

        readonly float ParticleBoxLengthM;
        readonly float ParticleBoxWidthM;
        readonly float ParticleBoxHeightM;

        const int IndiciesPerParticle = 6;
        const int VerticiesPerParticle = 4;
        const int PrimitivesPerParticle = 2;

        readonly int MaxParticles;
        readonly ParticleVertex[] Vertices;
        readonly VertexDeclaration VertexDeclaration;
        readonly int VertexStride;
        readonly DynamicVertexBuffer VertexBuffer;
        readonly IndexBuffer IndexBuffer;

        struct ParticleVertex
        {
            public Vector4 StartPosition_StartTime;
            public Vector4 EndPosition_EndTime;
            public Vector4 TileXZ_Vertex;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.Position, 1),
                new VertexElement(16 + 16, VertexElementFormat.Vector4, VertexElementUsage.Position, 2),
            };
            public static int SizeInBytes = sizeof(float) * (4 + 4) + sizeof(float) * 4;
        }

        struct PosMemory {
            public Vector3 Old_pos;
            public int Counter;
        }
        PosMemory posMemory;

        float ParticleDuration;
        Vector3 ParticleDirection;
        HeightCache Heights;

        // Particle buffer goes like this:
        //   +--active>-----new>--+
        //   |                    |
        //   +--<retired---<free--+

        int FirstActiveParticle;
        int FirstNewParticle;
        int FirstFreeParticle;
        int FirstRetiredParticle;

        float ParticlesToEmit;
        float TimeParticlesLastEmitted;
        int DrawCounter;
        PrecipitationViewer.PrecipNumber Precipnumber;

        public PrecipitationPrimitive(GraphicsDevice graphicsDevice, PrecipitationViewer.PrecipNumber ex )
        {
            posMemory = new PosMemory
            {
                Old_pos = new Vector3(),
                Counter = 0
            };

            Precipnumber = ex;
            
            switch(ex)
            {
                case PrecipitationViewer.PrecipNumber.Precip1:
                ParticleBoxLengthM = (float)Program.Simulator.Settings.PrecipitationBoxLength;
                ParticleBoxWidthM  = (float)Program.Simulator.Settings.PrecipitationBoxWidth;
                ParticleBoxHeightM = (float)Program.Simulator.Settings.PrecipitationBoxHeight;
                MaxParticles       =  (int)(float)Program.Simulator.Settings.NumberOfParticles;
                break;
             
                case PrecipitationViewer.PrecipNumber.Precip2:
                ParticleBoxLengthM =  (float)Program.Simulator.Settings.PrecipitationBoxLength2;
                ParticleBoxWidthM  =  (float)Program.Simulator.Settings.PrecipitationBoxWidth2;
                ParticleBoxHeightM =  (float)Program.Simulator.Settings.PrecipitationBoxHeight2;
                MaxParticles       =    (int)Program.Simulator.Settings.NumberOfParticles2;
                break;
            }

            Vertices = new ParticleVertex[MaxParticles * VerticiesPerParticle];
            VertexDeclaration = new VertexDeclaration(ParticleVertex.SizeInBytes, ParticleVertex.VertexElements);
            VertexStride = Marshal.SizeOf(typeof(ParticleVertex));
            VertexBuffer = new DynamicVertexBuffer(graphicsDevice, VertexDeclaration, MaxParticles * VerticiesPerParticle, BufferUsage.WriteOnly);
            IndexBuffer = InitIndexBuffer(graphicsDevice, MaxParticles * IndiciesPerParticle);

            Heights = new HeightCache(8);
            // This Trace command is used to show how much memory is used.
            Trace.TraceInformation(String.Format("Allocation for {0:N0} particles:\n\n  {1,13:N0} B RAM vertex data\n  {2,13:N0} B RAM index data (temporary)\n  {1,13:N0} B VRAM DynamicVertexBuffer\n  {2,13:N0} B VRAM IndexBuffer"
                , MaxParticles, Marshal.SizeOf(typeof(ParticleVertex)) * MaxParticles * VerticiesPerParticle
                , sizeof(uint) * MaxParticles * IndiciesPerParticle));
        }

        void VertexBuffer_ContentLost()
        {
            VertexBuffer.SetData(0, Vertices, 0, Vertices.Length, VertexStride, SetDataOptions.NoOverwrite);
        }
        // IndexBuffer for 32bit process.
        static IndexBuffer InitIndexBuffer(GraphicsDevice graphicsDevice, int numIndicies)
        {
            var indices = new uint[numIndicies];
            var index = 0;
            for (var i = 0; i < numIndicies; i += IndiciesPerParticle)
            {
                indices[i] = (uint)index;
                indices[i + 1] = (uint)(index + 1);
                indices[i + 2] = (uint)(index + 2);

                indices[i + 3] = (uint)(index + 2);
                indices[i + 4] = (uint)(index + 3);
                indices[i + 5] = (uint)(index);

                index += VerticiesPerParticle;
            }
            var indexBuffer = new IndexBuffer(graphicsDevice, typeof(uint), numIndicies, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
            return indexBuffer;
        }

        void RetireActiveParticles(float currentTime)
        {
            while (FirstActiveParticle != FirstNewParticle)
            {
                var vertex = FirstActiveParticle * VerticiesPerParticle;
                var expiry = Vertices[vertex].EndPosition_EndTime.W;

                // Stop as soon as we find the first particle which hasn't expired.
                if (expiry > currentTime)
                    break;

                // Expire particle.
                Vertices[vertex].StartPosition_StartTime.W = (float)DrawCounter;
                FirstActiveParticle = (FirstActiveParticle + 1) % MaxParticles;
            }
        }

        void FreeRetiredParticles()
        {
            while (FirstRetiredParticle != FirstActiveParticle)
            {
                var vertex = FirstRetiredParticle * VerticiesPerParticle;
                var age = DrawCounter - (int)Vertices[vertex].StartPosition_StartTime.W;

                // Stop as soon as we find the first expired particle which hasn't been expired for at least 2 'ticks'.
                if (age < 2)
                    break;

                FirstRetiredParticle = (FirstRetiredParticle + 1) % MaxParticles;
            }
        }

        int GetCountFreeParticles()
        {
            var nextFree = (FirstFreeParticle + 1) % MaxParticles;

            if (nextFree <= FirstRetiredParticle)
                return FirstRetiredParticle - nextFree;

            return (MaxParticles - nextFree) + FirstRetiredParticle;
        }
        
        // ExRail - Added weathertype extension Snowstorm
        public void Initialize(Orts.Formats.Msts.WeatherType weather, Vector3 wind, Viewer viewer)
        {   
            switch(Precipnumber)
            {
                case PrecipitationViewer.PrecipNumber.Precip1: 
                    viewer.MaterialManager.PrecipitationShader.particleSize.SetValue( viewer.Simulator.Weather.ParticleSize1); 
                    break;   
                case PrecipitationViewer.PrecipNumber.Precip2: 
                    viewer.MaterialManager.PrecipitationShader.particleSize.SetValue( viewer.Simulator.Weather.ParticleSize2); 
                    break;
                default: break;
            }
            

            if (weather == Orts.Formats.Msts.WeatherType.Snow || weather == Orts.Formats.Msts.WeatherType.SnowStorm ) {
                ParticleDuration = ParticleBoxHeightM / SnowVelocityMpS / ParticleVelocityFactor;
            }
            else { 
                ParticleDuration = ParticleBoxHeightM / RainVelocityMpS / ParticleVelocityFactor;
            }
            ParticleDirection = wind;
            FirstActiveParticle = FirstNewParticle = FirstFreeParticle = FirstRetiredParticle = 0;
            ParticlesToEmit = TimeParticlesLastEmitted = 0;
            DrawCounter = 0;
        }

        public void DynamicUpdate(WeatherExtension weather, Viewer viewer, ref Vector3 wind)
        {
            if (weather.PrecipitationLiquidity == 0 || weather.PrecipitationLiquidity == 1) return;
            ParticleDuration = ParticleBoxHeightM / ((RainVelocityMpS-SnowVelocityMpS) * weather.PrecipitationLiquidity + SnowVelocityMpS)/ ParticleVelocityFactor;
            wind.X = 18 * weather.PrecipitationLiquidity + 2;
            ParticleDirection = wind;
        }

        private static double GetHeadingError(double initial, double final)
        {
            if (initial > 360 || initial < 0 || final > 360 || final < 0)
            {
                //throw some error
            }

            var diff = final - initial;
            var absDiff = Math.Abs(diff);

            if (absDiff <= 180)
            {
                //Edit 1:27pm
                return absDiff == 180 ? absDiff : diff;
            }

            else if (final > initial)
            {
                return absDiff - 360;
            }

            else
            {
                return 360 - absDiff;
            }
        }
        // <----------------------------- ExRail ------------------------------------->
        // calculate train heading from two points - some javascript rewrite
        public double findAngleBetween ( double x1 , double y1 , double x2 , double y2 )
        {
            var calc_angle = Math.Atan2 ( y2 - y1 , x2 - x1 );
            // we don't want negative angles
            if ( calc_angle < 0 ) {
                calc_angle += Math.PI * 2;
                // make negative angles positive by adding 360 degrees
            }
            var f = calc_angle * ( 180 / Math.PI );
            return f;    
        } 

        float CompDirection = 0;

        public void Update(float currentTime, ElapsedTime elapsedTime, float particlesPerSecondPerM2, Viewer viewer, ref Vector3 wind)
        {
            var tiles = viewer.Tiles;
            var scenery = viewer.World.Scenery;

            // get train position 
            var worldLocation = viewer.Simulator.PlayerLocomotive.WorldPosition.WorldLocation;
            
            posMemory.Counter += 1;       
            if (posMemory.Counter == 60) {
                    // store old position
                    posMemory.Old_pos = worldLocation.Location;
            }

            if (posMemory.Counter >= 240) {  
                posMemory.Counter = 0; 
                // calculate train heading from old and new positions 
                CompDirection = (float)findAngleBetween( posMemory.Old_pos.Z , posMemory.Old_pos.X 
                    , worldLocation.Location.Z , worldLocation.Location.X );
            }
       
            Vector3 Exlocation = viewer.Simulator.PlayerLocomotive.WorldPosition.WorldLocation.Location;
            float projectfactor = 10.0f;
            // Compensate for trains heading and project forward in relation to speed
            if(CompDirection > 0.0f && CompDirection < 90.0f)
            { 
                float multi90Inv = 90f - CompDirection;
                float multi90 = CompDirection;
                Exlocation.Z += viewer.Simulator.PlayerLocomotive.SpeedMpS * (multi90Inv / projectfactor);
                Exlocation.X += viewer.Simulator.PlayerLocomotive.SpeedMpS * (multi90 / projectfactor);
            }

            if(CompDirection >= 90.0f && CompDirection < 180.0f)
            { 
                float multi90Inv = 90 - (CompDirection-90);
                float multi90 = CompDirection-90;
                Exlocation.Z -= viewer.Simulator.PlayerLocomotive.SpeedMpS * (multi90 / projectfactor);
                Exlocation.X += viewer.Simulator.PlayerLocomotive.SpeedMpS * (multi90Inv / projectfactor);
            }

            if(CompDirection >= 180.0f && CompDirection <= 270.0f) 
            { 
                float multi90Inv = 90 - (CompDirection-180);
                float multi90 = CompDirection-180;
                Exlocation.Z -= viewer.Simulator.PlayerLocomotive.SpeedMpS * (multi90Inv / projectfactor) ;
                Exlocation.X -= viewer.Simulator.PlayerLocomotive.SpeedMpS * (multi90 / projectfactor) ;
            }

            if(CompDirection > 270.0f && CompDirection <= 359.999f) 
            { 
                float multi90Inv = 90 - (CompDirection-270);
                float multi90 = CompDirection-270;
                Exlocation.Z += viewer.Simulator.PlayerLocomotive.SpeedMpS * (multi90 / projectfactor) ;
                Exlocation.X -= viewer.Simulator.PlayerLocomotive.SpeedMpS * (multi90Inv / projectfactor) ;
            }

            worldLocation.Location = Exlocation;
            // <----------------------------- ExRail ------------------------------------->
           
            ParticleDirection = wind;

            if (TimeParticlesLastEmitted == 0)
            {
                TimeParticlesLastEmitted = currentTime - ParticleDuration;
                ParticlesToEmit += ParticleDuration * particlesPerSecondPerM2 * ParticleBoxLengthM * ParticleBoxWidthM;
            }
            else
            {
                RetireActiveParticles(currentTime);
                FreeRetiredParticles();
                ParticlesToEmit += elapsedTime.ClockSeconds * particlesPerSecondPerM2 * ParticleBoxLengthM * ParticleBoxWidthM;
            }

            var numParticlesAdded = 0;
            var numToBeEmitted = (int)ParticlesToEmit;
            var numCanBeEmitted = GetCountFreeParticles();
            var numToEmit = Math.Min(numToBeEmitted, numCanBeEmitted);
            bool count = false;
            for (var i = 0; i < numToEmit; i++)
            {
                if(!count) { 
                    
                        var temp = new WorldLocation(worldLocation.TileX, worldLocation.TileZ
                            , worldLocation.Location.X + (float)((Viewer.Random.NextDouble() - 0.5) * ParticleBoxWidthM)
                            , 0, worldLocation.Location.Z + (float)((Viewer.Random.NextDouble() - 0.5) * ParticleBoxLengthM)
                            );
                            temp.Location.Y = Heights.GetHeight(temp, tiles, scenery);
                            var position = new WorldPosition(temp);
                                     
                        var time = MathHelper.Lerp(TimeParticlesLastEmitted, currentTime, (float)i / numToEmit);
                        var particle = (FirstFreeParticle + 1) % MaxParticles;
                        var vertex = particle * VerticiesPerParticle;

                        for (var j = 0; j < VerticiesPerParticle; j++)
                        {
                            Vertices[vertex + j].StartPosition_StartTime = new Vector4(position.XNAMatrix.Translation - ParticleDirection * ParticleDuration, time);
                            Vertices[vertex + j].StartPosition_StartTime.Y += ParticleBoxHeightM;
                            Vertices[vertex + j].EndPosition_EndTime = new Vector4(position.XNAMatrix.Translation, time + ParticleDuration);
                            Vertices[vertex + j].TileXZ_Vertex = new Vector4(position.TileX, position.TileZ, j, 0);
                        }
                        FirstFreeParticle = particle;
                        ParticlesToEmit--;
                        numParticlesAdded++;
                        count = true;
                }

                else { 
                        var temp = new WorldLocation(worldLocation.TileX, worldLocation.TileZ
                            , worldLocation.Location.X + (float)((Viewer.Random.NextDouble() - 0.5) * ParticleBoxWidthM/2)
                            , 0, worldLocation.Location.Z + (float)((Viewer.Random.NextDouble() - 0.5) * ParticleBoxLengthM/2)
                            );
                            temp.Location.Y = Heights.GetHeight(temp, tiles, scenery);
                            var position = new WorldPosition(temp);
                                     
                        var time = MathHelper.Lerp(TimeParticlesLastEmitted, currentTime, (float)i / numToEmit);
                        var particle = (FirstFreeParticle + 1) % MaxParticles;
                        var vertex = particle * VerticiesPerParticle;

                        for (var j = 0; j < VerticiesPerParticle; j++)
                        {
                            Vertices[vertex + j].StartPosition_StartTime = new Vector4(position.XNAMatrix.Translation - ParticleDirection * ParticleDuration, time);
                            Vertices[vertex + j].StartPosition_StartTime.Y += ParticleBoxHeightM;
                            Vertices[vertex + j].EndPosition_EndTime = new Vector4(position.XNAMatrix.Translation, time + ParticleDuration);
                            Vertices[vertex + j].TileXZ_Vertex = new Vector4(position.TileX, position.TileZ, j, 0);
                        }
                        FirstFreeParticle = particle;
                        ParticlesToEmit--;
                        numParticlesAdded++;
                        count = false;
                }
            }

            if (numParticlesAdded > 0)
                TimeParticlesLastEmitted = currentTime;

            ParticlesToEmit -=  (int)ParticlesToEmit;
        }

        void AddNewParticlesToVertexBuffer()
        {
            if (FirstNewParticle < FirstFreeParticle)
            {
                var numParticlesToAdd = FirstFreeParticle - FirstNewParticle;
                VertexBuffer.SetData(FirstNewParticle * VertexStride * VerticiesPerParticle, Vertices, FirstNewParticle * VerticiesPerParticle, numParticlesToAdd * VerticiesPerParticle, VertexStride, SetDataOptions.NoOverwrite);
            }
            else
            {
                var numParticlesToAddAtEnd = MaxParticles - FirstNewParticle;
                VertexBuffer.SetData(FirstNewParticle * VertexStride * VerticiesPerParticle, Vertices, FirstNewParticle * VerticiesPerParticle, numParticlesToAddAtEnd * VerticiesPerParticle, VertexStride, SetDataOptions.NoOverwrite);
                if (FirstFreeParticle > 0)
                    VertexBuffer.SetData(0, Vertices, 0, FirstFreeParticle * VerticiesPerParticle, VertexStride, SetDataOptions.NoOverwrite);
            }

            FirstNewParticle = FirstFreeParticle;
        }

        public bool HasParticlesToRender()
        {
            return FirstActiveParticle != FirstFreeParticle;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (VertexBuffer.IsContentLost)
                VertexBuffer_ContentLost();

            if (FirstNewParticle != FirstFreeParticle)
                AddNewParticlesToVertexBuffer();

            if (HasParticlesToRender())
            {
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.SetVertexBuffer(VertexBuffer);

                if (FirstActiveParticle < FirstFreeParticle)
                {
                    var numParticles = FirstFreeParticle - FirstActiveParticle;
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: FirstActiveParticle * IndiciesPerParticle, primitiveCount: numParticles * PrimitivesPerParticle);
                }
                else
                {
                    var numParticlesAtEnd = MaxParticles - FirstActiveParticle;
                    if (numParticlesAtEnd > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: FirstActiveParticle * IndiciesPerParticle, primitiveCount: numParticlesAtEnd * PrimitivesPerParticle);
                    if (FirstFreeParticle > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: 0, primitiveCount: FirstFreeParticle * PrimitivesPerParticle);
                }
            }

            DrawCounter++;
        }

        class HeightCache
        {
            const int TileCount = 10;

            readonly int BlockSize;
            readonly int Divisions;
            readonly List<Tile> Tiles = new List<Tile>();

            public HeightCache(int blockSize)
            {
                BlockSize = blockSize;
                Divisions = (int)Math.Round(2048f / blockSize);
            }

            public float GetHeight(WorldLocation location, TileManager tiles, SceneryDrawer scenery)
            {
                location.Normalize();

                // First, ensure we have the tile in question cached.
                var tile = Tiles.FirstOrDefault(t => t.TileX == location.TileX && t.TileZ == location.TileZ);
                if (tile == null)
                    Tiles.Add(tile = new Tile(location.TileX, location.TileZ, Divisions));

                // Remove excess entries.
                if (Tiles.Count > TileCount)
                    Tiles.RemoveAt(0);

                // Now calculate division to query.
                var x = ((int)location.Location.X + 1024) / BlockSize;
                var z = ((int)location.Location.Z + 1024) / BlockSize;

                // Trace the case where x or z are out of bounds and fix
                var xSize = tile.Height.GetLength(0);
                var zSize = tile.Height.GetLength(1);
                if (x < 0 || x >= xSize || z < 0 || z >= zSize)
                {
                    Trace.TraceWarning("At least one precipitation index is out of bounds:  x = {0}, z = {1}, Location.X = {2}, Location.Z = {3}, BlockSize = {4}, HeightDimensionX = {5}, HeightDimensionZ = {6} ; fixing it",
                        x, z, location.Location.X, location.Location.Z, BlockSize, xSize, zSize);
                    if (x >= xSize) x = xSize - 1;
                    if (z >= zSize) z = zSize - 1;
                    if (x < 0) x = 0;
                    if (z < 0) z = 0;
                }

                // If we don't have it cached, load it.
                if (tile.Height[x, z] == float.MinValue)
                {
                    var position = new WorldLocation(location.TileX, location.TileZ, (x + 0.5f) * BlockSize - 1024, 0, (z + 0.5f) * BlockSize - 1024);
                    tile.Height[x, z] = Math.Max(tiles.GetElevation(position), scenery.GetBoundingBoxTop(position, BlockSize));
                    tile.Used++;
                }

                return tile.Height[x, z];
            }

            [DebuggerDisplay("Tile = {TileX},{TileZ} Used = {Used}")]
            class Tile
            {
                public readonly int TileX;
                public readonly int TileZ;
                public readonly float[,] Height;
                public int Used;

                public Tile(int tileX, int tileZ, int divisions)
                {
                    TileX = tileX;
                    TileZ = tileZ;
                    Height = new float[divisions, divisions];
                    for (var x = 0; x < divisions; x++)
                        for (var z = 0; z < divisions; z++)
                            Height[x, z] = float.MinValue;
                }
            }
        }
    }

    public class PrecipitationMaterial : Material
    {
        Texture2D RainTexture;
        Texture2D SnowTexture;
        Texture2D[] DynamicPrecipitationTexture = new Texture2D[12];
        IEnumerator<EffectPass> ShaderPasses;

        public PrecipitationMaterial(Viewer viewer)
            : base(viewer, null)
        {
            // TODO: This should happen on the loader thread.
            RainTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Raindrop.png"));
            SnowTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Weather/Snowflake.png"));
            DynamicPrecipitationTexture[0] = SnowTexture;
            DynamicPrecipitationTexture[11] = RainTexture;
            for (int i = 1; i<=10; i++)
            {
                var path = "Weather/Raindrop" + i.ToString() + ".png";
                DynamicPrecipitationTexture[11 - i] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, path));
            }
            var shader = Viewer.MaterialManager.PrecipitationShader; 
            Viewer.MaterialManager.PrecipitationShader.CurrentTechnique = shader.Techniques["Pricipitation"];
            if (ShaderPasses == null) ShaderPasses = shader.Techniques["Pricipitation"].Passes.GetEnumerator();
            if (Viewer.Simulator.Weather.PrecipitationLiquidity == 0 || Viewer.Simulator.Weather.PrecipitationLiquidity == 1)
            {
                shader.precipitation_Tex.SetValue(Viewer.Simulator.WeatherType == Orts.Formats.Msts.WeatherType.Snow ? SnowTexture :
                Viewer.Simulator.WeatherType == Orts.Formats.Msts.WeatherType.Rain ? RainTexture :
                Viewer.Simulator.Weather.PrecipitationLiquidity == 0 ? SnowTexture : RainTexture);
            }
            else {
                var precipitation_TexIndex = (int)(Viewer.Simulator.Weather.PrecipitationLiquidity * 11);
                shader.precipitation_Tex.SetValue(DynamicPrecipitationTexture[precipitation_TexIndex]);
            }

        }
        
        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.PrecipitationShader;    
            shader.LightVector.SetValue(Viewer.Settings.UseMSTSEnv ? Viewer.World.MSTSSky.mstsskysolarDirection : Viewer.World.Sky.SolarDirection);

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        }
           
        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.PrecipitationShader;

            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    // Note: This is quite a hack. We ideally should be able to pass this through RenderItem somehow.
                    shader.cameraTileXZ.SetValue(new Vector2(item.XNAMatrix.M21, item.XNAMatrix.M22));
                    shader.currentTime.SetValue(item.XNAMatrix.M11);

                    shader.SetMatrix(Matrix.Identity, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    ShaderPasses.Current.Apply();
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
            return true;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(RainTexture);
            Viewer.TextureManager.Mark(SnowTexture);
            for (int i = 1; i <= 10; i++)
                Viewer.TextureManager.Mark(DynamicPrecipitationTexture[i]);
            base.Mark();
        }
    }

    [CallOnThread("Render")]
    public class PrecipitationShader : Shader
    {
        internal readonly EffectParameter worldViewProjection;
        internal readonly EffectParameter invView;
        internal readonly EffectParameter LightVector;
        internal readonly EffectParameter fog;
        internal readonly EffectParameter particleSize;
        internal readonly EffectParameter cameraTileXZ;
        internal readonly EffectParameter currentTime;
        internal readonly EffectParameter precipitation_Tex;

        public PrecipitationShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "PrecipitationShader")
        {
            worldViewProjection = Parameters["worldViewProjection"];
            invView = Parameters["invView"];
            LightVector = Parameters["LightVector"];
            fog = Parameters["Fog"];
            particleSize = Parameters["particleSize"];
            cameraTileXZ = Parameters["cameraTileXZ"];
            currentTime = Parameters["currentTime"];
            precipitation_Tex = Parameters["precipitation_Tex"];
        }

        public void SetParticleSize(float particle_size) {
            particleSize.SetValue( particle_size );
        }

        public void SetMatrix(Matrix world, ref Matrix view, ref Matrix projection)
        {
            worldViewProjection.SetValue(world * view * projection);
            invView.SetValue(Matrix.Invert(view));
        }
        public void SetFog(float depth, ref Color color)
        {
            //fogColor.SetValue(new Vector3(color.R / 255f, color.G / 255f, color.B / 255f));
            //fog.SetValue(new Vector4(5000f / depth, 0.015f * MathHelper.Clamp(depth / 5000f, 0, 1), MathHelper.Clamp(depth / 10000f, 0, 1), 0.05f * MathHelper.Clamp(depth / 10000f, 0, 1)));
            fog.SetValue(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, MathHelper.Clamp(300f / depth, 0, 1)));
        }

    }
}
