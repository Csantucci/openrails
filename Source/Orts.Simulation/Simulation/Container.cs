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
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
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

        readonly Simulator Simulator;
        public string ShapeFileName;
        public readonly string BaseShapeFileFolderSlash;
        public float MassKG = 2000;
        public float WidthM = 2.44f;
        public float LengthM = 12.19f;       
        public float HeightM = 2.59f;
        public ContainerType ContainerType = ContainerType.C40ft;
        public bool Flipped = false;

        public WorldPosition WorldPosition = new WorldPosition();  // current position of the container
        public float RealRelativeYOffset = 0;
        public float RealRelativeZOffset = 0;
        public float XOffset = 0;
        public float YOffset = 0;
        public float ZOffset = 0;
        public Vector3 IntrinsicShapeOffset;
        public ContainerHandlingItem ContainerStation;
        public Matrix RelativeContainerMatrix = Matrix.Identity;

        public Container(Simulator simulator)
        {
            Simulator = simulator;
 
        }

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
            Simulator = freightAnimDiscrete.Wagon.Simulator;
            BaseShapeFileFolderSlash = Path.GetDirectoryName(freightAnimDiscrete.Wagon.WagFilePath) + @"\";
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[]
            {
                new STFReader.TokenProcessor("shape", ()=>{ ShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("intrinsicshapeoffset", ()=>{ IntrinsicShapeOffset = stf.ReadVector3Block(STFReader.UNITS.Distance,  new Vector3(0, 0, 0)); }),
                new STFReader.TokenProcessor("containertype", ()=>
                {
                    var containerTypeString = stf.ReadStringBlock(null);
                    Enum.TryParse<ContainerType>(containerTypeString, out var containerType);
                    ContainerType = containerType;
                    ComputeDimensions();
                 }),
                new STFReader.TokenProcessor("offset", ()=>{
                    stf.MustMatch("(");
                    XOffset = stf.ReadFloat(STFReader.UNITS.Distance, 0);
                    YOffset = stf.ReadFloat(STFReader.UNITS.Distance, 0);
                    ZOffset = stf.ReadFloat(STFReader.UNITS.Distance, 0);
                    stf.MustMatch(")");
                }),
                new STFReader.TokenProcessor("flip", ()=>{ Flipped = stf.ReadBoolBlock(true);}),
                new STFReader.TokenProcessor("loadweight", ()=>{ MassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, 0);
                }),
            });
            WorldPosition.XNAMatrix = freightAnimDiscrete.Wagon.WorldPosition.XNAMatrix;
            WorldPosition.TileX = freightAnimDiscrete.Wagon.WorldPosition.TileX;
            WorldPosition.TileZ = freightAnimDiscrete.Wagon.WorldPosition.TileZ;
            var translation = Matrix.CreateTranslation(XOffset, YOffset, ZOffset);
            WorldPosition.XNAMatrix = translation * WorldPosition.XNAMatrix;
        }

        public Container(FreightAnimationDiscrete freightAnimaDiscreteCopy, FreightAnimationDiscrete freightAnimDiscrete)
        {
            Simulator = freightAnimDiscrete.Wagon.Simulator;
            var containerCopy = freightAnimaDiscreteCopy.Container;
            BaseShapeFileFolderSlash = containerCopy.BaseShapeFileFolderSlash;
            ShapeFileName = containerCopy.ShapeFileName;
            IntrinsicShapeOffset = containerCopy.IntrinsicShapeOffset;
            ContainerType = containerCopy.ContainerType;
            ComputeDimensions();
            XOffset = containerCopy.XOffset;
            YOffset = containerCopy.YOffset;
            ZOffset = containerCopy.ZOffset;
            Flipped = containerCopy.Flipped;
            MassKG = containerCopy.MassKG;
            WorldPosition.XNAMatrix = freightAnimDiscrete.Wagon.WorldPosition.XNAMatrix;
            WorldPosition.TileX = freightAnimDiscrete.Wagon.WorldPosition.TileX;
            WorldPosition.TileZ = freightAnimDiscrete.Wagon.WorldPosition.TileZ;
            var translation = Matrix.CreateTranslation(XOffset, YOffset, ZOffset);
            WorldPosition.XNAMatrix = translation * WorldPosition.XNAMatrix;
        }

        private void ComputeDimensions()
        {
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
        }

        public void Update()
        {

        }

    }

    public class ContainerManager
    {
        readonly Simulator Simulator;
        public Dictionary<int, ContainerHandlingItem> ContainerHandlingItems;
        public List<Container> Containers;

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
        public int[] AllocatedContainerIndices;
        public float PickingSurfaceYOffset;
        public Vector3 PickingSurfaceRelativeTopStartPosition;
        public float TargetX;
        public float TargetY;
        public float TargetZ;
        public float ActualX;
        public float ActualY;
        public float ActualZ;
        public bool MoveX;
        public bool MoveY;
        public bool MoveZ;
        private int freePositionVertical;
        private int loadPositionVertical;
        public Container UnloadContainer;
        public Matrix GeneralRelativeContainerPosition;
        public Matrix RelativeContainerPosition;
        public Matrix InitialInvAnimationXNAMatrix = Matrix.Identity;
        public Matrix AnimationXNAMatrix = Matrix.Identity;
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
            AllocatedContainerIndices = new int[StackXNALocationsCount * MaxStackedContainers];
            int i;
            for (i = 0; i < AllocatedContainerIndices.Length; i++)
                AllocatedContainerIndices[i] = -1;
            i = 0;
            foreach (var stackLocation in thisWorldObj.StackXNALocations.Locations)
            {
                StackXNALocations[i].X = stackLocation.X;
                StackXNALocations[i].Y = stackLocation.Y;
                StackXNALocations[i].Z = stackLocation.Z;
                i++;
            }
            PickingSurfaceYOffset = thisWorldObj.PickingSurfaceYOffset;
            PickingSurfaceRelativeTopStartPosition = thisWorldObj.PickingSurfaceRelativeTopStartPosition;
            DelayTimer = new Timer(this);
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
            if (Math.Abs(ActualX - TargetX) < 0.02f && Math.Abs(ActualY - TargetY) < 0.02f && Math.Abs(ActualZ - TargetZ) < 0.02f)
                subMissionTerminated = true;
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
                    if (subMissionTerminated)
                    {
                        MoveX = false;
                        MoveZ = false;
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
                            var translation = Matrix.CreateTranslation(container.XOffset, container.YOffset, container.ZOffset);
                            animWorldPosition.XNAMatrix = translation * animWorldPosition.XNAMatrix;
                            /*                    IntrinsicShapeYOffset = freightAnimDiscrete.IntrinsicShapeYOffset;
                                                IntrinsicShapeZOffset = freightAnimDiscrete.IntrinsicShapeZOffset;
                                                RealRelativeYOffset = IntrinsicShapeYOffset + freightAnimDiscrete.YOffset;*/
                            var relativeAnimationPosition = Matrix.Multiply(animWorldPosition.XNAMatrix, InitialInvAnimationXNAMatrix);
                            TargetZ = PickingSurfaceRelativeTopStartPosition.Z - relativeAnimationPosition.Translation.Z + LinkedFreightAnimation.LoadingSurfaceOffset.Z * 
                                (WagonFlipped ? -1 : 1);
                            if (TargetZ < PickingSurfaceRelativeTopStartPosition.Z)
                            {
                                if (!messageWritten)
                                {
                                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Wagon out of range: move wagon towards crane by {0} metres",
                                        PickingSurfaceRelativeTopStartPosition.Z - TargetZ));
                                    messageWritten = true;
                                }
                            }
                            else
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
                        TargetY = Containers[AttachedContainerIndex].HeightM + Containers[AttachedContainerIndex].IntrinsicShapeOffset.Y - PickingSurfaceYOffset + GeneralRelativeContainerPosition.M42 - PickingSurfaceYOffset;
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
                        LinkedFreightAnimation.Container = Containers[AttachedContainerIndex];
                        var invertedWagonMatrix = Matrix.Invert(LinkedFreightAnimation.Wagon.WorldPosition.XNAMatrix);
                        LinkedFreightAnimation.Container.RelativeContainerMatrix = Matrix.Multiply(LinkedFreightAnimation.Container.WorldPosition.XNAMatrix, invertedWagonMatrix);
                        Containers.RemoveAt(AttachedContainerIndex);
                        AttachedContainerIndex = -1;
                        LinkedFreightAnimation.Loaded = true;
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
                            TargetX = PickingSurfaceRelativeTopStartPosition.X;
                            TargetZ = PickingSurfaceRelativeTopStartPosition.Z - RelativeContainerPosition.Translation.Z + LinkedFreightAnimation.Container.IntrinsicShapeOffset.Z * 
                                (ContainerFlipped ? -1 : 1);                              
                            if (TargetZ < PickingSurfaceRelativeTopStartPosition.Z)
                            {
                                if (!messageWritten)
                                {
                                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Wagon out of range: move wagon towards crane by {0} metres",
                                        PickingSurfaceRelativeTopStartPosition.Z - TargetZ));
                                    messageWritten = true;
                                }
                            }
                            else
                            {
                                UnloadContainer = LinkedFreightAnimation.Container;
                                Status = ContainerStationStatus.UnloadHorizontallyMoveToPick;
                                RelativeContainerPosition.M43 = UnloadContainer.IntrinsicShapeOffset.Z * (ContainerFlipped ? -1 : 1);
                                MoveX = true;
                                MoveZ = true;
                                UnloadContainer.ContainerStation = this;
                                Containers.Add(UnloadContainer);
                            }
                        }
                    }
                    break;
                case ContainerStationStatus.UnloadHorizontallyMoveToPick:
                    if (subMissionTerminated)
                    {
                        MoveX = false;
                        MoveZ = false;
                        Status = ContainerStationStatus.UnloadLowerToPick;
                        TargetY = - PickingSurfaceYOffset + UnloadContainer.HeightM + UnloadContainer.IntrinsicShapeOffset.Y + GeneralRelativeContainerPosition.M42 - PickingSurfaceYOffset;
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
            GeneralRelativeContainerPosition = Matrix.Multiply(LinkedFreightAnimation.Wagon.WorldPosition.XNAMatrix, InitialInvAnimationXNAMatrix);
            GeneralRelativeContainerPosition.M42 += PickingSurfaceYOffset;
            RelativeContainerPosition = GeneralRelativeContainerPosition;
            RelativeContainerPosition.M41 += LinkedFreightAnimation.Container.XOffset;
            RelativeContainerPosition.M42 += LinkedFreightAnimation.Container.YOffset;
            RelativeContainerPosition.M43 += LinkedFreightAnimation.Container.ZOffset;
            ContainerFlipped = (Math.Abs(InitialInvAnimationXNAMatrix.M11 - LinkedFreightAnimation.Container.WorldPosition.XNAMatrix.M11) < 0.1f ? false : true);
            Status = ContainerStationStatus.UnloadRaiseToPick;
            TargetY = PickingSurfaceRelativeTopStartPosition.Y;
            MoveY = true;
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
            RelativeContainerPosition.M43 = Containers[Containers.Count - 1].IntrinsicShapeOffset.Z * (ContainerFlipped ? -1 : 1);
            Status = ContainerStationStatus.LoadRaiseToPick;
            TargetY = PickingSurfaceRelativeTopStartPosition.Y;
            MoveY = true;
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

    } // end Class ContainerHandlingItem
}

