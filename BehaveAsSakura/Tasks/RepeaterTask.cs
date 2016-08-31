﻿using ProtoBuf;

namespace BehaveAsSakura.Tasks
{
	[ProtoContract]
	public class RepeaterTaskDesc : ITaskDesc
	{
		[ProtoMember( 1 )]
		public uint Count { get; set; }
	}

	[ProtoContract]
	class RepeaterTaskProps : ITaskProps
	{
		[ProtoMember( 1 )]
		public bool WaitForChildCompleted { get; set; }

		[ProtoMember( 2 )]
		public uint LastUpdateTime { get; set; }

		[ProtoMember( 3 )]
		public uint Count { get; set; }
	}

	class RepeaterTask : DecoratorTask
	{
		private RepeaterTaskDesc description;
		private RepeaterTaskProps props;

		public RepeaterTask(BehaviorTree tree, Task parentTask, uint id, uint childTaskId, RepeaterTaskDesc description)
			: this( tree, parentTask, id, childTaskId, description, new RepeaterTaskProps() )
		{
		}

		protected RepeaterTask(BehaviorTree tree, Task parentTask, uint id, uint childTaskId, RepeaterTaskDesc description, RepeaterTaskProps props)
			: base( tree, parentTask, id, childTaskId, description, props )
		{
			this.description = description;
			this.props = props;
		}

		protected override void OnStart()
		{
			base.OnStart();

			props.WaitForChildCompleted = true;
			props.Count = 0;

			ChildTask.EnqueueForUpdate();
		}

		protected override TaskResult OnUpdate()
		{
			if( props.WaitForChildCompleted )
			{
				if( ChildTask.LastResult == TaskResult.Running )
					return TaskResult.Running;

				if( description.Count > 0 && ++props.Count >= description.Count )
					return TaskResult.Failure;

				if( !IsRepeaterCompleted( ChildTask.LastResult ) )
				{
					props.WaitForChildCompleted = false;
					if( Owner.CurrentTime > props.LastUpdateTime )
						EnqueueForUpdate();
					else
						EnqueueForNextUpdate();
				}
			}
			else
			{
				props.WaitForChildCompleted = true;
				ChildTask.EnqueueForUpdate();
			}

			props.LastUpdateTime = Owner.CurrentTime;

			return TaskResult.Running;
		}

		protected virtual bool IsRepeaterCompleted(TaskResult result)
		{
			return false;
		}
	}
}