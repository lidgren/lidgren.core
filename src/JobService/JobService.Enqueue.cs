﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Lidgren.Core
{
	public static partial class JobService
	{
		private const int k_maxInFlightJobs = 1024; // must be power of two
		private const int k_instancesMask = k_maxInFlightJobs - 1;

		private static readonly Job[] s_instances = new Job[k_maxInFlightJobs];
		private static int s_nextInstance;
		private static int s_instancesCount;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void EnqueueInternal(string name, Action<object> work, object argument, JobCompletion completion = null)
		{
#if DEBUG
			CoreException.Assert(Monitor.IsEntered(s_instances));
#endif

			int idx = (s_nextInstance + s_instancesCount) & k_instancesMask;
			ref var job = ref s_instances[idx];
			job.Name = name;
			job.Work = work;
			job.Argument = argument;
			job.Completion = completion;
			s_instancesCount++;
			CoreException.Assert(s_instancesCount <= s_instances.Length);
			JobWait.Set();
		}

		/// <summary>
		/// Enqueue work to be done as soon as possible; returns immediately
		/// </summary>
		public static void Enqueue(string name, Action<object> work, object argument)
		{
			CoreException.Assert(s_workers != null, "JobService not initialized");
			lock(s_instances)
				EnqueueInternal(name, work, argument, null);
		}

		/// <summary>
		/// Enqueue work to be done as soon as possible; returns immediately
		/// </summary>
		public static void Enqueue(Action<object> work, object argument = null)
		{
			CoreException.Assert(s_workers != null, "JobService not initialized");
			lock(s_instances)
				EnqueueInternal("unnamed", work, argument, null);
		}

		/// <summary>
		/// Enqueue work to be done as soon as possible; returns immediately. When work is completed, continuation runs with continuationArgument.
		/// </summary>
		public static void Enqueue(string name, Action<object> work, object argument, Action<object> continuation, object continuationArgument = null, string continuationName = "continuation")
		{
			CoreException.Assert(s_workers != null, "JobService not initialized");
			var completion = JobCompletion.Acquire();
			completion.Continuation = continuation;
			completion.ContinuationArgument = continuationArgument;
			completion.ContinuationAtCount = 1;
#if DEBUG
			completion.ContinuationName = string.IsNullOrWhiteSpace(continuationName) ? name + "Contd" : continuationName;
#else
			completion.ContinuationName = continuationName;
#endif

			lock(s_instances)
				EnqueueInternal(name, work, argument, completion);
		}

		/// <summary>
		/// Enqueue work to be run (available threads) times; returns immediately.
		/// </summary>
		public static void EnqueueWide(string name, Action<object> work, object argument)
		{
			CoreException.Assert(s_workers != null, "JobService not initialized");
			lock (s_instances)
			{
				for (int i = 0; i < s_workers.Length; i++)
					EnqueueInternal(name, work, argument, null);
			}
		}

		/// <summary>
		/// Enqueue work to be run (available threads) times concurrently; blocks until all has completed
		/// </summary>
		public static void EnqueueWideBlock(string name, Action<object> work, object argument)
		{
			EnqueueWideBlock(int.MaxValue, name, work, argument);
		}

		/// <summary>
		/// Enqueue work to be run (available threads, but max maxConcurrency) times concurrently; blocks until all has completed
		/// </summary>
		public static void EnqueueWideBlock(int maxConcurrency, string name, Action<object> work, object argument)
		{
			CoreException.Assert(s_workers != null, "JobService not initialized");
			int numJobs = s_workers.Length - 1; // -1 because we assume local thread is one of the worker threads
			numJobs = Math.Min(numJobs, maxConcurrency - 1);

			var completion = JobCompletion.Acquire();
			completion.ContinuationAtCount = -1;
			lock (s_instances)
			{
				for (int i = 0; i < numJobs; i++)
					EnqueueInternal(name, work, argument, completion);
			}
			work(argument); // do one run on this thread as well
			completion.WaitAndRelease(numJobs);
		}

		/// <summary>
		/// Enqueue work to be run (available threads) times concurrently; calls continuation(continuationArgument) when all completed
		/// </summary>
		public static void EnqueueWide(string name, Action<object> work, object argument, Action<object> continuation, object continuationArgument)
		{
			CoreException.Assert(s_workers != null, "JobService not initialized");
			int numJobs = s_workers.Length;

			var completion = JobCompletion.Acquire();
			completion.ContinuationAtCount = numJobs;
			completion.Continuation = continuation;
			completion.ContinuationArgument = continuationArgument;
#if DEBUG
			completion.ContinuationName = name + "Contd";
#endif
			lock (s_instances)
			{
				for (int i = 0; i < numJobs; i++)
					EnqueueInternal(name, work, argument, completion);
			}
		}
	}
}
