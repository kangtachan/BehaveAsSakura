﻿using BehaveAsSakura.Events;
using BehaveAsSakura.Timers;

namespace BehaveAsSakura.Tasks
{
	public enum TaskResult : byte
	{
		Running,
		Success,
		Failure,
	}

	public enum TaskState : byte
	{
		Suspend,            // (WaitForUpdate, WaitForAbort) -> Suspend -> WaitForStart
		WaitForStart,       // Suspend -> WaitForStart -> (WaitForUpdate, WaitForAbort)
		WaitForUpdate,      // WaitForStart -> WaitForUpdate -> (WaitForEnqueue, WaitForAbort, Suspend)
		WaitForAbort,       // (WaitForStart, WaitForUpdate, WaitForEnqueue) -> WaitForAbort -> Suspend
		WaitForEnqueue,     // WaitForUpdate -> WaitForEnqueue -> (WaitForUpdate, WaitForAbort)
	}

	public interface ITaskDesc
	{
	}

	public interface ITaskProps
	{
	}

	public abstract class Task : ILogger, ISubscriber
	{
		private BehaviorTree tree;
		private Task parentTask;
		private uint id;
		private ITaskDesc description;

		private TaskState state = TaskState.Suspend;
		private TaskResult lastResult = TaskResult.Running;
		private ITaskProps props;
		private Timer immediateTimer;

		protected Task(BehaviorTree tree, Task parentTask, uint id, ITaskDesc description, ITaskProps props)
		{
			this.tree = tree;
			this.parentTask = parentTask;
			this.id = id;
			this.description = description;
			this.props = props;
		}

		#region Life-cycle

		internal void Update()
		{
			switch( state )
			{
				case TaskState.WaitForStart:
					state = TaskState.WaitForUpdate;
					OnStart();
					Update();
					break;

				case TaskState.WaitForUpdate:
					state = TaskState.WaitForEnqueue;
					var result = OnUpdate();

					if( result != TaskResult.Running )
					{
						state = TaskState.Suspend;
						lastResult = result;
						OnEnd();

						if( parentTask != null )
							parentTask.EnqueueForUpdate();
					}
					break;


				case TaskState.WaitForAbort:
					state = TaskState.Suspend;
					lastResult = TaskResult.Failure;
					OnAbort();
					OnEnd();
					break;

				default:
					break;
			}
		}

		internal protected void EnqueueForUpdate()
		{
			switch( state )
			{
				case TaskState.Suspend:
					lastResult = TaskResult.Running;
					state = TaskState.WaitForStart;
					tree.EnqueueTask( this );
					break;

				case TaskState.WaitForEnqueue:
					state = TaskState.WaitForUpdate;
					tree.EnqueueTask( this );
					break;

				default:
					LogDebug( "[{0}] ignored update request due to stage {1}", this, state );
					break;
			}
		}

		internal protected void EnqueueForAbort()
		{
			switch( state )
			{
				case TaskState.WaitForStart:
				case TaskState.WaitForUpdate:
				case TaskState.WaitForEnqueue:
					state = TaskState.WaitForAbort;
					tree.EnqueueTask( this );
					break;

				default:
					LogDebug( "[{0}] ignored abort request due to stage {1}", this, state );
					break;
			}
		}

		internal protected void EnqueueForNextUpdate()
		{
			if( immediateTimer != null )
				tree.TimerManager.CancelTimer( immediateTimer );

			immediateTimer = tree.TimerManager.StartTimer( 0 );
			Owner.EventBus.Subscribe<TimerTriggeredEvent>( this );
		}

		protected virtual void OnStart()
		{
		}

		protected abstract TaskResult OnUpdate();

		protected virtual void OnEnd()
		{
			Owner.EventBus.Unsubscribe<TimerTriggeredEvent>( this );
		}

		protected virtual void OnAbort()
		{
		}

		protected virtual void OnEventTriggered(IEvent @event)
		{
		}

		#endregion

		#region ILogger

		public void LogDebug(string msg, params object[] args)
		{
			tree.Owner.LogDebug( msg, args );
		}

		public void LogInfo(string msg, params object[] args)
		{
			tree.Owner.LogInfo( msg, args );
		}

		public void LogWarning(string msg, params object[] args)
		{
			tree.Owner.LogWarning( msg, args );
		}

		public void LogError(string msg, params object[] args)
		{
			tree.Owner.LogError( msg, args );
		}

		#endregion

		#region ISubscriber

		void ISubscriber.OnEventTriggered(IEvent @event)
		{
			var timerTriggered = @event as TimerTriggeredEvent;
			if( timerTriggered != null && immediateTimer != null && immediateTimer.Id == timerTriggered.TimerId )
			{
				immediateTimer = null;
				Owner.EventBus.Unsubscribe<TimerTriggeredEvent>( this );
				EnqueueForUpdate();
			}

			OnEventTriggered( @event );
		}

		#endregion

		#region Properties

		internal protected BehaviorTree Tree
		{
			get { return tree; }
		}

		internal protected IBehaviorTreeOwner Owner
		{
			get { return tree.Owner; }
		}

		internal protected Task ParentTask
		{
			get
			{
				if( parentTask != null )
					return parentTask;

				if( tree.ParentTask != null )
					return tree.ParentTask;

				return null;
			}
		}

		internal protected uint Id
		{
			get { return id; }
		}

		internal protected ITaskDesc Description
		{
			get { return description; }
		}

		protected ITaskProps Props
		{
			get { return props; }
		}

		internal protected TaskResult LastResult
		{
			get { return lastResult; }
		}

		#endregion
	}
}
