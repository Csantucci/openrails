﻿///
/// Background process prepares the next frame for rendering.
/// Handles keyboard and mouse input
/// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ORTS
{
	public class UpdaterProcess
	{
		public readonly bool Threaded;
		public readonly Profiler Profiler = new Profiler("Updater");
		public bool Slow;
		readonly Viewer3D Viewer;
		readonly Thread Thread;
		readonly ProcessState State;

		public UpdaterProcess(Viewer3D viewer)
		{
			Threaded = System.Environment.ProcessorCount > 1;
			Viewer = viewer;
			if (Threaded)
			{
				State = new ProcessState();
				Thread = new Thread(UpdateLoop);
			}
		}

		public void Run()
		{
			if (Threaded)
				Thread.Start();
		}

		public void Stop()
		{
			if (Threaded)
				Thread.Abort();
		}

		public bool Finished
		{
			get
			{
				// Non-threaded updater is always "finished".
				return !Threaded || State.Finished;
			}
		}

		public void WaitTillFinished()
		{
			Slow = !Finished;
			// Non-threaded updater never waits.
			if (Threaded)
				State.WaitTillFinished();
		}

		void UpdateLoop()
		{
			Thread.CurrentThread.Name = "Updater Process";

			while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
			{
				// Wait for a new Update() command
				State.WaitTillStarted();

				try
				{
					Update();
				}
				catch (Exception error)
				{
					if (!(error is ThreadAbortException))
					{
						// Unblock anyone waiting for us, report error and die.
						State.SignalFinish();
						Viewer.ProcessReportError(error);
						// Finally unblock any process that may have started us, while the message was showing
						State.SignalFinish();
						return;
					}
				}

				// Signal finished so RenderProcess can start drawing
				State.SignalFinish();
			}
		}

		public void StartUpdate(RenderFrame frame, double totalRealSeconds)
		{
			if (!Finished)
				throw new InvalidOperationException("Can't overlap updates.");
			CurrentFrame = frame;
			TotalRealSeconds = totalRealSeconds;
			if (Threaded)
			{
				State.SignalStart();
			}
			else
			{
				try
				{
					Update();
				}
				catch (Exception error)
				{
					Viewer.ProcessReportError(error);
				}
			}
		}

		RenderFrame CurrentFrame;
		double TotalRealSeconds;
		double LastTotalRealSeconds;

		public void Update()
		{
			Profiler.Start();

			Program.RealTime = TotalRealSeconds;
			ElapsedTime elapsedTime = new ElapsedTime();
			elapsedTime.RealSeconds = (float)(TotalRealSeconds - LastTotalRealSeconds);
			elapsedTime.ClockSeconds = Viewer.Simulator.GetElapsedClockSeconds(elapsedTime.RealSeconds);
			LastTotalRealSeconds = TotalRealSeconds;

			try
			{
				Viewer.RenderProcess.ComputeFPS(elapsedTime.RealSeconds);
				Viewer.Simulator.Update(elapsedTime.ClockSeconds);
				Viewer.HandleUserInput(elapsedTime);
				Viewer.HandleMouseMovement();
				UserInput.Handled();
				CurrentFrame.Clear();
				Viewer.PrepareFrame(CurrentFrame, elapsedTime);
				CurrentFrame.Sort();
			}
			finally
			{
				Profiler.Stop();

				// Update the loader - it should only copy volatile data and return.
				if (Program.RealTime - Viewer.LoaderProcess.LastUpdate > LoaderProcess.UpdatePeriod)
					Viewer.LoaderProcess.StartUpdate();
			}
		}
	} // Updater Process
}
