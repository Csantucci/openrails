﻿// COPYRIGHT 2014 by the Open Rails project.
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

// This code processes the Timetable definition and converts it into playable train information
//

// Set debug flag to extract additional info
// Info is printed to C:\temp\timetableproc.txt
// #define DEBUG_TIMETABLE
// #define DEBUG_TRACEINFO

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.Parsers.OR;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.Signalling;
using ORTS.Common;
using System.Text;
using Event = Orts.Common.Event;

namespace Orts.Simulation.Timetables
{
    public class TimetableInfo
    {
        private readonly Simulator simulator;

        private enum columnType
        {
            stationInfo,
            addStationInfo,
            comment,
            trainDefinition,
            trainAddInfo,
            invalid,
        }

        private enum rowType
        {
            trainInfo,
            stationInfo,
            addStationInfo,
            consistInfo,
            pathInfo,
            startInfo,
            disposeInfo,
            directionInfo,
            trainNotesInfo,
            restartDelayInfo,
            speedInfo,
            lightInfo,
            comment,
            briefing,
            invalid,
        }

        Dictionary<string, AIPath> Paths = new Dictionary<string, AIPath>();             // Original path referenced by path name
        readonly List<string> reportedPaths = new List<string>();                        // Reported path fails
        readonly Dictionary<int, string> TrainRouteXRef = new Dictionary<int, string>(); // Path name referenced from train index    

        public bool BinaryPaths = false;

        public static int? PlayerTrainOriginalStartTime; // Set by TimetableInfo.ProcessTimetable() and read by AI.PrerunAI()

        //================================================================================================//
        /// <summary>
        ///  Constructor - empty constructor
        /// </summary>
        public TimetableInfo(Simulator simulatorref)
        {
            simulator = simulatorref;
        }

        //================================================================================================//
        /// <summary>
        /// Process timetable file
        /// Convert info into list of trains
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>List of extracted Trains</returns>
        public List<TTTrain> ProcessTimetable(string[] arguments, CancellationToken cancellation)
        {
            TTTrain reqPlayerTrain;

            bool loadPathNoFailure = true;
            List<TTTrain> trainList = new List<TTTrain>();
            List<TTTrainInfo> trainInfoList = new List<TTTrainInfo>();
            TTTrainInfo playerTrain = null;
            List<string> filenames;
            int indexcount = 0;

            // Get filenames to process
            filenames = GetFilenames(arguments[0]);

            // Get file contents as strings
            Trace.Write("\n");
            foreach (string filePath in filenames)
            {
                // Get contents as strings
                Trace.Write("TT File : " + filePath + "\n");
                var fileContents = new TimetableReader(filePath);

#if DEBUG_TIMETABLE
                File.AppendAllText(@"C:\temp\timetableproc.txt", "\nProcessing file : " + filePath + "\n");
#endif

                // Convert to train info
                indexcount = ConvertFileContents(fileContents, simulator.Signals, ref trainInfoList, indexcount, filePath);
            }

            // Read and pre-process routes
            Trace.Write(" TTROUTES:" + Paths.Count.ToString() + " ");
            loadPathNoFailure = PreProcessRoutes(cancellation);
            Trace.Write(" TTTRAINS:" + trainInfoList.Count.ToString() + " ");

            // Get startinfo for player train
            playerTrain = GetPlayerTrain(ref trainInfoList, arguments);

            // pre-init player train to abstract alternative paths if set
            if (playerTrain != null)
            {
                PreInitPlayerTrain(playerTrain);
            }

            // create AI trainlist
            bool addPathNoLoadFailure;
            trainList = BuildAITrains(cancellation, trainInfoList, playerTrain, arguments, out addPathNoLoadFailure);
            if (!addPathNoLoadFailure) loadPathNoFailure = false;

            // Set references (required to process commands)
            foreach (Train thisTrain in trainList)
            {
                if (simulator.NameDictionary.ContainsKey(thisTrain.Name.ToLower()))
                {
                    Trace.TraceWarning("Train : " + thisTrain.Name + " : duplicate name");
                }
                else
                {
                    simulator.TrainDictionary.Add(thisTrain.Number, thisTrain);
                    simulator.NameDictionary.Add(thisTrain.Name.ToLower(), thisTrain);
                }
            }

            // Set player train
            reqPlayerTrain = null;
            if (playerTrain != null)
            {
                if (playerTrain.DisposeDetails != null)
                {
                    addPathNoLoadFailure = playerTrain.ProcessDisposeInfo(ref trainList, null, simulator);
                    if (!addPathNoLoadFailure) loadPathNoFailure = false;
                }
                PlayerTrainOriginalStartTime = playerTrain.StartTime; // Saved here for use after `playerTrain.StartTime` gets changed.
                reqPlayerTrain = InitializePlayerTrain(playerTrain, ref Paths, ref trainList);
                simulator.TrainDictionary.Add(reqPlayerTrain.Number, reqPlayerTrain);
                simulator.NameDictionary.Add(reqPlayerTrain.Name.ToLower(), reqPlayerTrain);
            }

            // Process additional commands for all extracted trains
            reqPlayerTrain.FinalizeTimetableCommands();
            reqPlayerTrain.StationStops.Sort();

            foreach (TTTrain thisTrain in trainList)
            {
                thisTrain.FinalizeTimetableCommands();
                thisTrain.StationStops.Sort();

                // Finalize attach details
                if (thisTrain.AttachDetails != null && thisTrain.AttachDetails.Valid)
                {
                    thisTrain.AttachDetails.FinalizeAttachDetails(thisTrain, trainList, playerTrain.thisTTTrain);
                }

                // Finalize pickup details
                if (thisTrain.PickUpDetails != null && thisTrain.PickUpDetails.Count > 0)
                {
                    foreach (PickUpInfo thisPickUp in thisTrain.PickUpDetails)
                    {
                        thisPickUp.FinalizePickUpDetails(thisTrain, trainList, playerTrain.thisTTTrain);
                    }
                    thisTrain.PickUpDetails.Clear();
                }

                // Finalize transfer details
                if (thisTrain.TransferStationDetails != null && thisTrain.TransferStationDetails.Count > 0)
                {
                    foreach (KeyValuePair<int, TransferInfo> thisTransferStation in thisTrain.TransferStationDetails)
                    {
                        TransferInfo thisTransfer = thisTransferStation.Value;
                        thisTransfer.SetTransferXRef(thisTrain, trainList, playerTrain.thisTTTrain, true, false);
                    }
                }

                if (thisTrain.TransferTrainDetails != null && thisTrain.TransferTrainDetails.ContainsKey(-1))
                {
                    foreach (TransferInfo thisTransfer in thisTrain.TransferTrainDetails[-1])
                    {
                        thisTransfer.SetTransferXRef(thisTrain, trainList, playerTrain.thisTTTrain, false, true);
                        if (thisTransfer.Valid)
                        {
                            if (thisTrain.TransferTrainDetails.ContainsKey(thisTransfer.TransferTrain))
                            {
                                Trace.TraceInformation("Train {0} : transfer command : cannot transfer to same train twice : {1}", thisTrain.Name, thisTransfer.TransferTrainName);
                            }
                            else
                            {
                                List<TransferInfo> thisTransferList = new List<TransferInfo>
                                {
                                    thisTransfer
                                };
                                thisTrain.TransferTrainDetails.Add(thisTransfer.TransferTrain, thisTransferList);
                            }
                        }
                    }

                    thisTrain.TransferTrainDetails.Remove(-1);
                }
            }

            // Process activation commands for all trains
            FinalizeActivationCommands(ref trainList, ref reqPlayerTrain);

            // Set timetable identification for simulator for saves etc.
            simulator.TimetableFileName = Path.GetFileNameWithoutExtension(arguments[0]);

            if (!loadPathNoFailure)
            {
                Trace.TraceError("Load path failures");
            }

            // Check on engine in player train
            if (simulator.PlayerLocomotive == null)
            {
                if (reqPlayerTrain.NeedAttach != null && reqPlayerTrain.NeedAttach.Count > 0)
                {
                    Trace.TraceInformation("Player trains " + reqPlayerTrain.Name + " defined without engine, engine assumed to be attached later");
                }
                else if (reqPlayerTrain.FormedOf >= 0)
                {
                    Trace.TraceInformation("Player trains " + reqPlayerTrain.Name + " defined without engine, train is assumed to be formed out of other train");
                }
                else
                {
                    throw new InvalidDataException("Can't find player locomotive in " + reqPlayerTrain.Name);
                }
            }

            // set power states on start for all trains

            reqPlayerTrain.SetPowerStateOnStart();
            foreach (TTTrain thisTrain in trainList)
            {
                thisTrain.SetPowerStateOnStart();
            }

            trainList.Insert(0, reqPlayerTrain);
            return trainList;
        }

        //================================================================================================//
        /// <summary>
        /// Get filenames of TTfiles to process
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<string> GetFilenames(string filePath)
        {
            List<string> filenames = new List<string>();

            // Check type of timetable file - list or single
            string fileExtension = Path.GetExtension(filePath);
            string fileDirectory = Path.GetDirectoryName(filePath);

            switch (fileExtension)
            {
                case ".timetable_or":
                case ".timetable-or":
                    filenames.Add(filePath);
                    break;

                case ".timetablelist_or":
                case ".timetablelist-or":
                    TimetableGroupFile multiInfo = new TimetableGroupFile(filePath, fileDirectory);
                    filenames = multiInfo.TTFiles;
                    break;

                default:
                    break;
            }

#if DEBUG_TIMETABLE
            if (File.Exists(@"C:\temp\timetableproc.txt"))
            {
                File.Delete(@"C:\temp\timetableproc.txt");
            }

            File.AppendAllText(@"C:\temp\timetableproc.txt", "Files : \n");
            foreach (string ttfile in filenames)
            {
                File.AppendAllText(@"C:\temp\timetableproc.txt", ttfile + "\n");
            }
#endif
            return filenames;
        }

        //================================================================================================//
        /// <summary>
        /// Extract information and convert to traininfo
        /// </summary>
        /// <param name="fileContents"></param>
        /// <param name="signalRef"></param>
        /// <param name="TDB"></param>
        /// <param name="trainInfoList"></param>
        private int ConvertFileContents(TimetableReader fileContents, Signals signalRef, ref List<TTTrainInfo> trainInfoList, int indexcount, string filePath)
        {
            int consistRow = -1;
            int pathRow = -1;
            int startRow = -1;
            int disposeRow = -1;
            int briefingRow = -1;

            int firstCommentRow = -1;
            int firstCommentColumn = -1;

            Dictionary<int, string> trainHeaders = new Dictionary<int, string>();           // Key int = column no, value string = train header
            Dictionary<int, TTTrainInfo> trainInfo = new Dictionary<int, TTTrainInfo>();    // Key int = column no, value = train info class
            Dictionary<int, int> addTrainInfo = new Dictionary<int, int>();                 // Key int = column no, value int = main train column
            Dictionary<int, List<int>> addTrainColumns = new Dictionary<int, List<int>>();  // Key int = main train column, value = add columns
            Dictionary<int, StationInfo> stationNames = new Dictionary<int, StationInfo>(); // Key int = row no, value string = station name

            float actSpeedConv = 1.0f; // Actual set speedconversion

            rowType[] RowInfo = new rowType[fileContents.Strings.Count];
            columnType[] ColInfo = new columnType[fileContents.Strings[0].Length];

            // Process first row separately
            ColInfo[0] = columnType.stationInfo;

            for (int iColumn = 1; iColumn <= fileContents.Strings[0].Length - 1; iColumn++)
            {
                string columnDef = fileContents.Strings[0][iColumn];

                if (String.IsNullOrEmpty(columnDef))
                {
                    // Empty: continuation column
                    switch (ColInfo[iColumn - 1])
                    {
                        case columnType.stationInfo:
                        case columnType.addStationInfo:
                            ColInfo[iColumn] = columnType.addStationInfo;
                            break;

                        case columnType.comment:
                            ColInfo[iColumn] = columnType.comment;
                            break;

                        case columnType.trainDefinition:
                            ColInfo[iColumn] = columnType.trainAddInfo;
                            addTrainInfo.Add(iColumn, iColumn - 1);
                            break;

                        case columnType.trainAddInfo:
                            ColInfo[iColumn] = columnType.trainAddInfo;
                            addTrainInfo.Add(iColumn, addTrainInfo[iColumn - 1]);
                            break;
                    }
                }
                else if (String.Compare(columnDef, "#comment", true) == 0)
                {
                    // Comment
                    ColInfo[iColumn] = columnType.comment;
                    if (firstCommentColumn < 0) firstCommentColumn = iColumn;
                }

                else if (columnDef.Substring(0, 1).Equals("#"))
                {
                    // Check for invalid command definition
                    Trace.TraceWarning("Invalid column definition in {0} : column {1} : {2}", filePath, iColumn, columnDef);
                    ColInfo[iColumn] = columnType.invalid;
                }
                else
                {
                    // Otherwise it is a train definition
                    ColInfo[iColumn] = columnType.trainDefinition;
                    trainHeaders.Add(iColumn, String.Copy(columnDef));
                    trainInfo.Add(iColumn, new TTTrainInfo(iColumn, columnDef, simulator, indexcount, this));
                    indexcount++;
                }
            }

            // Get row information
            RowInfo[0] = rowType.trainInfo;

            for (int iRow = 1; iRow <= fileContents.Strings.Count - 1; iRow++)
            {

                string rowDef = fileContents.Strings[iRow][0].ToLower();

                string[] rowCommands = null;
                if (rowDef.Contains('/'))
                {
                    rowCommands = rowDef.Split('/');
                    rowDef = rowCommands[0].Trim().ToLower();
                }

                if (String.IsNullOrEmpty(rowDef))
                {
                    // Emtpy: continuation
                    switch (RowInfo[iRow - 1])
                    {
                        case rowType.stationInfo:
                            RowInfo[iRow] = rowType.addStationInfo;
                            break;

                        default: // Continuation of other types not allowed, treat line as comment
                            RowInfo[iRow] = rowType.comment;
                            break;
                    }
                }
                else
                {
                    // switch on actual string
                    switch (rowDef)
                    {
                        case "#consist":
                            RowInfo[iRow] = rowType.consistInfo;
                            consistRow = iRow;
                            break;

                        case "#path":
                            RowInfo[iRow] = rowType.pathInfo;
                            pathRow = iRow;
                            if (rowCommands != null && rowCommands.Length >= 2 && String.Equals(rowCommands[1], "binary")) BinaryPaths = true;
                            break;

                        case "#start":
                            RowInfo[iRow] = rowType.startInfo;
                            startRow = iRow;
                            break;

                        case "#dispose":
                            RowInfo[iRow] = rowType.disposeInfo;
                            disposeRow = iRow;
                            break;

                        case "#direction":
                            RowInfo[iRow] = rowType.directionInfo;
                            break;

                        case "#note":
                            RowInfo[iRow] = rowType.trainNotesInfo;
                            break;

                        case "#lights":
                            RowInfo[iRow] = rowType.lightInfo;
                            break;

                        case "#restartdelay":
                            RowInfo[iRow] = rowType.restartDelayInfo;
                            break;

                        case "#speed":
                            bool speeddef = false;
                            for (int rowIndex = 0; rowIndex < iRow; rowIndex++)
                            {
                                if (RowInfo[rowIndex] == rowType.speedInfo)
                                {
                                    Trace.TraceInformation("Multiple speed row definition - second entry ignored \n");
                                    speeddef = true;
                                    break;
                                }
                            }
                            if (!speeddef)
                            {
                                RowInfo[iRow] = rowType.speedInfo;
                                actSpeedConv = 1.0f;
                            }
                            break;
                        case "#speedmph":
                            speeddef = false;
                            for (int rowIndex = 0; rowIndex < iRow; rowIndex++)
                            {
                                if (RowInfo[rowIndex] == rowType.speedInfo)
                                {
                                    Trace.TraceInformation("Multiple speed row definition - second entry ignored \n");
                                    speeddef = true;
                                    break;
                                }
                            }
                            if (!speeddef)
                            {
                                RowInfo[iRow] = rowType.speedInfo;
                                actSpeedConv = MpS.FromMpH(1.0f);
                            }
                            break;
                        case "#speedkph":
                            speeddef = false;
                            for (int rowIndex = 0; rowIndex < iRow; rowIndex++)
                            {
                                if (RowInfo[rowIndex] == rowType.speedInfo)
                                {
                                    Trace.TraceInformation("Multiple speed row definition - second entry ignored \n");
                                    speeddef = true;
                                    break;
                                }
                            }
                            if (!speeddef)
                            {
                                RowInfo[iRow] = rowType.speedInfo;
                                actSpeedConv = MpS.FromKpH(1.0f);
                            }
                            break;

                        case "#comment":
                            RowInfo[iRow] = rowType.comment;
                            if (firstCommentRow < 0) firstCommentRow = iRow;
                            break;

                        case "#briefing":
                            RowInfo[iRow] = rowType.briefing;
                            briefingRow = iRow;
                            break;

                        default:  // default is station definition
                            if (rowDef.Substring(0, 1).Equals("#"))
                            {
                                Trace.TraceWarning("Invalid row definition in {0} : {1}", filePath, rowDef);
                                RowInfo[iRow] = rowType.invalid;
                            }
                            else
                            {
                                RowInfo[iRow] = rowType.stationInfo;
                                stationNames.Add(iRow, new StationInfo(String.Copy(rowDef)));
                            }
                            break;
                    }
                }
            }

#if DEBUG_TIMETABLE
            File.AppendAllText(@"C:\temp\timetableproc.txt", "\n Row and Column details : \n");

            File.AppendAllText(@"C:\temp\timetableproc.txt", "\n Columns : \n");
            for (int iColumn = 0; iColumn <= ColInfo.Length - 1; iColumn++)
            {
                columnType ctype = ColInfo[iColumn];

                var stbuild = new StringBuilder();
                stbuild.AppendFormat("Column : {0} = {1}", iColumn, ctype.ToString());
                if (ctype == columnType.trainDefinition)
                {
                    stbuild.AppendFormat(" = train : {0}", trainHeaders[iColumn]);
                }
                stbuild.Append("\n");
                File.AppendAllText(@"C:\temp\timetableproc.txt", stbuild.ToString());
            }

            File.AppendAllText(@"C:\temp\timetableproc.txt", "\n Rows : \n");
            for (int iRow = 0; iRow <= RowInfo.Length - 1; iRow++)
            {
                rowType rtype = RowInfo[iRow];

                var stbuild = new StringBuilder();
                stbuild.AppendFormat("Row    : {0} = {1}", iRow, rtype.ToString());
                if (rtype == rowType.stationInfo)
                {
                    stbuild.AppendFormat(" = station {0}", stationNames[iRow]);
                }
                stbuild.Append("\n");
                File.AppendAllText(@"C:\temp\timetableproc.txt", stbuild.ToString());
            }
#endif

            bool validFile = true;

            // Check if all required row definitions are available
            if (consistRow < 0)
            {
                Trace.TraceWarning("File : {0} - Consist definition row missing, file cannot be processed", filePath);
                validFile = false;
            }

            if (pathRow < 0)
            {
                Trace.TraceWarning("File : {0} - Path definition row missing, file cannot be processed", filePath);
                validFile = false;
            }

            if (startRow < 0)
            {
                Trace.TraceWarning("File : {0} - Start definition row missing, file cannot be processed", filePath);
                validFile = false;
            }

            if (!validFile) return indexcount; // Abandon processing

            // Extract description
            string description = (firstCommentRow >= 0 && firstCommentColumn >= 0) ?
                fileContents.Strings[firstCommentRow][firstCommentColumn] : Path.GetFileNameWithoutExtension(fileContents.FilePath);

            // Extract additional station info
            for (int iRow = 1; iRow <= fileContents.Strings.Count - 1; iRow++)
            {
                if (RowInfo[iRow] == rowType.stationInfo)
                {
                    string[] columnStrings = fileContents.Strings[iRow];
                    for (int iColumn = 1; iColumn <= ColInfo.Length - 1; iColumn++)
                    {
                        if (ColInfo[iColumn] == columnType.addStationInfo)
                        {
                            string[] stationCommands = columnStrings[iColumn].Split('$');
                            stationNames[iRow].ProcessStationCommands(stationCommands);
                        }
                    }
                }
            }

            // Build list of additional train columns
            foreach (KeyValuePair<int, int> addColumn in addTrainInfo)
            {
                if (addTrainColumns.ContainsKey(addColumn.Value))
                {
                    addTrainColumns[addColumn.Value].Add(addColumn.Key);
                }
                else
                {
                    List<int> addTrainColumn = new List<int>
                    {
                        addColumn.Key
                    };
                    addTrainColumns.Add(addColumn.Value, addTrainColumn);
                }
            }

            // Build actual trains
            // set flag not to switch on diesel engines to prevent diesel engine being switch on by system by default
            simulator.Settings.NoDieselEngineStart = true;

            bool allCorrectBuild = true;

            for (int iColumn = 1; iColumn <= ColInfo.Length - 1; iColumn++)
            {
                if (ColInfo[iColumn] == columnType.trainDefinition)
                {
                    List<int> addColumns = null;
                    addTrainColumns.TryGetValue(iColumn, out addColumns);

                    if (addColumns != null)
                    {
                        ConcatTrainStrings(fileContents.Strings, iColumn, addColumns);
                    }

                    if (!trainInfo[iColumn].BuildTrain(fileContents.Strings, RowInfo, pathRow, consistRow, startRow, disposeRow, briefingRow, description, stationNames, actSpeedConv, this))
                    {
                        allCorrectBuild = false;
                    }
                }
            }

            if (!allCorrectBuild)
            {
                Trace.TraceError("Failed to build trains");
            }

            // Extract valid trains
            foreach (KeyValuePair<int, TTTrainInfo> train in trainInfo)
            {
                if (train.Value.validTrain)
                {
                    trainInfoList.Add(train.Value);
                }
            }

            return indexcount;
        }

        //================================================================================================//
        /// <summary>
        /// Concatinate train string with info from additional columns
        /// </summary>
        /// <param name="fileStrings"></param>
        /// <param name="iColumn"></param>
        /// <param name="addColumns"></param>
        private void ConcatTrainStrings(List<string[]> fileStrings, int iColumn, List<int> addColumns)
        {
            for (int iRow = 1; iRow < fileStrings.Count - 1; iRow++)
            {
                string[] columnStrings = fileStrings[iRow];
                foreach (int addCol in addColumns)
                {
                    string addCols = columnStrings[addCol];
                    if (!String.IsNullOrEmpty(addCols))
                    {
                        columnStrings[iColumn] = String.Concat(columnStrings[iColumn], " ", addCols);
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// GetPlayerTrain : extract player train from list of all available trains
        /// </summary>
        /// <param name="allTrains"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private TTTrainInfo GetPlayerTrain(ref List<TTTrainInfo> allTrains, string[] arguments)
        {
            TTTrainInfo reqTrain = null;

            string[] playerTrainDetails = arguments[1].Split(':');

            // loop through all trains to find player train
            int playerIndex = -1;

            for (int iTrain = 0; iTrain <= allTrains.Count - 1 && playerIndex < 0; iTrain++)
            {
                if (String.Compare(allTrains[iTrain].Name, playerTrainDetails[1]) == 0 &&
                    String.Compare(allTrains[iTrain].TTDescription, playerTrainDetails[0]) == 0)
                {
                    playerIndex = iTrain;
                }
            }

            if (playerIndex >= 0)
            {
                reqTrain = allTrains[playerIndex];
                allTrains.RemoveAt(playerIndex);
            }
            else
            {
                throw new InvalidDataException("Player train : " + arguments[1] + " not found in timetables");
            }

            return reqTrain;
        }

        //================================================================================================//
        /// <summary>
        /// Build AI trains
        /// </summary>
        /// <param name="allTrains"></param>
        /// <param name="playerTrain"></param>
        /// <param name="arguments"></param>
        private List<TTTrain> BuildAITrains(CancellationToken cancellation, List<TTTrainInfo> allTrains, TTTrainInfo playerTrain, string[] arguments, out bool allPathsLoaded)
        {
            allPathsLoaded = true;
            List<TTTrain> trainList = new List<TTTrain>();

            foreach (TTTrainInfo reqTrain in allTrains)
            {
                if (cancellation.IsCancellationRequested) continue; // Ping watchdog token

                // Create train route
                if (TrainRouteXRef.ContainsKey(reqTrain.Index) && Paths.ContainsKey(TrainRouteXRef[reqTrain.Index]))
                {
                    AIPath usedPath = new AIPath(Paths[TrainRouteXRef[reqTrain.Index]]);
                    reqTrain.thisTTTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, usedPath);
                    reqTrain.thisTTTrain.Path = usedPath;
                    reqTrain.thisTTTrain.CreateRoute(false);  // create route without use of FrontTDBtraveller
                    reqTrain.thisTTTrain.EndRouteAtLastSignal();
                    reqTrain.thisTTTrain.ValidRoute[0] = new Train.TCSubpathRoute(reqTrain.thisTTTrain.TCRoute.TCRouteSubpaths[0]);
                    reqTrain.thisTTTrain.AITrainDirectionForward = true;

                    // Process stops
                    reqTrain.ConvertStops(simulator, reqTrain.thisTTTrain, reqTrain.Name);

                    // Process commands
                    if (reqTrain.TrainCommands.Count > 0)
                    {
                        reqTrain.ProcessCommands(simulator, reqTrain.thisTTTrain);
                    }

                    // Add AI train to output list
                    trainList.Add(reqTrain.thisTTTrain);
                }
            }

            // Process dispose commands
            foreach (TTTrainInfo reqTrain in allTrains)
            {
                if (reqTrain.DisposeDetails != null)
                {
                    bool pathsNoLoadFailure = reqTrain.ProcessDisposeInfo(ref trainList, playerTrain, simulator);
                    if (!pathsNoLoadFailure)
                    {
                        allPathsLoaded = false;
                        return trainList;
                    }
                }

                // Build detach cross references
                if (reqTrain.thisTTTrain.DetachDetails != null)
                {
                    int detachCount = 0;

                    foreach (KeyValuePair<int, List<DetachInfo>> thisDetachInfo in reqTrain.thisTTTrain.DetachDetails)
                    {
                        List<DetachInfo> detachList = thisDetachInfo.Value;

                        foreach (DetachInfo thisDetach in detachList)
                        {
                            if (!thisDetach.DetachFormedStatic)
                            {
                                if (thisDetach.DetachFormedTrain < 0)
                                {
                                    thisDetach.SetDetachXRef(reqTrain.thisTTTrain, trainList, playerTrain.thisTTTrain);
                                }
                            }
                            else
                            {
                                int lastSectionIndex = reqTrain.thisTTTrain.TCRoute.TCRouteSubpaths.Last().Last().TCSectionIndex;
                                thisDetach.DetachFormedTrain = reqTrain.thisTTTrain.CreateStaticTrainRef(reqTrain.thisTTTrain, ref trainList, thisDetach.DetachFormedTrainName, lastSectionIndex, detachCount);
                                detachCount++;
                            }
                        }
                    }
                }
            }

            return trainList;
        }

        //================================================================================================//
        /// <summary>
        /// Pre-Initialize player train : set all default details
        /// </summary>
        private void PreInitPlayerTrain(TTTrainInfo reqTrain)
        {
            // Set player train idents
            TTTrain playerTrain = reqTrain.thisTTTrain;
            reqTrain.playerTrain = true;

            playerTrain.TrainType = Train.TRAINTYPE.INTENDED_PLAYER;
            playerTrain.OrgAINumber = playerTrain.Number;
            playerTrain.Number = 0;
            playerTrain.ControlMode = Train.TRAIN_CONTROL.INACTIVE;
            playerTrain.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;

            // Define style of passing path
            simulator.Signals.UseLocationPassingPaths = true;

            // Create traveller
            AIPath usedPath = Paths[TrainRouteXRef[reqTrain.Index]];
            playerTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, usedPath);

            // Extract train path
            playerTrain.SetRoutePath(usedPath, simulator.Signals);
            playerTrain.EndRouteAtLastSignal();
            playerTrain.ValidRoute[0] = new Train.TCSubpathRoute(playerTrain.TCRoute.TCRouteSubpaths[0]);

            // remove any power commands
            // player train starts as created or as left behind by previous AI
            // no other power commands should be set for player train

            if (playerTrain.PowerActionRequired != null)
            {
                playerTrain.PowerActionRequired.Clear();
            }
        }

        //================================================================================================//
        /// <summary>
        /// Extract and initialize player train
        /// contains extracted train plus additional info for identification and selection
        /// </summary>
        private TTTrain InitializePlayerTrain(TTTrainInfo reqTrain, ref Dictionary<string, AIPath> paths, ref List<TTTrain> trainList)
        {
            // Set player train idents
            TTTrain playerTrain = reqTrain.thisTTTrain;

            simulator.Trains.Add(playerTrain);

            // Reset train for each car

            int icar = 1;
            foreach (TrainCar car in playerTrain.Cars)
            {
                car.Train = playerTrain;
                car.CarID = String.Concat(playerTrain.Number.ToString("0###"), "_", icar.ToString("0##"));
                icar++;
            }

            // Set player locomotive
            // First test first and last cars - if either is drivable, use it as player locomotive
            int lastIndex = playerTrain.Cars.Count - 1;

            if (playerTrain.Cars[0].IsDriveable)
            {
                simulator.PlayerLocomotive = playerTrain.LeadLocomotive = playerTrain.Cars[0];
            }
            else if (playerTrain.Cars[lastIndex].IsDriveable)
            {
                simulator.PlayerLocomotive = playerTrain.LeadLocomotive = playerTrain.Cars[lastIndex];
            }
            else
            {
                foreach (TrainCar car in playerTrain.Cars)
                {
                    if (car.IsDriveable) // First loco is the one the player drives
                    {
                        simulator.PlayerLocomotive = playerTrain.LeadLocomotive = car;
                        playerTrain.leadLocoAntiSlip = ((MSTSLocomotive)car).AntiSlip;
                        break;
                    }
                }
            }

            // Initialize brakes
            playerTrain.AITrainBrakePercent = 100;
            playerTrain.InitializeBrakes();

            // Set stops
            reqTrain.ConvertStops(simulator, playerTrain, reqTrain.Name);

            // Process commands
            if (reqTrain.TrainCommands.Count > 0)
            {
                reqTrain.ProcessCommands(simulator, reqTrain.thisTTTrain);
            }

            // Set detach cross-references
            foreach (KeyValuePair<int, List<DetachInfo>> thisDetachInfo in reqTrain.thisTTTrain.DetachDetails)
            {
                int detachCount = 0;

                List<DetachInfo> detachList = thisDetachInfo.Value;

                foreach (DetachInfo thisDetach in detachList)
                {
                    if (thisDetach.DetachFormedTrain < 0)
                    {
                        if (!thisDetach.DetachFormedStatic)
                        {
                            if (thisDetach.DetachFormedTrain < 0)
                            {
                                thisDetach.SetDetachXRef(reqTrain.thisTTTrain, trainList, null);
                            }
                        }
                        else
                        {
                            int lastSectionIndex = reqTrain.thisTTTrain.TCRoute.TCRouteSubpaths.Last().Last().TCSectionIndex;
                            thisDetach.DetachFormedTrain = reqTrain.thisTTTrain.CreateStaticTrainRef(reqTrain.thisTTTrain, ref trainList, thisDetach.DetachFormedTrainName, lastSectionIndex, detachCount);
                            detachCount++;
                        }
                    }
                }
            }

            // Finalize attach details
            if (reqTrain.thisTTTrain.AttachDetails != null && reqTrain.thisTTTrain.AttachDetails.Valid)
            {
                reqTrain.thisTTTrain.AttachDetails.FinalizeAttachDetails(reqTrain.thisTTTrain, trainList, null);
            }

            // Finalize pickup details
            if (reqTrain.thisTTTrain.PickUpDetails != null && reqTrain.thisTTTrain.PickUpDetails.Count > 0)
            {
                foreach (PickUpInfo thisPickUp in reqTrain.thisTTTrain.PickUpDetails)
                {
                    thisPickUp.FinalizePickUpDetails(reqTrain.thisTTTrain, trainList, null);
                }
                reqTrain.thisTTTrain.PickUpDetails.Clear();
            }

            // Finalize transfer details
            if (reqTrain.thisTTTrain.TransferStationDetails != null && reqTrain.thisTTTrain.TransferStationDetails.Count > 0)
            {
                foreach (KeyValuePair<int, TransferInfo> thisTransferStation in reqTrain.thisTTTrain.TransferStationDetails)
                {
                    TransferInfo thisTransfer = thisTransferStation.Value;
                    thisTransfer.SetTransferXRef(reqTrain.thisTTTrain, trainList, null, true, false);
                }
            }

            if (reqTrain.thisTTTrain.TransferTrainDetails != null && reqTrain.thisTTTrain.TransferTrainDetails.ContainsKey(-1))
            {
                foreach (TransferInfo thisTransfer in reqTrain.thisTTTrain.TransferTrainDetails[-1])
                {
                    thisTransfer.SetTransferXRef(reqTrain.thisTTTrain, trainList, null, false, true);
                    if (thisTransfer.Valid)
                    {
                        if (reqTrain.thisTTTrain.TransferTrainDetails.ContainsKey(thisTransfer.TransferTrain))
                        {
                            Trace.TraceInformation("Train {0} : transfer command : cannot transfer to same train twice : {1}", reqTrain.thisTTTrain.Name, thisTransfer.TransferTrainName);
                        }
                        else
                        {
                            List<TransferInfo> thisTransferList = new List<TransferInfo>
                            {
                                thisTransfer
                            };
                            reqTrain.thisTTTrain.TransferTrainDetails.Add(thisTransfer.TransferTrain, thisTransferList);
                        }
                    }
                }
                reqTrain.thisTTTrain.TransferTrainDetails.Remove(-1);
            }

            // Set activity details
            simulator.ClockTime = reqTrain.StartTime;
            simulator.ActivityFileName = reqTrain.TTDescription + "_" + reqTrain.Name;

            // If train is created before start time, create train as intended player train
            if (playerTrain.StartTime != playerTrain.ActivateTime)
            {
                playerTrain.TrainType = Train.TRAINTYPE.INTENDED_PLAYER;
                playerTrain.FormedOf = -1;
                playerTrain.FormedOfType = TTTrain.FormCommand.Created;

                playerTrain.PowerState = false;
            }

            return playerTrain;
        }

        //================================================================================================//
        /// <summary>
        /// Finalize activation commands
        /// </summary>
        public void FinalizeActivationCommands(ref List<TTTrain> trainList, ref TTTrain reqPlayerTrain)
        {
            List<int> activatedTrains = new List<int>();

            // Build list of trains to be activated
            // Set original AI number for player train
            foreach (TTTrain thisTrain in trainList)
            {
                if (thisTrain.TriggeredActivationRequired)
                {
                    activatedTrains.Add(thisTrain.OrgAINumber);
                }
            }

            if (reqPlayerTrain.TriggeredActivationRequired)
            {
                activatedTrains.Add(reqPlayerTrain.OrgAINumber);
            }

            // Process all activation commands
            foreach (TTTrain thisTrain in trainList)
            {
                if (thisTrain.activatedTrainTriggers != null && thisTrain.activatedTrainTriggers.Count > 0)
                {
                    for (int itrigger = thisTrain.activatedTrainTriggers.Count - 1; itrigger >= 0; itrigger--)
                    {
                        TTTrain.TriggerActivation thisTrigger = thisTrain.activatedTrainTriggers[itrigger];
                        thisTrain.activatedTrainTriggers.RemoveAt(itrigger);
                        string otherTrainName = String.Copy(thisTrigger.activatedName);

                        if (!otherTrainName.Contains(':'))
                        {
                            int seppos = thisTrain.Name.IndexOf(':');
                            otherTrainName = String.Concat(otherTrainName, ":", thisTrain.Name.Substring(seppos + 1).ToLower());
                        }

                        TTTrain otherTrain = thisTrain.GetOtherTTTrainByName(otherTrainName);
                        if (otherTrain == null)
                        {
                            Trace.TraceInformation("Invalid train activation command: train {0} not found, for train {1}", otherTrainName, thisTrain.Name);
                        }
                        else
                        {
                            if (activatedTrains.Contains(otherTrain.OrgAINumber))
                            {
                                activatedTrains.Remove(otherTrain.OrgAINumber);
                                thisTrigger.activatedTrain = otherTrain.Number;
                                thisTrain.activatedTrainTriggers.Insert(itrigger, thisTrigger);
                            }
                            else
                            {
                                Trace.TraceInformation("Invalid train activation command: train {0} not waiting for activation, for train {1}", otherTrainName, thisTrain.Name);
                            }
                        }
                    }
                }
            }

            // Process activation request for player train
            if (reqPlayerTrain.activatedTrainTriggers != null && reqPlayerTrain.activatedTrainTriggers.Count > 0)
            {
                for (int itrigger = reqPlayerTrain.activatedTrainTriggers.Count - 1; itrigger >= 0; itrigger--)
                {
                    TTTrain.TriggerActivation thisTrigger = reqPlayerTrain.activatedTrainTriggers[itrigger];
                    reqPlayerTrain.activatedTrainTriggers.RemoveAt(itrigger);
                    string otherTrainName = String.Copy(thisTrigger.activatedName);

                    if (!otherTrainName.Contains(':'))
                    {
                        int seppos = reqPlayerTrain.Name.IndexOf(':');
                        otherTrainName = String.Concat(otherTrainName, ":", reqPlayerTrain.Name.Substring(seppos + 1).ToLower());
                    }

                    TTTrain otherTrain = reqPlayerTrain.GetOtherTTTrainByName(otherTrainName);
                    if (otherTrain == null)
                    {
                        Trace.TraceInformation("Invalid train activation command: train {0} not found, for train {1}", otherTrainName, reqPlayerTrain.Name);
                    }
                    else
                    {
                        if (activatedTrains.Contains(otherTrain.OrgAINumber))
                        {
                            activatedTrains.Remove(otherTrain.OrgAINumber);
                            thisTrigger.activatedTrain = otherTrain.Number;
                            reqPlayerTrain.activatedTrainTriggers.Insert(itrigger, thisTrigger);
                        }
                        else
                        {
                            Trace.TraceInformation("Invalid train activation command: train {0} not waiting for activation, for train {1}", otherTrainName, reqPlayerTrain.Name);
                        }
                    }
                }
            }

            // Check if any activated trains remain untriggered
            foreach (int inumber in activatedTrains)
            {
                TTTrain otherTrain = trainList[0].GetOtherTTTrainByNumber(inumber);
                Trace.TraceInformation("Train activation required but no activation command set for train {0}", otherTrain.Name);
                otherTrain.TriggeredActivationRequired = false;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Pre-process all routes : read routes and convert to AIPath structure
        /// </summary>
        public bool PreProcessRoutes(CancellationToken cancellation)
        {

            // Extract names
            List<string> routeNames = new List<string>();

            foreach (KeyValuePair<string, AIPath> thisRoute in Paths)
            {
                routeNames.Add(thisRoute.Key);
            }

            // Clear routes - will be refilled
            Paths.Clear();
            bool allPathsLoaded = true;

            // Create routes
            foreach (string thisRoute in routeNames)
            {
                // Read route
                bool pathValid = true;
                LoadPath(thisRoute, out pathValid);
                if (!pathValid) allPathsLoaded = false;
                if (cancellation.IsCancellationRequested)
                    return false;
            }

            return allPathsLoaded;
        }

        //================================================================================================//
        /// <summary>
        /// Load path
        /// </summary>
        /// <param name="pathstring"></param>
        /// <param name="validPath"></param>
        /// <returns></returns>
        public AIPath LoadPath(string pathstring, out bool validPath)
        {
            validPath = true;

            string pathDirectory = Path.Combine(simulator.RoutePath, "Paths");
            string formedpathFilefull = Path.Combine(pathDirectory, pathstring);
            string pathExtension = Path.GetExtension(formedpathFilefull);

            if (String.IsNullOrEmpty(pathExtension))
                formedpathFilefull = Path.ChangeExtension(formedpathFilefull, "pat");

            if (!Paths.TryGetValue(formedpathFilefull, out var outPath))
            {
                // Try to load binary path if required
                bool binaryloaded = false;
                var formedpathFilefullBinary = simulator.Settings.GetCacheFilePath("Path", formedpathFilefull);

                if (BinaryPaths && File.Exists(formedpathFilefullBinary))
                {
                    var binaryLastWriteTime = File.GetLastWriteTime(formedpathFilefullBinary);
                    if (binaryLastWriteTime < simulator.TDB.LastWriteTime ||
                        (File.Exists(formedpathFilefull) && binaryLastWriteTime < File.GetLastWriteTime(formedpathFilefull)))
                    {
                        File.Delete(formedpathFilefullBinary);
                    }
                    else
                    {
                        try
                        {
                            var infpath = new BinaryReader(new FileStream(formedpathFilefullBinary, FileMode.Open, FileAccess.Read));
                            var cachePath = infpath.ReadString();
                            if (cachePath != formedpathFilefull)
                            {
                                Trace.TraceWarning($"Expected cache file for '{formedpathFilefull}'; got '{cachePath}' in {formedpathFilefullBinary}");
                            }
                            else
                            {
                                outPath = new AIPath(simulator.TDB, simulator.TSectionDat, infpath);

                                if (outPath.Nodes != null)
                                {
                                    Paths.Add(formedpathFilefull, outPath);
                                    binaryloaded = true;
                                }
                            }
                            infpath.Close();
                        }
                        catch
                        {
                            binaryloaded = false;
                        }
                    }
                }

                if (!binaryloaded)
                {
                    try
                    {
                        outPath = new AIPath(simulator.TDB, simulator.TSectionDat, formedpathFilefull, true, simulator.orRouteConfig);
                        validPath = outPath.Nodes != null;

                        if (validPath)
                        {
                            try
                            {
                                Paths.Add(formedpathFilefull, outPath);
                            }
                            catch (Exception e)
                            {
                                validPath = false;
                                if (!reportedPaths.Contains(formedpathFilefull))
                                {
                                    Trace.TraceInformation(new FileLoadException(formedpathFilefull, e).ToString());
                                    reportedPaths.Add(formedpathFilefull);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        validPath = false;
                        if (!reportedPaths.Contains(formedpathFilefull))
                        {
                            Trace.TraceInformation(new FileLoadException(formedpathFilefull, e).ToString());
                            reportedPaths.Add(formedpathFilefull);
                        }
                    }

                    if (validPath)
                    {
                        if (!binaryloaded && BinaryPaths)
                        {
                            try
                            {
                                var outfpath = new BinaryWriter(new FileStream(formedpathFilefullBinary, FileMode.Create));
                                outfpath.Write(formedpathFilefull);
                                outPath.Save(outfpath);
                                outfpath.Close();
                            }
                            // Dummy catch to avoid error
                            catch
                            { }
                        }
                    }
                    // Report path load failure
                }
            }

            return outPath;
        }

        //================================================================================================//
        /// <summary>
        /// class TTTrainInfo
        /// contains extracted train plus additional info for identification and selection
        /// </summary>
        private class TTTrainInfo
        {
            public TTTrain thisTTTrain;
            public string Name;
            public int StartTime;
            public string TTDescription;
            public int columnIndex;
            public bool validTrain = true;
            public bool playerTrain = false;
            public Dictionary<string, StopInfo> Stops = new Dictionary<string, StopInfo>();
            public List<string> reportedConsistFailures = new List<string>();
            public int Index;
            public List<TTTrainCommands> TrainCommands = new List<TTTrainCommands>();
            public DisposeInfo DisposeDetails = null;
            public SpecialLightsCondition[] SpecialLights = new SpecialLightsCondition[3];
            public List<string>[] SpecialLightSelection = new List<string>[3];

            public readonly TimetableInfo parentInfo;

            public struct consistInfo
            {
                public string consistFile;
                public bool reversed;
            }

            //================================================================================================//
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="trainName"></param>
            /// <param name="simulator"></param>
            /// <param name="ttfilename"></param>
            public TTTrainInfo(int icolumn, string trainName, Simulator simulator, int index, TimetableInfo thisParent)
            {
                parentInfo = thisParent;
                Name = String.Copy(trainName.Trim());
                thisTTTrain = new TTTrain(simulator);
                columnIndex = icolumn;
                Index = index;

                // presets
                for (int i = 0; i <= (int)SpecialLightsPhase.PostForms; i++)
                {
                    SpecialLights[i] = SpecialLightsCondition.Normal;
                    SpecialLightSelection[i] = new List<string>();
                }
            }

            //================================================================================================//
            /// <summary>
            /// Build train from info in single column
            /// </summary>
            /// <param name="fileStrings"></param>
            /// <param name="RowInfo"></param>
            /// <param name="pathRow"></param>
            /// <param name="consistRow"></param>
            /// <param name="startRow"></param>
            /// <param name="description"></param>
            /// <param name="stationNames"></param>
            /// <param name="ttInfo"></param>
            public bool BuildTrain(List<string[]> fileStrings, rowType[] RowInfo, int pathRow, int consistRow, int startRow, int disposeRow, int briefingRow, string description,
                Dictionary<int, StationInfo> stationNames, float actSpeedConv, TimetableInfo ttInfo)
            {
                TTDescription = string.Copy(description);

                // Set name
                // If $static, set starttime row to $static and create unique name
                if (Name.ToLower().Contains("$static"))
                {
                    fileStrings[startRow][columnIndex] = String.Copy("$static");

                    if (String.Equals(Name.Trim().Substring(0, 1), "$"))
                    {
                        string trainName = "S" + columnIndex.ToString().Trim();
                        thisTTTrain.Name = trainName + ":" + TTDescription;
                    }
                    else
                    {
                        string[] nameParts = Name.Split('$');
                        thisTTTrain.Name = nameParts[0].Trim() + ":" + TTDescription;
                    }
                }
                else
                {
                    thisTTTrain.Name = Name + ":" + TTDescription;
                }

                thisTTTrain.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;
                thisTTTrain.OrgAINumber = thisTTTrain.Number;

                // Derive various directory paths
                string pathDirectory = Path.Combine(ttInfo.simulator.RoutePath, "Paths");

                // No path defined: exit
                if (String.IsNullOrEmpty(fileStrings[pathRow][columnIndex]))
                {
                    Trace.TraceInformation("Error for train {0} : no path defined", thisTTTrain.Name);
                    return false;
                }

                string pathFilefull = ExtractPathString(pathDirectory, fileStrings[pathRow][columnIndex], ref thisTTTrain);

                string trainsDirectory = Path.Combine(ttInfo.simulator.BasePath, "Trains");
                string consistDirectory = Path.Combine(trainsDirectory, "Consists");

                string consistdef = fileStrings[consistRow][columnIndex];

                // No consist defined: exit
                if (String.IsNullOrEmpty(consistdef))
                {
                    Trace.TraceInformation("Error for train {0} : no consist defined", thisTTTrain.Name);
                    return false;
                }

                List<consistInfo> consistdetails = ProcessConsistInfo(consistdef);
                string trainsetDirectory = Path.Combine(trainsDirectory, "trainset");

                // EExtract path
                string pathExtension = Path.GetExtension(pathFilefull);
                if (String.IsNullOrEmpty(pathExtension))
                    pathFilefull = Path.ChangeExtension(pathFilefull, "pat");
                ttInfo.TrainRouteXRef.Add(Index, pathFilefull); // Set reference to path

                if (!ttInfo.Paths.ContainsKey(pathFilefull))
                {
                    ttInfo.Paths.Add(pathFilefull, null); // Insert name in dictionary, path will be loaded later
                }

                // Build consist
                bool returnValue = true;
                returnValue = BuildConsist(consistdetails, trainsetDirectory, consistDirectory, ttInfo.simulator);

                // Return if consist could not be loaded
                if (!returnValue) return returnValue;

                // Derive starttime
                if (validTrain)
                {
                    string startString = fileStrings[startRow][columnIndex].ToLower().Trim();
                    ExtractStartTime(startString, consistdetails[0].consistFile, ttInfo.simulator);
                }

                // Process dispose info
                if (disposeRow > 0)
                {
                    string disposeString = fileStrings[disposeRow][columnIndex].ToLower().Trim();

                    if (!String.IsNullOrEmpty(disposeString))
                    {
                        string[] disposeCommandString = disposeString.Split('$');

                        foreach (string thisDisposeString in disposeCommandString)
                        {
                            if (!String.IsNullOrEmpty(thisDisposeString))
                            {
                                TTTrainCommands disposeCommands = new TTTrainCommands(thisDisposeString);
                                switch (disposeCommands.CommandToken)
                                {
                                    case "forms":
                                        DisposeDetails = new DisposeInfo(DisposeInfo.DisposeType.Forms, disposeCommands, TTTrain.FormCommand.TerminationFormed, thisTTTrain.Name);
                                        break;

                                    case "triggers":
                                        DisposeDetails = new DisposeInfo(DisposeInfo.DisposeType.Triggers, disposeCommands, TTTrain.FormCommand.TerminationTriggered, thisTTTrain.Name);
                                        break;

                                    case "static":
                                        DisposeDetails = new DisposeInfo(DisposeInfo.DisposeType.Static, disposeCommands, TTTrain.FormCommand.TerminationFormed, thisTTTrain.Name);
                                        break;

                                    case "stable":
                                        DisposeDetails = new DisposeInfo(DisposeInfo.DisposeType.Stable, disposeCommands, TTTrain.FormCommand.TerminationFormed, thisTTTrain.Name);
                                        break;

                                    case "pool":
                                        DisposeDetails = new DisposeInfo(DisposeInfo.DisposeType.Pool, disposeCommands, TTTrain.FormCommand.None, thisTTTrain.Name);
                                        break;

                                    case "attach":
                                        thisTTTrain.AttachDetails = new AttachInfo(-1, disposeCommands, thisTTTrain);
                                        break;

                                    case "detach":
                                        DetachInfo thisDetach = new DetachInfo(thisTTTrain, disposeCommands, false, false, true, -1, null);
                                        if (thisTTTrain.DetachDetails.ContainsKey(-1))
                                        {
                                            List<DetachInfo> tempList = thisTTTrain.DetachDetails[-1];
                                            tempList.Add(thisDetach);
                                        }
                                        else
                                        {
                                            List<DetachInfo> tempList = new List<DetachInfo>
                                            {
                                                thisDetach
                                            };
                                            thisTTTrain.DetachDetails.Add(-1, tempList);
                                        }
                                        break;

                                    case "pickup":
                                        if (!DisposeDetails.FormTrain)
                                        {
                                            Trace.TraceInformation("Train : {0} : $pickup in dispose command is only allowed if preceded by a $forms command", thisTTTrain.Name);
                                        }
                                        else
                                        {
                                            PickUpInfo thisPickup = new PickUpInfo(-1, disposeCommands, thisTTTrain);
                                            thisTTTrain.PickUpDetails.Add(thisPickup);
                                        }
                                        break;

                                    case "transfer":
                                        if (!DisposeDetails.FormTrain)
                                        {
                                            Trace.TraceInformation("Train : {0} : $transfer in dispose command is only allowed if preceded by a $forms command", thisTTTrain.Name);
                                        }
                                        else if (thisTTTrain.TransferTrainDetails.ContainsKey(-1))
                                        {
                                            Trace.TraceInformation("Train : {0} : cannot define multiple transfer on static consists", thisTTTrain.Name);
                                        }
                                        else
                                        {
                                            TransferInfo thisTransfer = new TransferInfo(-1, disposeCommands, thisTTTrain);
                                            List<TransferInfo> newList = new List<TransferInfo>
                                            {
                                                thisTransfer
                                            };

                                            if (thisTransfer.TransferTrain == -99)
                                            {
                                                thisTTTrain.TransferTrainDetails.Add(-99, newList); //set key to -99 as reference
                                            }
                                            else
                                            {
                                                thisTTTrain.TransferTrainDetails.Add(-1, newList); // set key to -1 to work out reference later
                                            }
                                        }
                                        break;

                                    case "activate":
                                        if (disposeCommands.CommandValues == null || disposeCommands.CommandValues.Count < 1)
                                        {
                                            Trace.TraceInformation("No train reference set for train activation, train {0}", Name);
                                            break;
                                        }

                                        TTTrain.TriggerActivation thisTrigger = new TTTrain.TriggerActivation
                                        {
                                            activationType = TTTrain.TriggerActivationType.Dispose,
                                            activatedName = disposeCommands.CommandValues[0]
                                        };
                                        thisTTTrain.activatedTrainTriggers.Add(thisTrigger);

                                        break;

                                    default:
                                        Trace.TraceWarning("Invalid dispose string defined for train {0} : {1}",
                                            thisTTTrain.Name, disposeCommands.CommandToken);
                                        break;
                                }
                            }
                        }
                    }
                }

                // Derive station stops and other info
                for (int iRow = 1; iRow <= fileStrings.Count - 1; iRow++)
                {
                    switch (RowInfo[iRow])
                    {
                        case rowType.directionInfo: // No longer used, maintained for compatibility with existing timetables
                            break;

                        case rowType.stationInfo:
                            StationInfo stationDetails = stationNames[iRow];
                            if (!String.IsNullOrEmpty(fileStrings[iRow][columnIndex]))
                            {
                                if (Stops.ContainsKey(stationDetails.StationName))
                                {
                                    Trace.TraceInformation("Double station reference : train " + Name + " ; station : " + stationDetails.StationName);
                                }
                                else if (fileStrings[iRow][columnIndex].StartsWith("P"))
                                {
                                    // Allowed in timetable but not yet implemented
                                }
                                else
                                {
                                    Stops.Add(stationDetails.StationName, ProcessStopInfo(fileStrings[iRow][columnIndex], stationDetails));
                                }
                            }
                            break;

                        case rowType.restartDelayInfo:
                            ProcessRestartDelay(fileStrings[iRow][columnIndex].ToLower().Trim());
                            break;

                        case rowType.speedInfo:
                            ProcessSpeedInfo(fileStrings[iRow][columnIndex].ToLower().Trim(), actSpeedConv);
                            break;

                        case rowType.lightInfo:
                            ProcessLightInfo(fileStrings[iRow][columnIndex].ToLower().Trim());
                            break;

                        case rowType.trainNotesInfo:
                            if (!String.IsNullOrEmpty(fileStrings[iRow][columnIndex]))
                            {
                                string[] commandStrings = fileStrings[iRow][columnIndex].Split('$');
                                foreach (string thisCommand in commandStrings)
                                {
                                    if (!String.IsNullOrEmpty(thisCommand))
                                    {
                                        TrainCommands.Add(new TTTrainCommands(thisCommand));
                                    }
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }

                // Set speed details based on route, config and input
                thisTTTrain.ProcessSpeedSettings();

                if (briefingRow >= 0)
                    thisTTTrain.Briefing = fileStrings[briefingRow][columnIndex].Replace("<br>", "\n");

                return true;
            }

            //================================================================================================//
            /// <summary>
            /// Extract path string from train details, add it to list of paths if not yet added
            /// </summary>
            /// <param name="pathDirectory"></param>
            /// <param name="pathString"></param>
            /// <param name="thisTrain"></param>
            /// <returns></returns>
            public string ExtractPathString(string pathDirectory, string pathString, ref TTTrain thisTrain)
            {
                string fullstring = string.Empty;

                // Process strings
                string procPathString = pathString.ToLower().Trim();
                List<TTTrainCommands> pathCommands = new List<TTTrainCommands>();

                if (!String.IsNullOrEmpty(procPathString))
                {
                    string[] pathCommandString = procPathString.Split('$');
                    foreach (string thisCommand in pathCommandString)
                    {
                        if (!String.IsNullOrEmpty(thisCommand))
                        {
                            pathCommands.Add(new TTTrainCommands(thisCommand));
                        }
                    }

                    // Actual path is string [0]
                    fullstring = Path.Combine(pathDirectory, pathCommandString[0].Trim());
                    pathCommands.RemoveAt(0);

                    // Process qualifiers
                    foreach (TTTrainCommands thisCommand in pathCommands)
                    {
                        if (!String.IsNullOrEmpty(thisCommand.CommandToken))
                        {
                            switch (thisCommand.CommandToken)
                            {
                                case "endatlastsignal":
                                    bool reverse = false;

                                    if (thisCommand.CommandQualifiers != null && thisCommand.CommandQualifiers.Count > 0)
                                    {
                                        if (String.Equals(thisCommand.CommandQualifiers[0].QualifierName, "reverse"))
                                        {
                                            reverse = true;
                                        }
                                    }

                                    thisTrain.ReqLastSignalStop = reverse ? TTTrain.LastSignalStop.Reverse : TTTrain.LastSignalStop.Last;
                                    break;

                                default:
                                    Trace.TraceInformation("Train {0} : invalid qualifier for path field : {1} \n", thisTTTrain.Name, thisCommand.CommandToken);
                                    break;
                            }
                        }
                    }
                }

                return fullstring;
            }

            //================================================================================================//
            /// <summary>
            /// Extract start time info from train details
            /// </summary>
            /// <param name="startString"></param>
            /// <param name="consistInfo"></param>
            /// <param name="simulator"></param>
            public void ExtractStartTime(string startString, string consistInfo, Simulator simulator)
            {
                string[] startparts = new string[1];
                string startTimeString = String.Empty;
                string activateTimeString = String.Empty;
                bool created = false;
                bool createStatic = false;
                string createAhead = String.Empty;
                string createInPool = String.Empty;
                bool startNextNight = false;
                string createFromPool = String.Empty;
                string createPoolDirection = String.Empty;
                bool setConsistName = false;
                bool activationRequired = false;
                bool powerOffOnCreate = false;
                bool powerPantoUp = false;
                bool startPowerOn = false;
                string startPowerOnTime = String.Empty;

                // Process qualifier if set
                List<TTTrainCommands> StartCommands = new List<TTTrainCommands>();

                if (startString.Contains('$'))
                {
                    string[] commandStrings = startString.Split('$');
                    foreach (string thisCommand in commandStrings)
                    {
                        if (!String.IsNullOrEmpty(thisCommand))
                        {
                            StartCommands.Add(new TTTrainCommands(thisCommand));
                        }
                    }
                }
                else
                {
                    StartCommands.Add(new TTTrainCommands(startString));
                }

                // First command is start time except for static
                if (!String.Equals(StartCommands[0].CommandToken, "static"))
                {
                    startTimeString = StartCommands[0].CommandToken;
                    activateTimeString = StartCommands[0].CommandToken;

                    // check additional qualifiers
                    if (StartCommands[0].CommandQualifiers != null && StartCommands[0].CommandQualifiers.Count > 0)
                    {
                        foreach (TTTrainCommands.TTTrainComQualifiers thisQualifier in StartCommands[0].CommandQualifiers)
                        {
                            switch (thisQualifier.QualifierName)
                            {
                                case "poweron":
                                    if (thisQualifier.QualifierValues == null | thisQualifier.QualifierValues.Count <= 0)
                                    {
                                        Trace.TraceWarning("Train {0} : start command : PowerOn defined without time value", Name);
                                    }
                                    else
                                    {
                                        startPowerOn = true;
                                        startPowerOnTime = thisQualifier.QualifierValues[0];
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    StartCommands.RemoveAt(0);
                }

                foreach (TTTrainCommands thisCommand in StartCommands)
                {
                    switch (thisCommand.CommandToken)
                    {
                        // check for create - syntax : $create [=starttime] [/ahead = train] [/pool = pool] [/poweroff]
                        case "create":
                            created = true;

                            // Process starttime
                            if (thisCommand.CommandValues != null && thisCommand.CommandValues.Count > 0)
                            {
                                startTimeString = String.Copy(thisCommand.CommandValues[0]);
                            }
                            else
                            // if not set, start at 1 second (same start time as for static so /ahead will work for both create and static)
                            {
                                startTimeString = "00:00:01";
                            }

                            // Check additional qualifiers
                            if (thisCommand.CommandQualifiers != null && thisCommand.CommandQualifiers.Count > 0)
                            {
                                foreach (TTTrainCommands.TTTrainComQualifiers thisQualifier in thisCommand.CommandQualifiers)
                                {
                                    switch (thisQualifier.QualifierName)
                                    {
                                        case "ahead":
                                            if (thisQualifier.QualifierValues != null && thisQualifier.QualifierValues.Count > 0)
                                            {
                                                createAhead = String.Copy(thisQualifier.QualifierValues[0]);
                                            }
                                            break;

                                        case "poweroff":
                                            powerOffOnCreate = true;
                                            if (thisQualifier.QualifierValues != null && thisQualifier.QualifierValues.Count > 0)
                                            {
                                                if (thisQualifier.QualifierValues[0] == "pantoup")
                                                {
                                                    powerPantoUp = true;
                                                }
                                                else
                                                {
                                                    Trace.TraceInformation("Train : {0} : invalid value for poweroff in create command : {1} \n", thisTTTrain.Name, thisQualifier.QualifierValues[0]);
                                                }
                                            }
                                            break;

                                        default:
                                            break;
                                    }
                                }
                            }
                            break;

                        // Pool : created from pool - syntax : $pool = pool [/direction = backward | forward]
                        case "pool":
                            if (thisCommand.CommandValues == null || thisCommand.CommandValues.Count < 1)
                            {
                                Trace.TraceInformation("Missing poolname for train {0}, train not included", thisTTTrain.Name + "\n");
                            }
                            else
                            {
                                createFromPool = String.Copy(thisCommand.CommandValues[0]);
                                if (thisCommand.CommandQualifiers != null)
                                {
                                    foreach (TTTrainCommands.TTTrainComQualifiers thisQualifier in thisCommand.CommandQualifiers)
                                    {
                                        switch (thisQualifier.QualifierName.ToLower().Trim())
                                        {
                                            case "set_consist_name":
                                                setConsistName = true;
                                                break;

                                            case "direction":
                                                createPoolDirection = String.Copy(thisQualifier.QualifierValues[0]);
                                                break;

                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                            break;

                            // Check for $next: set special flag to start after midnight
                            case "next":
                                startNextNight = true;
                                break;

                        // Sstatic : syntax : $static [/ahead = train] [/poweroff]
                        case "static":
                            createStatic = true;

                            // Check additional qualifiers
                            if (thisCommand.CommandQualifiers != null && thisCommand.CommandQualifiers.Count > 0)
                            {
                                foreach (TTTrainCommands.TTTrainComQualifiers thisQualifier in thisCommand.CommandQualifiers)
                                {
                                    switch (thisQualifier.QualifierName)
                                    {
                                        case "ahead":
                                            if (thisQualifier.QualifierValues != null && thisQualifier.QualifierValues.Count > 0)
                                            {
                                                createAhead = String.Copy(thisQualifier.QualifierValues[0]);
                                            }
                                            break;

                                        case "poweroff":
                                            powerOffOnCreate = true;
                                            if (thisQualifier.QualifierValues != null && thisQualifier.QualifierValues.Count > 0)
                                            {
                                                if (thisQualifier.QualifierValues[0] == "pantoup")
                                                {
                                                    powerPantoUp = true;
                                                }
                                                else
                                                {
                                                    Trace.TraceInformation("Train : {0} : invalid value for poweroff in create command : {1} \n", thisTTTrain.Name, thisQualifier.QualifierValues[0]);
                                                }
                                            }
                                            break;

                                        case "pool":
                                            if (thisQualifier.QualifierValues != null && thisQualifier.QualifierValues.Count > 0)
                                            {
                                                createInPool = String.Copy(thisQualifier.QualifierValues[0]);
                                                if (!simulator.PoolHolder.Pools.ContainsKey(createInPool))
                                                {
                                                    Trace.TraceInformation("Train : " + thisTTTrain.Name + " : no such pool : " + createInPool + " ; train not created");
                                                    createInPool = String.Empty;
                                                }
                                            }
                                            break;

                                        default:
                                            break;
                                    }
                                }
                            }

                            if (!String.IsNullOrEmpty(createInPool) && powerOffOnCreate)
                            {
                                Trace.TraceInformation("Train : " + thisTTTrain.Name + " : poweroff setting ignored as train is created in pool");
                            }
                            break;

                        // activated : set activated flag
                        case "activated":
                            activationRequired = true;
                            break;

                        // invalid command
                        default:
                            Trace.TraceInformation("Train : " + Name + " invalid command for start value : " + thisCommand.CommandToken + "\n");
                            break;
                    }
                }

                TimeSpan startingTime;
                bool validSTime = TimeSpan.TryParse(startTimeString, out startingTime);
                TimeSpan activateTime;
                bool validATime = TimeSpan.TryParse(activateTimeString, out activateTime);

                TimeSpan powerTime = TimeSpan.Zero;
                bool validPTime = false;
                if (startPowerOn)
                {
                    validPTime = TimeSpan.TryParse(startPowerOnTime, out powerTime);
                }

                if (validSTime && validATime)
                {
                    thisTTTrain.StartTime = Math.Max(Convert.ToInt32(startingTime.TotalSeconds), 1);
                    thisTTTrain.ActivateTime = Math.Max(Convert.ToInt32(activateTime.TotalSeconds), 1);
                    thisTTTrain.Created = created;
                    thisTTTrain.TriggeredActivationRequired = activationRequired;

                    // Trains starting after midnight
                    if (startNextNight && thisTTTrain.StartTime.HasValue)
                    {
                        thisTTTrain.StartTime = thisTTTrain.StartTime.Value + (24 * 3600);
                        thisTTTrain.ActivateTime = thisTTTrain.ActivateTime.Value + (24 * 3600);
                    }

                    // set power-up time
                    if (validPTime && !createStatic && String.IsNullOrEmpty(createFromPool))
                    {
                        if (powerTime > activateTime) powerTime = activateTime;  // ensure power is always on at start time
                        double powerTimeValue = startNextNight ? (Math.Max(powerTime.TotalSeconds, 1) + (24 * 3600)) : Math.Max(powerTime.TotalSeconds, 1);
                        thisTTTrain.SetRequiredPowerChange(TTTrain.PowerActionType.On, null, powerTimeValue);
                    }

                    if (created && !String.IsNullOrEmpty(createAhead))
                    {
                        if (!createAhead.Contains(':'))
                        {
                            thisTTTrain.CreateAhead = createAhead + ":" + TTDescription;
                        }
                        else
                        {
                            thisTTTrain.CreateAhead = createAhead;
                        }
                        thisTTTrain.CreateAhead = thisTTTrain.CreateAhead.ToLower();
                    }

                    if (!String.IsNullOrEmpty(createFromPool))
                    {
                        thisTTTrain.CreateFromPool = String.Copy(createFromPool);
                        thisTTTrain.ForcedConsistName = String.Empty;

                        if (setConsistName)
                        {
                            thisTTTrain.ForcedConsistName = String.Copy(consistInfo);
                        }

                        switch (createPoolDirection)
                        {
                            case "backward":
                                thisTTTrain.CreatePoolDirection = TimetablePool.PoolExitDirectionEnum.Backward;
                                break;

                            case "forward":
                                thisTTTrain.CreatePoolDirection = TimetablePool.PoolExitDirectionEnum.Forward;
                                break;

                            default:
                                thisTTTrain.CreatePoolDirection = TimetablePool.PoolExitDirectionEnum.Undefined;
                                break;
                        }
                    }

                    StartTime = thisTTTrain.ActivateTime.Value;

                    if (created && powerOffOnCreate && String.IsNullOrEmpty(createFromPool))
                    {
                        thisTTTrain.PowerOffOnCreate = powerPantoUp ? TTTrain.PowerActionType.Off_PantoUp : TTTrain.PowerActionType.Off;
                    }
                }
                else if (!String.IsNullOrEmpty(createInPool))
                {
                    thisTTTrain.StartTime = 1;
                    thisTTTrain.ActivateTime = null;
                    thisTTTrain.CreateInPool = String.Copy(createInPool);
                }
                else if (createStatic)
                {
                    thisTTTrain.StartTime = 1;
                    thisTTTrain.ActivateTime = null;
                    thisTTTrain.PowerOffOnCreate = TTTrain.PowerActionType.Off;

                    if (!String.IsNullOrEmpty(createAhead))
                    {
                        if (!createAhead.Contains(':'))
                        {
                            thisTTTrain.CreateAhead = createAhead + ":" + TTDescription;
                        }
                        else
                        {
                            thisTTTrain.CreateAhead = createAhead;
                        }
                        thisTTTrain.CreateAhead = thisTTTrain.CreateAhead.ToLower();
                    }
                }
                else
                {
                    Trace.TraceInformation("Invalid starttime {0} for train {1}, train not included", startString, thisTTTrain.Name);
                    validTrain = false;
                }

                // Activation is not allowed if started from pool
                if (activationRequired && !String.IsNullOrEmpty(createFromPool))
                {
                    activationRequired = false;
                    Trace.TraceInformation("Trigger activation not allowed when starting from pool, trigger activation reset for train {0}", thisTTTrain.Name);
                }
            }

            //================================================================================================//
            /// <summary>
            /// Extract restart delay info from train details
            /// </summary>
            /// <param name="restartDelayInfo"></param>
            public void ProcessRestartDelay(string restartDelayInfo)
            {
                // Build list of commands
                List<TTTrainCommands> RestartDelayCommands = new List<TTTrainCommands>();

                if (!String.IsNullOrEmpty(restartDelayInfo))
                {
                    string[] commandStrings = restartDelayInfo.Split('$');
                    foreach (string thisCommand in commandStrings)
                    {
                        if (!String.IsNullOrEmpty(thisCommand))
                        {
                            RestartDelayCommands.Add(new TTTrainCommands(thisCommand));
                        }
                    }
                }

                // Process list of commands
                foreach (TTTrainCommands thisCommand in RestartDelayCommands)
                {
                    switch (thisCommand.CommandToken)
                    {
                        // Delay when new
                        case "new":
                            thisTTTrain.DelayedStartSettings.newStart = ProcessRestartDelayValues(thisTTTrain.Name, thisCommand.CommandQualifiers, thisCommand.CommandToken);
                            break;

                        // Delay when restarting from signal or other path action
                        case "path":
                            thisTTTrain.DelayedStartSettings.pathRestart = ProcessRestartDelayValues(thisTTTrain.Name, thisCommand.CommandQualifiers, thisCommand.CommandToken);
                            break;

                        // Delay when restarting from station stop
                        case "station":
                            thisTTTrain.DelayedStartSettings.stationRestart = ProcessRestartDelayValues(thisTTTrain.Name, thisCommand.CommandQualifiers, thisCommand.CommandToken);
                            break;

                        // Delay when restarting when following stopped train
                        case "follow":
                            thisTTTrain.DelayedStartSettings.followRestart = ProcessRestartDelayValues(thisTTTrain.Name, thisCommand.CommandQualifiers, thisCommand.CommandToken);
                            break;

                        // Delay after attaching
                        case "attach":
                            thisTTTrain.DelayedStartSettings.attachRestart = ProcessRestartDelayValues(thisTTTrain.Name, thisCommand.CommandQualifiers, thisCommand.CommandToken);
                            break;

                        // Delay on detaching
                        case "detach":
                            thisTTTrain.DelayedStartSettings.detachRestart = ProcessRestartDelayValues(thisTTTrain.Name, thisCommand.CommandQualifiers, thisCommand.CommandToken);
                            break;

                        // Delay for train and moving table
                        case "movingtable":
                            thisTTTrain.DelayedStartSettings.movingtableRestart = ProcessRestartDelayValues(thisTTTrain.Name, thisCommand.CommandQualifiers, thisCommand.CommandToken);
                            break;

                        // Delay when restarting at reversal
                        case "reverse":
                            // Process additional reversal delay
                            for (int iIndex = thisCommand.CommandQualifiers.Count - 1; iIndex >= 0; iIndex--)
                            {
                                TTTrainCommands.TTTrainComQualifiers thisQual = thisCommand.CommandQualifiers[iIndex];
                                switch (thisQual.QualifierName)
                                {
                                    case "additional":
                                        try
                                        {
                                            thisTTTrain.DelayedStartSettings.reverseAddedDelaySperM = Convert.ToSingle(thisQual.QualifierValues[0]);
                                        }
                                        catch
                                        {
                                            Trace.TraceInformation("Train {0} : invalid value for '$reverse /additional' delay value : {1} \n", thisTTTrain.Name, thisQual.QualifierValues[0]);
                                        }
                                        break;

                                    default:
                                        Trace.TraceInformation("Invalid qualifier in restartDelay value for reversal : {0} for train : {1}", thisQual.QualifierName, thisTTTrain.Name);
                                        break;
                                }
                            }
                            break;

                        default:
                            Trace.TraceInformation("Invalid command in restartDelay value : {0} for train : {1}", thisCommand.CommandToken, thisTTTrain.Name);
                            break;
                    }
                }
            }

            //================================================================================================//
            /// <summary>
            /// Read and convert input of restart delay values
            /// </summary>
            /// <param name="trainName"></param>
            /// <param name="commandQualifiers"></param>
            /// <param name="delayType"></param>
            /// <returns></returns>
            public TTTrain.DelayedStartBase ProcessRestartDelayValues(String trainName, List<TTTrainCommands.TTTrainComQualifiers> commandQualifiers, string delayType)
            {
                // Preset values
                TTTrain.DelayedStartBase newDelayInfo = new TTTrain.DelayedStartBase();

                // Process command qualifiers
                foreach (TTTrainCommands.TTTrainComQualifiers thisQual in commandQualifiers)
                {
                    switch (thisQual.QualifierName)
                    {
                        case "fix":
                            try
                            {
                                newDelayInfo.fixedPartS = Convert.ToInt32(thisQual.QualifierValues[0]);
                            }
                            catch
                            {
                                Trace.TraceInformation("Train {0} : invalid value for fixed part for '{1}' restart delay : {2} \n",
                                    trainName, delayType, thisQual.QualifierValues[0]);
                            }
                            break;

                        case "var":
                            try
                            {
                                newDelayInfo.randomPartS = Convert.ToInt32(thisQual.QualifierValues[0]);
                            }
                            catch
                            {
                                Trace.TraceInformation("Train {0} : invalid value for variable part for '{1}' restart delay : {2} \n",
                                    trainName, delayType, thisQual.QualifierValues[0]);
                            }
                            break;

                        default:
                            Trace.TraceInformation("Invalid qualifier in restartDelay value : {0} for train : {1}", thisQual.QualifierName, trainName);
                            break;
                    }
                }

                return newDelayInfo;
            }

            //================================================================================================//
            /// <summary>
            /// Extract speed info from train details
            /// </summary>
            /// <param name="speedInfo"></param>
            /// <param name="actSpeedConv"></param>
            public void ProcessSpeedInfo(string speedInfo, float actSpeedConv)
            {
                // Build list of commands
                List<TTTrainCommands> SpeedCommands = new List<TTTrainCommands>();

                if (!String.IsNullOrEmpty(speedInfo))
                {
                    string[] commandStrings = speedInfo.Split('$');
                    foreach (string thisCommand in commandStrings)
                    {
                        if (!String.IsNullOrEmpty(thisCommand))
                        {
                            SpeedCommands.Add(new TTTrainCommands(thisCommand));
                        }
                    }

                    foreach (TTTrainCommands thisCommand in SpeedCommands)
                    {
                        if (thisCommand.CommandValues == null || thisCommand.CommandValues.Count < 1)
                        {
                            Trace.TraceInformation("Value missing in speed command : {0} for train : {1}", thisCommand.CommandToken, thisTTTrain.Name);
                            break;
                        }

                        switch (thisCommand.CommandToken)
                        {
                            case "max":
                                try
                                {
                                    thisTTTrain.SpeedSettings.maxSpeedMpS = Convert.ToSingle(thisCommand.CommandValues[0]) * actSpeedConv;
                                }
                                catch
                                {
                                    Trace.TraceInformation("Train {0} : invalid value for '{1}' speed setting : {2} \n",
                                        thisTTTrain.Name, thisCommand.CommandToken, thisCommand.CommandValues[0]);
                                }
                                break;

                            case "cruise":
                                try
                                {
                                    thisTTTrain.SpeedSettings.cruiseSpeedMpS = Convert.ToSingle(thisCommand.CommandValues[0]) * actSpeedConv;
                                }
                                catch
                                {
                                    Trace.TraceInformation("Train {0} : invalid value for '{1}' speed setting : {2} \n",
                                        thisTTTrain.Name, thisCommand.CommandToken, thisCommand.CommandValues[0]);
                                }
                                break;

                            case "maxdelay":
                                try
                                {
                                    thisTTTrain.SpeedSettings.cruiseMaxDelayS = Convert.ToInt32(thisCommand.CommandValues[0]) * 60; // defined in minutes
                                }
                                catch
                                {
                                    Trace.TraceInformation("Train {0} : invalid value for '{1}' setting : {2} \n",
                                        thisTTTrain.Name, thisCommand.CommandToken, thisCommand.CommandValues[0]);
                                }
                                break;

                            case "creep":
                                try
                                {
                                    thisTTTrain.SpeedSettings.creepSpeedMpS = Convert.ToSingle(thisCommand.CommandValues[0]) * actSpeedConv;
                                }
                                catch
                                {
                                    Trace.TraceInformation("Train {0} : invalid value for '{1}' speed setting : {2} \n",
                                        thisTTTrain.Name, thisCommand.CommandToken, thisCommand.CommandValues[0]);
                                }
                                break;

                            case "attach":
                                try
                                {
                                    thisTTTrain.SpeedSettings.attachSpeedMpS = Convert.ToSingle(thisCommand.CommandValues[0]) * actSpeedConv;
                                }
                                catch
                                {
                                    Trace.TraceInformation("Train {0} : invalid value for '{1}' speed setting : {2} \n",
                                        thisTTTrain.Name, thisCommand.CommandToken, thisCommand.CommandValues[0]);
                                }
                                break;

                            case "detach":
                                try
                                {
                                    thisTTTrain.SpeedSettings.detachSpeedMpS = Convert.ToSingle(thisCommand.CommandValues[0]) * actSpeedConv;
                                }
                                catch
                                {
                                    Trace.TraceInformation("Train {0} : invalid value for '{1}' speed setting : {2} \n",
                                        thisTTTrain.Name, thisCommand.CommandToken, thisCommand.CommandValues[0]);
                                }
                                break;

                            case "movingtable":
                                try
                                {
                                    thisTTTrain.SpeedSettings.movingtableSpeedMpS = Convert.ToSingle(thisCommand.CommandValues[0]) * actSpeedConv;
                                }
                                catch
                                {
                                    Trace.TraceInformation("Train {0} : invalid value for '{1}' speed setting : {2} \n",
                                        thisTTTrain.Name, thisCommand.CommandToken, thisCommand.CommandValues[0]);
                                }
                                break;

                            default:
                                Trace.TraceInformation("Invalid token in speed command : {0} for train : {1}", thisCommand.CommandToken, thisTTTrain.Name);
                                break;
                        }
                    }
                }
            }

            //================================================================================================//
            /// <summary>
            /// Extract light info from train details
            /// </summary>
            /// <param name="LightInfo"></param>
            public void ProcessLightInfo(string LightInfo)
            {
                bool useRunningforPrestart = false;
                bool usePostFormforRUnning = false;
                bool useFulltime = false;

                bool[] validLights = new bool[3] { false, false, false };

                // build list of commands
                List<TTTrainCommands> LightCommands = new List<TTTrainCommands>();

                if (!String.IsNullOrEmpty(LightInfo))
                {
                    string[] commandStrings = LightInfo.Split('$');
                    foreach (string thisCommand in commandStrings)
                    {
                        if (!String.IsNullOrEmpty(thisCommand))
                        {
                            LightCommands.Add(new TTTrainCommands(thisCommand));
                        }
                    }

                    foreach (var LightCommand in LightCommands)
                    {
                        bool validLight = true;
                        SpecialLightsPhase thisPhase = SpecialLightsPhase.PreStart;
                        bool setphase = false;

                        // string contains special light settings, separated by '+'
                        if (!String.IsNullOrEmpty(LightInfo))
                        {
                            switch (LightCommand.CommandToken.Trim())
                            {
                                case "prestart":
                                    thisPhase = SpecialLightsPhase.PreStart;
                                    setphase = true;
                                    break;

                                case "active":
                                    thisPhase = SpecialLightsPhase.Active;
                                    setphase = true;
                                    break;

                                case "postform":
                                    thisPhase = SpecialLightsPhase.PostForms;
                                    setphase = true;
                                    break;

                                case "fulltime":
                                    thisPhase = SpecialLightsPhase.PreStart;
                                    useFulltime = true;
                                    break;

                                default:
                                    Trace.TraceInformation("Train {0} : invalid phase for light definition : {1} \n", thisTTTrain.Name, LightCommand.CommandToken);
                                    validLight = false;
                                    break;
                            }

                            if (useFulltime && setphase)
                            {
                                setphase = false;
                                Trace.TraceInformation("Train {0} : both phase ${1} and $Fulltime defined, phase definition is ignored \n", thisTTTrain.Name, LightCommand.CommandToken);
                            }
                        }
                        else
                        {
                            validLight = false;
                            Trace.TraceInformation("Train {0} : missing phase in light definition \n", thisTTTrain);
                        }

                        if (validLight)
                        {
                            if ((LightCommand.CommandValues == null || LightCommand.CommandValues.Count < 0) && (LightCommand.CommandQualifiers == null || LightCommand.CommandQualifiers.Count < 0))
                            {
                                if (thisPhase == SpecialLightsPhase.PreStart)
                                {
                                    useRunningforPrestart = true;
                                }
                                else if (thisPhase == SpecialLightsPhase.Active)
                                {
                                    usePostFormforRUnning = true;
                                }
                                else
                                {
                                    validLight = false;
                                    Trace.TraceInformation("Train {0} : no light conditions defined for phase {1} \n", thisTTTrain.Name, thisPhase.ToString());
                                }
                            }
                        }

                        bool reqProcessing = true;
                        if (thisPhase == SpecialLightsPhase.PreStart && useRunningforPrestart) reqProcessing = false;
                        if (thisPhase == SpecialLightsPhase.Active && usePostFormforRUnning) reqProcessing = false;

                        if (validLight && reqProcessing)
                        {
                            if (LightCommand.CommandQualifiers == null || LightCommand.CommandQualifiers.Count < 0)
                            {
                                validLight = false;
                                Trace.TraceInformation("Train {0} : no light conditions defined for phase {1} \n", thisTTTrain.Name, thisPhase.ToString());
                            }
                            else if (LightCommand.CommandQualifiers.Count > 1)
                            {
                                validLight = false;
                                Trace.TraceInformation("Train {0} : multiple light conditions defined for phase {1} \n", thisTTTrain.Name, thisPhase.ToString());
                            }
                            else
                            {
                                switch (LightCommand.CommandQualifiers[0].QualifierName.Trim())
                                {
                                    case "normal":
                                        SpecialLights[(int)thisPhase] = SpecialLightsCondition.Normal;
                                        break;

                                    case "off":
                                        SpecialLights[(int)thisPhase] = SpecialLightsCondition.Off;
                                        break;

                                    case "special_additional":
                                        SpecialLights[(int)thisPhase] = SpecialLightsCondition.Special_additional;
                                        break;

                                    case "special_only":
                                        SpecialLights[(int)thisPhase] = SpecialLightsCondition.Special_only;
                                        break;

                                    default:
                                        Trace.TraceInformation("Train {0} : invalid light conditions defined for phase {1} : {2} \n",
                                                                 thisTTTrain.Name, thisPhase.ToString(), LightCommand.CommandQualifiers[0].QualifierName);
                                        validLight = false;
                                        break;
                                }
                            }
                        }

                        if (validLight && reqProcessing &&
                                (SpecialLights[(int)thisPhase] == SpecialLightsCondition.Special_additional || SpecialLights[(int)thisPhase] == SpecialLightsCondition.Special_only))
                        {
                            if (LightCommand.CommandValues == null || LightCommand.CommandValues.Count < 0)
                            {
                                Trace.TraceInformation("Train {0} : missing special light definition for phase {1} \n", thisTTTrain.Name, thisPhase.ToString());
                                validLight = false;
                            }
                            else
                            {
                                foreach (string thisString in LightCommand.CommandValues)
                                {
                                    SpecialLightSelection[(int)thisPhase].Add(string.Copy(thisString));
                                }
                            }
                        }

                        if (validLight) validLights[(int)thisPhase] = true;
                    }
                }

                if (useRunningforPrestart && validLights[(int)SpecialLightsPhase.Active])
                {
                    SpecialLights[(int)SpecialLightsPhase.PreStart] = SpecialLights[(int)SpecialLightsPhase.Active];
                    SpecialLightSelection[(int)SpecialLightsPhase.PreStart] = SpecialLightSelection[(int)SpecialLightsPhase.Active];
                }
                if (usePostFormforRUnning && validLights[(int)SpecialLightsPhase.PostForms])
                {
                    SpecialLights[(int)SpecialLightsPhase.Active] = SpecialLights[(int)SpecialLightsPhase.PostForms];
                    SpecialLightSelection[(int)SpecialLightsPhase.Active] = SpecialLightSelection[(int)SpecialLightsPhase.PostForms];
                }
                if (useFulltime && validLights[(int)SpecialLightsPhase.PreStart])
                {
                    SpecialLights[(int)SpecialLightsPhase.Active] = SpecialLights[(int)SpecialLightsPhase.PreStart];
                    SpecialLightSelection[(int)SpecialLightsPhase.Active] = SpecialLightSelection[(int)SpecialLightsPhase.PreStart];
                    SpecialLights[(int)SpecialLightsPhase.PostForms] = SpecialLights[(int)SpecialLightsPhase.PreStart];
                    SpecialLightSelection[(int)SpecialLightsPhase.PostForms] = SpecialLightSelection[(int)SpecialLightsPhase.PreStart];
                }

                // set light conditions to train and to each car in consist, assume prestart

                thisTTTrain.TrainSpecialLights = SpecialLights;
                thisTTTrain.TrainSpecialLightSelection = SpecialLightSelection;

                foreach (var car in thisTTTrain.Cars)
                {
                    car.SpecialLights = SpecialLights[(int)SpecialLightsPhase.PreStart];
                    car.SpecialLightSelection = SpecialLightSelection[(int)SpecialLightsPhase.PreStart];
                }
            }

            //================================================================================================//
            /// <summary>
            /// Extract and process consist info from train details
            /// </summary>
            /// <param name="consistDef"></param>
            /// <returns></returns>
            public List<consistInfo> ProcessConsistInfo(string consistDef)
            {
                List<consistInfo> consistDetails = new List<consistInfo>();
                string consistProc = String.Copy(consistDef).Trim();

                while (!String.IsNullOrEmpty(consistProc))
                {
                    if (consistProc.Substring(0, 1).Equals("<"))
                    {
                        int endIndex = consistProc.IndexOf('>');
                        if (endIndex < 0)
                        {
                            Trace.TraceWarning("Incomplete consist definition : \">\" character missing : {0}", consistProc);
                            consistInfo thisConsist = new consistInfo
                            {
                                consistFile = String.Copy(consistProc.Substring(1)),
                                reversed = false
                            };
                            consistDetails.Add(thisConsist);
                            consistProc = String.Empty;
                        }
                        else
                        {
                            consistInfo thisConsist = new consistInfo
                            {
                                consistFile = String.Copy(consistProc.Substring(1, endIndex - 1)),
                                reversed = false
                            };
                            consistDetails.Add(thisConsist);
                            consistProc = consistProc.Substring(endIndex + 1).Trim();
                        }
                    }
                    else if (consistProc.Substring(0, 1).Equals("$"))
                    {
                        if (consistProc.Length >= 8 && consistProc.Substring(1, 7).Equals("reverse"))
                        {
                            if (consistDetails.Count > 0)
                            {
                                consistInfo thisConsist = consistDetails[consistDetails.Count - 1];
                                consistDetails.RemoveAt(consistDetails.Count - 1);
                                thisConsist.reversed = true;
                                consistDetails.Add(thisConsist);
                            }
                            else
                            {
                                Trace.TraceInformation("Invalid conmand at start of consist string {0}, command ingored", consistProc);
                            }
                            consistProc = consistProc.Substring(8).Trim();
                        }
                        else
                        {
                            Trace.TraceWarning("Invalid command in consist string : {0}", consistProc);
                            consistProc = String.Empty;
                        }
                    }
                    else
                    {
                        int plusIndex = consistProc.IndexOf('+');
                        if (plusIndex == 0)
                        {
                            consistProc = consistProc.Substring(1).Trim();
                        }
                        else if (plusIndex > 0)
                        {
                            consistInfo thisConsist = new consistInfo
                            {
                                consistFile = String.Copy(consistProc.Substring(0, plusIndex).Trim())
                            };

                            int sepIndex = thisConsist.consistFile.IndexOf('$');
                            if (sepIndex > 0)
                            {
                                consistProc = String.Concat(thisConsist.consistFile.Substring(sepIndex).Trim(), consistProc.Substring(plusIndex).Trim());
                                thisConsist.consistFile = thisConsist.consistFile.Substring(0, sepIndex).Trim();
                            }
                            else
                            {
                                consistProc = consistProc.Substring(plusIndex + 1).Trim();
                            }
                            thisConsist.reversed = false;
                            consistDetails.Add(thisConsist);
                        }
                        else
                        {
                            consistInfo thisConsist = new consistInfo
                            {
                                consistFile = String.Copy(consistProc)
                            };

                            int sepIndex = consistProc.IndexOf('$');
                            if (sepIndex > 0)
                            {
                                thisConsist.consistFile = consistProc.Substring(0, sepIndex).Trim();
                                consistProc = consistProc.Substring(sepIndex).Trim();
                            }
                            else
                            {
                                consistProc = String.Empty;
                            }
                            thisConsist.reversed = false;
                            consistDetails.Add(thisConsist);
                        }
                    }
                }

                return consistDetails;
            }

            //================================================================================================//
            /// <summary>
            /// Build train consist
            /// </summary>
            /// <param name="consistFile">Defined consist file</param>
            /// <param name="trainsetDirectory">Consist directory</param>
            /// <param name="simulator">Simulator</param>
            public bool BuildConsist(List<consistInfo> consistSets, string trainsetDirectory, string consistDirectory, Simulator simulator)
            {
                thisTTTrain.IsTilting = true;

                float? confMaxSpeed = null;
                thisTTTrain.Length = 0.0f;

                foreach (consistInfo consistDetails in consistSets)
                {
                    string consistFile = Path.Combine(consistDirectory, consistDetails.consistFile);

                    string pathExtension = Path.GetExtension(consistFile);
                    if (String.IsNullOrEmpty(pathExtension))
                        consistFile = Path.ChangeExtension(consistFile, "con");

                    if (!consistFile.Contains("tilted"))
                    {
                        thisTTTrain.IsTilting = false;
                    }

                    ConsistFile conFile = null;

                    // Try to load config file, exit if failed
                    try
                    {
                        conFile = new ConsistFile(consistFile);
                    }
                    catch (Exception e)
                    {
                        if (!reportedConsistFailures.Contains(consistFile.ToString()))
                        {
                            Trace.TraceInformation("Reading " + consistFile.ToString() + " : " + e.ToString());
                            reportedConsistFailures.Add(consistFile.ToString());
                            return false;
                        }
                    }

                    thisTTTrain.TcsParametersFileName = conFile.Train.TrainCfg.TcsParametersFileName;

                    AddWagons(conFile, consistDetails, trainsetDirectory, simulator);

                    // Derive speed
                    if (conFile.Train.TrainCfg.MaxVelocity != null && conFile.Train.TrainCfg.MaxVelocity.A > 0)
                    {
                        confMaxSpeed = confMaxSpeed.HasValue
                            ? Math.Min(confMaxSpeed.Value, conFile.Train.TrainCfg.MaxVelocity.A)
                            : Math.Min((float)simulator.TRK.Tr_RouteFile.SpeedLimit, conFile.Train.TrainCfg.MaxVelocity.A);
                    }
                }

                if (thisTTTrain.Cars.Count <= 0)
                {
                    Trace.TraceInformation("Empty consists for train " + thisTTTrain.Name + " : train removed");
                    validTrain = false;
                }

                // Set train details
                thisTTTrain.CheckFreight();
                thisTTTrain.SetDPUnitIDs();
                thisTTTrain.ReinitializeEOT();
                thisTTTrain.HasDirectionalPantographs = thisTTTrain.CheckDirectionalPantographs();
                thisTTTrain.SpeedSettings.routeSpeedMpS = (float)simulator.TRK.Tr_RouteFile.SpeedLimit;

                if (!confMaxSpeed.HasValue || confMaxSpeed.Value <= 0f)
                {
                    float tempMaxSpeedMpS = thisTTTrain.TrainMaxSpeedMpS;

                    foreach (TrainCar car in thisTTTrain.Cars)
                    {
                        float engineMaxSpeedMpS = 0;
                        if (car is MSTSLocomotive locomotive)
                            engineMaxSpeedMpS = locomotive.MaxSpeedMpS;
                        if (car is MSTSElectricLocomotive electricLocomotive)
                            engineMaxSpeedMpS = electricLocomotive.MaxSpeedMpS;
                        if (car is MSTSDieselLocomotive dieselLocomotive)
                            engineMaxSpeedMpS = dieselLocomotive.MaxSpeedMpS;
                        if (car is MSTSSteamLocomotive steamLocomotive)
                            engineMaxSpeedMpS = steamLocomotive.MaxSpeedMpS;

                        if (engineMaxSpeedMpS > 0)
                        {
                            tempMaxSpeedMpS = Math.Min(tempMaxSpeedMpS, engineMaxSpeedMpS);
                        }
                    }

                    thisTTTrain.SpeedSettings.consistSpeedMpS = tempMaxSpeedMpS;
                }
                else
                {
                    thisTTTrain.SpeedSettings.consistSpeedMpS = confMaxSpeed.Value;
                }

                return true;
            }

            /// <summary>
            /// Add wagons from consist file to traincar list
            /// </summary>
            /// <param name="consistFile">Processed consist File</param>
            /// <param name="trainsDirectory">Consist Directory</param>
            /// <param name="simulator">Simulator</param>
            public void AddWagons(ConsistFile consistFile, consistInfo consistDetails, string trainsDirectory, Simulator simulator)
            {
                int carId = 0;

                List<Wagon> wagonList = consistDetails.reversed
                    ? consistFile.Train.TrainCfg.WagonList.AsEnumerable().Reverse().ToList()
                    : consistFile.Train.TrainCfg.WagonList;

                // Add wagons
                foreach (Wagon wagon in wagonList)
                {
                    string wagonFolder = Path.Combine(trainsDirectory, wagon.Folder);
                    string wagonFilePath = Path.Combine(wagonFolder, wagon.Name + ".wag");

                    TrainCar car = null;

                    if (wagon.IsEngine)
                        wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");
                    else if (wagon.IsEOT)
                    {
                        wagonFolder = simulator.BasePath + @"\trains\orts_eot\" + wagon.Folder;
                        wagonFilePath = wagonFolder + @"\" + wagon.Name + ".eot";
                    }

                    if (!File.Exists(wagonFilePath))
                    {
                        Trace.TraceWarning($"Ignored missing {(wagon.IsEngine ? "engine" : "wagon")} {wagonFilePath} in consist {consistFile}");
                        continue;
                    }

                    car = RollingStock.Load(simulator, thisTTTrain, wagonFilePath);
                    car.UiD = wagon.UiD;
                    car.Flipped = consistDetails.reversed ? !wagon.Flip : wagon.Flip;
                    car.CarID = string.Concat(thisTTTrain.Number.ToString("0###"), "_", carId.ToString("0##"));
                    carId++;
                    car.OrgConsist = string.Copy(consistDetails.consistFile).ToLower();

                    thisTTTrain.Length += car.CarLengthM;
                    if (car is EOT)
                        thisTTTrain.EOT = car as EOT;
                }
            }

            //================================================================================================//
            /// <summary>
            /// Process station stop info cell including possible commands
            /// Info may consist of:
            ///  - one or two time values (arr / dep time or pass time)
            ///  - commands
            ///  - time values and commands
            /// </summary>
            /// <param name="stationInfo">Reference to station string</param>
            /// <param name="stationName">Station Details class</param>
            /// <returns> StopInfo structure</returns>
            public StopInfo ProcessStopInfo(string stationInfo, StationInfo stationDetails)
            {
                string[] arr_dep = new string[2] { String.Empty, String.Empty };
                string fullCommandString = String.Empty;

                if (stationInfo.Contains('$'))
                {
                    int commandseparator = stationInfo.IndexOf('$');
                    fullCommandString = stationInfo.Substring(commandseparator + 1);
                    stationInfo = stationInfo.Substring(0, commandseparator);
                }

                if (!String.IsNullOrEmpty(stationInfo))
                {
                    if (stationInfo.Contains('-'))
                    {
                        arr_dep = stationInfo.Split(new char[1] { '-' }, 2);
                    }
                    else
                    {
                        arr_dep[0] = String.Copy(stationInfo);
                        arr_dep[1] = String.Copy(stationInfo);
                    }
                }

                StopInfo newStop = new StopInfo(stationDetails.StationName, arr_dep[0], arr_dep[1], parentInfo)
                {
                    holdState = stationDetails.HoldState == StationInfo.HoldInfo.Hold ? StopInfo.SignalHoldType.Normal : StopInfo.SignalHoldType.None,
                    noWaitSignal = stationDetails.NoWaitSignal
                };

                if (!String.IsNullOrEmpty(fullCommandString))
                {
                    newStop.Commands = new List<TTTrainCommands>();
                    string[] commandStrings = fullCommandString.Split('$');
                    foreach (string thisCommand in commandStrings)
                    {
                        newStop.Commands.Add(new TTTrainCommands(thisCommand));
                    }
                }

                // Process forced stop through station commands
                if (stationDetails.HoldState == StationInfo.HoldInfo.ForceHold)
                {
                    if (newStop.Commands == null)
                    {
                        newStop.Commands = new List<TTTrainCommands>();
                    }

                    newStop.Commands.Add(new TTTrainCommands("forcehold"));
                }

                // Process forced wait signal command
                if (stationDetails.ForceWaitSignal)
                {
                    if (newStop.Commands == null)
                    {
                        newStop.Commands = new List<TTTrainCommands>();
                    }

                    newStop.Commands.Add(new TTTrainCommands("forcewait"));
                }

                // Process terminal through station commands
                if (stationDetails.IsTerminal)
                {
                    if (newStop.Commands == null)
                    {
                        newStop.Commands = new List<TTTrainCommands>();
                    }

                    newStop.Commands.Add(new TTTrainCommands("terminal"));
                }

                // PProcess closeupsignal through station commands
                if (stationDetails.CloseupSignal)
                {
                    if (newStop.Commands == null)
                    {
                        newStop.Commands = new List<TTTrainCommands>();
                    }

                    newStop.Commands.Add(new TTTrainCommands("closeupsignal"));
                }

                // Process actual min stop time
                if (stationDetails.actMinStopTime.HasValue)
                {
                    if (newStop.Commands == null)
                    {
                        newStop.Commands = new List<TTTrainCommands>();
                    }

                    newStop.Commands.Add(new TTTrainCommands(String.Concat("stoptime=", stationDetails.actMinStopTime.Value.ToString().Trim())));
                }

                // Process restrict to signal
                if (stationDetails.RestrictPlatformToSignal)
                {
                    if (newStop.Commands == null)
                    {
                        newStop.Commands = new List<TTTrainCommands>();
                    }

                    newStop.Commands.Add(new TTTrainCommands("restrictplatformtosignal"));
                }

                // Process restrict to signal
                if (stationDetails.ExtendPlatformToSignal)
                {
                    if (newStop.Commands == null)
                    {
                        newStop.Commands = new List<TTTrainCommands>();
                    }

                    newStop.Commands.Add(new TTTrainCommands("extendplatformtosignal"));
                }

                return newStop;
            }

            //================================================================================================//
            /// <summary>
            /// Convert station stops to train stationStop info
            /// </summary>
            /// <param name="simulator"></param>
            /// <param name="actTrain"></param>
            /// <param name="name"></param>
            public void ConvertStops(Simulator simulator, TTTrain actTrain, string name)
            {
                foreach (KeyValuePair<string, StopInfo> stationStop in Stops)
                {
                    if (actTrain.TCRoute.StationXRef.ContainsKey(stationStop.Key))
                    {
                        StopInfo stationInfo = stationStop.Value;
                        int[] platformInfo = actTrain.TCRoute.StationXRef[stationStop.Key];
                        bool ValidStop = stationInfo.BuildStopInfo(actTrain, platformInfo[2], simulator.Signals);
                        if (!ValidStop)
                        {
                            Trace.TraceInformation("Station {0} not found for train {1}:{2} ", stationStop.Key, Name, TTDescription);
                        }
                        actTrain.TCRoute.StationXRef.Remove(stationStop.Key);
                    }
                    else
                    {
                        Trace.TraceInformation("Station {0} not found for train {1}:{2} ", stationStop.Key, Name, TTDescription);
                    }
                }
                actTrain.TCRoute.StationXRef.Clear(); // Info no longer required
            }

            //================================================================================================//
            /// <summary>
            /// Process Timetable commands entered as general notes
            /// All commands are valid from start of route
            /// </summary>
            /// <param name="simulator"></param>
            /// <param name="actTrain"></param>
            public void ProcessCommands(Simulator simulator, TTTrain actTTTrain)
            {
                foreach (TTTrainCommands thisCommand in TrainCommands)
                {
                    switch (thisCommand.CommandToken)
                    {
                        case "acc":
                            try
                            {
                                actTTTrain.MaxAccelMpSSP = actTTTrain.DefMaxAccelMpSSP * Convert.ToSingle(thisCommand.CommandValues[0]);
                                actTTTrain.MaxAccelMpSSF = actTTTrain.DefMaxAccelMpSSF * Convert.ToSingle(thisCommand.CommandValues[0]);
                            }
                            catch
                            {
                                Trace.TraceInformation("Train {0} : invalid value for '{1}' setting : {2} \n",
                                    actTTTrain.Name, thisCommand.CommandToken, thisCommand.CommandValues[0]);
                            }
                            break;

                        case "dec":
                            try
                            {
                                actTTTrain.MaxDecelMpSSP = actTTTrain.DefMaxDecelMpSSP * Convert.ToSingle(thisCommand.CommandValues[0]);
                                actTTTrain.MaxDecelMpSSF = actTTTrain.DefMaxDecelMpSSF * Convert.ToSingle(thisCommand.CommandValues[0]);
                            }
                            catch
                            {
                                Trace.TraceInformation("Train {0} : invalid value for '{1}' setting : {2} \n",
                                    actTTTrain.Name, thisCommand.CommandToken, thisCommand.CommandValues[0]);
                            }
                            break;

                        case "doo":
                            actTTTrain.DriverOnlyOperation = true;
                            break;

                        case "forcereversal":
                            actTTTrain.ForceReversal = true;
                            break;

                        case "reverse_nopantoswitch":
                            actTTTrain.NoPantoSwitchOnReverse = true;
                            break;

                        default:
                            actTTTrain.ProcessTimetableStopCommands(thisCommand, 0, -1, -1, -1, parentInfo);
                            break;
                    }
                }
            }

            //================================================================================================//
            /// <summary>
            /// Extract and process dispose info
            /// </summary>
            /// <param name="trainList"></param>
            /// <param name="playerTrain"></param>
            /// <param name="simulator"></param>
            /// <returns></returns>
            public bool ProcessDisposeInfo(ref List<TTTrain> trainList, TTTrainInfo playerTrain, Simulator simulator)
            {
                bool loadPathNoFailure = true;
                TTTrain formedTrain = null;
                TTTrain.FormCommand formtype = TTTrain.FormCommand.None;
                bool trainFound = false;

                // Set closeup if required
                thisTTTrain.Closeup = DisposeDetails.Closeup;

                // Train forms other train
                if (DisposeDetails.FormType == TTTrain.FormCommand.TerminationFormed || DisposeDetails.FormType == TTTrain.FormCommand.TerminationTriggered)
                {
                    formtype = DisposeDetails.FormType;
                    string[] otherTrainName = null;
                    thisTTTrain.PowerOffOnFormed = DisposeDetails.PowerOffOnForms;
                    thisTTTrain.FormedPowerOffDelay = DisposeDetails.FormedPowerOffDelay;

                    if (DisposeDetails.FormedTrain == null)
                    {
                        Trace.TraceInformation("Error in dispose details for train : " + Name + " : no formed train defined");
                        return true;
                    }

                    // Extract name
                    if (DisposeDetails.FormedTrain.Contains('='))
                    {
                        otherTrainName = DisposeDetails.FormedTrain.Split('='); // extract train name
                    }
                    else
                    {
                        otherTrainName = new string[2];
                        otherTrainName[1] = String.Copy(DisposeDetails.FormedTrain);
                    }

                    if (otherTrainName[1].Contains('/'))
                    {
                        int splitPosition = otherTrainName[1].IndexOf('/');
                        otherTrainName[1] = otherTrainName[1].Substring(0, splitPosition);
                    }

                    if (!otherTrainName[1].Contains(':'))
                    {
                        string[] timetableName = thisTTTrain.Name.Split(':');
                        otherTrainName[1] = String.Concat(otherTrainName[1], ":", timetableName[1]);
                    }

                    // Search train
                    foreach (TTTrain otherTrain in trainList)
                    {
                        if (String.Compare(otherTrain.Name, otherTrainName[1], true) == 0)
                        {
                            if (otherTrain.FormedOf >= 0)
                            {
                                Trace.TraceWarning("Train : {0} : dispose details : formed train {1} already formed out of another train",
                                    thisTTTrain.Name, otherTrain.Name);
                                break;
                            }

                            thisTTTrain.Forms = otherTrain.Number;
                            thisTTTrain.SetStop = DisposeDetails.SetStop;
                            thisTTTrain.FormsAtStation = DisposeDetails.FormsAtStation;
                            otherTrain.FormedOf = thisTTTrain.Number;
                            otherTrain.FormedOfType = DisposeDetails.FormType;
                            trainFound = true;
                            formedTrain = otherTrain;
                            break;
                        }
                    }

                    // If not found, try player train
                    if (!trainFound)
                    {
                        if (playerTrain != null && String.Compare(playerTrain.thisTTTrain.Name, otherTrainName[1], true) == 0)
                        {
                            if (playerTrain.thisTTTrain.FormedOf >= 0)
                            {
                                Trace.TraceWarning("Train : {0} : dispose details : formed train {1} already formed out of another train",
                                    thisTTTrain.Name, playerTrain.Name);
                            }

                            thisTTTrain.Forms = playerTrain.thisTTTrain.Number;
                            thisTTTrain.SetStop = DisposeDetails.SetStop;
                            thisTTTrain.FormsAtStation = DisposeDetails.FormsAtStation;
                            playerTrain.thisTTTrain.FormedOf = thisTTTrain.Number;
                            playerTrain.thisTTTrain.FormedOfType = DisposeDetails.FormType;
                            trainFound = true;
                            formedTrain = playerTrain.thisTTTrain;
                        }
                    }

                    if (!trainFound)
                    {
                        Trace.TraceWarning("Train :  {0} : Dispose details : formed train {1} not found",
                            thisTTTrain.Name, otherTrainName[1]);
                    }

#if DEBUG_TRACEINFO
                    if (trainFound)
                    {
                        Trace.TraceInformation("Dispose : {0} {1} {2} ", thisTTTrain.Name, DisposeDetails.FormType.ToString(), otherTrainName[1]);
                    }
                    else
                    {
                        Trace.TraceInformation("Dispose : {0} : cannot find {1} ", thisTTTrain.Name, otherTrainName[1]);
                    }
#endif
                }

                TTTrain outTrain = null;
                TTTrain inTrain = null;

                // Check if train must be stabled
                if (DisposeDetails.Stable && (trainFound || DisposeDetails.FormStatic))
                {
                    // Save final train
                    int finalForms = thisTTTrain.Forms;

                    // Create outbound train (note: train is defined WITHOUT consist as it is formed of incoming train)
                    outTrain = new TTTrain(simulator, thisTTTrain);

                    bool addPathNoLoadFailure;
                    AIPath outPath = parentInfo.LoadPath(DisposeDetails.StableInfo.Stable_outpath, out addPathNoLoadFailure);
                    if (!addPathNoLoadFailure)
                    {
                        loadPathNoFailure = false;
                    }
                    else
                    {
                        outTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, outPath);
                        outTrain.Path = outPath;
                        outTrain.CreateRoute(false);
                        outTrain.ValidRoute[0] = new Train.TCSubpathRoute(outTrain.TCRoute.TCRouteSubpaths[0]);
                        outTrain.AITrainDirectionForward = true;
                        outTrain.StartTime = DisposeDetails.StableInfo.Stable_outtime;
                        outTrain.ActivateTime = DisposeDetails.StableInfo.Stable_outtime;
                        if (String.IsNullOrEmpty(DisposeDetails.StableInfo.Stable_name))
                        {
                            outTrain.Name = String.Concat("SO_", thisTTTrain.Number.ToString("0000"));
                        }
                        else
                        {
                            outTrain.Name = String.Copy(DisposeDetails.StableInfo.Stable_name.ToLower());
                            if (!outTrain.Name.Contains(":"))
                            {
                                int seppos = thisTTTrain.Name.IndexOf(':');
                                outTrain.Name = String.Concat(outTrain.Name, ":", thisTTTrain.Name.Substring(seppos + 1).ToLower());
                            }
                        }
                        outTrain.FormedOf = thisTTTrain.Number;
                        outTrain.FormedOfType = TTTrain.FormCommand.TerminationFormed;
                        outTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
                        if (DisposeDetails.DisposeSpeed != null)
                        {
                            outTrain.SpeedSettings.maxSpeedMpS = DisposeDetails.DisposeSpeed.Value;
                            outTrain.SpeedSettings.restrictedSet = true;
                            outTrain.ProcessSpeedSettings();
                        }
                        trainList.Add(outTrain);

                        thisTTTrain.Forms = outTrain.Number;
                    }

                    // If stable to static
                    if (DisposeDetails.FormStatic)
                    {
                        outTrain.FormsStatic = true;
                    }
                    else
                    {
                        outTrain.FormsStatic = false;

                        // Create inbound train
                        inTrain = new TTTrain(simulator, thisTTTrain);

                        AIPath inPath = parentInfo.LoadPath(DisposeDetails.StableInfo.Stable_inpath, out addPathNoLoadFailure);
                        if (!addPathNoLoadFailure)
                        {
                            loadPathNoFailure = false;
                        }
                        else
                        {
                            inTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, inPath);
                            inTrain.Path = inPath;
                            inTrain.CreateRoute(false);
                            inTrain.ValidRoute[0] = new Train.TCSubpathRoute(inTrain.TCRoute.TCRouteSubpaths[0]);
                            inTrain.AITrainDirectionForward = true;
                            inTrain.StartTime = DisposeDetails.StableInfo.Stable_intime;
                            inTrain.ActivateTime = DisposeDetails.StableInfo.Stable_intime;
                            inTrain.Name = String.Concat("SI_", finalForms.ToString("0000"));
                            inTrain.FormedOf = outTrain.Number;
                            inTrain.FormedOfType = DisposeDetails.FormType; // Set forms or triggered as defined in stable
                            inTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
                            inTrain.Forms = finalForms;
                            inTrain.SetStop = DisposeDetails.SetStop;
                            inTrain.FormsStatic = false;
                            inTrain.Stable_CallOn = DisposeDetails.CallOn;
                            if (DisposeDetails.DisposeSpeed != null)
                            {
                                inTrain.SpeedSettings.maxSpeedMpS = DisposeDetails.DisposeSpeed.Value;
                                inTrain.SpeedSettings.restrictedSet = true;
                                inTrain.ProcessSpeedSettings();
                            }

                            trainList.Add(inTrain);

                            outTrain.Forms = inTrain.Number;

                            formtype = inTrain.FormedOfType;

                            // Set back reference from final train

                            formedTrain.FormedOf = inTrain.Number;
                            formedTrain.FormedOfType = TTTrain.FormCommand.TerminationFormed;

                            Train.TCSubpathRoute lastSubpath = inTrain.TCRoute.TCRouteSubpaths[inTrain.TCRoute.TCRouteSubpaths.Count - 1];
                            if (inTrain.FormedOfType == TTTrain.FormCommand.TerminationTriggered && formedTrain.Number != 0) // No need to set consist for player train
                            {
                                bool reverseTrain = CheckFormedReverse(lastSubpath, formedTrain.TCRoute.TCRouteSubpaths[0]);
                                BuildStabledConsist(ref inTrain, formedTrain.Cars, formedTrain.TCRoute.TCRouteSubpaths[0], reverseTrain);
                            }
                        }
                    }
                }

                // If run round required, build runround
                if (formtype == TTTrain.FormCommand.TerminationFormed && trainFound && DisposeDetails.RunRound)
                {
                    TTTrain usedTrain;
                    bool atStart = false; // Indicates if run-round is to be performed before start of move or forms, or at end of move

                    if (DisposeDetails.Stable)
                    {
                        switch (DisposeDetails.RunRoundPos)
                        {
                            case DisposeInfo.RunRoundPosition.outposition:
                                usedTrain = outTrain;
                                atStart = true;
                                break;

                            case DisposeInfo.RunRoundPosition.inposition:
                                usedTrain = inTrain;
                                atStart = false;
                                break;

                            default:
                                usedTrain = inTrain;
                                atStart = true;
                                break;
                        }
                    }
                    else
                    {
                        usedTrain = formedTrain;
                        atStart = true;
                    }

                    bool addPathNoLoadFailure = BuildRunRound(ref usedTrain, atStart, DisposeDetails, simulator, ref trainList);
                    if (!addPathNoLoadFailure) loadPathNoFailure = false;
                }

                // Static
                if (DisposeDetails.FormStatic)
                {
                    thisTTTrain.FormsStatic = true;
                    thisTTTrain.PowerOffOnFormed = DisposeDetails.PowerOffOnForms;
                    thisTTTrain.FormedPowerOffDelay = DisposeDetails.FormedPowerOffDelay;
                }

                // Pool
                if (DisposeDetails.Pool)
                {
                    // Check pool name
                    if (!simulator.PoolHolder.Pools.ContainsKey(DisposeDetails.PoolName))
                    {
                        Trace.TraceInformation("Train : " + thisTTTrain.Name + " : reference to unkown pool in dispose command : " + DisposeDetails.PoolName + "\n");
                    }
                    else
                    {
                        thisTTTrain.ExitPool = String.Copy(DisposeDetails.PoolName);

                        switch (DisposeDetails.PoolExitDirection)
                        {
                            case "backward":
                                thisTTTrain.PoolExitDirection = TimetablePool.PoolExitDirectionEnum.Backward;
                                break;

                            case "forward":
                                thisTTTrain.PoolExitDirection = TimetablePool.PoolExitDirectionEnum.Forward;
                                break;

                            default:
                                thisTTTrain.PoolExitDirection = TimetablePool.PoolExitDirectionEnum.Undefined;
                                break;
                        }
                    }
                }
                return loadPathNoFailure;
            }

            //================================================================================================//
            /// <summary>
            /// Build run round details and train
            /// </summary>
            /// <param name="rrtrain"></param>
            /// <param name="atStart"></param>
            /// <param name="disposeDetails"></param>
            /// <param name="simulator"></param>
            /// <param name="trainList"></param>
            /// <param name="paths"></param>
            public bool BuildRunRound(ref TTTrain rrtrain, bool atStart, DisposeInfo disposeDetails, Simulator simulator, ref List<TTTrain> trainList)
            {
                bool loadPathNoFailure = true;
                TTTrain formedTrain = new TTTrain(simulator, thisTTTrain);

                string pathDirectory = Path.Combine(simulator.RoutePath, "Paths");
                string formedpathFilefull = Path.Combine(pathDirectory, DisposeDetails.RunRoundPath);
                string pathExtension = Path.GetExtension(formedpathFilefull);
                if (String.IsNullOrEmpty(pathExtension))
                    formedpathFilefull = Path.ChangeExtension(formedpathFilefull, "pat");

                bool addPathNoLoadFailure;
                AIPath formedPath = parentInfo.LoadPath(formedpathFilefull, out addPathNoLoadFailure);
                if (!addPathNoLoadFailure)
                {
                    loadPathNoFailure = false;
                }
                else
                {
                    formedTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, formedPath);
                    formedTrain.Path = formedPath;
                    formedTrain.CreateRoute(false);
                    formedTrain.ValidRoute[0] = new Train.TCSubpathRoute(formedTrain.TCRoute.TCRouteSubpaths[0]);
                    formedTrain.AITrainDirectionForward = true;
                    formedTrain.Name = String.Concat("RR_", rrtrain.Number.ToString("0000"));
                    formedTrain.FormedOf = rrtrain.Number;
                    formedTrain.FormedOfType = TTTrain.FormCommand.Detached;
                    formedTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
                    if (disposeDetails.DisposeSpeed != null)
                    {
                        formedTrain.SpeedSettings.maxSpeedMpS = disposeDetails.DisposeSpeed.Value;
                        formedTrain.SpeedSettings.restrictedSet = true;
                        formedTrain.ProcessSpeedSettings();
                    }

                    formedTrain.Closeup = disposeDetails.RunRoundCloseup;
                    formedTrain.ForceReversal = disposeDetails.RunRoundForceReversal;

                    formedTrain.AttachDetails = new AttachInfo(rrtrain);
                    trainList.Add(formedTrain);

                    Train.TCSubpathRoute lastSubpath = rrtrain.TCRoute.TCRouteSubpaths[rrtrain.TCRoute.TCRouteSubpaths.Count - 1];
                    if (atStart) lastSubpath = rrtrain.TCRoute.TCRouteSubpaths[0]; // if runround at start use first subpath

                    bool reverseTrain = CheckFormedReverse(lastSubpath, formedTrain.TCRoute.TCRouteSubpaths[0]);

                    if (atStart)
                    {
                        int? rrtime = disposeDetails.RunRoundTime;
                        DetachInfo detachDetails = new DetachInfo(true, false, false, 0, false, false, false, false, true, false, -1, rrtime, formedTrain.Number, reverseTrain);
                        if (rrtrain.DetachDetails.ContainsKey(-1))
                        {
                            List<DetachInfo> thisDetachList = rrtrain.DetachDetails[-1];
                            thisDetachList.Add(detachDetails);
                        }
                        else
                        {
                            List<DetachInfo> thisDetachList = new List<DetachInfo>
                            {
                                detachDetails
                            };
                            rrtrain.DetachDetails.Add(-1, thisDetachList);
                        }
                        formedTrain.ActivateTime = rrtime.HasValue ? (rrtime.Value + 30) : 0;
                    }
                    else
                    {
                        DetachInfo detachDetails = new DetachInfo(false, true, false, 0, false, false, false, false, true, false, -1, null, formedTrain.Number, reverseTrain);
                        if (rrtrain.DetachDetails.ContainsKey(-1))
                        {
                            List<DetachInfo> thisDetachList = rrtrain.DetachDetails[-1];
                            thisDetachList.Add(detachDetails);
                        }
                        else
                        {
                            List<DetachInfo> thisDetachList = new List<DetachInfo>
                            {
                                detachDetails
                            };
                            rrtrain.DetachDetails.Add(-1, thisDetachList);
                        }
                        formedTrain.ActivateTime = 0;
                    }
                }

                return loadPathNoFailure;
            }

            //================================================================================================//
            /// <summary>
            /// Build consist for stabled train from final train
            /// </summary>
            /// <param name="stabledTrain"></param>
            /// <param name="cars"></param>
            /// <param name="trainRoute"></param>
            private void BuildStabledConsist(ref TTTrain stabledTrain, List<TrainCar> cars, Train.TCSubpathRoute trainRoute, bool reverseTrain)
            {
                int totalreverse = 0;

                // Check no. of reversals
                foreach (Train.TCReversalInfo reversalInfo in stabledTrain.TCRoute.ReversalInfo)
                {
                    if (reversalInfo.Valid) totalreverse++;
                }

                if (reverseTrain) totalreverse++;

                // Copy consist in same or reverse direction
                if ((totalreverse % 2) == 0) // Even number, so same direction
                {
                    int carId = 0;
                    foreach (TrainCar car in cars)
                    {
                        car.Train = stabledTrain;
                        car.CarID = String.Concat(stabledTrain.Number.ToString("0###"), "_", carId.ToString("0##"));
                        carId++;
                        stabledTrain.Cars.Add(car);
                    }
                }
                else
                {
                    int carId = 0;
                    foreach (TrainCar car in cars)
                    {
                        car.Train = stabledTrain;
                        car.CarID = String.Concat(stabledTrain.Number.ToString("0###"), "_", carId.ToString("0##"));
                        carId++;
                        car.Flipped = !car.Flipped;
                        stabledTrain.Cars.Insert(0, car);
                    }
                }
            }

            //================================================================================================//
            /// <summary>
            /// Check if formed train is reversed of present train
            /// </summary>
            /// <param name="thisTrainRoute"></param>
            /// <param name="formedTrainRoute"></param>
            /// <returns></returns>
            public bool CheckFormedReverse(Train.TCSubpathRoute thisTrainRoute, Train.TCSubpathRoute formedTrainRoute)
            {
                // Get matching route sections to check on direction
                int lastElementIndex = thisTrainRoute.Count - 1;
                Train.TCRouteElement lastElement = thisTrainRoute[lastElementIndex];

                int firstElementIndex = formedTrainRoute.GetRouteIndex(lastElement.TCSectionIndex, 0);

                while (firstElementIndex < 0 && lastElementIndex > 0)
                {
                    lastElementIndex--;
                    lastElement = thisTrainRoute[lastElementIndex];
                    firstElementIndex = formedTrainRoute.GetRouteIndex(lastElement.TCSectionIndex, 0);
                }

                // If no matching sections found leave train without consist
                if (firstElementIndex < 0)
                {
                    return false;
                }

                Train.TCRouteElement firstElement = formedTrainRoute[firstElementIndex];

                // Reverse required
                return firstElement.Direction != lastElement.Direction;
            }
        }

        //================================================================================================//
        //================================================================================================//
        /// <summary>
        /// Class to hold stop info
        /// Class is used during extraction process only
        /// </summary>
        private class StopInfo
        {
            public enum SignalHoldType
            {
                None,
                Normal,
                Forced,
            }

            public string StopName;
            public int arrivalTime;
            public int departureTime;
            public int passTime;
            public DateTime arrivalDT;
            public DateTime departureDT;
            public DateTime passDT;
            public bool arrdeppassvalid;
            public SignalHoldType holdState;
            public bool noWaitSignal;
            // TODO
            // public int passageTime; // not yet implemented
            // public bool passvalid;  // not yet implemented
            public List<TTTrainCommands> Commands;

            public TimetableInfo refTTInfo;

            //================================================================================================//
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name"></param>
            /// <param name="arrTime"></param>
            /// <param name="depTime"></param>
            public StopInfo(string name, string arrTime, string depTime, TimetableInfo ttinfo)
            {
                refTTInfo = ttinfo;
                arrivalTime = -1;
                departureTime = -1;
                passTime = -1;
                Commands = null;

                TimeSpan atime;
                bool validArrTime = false;
                bool validDepTime = false;
                bool validPassTime = false;

                if (arrTime.Contains("P"))
                {
                    string passingTime = arrTime.Replace('P', ':');
                    validPassTime = TimeSpan.TryParse(passingTime, out atime);

                    if (validPassTime)
                    {
                        passTime = Convert.ToInt32(atime.TotalSeconds);
                        passDT = new DateTime(atime.Ticks);
                    }
                }
                else
                {
					
                    validArrTime = TimeSpan.TryParse(arrTime, out atime);
                    if (validArrTime)
                    {
                        arrivalTime = Convert.ToInt32(atime.TotalSeconds);
                        arrivalDT = new DateTime(atime.Ticks);
                    }
				}

                validDepTime = TimeSpan.TryParse(depTime, out atime);
                if (validDepTime)
                {
                    departureTime = Convert.ToInt32(atime.TotalSeconds);
                    departureDT = new DateTime(atime.Ticks);
                }

                arrdeppassvalid = validArrTime || validDepTime;

                StopName = String.Copy(name.ToLower());
            }

            //================================================================================================//
            /// <summary>
            /// Build station stop info
            /// </summary>
            /// <param name="actTrain"></param>
            /// <param name="TDB"></param>
            /// <param name="signalRef"></param>
            /// <returns>bool (indicating stop is found on route)</returns>
            public bool BuildStopInfo(TTTrain actTrain, int actPlatformID, Signals signalRef)
            {
                bool validStop = false;

                // Valid stop and not passing
                if (arrdeppassvalid && passTime < 0)
                {
                    // Check for station flags
                    bool terminal = false;
                    int? actMinStopTime = null;
                    float? keepClearFront = null;
                    float? keepClearRear = null;
                    bool forcePosition = false;
                    bool closeupSignal = false;
                    bool closeup = false;
                    bool restrictPlatformToSignal = false;
                    bool extendPlatformToSignal = false;
                    bool endStop = false;

                    if (Commands != null)
                    {
                        foreach (TTTrainCommands thisCommand in Commands)
                        {
                            switch (thisCommand.CommandToken)
                            {
                                case "terminal":
                                    terminal = true;
                                    break;

                                case "closeupsignal":
                                    closeupSignal = true;
                                    break;

                                case "closeup":
                                    closeup = true;
                                    break;

                                case "restrictplatformtosignal":
                                    restrictPlatformToSignal = true;
                                    break;

                                case "extendplatformtosignal":
                                    extendPlatformToSignal = true;
                                    break;

                                case "keepclear":
                                    if (thisCommand.CommandQualifiers == null || thisCommand.CommandQualifiers.Count <= 0)
                                    {
                                        Trace.TraceInformation("Train {0} : station stop : keepclear command : missing value", actTrain.Name);
                                    }
                                    else
                                    {
                                        bool setfront = false;
                                        bool setrear = false;

                                        foreach (TTTrainCommands.TTTrainComQualifiers thisQualifier in thisCommand.CommandQualifiers)
                                        {
                                            bool getPosition = false;

                                            switch (thisQualifier.QualifierName.ToLower())
                                            {
                                                case "front":
                                                    if (setrear)
                                                    {
                                                        Trace.TraceInformation("Train {0} : station stop : keepclear command : spurious value : {1}", actTrain.Name, thisQualifier.QualifierName.ToLower());
                                                    }
                                                    else
                                                    {
                                                        setfront = true;
                                                        getPosition = true;
                                                    }
                                                    break;

                                                case "rear":
                                                    if (setfront)
                                                    {
                                                        Trace.TraceInformation("Train {0} : station stop : keepclear command : spurious value : {1}", actTrain.Name, thisQualifier.QualifierName.ToLower());
                                                    }
                                                    else
                                                    {
                                                        setrear = true;
                                                        getPosition = true;
                                                    }
                                                    break;

                                                case "force":
                                                    forcePosition = true;
                                                    break;

                                                default:
                                                    Trace.TraceInformation("Train {0} : station stop : keepclear command : unknown value", actTrain.Name);
                                                    break;
                                            }

                                            if (getPosition)
                                            {
                                                if (thisQualifier.QualifierValues == null || thisQualifier.QualifierValues.Count <= 0)
                                                {
                                                    Trace.TraceInformation("Train {0} : station stop : keepclear command : missing value", actTrain.Name);
                                                }
                                                else
                                                {
                                                    float clearValue = 0.0f;
                                                    try
                                                    {
                                                        clearValue = Convert.ToSingle(thisCommand.CommandQualifiers[0].QualifierValues[0]);
                                                        if (setfront)
                                                        {
                                                            keepClearFront = clearValue;
                                                        }
                                                        else if (setrear)
                                                        {
                                                            keepClearRear = clearValue;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        Trace.TraceInformation("Train {0} : station stop : keepclear command : invalid value", actTrain.Name);
                                                    }
                                                }
                                            }
                                        }

                                        if (!setfront && !setrear)
                                        {
                                            Trace.TraceInformation("Train {0} : station stop : keepclear command : missing position definition", actTrain.Name);
                                        }
                                    }
                                    break;

                                // Train terminates at station
                                case "endstop":
                                    endStop = true;
                                    break;

                                // Required minimal stop time
                                case "stoptime":
                                    if (thisCommand.CommandValues != null && thisCommand.CommandValues.Count > 0)
                                    {
                                        try
                                        {
                                            actMinStopTime = Convert.ToInt32(thisCommand.CommandValues[0]);
                                        }
                                        catch
                                        {
                                            Trace.TraceInformation("Train {0} : station stop : invalid value for stop time", actTrain.Name);
                                        }
                                    }
                                    else
                                    {
                                        Trace.TraceInformation("Train {0} : station stop : missing value for station stop time", actTrain.Name);
                                    }
                                    break;

                                // Other commands processed in station stop handling
                                default:
                                    break;
                            }
                        }
                    }

                    // Create station stop info
                    validStop = actTrain.CreateStationStop(actPlatformID, arrivalTime, departureTime, arrivalDT, departureDT, AITrain.clearingDistanceM,
                        AITrain.minStopDistanceM, terminal, actMinStopTime, keepClearFront, keepClearRear, forcePosition, closeupSignal, closeup, restrictPlatformToSignal, extendPlatformToSignal, endStop);

                    // Override holdstate using stop info - but only if exit signal is defined
                    int exitSignal = actTrain.StationStops[actTrain.StationStops.Count - 1].ExitSignal;
                    bool holdSignal = holdState != SignalHoldType.None && (exitSignal >= 0);
                    actTrain.StationStops[actTrain.StationStops.Count - 1].HoldSignal = holdSignal;

                    // Override nosignalwait using stop info
                    actTrain.StationStops[actTrain.StationStops.Count - 1].NoWaitSignal = noWaitSignal;

                    // Process additional commands
                    if (Commands != null && validStop)
                    {
                        int sectionIndex = actTrain.StationStops[actTrain.StationStops.Count - 1].TCSectionIndex;
                        int subrouteIndex = actTrain.StationStops[actTrain.StationStops.Count - 1].SubrouteIndex;

                        foreach (TTTrainCommands thisCommand in Commands)
                        {
                            actTrain.ProcessTimetableStopCommands(thisCommand, subrouteIndex, sectionIndex, actTrain.StationStops.Count - 1, actPlatformID, refTTInfo);
                        }

                        holdSignal = actTrain.StationStops[actTrain.StationStops.Count - 1].HoldSignal;
                    }

                    // Check holdsignal list
                    if (holdSignal)
                    {
                        if (!actTrain.HoldingSignals.Contains(exitSignal))
                        {
                            actTrain.HoldingSignals.Add(exitSignal);
                        }
                    }
                    else
                    {
                        if (actTrain.HoldingSignals.Contains(exitSignal))
                        {
                            actTrain.HoldingSignals.Remove(exitSignal);
                        }
                    }
                }

                // Stop used to define command only - find related section in route
                else if (Commands != null)
                {
                    // Get platform details
                    int platformIndex;
                    int actSubpath = 0;

                    if (signalRef.PlatformXRefList.TryGetValue(actPlatformID, out platformIndex))
                    {
                        PlatformDetails thisPlatform = signalRef.PlatformDetailsList[platformIndex];
                        int sectionIndex = thisPlatform.TCSectionIndex[0];
                        int routeIndex = actTrain.TCRoute.TCRouteSubpaths[actSubpath].GetRouteIndex(sectionIndex, 0);

                        // If first section not found in route, try last
                        if (routeIndex < 0)
                        {
                            sectionIndex = thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1];
                            routeIndex = actTrain.TCRoute.TCRouteSubpaths[actSubpath].GetRouteIndex(sectionIndex, 0);
                        }

                        // If neither section found - try next subroute - keep trying till found or out of subroutes
                        while (routeIndex < 0 && actSubpath < (actTrain.TCRoute.TCRouteSubpaths.Count - 1))
                        {
                            actSubpath++;
                            Train.TCSubpathRoute thisRoute = actTrain.TCRoute.TCRouteSubpaths[actSubpath];
                            routeIndex = thisRoute.GetRouteIndex(sectionIndex, 0);

                            // If first section not found in route, try last
                            if (routeIndex < 0)
                            {
                                sectionIndex = thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1];
                                routeIndex = thisRoute.GetRouteIndex(sectionIndex, 0);
                            }
                        }

                        // If section found: process stop
                        if (routeIndex >= 0)
                        {
                            validStop = true;

                            sectionIndex = actTrain.TCRoute.TCRouteSubpaths[actSubpath][routeIndex].TCSectionIndex;
                            foreach (TTTrainCommands thisCommand in Commands)
                            {
                                actTrain.ProcessTimetableStopCommands(thisCommand, actSubpath, sectionIndex, -1, actPlatformID, refTTInfo);
                            }
                        }
                    }
                }

                // Pass time only - valid condition but not yet processed
                if (!validStop && passTime >= 0)
                {
                    validStop = true;
                }

                return validStop;
            } // End buildStopInfo

        } // End class stopInfo

        //================================================================================================//
        //================================================================================================//
        /// <summary>
        /// Class to hold station information
        /// Class is used during extraction process only
        /// </summary>
        private class StationInfo
        {
            public enum HoldInfo
            {
                Hold,
                NoHold,
                ForceHold,
                HoldConditional_DwellTime,
            }

            public string StationName;            // Station Name
            public HoldInfo HoldState;            // Hold State
            public bool NoWaitSignal;             // Train will run up to signal and not wait in platform
            public bool ForceWaitSignal;          // Force to wait for signal even if not exit signal for platform
            public int? actMinStopTime;           // Min Dwell time for Conditional Holdstate
            public bool IsTerminal;               // Station is terminal
            public bool CloseupSignal;            // Train may close up to signal
            public bool RestrictPlatformToSignal; // Restrict platform end to signal position
            public bool ExtendPlatformToSignal;   // Extend platform end to next signal position

            //================================================================================================//
            /// <summary>
            /// Constructor from String
            /// </summary>
            /// <param name="stationName"></param>
            public StationInfo(string stationString)
            {
                // Default settings
                HoldState = HoldInfo.NoHold;
                NoWaitSignal = false;
                ForceWaitSignal = false;
                actMinStopTime = null;
                IsTerminal = false;
                CloseupSignal = false;
                RestrictPlatformToSignal = false;
                ExtendPlatformToSignal = false;

                if (stationString.Contains("$"))
                {
                    // WIf string contains commands: split name and commands
                    string[] stationDetails = stationString.Split('$');
                    StationName = String.Copy(stationDetails[0]).ToLower().Trim();
                    ProcessStationCommands(stationDetails);
                }
                else
                {
                    // String contains name only
                    StationName = String.Copy(stationString).ToLower().Trim();
                }
            }

            //================================================================================================//
            /// <summary>
            /// Process Station Commands : add command info to stationInfo class
            /// </summary>
            /// <param name="commands"></param>
            public void ProcessStationCommands(string[] commands)
            {
                // Start at 1 as 0 is station name
                for (int iString = 1; iString <= commands.Length - 1; iString++)
                {
                    string commandFull = commands[iString];
                    TTTrainCommands thisCommand = new TTTrainCommands(commandFull);

                    switch (thisCommand.CommandToken)
                    {
                        case "hold":
                            HoldState = HoldInfo.Hold;
                            break;

                        case "nohold":
                            HoldState = HoldInfo.NoHold;
                            break;

                        case "forcehold":
                            HoldState = HoldInfo.ForceHold;
                            break;

                        case "forcewait":
                            ForceWaitSignal = true;
                            break;

                        case "nowaitsignal":
                            NoWaitSignal = true;
                            break;

                        case "terminal":
                            IsTerminal = true;
                            break;

                        case "closeupsignal":
                            CloseupSignal = true;
                            break;

                        case "extendplatformtosignal":
                            ExtendPlatformToSignal = true;
                            break;

                        case "restrictplatformtosignal":
                            RestrictPlatformToSignal = true;
                            break;

                        // Required minimal stop time
                        case "stoptime":
                            if (thisCommand.CommandValues != null && thisCommand.CommandValues.Count > 0)
                            {
                                try
                                {
                                    actMinStopTime = Convert.ToInt32(thisCommand.CommandValues[0]);
                                }
                                catch
                                {
                                    Trace.TraceInformation("Station stop {0} : invalid value for stop time", commands[0]);
                                }
                            }
                            else
                            {
                                Trace.TraceInformation("Station stop {0} : missing value for station stop time", commands[0]);
                            }
                            break;

                        // Other commands not yet implemented
                        default:
                            break;
                    }
                }
            }
        }

        //================================================================================================//
        //================================================================================================//
        /// <summary>
        /// Class to hold dispose info
        /// Class is used during extraction process only
        /// </summary>
        private class DisposeInfo
        {
            public enum DisposeType
            {
                Forms,
                Triggers,
                Static,
                Stable,
                Pool,
                None,
            }

            public string FormedTrain;
            public TTTrain.FormCommand FormType;
            public bool FormTrain;
            public bool FormStatic;
            public bool SetStop;
            public bool FormsAtStation;
            public bool Closeup;
            public TTTrain.PowerActionType PowerOffOnForms;
            public int FormedPowerOffDelay;

            public struct StableDetails
            {
                public string Stable_outpath;
                public int? Stable_outtime;
                public string Stable_inpath;
                public int? Stable_intime;
                public string Stable_name;
            }

            public bool Stable;
            public StableDetails StableInfo;

            public bool Pool;
            public string PoolName;
            public string PoolExitDirection;

            public float? DisposeSpeed;
            public bool RunRound;
            public string RunRoundPath;
            public int? RunRoundTime;
            public bool RunRoundCloseup;
            public bool RunRoundForceReversal;

            public enum RunRoundPosition
            {
                inposition,
                stableposition,
                outposition,
            }

            public RunRoundPosition RunRoundPos;

            public bool CallOn;

            //================================================================================================//
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="typeOfDispose></param>
            /// <param name="trainCommands"></param>
            /// <param name="formType"></param>
            public DisposeInfo(DisposeType typeOfDispose, TTTrainCommands trainCommands, TTTrain.FormCommand formType, string trainName)
            {
                FormTrain = false;
                FormStatic = false;
                Closeup = false;
                Stable = false;
                Pool = false;
                RunRound = false;
                SetStop = false;
                FormsAtStation = false;
                DisposeSpeed = null;
                PowerOffOnForms = TTTrain.PowerActionType.On;
                FormedPowerOffDelay = 0;

                switch (typeOfDispose)
                {
                    case DisposeType.Forms:
                    case DisposeType.Triggers:
                        FormedTrain = String.Copy(trainCommands.CommandValues[0]);
                        FormType = formType;
                        FormTrain = true;

                        if (trainCommands.CommandQualifiers != null && (formType == TTTrain.FormCommand.TerminationFormed || formType == TTTrain.FormCommand.TerminationTriggered))
                        {
                            foreach (TTTrainCommands.TTTrainComQualifiers formedTrainQualifiers in trainCommands.CommandQualifiers)
                            {
                                switch (formedTrainQualifiers.QualifierName)
                                {
                                    case "runround":
                                        RunRound = true;
                                        RunRoundPath = String.Copy(formedTrainQualifiers.QualifierValues[0]);
                                        RunRoundTime = -1;
                                        break;

                                    case "rrtime":
                                        TimeSpan RRSpan;
                                        TimeSpan.TryParse(formedTrainQualifiers.QualifierValues[0], out RRSpan);
                                        RunRoundTime = Convert.ToInt32(RRSpan.TotalSeconds);
                                        break;

                                    case "rrcloseup":
                                        RunRoundCloseup = true;
                                        break;

                                    case "rrforcereversal":
                                        RunRoundForceReversal = true;
                                        break;

                                    case "setstop":
                                        SetStop = true;
                                        break;

                                    case "atstation":
                                        FormsAtStation = true;
                                        break;

                                    case "closeup":
                                        Closeup = true;
                                        break;

                                    case "speed":
                                        try
                                        {
                                            DisposeSpeed = Convert.ToSingle(formedTrainQualifiers.QualifierValues[0]);
                                        }
                                        catch
                                        {
                                            Trace.TraceInformation("Train : {0} : invalid value for runround speed : {1} \n", trainName, formedTrainQualifiers.QualifierValues[0]);
                                        }
                                        break;

                                    case "poweroff":
                                        bool pantoup = false;
                                        if (formedTrainQualifiers.QualifierValues != null && formedTrainQualifiers.QualifierValues.Count > 0)
                                        {
                                            if (formedTrainQualifiers.QualifierValues[0] == "pantoup")
                                            {
                                                pantoup = true;
                                            }
                                            else
                                            {
                                                Trace.TraceInformation("Train : {0} : invalid value for poweroff in forms command : {1} \n", trainName, formedTrainQualifiers.QualifierValues[0]);
                                            }
                                        }

                                        PowerOffOnForms = pantoup ? TTTrain.PowerActionType.Off_PantoUp : TTTrain.PowerActionType.Off;
                                        FormedPowerOffDelay = 30 + Simulator.Random.Next(30);
                                        break;

                                    case "poweroffdelay":
                                        if (PowerOffOnForms == TTTrain.PowerActionType.On)
                                        {
                                            Trace.TraceInformation("Train {0} : PowerOffDelay in Forms command : defined without or before poweroff qualifier", trainName);
                                        }
                                        else if (formedTrainQualifiers.QualifierValues == null || formedTrainQualifiers.QualifierValues.Count <= 0)
                                        {
                                            Trace.TraceInformation("Train {0} : PowerOffDelay in Forms command : missing value", trainName);
                                        }
                                        else
                                        {
                                            try
                                            {
                                                FormedPowerOffDelay = Convert.ToInt32(formedTrainQualifiers.QualifierValues[0]); // defined in seconds
                                            }
                                            catch
                                            {
                                                Trace.TraceInformation("Train {0} : invalid value in PowerOffDelay in Forms command : {1} \n",
                                                    trainName, formedTrainQualifiers.QualifierValues[0]);
                                            }
                                        }
                                        break;

                                    default:
                                        Trace.TraceInformation("Train {0} : invalid qualifier in {1} command : {2} \n", trainName, typeOfDispose, formedTrainQualifiers.QualifierName);
                                        break;
                                }
                            }

                            break;
                        }

                        // Reset speed if runround is not set
                        if (!RunRound && DisposeSpeed != null)
                        {
                            DisposeSpeed = null;
                        }

                        // reset power off if runround is set
                        if (RunRound && PowerOffOnForms != TTTrain.PowerActionType.On)
                        {
                            PowerOffOnForms = TTTrain.PowerActionType.On;
                        }

                        break;
                    // End of Forms and Triggers

                    case DisposeType.Static:
                        List<TTTrainCommands.TTTrainComQualifiers> staticQualifiers = trainCommands.CommandQualifiers;
                        FormStatic = true;
                        FormType = TTTrain.FormCommand.None;

                        if (staticQualifiers != null)
                        {
                            foreach (TTTrainCommands.TTTrainComQualifiers staticQualifier in staticQualifiers)
                            {
                                switch (staticQualifier.QualifierName)
                                {
                                    case "closeup":
                                        Closeup = true;
                                        break;

                                    case "poweroff":
                                        bool pantoup = false;
                                        if (staticQualifier.QualifierValues != null && staticQualifier.QualifierValues.Count > 0)
                                        {
                                            if (staticQualifier.QualifierValues[0] == "pantoup")
                                            {
                                                pantoup = true;
                                            }
                                            else
                                            {
                                                Trace.TraceInformation("Train : {0} : invalid value for poweroff in Detach command : {1} \n", trainName, staticQualifier.QualifierValues[0]);
                                            }
                                        }

                                        PowerOffOnForms = pantoup ? TTTrain.PowerActionType.Off_PantoUp : TTTrain.PowerActionType.Off;
                                        FormedPowerOffDelay = 30 + Simulator.Random.Next(30);
                                        break;

                                    case "poweroffdelay":
                                        if (PowerOffOnForms == TTTrain.PowerActionType.On)
                                        {
                                            Trace.TraceInformation("Train {0} : PowerOffDelay in Detach command : defined without or before poweroff qualifier", trainName);
                                        }
                                        else if (staticQualifier.QualifierValues == null || staticQualifier.QualifierValues.Count <= 0)
                                        {
                                            Trace.TraceInformation("Train {0} : PowerOffDelay in Detach command : missing value", trainName);
                                        }
                                        else
                                        {
                                            try
                                            {
                                                FormedPowerOffDelay = Convert.ToInt32(staticQualifier.QualifierValues[0]); // defined in seconds
                                            }
                                            catch
                                            {
                                                Trace.TraceInformation("Train {0} : invalid value in PowerOffDelay in Detach command : {1} \n",
                                                    trainName, staticQualifier.QualifierValues[0]);
                                            }
                                        }
                                        break;

                                    default:
                                        Trace.TraceInformation("Train {0} : invalid qualifier in {1} command : {2} \n", trainName, typeOfDispose, staticQualifier.QualifierName);
                                        break;
                                }
                            }
                        }
                        break;
                    // End of static

                    case DisposeType.Stable:
                        Stable = true;
                        RunRound = false;
                        SetStop = true;
                        StableInfo.Stable_name = String.Empty;

                        foreach (TTTrainCommands.TTTrainComQualifiers stableQualifier in trainCommands.CommandQualifiers)
                        {
                            switch (stableQualifier.QualifierName)
                            {
                                case "out_path":
                                    StableInfo.Stable_outpath = String.Copy(stableQualifier.QualifierValues[0]);
                                    break;

                                case "out_time":
                                    TimeSpan outtime;
                                    TimeSpan.TryParse(stableQualifier.QualifierValues[0], out outtime);
                                    StableInfo.Stable_outtime = Convert.ToInt32(outtime.TotalSeconds);
                                    break;

                                case "in_path":
                                    StableInfo.Stable_inpath = String.Copy(stableQualifier.QualifierValues[0]);
                                    break;

                                case "in_time":
                                    TimeSpan intime;
                                    TimeSpan.TryParse(stableQualifier.QualifierValues[0], out intime);
                                    StableInfo.Stable_intime = Convert.ToInt32(intime.TotalSeconds);
                                    break;

                                case "forms":
                                    FormTrain = true;
                                    FormedTrain = String.Copy(stableQualifier.QualifierValues[0]);
                                    FormStatic = false;
                                    FormType = TTTrain.FormCommand.TerminationFormed;
                                    break;

                                case "triggers":
                                    FormTrain = true;
                                    FormedTrain = String.Copy(stableQualifier.QualifierValues[0]);
                                    FormStatic = false;
                                    FormType = TTTrain.FormCommand.TerminationTriggered;
                                    break;

                                case "static":
                                    FormTrain = false;
                                    FormStatic = true;
                                    FormType = TTTrain.FormCommand.None;
                                    break;

                                case "closeup":
                                    Closeup = true;
                                    break;

                                case "runround":
                                    RunRound = true;
                                    RunRoundPath = String.Copy(stableQualifier.QualifierValues[0]);
                                    RunRoundTime = -1;
                                    RunRoundPos = RunRoundPosition.stableposition;
                                    break;

                                case "rrtime":
                                    TimeSpan RRSpan;
                                    TimeSpan.TryParse(stableQualifier.QualifierValues[0], out RRSpan);
                                    RunRoundTime = Convert.ToInt32(RRSpan.TotalSeconds);
                                    break;

                                case "rrcloseup":
                                    RunRoundCloseup = true;
                                    break;

                                case "rrforcereversal":
                                    RunRoundForceReversal = true;
                                    break;

                                case "callon":
                                    CallOn = true;
                                    break;

                                case "rrpos":
                                    switch (stableQualifier.QualifierValues[0])
                                    {
                                        case "in":
                                            RunRoundPos = RunRoundPosition.inposition;
                                            break;

                                        case "out":
                                            RunRoundPos = RunRoundPosition.outposition;
                                            break;

                                        case "stable":
                                            RunRoundPos = RunRoundPosition.stableposition;
                                            break;

                                        default:
                                            break;
                                    }
                                    break;

                                case "speed":
                                    try
                                    {
                                        DisposeSpeed = Convert.ToSingle(stableQualifier.QualifierValues[0]);
                                    }
                                    catch
                                    {
                                        Trace.TraceInformation("Train : {0} : invalid value for stable speed : {1} \n", trainName, stableQualifier.QualifierValues[0]);
                                    }
                                    break;

                                case "name":
                                    StableInfo.Stable_name = String.Copy(stableQualifier.QualifierValues[0]);
                                    break;

                                default:
                                    break;
                            }
                        }
                        break;
                    // End of stable

                    // Process pool
                    case DisposeType.Pool:
                        Pool = true;
                        FormType = formType;
                        PoolName = String.Copy(trainCommands.CommandValues[0]).ToLower().Trim();
                        PoolExitDirection = String.Empty;

                        if (trainCommands.CommandQualifiers != null)
                        {
                            foreach (TTTrainCommands.TTTrainComQualifiers poolQualifiers in trainCommands.CommandQualifiers)
                            {
                                switch (poolQualifiers.QualifierName)
                                {
                                    case "direction":
                                        PoolExitDirection = String.Copy(poolQualifiers.QualifierValues[0]);
                                        break;

                                    default:
                                        Trace.TraceInformation("Train : {0} : invalid qualifier for dispose to pool : {1} : {2}\n", trainName, PoolName, poolQualifiers.QualifierName);
                                        break;
                                }
                            }
                        }

                        break;
                    // End of pool

                    // Unknow type
                    default:
                        Trace.TraceInformation("Train : {0} : invalid qualifier for dispose {1}\n", trainName, typeOfDispose);
                        break;
                }
            }

        } // End class DisposeInfo

    } // End class TimetableInfo

    //================================================================================================//
    //================================================================================================//
    /// <summary>
    /// Class to hold all additional commands in unprocessed form
    /// </summary>
    /// 
    public class TTTrainCommands
    {
        public string CommandToken;
        public List<string> CommandValues;
        public List<TTTrainComQualifiers> CommandQualifiers;

        //================================================================================================//
        /// <summary>
        /// Constructor from string (excludes leading '$')
        /// </summary>
        /// <param name="CommandString"></param>
        public TTTrainCommands(string CommandString)
        {
            string workString = String.Copy(CommandString).ToLower().Trim();
            string restString = String.Empty;
            string commandValueString = String.Empty;

            // Check for qualifiers
            if (workString.Contains('/'))
            {
                string[] tempStrings = workString.Split('/'); // First string is token plus value, rest is qualifiers
                restString = String.Copy(tempStrings[0]);

                if (CommandQualifiers == null) CommandQualifiers = new List<TTTrainComQualifiers>();

                for (int iQual = 1; iQual < tempStrings.Length; iQual++)
                {
                    CommandQualifiers.Add(new TTTrainComQualifiers(tempStrings[iQual]));
                }
            }
            else
            {
                restString = String.Copy(workString);
            }

            // Extract command token and values
            if (restString.Contains('='))
            {
                int splitPosition = restString.IndexOf('=');
                CommandToken = restString.Substring(0, splitPosition);
                commandValueString = restString.Substring(splitPosition + 1);
            }
            else
            {
                CommandToken = String.Copy(restString.Trim());
            }

            // Process values
            // Split on "+" sign (multiple values)
            string[] valueStrings = null;

            if (String.IsNullOrEmpty(commandValueString))
            {
                CommandValues = null;
            }
            else
            {
                CommandValues = new List<string>();

                valueStrings = commandValueString.Contains('+') ? commandValueString.Split('+') : (new string[1] { commandValueString });

                foreach (string thisValue in valueStrings)
                {
                    CommandValues.Add(thisValue.Trim());
                }
            }
        }

        //================================================================================================//
        //================================================================================================//
        /// <summary>
        /// Class for command qualifiers
        /// </summary>
        public class TTTrainComQualifiers
        {
            public string QualifierName;
            public List<string> QualifierValues = new List<string>();

            //================================================================================================//
            /// <summary>
            /// Constructor (string is without leading '/')
            /// </summary>
            /// <param name="qualifier"></param>
            public TTTrainComQualifiers(string qualifier)
            {
                var qualparts = qualifier.Contains('=') ? qualifier.Split('=') : (new string[1] { qualifier });
                QualifierName = String.Copy(qualparts[0].Trim());

                string[] valueStrings;

                if (qualparts.Length > 1)
                {
                    valueStrings = qualparts[1].Contains('+') ? qualparts[1].Trim().Split('+') : (new string[1] { qualparts[1].Trim() });

                    foreach (string thisValue in valueStrings)
                    {
                        QualifierValues.Add(thisValue.Trim());
                    }
                }
            }

        } // End class TTTrainComQualifiers
    } // End class TTTrainCommands
}
