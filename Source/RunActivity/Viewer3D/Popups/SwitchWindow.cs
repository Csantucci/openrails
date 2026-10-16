// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.Signalling;
using ORTS.Common;
using System;
using System.Diagnostics;
using System.Linq;

namespace Orts.Viewer3D.Popups
{
    public class SwitchWindow : Window
    {
        const int SwitchImageSize = 32;

        Image FirstSwitchForwards;
        Image FirstSwitchBackwards;
        Image OppositeSwitchForwards;
        Image OppositeSwitchBackwards;
        Image TrainDirection;
        Image ForwardEye;
        Image BackwardEye;
        SwitchOrientation OppositeSwitchForwardsOrientation = SwitchOrientation.Any;
        SwitchOrientation OppositeSwitchBackwardsOrientation = SwitchOrientation.Any;

        static Texture2D SwitchStates;

        public SwitchWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + (int)3.5 * SwitchImageSize, Window.DecorationSize.Y + 2 * SwitchImageSize, Viewer.Catalog.GetString("Switch"))
        {
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            if (SwitchStates == null)
                // TODO: This should happen on the loader thread.
                SwitchStates = SharedTextureManager.LoadInternal(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "SwitchStates.png"));
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var hbox = base.Layout(layout).AddLayoutHorizontal();
            {
                var vbox1 = hbox.AddLayoutVertical(SwitchImageSize);
                vbox1.Add(ForwardEye = new Image(0, 0, SwitchImageSize, SwitchImageSize / 2));
                vbox1.Add(TrainDirection = new Image(0, 0, SwitchImageSize, SwitchImageSize));
                vbox1.Add(BackwardEye = new Image(0, 0, SwitchImageSize, SwitchImageSize / 2));

                var vbox2 = hbox.AddLayoutVertical(SwitchImageSize);
                vbox2.Add(FirstSwitchForwards = new Image(0, 0, SwitchImageSize, SwitchImageSize));
                vbox2.Add(FirstSwitchBackwards = new Image(0, 0, SwitchImageSize, SwitchImageSize));
                FirstSwitchForwards.Texture = FirstSwitchBackwards.Texture = SwitchStates;
                FirstSwitchForwards.Click += new Action<Control, Point>(FirstSwitchForwards_Click);
                FirstSwitchBackwards.Click += new Action<Control, Point>(FirstSwitchBackwards_Click);
                TrainDirection.Texture = ForwardEye.Texture = BackwardEye.Texture = SwitchStates;

                var vbox3 = hbox.AddLayoutVertical(hbox.RemainingWidth);
                vbox3.Add(OppositeSwitchForwards = new Image(0, 0, SwitchImageSize, SwitchImageSize));
                vbox3.Add(OppositeSwitchBackwards = new Image(0, 0, SwitchImageSize, SwitchImageSize));
                OppositeSwitchForwards.Texture = OppositeSwitchBackwards.Texture = SwitchStates;
                OppositeSwitchForwards.Click += new Action<Control, Point>(OppositeSwitchForwards_Click);
                OppositeSwitchBackwards.Click += new Action<Control, Point>(OppositeSwitchBackwards_Click);
                TrainDirection.Texture = ForwardEye.Texture = BackwardEye.Texture = SwitchStates;
            }
            return hbox;
        }

        void FirstSwitchForwards_Click(Control arg1, Point arg2)
        {
            new ToggleSwitchAheadCommand(Owner.Viewer.Log);
        }

        void FirstSwitchBackwards_Click(Control arg1, Point arg2)
        {
            new ToggleSwitchBehindCommand(Owner.Viewer.Log);
        }

        void OppositeSwitchForwards_Click(Control arg1, Point arg2)
        {
            new ToggleSwitchAheadCommand(Owner.Viewer.Log, OppositeSwitchForwardsOrientation);
        }

        void OppositeSwitchBackwards_Click(Control arg1, Point arg2)
        {
            new ToggleSwitchBehindCommand(Owner.Viewer.Log, OppositeSwitchBackwardsOrientation);
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var train = Owner.Viewer.PlayerTrain;
                try
                {
                    var switchOrientation = SwitchOrientation.Any;
                    // forward switches with both orientations
                    var traveller = new Traveller(train.FrontTDBTraveller);
                    switchOrientation = UpdateSwitch(FirstSwitchForwards, train, true, ref traveller);
                    if (switchOrientation != SwitchOrientation.Any)
                        OppositeSwitchForwardsOrientation = UpdateSwitch(OppositeSwitchForwards, train, true, ref traveller,
                        switchOrientation: switchOrientation == SwitchOrientation.Facing ? SwitchOrientation.Trailing : SwitchOrientation.Facing);
                    // backward switches with both orientations
                    switchOrientation = SwitchOrientation.Any;
                    traveller = new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
                    switchOrientation = UpdateSwitch(FirstSwitchBackwards, train, false, ref traveller);
                    if (switchOrientation != SwitchOrientation.Any)
                        OppositeSwitchBackwardsOrientation = UpdateSwitch(OppositeSwitchBackwards, train, false, ref traveller,
                        switchOrientation: switchOrientation == SwitchOrientation.Facing ? SwitchOrientation.Trailing : SwitchOrientation.Facing);
                }
                catch (Exception) { }

                UpdateDirection(TrainDirection, train);
                UpdateEye(ForwardEye, train, true);
                UpdateEye(BackwardEye, train, false);
            }
        }

        SwitchOrientation UpdateSwitch(Image image, Train train, bool front, ref Traveller traveller, SwitchOrientation switchOrientation = SwitchOrientation.Any)
        {
            image.Source = new Rectangle(0, 0, SwitchImageSize, SwitchImageSize);

            TrackNode SwitchPreviousNode = traveller.TN;
            TrackNode SwitchNode = null;

            int switchPreviousNodeID;
            bool switchBranchesAwayFromUs = false;
            while (traveller.NextSection())
            {
                if (traveller.IsJunction)
                {
                    SwitchNode = traveller.TN;
                    switchPreviousNodeID = Owner.Viewer.Simulator.TDB.TrackDB.TrackNodesIndexOf(SwitchPreviousNode);
                    switchBranchesAwayFromUs = SwitchNode.TrPins[0].Link == switchPreviousNodeID;
                    if (switchOrientation == SwitchOrientation.Any)
                    {
                        switchOrientation = switchBranchesAwayFromUs ? SwitchOrientation.Facing : SwitchOrientation.Trailing;
                        break;
                    }
                    else if (switchOrientation == SwitchOrientation.Facing && switchBranchesAwayFromUs)
                        break;
                    else if (switchOrientation == SwitchOrientation.Trailing && !switchBranchesAwayFromUs)
                        break;
                }
                SwitchPreviousNode = traveller.TN;
            }
            if (SwitchNode == null)
                return switchOrientation;

            Debug.Assert(SwitchPreviousNode != null);
            Debug.Assert(SwitchNode.Inpins == 1);
            Debug.Assert(SwitchNode.Outpins == 2 || SwitchNode.Outpins == 3);  // allow for 3-way switch
            Debug.Assert(SwitchNode.TrPins.Count() == 3 || SwitchNode.TrPins.Count() == 4);  // allow for 3-way switch
            Debug.Assert(SwitchNode.TrJunctionNode != null);
            Debug.Assert(SwitchNode.TrJunctionNode.SelectedRoute == 0 || SwitchNode.TrJunctionNode.SelectedRoute == 1);

            var switchTrackSection = Owner.Viewer.Simulator.TSectionDat.TrackShapes.Get(SwitchNode.TrJunctionNode.ShapeIndex);  // TSECTION.DAT tells us which is the main route
            var switchMainRouteIsLeft = SwitchNode.TrJunctionNode.GetAngle(Owner.Viewer.Simulator.TSectionDat) > 0;  // align the switch

            image.Source.X = ((switchBranchesAwayFromUs == front ? 1 : 3) + (switchMainRouteIsLeft ? 1 : 0)) * SwitchImageSize;
            image.Source.Y = SwitchNode.TrJunctionNode.SelectedRoute * SwitchImageSize;

            TrackCircuitSection switchSection = Owner.Viewer.Simulator.Signals.TrackCircuitList[SwitchNode.TCCrossReference[0].Index];
            if (switchSection.CircuitState.HasTrainsOccupying() || switchSection.CircuitState.SignalReserved >= 0 ||
                (switchSection.CircuitState.TrainReserved != null && switchSection.CircuitState.TrainReserved.Train.ControlMode != Train.TRAIN_CONTROL.MANUAL))
                image.Source.Y += 2 * SwitchImageSize;
            return switchOrientation;
        }

        static void UpdateDirection(Image image, Train train)
        {
            image.Source = new Rectangle(0, 0, SwitchImageSize, SwitchImageSize);
            image.Source.Y = 4 * SwitchImageSize;
            image.Source.X = train.MUDirection == Direction.Forward ? 2 * SwitchImageSize :
                (train.MUDirection == Direction.Reverse ? 1 * SwitchImageSize : 0);
        }

        static void UpdateEye(Image image, Train train, bool front)
        {
            image.Source = new Rectangle(0, 0, SwitchImageSize, SwitchImageSize / 2);
            image.Source.Y = (int)(4.25 * SwitchImageSize);
            bool flipped = Program.Simulator.PlayerLocomotive.Flipped ^ Program.Simulator.PlayerLocomotive.GetCabFlipped();
            image.Source.X = (front ^ !flipped) ? 0 : 3 * SwitchImageSize;
        }

    }
}
