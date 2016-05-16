using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ProceduralCities
{
	public class ThreadDispatcher
	{
		static ThreadDispatcher _instance;
		static Thread MainThread;
		Thread Worker;
		Queue<Action> MainThreadQueue = new Queue<Action>();
		Queue<Action> WorkerQueue = new Queue<Action>();
		bool WorkerQuitRequested;

		static ThreadDispatcher Instance
		{
			get
			{
				if (_instance == null)
					throw new InvalidOperationException("ThreadDispatcher singleton does not exist");

				return _instance;
			}
		}

		public static bool Loaded
		{
			get
			{
				return _instance != null;
			}
		}

		public static bool IsMainThread
		{
			get
			{
				return _instance == null || Thread.CurrentThread.Equals(MainThread);
			}
		}

		public ThreadDispatcher()
		{
			Utils.Log("Starting ThreadDispatcher");

			if (_instance != null)
				throw new InvalidOperationException("ThreadDispatcher singleton already exists");

			_instance = this;
			MainThread = Thread.CurrentThread;
			Worker = new Thread(DoWork);
			WorkerQuitRequested = false;
			Worker.Start();

			GameEvents.onLevelWasLoaded.Add(LevelWasLoaded);
		}

		void LevelWasLoaded(GameScenes scene)
		{
			if (scene == GameScenes.MAINMENU)
				Destroy();
		}

		void Destroy()
		{
			GameEvents.onLevelWasLoaded.Remove(LevelWasLoaded);

			Utils.Log("Stopping ThreadDispatcher");
			DebugUtils.Assert(Worker != null);
			DebugUtils.Assert(IsMainThread);

			ClearAndQueueToWorker(() => WorkerQuitRequested = true);

			// Get the last log messages
			DequeueFromWorker();

			if (Worker.Join(5000))
			{
				Utils.Log("Worker thread stopped");
			}
			else
			{
				Utils.Log("Worker thread not stopped, aborting");
				Worker.Abort();
			}

			Worker = null;

			_instance = null;
		}

		static public void LogException(Exception e)
		{
			DebugUtils.Assert(Loaded);
			DebugUtils.Assert(!IsMainThread);

			QueueToMainThread(() =>
			{
				Debug.LogException(e);
			});
		}

		void DoWork()
		{
			Utils.Log("Worker thread started");
			while(!WorkerQuitRequested)
			{
				Action act;
				Monitor.Enter(WorkerQueue);
				try
				{
					while(WorkerQueue.Count == 0)
						Monitor.Wait(WorkerQueue);
					act = WorkerQueue.Dequeue();
				}
				finally
				{
					Monitor.Exit(WorkerQueue);
				}

				try
				{
					act();
				}
				catch(Exception e)
				{
					Utils.Log("Exception during processing of request");
					LogException(e);
				}
			}
		}

		public static void QueueToWorker(Action act)
		{
			DebugUtils.Assert(Loaded);
			DebugUtils.Assert(IsMainThread);

			Monitor.Enter(Instance.WorkerQueue);
			try
			{
				Instance.WorkerQueue.Enqueue(act);
				Monitor.Pulse(Instance.WorkerQueue);
			}
			finally
			{
				Monitor.Exit(Instance.WorkerQueue);
			}
		}

		static void ClearAndQueueToWorker(Action act)
		{
			DebugUtils.Assert(Loaded);
			DebugUtils.Assert(IsMainThread);
			DebugUtils.Assert(Loaded);
			Monitor.Enter(Instance.WorkerQueue);
			try
			{
				Instance.WorkerQueue.Clear();
				Instance.WorkerQueue.Enqueue(act);
				Monitor.Pulse(Instance.WorkerQueue);
			}
			finally
			{
				Monitor.Exit(Instance.WorkerQueue);
			}
		}

		public static void DequeueFromWorker(int timeout = int.MaxValue, bool wait = false)
		{
			DebugUtils.Assert(Loaded);
			DebugUtils.Assert(IsMainThread);

			var watch = System.Diagnostics.Stopwatch.StartNew();

			Monitor.Enter(Instance.MainThreadQueue);
			try
			{
				if (wait && Instance.MainThreadQueue.Count == 0)
				{
					while(Instance.MainThreadQueue.Count == 0)
						Monitor.Wait(Instance.MainThreadQueue);

					Action act = Instance.MainThreadQueue.Dequeue();
					act();
				}
				else
				{
					while (Instance.MainThreadQueue.Count > 0 && watch.ElapsedMilliseconds < timeout)
					{
						Action act = Instance.MainThreadQueue.Dequeue();

						Monitor.Exit(Instance.MainThreadQueue);
						try
						{
							act();
						}
						finally
						{
							Monitor.Enter(Instance.MainThreadQueue);
						}
					}
				}
			}
			catch(Exception e)
			{
				Debug.LogException(e);
			}
			finally
			{
				Monitor.Exit(Instance.MainThreadQueue);
			}
		}

		public static void QueueToMainThread(Action act)
		{
			DebugUtils.Assert(Loaded);
			DebugUtils.Assert(!IsMainThread);

			Monitor.Enter(Instance.MainThreadQueue);
			try
			{
				Instance.MainThreadQueue.Enqueue(act);
				Monitor.Pulse(Instance.MainThreadQueue);
			}
			finally
			{
				Monitor.Exit(Instance.MainThreadQueue);
			}
		}

		public static void QueueToMainThreadSync(Action act)
		{
			DebugUtils.Assert(Loaded);
			DebugUtils.Assert(!IsMainThread);

			object monitor = new object();
			bool finished = false;
			bool error = false;

			QueueToMainThread(() =>
			{
				try
				{
					act();
				}
				catch(Exception)
				{
					Monitor.Enter(monitor);
					error = true;
					Monitor.Pulse(monitor);
					Monitor.Exit(monitor);
				}
				finally
				{
					Monitor.Enter(monitor);
					finished = true;
					Monitor.Pulse(monitor);
					Monitor.Exit(monitor);
				}
			});

			Monitor.Enter(monitor);
			try
			{
				while (!finished && !error)
					Monitor.Wait(monitor);
			}
			finally
			{
				Monitor.Exit(monitor);
			}

			if (error)
			{
				// TODO
			}
		}
	}
}
