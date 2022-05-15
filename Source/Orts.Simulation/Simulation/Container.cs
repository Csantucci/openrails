// COPYRIGHT 2012, 2013 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Common;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orts.Simulation
{

    public enum ContainerType
    {
        None,
        C20ft,
        C40ft,
        C40ftHC,
        C45ft,
        C48ft,
        C53ft
    }

    public class ContainerList
    {
        public List<Container> Containers = new List<Container>();
    }
    public class Container
    {
        public enum Status
        {
            OnEarth,
            Loading,
            Unloading,
            WaitingForLoading,
            WaitingForUnloading
        }

        public string ShapeFileName;
        public string BaseShapeFileFolderSlash;
        public float MassKG = 2000;
        public float WidthM = 2.44f;
        public float LengthM = 12.19f;       
        public float HeightM = 2.59f;
        public ContainerType ContainerType = ContainerType.C40ft;
        public bool Flipped = false;

        public WorldPosition WorldPosition = new WorldPosition();  // current position of the container
        public float RealRelativeYOffset = 0;
        public float RealRelativeZOffset = 0;
        public Vector3 IntrinsicShapeOffset;
        public ContainerHandlingItem ContainerStation;
        public Matrix RelativeContainerMatrix = Matrix.Identity;
        public MSTSWagon Wagon;
        public string LoadFilePath;


        // generates container from FreightAnim
 /*       public Container(Simulator simulator, string baseShapeFileFolderSlash, FreightAnimationDiscrete freightAnimDiscrete, ContainerHandlingItem containerStation )
        {
            Simulator = simulator;
            ShapeFileName = freightAnimDiscrete.ShapeFileName;
            BaseShapeFileFolderSlash = baseShapeFileFolderSlash;
            MassKG = freightAnimDiscrete.LoadWeightKG;
            ContainerType = freightAnimDiscrete.ContainerType;
            switch (ContainerType)
            {
                case ContainerType.C20ft:
                    LengthM = 6.1f;
                    break;
                case ContainerType.C40ft:
                    LengthM = 12.19f;
                    break;
                case ContainerType.C40ftHC:
                    LengthM = 12.19f;
                    HeightM = 2.9f;
                    break;
                case ContainerType.C45ft:
                    LengthM = 13.7f;
                    break;
                case ContainerType.C48ft:
                    LengthM = 14.6f;
                    break;
                case ContainerType.C53ft:
                    LengthM = 16.15f;
                    break;
                default:
                    break;
            }
            WorldPosition.XNAMatrix = freightAnimDiscrete.Wagon.WorldPosition.XNAMatrix;
            WorldPosition.TileX = freightAnimDiscrete.Wagon.WorldPosition.TileX;
            WorldPosition.TileZ = freightAnimDiscrete.Wagon.WorldPosition.TileZ;
            var translation = Matrix.CreateTranslation(freightAnimDiscrete.XOffset, freightAnimDiscrete.YOffset, freightAnimDiscrete.ZOffset);
            WorldPosition.XNAMatrix = translation * WorldPosition.XNAMatrix;
            IntrinsicShapeOffset = freightAnimDiscrete.IntrinsicShapeOffset;

            ContainerStation = containerStation;
        }*/

        public Container(STFReader stf, FreightAnimationDiscrete freightAnimDiscrete)
        {
            Wagon = freightAnimDiscrete.Wagon;
            BaseShapeFileFolderSlash = Path.GetDirectoryName(freightAnimDiscrete.Wagon.WagFilePath) + @"\";
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[]
            {
                new STFReader.TokenProcessor("shape", ()=>{ ShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("intrinsicshapeoffset", ()=>
                {
                    IntrinsicShapeOffset = stf.ReadVector3Block(STFReader.UNITS.Distance,  new Vector3(0, 0, 0));
                    IntrinsicShapeOffset.Z *= -1;
                }),
                new STFReader.TokenProcessor("containertype", ()=>
                {
                    var containerTypeString = stf.ReadStringBlock(null);
                    Enum.TryParse<ContainerType>(containerTypeString, out var containerType);
                    ContainerType = containerType;
                    ComputeDimensions();
                 }),
                new STFReader.TokenProcessor("flip", ()=>{ Flipped = stf.ReadBoolBlock(true);}),
                new STFReader.TokenProcessor("loadweight", ()=>{ MassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, 0);
                }),
            });
            ComputeWorldPosition(freightAnimDiscrete);
        }

        public Container(FreightAnimationDiscrete freightAnimDiscreteCopy, FreightAnimationDiscrete freightAnimDiscrete, bool stacked = false)
        {
            Wagon = freightAnimDiscrete.Wagon;
            Copy(freightAnimDiscreteCopy.Container);

            WorldPosition.XNAMatrix = Wagon.WorldPosition.XNAMatrix;
            WorldPosition.TileX = Wagon.WorldPosition.TileX;
            WorldPosition.TileZ = Wagon.WorldPosition.TileZ;
            var totalOffset = freightAnimDiscrete.Offset - IntrinsicShapeOffset;
            if (stacked)
                totalOffset.Y += freightAnimDiscreteCopy.Container.HeightM;
            var translation = Matrix.CreateTranslation(totalOffset);
            WorldPosition.XNAMatrix = translation * WorldPosition.XNAMatrix;
            var invWagonMatrix = Matrix.Invert(freightAnimDiscrete.Wagon.WorldPosition.XNAMatrix);
            RelativeContainerMatrix = Matrix.Multiply(WorldPosition.XNAMatrix, invWagonMatrix);
        }

        public Container (MSTSWagon wagon, string  loadFilePath)
        {
            Wagon = wagon;
            BaseShapeFileFolderSlash = Path.GetDirectoryName(wagon.WagFilePath) + @"\";
            LoadFilePath = loadFilePath;
        }

        public virtual void Copy(Container containerCopy)
        {
            BaseShapeFileFolderSlash = containerCopy.BaseShapeFileFolderSlash;
            ShapeFileName = containerCopy.ShapeFileName;
            IntrinsicShapeOffset = containerCopy.IntrinsicShapeOffset;
            ContainerType = containerCopy.ContainerType;
            ComputeDimensions();
            Flipped = containerCopy.Flipped;
            MassKG = containerCopy.MassKG;
        }

        public Container(BinaryReader inf, FreightAnimationDiscrete freightAnimDiscrete, ContainerHandlingItem containerStation, bool fromContainerStation = false)
        {
            if (fromContainerStation) ContainerStation = containerStation;
            else Wagon = freightAnimDiscrete.Wagon;
            BaseShapeFileFolderSlash = inf.ReadString();
            ShapeFileName = inf.ReadString();
            IntrinsicShapeOffset.X = inf.ReadSingle();
            IntrinsicShapeOffset.Y = inf.ReadSingle();
            IntrinsicShapeOffset.Z = inf.ReadSingle();
            ContainerType = (ContainerType)inf.ReadInt32();
            ComputeDimensions();
            Flipped = inf.ReadBoolean();
            MassKG = inf.ReadSingle();
            if (fromContainerStation)
            {
                // compute WorldPosition starting from offsets and position of container station
                var containersCount = containerStation.Containers.Count;
                var loadPositionHorizontal = containersCount / containerStation.MaxStackedContainers;
                var loadPositionVertical = containersCount - loadPositionHorizontal * containerStation.MaxStackedContainers;
                var mstsOffset = IntrinsicShapeOffset;
                mstsOffset.Z *= -1;
                var totalOffset = containerStation.StackXNALocations[loadPositionHorizontal] - mstsOffset;
                if (containersCount != 0)
                for (var iPos = (containersCount - 1); iPos >= containersCount- loadPositionVertical; iPos--)
                    totalOffset.Y += containerStation.Containers[iPos].HeightM;
                totalOffset.Z *= -1;
                Vector3.Transform(ref totalOffset, ref containerStation.ShapePosition.XNAMatrix, out totalOffset);
                //               WorldPosition = new WorldLocation(car.WorldPosition.TileX, car.WorldPosition.TileZ,
                //                   totalOffset.X, totalOffset.Y, -totalOffset.Z);
                WorldPosition.XNAMatrix = containerStation.ShapePosition.XNAMatrix;
                WorldPosition.XNAMatrix.Translation = totalOffset;
                WorldPosition.TileX = containerStation.ShapePosition.TileX;
                WorldPosition.TileZ = containerStation.ShapePosition.TileZ;
            }
            else
            {
                RelativeContainerMatrix = ORTSMath.RestoreMatrix(inf);
                WorldPosition.XNAMatrix = Matrix.Multiply(RelativeContainerMatrix, Wagon.WorldPosition.XNAMatrix);
                WorldPosition.TileX = Wagon.WorldPosition.TileX;
                WorldPosition.TileZ = Wagon.WorldPosition.TileZ;
            }
        }

        private void ComputeDimensions()
        {
            switch (ContainerType)
            {
                case ContainerType.C20ft:
                    LengthM = 6.095f;
                    break;
                case ContainerType.C40ft:
                    LengthM = 12.19f;
                    break;
                case ContainerType.C40ftHC:
                    LengthM = 12.19f;
                    HeightM = 2.9f;
                    break;
                case ContainerType.C45ft:
                    LengthM = 13.7f;
                    break;
                case ContainerType.C48ft:
                    LengthM = 14.6f;
                    break;
                case ContainerType.C53ft:
                    LengthM = 16.15f;
                    break;
                default:
                    break;
            }
        }

        public void Update()
        {

        }

        public void Save(BinaryWriter outf, bool fromContainerStation = false)
        {
            outf.Write(BaseShapeFileFolderSlash);
            outf.Write(ShapeFileName);
            outf.Write(IntrinsicShapeOffset.X);
            outf.Write(IntrinsicShapeOffset.Y);
            outf.Write(IntrinsicShapeOffset.Z);
            outf.Write((int)ContainerType);
            outf.Write(Flipped);
            outf.Write(MassKG);
            if (fromContainerStation)
            {

            }
            else
                ORTSMath.SaveMatrix(outf, RelativeContainerMatrix);
        }

        public void LoadFromContainerFile(string loadFilePath)
        {
            var containerFile = new ContainerFile(loadFilePath);
            var containerParameters = containerFile.ContainerParameters;
            ShapeFileName = containerParameters.ShapeFileName;
            Enum.TryParse(containerParameters.ContainerType, out ContainerType containerType);
            ContainerType = containerType;
            ComputeDimensions();
            IntrinsicShapeOffset = containerParameters.IntrinsicShapeOffset;
            IntrinsicShapeOffset.Z *= -1;
            Flipped = containerParameters.Flipped;
        }

        public void ComputeWorldPosition (FreightAnimationDiscrete freightAnimDiscrete)
         {
            WorldPosition.XNAMatrix = freightAnimDiscrete.Wagon.WorldPosition.XNAMatrix;
            WorldPosition.TileX = freightAnimDiscrete.Wagon.WorldPosition.TileX;
            WorldPosition.TileZ = freightAnimDiscrete.Wagon.WorldPosition.TileZ;
            var offset = freightAnimDiscrete.Offset;
//            if (freightAnimDiscrete.Container != null) offset.Y += freightAnimDiscrete.Container.HeightM;
            var translation = Matrix.CreateTranslation(offset - IntrinsicShapeOffset);
            WorldPosition.XNAMatrix = translation * WorldPosition.XNAMatrix;
            var invWagonMatrix = Matrix.Invert(freightAnimDiscrete.Wagon.WorldPosition.XNAMatrix);
            RelativeContainerMatrix = Matrix.Multiply(WorldPosition.XNAMatrix, invWagonMatrix);
        }
    }

    public class ContainerManager
    {
        readonly Simulator Simulator;
        public Dictionary<int, ContainerHandlingItem> ContainerHandlingItems;
        public List<Container> Containers;
        public static Dictionary<string, Container> LoadedContainers = new Dictionary<string, Container>();

        public ContainerManager(Simulator simulator)
        {
            Simulator = simulator;
            ContainerHandlingItems = new Dictionary<int, ContainerHandlingItem>();
            Containers = new List<Container>();
        }

/*        static Dictionary<int, FuelPickupItem> GetContainerHandlingItemsFromDB(TrackNode[] trackNodes, TrItem[] trItemTable)
        {
            return (from trackNode in trackNodes
                    where trackNode != null && trackNode.TrVectorNode != null && trackNode.TrVectorNode.NoItemRefs > 0
                    from itemRef in trackNode.TrVectorNode.TrItemRefs.Distinct()
                    where trItemTable[itemRef] != null && trItemTable[itemRef].ItemType == TrItem.trItemType.trPICKUP
                    select new KeyValuePair<int, ContainerHandlingItem>(itemRef, new ContainerHandlingItem(trackNode, trItemTable[itemRef])))
                    .ToDictionary(_ => _.Key, _ => _.Value);
        }*/

        public ContainerHandlingItem CreateContainerStation(WorldPosition shapePosition, IEnumerable<int> trackIDs, PickupObj thisWorldObj)
        {
            var trackItem = trackIDs.Select(id => Simulator.FuelManager.FuelPickupItems[id]).First();
            return new ContainerHandlingItem(Simulator, shapePosition, trackItem, thisWorldObj);
        }

        public void Save(BinaryWriter outf)
        {
            foreach (var containerStation in ContainerHandlingItems.Values)
                containerStation.Save(outf);
        }

        public void Restore(BinaryReader inf)
        {
            foreach (var containerStation in ContainerHandlingItems.Values)
                containerStation.Restore(inf);
        }

        public void Update()
        {
            foreach (var containerStation in ContainerHandlingItems.Values)
                containerStation.Update();
        }


    } 

    public class ContainerHandlingItem : FuelPickupItem
    {
        public Simulator Simulator;


        public List<Container> Containers = new List<Container>();
        public WorldPosition ShapePosition;
        public int MaxStackedContainers;
        public Vector3[] StackXNALocations;
        public int StackXNALocationsCount;
 //       public int[] AllocatedContainerIndices;
        public float PickingSurfaceYOffset;
        public Vector3 PickingSurfaceRelativeTopStartPosition;
        public float TargetX;
        public float TargetY;
        public float TargetZ;
        public float TargetGrabber01;
        public float TargetGrabber02;
        public float ActualX;
        public float ActualY;
        public float ActualZ;
        public float ActualGrabber01;
        public float ActualGrabber02;
        public bool MoveX;
        public bool MoveY;
        public bool MoveZ;
        public bool MoveGrabber;
        public float Grabber01Offset;
        private int freePositionVertical;
        private int loadPositionVertical;
        public Container UnloadContainer;
        public Matrix RelativeContainerPosition;
        public Matrix InitialInvAnimationXNAMatrix = Matrix.Identity;
        public Matrix AnimationXNAMatrix = Matrix.Identity;
        private float GeneralVerticalOffset;
        public float MinZSpan;
        public float Grabber01Max;
        public float Grabber02Max;
        public float MaxGrabberSpan;
        private FreightAnimationDiscrete LinkedFreightAnimation;
        public float LoadingEndDelayS { get; protected set; } = 2f;
        public float UnloadingStartDelayS { get; protected set; } = 3f;
        protected Timer DelayTimer;

        private bool messageWritten = false;
        private bool ContainerFlipped = false;
        private bool WagonFlipped = false;

        public enum ContainerStationStatus
        {
            Idle,
            LoadRaiseToPick,
            LoadHorizontallyMoveToPick,
            LoadLowerToPick,
            LoadWaitingForPick,
            LoadRaiseToLayOnWagon,
            LoadHorizontallyMoveToLayOnWagon,
            LoadLowerToLayOnWagon,
            LoadWaitingForLayingOnWagon,
            UnloadRaiseToPick,
            UnloadHorizontallyMoveToPick,
            UnloadLowerToPick,
            UnloadWaitingForPick,
            UnloadRaiseToLayOnEarth,
            UnloadHorizontallyMoveToLayOnEarth,
            UnloadLowerToLayOnEarth,
            UnloadWaitingForLayingOnEarth,
            RaiseToIdle,
        }

        public ContainerStationStatus Status = ContainerStationStatus.Idle;
        public int AttachedContainerIndex = -1;
        public double TimerStartTime { get; set; }

        public ContainerHandlingItem(Simulator simulator, TrackNode trackNode, TrItem trItem)
            : base(trackNode, trItem)
        {

        }

        public ContainerHandlingItem(Simulator simulator, WorldPosition shapePosition, FuelPickupItem item, PickupObj thisWorldObj)
        {
            Simulator = simulator;
            ShapePosition = shapePosition;
            Location = item.Location;
            TrackNode = item.TrackNode;
            MaxStackedContainers = thisWorldObj.MaxStackedContainers;
            StackXNALocationsCount = thisWorldObj.StackXNALocations.Locations.Length;
            StackXNALocations = new Vector3[StackXNALocationsCount];
//            AllocatedContainerIndices = new int[StackXNALocationsCount * MaxStackedContainers];
 //           int i;
//            for (i = 0; i < AllocatedContainerIndices.Length; i++)
//                AllocatedContainerIndices[i] = -1;
            int i = 0;
            foreach (var stackLocation in thisWorldObj.StackXNALocations.Locations)
            {
                StackXNALocations[i].X = stackLocation.X;
                StackXNALocations[i].Y = stackLocation.Y;
                StackXNALocations[i].Z = stackLocation.Z;
                i++;
            }
            PickingSurfaceYOffset = thisWorldObj.PickingSurfaceYOffset;
            PickingSurfaceRelativeTopStartPosition = thisWorldObj.PickingSurfaceRelativeTopStartPosition;
            MaxGrabberSpan = thisWorldObj.MaxGrabberSpan;
            DelayTimer = new Timer(this);
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write((int)Status);
            outf.Write(GeneralVerticalOffset);
            ORTSMath.SaveMatrix(outf, RelativeContainerPosition);
            outf.Write(Containers.Count);
            foreach (var container in Containers)
                container.Save(outf, fromContainerStation:true);

        }

        public void Restore(BinaryReader inf)
        {
            var status = (ContainerStationStatus)inf.ReadInt32();
            // in general start with preceding state
            switch (status)
            {
                case ContainerStationStatus.Idle:
                    Status = status;
                    break;
                 default:
                    Status = ContainerStationStatus.Idle;
                    break;
            }
            GeneralVerticalOffset = inf.ReadSingle();
            RelativeContainerPosition = ORTSMath.RestoreMatrix(inf);
            var nContainer = inf.ReadInt32();
            if (nContainer > 0)
                for (int i = 0; i < nContainer; i++)
                {
                    var container = new Container(inf, null, this, fromContainerStation:true);
                    Containers.Add(container);
                    Simulator.ContainerManager.Containers.Add(container);
                }
        }

        public bool Refill()
        {
            while (MSTSWagon.RefillProcess.OkToRefill)
            {
                return true;
            }
            if (!MSTSWagon.RefillProcess.OkToRefill)
                return false;
            return false;
        }

        public void Update()
        {
            var subMissionTerminated = false;
            if ((!MoveX || Math.Abs(ActualX - TargetX) < 0.02f) && (!MoveY || Math.Abs(ActualY - TargetY) < 0.02f) && 
                (!MoveZ || Math.Abs(ActualZ - TargetZ) < 0.02f))
                subMissionTerminated = true;
            if (Math.Abs(ActualX - TargetX) < 0.02f) MoveX = false;
            if (Math.Abs(ActualZ - TargetZ) < 0.02f) MoveZ = false;

            switch (Status)
            {
                case ContainerStationStatus.Idle:
                    break;
                case ContainerStationStatus.LoadRaiseToPick:
                    if (subMissionTerminated)
                    {
                        MoveY = false;
                        Status = ContainerStationStatus.LoadHorizontallyMoveToPick;
                        var loadPositionHorizontal = (Containers.Count - 1) / MaxStackedContainers;
                        loadPositionVertical = (Containers.Count - 1) - loadPositionHorizontal * MaxStackedContainers;
                        TargetX = StackXNALocations[loadPositionHorizontal].X;
                        TargetZ = StackXNALocations[loadPositionHorizontal].Z;
 //                       TargetX = PickingSurfaceRelativeTopStartPosition.X;
 //                       TargetZ = PickingSurfaceRelativeTopStartPosition.Z - RelativeContainerPosition.Translation.Z + UnloadContainer.IntrinsicShapeZOffset;
 //                       RelativeContainerPosition.M43 = UnloadContainer.IntrinsicShapeZOffset;
                        MoveX = true;
                        MoveZ = true;
                    }
                    break;
                case ContainerStationStatus.LoadHorizontallyMoveToPick:
                    if (subMissionTerminated && (!MoveGrabber ||
                        Math.Abs(ActualGrabber01 - TargetGrabber01) < 0.02f && Math.Abs(ActualGrabber02 - TargetGrabber02) < 0.02))
                    {
                        MoveX = false;
                        MoveZ = false;
                        MoveGrabber = false;
                        Status = ContainerStationStatus.LoadLowerToPick;
                        //                       TargetY = UnloadContainer.HeightM + UnloadContainer.IntrinsicShapeYOffset - PickingSurfaceYOffset;
                        TargetY = ComputeTargetYBase(loadPositionVertical) - PickingSurfaceYOffset;
                        RelativeContainerPosition.M42 = PickingSurfaceYOffset - ComputeTargetYBase(loadPositionVertical) + Containers[Containers.Count -1].WorldPosition.XNAMatrix.M42 + InitialInvAnimationXNAMatrix.M42;
                        MoveY = true;
                    }
                    break;
                case ContainerStationStatus.LoadLowerToPick:
                    if (subMissionTerminated)
                    {
                        MoveY = false;
                        if (DelayTimer == null)
                            DelayTimer = new Timer(this);
                        DelayTimer.Setup(UnloadingStartDelayS);
                        DelayTimer.Start();
                        Status = ContainerStationStatus.LoadWaitingForPick;
                    }
                    break;
                case ContainerStationStatus.LoadWaitingForPick:
                    if (DelayTimer.Triggered)
                    {
                        DelayTimer.Stop();
                        AttachedContainerIndex = Containers.Count - 1;
                        TargetY = PickingSurfaceRelativeTopStartPosition.Y;
                        MoveY = true;
                        Status = ContainerStationStatus.LoadRaiseToLayOnWagon;
                        messageWritten = false;
                    }
                    break;
                case ContainerStationStatus.LoadRaiseToLayOnWagon:
                    if (subMissionTerminated || messageWritten)
                    {
                        if (Math.Abs(LinkedFreightAnimation.Wagon.SpeedMpS) < 0.01f)
                        {
                            MoveY = false;
                            // Search first free position
                            var container = Containers[AttachedContainerIndex];
                            var freePositionHorizontal = AttachedContainerIndex / MaxStackedContainers;
                            freePositionVertical = AttachedContainerIndex - freePositionHorizontal * MaxStackedContainers;
                            TargetX = PickingSurfaceRelativeTopStartPosition.X;
                            WorldPosition animWorldPosition = new WorldPosition(LinkedFreightAnimation.Wagon.WorldPosition);
            //                var translation = Matrix.CreateTranslation(LinkedFreightAnimation.Offset);
            //                animWorldPosition.XNAMatrix = translation * animWorldPosition.XNAMatrix;
                            var relativeAnimationPosition = Matrix.Multiply(animWorldPosition.XNAMatrix, InitialInvAnimationXNAMatrix);
                            // compute where within the free space to lay down the container
                            var freightAnims = LinkedFreightAnimation.FreightAnimations;
                            var offsetZ = LinkedFreightAnimation.Offset.Z;
                            if (Math.Abs(LinkedFreightAnimation.LoadingAreaLength - container.LengthM) > 0.01)
                            {
                                var loadedFreightAnim = new FreightAnimationDiscrete(LinkedFreightAnimation, LinkedFreightAnimation.FreightAnimations);
                                var loadedIntakePoint = loadedFreightAnim.LinkedIntakePoint;
                                if (!(container.ContainerType == ContainerType.C20ft && LinkedFreightAnimation.LoadPosition == LoadPosition.Center &&
                                    LinkedFreightAnimation.LoadingAreaLength + 0.01f >= 12.19))
                                {
                                    if (LinkedFreightAnimation.LoadingAreaLength == freightAnims.LoadingAreaLength && !freightAnims.DoubleStacker)
                                    {
                                        loadedFreightAnim.LoadPosition = LoadPosition.Rear;
                                        loadedFreightAnim.Offset.Z = freightAnims.Offset.Z + (freightAnims.LoadingAreaLength - container.LengthM) / 2;
                                    }
                                    else if (loadedFreightAnim.LoadPosition != LoadPosition.Center && loadedFreightAnim.LoadPosition != LoadPosition.Above)
                                    {
                                        switch (loadedFreightAnim.LoadPosition)
                                        {
                                            case LoadPosition.Front:
                                                loadedFreightAnim.Offset.Z = freightAnims.Offset.Z -(freightAnims.LoadingAreaLength - container.LengthM) / 2;
                                                break;
                                            case LoadPosition.Rear:
                                                loadedFreightAnim.Offset.Z = freightAnims.Offset.Z + (freightAnims.LoadingAreaLength - container.LengthM) / 2;
                                                break;
                                            case LoadPosition.CenterFront:
                                                loadedFreightAnim.Offset.Z = freightAnims.Offset.Z - container.LengthM / 2;
                                                break;
                                            case LoadPosition.CenterRear:
                                                loadedFreightAnim.Offset.Z = freightAnims.Offset.Z + container.LengthM / 2;
                                                break;
                                            default:
                                                break;
                                        }
                                    }         
                                }
                                else
                                // don't lay down a short container in the middle of the wagon
                                {
                                    if (LinkedFreightAnimation.LoadingAreaLength == freightAnims.LoadingAreaLength && !freightAnims.DoubleStacker)
                                    {
                                        loadedFreightAnim.LoadPosition = LoadPosition.Rear;
                                        loadedFreightAnim.Offset.Z = freightAnims.Offset.Z + (freightAnims.LoadingAreaLength - container.LengthM) / 2;
                                    }
                                    else
                                    {
                                        loadedFreightAnim.LoadPosition = LoadPosition.CenterFront;
                                        loadedFreightAnim.Offset.Z = freightAnims.Offset.Z - container.LengthM / 2;
                                    }
                                }
                                loadedFreightAnim.LoadingAreaLength = container.LengthM;
                                loadedIntakePoint.OffsetM = -loadedFreightAnim.Offset.Z;
                                freightAnims.Animations.Add(loadedFreightAnim);
                                loadedFreightAnim.Container = container;
                                freightAnims.UpdateEmptyFreightAnims(container.LengthM);
                                // Too early to have container on wagon
                                loadedFreightAnim.Container = null;
                                LinkedFreightAnimation = loadedFreightAnim;
                            }
                            else
                            {
                                freightAnims.EmptyAnimations.Remove(LinkedFreightAnimation);
                                freightAnims.Animations.Add(LinkedFreightAnimation);
                                (freightAnims.Animations.Last() as FreightAnimationDiscrete).Container = container;
                                freightAnims.EmptyAbove();
                                (freightAnims.Animations.Last() as FreightAnimationDiscrete).Container = null;


                            }
                            TargetZ = PickingSurfaceRelativeTopStartPosition.Z - relativeAnimationPosition.Translation.Z - LinkedFreightAnimation.Offset.Z * 
                                (WagonFlipped ? -1 : 1);
 /*                           if (TargetZ < PickingSurfaceRelativeTopStartPosition.Z)
                            {
                                if (!messageWritten)
                                {
                                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Wagon out of range: move wagon towards crane by {0} metres",
                                        PickingSurfaceRelativeTopStartPosition.Z - TargetZ));
                                    messageWritten = true;
                                }
                            }
                            else*/
                            {
                                MoveX = MoveZ = true;
                                Status = ContainerStationStatus.LoadHorizontallyMoveToLayOnWagon;
                            }
                        }
                    }
                    break;
                case ContainerStationStatus.LoadHorizontallyMoveToLayOnWagon:
                    if (subMissionTerminated)
                    {
                        MoveX = MoveZ = false;
                        TargetY = Containers[AttachedContainerIndex].HeightM + LinkedFreightAnimation.Wagon.WorldPosition.XNAMatrix.M42
                            + LinkedFreightAnimation.Offset.Y - ShapePosition.XNAMatrix.M42 - PickingSurfaceYOffset;
                        if (LinkedFreightAnimation.LoadPosition == LoadPosition.Above)
                        {
                            var addHeight = 0.0f;
                            foreach (var freightAnim in LinkedFreightAnimation.FreightAnimations.Animations)
                            {
                                if (freightAnim  is FreightAnimationDiscrete discreteFreightAnim && discreteFreightAnim.LoadPosition != LoadPosition.Above)
                                {
                                    addHeight = discreteFreightAnim.Container.HeightM;
                                    break;
                                }
                            }
                            TargetY += addHeight;
                        }
                        MoveY = true;
                        Status = ContainerStationStatus.LoadLowerToLayOnWagon;
                    }
                    break;
                case ContainerStationStatus.LoadLowerToLayOnWagon:
                    if (subMissionTerminated)
                    {
                        MoveY = false;
                        DelayTimer = new Timer(this);
                        DelayTimer.Setup(LoadingEndDelayS);
                        DelayTimer.Start();
                        Status = ContainerStationStatus.LoadWaitingForLayingOnWagon;
                        var invertedWagonMatrix = Matrix.Invert(LinkedFreightAnimation.Wagon.WorldPosition.XNAMatrix);
                        var freightAnim = LinkedFreightAnimation.Wagon.FreightAnimations.Animations.Last() as FreightAnimationDiscrete;
                        freightAnim.Container = Containers[AttachedContainerIndex];
                        freightAnim.Container.Wagon = LinkedFreightAnimation.Wagon;
                        freightAnim.Container.RelativeContainerMatrix = Matrix.Multiply(LinkedFreightAnimation.Container.WorldPosition.XNAMatrix, invertedWagonMatrix);
                        Containers.RemoveAt(AttachedContainerIndex);
                        AttachedContainerIndex = -1;
                        freightAnim.Loaded = true;
                    }
                    break;
                case ContainerStationStatus.LoadWaitingForLayingOnWagon:
                    if (DelayTimer.Triggered)
                    {
                        DelayTimer.Stop();
                        TargetY = PickingSurfaceRelativeTopStartPosition.Y;
                        MoveY = true;
                        Status = ContainerStationStatus.RaiseToIdle;
                        messageWritten = false;
                    }
                    break;
                case ContainerStationStatus.UnloadRaiseToPick:
                    if (subMissionTerminated || messageWritten)
                    {
                        if (Math.Abs(LinkedFreightAnimation.Wagon.SpeedMpS) < 0.01f)
                        {
                            MoveY = false;
                            UnloadContainer = LinkedFreightAnimation.Container;
                            TargetX = PickingSurfaceRelativeTopStartPosition.X;
                            TargetZ = PickingSurfaceRelativeTopStartPosition.Z - RelativeContainerPosition.Translation.Z - UnloadContainer.IntrinsicShapeOffset.Z * 
                                (ContainerFlipped ? -1 : 1);                              
 /*                           if (TargetZ < PickingSurfaceRelativeTopStartPosition.Z)
                            {
                                if (!messageWritten)
                                {
                                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Wagon out of range: move wagon towards crane by {0} metres",
                                        PickingSurfaceRelativeTopStartPosition.Z - TargetZ));
                                    messageWritten = true;
                                }
                            }
                            else*/
                            {
                                Status = ContainerStationStatus.UnloadHorizontallyMoveToPick;
                                RelativeContainerPosition.M43 = UnloadContainer.IntrinsicShapeOffset.Z * (ContainerFlipped ? 1 : -1);
                                MoveX = true;
                                MoveZ = true;
                                UnloadContainer.ContainerStation = this;
                                Containers.Add(UnloadContainer);
                            }
                        }
                    }
                    break;
                case ContainerStationStatus.UnloadHorizontallyMoveToPick:
                    if (subMissionTerminated && (!MoveGrabber ||
                        Math.Abs(ActualGrabber01 - TargetGrabber01) < 0.02f && Math.Abs(ActualGrabber02 - TargetGrabber02) < 0.02))
                    {
                        MoveX = false;
                        MoveZ = false;
                        MoveGrabber = false;
                        Status = ContainerStationStatus.UnloadLowerToPick;
                        TargetY = - PickingSurfaceYOffset + UnloadContainer.HeightM + UnloadContainer.IntrinsicShapeOffset.Y + GeneralVerticalOffset - PickingSurfaceYOffset;
                        RelativeContainerPosition.M42 = PickingSurfaceYOffset - (UnloadContainer.HeightM + UnloadContainer.IntrinsicShapeOffset.Y);
                        MoveY = true;
                    }
                    break;
                case ContainerStationStatus.UnloadLowerToPick:
                    if (subMissionTerminated)
                    {
                        MoveY = false;
                        if (DelayTimer == null)
                            DelayTimer = new Timer(this);
                        DelayTimer.Setup(UnloadingStartDelayS);
                        DelayTimer.Start();
                        Status = ContainerStationStatus.UnloadWaitingForPick;
                    }
                    break;
                case ContainerStationStatus.UnloadWaitingForPick:
                    if (DelayTimer.Triggered)
                    {
                        LinkedFreightAnimation.Loaded = false;
                        LinkedFreightAnimation.Container = null;
                        var freightAnims = UnloadContainer.Wagon.FreightAnimations;
                        if (LinkedFreightAnimation.LoadPosition == LoadPosition.Above)
                        {
                            LinkedFreightAnimation.Offset.Y = freightAnims.Offset.Y;
                            LinkedFreightAnimation.AboveLoadingAreaLength = freightAnims.AboveLoadingAreaLength;
                            freightAnims.EmptyAnimations.Add(LinkedFreightAnimation);
                        }
                        else
                        {
                            var discreteAnimCount = 0;
                            if (freightAnims.EmptyAnimations.Count > 0 && freightAnims.EmptyAnimations.Last().LoadPosition == LoadPosition.Above)
                            {
                                UnloadContainer.Wagon.IntakePointList.Remove(freightAnims.EmptyAnimations.Last().LinkedIntakePoint);
                                freightAnims.EmptyAnimations.Remove(freightAnims.EmptyAnimations.Last());
                            }
                            foreach (var freightAnim in UnloadContainer.Wagon.FreightAnimations.Animations)
                            {
                                if (freightAnim is FreightAnimationDiscrete discreteFreightAnim)
                                {
                                    if (discreteFreightAnim.LoadPosition != LoadPosition.Above)
                                        discreteAnimCount++;
                                }
                            }
                            if (discreteAnimCount == 1)
                            {
                                foreach (var emptyAnim in freightAnims.EmptyAnimations)
                                {
                                    UnloadContainer.Wagon.IntakePointList.Remove(emptyAnim.LinkedIntakePoint);
                                }
                                freightAnims.EmptyAnimations.Clear();
                                freightAnims.EmptyAnimations.Add(new FreightAnimationDiscrete(freightAnims, LoadPosition.Center));
                                UnloadContainer.Wagon.IntakePointList.Remove(LinkedFreightAnimation.LinkedIntakePoint);
                            }
                            else
                            {
                                freightAnims.EmptyAnimations.Add(LinkedFreightAnimation);
                                LinkedFreightAnimation.Container = null;
                                LinkedFreightAnimation.Loaded = false;
                                freightAnims.MergeEmptyAnims();
                            }

                        }
                        freightAnims.Animations.Remove(LinkedFreightAnimation);
                        LinkedFreightAnimation = null;
                        DelayTimer.Stop();
                        AttachedContainerIndex = Containers.Count - 1;
                        TargetY = PickingSurfaceRelativeTopStartPosition.Y;
                        MoveY = true;
                        Status = ContainerStationStatus.UnloadRaiseToLayOnEarth;
                    }
                    break;
                case ContainerStationStatus.UnloadRaiseToLayOnEarth:
                    if (subMissionTerminated)
                    {
                        MoveY = false;
                        // Search first free position
                        var freePositionHorizontal = AttachedContainerIndex / MaxStackedContainers;
                        freePositionVertical = AttachedContainerIndex - freePositionHorizontal * MaxStackedContainers;
                        TargetX = StackXNALocations[freePositionHorizontal].X;
                        TargetZ = StackXNALocations[freePositionHorizontal].Z;
                        MoveX = MoveZ = true;
                        Status = ContainerStationStatus.UnloadHorizontallyMoveToLayOnEarth;
                    }
                    break;
                case ContainerStationStatus.UnloadHorizontallyMoveToLayOnEarth:
                    if (subMissionTerminated)
                    {
                        MoveX = MoveZ = false;
                        TargetY = ComputeTargetYBase(freePositionVertical) - PickingSurfaceYOffset;
                        MoveY = true;
                        Status = ContainerStationStatus.UnloadLowerToLayOnEarth;
                    }
                    break;
                case ContainerStationStatus.UnloadLowerToLayOnEarth:
                    if (subMissionTerminated)
                    {
                        MoveY = false;
                        DelayTimer.Setup(LoadingEndDelayS);
                        DelayTimer.Start();
                        Status = ContainerStationStatus.UnloadWaitingForLayingOnEarth;
                    }
                    break;
                case ContainerStationStatus.UnloadWaitingForLayingOnEarth:
                    if (DelayTimer.Triggered)
                    {
                        DelayTimer.Stop();
                        RelativeContainerPosition.M43 = 0;
                        AttachedContainerIndex = -1;
                        TargetY = PickingSurfaceRelativeTopStartPosition.Y;
                        MoveY = true;
                        Status = ContainerStationStatus.RaiseToIdle;
                    }
                    break;
                case ContainerStationStatus.RaiseToIdle:
                    if (subMissionTerminated)
                    {
                        MoveY = false;
                        Status = ContainerStationStatus.Idle;
                        MSTSWagon.RefillProcess.OkToRefill = false;
                    }
                    break;
                default:
                    break;
            }
        }

        public void PrepareForUnload(FreightAnimationDiscrete linkedFreightAnimation)
        {
            LinkedFreightAnimation = linkedFreightAnimation;
            RelativeContainerPosition = new Matrix();
            LinkedFreightAnimation.Wagon.WorldPosition.NormalizeTo(ShapePosition.TileX, ShapePosition.TileZ);
            var container = LinkedFreightAnimation.Container;
            RelativeContainerPosition = Matrix.Multiply(container.WorldPosition.XNAMatrix, InitialInvAnimationXNAMatrix);
            RelativeContainerPosition.M42 += PickingSurfaceYOffset;
            RelativeContainerPosition.M41 -= PickingSurfaceRelativeTopStartPosition.X;
            GeneralVerticalOffset = RelativeContainerPosition.M42;
//            RelativeContainerPosition.Translation += LinkedFreightAnimation.Offset;
            ContainerFlipped = (Math.Abs(InitialInvAnimationXNAMatrix.M11 - container.WorldPosition.XNAMatrix.M11) < 0.1f ? false : true);
            Status = ContainerStationStatus.UnloadRaiseToPick;
            TargetY = PickingSurfaceRelativeTopStartPosition.Y;
            MoveY = true;
            SetGrabbers(container);
        }

        public void PrepareForLoad(FreightAnimationDiscrete linkedFreightAnimation)
        {
            //           var invAnimationXNAMatrix = Matrix.Invert(InitialAnimationXNAMatrix);
            //           RelativeContainerPosition = new Matrix();
            //           RelativeContainerPosition = Matrix.Multiply(Containers.Last().WorldPosition.XNAMatrix, invAnimationXNAMatrix);
            LinkedFreightAnimation = linkedFreightAnimation;
            ContainerFlipped = (Math.Abs(InitialInvAnimationXNAMatrix.M11 - Containers[Containers.Count - 1].WorldPosition.XNAMatrix.M11) < 0.1f ? false : true);
            WagonFlipped = (Math.Abs(InitialInvAnimationXNAMatrix.M11 - LinkedFreightAnimation.Wagon.WorldPosition.XNAMatrix.M11) < 0.1f ? false : true);
            if (ContainerFlipped)
            {
                RelativeContainerPosition.M11 = -1;
                RelativeContainerPosition.M33 = -1;
            }
            else
            {
                RelativeContainerPosition.M11 = 1;
                RelativeContainerPosition.M33 = 1;
            }
            RelativeContainerPosition.M43 = Containers[Containers.Count - 1].IntrinsicShapeOffset.Z * (ContainerFlipped ? 1 : -1);
            Status = ContainerStationStatus.LoadRaiseToPick;
            TargetY = PickingSurfaceRelativeTopStartPosition.Y;
            MoveY = true;
            SetGrabbers(Containers[Containers.Count - 1]);
        }

        public float ComputeTargetYBase(int positionVertical)
        {
            float retVal = 0;
            for (var iPos = (Containers.Count - 1); iPos >= (Containers.Count - 1) - positionVertical; iPos--)
                retVal += Containers[iPos].HeightM;
            return retVal;
        }

        /// <summary>
        /// Move container together with container station
        /// </summary>
        /// 
        public void TransferContainer(Matrix animationXNAMatrix)
        {
            AnimationXNAMatrix = animationXNAMatrix;
            if (AttachedContainerIndex != -1)
            {
                // Move together also containers
                Containers[AttachedContainerIndex].WorldPosition.XNAMatrix = Matrix.Multiply(RelativeContainerPosition, AnimationXNAMatrix);
            }
        }

        public void ReInitPositionOffset (Matrix animationXNAMatrix)
        {
            InitialInvAnimationXNAMatrix = Matrix.Invert(animationXNAMatrix);
        }

        public void PassSpanParameters(float z1Span, float z2Span, float grabber01Max, float grabber02Max)
        {
            MinZSpan = Math.Min(Math.Abs(z1Span), Math.Abs(z2Span));
            Grabber01Max = grabber01Max;
            Grabber02Max = grabber02Max;

        }

        private void SetGrabbers(Container container)
        {
            TargetGrabber01 = Math.Min(Grabber01Max, (container.LengthM - MaxGrabberSpan) / 2 + Grabber01Max);
            TargetGrabber02 = Math.Max(Grabber02Max, (-container.LengthM + MaxGrabberSpan) / 2 + Grabber02Max);
            MoveGrabber = true;
        }

    } // end Class ContainerHandlingItem
}

