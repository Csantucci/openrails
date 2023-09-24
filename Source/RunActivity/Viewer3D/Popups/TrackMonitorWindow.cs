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

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Settings;
using System.Linq;

namespace Orts.Viewer3D.Popups
{
    public class TrackMonitorWindow : Window
    {
        public const int MaximumDistance = 5000;
        public const int TrackMonitorLabelHeight = 130; // Height of labels above the main display.
        public const int TrackMonitorOffsetY = 25/*Window.DecorationOffset.Y*/ + TrackMonitorLabelHeight;
        const int TrackMonitorHeightInLinesOfText = 16;

        Label SpeedCurrent;
        Label SpeedProjected;
        Label SpeedAllowed;
        Label ControlMode;
        Label Gradient;
        public TrackMonitor Monitor { get; private set; }

        readonly Dictionary<Train.TRAIN_CONTROL, string> ControlModeLabels;

        static readonly Dictionary<Train.END_AUTHORITY, string> AuthorityLabels = new Dictionary<Train.END_AUTHORITY, string>
        {
            { Train.END_AUTHORITY.END_OF_TRACK, "End Trck" },
            { Train.END_AUTHORITY.END_OF_PATH, "End Path" },
            { Train.END_AUTHORITY.RESERVED_SWITCH, "Switch" },
            { Train.END_AUTHORITY.LOOP, "Loop" },
            { Train.END_AUTHORITY.TRAIN_AHEAD, "TrainAhd" },
            { Train.END_AUTHORITY.MAX_DISTANCE, "Max Dist" },
            { Train.END_AUTHORITY.NO_PATH_RESERVED, "No Path" },
            { Train.END_AUTHORITY.SIGNAL, "Signal" },
            { Train.END_AUTHORITY.END_OF_AUTHORITY, "End Auth" },
        };

        static readonly Dictionary<Train.OUTOFCONTROL, string> OutOfControlLabels = new Dictionary<Train.OUTOFCONTROL, string>
        {
            { Train.OUTOFCONTROL.SPAD, "SPAD" },
            { Train.OUTOFCONTROL.SPAD_REAR, "SPAD-Rear" },
            { Train.OUTOFCONTROL.MISALIGNED_SWITCH, "Misalg Sw" },
            { Train.OUTOFCONTROL.OUT_OF_AUTHORITY, "Off Auth" },
            { Train.OUTOFCONTROL.OUT_OF_PATH, "Off Path" },
            { Train.OUTOFCONTROL.SLIPPED_INTO_PATH, "Splipped" },
            { Train.OUTOFCONTROL.SLIPPED_TO_ENDOFTRACK, "Slipped" },
            { Train.OUTOFCONTROL.OUT_OF_TRACK, "Off Track" },
            { Train.OUTOFCONTROL.SLIPPED_INTO_TURNTABLE, "Slip Turn" },
            { Train.OUTOFCONTROL.UNDEFINED, "Undefined" },
        };

        // Web Api
        public bool DataUpdating = false;
        public bool itemLocationWSChanged = false;
        public bool WebServerEnabled = false;

        int BottomLabelRow;
        int BottomMaxDistanceMarker = 0;
        bool itemLocationWSBusy = false;
        float MaxDistanceMarker = 0.0f;
        int PointBottomRow;
        int PointTopRow;
        int rowOffset;
        int TopLabelRow;

        public struct ListLabel
        {
            public string FirstCol { get; set; }
            public string TrackColLeft { get; set; }
            public string TrackCol { get; set; }
            public string TrackColRight { get; set; }
            public string LimitCol { get; set; }
            public string SignalCol { get; set; }
            public string DistCol { get; set; }
        }
        public List<ListLabel> TrackMonitorListLabel = new List<ListLabel>();

        public struct ListTrackControl
        {
            public string FirstCol { get; set; }
            public string TrackColLeft { get; set; }
            public string TrackCol { get; set; }
            public string TrackColRight { get; set; }
            public string LimitCol { get; set; }
            public string SignalCol { get; set; }
            public int Row { get; set; }
            public float DistanceToTrainM { get; set; }
            public string DistCol { get; set; }
        }
        public List<ListTrackControl> TrackControlList = new List<ListTrackControl>();

        public ListTrackControl DataCol = new ListTrackControl();

        Dictionary<TrackMonitorSignalAspect, string> SignalMarkersWebApi = new Dictionary<TrackMonitorSignalAspect, string>
        {
            { TrackMonitorSignalAspect.Clear_2, '\u25D5'.ToString() + "?!!" },
            { TrackMonitorSignalAspect.Clear_1, '\u25D5'.ToString() + "?!!" },
            { TrackMonitorSignalAspect.Approach_3, '\u25D5'.ToString() + "???" },
            { TrackMonitorSignalAspect.Approach_2, '\u25D5'.ToString() + "???" },
            { TrackMonitorSignalAspect.Approach_1, '\u25D5'.ToString() + "???" },
            { TrackMonitorSignalAspect.Restricted, '\u25D5'.ToString() + "!!!" },
            { TrackMonitorSignalAspect.StopAndProceed, '\u25D5'.ToString() + "!!!" },
            { TrackMonitorSignalAspect.Stop, '\u25D5'.ToString() + "!!!"},
            { TrackMonitorSignalAspect.Permission, '\u25D5'.ToString() + "!!!" },
            { TrackMonitorSignalAspect.None, '\u25D5'.ToString() + "?!?" }
        };

        // Equivalent symbol
        string eyeWS = '\u26EF'.ToString() + "%%$";         // ⛯  Color.LightGreen
        string trainPositionAutoForwardsWS = "⧯" + "!??";   // ⧯ Color.White
        string trainPositionAutoBackwardsWS = "⧯" + "!??";  // ⧯ Color.White
        string trainPositionManualOnRouteWS = "⧯" + "!??";  // ⧯ Color.White
        string trainPositionManualOffRouteWS = "⧯" + "!!!"; // ⧯ Color.Red
        string endAuthorityWS = '\u25AC'.ToString() + "!!!"; // ▬ Color.Red
        string oppositeTrainForwardWS = '\u2588'.ToString() + "!!?"; // █ Color.Orange
        string oppositeTrainBackwardWS = '\u2588'.ToString() + "!!?";// █ Color.Orange
        string stationLeftWS = '\u2590'.ToString() + "$%$"; //▐ RIGHT HALF BLOCK Color.Blue
        string stationRightWS = '\u258C'.ToString() + "$%$";// ▌ LEFT HALF BLOCK Color.Blue
        string trackWS = '\u2502'.ToString() + '\u2502'.ToString();
        string reversalWS = '\u21B6'.ToString();// ↶ ANTICLOCKWISE TOP SEMICIRCLE ARROW
        string waitingPointWS = '\u270B'.ToString();// ✋ RAISED HAND
        string forwardArrowWS = '\u25B2'.ToString() + "?!!";  //▲
        string backwardArrowWS = '\u25BC'.ToString() + "?!!"; //▼
        string invalidReversalWS = '\u25AC'.ToString() + "!!?";// ▬ + Color.Orange
        string leftArrowWS = '\u25C4'.ToString() + "!!?";      // ◄ + Color.Orange
        string rightArrowWS = '\u25BA'.ToString() + "!!?";     // ► + Color.Orange

        // Start Duplicated variables from TrackMonitor:Control
        public int PositionHeight = 240;
        public int PositionWidth = 240;
        public float markerIntervalD = 0;

        bool metric;
        double Scale = 1;// scaling reference
        const int DesignWidth = 150; // All Width/X values are relative to this width.

        public static int DbfEvalOverSpeed;//Debrief eval
        bool istrackColorRed = false;//Debrief eval
        public static double DbfEvalOverSpeedTimeS = 0;//Debrief eval
        public static double DbfEvalIniOverSpeedTimeS = 0;//Debrief eval

        // position constants
        readonly int additionalInfoHeight = 16; // vertical offset on window for additional out-of-range info at top and bottom
        readonly int[] mainOffset = new int[2] { 12, 12 }; // offset for items, cell 0 is upward, 1 is downward
        readonly int textSpacing = 10; // minimum vertical distance between two labels

        // Horizontal offsets for various elements.
        readonly int distanceTextOffset = 117;
        //readonly int trackOffset = 42;
        readonly int speedTextOffset = 70;
        //readonly int milepostTextOffset = 0;

        // Vertical offset for text for forwards ([0]) and backwards ([1]).
        readonly int[] textOffset = new int[2] { -11, -3 };

        int[] eyePosition = new int[5] { 42, -4, -20, 24, 24 };
        int[] trainPosition = new int[5] { 42, -12, -12, 24, 24 }; // Relative positioning
        int[] otherTrainPosition = new int[5] { 42, -24, 0, 24, 24 }; // Relative positioning
        int[] stationPosition = new int[5] { 42, 0, -24, 24, 12 }; // Relative positioning
        int[] reversalPosition = new int[5] { 42, -21, -3, 24, 24 }; // Relative positioning
        int[] waitingPointPosition = new int[5] { 42, -21, -3, 24, 24 }; // Relative positioning
        int[] endAuthorityPosition = new int[5] { 42, -14, -10, 24, 24 }; // Relative positioning
        int[] signalPosition = new int[5] { 95, -16, 0, 16, 16 }; // Relative positioning
        int[] arrowPosition = new int[5] { 22, -12, -12, 24, 24 };
        int[] invalidReversalPosition = new int[5] { 42, -14, -10, 24, 24 }; // Relative positioning
        int[] leftSwitchPosition = new int[5] { 37, -14, -10, 24, 24 }; // Relative positioning
        int[] rightSwitchPosition = new int[5] { 47, -14, -10, 24, 24 }; // Relative positioning

        // texture rectangles : X-offset, Y-offset, width, height
        Rectangle eyeSprite = new Rectangle(0, 144, 24, 24);
        Rectangle trainPositionAutoForwardsSprite = new Rectangle(0, 72, 24, 24);
        Rectangle trainPositionAutoBackwardsSprite = new Rectangle(24, 72, 24, 24);
        Rectangle trainPositionManualOnRouteSprite = new Rectangle(24, 96, 24, 24);
        Rectangle trainPositionManualOffRouteSprite = new Rectangle(0, 96, 24, 24);
        Rectangle endAuthoritySprite = new Rectangle(0, 0, 24, 24);
        Rectangle oppositeTrainForwardSprite = new Rectangle(24, 120, 24, 24);
        Rectangle oppositeTrainBackwardSprite = new Rectangle(0, 120, 24, 24);
        Rectangle stationSprite = new Rectangle(24, 0, 24, 24);
        Rectangle reversalSprite = new Rectangle(0, 24, 24, 24);
        Rectangle waitingPointSprite = new Rectangle(24, 24, 24, 24);
        Rectangle forwardArrowSprite = new Rectangle(24, 48, 24, 24);
        Rectangle backwardArrowSprite = new Rectangle(0, 48, 24, 24);
        Rectangle invalidReversalSprite = new Rectangle(24, 144, 24, 24);
        Rectangle leftArrowSprite = new Rectangle(0, 168, 24, 24);
        Rectangle rightArrowSprite = new Rectangle(24, 168, 24, 24);

        Dictionary<TrackMonitorSignalAspect, Rectangle> SignalMarkers = new Dictionary<TrackMonitorSignalAspect, Rectangle>
        {
            { TrackMonitorSignalAspect.Clear_2, new Rectangle(0, 0, 16, 16) },
            { TrackMonitorSignalAspect.Clear_1, new Rectangle(16, 0, 16, 16) },
            { TrackMonitorSignalAspect.Approach_3, new Rectangle(0, 16, 16, 16) },
            { TrackMonitorSignalAspect.Approach_2, new Rectangle(16, 16, 16, 16) },
            { TrackMonitorSignalAspect.Approach_1, new Rectangle(0, 32, 16, 16) },
            { TrackMonitorSignalAspect.Restricted, new Rectangle(16, 32, 16, 16) },
            { TrackMonitorSignalAspect.StopAndProceed, new Rectangle(0, 48, 16, 16) },
            { TrackMonitorSignalAspect.Stop, new Rectangle(16, 48, 16, 16) },
            { TrackMonitorSignalAspect.Permission, new Rectangle(0, 64, 16, 16) },
            { TrackMonitorSignalAspect.None, new Rectangle(16, 64, 16, 16) }
        };

        // fixed distance rounding values as function of maximum distance
        Dictionary<float, float> roundingValues = new Dictionary<float, float>
        {
            { 0.0f, 0.5f },
            { 5.0f, 1.0f },
            { 10.0f, 2.0f }
        };

        public float markerIntervalM = 0.0f;
        // End  Duplicated from TrackMonitor:Control

        public TrackMonitorWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 10, Window.DecorationSize.Y + owner.TextFontDefault.Height * (5 + TrackMonitorHeightInLinesOfText) + ControlLayout.SeparatorSize * 3, Viewer.Catalog.GetString("Track Monitor"))
        {
            ControlModeLabels = new Dictionary<Train.TRAIN_CONTROL, string>
            {
                { Train.TRAIN_CONTROL.AUTO_SIGNAL , Viewer.Catalog.GetString("Auto Signal") },
                { Train.TRAIN_CONTROL.AUTO_NODE, Viewer.Catalog.GetString("Node") },
                { Train.TRAIN_CONTROL.MANUAL, Viewer.Catalog.GetString("Manual") },
                { Train.TRAIN_CONTROL.EXPLORER, Viewer.Catalog.GetString("Explorer") },
                { Train.TRAIN_CONTROL.OUT_OF_CONTROL, Viewer.Catalog.GetString("OutOfControl : ") },
                { Train.TRAIN_CONTROL.INACTIVE, Viewer.Catalog.GetString("Inactive") },
                { Train.TRAIN_CONTROL.TURNTABLE, Viewer.Catalog.GetString("Turntable") },
                { Train.TRAIN_CONTROL.UNDEFINED, Viewer.Catalog.GetString("Unknown") },
            };

            // Required
            metric = Owner.Viewer.MilepostUnitsMetric;

            // WebServer status
            WebServerEnabled = owner.Viewer.Settings.WebServer;
        }

        public override void TabAction() => Monitor.CycleMode();

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, Viewer.Catalog.GetString("Speed:")));
                hbox.Add(SpeedCurrent = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, Viewer.Catalog.GetString("Projected:")));
                hbox.Add(SpeedProjected = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, Viewer.Catalog.GetString("Limit:")));
                hbox.Add(SpeedAllowed = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(ControlMode = new Label(hbox.RemainingWidth - 18, hbox.RemainingHeight, "", LabelAlignment.Left));
                hbox.Add(Gradient = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));

            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(new Label(hbox.RemainingWidth, hbox.RemainingHeight, Viewer.Catalog.GetString(" Milepost   Limit     Dist")));
            }
            vbox.AddHorizontalSeparator();
            vbox.Add(Monitor = new TrackMonitor(vbox.RemainingWidth, vbox.RemainingHeight, Owner));

            return vbox;
        }

        private void UpdateData() //WebApi
        {
            DataUpdating = true;

            // Reset webApi data
            if (WebServerEnabled)
            {
                TrackMonitorListLabel.Clear();
                Scale = (Double)Monitor.Position.Width / DesignWidth;
            }
            TrackControlList.Clear();

            // Always get train details to pass on to TrackMonitor.
            var thisInfo = Owner.Viewer.PlayerTrain.GetTrainInfo();
            Monitor.StoreInfo(thisInfo);

            // Speed
            SpeedCurrent.Text = FormatStrings.FormatSpeedDisplay(Math.Abs(thisInfo.speedMpS), Owner.Viewer.MilepostUnitsMetric);
            InfoToLabel(Viewer.Catalog.GetString("Speed"), "", SpeedCurrent.Text +
                (thisInfo.speedMpS < thisInfo.allowedSpeedMpS - 1.0f ? "!??" :// White
                thisInfo.speedMpS < thisInfo.allowedSpeedMpS + 0.0f ? "?!!" :// PaleGreen
                thisInfo.speedMpS < thisInfo.allowedSpeedMpS + 5.0f ? "!!?" : "!!!"), "", "", "", "");// Orange : Red, "")

            //Projected
            SpeedProjected.Text = FormatStrings.FormatSpeedDisplay(Math.Abs(thisInfo.projectedSpeedMpS), Owner.Viewer.MilepostUnitsMetric);
            InfoToLabel(Viewer.Catalog.GetString("Projected"), "", SpeedProjected.Text, "", "", "", "");

            //SpeedAllowed
            SpeedAllowed.Text = FormatStrings.FormatSpeedLimit(thisInfo.allowedSpeedMpS, Owner.Viewer.MilepostUnitsMetric);
            InfoToLabel(Viewer.Catalog.GetString("Limit"), "", SpeedAllowed.Text, "", "", "", "");
            InfoToLabel(Viewer.Catalog.GetString("Sprtr"), "", "", "", "", "", "");

            //Control Mode
            var ControlText = ControlModeLabels[thisInfo.ControlMode];
            if (thisInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_NODE)
            {
                ControlText = FindAuthorityInfo(thisInfo.ObjectInfoForward, ControlText);
            }
            else if (thisInfo.ControlMode == Train.TRAIN_CONTROL.OUT_OF_CONTROL)
            {
                ControlText = String.Concat(ControlText, OutOfControlLabels[thisInfo.ObjectInfoForward[0].OutOfControlReason]);
            }
            ControlMode.Text = String.Copy(ControlText);

            //Gradient
            if (-thisInfo.currentElevationPercent < -0.00015)
            {
                var c = '\u2198';
                Gradient.Text = String.Format("|  {0:F1}%{1} ", -thisInfo.currentElevationPercent, c);
                Gradient.Color = Color.LightSkyBlue;
                InfoToLabel(Viewer.Catalog.GetString("Gradient"), "", Gradient.Text.Replace("|", "") + "$$$", "", "", "", "");
            }
            else if (-thisInfo.currentElevationPercent > 0.00015)
            {
                var c = '\u2197';
                Gradient.Text = String.Format("|  {0:F1}%{1} ", -thisInfo.currentElevationPercent, c);
                Gradient.Color = Color.Yellow;
                InfoToLabel(Viewer.Catalog.GetString("Gradient"), "", Gradient.Text.Replace("|", "") + " ???", "", "", "", "");
            }
            else
            {
                Gradient.Text = "-";
                InfoToLabel(Viewer.Catalog.GetString("Gradient"), "", "", "", Gradient.Text, "", "");
            }
            InfoToLabel(Viewer.Catalog.GetString("Sprtr"), "", "", "", "", "", "");

            // Direction
            var PlayerTrain = Owner.Viewer.PlayerLocomotive.Train;
            var ShowMUReverser = Math.Abs(PlayerTrain.MUReverserPercent) != 100;
            InfoToLabel(Owner.Viewer.PlayerLocomotive.EngineType == TrainCar.EngineTypes.Steam ? Viewer.Catalog.GetString("Reverser") : Viewer.Catalog.GetString("Direction"), "",
                (ShowMUReverser ? Math.Abs(PlayerTrain.MUReverserPercent).ToString("0") + "% " : "") + FormatStrings.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(Owner.Viewer.PlayerLocomotive.Direction)), "", "", "", "");

            // Present cab orientation (0=forward, 1=backward)
            InfoToLabel(Viewer.Catalog.GetString("Cab ORIEN"), "", thisInfo.cabOrientation == 0 ? Viewer.Catalog.GetString("Forward") : Viewer.Catalog.GetString("Backward"), "", "", "", "");

            //Control Mode for webApi
            InfoToLabel(Viewer.Catalog.GetString("Sprtr"), "", "", "", "", "", "");
            InfoToLabel(ControlText, "", "", "", "", "", "");
            InfoToLabel(Viewer.Catalog.GetString("Sprtr"), "", "", "", "", "", "");

            // TrackMonitor:Control text emulation
            if (WebServerEnabled)
            {
                bool metric = Owner.Viewer.MilepostUnitsMetric;
                // track
                var verticalDraw = '\u2502'; // ▏
                var Track = verticalDraw.ToString() + verticalDraw.ToString();
                Point offset = new Point(0, 0);
                if (thisInfo == null)
                {
                    TMCdrawTrack("", offset, 0f, 1f);
                    return;
                }
                TMCdrawTrack("", offset, thisInfo.speedMpS, thisInfo.allowedSpeedMpS);

                // Simulator mode
                if (Orts.MultiPlayer.MPManager.IsMultiPlayer())
                {
                    TMCdrawMPInfo("", offset);
                }
                else if (thisInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_NODE || thisInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_SIGNAL)
                {
                    TMCdrawAutoInfo("", offset);
                }
                else if (thisInfo.ControlMode == Train.TRAIN_CONTROL.TURNTABLE)
                {
                    DataUpdating = false;
                    return;
                }
                else
                {
                    TMCdrawManualInfo("", offset);
                }

                // OwnTrain row position
                rowOffset = (Orts.MultiPlayer.MPManager.IsMultiPlayer() ? 1 : 2);

                //Milepost Limit Dist
                InfoToLabel(Viewer.Catalog.GetString("Milepost"), "", "", "", Viewer.Catalog.GetString("Limit"), "", Viewer.Catalog.GetString("Dist"));
                InfoToLabel(Viewer.Catalog.GetString("Sprtr"), "", "", "", "", "", "");

                //Track color
                var absoluteSpeedMpS = Math.Abs(thisInfo.speedMpS);
                Track = Track + (absoluteSpeedMpS < thisInfo.allowedSpeedMpS - 1.0f ? "??!" : // Green
                absoluteSpeedMpS < thisInfo.allowedSpeedMpS + 0.0f ? "?!!" : // PaleGreen
                absoluteSpeedMpS < thisInfo.allowedSpeedMpS + 5.0f ? "!!?" : "!!!"); // Orange : Red

                //Update data to webApi
                if (TrackControlList.Count > 0)
                {
                    var item = 0;
                    foreach (var data in TrackControlList)
                    {
                        InfoToLabel(data.FirstCol, data.TrackColLeft, data.TrackCol, data.TrackColRight, data.LimitCol, data.SignalCol, data.DistCol);

                        item += 1;
                    }
                }
            }
            DataUpdating = false;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            var TDWUpdating = Owner.Viewer.TrainDrivingWindow.TrainDrivingUpdating;

            // Update text fields on full update only.
            if (updateFull && !DataUpdating && !TDWUpdating)// Avoid updating data when others are doing it
            {
                UpdateData();
            }
        }

        // ==========================================================================================================================================
        //      Method to construct the train driving info data for use by the WebServer
        //      Replaces the Prepare Frame Method
        //      updated from  djr - 20171221
        // ==========================================================================================================================================
        public List<ListLabel> TrackMonitorWebApiData()
        {
            var TDWUpdating = Owner.Viewer.TrainDrivingWindow.TrainDrivingUpdating;

            if (!DataUpdating && !TDWUpdating)// Avoid updating data when others are doing it
            {
                UpdateData();
            }
            return TrackMonitorListLabel.ToList(); // try to avoid crash in the JsonConvert.SerializeObject
        }

        static string FindAuthorityInfo(List<Train.TrainObjectItem> ObjectInfo, string ControlText)
        {
            foreach (var thisInfo in ObjectInfo)
            {
                if (thisInfo.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY)
                {
                    // TODO: Concatenating strings is bad for localization.
                    return ControlText + " : " + AuthorityLabels[thisInfo.AuthorityType];
                }
            }
            return ControlText;
        }

        // Text version - WebApi
        /// <summary>
        /// Translate itemLocation graphic value to equivalent row text position
        /// </summary>
        /// <param name="zeroPoint"></param>
        /// <param name="itemLocation"></param>
        /// <returns></returns>
        private int itemLocationToRow(int zeroPoint, int itemLocation)
        {
            var row = 0;
            if (zeroPoint == 200) // Auto mode track zone only
            {
                row = (int)Math.Round(MathHelper.Clamp(itemLocation * (11.0f / 200.0f), 0, 11));
                BottomLabelRow = 11;
            }
            else if (zeroPoint == 224) // Auto mode track + train zone
            {
                row = (int)Math.Round(MathHelper.Clamp(itemLocation * (12.0f / 200.0f), 0, 12));
                BottomLabelRow = 12;
            }
            else if (zeroPoint == 240 || zeroPoint == 0) // forwardsY  backwardsY
            {
                row = (int)Math.Round(MathHelper.Clamp(itemLocation * (16.0f / 240.0f), 0, 16));
                BottomLabelRow = 16;
            }
            else if (zeroPoint == 108) // Manual mode upper zone
            {
                row = (int)(MathHelper.Clamp(itemLocation * (6.0f / 93.0f), 0, 6));
                BottomLabelRow = 6;
            }
            else if (zeroPoint == 132) // Manual mode lower zone
            {
                row = (int)Math.Round(MathHelper.Clamp(itemLocation * (16.0f / 232.0f), 10, 16));
                BottomLabelRow = 16;
            }


            return row;
        }

        /// <summary>
        /// Avoids overlapping
        /// </summary>
        /// <param name="itemLocationWS"></param>
        /// <param name="DataCol"></param>
        /// <param name="thisItem"></param>
        /// <param name="SymbolItemOne"></param>
        /// <param name="SymbolItemTwo"></param>
        private void Overlapping(int itemLocationWS, ListTrackControl DataColOver, Train.TrainObjectItem thisItem, string SymbolItemOne, string SymbolItemTwo, bool firstLabelShown)
        {
            if (itemLocationWS == 0)
            {
                return;
            }

            if (itemLocationWS <= BottomLabelRow
                && DataColOver.TrackCol != SymbolItemOne
                && ((DataColOver.FirstCol.Length > 1 && itemLocationWS != PointTopRow)
                || DataColOver.TrackColLeft.Length > 1
                || DataColOver.TrackColRight.Length > 1
                || DataColOver.SignalCol.Length > 1
                || DataColOver.LimitCol.Length > 1
                || (!DataColOver.TrackCol.Contains('\u2502'.ToString())
                )))
            {
                ListTrackControl DataColTemp = TrackControlList[itemLocationWS];
                var n = itemLocationWS;

                if (BottomLabelRow > 11)
                {
                    for (n = itemLocationWS; n < BottomLabelRow; n++)
                    {
                        DataColTemp = TrackControlList[n > 0 ? n : 0];
                        // Station
                        if (DataColTemp.TrackColRight.Contains('\u258C'.ToString()) && DataColTemp.DistanceToTrainM > thisItem.DistanceToTrainM)
                        {
                            TrackControlList[n + 1] = DataColTemp;
                            break;
                        }
                        else if (EmptyRowItems(n))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    for (n = itemLocationWS; n > 0; n--)
                    {
                        DataColTemp = TrackControlList[n > 0 ? n : 0];
                        // Station
                        if (DataColTemp.TrackColRight.Contains('\u258C'.ToString()) && DataColTemp.DistanceToTrainM > thisItem.DistanceToTrainM)
                        {
                            TrackControlList[n - 1] = DataColTemp;
                            break;
                        }
                        else if (EmptyRowItems(n))
                        {
                            break;
                        }
                    }
                }

                if (n == 0)
                {
                    return;
                }

                if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY)
                {
                    DataColTemp.TrackCol = SymbolItemOne;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.FACING_SWITCH)
                {   // See TOP
                    itemLocationWSBusy = n == 0 ? true : false;
                    if (!itemLocationWSBusy)
                    {
                        DataColTemp.TrackColRight = thisItem.IsRightSwitch ? SymbolItemOne : "";
                        DataColTemp.TrackColLeft = !thisItem.IsRightSwitch ? SymbolItemOne : "";
                    }
                    else if (itemLocationWSBusy && n == 0)
                    {
                        DataColTemp.TrackColRight = thisItem.IsRightSwitch ? SymbolItemOne : "";
                        DataColTemp.TrackColLeft = !thisItem.IsRightSwitch ? SymbolItemOne : "";
                    }
                    DataColTemp.LimitCol = DataColOver.Row == n && DataColOver.DistanceToTrainM < thisItem.DistanceToTrainM ? DataColOver.LimitCol : "";
                    DataColTemp.SignalCol = DataColOver.Row == n && DataColOver.DistanceToTrainM < thisItem.DistanceToTrainM ? DataColOver.SignalCol : "";
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.MILEPOST)
                {
                    DataColTemp.FirstCol = SymbolItemOne;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.REVERSAL)
                {
                    DataColTemp.TrackCol = SymbolItemOne;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL)
                {
                    DataColTemp.TrackColLeft = DataColOver.DistanceToTrainM < thisItem.DistanceToTrainM ? "" : DataColTemp.TrackColLeft;
                    DataColTemp.TrackColRight = DataColOver.DistanceToTrainM < thisItem.DistanceToTrainM ? "" : DataColTemp.TrackColRight;
                    DataColTemp.SignalCol = SymbolItemOne;
                    DataColTemp.LimitCol = SymbolItemTwo.Length > 1 ? SymbolItemTwo : "";
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST)
                {
                    DataColTemp.LimitCol = SymbolItemOne;
                    DataColTemp.TrackColLeft = DataColOver.DistanceToTrainM < thisItem.DistanceToTrainM ? "" : DataColTemp.TrackColLeft;
                    DataColTemp.TrackColRight = DataColOver.DistanceToTrainM < thisItem.DistanceToTrainM ? "" : DataColTemp.TrackColRight;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.STATION)
                {
                    DataColTemp.TrackColLeft = SymbolItemOne;
                    DataColTemp.TrackColRight = SymbolItemTwo;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.WAITING_POINT)
                {
                    DataColTemp.TrackCol = SymbolItemOne;
                }

                DataColTemp.DistanceToTrainM = thisItem.DistanceToTrainM;

                if (n < BottomMaxDistanceMarker && thisItem.DistanceToTrainM < MaxDistanceMarker)
                {
                    ListTrackControl DataTemp = TrackControlList[BottomMaxDistanceMarker];
                    DataTemp.DistCol = "";
                    TrackControlList[BottomMaxDistanceMarker] = DataTemp;
                }
                else if (!firstLabelShown && (n == PointTopRow || n == PointBottomRow))
                {
                    DataColTemp.DistCol = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                }
                else if (DataCol.TrackColRight.Contains('\u258C'.ToString()) && TopLabelRow < n)
                {
                    DataColTemp.DistCol = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                }
                else
                {  // Erases distanceMarker when overlapping
                    DataColTemp.DistCol = TopLabelRow > n ? ""
                        : !firstLabelShown && TopLabelRow < n && DataColTemp.TrackColLeft != stationLeftWS ? FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric)
                        : DataColTemp.DistCol;
                }

                DataColTemp.Row = n;
                TrackControlList[n] = DataColTemp;

                itemLocationWSChanged = true;// Row value less than the initial ItemLocationWS value.
            }
            else
            {
                if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY
                    || thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.REVERSAL)
                {
                    DataColOver.TrackCol = SymbolItemOne;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.FACING_SWITCH)
                {
                    if (thisItem.IsRightSwitch)
                    {
                        DataColOver.TrackColRight = SymbolItemOne;
                    }
                    else
                    {
                        DataColOver.TrackColLeft = SymbolItemOne;
                    }
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.MILEPOST)
                {
                    DataColOver.FirstCol = SymbolItemOne;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL)
                {
                    DataColOver.TrackColLeft = DataColOver.DistanceToTrainM < thisItem.DistanceToTrainM ? "" : DataColOver.TrackColLeft;
                    DataColOver.TrackColRight = DataColOver.DistanceToTrainM < thisItem.DistanceToTrainM ? "" : DataColOver.TrackColRight;

                    //OK
                    DataColOver.SignalCol = SymbolItemOne;
                    DataColOver.LimitCol = SymbolItemTwo;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST)
                {
                    DataColOver.LimitCol = SymbolItemOne;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.STATION)
                {
                    DataColOver.TrackColLeft = SymbolItemOne;
                    DataColOver.TrackColRight = SymbolItemTwo;
                }
                else if (thisItem.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.WAITING_POINT)
                {
                    DataColOver.TrackCol = SymbolItemOne;
                }
                DataColOver.DistanceToTrainM = thisItem.DistanceToTrainM;
                DataColOver.DistCol = !firstLabelShown && thisItem.ItemType != Train.TrainObjectItem.TRAINOBJECTTYPE.MILEPOST && (itemLocationWS == PointTopRow || itemLocationWS == PointBottomRow) ? FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric) : DataColOver.DistCol;
                DataColOver.Row = itemLocationWS;

                TrackControlList[itemLocationWS] = DataColOver;
                DataCol = DataColOver;

                itemLocationWSChanged = false;
            }
        }

        private bool EmptyRowItems(int currentrow)
        {
            ListTrackControl DataColTemp = TrackControlList[currentrow];
            // Check if some items are empty
            var RowEmpty = DataColTemp.FirstCol.Length < 2 && DataColTemp.TrackColLeft.ToString().Length < 2 && DataColTemp.TrackCol.Contains('\u2502'.ToString()) && DataColTemp.TrackColRight.ToString().Length < 2 && DataColTemp.LimitCol.Length < 2 && DataColTemp.SignalCol.Length < 2;
            return RowEmpty;
        }

        public void InfoToLabel(string firstcol, string trackcolleft, string trackcol, string trackcolright, string limitcol, string signalcol, string distcol)
        {
            firstcol = firstcol == null || firstcol == "" ? " " : firstcol;
            trackcolleft = trackcolleft == null || trackcolleft == "" ? " " : trackcolleft;
            trackcol = trackcol == null || trackcol == "" ? " " : trackcol;
            trackcolright = trackcolright == null || trackcolright == "" ? " " : trackcolright;
            limitcol = limitcol == null || limitcol == "" ? " " : limitcol;
            signalcol = signalcol == null || signalcol == "" ? " " : signalcol;
            distcol = distcol == null || distcol == "" ? " " : distcol;

            TrackMonitorListLabel.Add(new ListLabel
            {
                FirstCol = firstcol,
                TrackColLeft = trackcolleft,
                TrackCol = trackcol,
                TrackColRight = trackcolright,
                LimitCol = limitcol,
                SignalCol = signalcol,
                DistCol = distcol
            });
        }

        void LabelText(string a, Point labelPoint, string distanceString, Color color) { }

        void TMCdrawTrack(string a, Point offset, float speedMpS, float allowedSpeedMpS)
        {
            if (DataUpdating)
            {
                var train = Program.Viewer.PlayerLocomotive.Train;
                var absoluteSpeedMpS = Math.Abs(speedMpS);

                //var trackColor =
                //    absoluteSpeedMpS < allowedSpeedMpS - 1.0f ? Color.Green :
                //    absoluteSpeedMpS < allowedSpeedMpS + 0.0f ? Color.PaleGreen :
                //    absoluteSpeedMpS < allowedSpeedMpS + 5.0f ? Color.Orange : Color.Red;

                //spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X + trackOffset + trackRail1Offset, offset.Y, trackRailWidth, Position.Height), trackColor);
                //spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X + trackOffset + trackRail2Offset, offset.Y, trackRailWidth, Position.Height), trackColor);
                var trackColor =
                        absoluteSpeedMpS < allowedSpeedMpS - 1.0f ? "??!" :// Color.Green :
                        absoluteSpeedMpS < allowedSpeedMpS + 0.0f ? "?!!" :// Color.PaleGreen :
                        absoluteSpeedMpS < allowedSpeedMpS + 5.0f ? "!!?" : "!!!";// Color.Orange : Color.Red;

                if (trackColor == "!!!" && !istrackColorRed)//Debrief Eval
                {
                    istrackColorRed = true;
                    DbfEvalIniOverSpeedTimeS = Orts.MultiPlayer.MPManager.Simulator.ClockTime;
                }

                if (istrackColorRed && trackColor != "!!!")//Debrief Eval
                {
                    istrackColorRed = false;
                    DbfEvalOverSpeed++;
                }

                if (istrackColorRed && (Orts.MultiPlayer.MPManager.Simulator.ClockTime - DbfEvalIniOverSpeedTimeS) > 1.0000)//Debrief Eval
                {
                    DbfEvalOverSpeedTimeS = DbfEvalOverSpeedTimeS + (Orts.MultiPlayer.MPManager.Simulator.ClockTime - DbfEvalIniOverSpeedTimeS);
                    train.DbfEvalValueChanged = true;
                    DbfEvalIniOverSpeedTimeS = Orts.MultiPlayer.MPManager.Simulator.ClockTime;
                }

                // Reset TrackControlList
                for (int n = 0; n < 17; n++)
                {
                    if (TrackControlList.Count == 17)
                    {
                        ListTrackControl DataCol = TrackControlList[n];
                        DataCol.FirstCol = "";
                        DataCol.TrackColLeft = " ";
                        DataCol.TrackCol = trackWS + trackColor;
                        TrackControlList[n] = DataCol;
                    }
                    else
                    {
                        TrackControlList.Add(new ListTrackControl()
                        {
                            FirstCol = "",
                            TrackColLeft = " ",
                            TrackCol = trackWS + trackColor,
                            TrackColRight = "",
                            LimitCol = "",
                            SignalCol = "",
                            Row = 0,
                            DistanceToTrainM = 0,
                            DistCol = ""
                        });
                    }
                }
            }
        }

        void TMCdrawAutoInfo(string a, Point offset)
        {
            var validInfo = Owner.Viewer.PlayerTrain.GetTrainInfo();

            // set area details
            var startObjectArea = additionalInfoHeight;
            var endObjectArea = Monitor.Position.Height - (Monitor.Position.Height - (int)Math.Ceiling((Monitor.Position.Height / Scale))) - additionalInfoHeight - trainPosition[4];
            var zeroObjectPointTop = endObjectArea;
            var zeroObjectPointMiddle = zeroObjectPointTop - trainPosition[1];
            var zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            var distanceFactor = (float)(endObjectArea - startObjectArea) / TrackMonitorWindow.MaximumDistance;

            // Translate itemLocation value to row value
            var itemLocationWS = itemLocationToRow(zeroObjectPointBottom, zeroObjectPointBottom);
            PointBottomRow = 0; // should be 11;
            PointTopRow = itemLocationToRow(zeroObjectPointTop, zeroObjectPointTop);  // should be 11;

            // Points to current row
            DataCol = TrackControlList[itemLocationWS];

            // draw train position line
            // use red if no info for reverse move available
            var lineColor = Color.DarkGray;
            DataCol.FirstCol = Viewer.Catalog.GetString("SprtrDarkGray");

            if (validInfo.ObjectInfoBackward != null && validInfo.ObjectInfoBackward.Count > 0 &&
                validInfo.ObjectInfoBackward[0].ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY &&
                validInfo.ObjectInfoBackward[0].AuthorityType == Train.END_AUTHORITY.NO_PATH_RESERVED)
            {
                //lineColor = Color.Red;
                DataCol.FirstCol = Viewer.Catalog.GetString("SprtrRed");
            }
            DataCol.TrackColLeft = "";
            DataCol.TrackCol = "";
            DataCol.TrackColRight = "";
            DataCol.LimitCol = "";
            DataCol.SignalCol = "";
            DataCol.DistCol = "";
            TrackControlList[itemLocationWS] = DataCol;

            //spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X, offset.Y + endObjectArea, Position.Width, 1), lineColor);

            // draw direction arrow
            if (validInfo.direction == 0)
            {
                TMCdrawArrow("", offset, forwardArrowSprite, zeroObjectPointMiddle + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                TMCdrawArrow("", offset, backwardArrowSprite, zeroObjectPointMiddle + arrowPosition[2]);
            }

            // draw eye
            TMCdrawEye("", offset, 0, Monitor.Position.Height);

            // draw fixed distance indications
            var firstMarkerDistance = TMCdrawDistanceMarkers("", offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointTop, 4, true);
            var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

            // draw forward items
            TMCdrawItems("", offset, startObjectArea, endObjectArea, zeroObjectPointTop, zeroObjectPointBottom, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoForward, true);

            // draw own train marker
            TMCdrawOwnTrain("", offset, trainPositionAutoForwardsSprite, zeroObjectPointTop);
        }

        // draw Multiplayer info
        // all details accessed through class variables
        void TMCdrawMPInfo(string a, Point offset)
        {
            var validInfo = Owner.Viewer.PlayerTrain.GetTrainInfo();

            // set area details
            var startObjectArea = additionalInfoHeight;
            var endObjectArea = Monitor.Position.Height - (Monitor.Position.Height - (int)Math.Ceiling((Monitor.Position.Height / Scale))) - additionalInfoHeight;
            var zeroObjectPointTop = 0;
            var zeroObjectPointMiddle = 0;
            var zeroObjectPointBottom = 0;
            if (validInfo.direction == 0)
            {
                zeroObjectPointTop = endObjectArea - trainPosition[4];
                zeroObjectPointMiddle = zeroObjectPointTop - trainPosition[1];
                zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            }
            else if (validInfo.direction == 1)
            {
                zeroObjectPointTop = startObjectArea;
                zeroObjectPointMiddle = zeroObjectPointTop - trainPosition[1];
                zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            }
            else
            {
                zeroObjectPointMiddle = startObjectArea + (endObjectArea - startObjectArea) / 2;
                zeroObjectPointTop = zeroObjectPointMiddle + trainPosition[1];
                zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            }
            var distanceFactor = (float)(endObjectArea - startObjectArea - trainPosition[4]) / TrackMonitorWindow.MaximumDistance;
            if (validInfo.direction == -1)
                distanceFactor /= 2;

            if (validInfo.direction == 0)
            {
                // draw direction arrow
                TMCdrawArrow("", offset, forwardArrowSprite, zeroObjectPointMiddle + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                // draw direction arrow
                TMCdrawArrow("", offset, backwardArrowSprite, zeroObjectPointMiddle + arrowPosition[2]);
            }

            if (validInfo.direction != 1)
            {
                // draw fixed distance indications
                var firstMarkerDistance = TMCdrawDistanceMarkers("", offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointTop, 4, true);
                var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

                // draw forward items
                TMCdrawItems("", offset, startObjectArea, endObjectArea, zeroObjectPointTop, zeroObjectPointBottom, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoForward, true);
            }

            if (validInfo.direction != 0)
            {
                // draw fixed distance indications
                var firstMarkerDistance = TMCdrawDistanceMarkers("", offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointBottom, 4, false);
                var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

                // draw backward items
                TMCdrawItems("", offset, startObjectArea, endObjectArea, zeroObjectPointBottom, zeroObjectPointTop, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoBackward, false);
            }

            // draw own train marker
            TMCdrawOwnTrain("", offset, validInfo.direction == -1 ? trainPositionManualOnRouteSprite : validInfo.direction == 0 ? trainPositionAutoForwardsSprite : trainPositionAutoBackwardsSprite, zeroObjectPointTop);
        }

        // draw manual info
        // all details accessed through class variables
        void TMCdrawManualInfo(string a, Point offset)
        {
            var validInfo = Owner.Viewer.PlayerTrain.GetTrainInfo();

            // set area details
            var startObjectArea = additionalInfoHeight;
            var endObjectArea = Monitor.Position.Height - (Monitor.Position.Height - (int)Math.Ceiling((Monitor.Position.Height / Scale))) - additionalInfoHeight;
            var zeroObjectPointMiddle = startObjectArea + (endObjectArea - startObjectArea) / 2;
            var zeroObjectPointTop = zeroObjectPointMiddle + trainPosition[1];
            var zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            var distanceFactor = (float)(zeroObjectPointTop - startObjectArea) / TrackMonitorWindow.MaximumDistance;

            // draw lines through own train
            //spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X, offset.Y + zeroObjectPointTop, Position.Width, 1), Color.DarkGray);
            //spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X, offset.Y + zeroObjectPointBottom - 1, Position.Width, 1), Color.DarkGray);

            // Translate itemLocation value to row value
            var itemLocationWS = itemLocationToRow(zeroObjectPointTop, zeroObjectPointTop) + 1;
            PointTopRow = itemLocationWS - 1;// should be 6

            // Points to current row
            DataCol = TrackControlList[itemLocationWS];
            DataCol.FirstCol = Viewer.Catalog.GetString("SprtrDarkGray");
            DataCol.TrackColLeft = "";
            DataCol.TrackCol = "";
            DataCol.TrackColRight = "";
            DataCol.LimitCol = "";
            DataCol.SignalCol = "";
            DataCol.DistCol = "";
            TrackControlList[itemLocationWS] = DataCol;

            // Translate itemLocation value to row value
            itemLocationWS = itemLocationToRow(zeroObjectPointBottom, zeroObjectPointBottom) - 1;

            PointBottomRow = itemLocationWS + 1;// should be 10

            // Reset SignalCol value
            DataCol = TrackControlList[itemLocationWS];
            DataCol.FirstCol = Viewer.Catalog.GetString("SprtrDarkGray");
            DataCol.TrackColLeft = "";
            DataCol.TrackCol = "";
            DataCol.TrackColRight = "";
            DataCol.LimitCol = "";
            DataCol.SignalCol = "";
            DataCol.DistCol = "";
            TrackControlList[itemLocationWS] = DataCol;

            // draw direction arrow
            if (validInfo.direction == 0)
            {
                TMCdrawArrow("", offset, forwardArrowSprite, zeroObjectPointMiddle + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                TMCdrawArrow("", offset, backwardArrowSprite, zeroObjectPointMiddle + arrowPosition[2]);
            }

            // draw eye
            TMCdrawEye("", offset, 0, Monitor.Position.Height);

            // draw fixed distance indications
            var firstMarkerDistance = TMCdrawDistanceMarkers("", offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointTop, 3, true);
            TMCdrawDistanceMarkers("", offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointBottom, 3, false);  // no return required
            var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

            // draw forward items
            TMCdrawItems("", offset, startObjectArea, endObjectArea, zeroObjectPointTop, zeroObjectPointBottom, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoForward, true);

            // draw backward items
            TMCdrawItems("", offset, startObjectArea, endObjectArea, zeroObjectPointBottom, zeroObjectPointTop, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoBackward, false);

            // draw own train marker
            var ownTrainSprite = validInfo.isOnPath ? trainPositionManualOnRouteSprite : trainPositionManualOffRouteSprite;
            TMCdrawOwnTrain("", offset, ownTrainSprite, zeroObjectPointTop);
        }

        // draw own train marker at required position
        void TMCdrawOwnTrain(string a, Point offset, Rectangle sprite, int position)
        {
            var spriteWS = sprite == trainPositionAutoForwardsSprite ? trainPositionAutoForwardsWS
                   : sprite == trainPositionAutoBackwardsSprite ? trainPositionAutoBackwardsWS
                   : sprite == trainPositionManualOnRouteSprite ? trainPositionManualOnRouteWS
                   : sprite == trainPositionManualOffRouteSprite ? trainPositionManualOffRouteWS
                   : "";

            // Translate itemLocation value to row value
            var itemLocationWS = itemLocationToRow(position, position) + rowOffset;

            DataCol = TrackControlList.Count > 0 ? TrackControlList[itemLocationWS] : new ListTrackControl();
            DataCol.TrackCol = spriteWS;
            DataCol.DistCol = " ";
            TrackControlList[itemLocationWS] = DataCol;

            //spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + trainPosition[0], offset.Y + position, trainPosition[3], trainPosition[4]), sprite, Color.White);
        }

        // draw arrow at required position
        void TMCdrawArrow(string a, Point offset, Rectangle sprite, int position)
        {
            // WebApi
            var arrowDirection = sprite == forwardArrowSprite ? forwardArrowWS : backwardArrowWS;

            // Translate itemLocation value to row value
            var itemLocationWS = itemLocationToRow(position, position) + rowOffset;

            DataCol = TrackControlList[itemLocationWS];
            DataCol.TrackColLeft = arrowDirection;
            TrackControlList[itemLocationWS] = DataCol;
            //spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + arrowPosition[0], offset.Y + position, arrowPosition[3], arrowPosition[4]), sprite, Color.White);
        }

        // draw eye at required position
        void TMCdrawEye(string a, Point offset, int forwardsY, int backwardsY)
        {
            var validInfo = Owner.Viewer.PlayerTrain.GetTrainInfo();
            // draw eye
            if (validInfo.cabOrientation == 0)
            {
                forwardsY = forwardsY - (forwardsY - (int)Math.Ceiling((forwardsY / Scale)));// scaling
                DataCol = TrackControlList[itemLocationToRow(forwardsY , forwardsY )];
                DataCol.TrackCol = eyeWS;
                TrackControlList[itemLocationToRow(forwardsY , forwardsY )] = DataCol;
                //spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + eyePosition[0], offset.Y + forwardsY + eyePosition[1], eyePosition[3], eyePosition[4]), eyeSprite, Color.White);
            }
            else
            {
                backwardsY = backwardsY - (backwardsY - (int)Math.Ceiling((backwardsY / Scale)));// scaling
                DataCol = TrackControlList[itemLocationToRow(backwardsY , backwardsY )];
                DataCol.TrackCol = eyeWS;
                TrackControlList[itemLocationToRow(backwardsY , backwardsY )] = DataCol;
                //spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + eyePosition[0], offset.Y + backwardsY + eyePosition[2], eyePosition[3], eyePosition[4]), eyeSprite, Color.White);
            }
        }

        // draw fixed distance markers
        float TMCdrawDistanceMarkers(string a, Point offset, float maxDistance, float distanceFactor, int zeroPoint, int numberOfMarkers, bool forward)
        {
            var maxDistanceD = Me.FromM(maxDistance, metric); // in displayed units
            var markerIntervalD = maxDistanceD / numberOfMarkers;

            var roundingValue = roundingValues[0];
            foreach (var thisValue in roundingValues)
            {
                if (markerIntervalD > thisValue.Key)
                {
                    roundingValue = thisValue.Value;
                }
            }

            markerIntervalD = Convert.ToInt32(markerIntervalD / roundingValue) * roundingValue;
            var markerIntervalM = Me.ToM(markerIntervalD, metric);  // from display back to metre

            DataCol = TrackControlList[0];
            for (var ipos = 1; ipos <= numberOfMarkers; ipos++)
            {
                var actDistanceM = markerIntervalM * ipos;
                var distanceString = FormatStrings.FormatDistanceDisplay(actDistanceM, metric);
                if (actDistanceM < maxDistance)
                {
                    var itemOffset = Convert.ToInt32(actDistanceM * distanceFactor);
                    var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                    //Font.Draw(spriteBatch, new Point(offset.X + distanceTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]), distanceString, Color.White);

                    // Translate itemLocation value to row value
                    var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);

                    if (ipos == 1)
                    {
                        TopLabelRow = itemLocationWS;
                    }
                    else if (ipos == numberOfMarkers)
                    {   // Useful when overlapping
                        BottomMaxDistanceMarker = itemLocationWS + 1;
                        MaxDistanceMarker = actDistanceM;
                    }

                    DataCol = TrackControlList[itemLocationWS];
                    distanceString = FormatStrings.FormatDistanceDisplay(actDistanceM, metric);
                    DataCol.DistCol = distanceString;
                    TrackControlList[itemLocationWS] = DataCol;
                }
            }

            return markerIntervalM;
        }

        // draw signal, speed and authority items
        // items are sorted in order of increasing distance
        void TMCdrawItems(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, int lastLabelPosition, float maxDistance, float distanceFactor, int firstLabelPosition, List<Train.TrainObjectItem> itemList, bool forward)
        {
            var signalShown = false;
            var firstLabelShown = false;
            var borderSignalShown = false;

            foreach (var thisItem in itemList)
            {
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY:
                        TMCdrawAuthority("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL:
                        lastLabelPosition = TMCdrawSignalForward("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref signalShown, ref borderSignalShown, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST:
                        lastLabelPosition = TMCdrawSpeedpost("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.STATION:
                        TMCdrawStation("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.WAITING_POINT:
                        TMCdrawWaitingPoint("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.MILEPOST:
                        lastLabelPosition = TMCdrawMilePost("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.FACING_SWITCH:
                        TMCdrawSwitch("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.REVERSAL:
                        TMCdrawReversal("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    default:     // capture unkown item
                        break;
                }
            }
            //drawReversal and drawSwitch icons on top.
            foreach (var thisItem in itemList)
            {
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.FACING_SWITCH:
                        //TMCdrawSwitch("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.REVERSAL:
                        //TMCdrawReversal("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    default:
                        break;
                }
            }
            // reverse display of signals to have correct superposition
            for (int iItems = itemList.Count - 1; iItems >= 0; iItems--)
            {
                var thisItem = itemList[iItems];
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL:
                        //TMCdrawSignalBackward("", offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, forward, thisItem, signalShown);
                        break;

                    default:
                        break;
                }
            }
        }

        // draw authority information
        void TMCdrawAuthority(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = new Rectangle(0, 0, 0, 0);
            var displayItemWS = "";// WebApi
            var displayRequired = false;
            var offsetArray = new int[0];

            if (thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_AUTHORITY ||
                thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_PATH ||
                thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_TRACK ||
                thisItem.AuthorityType == Train.END_AUTHORITY.RESERVED_SWITCH ||
                thisItem.AuthorityType == Train.END_AUTHORITY.LOOP)
            {
                displayItem = endAuthoritySprite;
                displayItemWS = endAuthorityWS;// WebApi
                offsetArray = endAuthorityPosition;
                displayRequired = true;
            }
            else if (thisItem.AuthorityType == Train.END_AUTHORITY.TRAIN_AHEAD)
            {
                displayItem = forward ? oppositeTrainForwardSprite : oppositeTrainBackwardSprite;
                displayItemWS = forward ? oppositeTrainForwardWS : oppositeTrainBackwardWS;// WebApi
                offsetArray = otherTrainPosition;
                displayRequired = true;
            }

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor) && displayRequired)
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                //spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + offsetArray[0], offset.Y + itemLocation + offsetArray[forward ? 1 : 2], offsetArray[3], offsetArray[4]), displayItem, Color.White);

                // Translate itemLocation value to row value
                var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);

                DataCol = TrackControlList[itemLocationWS];
                DataCol.TrackCol = displayItemWS;

                // Avoids overlapping
                Overlapping(itemLocationWS, DataCol, thisItem, displayItemWS, "", firstLabelShown);

                if (itemOffset < firstLabelPosition && !firstLabelShown)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    //Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;

                    // AuthorityType WebApi
                    DataCol.DistCol = distanceString + "!??";
                    DataCol.TrackCol = displayItemWS;
                    TrackControlList[itemLocationWS] = DataCol;
                }
            }
        }

        // check signal information for reverse display
        int TMCdrawSignalForward(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool signalShown, ref bool borderSignalShown, ref bool firstLabelShown)
        {
            var displayItem = SignalMarkers[thisItem.SignalState];
            var newLabelPosition = lastLabelPosition;

            var displayRequired = false;
            var itemLocation = 0;
            var itemOffset = 0;
            var maxDisplayDistance = maxDistance - (textSpacing / 2) / distanceFactor;

            if (thisItem.DistanceToTrainM < maxDisplayDistance)
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                displayRequired = true;
                signalShown = true;
            }
            else if (!borderSignalShown && !signalShown)
            {
                itemOffset = 2 * startObjectArea;
                itemLocation = forward ? startObjectArea : endObjectArea;
                displayRequired = true;
                borderSignalShown = true;
            }

            // Translate itemLocation value to row value
            var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);

            // Reset SignalCol value
            DataCol = TrackControlList[itemLocationWS];

            if (displayRequired)
            {
                // SignalState WebApi
                var SignalStateItem = SignalMarkersWebApi[thisItem.SignalState];// WebApi
                var speedString = "";

                if (thisItem.SignalState != TrackMonitorSignalAspect.Stop && thisItem.AllowedSpeedMpS > 0)
                {
                    var labelPoint = new Point(offset.X + speedTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]);
                    speedString = FormatStrings.FormatSpeedLimitNoUoM(thisItem.AllowedSpeedMpS, metric);
                    speedString = speedString + "!??";// Color.White WebApi
                    //Font.Draw(spriteBatch, labelPoint, speedString, Color.White);
                }

                // Avoids overlapping
                Overlapping(itemLocationWS, DataCol, thisItem, SignalStateItem, speedString, firstLabelShown);

                if ((itemOffset < firstLabelPosition && !firstLabelShown && itemLocationWS > TopLabelRow) || thisItem.DistanceToTrainM > maxDisplayDistance)
                {
                    if (!itemLocationWSChanged)
                    {
                        var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]);
                        var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                        //Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);

                        DataCol.DistanceToTrainM = thisItem.DistanceToTrainM;
                        DataCol.DistCol = distanceString;
                        DataCol.LimitCol = speedString;
                        DataCol.Row = itemLocationWS;
                        DataCol.SignalCol = SignalStateItem;
                        TrackControlList[itemLocationWS] = DataCol;
                    }
                    firstLabelShown = true;
                }
            }
            return newLabelPosition;
        }

        // draw signal information
        void TMCdrawSignalBackward(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, bool forward, Train.TrainObjectItem thisItem, bool signalShown)
        {
            var displayItem = SignalMarkers[thisItem.SignalState];
            var displayRequired = false;
            var itemLocation = 0;
            var itemOffset = 0;
            var maxDisplayDistance = maxDistance - (textSpacing / 2) / distanceFactor;

            if (thisItem.DistanceToTrainM < maxDisplayDistance)
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                displayRequired = true;
            }
            else if (!signalShown)
            {
                itemOffset = 2 * startObjectArea;
                itemLocation = forward ? startObjectArea : endObjectArea;
                displayRequired = true;
            }

            if (displayRequired)
            {
                //spriteBatch.Draw(SignalAspects, new Rectangle(offset.X + signalPosition[0], offset.Y + itemLocation + signalPosition[forward ? 1 : 2], signalPosition[3], signalPosition[4]), displayItem, Color.White);

                var SignalSateItem = SignalMarkersWebApi[thisItem.SignalState];// WebApi
                var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);

                // Translate itemLocation value to row value
                var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);

                DataCol = TrackControlList[itemLocationWS];

                // Avoids overlapping
                Overlapping(itemLocationWS, DataCol, thisItem, SignalSateItem, "", false);

                //spriteBatch.Draw(SignalAspects, new Rectangle(offset.X + signalPosition[0], offset.Y + itemLocation + signalPosition[forward ? 1 : 2], signalPosition[3], signalPosition[4]), displayItem, Color.White);

                DataCol.SignalCol = SignalSateItem;
                if (thisItem.DistanceToTrainM < markerIntervalM)
                {
                    DataCol.DistCol = thisItem.DistanceToTrainM == 0 ? " " : distanceString;
                }
            }
        }

        // draw speedpost information
        int TMCdrawSpeedpost(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                // Translate itemLocation value to row value
                var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);
                DataCol = TrackControlList[itemLocationWS];

                var allowedSpeed = thisItem.AllowedSpeedMpS;
                if (allowedSpeed > 998)
                {
                    if (!Program.Simulator.TimetableMode)
                    {
                        allowedSpeed = (float)Program.Simulator.TRK.Tr_RouteFile.SpeedLimit;
                    }
                }

                var labelPoint = new Point(offset.X + speedTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                var speedString = FormatStrings.FormatSpeedLimitNoUoM(allowedSpeed, metric);
                //Font.Draw(spriteBatch, labelPoint, speedString, thisItem.SpeedObjectType == Train.TrainObjectItem.SpeedItemType.Standard ? Color.White :
                //    (thisItem.SpeedObjectType == Train.TrainObjectItem.SpeedItemType.TempRestrictedStart ? Color.Red : Color.LightGreen));

                //webApi
                speedString = speedString + (thisItem.SpeedObjectType == Train.TrainObjectItem.SpeedItemType.Standard ? "!??" :
                    (thisItem.SpeedObjectType == Train.TrainObjectItem.SpeedItemType.TempRestrictedStart ? "!!!" : "%%$"));

                // Avoids overlapping
                Overlapping(itemLocationWS, DataCol, thisItem, speedString, "", firstLabelShown);

                if (itemOffset < firstLabelPosition && !firstLabelShown && itemLocationWS > TopLabelRow)
                {
                    if (!itemLocationWSChanged)
                    {
                        labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                        var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                        //Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);

                        DataCol.DistanceToTrainM = thisItem.DistanceToTrainM;
                        DataCol.DistCol = distanceString;
                        DataCol.LimitCol = speedString;
                        DataCol.Row = itemLocationWS;
                        TrackControlList[itemLocationWS] = DataCol;
                    }
                    firstLabelShown = true;
                }
            }
            return newLabelPosition;
        }

        // draw station stop information
        int TMCdrawStation(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem)
        {
            var CurrentTime = FormatStrings.FormatTime(Owner.Viewer.Simulator.ClockTime);
            var StationPlatform = "";
            var StationCurrentDepartScheduled = "";
            var MessageText = "";
            var MessageColor = Color.White;

            Train playerTrain = Owner.Viewer.Simulator.PlayerLocomotive.Train;
            Simulation.ActivityTaskPassengerStopAt atask = null;

            Simulation.Timetables.TTTrain playerTimetableTrain = playerTrain as Simulation.Timetables.TTTrain;
            if (playerTimetableTrain != null && playerTimetableTrain.TrainType == Train.TRAINTYPE.PLAYER && playerTimetableTrain.StationStops[0].ActualStopType == Train.StationStop.STOPTYPE.STATION_STOP)
            {
                StationPlatform = playerTimetableTrain.StationStops[0].PlatformItem.Name;
                StationCurrentDepartScheduled = playerTimetableTrain.StationStops[0].departureDT.ToString("HH:mm:ss");
                MessageText = playerTimetableTrain.DisplayMessage;
                MessageColor = playerTimetableTrain.DisplayColor;
            }
            else
            {
                Simulation.Activity act = Owner.Viewer.Simulator.ActivityRun;
                Simulation.ActivityTaskPassengerStopAt Current = null;
                if (act != null && playerTrain == Owner.Viewer.Simulator.OriginalPlayerTrain)
                {
                    Current = act.Current == null ? act.Last as Simulation.ActivityTaskPassengerStopAt : act.Current as Simulation.ActivityTaskPassengerStopAt;
                    atask = Current != null ? Current.PrevTask as Simulation.ActivityTaskPassengerStopAt : null;
                }
                atask = Current;
                if (atask != null)
                {
                    StationPlatform = atask.PlatformEnd1.ItemName;
                }
            }

            //var displayItem = stationSprite;
            var newLabelPosition = lastLabelPosition;
            var itemOffset = Convert.ToInt32((thisItem.DistanceToTrainM - thisItem.StationPlatformLength) * distanceFactor);

            var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;

            // Translate itemLocation value to row value
            var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);
            DataCol = TrackControlList[itemLocationWS];

            // Avoids overlapping
            Overlapping(itemLocationWS, DataCol, thisItem, stationLeftWS, stationRightWS, false);

            // Station info
            var thisInfo = Owner.Viewer.PlayerTrain.GetTrainInfo();
            var absoluteSpeedMpS = Math.Abs(thisInfo.speedMpS);
            var trackColor = absoluteSpeedMpS < thisInfo.allowedSpeedMpS - 1.0f ? "??!" :// Color.Green :
                    absoluteSpeedMpS < thisInfo.allowedSpeedMpS + 0.0f ? "?!!" :// Color.PaleGreen :
                    absoluteSpeedMpS < thisInfo.allowedSpeedMpS + 5.0f ? "!!?" : "!!!";// Color.Orange : Color.Red;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor) && !itemLocationWSChanged)
            {
                //var startOfPlatform = (int)Math.Max(stationPosition[4], thisItem.StationPlatformLength * distanceFactor);
                //var markerPlacement = new Rectangle(offset.X + stationPosition[0], offset.Y + itemLocation + stationPosition[forward ? 1 : 2], stationPosition[3], startOfPlatform);
                //spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);

                // Update info
                DataCol.DistanceToTrainM = thisItem.DistanceToTrainM;
                DataCol.TrackColLeft = stationLeftWS;
                DataCol.TrackColRight = stationRightWS;

                if (itemLocationWS == (zeroPoint == 108 ? 6 : 11))
                {
                    DataCol.DistCol = "";// Station does not show the distance

                    if (thisInfo.speedMpS > 0 && ((atask != null && atask.DisplayMessage.Length == 0) || (MessageText != null && MessageText.Length == 0)))
                    {
                        ListTrackControl DataColTemp = TrackControlList[itemLocationWS + rowOffset];
                        if (thisItem.DistanceToTrainM < thisItem.StationPlatformLength)
                        {   // draw station symbol when inside station platform
                            DataColTemp.TrackColLeft = DataCol.TrackColLeft;
                            DataColTemp.TrackColRight = DataCol.TrackColRight;
                            TrackControlList[itemLocationWS + rowOffset] = DataColTemp;
                        }

                        if (thisItem.DistanceToTrainM > 0)
                        {   // arriving at station
                            DataCol.TrackColLeft = stationLeftWS;
                            DataCol.TrackColRight = stationRightWS;
                        }
                        else
                        {   // missing the station
                            DataCol.TrackColLeft = "";
                            DataCol.TrackColRight = "";

                            DataColTemp = TrackControlList[itemLocationWS + rowOffset + 1];
                            DataColTemp.TrackColLeft = DataColTemp.TrackColLeft + stationLeftWS;
                            DataColTemp.TrackColRight = stationRightWS;
                            TrackControlList[itemLocationWS + rowOffset + 1] = DataColTemp;
                        }
                    }
                    else if (thisInfo.speedMpS < 1 && thisItem.DistanceToTrainM < thisItem.StationPlatformLength)
                    {
                        // Further to the train
                        ListTrackControl DataColTemp = TrackControlList[itemLocationWS > 0 ? itemLocationWS - 1 : 0];
                        DataColTemp.Row = itemLocationWS > 0 ? itemLocationWS - 1 : 0;
                        DataColTemp.DistanceToTrainM = DataCol.DistanceToTrainM;
                        TrackControlList[itemLocationWS > 0 ? itemLocationWS - 1 : 0] = DataColTemp;

                        DataColTemp = TrackControlList[itemLocationWS + rowOffset];
                        DataColTemp.TrackColLeft = DataCol.TrackColLeft;
                        DataColTemp.TrackColRight = DataCol.TrackColRight;

                        // Split Stationplatform
                        if (StationPlatform.Length > 0)
                        {
                            var maxCharCell = 12;
                            var stationSplit = StationPlatform.Split(' ');
                            System.Text.StringBuilder cellData = new System.Text.StringBuilder();
                            var rowStation = itemLocationWS + rowOffset;
                            foreach (var station in stationSplit)
                            {
                                if (station.Length + cellData.Length > maxCharCell || station == stationSplit[stationSplit.Count() - 1])
                                {
                                    if (station == stationSplit[stationSplit.Count() - 1])
                                    {
                                        cellData.Append(station);// last item
                                    }

                                    if (rowStation == itemLocationWS + rowOffset)
                                    {
                                        DataColTemp.LimitCol = cellData.ToString();
                                    }
                                    else
                                    {
                                        ListTrackControl DataColStation = TrackControlList[rowStation];
                                        DataColStation.LimitCol = cellData.ToString();
                                        TrackControlList[rowStation] = DataColStation;
                                    }

                                    rowStation += 1;
                                    if (rowStation > 15) { break; }

                                    cellData = new System.Text.StringBuilder();
                                    cellData.Append(station + " ");
                                }
                                else if (station.Length + cellData.Length <= maxCharCell)
                                {
                                    cellData.Append(station + " ");
                                }
                            }
                        }

                        DataCol.TrackCol = trackWS + trackColor;
                        DataCol.TrackColLeft = DataCol.TrackColRight = " ";

                        // decodes displaycolor
                        if (atask != null)
                        {
                            var color = (atask.DisplayColor.R == 144 && atask.DisplayColor.G == 238 && atask.DisplayColor.B == 144) ? "%%$" ://Color.LightGreen
                            (atask.DisplayColor.R == 255 && atask.DisplayColor.G == 255 && atask.DisplayColor.B == 255) ? "!??" ://Color.White
                            (atask.DisplayColor.R == 255 && atask.DisplayColor.G == 255 && atask.DisplayColor.B == 128) ? "???" : "";//Color.Yellow

                            // show the time left to boarding and the station name.
                            DataColTemp.FirstCol = atask.DisplayMessage.Any(char.IsDigit) ? atask.DisplayMessage.Substring(atask.DisplayMessage.Length - 5) + color : Viewer.Catalog.GetString("Completed.") + "%%$";
                        }
                        else if (MessageText != null)
                        {
                            var color = (MessageColor.R == 144 && MessageColor.G == 238 && MessageColor.B == 144) ? "%%$" ://Color.LightGreen
                               (MessageColor.R == 255 && MessageColor.G == 255 && MessageColor.B == 255) ? "!??" ://Color.White
                               (MessageColor.R == 255 && MessageColor.G == 255 && MessageColor.B == 128) ? "???" : "";//Color.Yellow

                            // show the time left to boarding and the station name.
                            DataColTemp.FirstCol = MessageText.Any(char.IsDigit) ? MessageText.Substring(MessageText.Length - 5) + color : Viewer.Catalog.GetString("Completed.") + "%%$";
                        }
                        TrackControlList[itemLocationWS + rowOffset] = DataColTemp;
                    }
                }
                TrackControlList[itemLocationWS] = DataCol;
            }
            TrackControlList[itemLocationWS] = DataCol;

            return newLabelPosition;
        }

        // draw reversal information
        int TMCdrawReversal(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = thisItem.Valid ? reversalSprite : invalidReversalSprite;
            var displayItemWS = thisItem.Valid ? reversalWS : invalidReversalWS;
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                // What was this offset all about? Shouldn't we draw the icons in the correct location ALL the time? -- James Ross
                // var correctingOffset = Program.Simulator.TimetableMode || !Program.Simulator.Settings.EnhancedActCompatibility ? 0 : 7;

                if (thisItem.Valid)
                {
                    //var markerPlacement = new Rectangle(offset.X + reversalPosition[0], offset.Y + itemLocation + reversalPosition[forward ? 1 : 2], reversalPosition[3], reversalPosition[4]);
                    //spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, thisItem.Enabled ? Color.LightGreen : Color.White);
                    displayItemWS = displayItemWS == invalidReversalWS ? invalidReversalWS// ▬ yellow
                        : thisItem.Enabled ? reversalWS + "%%$" // ↶ Color.LightGreen
                        : reversalWS + "!??";// ↶ Color.White
                }
                else
                {
                    //var markerPlacement = new Rectangle(offset.X + invalidReversalPosition[0], offset.Y + itemLocation + invalidReversalPosition[forward ? 1 : 2], invalidReversalPosition[3], invalidReversalPosition[4]);
                    //spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);
                    displayItemWS = displayItemWS == invalidReversalWS ? invalidReversalWS // ▬ yellow
                        : reversalWS + "!??"; // ↶ Color.White
                }

                // Translate itemLocation value to row value
                var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);
                DataCol = TrackControlList[itemLocationWS];
                // Avoids overlapping
                Overlapping(itemLocationWS, DataCol, thisItem, displayItemWS, "", firstLabelShown);

                // Only show distance for enhanced MSTS compatibility (this is the only time the position is controlled by the author).
                if (itemOffset < firstLabelDistance && !firstLabelShown && !Program.Simulator.TimetableMode)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    //Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);

                    if (itemLocationWS > TopLabelRow)
                    {
                        DataCol.DistCol = distanceString;
                    }
                    DataCol.Row = itemLocationWS;
                    TrackControlList[itemLocationWS] = DataCol;

                    firstLabelShown = true;
                }
            }
            return newLabelPosition;
        }

        // draw waiting point information
        int TMCdrawWaitingPoint(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = waitingPointSprite;
            var displayItemWS = waitingPointWS;// WebApi
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                //var markerPlacement = new Rectangle(offset.X + waitingPointPosition[0], offset.Y + itemLocation + waitingPointPosition[forward ? 1 : 2], waitingPointPosition[3], waitingPointPosition[4]);
                //spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, thisItem.Enabled ? Color.Yellow : Color.Red);

                // Translate itemLocation value to row value
                var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);
                DataCol = TrackControlList[itemLocationWS];

                var waitpointWS = thisItem.Enabled ? waitingPointWS + "???" : waitingPointWS + "!!!";//Color.Yellow : Color.Red <- OrangeRed
                var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                DataCol.TrackCol = waitpointWS;

                if (thisItem.DistanceToTrainM < markerIntervalM)
                {
                    DataCol.DistCol = thisItem.DistanceToTrainM == 0 ? " " : distanceString;
                }

                // Avoids overlapping
                Overlapping(itemLocationWS, DataCol, thisItem, waitingPointWS, "", firstLabelShown);

                if (itemOffset < firstLabelDistance && !firstLabelShown)
                {
                    if (!itemLocationWSChanged)
                    {
                        var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                        distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                        //Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                        DataCol.TrackCol = waitingPointWS + "!??";
                        TrackControlList[itemLocationWS] = DataCol;
                    }
                    firstLabelShown = true;
                }
            }
            return newLabelPosition;
        }

        // draw milepost information
        int TMCdrawMilePost(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);
                //var labelPoint = new Point(offset.X + milepostTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                var milepostString = thisItem.ThisMile;
                //Font.Draw(spriteBatch, labelPoint, milepostString, Color.White);

                // Translate itemLocation value to row value
                var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);
                DataCol = TrackControlList[itemLocationWS];

                // Avoids overlapping
                Overlapping(itemLocationWS, DataCol, thisItem, milepostString, "", firstLabelShown);
            }
            return newLabelPosition;
        }

        // draw switch information
        int TMCdrawSwitch(string a, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = thisItem.IsRightSwitch ? rightArrowSprite : leftArrowSprite;
            var displayItemWS = thisItem.IsRightSwitch ? rightArrowWS : leftArrowWS;
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                //var markerPlacement = thisItem.IsRightSwitch ?
                //    new Rectangle(offset.X + rightSwitchPosition[0], offset.Y + itemLocation + rightSwitchPosition[forward ? 1 : 2], rightSwitchPosition[3], rightSwitchPosition[4]) :
                //    new Rectangle(offset.X + leftSwitchPosition[0], offset.Y + itemLocation + leftSwitchPosition[forward ? 1 : 2], leftSwitchPosition[3], leftSwitchPosition[4]);
                //spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);

                // Translate itemLocation value to row value
                var itemLocationWS = itemLocationToRow(zeroPoint, itemLocation);
                DataCol = TrackControlList[itemLocationWS];

                // Avoids overlapping
                Overlapping(itemLocationWS, DataCol, thisItem, thisItem.IsRightSwitch ? rightArrowWS : leftArrowWS, "", firstLabelShown);

                // Only show distance for enhanced MSTS compatibility (this is the only time the position is controlled by the author).
                if (itemOffset < firstLabelDistance && !firstLabelShown && !Program.Simulator.TimetableMode)
                {
                    if (!itemLocationWSChanged)
                    {
                        var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                        var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                        //Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);

                        DataCol.DistCol = thisItem.DistanceToTrainM == 0 ? " " : distanceString;
                        DataCol.DistanceToTrainM = thisItem.DistanceToTrainM;
                        DataCol.Row = itemLocationWS;
                        DataCol.TrackColLeft = DataCol.TrackColLeft;
                        DataCol.TrackColRight = DataCol.TrackColRight;
                        TrackControlList[itemLocationWS] = DataCol;
                    }
                    firstLabelShown = true;
                }
            }
            return newLabelPosition;
        }
    }

    public class TrackMonitor : Control
    {
        static Texture2D SignalAspects;
        static Texture2D TrackMonitorImages;
        static Texture2D MonitorTexture;

        WindowTextFont Font;

        readonly Viewer Viewer;
        private bool metric => Viewer.MilepostUnitsMetric;
        private readonly SavingProperty<int> StateProperty;
        public DisplayMode Mode
        {
            get => (DisplayMode)StateProperty.Value;
            private set
            {
                StateProperty.Value = (int)value;
            }
        }

        /// <summary>
        /// Different information views for the Track Monitor.
        /// </summary>
        public enum DisplayMode
        {
            /// <summary>
            /// Display all track and routing features.
            /// </summary>
            All = 0,
            /// <summary>
            /// Show only the static features that a train driver would know by memory.
            /// </summary>
            StaticOnly = 1,
        }

        public static int DbfEvalOverSpeed;//Debrief eval
        bool istrackColorRed = false;//Debrief eval
        public static double DbfEvalOverSpeedTimeS = 0;//Debrief eval
        public static double DbfEvalIniOverSpeedTimeS = 0;//Debrief eval

        Train.TrainInfo validInfo;

        const int DesignWidth = 150; // All Width/X values are relative to this width.

        // position constants
        readonly int additionalInfoHeight = 16; // vertical offset on window for additional out-of-range info at top and bottom
        readonly int[] mainOffset = new int[2] { 12, 12 }; // offset for items, cell 0 is upward, 1 is downward
        readonly int textSpacing = 10; // minimum vertical distance between two labels

        // The track is 24 wide = 6 + 2 + 8 + 2 + 6.
        readonly int trackRail1Offset = 6;
        readonly int trackRail2Offset = 6 + 2 + 8;
        readonly int trackRailWidth = 2;

        // Vertical offset for text for forwards ([0]) and backwards ([1]).
        readonly int[] textOffset = new int[2] { -11, -3 };

        // Horizontal offsets for various elements.
        readonly int distanceTextOffset = 117;
        readonly int trackOffset = 42;
        readonly int speedTextOffset = 70;
        readonly int milepostTextOffset = 0;

        // position definition arrays
        // contents :
        // cell 0 : X offset
        // cell 1 : Y offset down from top (absolute)/item location (relative)
        // cell 2 : Y offset down from bottom (absolute)/item location (relative)
        // cell 3 : X size
        // cell 4 : Y size

        int[] eyePosition = new int[5] { 42, -4, -20, 24, 24 };
        int[] trainPosition = new int[5] { 42, -12, -12, 24, 24 }; // Relative positioning
        int[] otherTrainPosition = new int[5] { 42, -24, 0, 24, 24 }; // Relative positioning
        int[] stationPosition = new int[5] { 42, 0, -24, 24, 12 }; // Relative positioning
        int[] reversalPosition = new int[5] { 42, -21, -3, 24, 24 }; // Relative positioning
        int[] waitingPointPosition = new int[5] { 42, -21, -3, 24, 24 }; // Relative positioning
        int[] endAuthorityPosition = new int[5] { 42, -14, -10, 24, 24 }; // Relative positioning
        int[] signalPosition = new int[5] { 95, -16, 0, 16, 16 }; // Relative positioning
        int[] arrowPosition = new int[5] { 22, -12, -12, 24, 24 };
        int[] invalidReversalPosition = new int[5] { 42, -14, -10, 24, 24 }; // Relative positioning
        int[] leftSwitchPosition = new int[5] { 37, -14, -10, 24, 24 }; // Relative positioning
        int[] rightSwitchPosition = new int[5] { 47, -14, -10, 24, 24 }; // Relative positioning

        // texture rectangles : X-offset, Y-offset, width, height
        Rectangle eyeSprite = new Rectangle(0, 144, 24, 24);
        Rectangle trainPositionAutoForwardsSprite = new Rectangle(0, 72, 24, 24);
        Rectangle trainPositionAutoBackwardsSprite = new Rectangle(24, 72, 24, 24);
        Rectangle trainPositionManualOnRouteSprite = new Rectangle(24, 96, 24, 24);
        Rectangle trainPositionManualOffRouteSprite = new Rectangle(0, 96, 24, 24);
        Rectangle endAuthoritySprite = new Rectangle(0, 0, 24, 24);
        Rectangle oppositeTrainForwardSprite = new Rectangle(24, 120, 24, 24);
        Rectangle oppositeTrainBackwardSprite = new Rectangle(0, 120, 24, 24);
        Rectangle stationSprite = new Rectangle(24, 0, 24, 24);
        Rectangle reversalSprite = new Rectangle(0, 24, 24, 24);
        Rectangle waitingPointSprite = new Rectangle(24, 24, 24, 24);
        Rectangle forwardArrowSprite = new Rectangle(24, 48, 24, 24);
        Rectangle backwardArrowSprite = new Rectangle(0, 48, 24, 24);
        Rectangle invalidReversalSprite = new Rectangle(24, 144, 24, 24);
        Rectangle leftArrowSprite = new Rectangle(0, 168, 24, 24);
        Rectangle rightArrowSprite = new Rectangle(24, 168, 24, 24);

        Dictionary<TrackMonitorSignalAspect, Rectangle> SignalMarkers = new Dictionary<TrackMonitorSignalAspect, Rectangle>
        {
            { TrackMonitorSignalAspect.Clear_2, new Rectangle(0, 0, 16, 16) },
            { TrackMonitorSignalAspect.Clear_1, new Rectangle(16, 0, 16, 16) },
            { TrackMonitorSignalAspect.Approach_3, new Rectangle(0, 16, 16, 16) },
            { TrackMonitorSignalAspect.Approach_2, new Rectangle(16, 16, 16, 16) },
            { TrackMonitorSignalAspect.Approach_1, new Rectangle(0, 32, 16, 16) },
            { TrackMonitorSignalAspect.Restricted, new Rectangle(16, 32, 16, 16) },
            { TrackMonitorSignalAspect.StopAndProceed, new Rectangle(0, 48, 16, 16) },
            { TrackMonitorSignalAspect.Stop, new Rectangle(16, 48, 16, 16) },
            { TrackMonitorSignalAspect.Permission, new Rectangle(0, 64, 16, 16) },
            { TrackMonitorSignalAspect.None, new Rectangle(16, 64, 16, 16) }
        };

        // fixed distance rounding values as function of maximum distance
        Dictionary<float, float> roundingValues = new Dictionary<float, float>
        {
            { 0.0f, 0.5f },
            { 5.0f, 1.0f },
            { 10.0f, 2.0f }
        };

        public TrackMonitor(int width, int height, WindowManager owner)
            : base(0, 0, width, height)
        {
            if (SignalAspects == null)
                // TODO: This should happen on the loader thread.
                SignalAspects = SharedTextureManager.Get(owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(owner.Viewer.ContentPath, "SignalAspects.png"));
            if (TrackMonitorImages == null)
                // TODO: This should happen on the loader thread.
                TrackMonitorImages = SharedTextureManager.Get(owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(owner.Viewer.ContentPath, "TrackMonitorImages.png"));

            Viewer = owner.Viewer;
            StateProperty = Viewer.Settings.GetSavingProperty<int>("TrackMonitorDisplayMode");
            Font = owner.TextFontSmall;

            ScaleDesign(ref additionalInfoHeight);
            ScaleDesign(ref mainOffset);
            ScaleDesign(ref textSpacing);

            ScaleDesign(ref trackRail1Offset);
            ScaleDesign(ref trackRail2Offset);
            ScaleDesign(ref trackRailWidth);

            ScaleDesign(ref textOffset);

            ScaleDesign(ref distanceTextOffset);
            ScaleDesign(ref trackOffset);
            ScaleDesign(ref speedTextOffset);

            ScaleDesign(ref eyePosition);
            ScaleDesign(ref trainPosition);
            ScaleDesign(ref otherTrainPosition);
            ScaleDesign(ref stationPosition);
            ScaleDesign(ref reversalPosition);
            ScaleDesign(ref waitingPointPosition);
            ScaleDesign(ref endAuthorityPosition);
            ScaleDesign(ref signalPosition);
            ScaleDesign(ref arrowPosition);
            ScaleDesign(ref leftSwitchPosition);
            ScaleDesign(ref rightSwitchPosition);
            ScaleDesign(ref invalidReversalPosition);
        }

        /// <summary>
        /// Change the Track Monitor display mode.
        /// </summary>
        public void CycleMode()
        {
            switch (Mode)
            {
                case DisplayMode.All:
                default:
                    Mode = DisplayMode.StaticOnly;
                    break;
                case DisplayMode.StaticOnly:
                    Mode = DisplayMode.All;
                    break;
            }
        }

        void ScaleDesign(ref int variable)
        {
            variable = variable * Position.Width / DesignWidth;
        }

        void ScaleDesign(ref int[] variable)
        {
            for (var i = 0; i < variable.Length; i++)
                ScaleDesign(ref variable[i]);
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            if (MonitorTexture == null)
            {
                MonitorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                MonitorTexture.SetData(new[] { Color.White });
            }

            // Adjust offset to point at the control's position so we can keep code below simple.
            offset.X += Position.X;
            offset.Y += Position.Y;

            if (validInfo == null)
            {
                drawTrack(spriteBatch, offset, 0f, 1f);
                return;
            }

            drawTrack(spriteBatch, offset, validInfo.speedMpS, validInfo.allowedSpeedMpS);

            if (Orts.MultiPlayer.MPManager.IsMultiPlayer())
            {
                drawMPInfo(spriteBatch, offset);
            }
            else if (validInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_NODE || validInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_SIGNAL)
            {
                drawAutoInfo(spriteBatch, offset);
            }
            else if (validInfo.ControlMode == Train.TRAIN_CONTROL.TURNTABLE) return;
            else
            {
                drawManualInfo(spriteBatch, offset);
            }
        }

        public void StoreInfo(Train.TrainInfo thisInfo)
        {
            validInfo = thisInfo;
        }

        void drawTrack(SpriteBatch spriteBatch, Point offset, float speedMpS, float allowedSpeedMpS)
        {
            var train = Program.Viewer.PlayerLocomotive.Train;
            var absoluteSpeedMpS = Math.Abs(speedMpS);
            var trackColor =
                absoluteSpeedMpS < allowedSpeedMpS - 1.0f ? Color.Green :
                absoluteSpeedMpS < allowedSpeedMpS + 0.0f ? Color.PaleGreen :
                absoluteSpeedMpS < allowedSpeedMpS + 5.0f ? Color.Orange : Color.Red;

            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X + trackOffset + trackRail1Offset, offset.Y, trackRailWidth, Position.Height), trackColor);
            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X + trackOffset + trackRail2Offset, offset.Y, trackRailWidth, Position.Height), trackColor);

            if (trackColor == Color.Red && !istrackColorRed)//Debrief Eval
            {
                istrackColorRed = true;
                DbfEvalIniOverSpeedTimeS = Orts.MultiPlayer.MPManager.Simulator.ClockTime;
            }            

            if (istrackColorRed && trackColor != Color.Red)//Debrief Eval
            {
                istrackColorRed = false;
                DbfEvalOverSpeed++;
            }

            if (istrackColorRed && (Orts.MultiPlayer.MPManager.Simulator.ClockTime - DbfEvalIniOverSpeedTimeS) > 1.0000)//Debrief Eval
            {
                DbfEvalOverSpeedTimeS = DbfEvalOverSpeedTimeS + (Orts.MultiPlayer.MPManager.Simulator.ClockTime - DbfEvalIniOverSpeedTimeS);
                train.DbfEvalValueChanged = true;
                DbfEvalIniOverSpeedTimeS = Orts.MultiPlayer.MPManager.Simulator.ClockTime;
            }
        }

        void drawAutoInfo(SpriteBatch spriteBatch, Point offset)
        {
            // set area details
            var startObjectArea = additionalInfoHeight;
            var endObjectArea = Position.Height - additionalInfoHeight - trainPosition[4];
            var zeroObjectPointTop = endObjectArea;
            var zeroObjectPointMiddle = zeroObjectPointTop - trainPosition[1];
            var zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            var distanceFactor = (float)(endObjectArea - startObjectArea) / TrackMonitorWindow.MaximumDistance;

            // draw train position line
            // use red if no info for reverse move available
            var lineColor = Color.DarkGray;
            if (validInfo.ObjectInfoBackward != null && validInfo.ObjectInfoBackward.Count > 0 &&
                validInfo.ObjectInfoBackward[0].ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY &&
                validInfo.ObjectInfoBackward[0].AuthorityType == Train.END_AUTHORITY.NO_PATH_RESERVED)
            {
                lineColor = Color.Red;
            }
            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X, offset.Y + endObjectArea, Position.Width, 1), lineColor);

            // draw direction arrow
            if (validInfo.direction == 0)
            {
                drawArrow(spriteBatch, offset, forwardArrowSprite, zeroObjectPointMiddle + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                drawArrow(spriteBatch, offset, backwardArrowSprite, zeroObjectPointMiddle + arrowPosition[2]);
            }

            // draw eye
            drawEye(spriteBatch, offset, 0, Position.Height);

            // draw fixed distance indications
            var firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointTop, 4, true);
            var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

            // draw forward items
            drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointTop, zeroObjectPointBottom, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoForward, true);

            // draw own train marker
            drawOwnTrain(spriteBatch, offset, trainPositionAutoForwardsSprite, zeroObjectPointTop);
        }

        // draw Multiplayer info
        // all details accessed through class variables

        void drawMPInfo(SpriteBatch spriteBatch, Point offset)
        {
            // set area details
            var startObjectArea = additionalInfoHeight;
            var endObjectArea = Position.Height - additionalInfoHeight;
            var zeroObjectPointTop = 0;
            var zeroObjectPointMiddle = 0;
            var zeroObjectPointBottom = 0;
            if (validInfo.direction == 0)
            {
                zeroObjectPointTop = endObjectArea - trainPosition[4];
                zeroObjectPointMiddle = zeroObjectPointTop - trainPosition[1];
                zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            }
            else if (validInfo.direction == 1)
            {
                zeroObjectPointTop = startObjectArea;
                zeroObjectPointMiddle = zeroObjectPointTop - trainPosition[1];
                zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            }
            else
            {
                zeroObjectPointMiddle = startObjectArea + (endObjectArea - startObjectArea) / 2;
                zeroObjectPointTop = zeroObjectPointMiddle + trainPosition[1];
                zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            }
            var distanceFactor = (float)(endObjectArea - startObjectArea - trainPosition[4]) / TrackMonitorWindow.MaximumDistance;
            if (validInfo.direction == -1)
                distanceFactor /= 2;

            if (validInfo.direction == 0)
            {
                // draw direction arrow
                drawArrow(spriteBatch, offset, forwardArrowSprite, zeroObjectPointMiddle + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                // draw direction arrow
                drawArrow(spriteBatch, offset, backwardArrowSprite, zeroObjectPointMiddle + arrowPosition[2]);
            }

            if (validInfo.direction != 1)
            {
                // draw fixed distance indications
                var firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointTop, 4, true);
                var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

                // draw forward items
                drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointTop, zeroObjectPointBottom, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoForward, true);
            }

            if (validInfo.direction != 0)
            {
                // draw fixed distance indications
                var firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointBottom, 4, false);
                var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

                // draw backward items
                drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointBottom, zeroObjectPointTop, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoBackward, false);
            }

            // draw own train marker
            drawOwnTrain(spriteBatch, offset, validInfo.direction == -1 ? trainPositionManualOnRouteSprite : validInfo.direction == 0 ? trainPositionAutoForwardsSprite : trainPositionAutoBackwardsSprite, zeroObjectPointTop);
        }

        // draw manual info
        // all details accessed through class variables

        void drawManualInfo(SpriteBatch spriteBatch, Point offset)
        {
            // set area details
            var startObjectArea = additionalInfoHeight;
            var endObjectArea = Position.Height - additionalInfoHeight;
            var zeroObjectPointMiddle = startObjectArea + (endObjectArea - startObjectArea) / 2;
            var zeroObjectPointTop = zeroObjectPointMiddle + trainPosition[1];
            var zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            var distanceFactor = (float)(zeroObjectPointTop - startObjectArea) / TrackMonitorWindow.MaximumDistance;

            // draw lines through own train
            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X, offset.Y + zeroObjectPointTop, Position.Width, 1), Color.DarkGray);
            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X, offset.Y + zeroObjectPointBottom - 1, Position.Width, 1), Color.DarkGray);

            // draw direction arrow
            if (validInfo.direction == 0)
            {
                drawArrow(spriteBatch, offset, forwardArrowSprite, zeroObjectPointMiddle + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                drawArrow(spriteBatch, offset, backwardArrowSprite, zeroObjectPointMiddle + arrowPosition[2]);
            }

            // draw eye
            drawEye(spriteBatch, offset, 0, Position.Height);

            // draw fixed distance indications
            var firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointTop, 3, true);
            drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointBottom, 3, false);  // no return required
            var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

            // draw forward items
            drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointTop, zeroObjectPointBottom, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoForward, true);

            // draw backward items
            drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointBottom, zeroObjectPointTop, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoBackward, false);

            // draw own train marker
            var ownTrainSprite = validInfo.isOnPath ? trainPositionManualOnRouteSprite : trainPositionManualOffRouteSprite;
            drawOwnTrain(spriteBatch, offset, ownTrainSprite, zeroObjectPointTop);
        }

        // draw own train marker at required position
        void drawOwnTrain(SpriteBatch spriteBatch, Point offset, Rectangle sprite, int position)
        {
            spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + trainPosition[0], offset.Y + position, trainPosition[3], trainPosition[4]), sprite, Color.White);
        }

        // draw arrow at required position
        void drawArrow(SpriteBatch spriteBatch, Point offset, Rectangle sprite, int position)
        {
            spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + arrowPosition[0], offset.Y + position, arrowPosition[3], arrowPosition[4]), sprite, Color.White);
        }

        // draw eye at required position
        void drawEye(SpriteBatch spriteBatch, Point offset, int forwardsY, int backwardsY)
        {
            // draw eye
            if (validInfo.cabOrientation == 0)
            {
                spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + eyePosition[0], offset.Y + forwardsY + eyePosition[1], eyePosition[3], eyePosition[4]), eyeSprite, Color.White);
            }
            else
            {
                spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + eyePosition[0], offset.Y + backwardsY + eyePosition[2], eyePosition[3], eyePosition[4]), eyeSprite, Color.White);
            }
        }

        // draw fixed distance markers
        float drawDistanceMarkers(SpriteBatch spriteBatch, Point offset, float maxDistance, float distanceFactor, int zeroPoint, int numberOfMarkers, bool forward)
        {
            var maxDistanceD = Me.FromM(maxDistance, metric); // in displayed units
            var markerIntervalD = maxDistanceD / numberOfMarkers;

            var roundingValue = roundingValues[0];
            foreach (var thisValue in roundingValues)
            {
                if (markerIntervalD > thisValue.Key)
                {
                    roundingValue = thisValue.Value;
                }
            }

            markerIntervalD = Convert.ToInt32(markerIntervalD / roundingValue) * roundingValue;
            var markerIntervalM = Me.ToM(markerIntervalD, metric);  // from display back to metre

            for (var ipos = 1; ipos <= numberOfMarkers; ipos++)
            {
                var actDistanceM = markerIntervalM * ipos;
                if (actDistanceM < maxDistance)
                {
                    var itemOffset = Convert.ToInt32(actDistanceM * distanceFactor);
                    var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                    var distanceString = FormatStrings.FormatDistanceDisplay(actDistanceM, metric);
                    Font.Draw(spriteBatch, new Point(offset.X + distanceTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]), distanceString, Color.White);
                }
            }

            return markerIntervalM;
        }

        // draw signal, speed and authority items
        // items are sorted in order of increasing distance

        void drawItems(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, int lastLabelPosition, float maxDistance, float distanceFactor, int firstLabelPosition, List<Train.TrainObjectItem> itemList, bool forward)
        {
            var signalShown = false;
            var firstLabelShown = false;
            var borderSignalShown = false;

            foreach (var thisItem in itemList)
            {
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY:
                        drawAuthority(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL:
                        lastLabelPosition = drawSignalForward(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref signalShown, ref borderSignalShown, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SPEED_SIGNAL:
                        lastLabelPosition = drawSpeedpost(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST:
                        lastLabelPosition = drawSpeedpost(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.STATION:
                        drawStation(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.WAITING_POINT:
                        drawWaitingPoint(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.MILEPOST:
                        lastLabelPosition = drawMilePost(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.FACING_SWITCH:
                        drawSwitch(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.REVERSAL:
                        drawReversal(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    default:     // capture unkown item
                        break;
                }
            }
            //drawReversal and drawSwitch icons on top.
            foreach (var thisItem in itemList)
            {
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.FACING_SWITCH:
                        drawSwitch(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.REVERSAL:
                        drawReversal(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    default:
                        break;
                }
            }
            // reverse display of signals to have correct superposition
            for (int iItems = itemList.Count-1 ; iItems >=0; iItems--)
            {
                var thisItem = itemList[iItems];
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL:
                        drawSignalBackward(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, forward, thisItem, signalShown);
                        break;

                    default:
                        break;
                }
            }
        }

        // draw authority information
        void drawAuthority(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = new Rectangle(0, 0, 0, 0);
            var displayRequired = false;
            var offsetArray = new int[0];

            if (thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_AUTHORITY ||
                thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_PATH ||
                thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_TRACK ||
                thisItem.AuthorityType == Train.END_AUTHORITY.RESERVED_SWITCH ||
                thisItem.AuthorityType == Train.END_AUTHORITY.LOOP)
            {
                displayItem = endAuthoritySprite;
                offsetArray = endAuthorityPosition;
                displayRequired = true;
            }
            else if (thisItem.AuthorityType == Train.END_AUTHORITY.TRAIN_AHEAD)
            {
                displayItem = forward ? oppositeTrainForwardSprite : oppositeTrainBackwardSprite;
                offsetArray = otherTrainPosition;
                displayRequired = true;
            }

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor) && displayRequired)
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + offsetArray[0], offset.Y + itemLocation + offsetArray[forward ? 1 : 2], offsetArray[3], offsetArray[4]), displayItem, Color.White);

                if (itemOffset < firstLabelPosition && !firstLabelShown)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }
        }

        // check signal information for reverse display
        int drawSignalForward(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool signalShown, ref bool borderSignalShown, ref bool firstLabelShown)
        {
            var displayItem = SignalMarkers[thisItem.SignalState];
            var newLabelPosition = lastLabelPosition;

            var displayRequired = false;
            var itemLocation = 0;
            var itemOffset = 0;
            var maxDisplayDistance = maxDistance - (textSpacing / 2) / distanceFactor;

            if (thisItem.DistanceToTrainM < maxDisplayDistance)
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                displayRequired = true;
                signalShown = true;
            }
            else if (!borderSignalShown && !signalShown)
            {
                itemOffset = 2 * startObjectArea;
                itemLocation = forward ? startObjectArea : endObjectArea;
                displayRequired = true;
                borderSignalShown = true;
            }

            bool showSpeeds;
            switch (Mode)
            {
                case DisplayMode.All:
                default:
                    showSpeeds = true;
                    break;
                case DisplayMode.StaticOnly:
                    showSpeeds = false;
                    break;
            }

            if (displayRequired)
            {
                if (showSpeeds && thisItem.SignalState != TrackMonitorSignalAspect.Stop && thisItem.AllowedSpeedMpS > 0)
                {
                    var labelPoint = new Point(offset.X + speedTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]);
                    var speedString = FormatStrings.FormatSpeedLimitNoUoM(thisItem.AllowedSpeedMpS, metric);
                    Font.Draw(spriteBatch, labelPoint, speedString, Color.White);
                }

                if ((itemOffset < firstLabelPosition && !firstLabelShown) || thisItem.DistanceToTrainM > maxDisplayDistance)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }

        // draw signal information
        void drawSignalBackward(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, bool forward, Train.TrainObjectItem thisItem, bool signalShown)
        {
            TrackMonitorSignalAspect aspect;
            switch (Mode)
            {
                case DisplayMode.All:
                default:
                    aspect = thisItem.SignalState;
                    break;
                case DisplayMode.StaticOnly:
                    aspect = TrackMonitorSignalAspect.None;
                    break;
            }
            var displayItem = SignalMarkers[aspect];
 
            var displayRequired = false;
            var itemLocation = 0;
            var itemOffset = 0;
            var maxDisplayDistance = maxDistance - (textSpacing / 2) / distanceFactor;

            if (thisItem.DistanceToTrainM < maxDisplayDistance)
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                displayRequired = true;
            }
            else if (!signalShown)
            {
                itemOffset = 2 * startObjectArea;
                itemLocation = forward ? startObjectArea : endObjectArea;
                displayRequired = true;
            }

            if (displayRequired)
            {
                spriteBatch.Draw(SignalAspects, new Rectangle(offset.X + signalPosition[0], offset.Y + itemLocation + signalPosition[forward ? 1 : 2], signalPosition[3], signalPosition[4]), displayItem, Color.White);
            }

        }

        // draw speedpost information
        int drawSpeedpost(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                var allowedSpeed = thisItem.AllowedSpeedMpS;
                if (allowedSpeed > 998)
                {
                    if (!Program.Simulator.TimetableMode)
                    {
                        allowedSpeed = (float)Program.Simulator.TRK.Tr_RouteFile.SpeedLimit;
                    }
                }

                var labelPoint = new Point(offset.X + speedTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                var speedString = FormatStrings.FormatSpeedLimitNoUoM(allowedSpeed, metric);
                Font.Draw(spriteBatch, labelPoint, speedString, thisItem.SpeedObjectType == Train.TrainObjectItem.SpeedItemType.Standard ? (thisItem.IsWarning ? Color.Yellow : Color.White) :
                    (thisItem.SpeedObjectType == Train.TrainObjectItem.SpeedItemType.TempRestrictedStart ? Color.Red : Color.LightGreen));

                if (itemOffset < firstLabelPosition && !firstLabelShown)
                {
                    labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }


        // draw station stop information
        int drawStation(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem)
        {
            var displayItem = stationSprite;
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                var startOfPlatform = (int)Math.Max(stationPosition[4], thisItem.StationPlatformLength * distanceFactor);
                var markerPlacement = new Rectangle(offset.X + stationPosition[0], offset.Y + itemLocation + stationPosition[forward ? 1 : 2], stationPosition[3], startOfPlatform);
                spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);
            }

            return newLabelPosition;
        }

        // draw reversal information
        int drawReversal(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = thisItem.Valid ? reversalSprite : invalidReversalSprite;
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                // What was this offset all about? Shouldn't we draw the icons in the correct location ALL the time? -- James Ross
                // var correctingOffset = Program.Simulator.TimetableMode || !Program.Simulator.Settings.EnhancedActCompatibility ? 0 : 7;

                if (thisItem.Valid)
                {
                    var markerPlacement = new Rectangle(offset.X + reversalPosition[0], offset.Y + itemLocation + reversalPosition[forward ? 1 : 2], reversalPosition[3], reversalPosition[4]);
                    spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, thisItem.Enabled ? Color.LightGreen : Color.White);
                }
                else
                {
                    var markerPlacement = new Rectangle(offset.X + invalidReversalPosition[0], offset.Y + itemLocation + invalidReversalPosition[forward ? 1 : 2], invalidReversalPosition[3], invalidReversalPosition[4]);
                    spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);
                }

                // Only show distance for enhanced MSTS compatibility (this is the only time the position is controlled by the author).
                if (itemOffset < firstLabelDistance && !firstLabelShown && !Program.Simulator.TimetableMode)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }

        // draw waiting point information
        int drawWaitingPoint(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = waitingPointSprite;
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                var markerPlacement = new Rectangle(offset.X + waitingPointPosition[0], offset.Y + itemLocation + waitingPointPosition[forward ? 1 : 2], waitingPointPosition[3], waitingPointPosition[4]);
                spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, thisItem.Enabled ? Color.Yellow : Color.Red);

                if (itemOffset < firstLabelDistance && !firstLabelShown)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }

        // draw milepost information
        int drawMilePost(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);
                var labelPoint = new Point(offset.X + milepostTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                var milepostString = thisItem.ThisMile;
                Font.Draw(spriteBatch, labelPoint, milepostString, Color.White);
            }

            return newLabelPosition;
        }

        // draw switch information
        int drawSwitch(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = thisItem.IsRightSwitch ? rightArrowSprite : leftArrowSprite;
            var newLabelPosition = lastLabelPosition;

            bool showSwitches;
            switch (Mode)
            {
                case DisplayMode.All:
                default:
                    showSwitches = true;
                    break;
                case DisplayMode.StaticOnly:
                    showSwitches = false;
                    break;
            }

            if (showSwitches && thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                var markerPlacement = thisItem.IsRightSwitch ?
                    new Rectangle(offset.X + rightSwitchPosition[0], offset.Y + itemLocation + rightSwitchPosition[forward ? 1 : 2], rightSwitchPosition[3], rightSwitchPosition[4]) :
                    new Rectangle(offset.X + leftSwitchPosition[0], offset.Y + itemLocation + leftSwitchPosition[forward ? 1 : 2], leftSwitchPosition[3], leftSwitchPosition[4]);
                spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);

                // Only show distance for enhanced MSTS compatibility (this is the only time the position is controlled by the author).
                if (itemOffset < firstLabelDistance && !firstLabelShown && !Program.Simulator.TimetableMode)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }


    }
}
