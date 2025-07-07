using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Hengine.Scheduling
{
	public delegate void ScheduledAction<T>(ref T context) where T : allows ref struct;

	public ref struct Scheduler<T> where T : allows ref struct
	{
		Span<ScheduledAction<T>> jobs;

		public Scheduler(Span<ScheduledAction<T>> jobs)
		{
			this.jobs = jobs;
		}

		public unsafe void ExecuteOneStep(float dt, ref T context)
		{
			/*
			void* contextPtr = Unsafe.AsPointer(ref context);
			ScheduledAction<T>* jobPtr = (ScheduledAction<T>*)Unsafe.AsPointer(ref jobs[0]);

			Parallel.For(0, jobs.Length, i =>
			{
				ref T contextRef = ref Unsafe.AsRef<T>(contextPtr);
				jobPtr[i](ref contextRef);
			});

			*/
			for (int i = 0; i < jobs.Length; i++)
			{
				jobs[i](ref context);
			}
		}
	}
}
