﻿using BehaveAsSakura.Attributes;
using BehaveAsSakura.Events;
using BehaveAsSakura.Utils;

namespace BehaveAsSakura.Tasks
{
    [BehaveAsTable]
    [BehaveAsUnionInclude(typeof(ITaskDesc), 16)]
    [Task("Event/Wait Event")]
    public class WaitEventTaskDesc : ILeafTaskDesc
    {
        [BehaveAsField(1)]
        public string EventType { get; set; }

        void ITaskDesc.Validate()
        {
            Validation.NotEmpty(EventType, nameof(EventType));
        }

        Task ITaskDesc.CreateTask(BehaviorTree tree, Task parentTask, uint id)
        {
            return new WaitEventTask(tree, parentTask, id, this);
        }
    }

    [BehaveAsTable]
    [BehaveAsUnionInclude(typeof(ITaskProps), 5)]
    public class WaitEventTaskProps : ITaskProps
    {
        [BehaveAsField(1)]
        public bool IsEventTriggered { get; set; }
    }

    class WaitEventTask : LeafTask
    {
        private WaitEventTaskDesc description;
        private WaitEventTaskProps props;

        public WaitEventTask(BehaviorTree tree, Task parentTask, uint id, WaitEventTaskDesc description)
            : base(tree, parentTask, id, description, new WaitEventTaskProps())
        {
            this.description = description;
            props = (WaitEventTaskProps)Props;
        }

        protected override void OnStart()
        {
            base.OnStart();

            SubscribeEvent<SimpleEventTriggeredEvent>();

            props.IsEventTriggered = false;
        }

        protected override TaskResult OnUpdate()
        {
            return props.IsEventTriggered ? TaskResult.Success : TaskResult.Running;
        }

        protected override void OnEnd()
        {
            UnsubscribeEvent<SimpleEventTriggeredEvent>();

            base.OnEnd();
        }

        internal protected override void OnEventTriggered(IEvent @event)
        {
            base.OnEventTriggered(@event);

            if (!props.IsEventTriggered)
            {
                var e = @event as SimpleEventTriggeredEvent;
                if (e != null && e.EventType == description.EventType)
                {
                    props.IsEventTriggered = true;

                    UnsubscribeEvent<SimpleEventTriggeredEvent>();

                    EnqueueForUpdate();
                }
            }
        }

        protected override void OnRestoreProps(ITaskProps props)
        {
            base.OnRestoreProps(props);

            this.props = (WaitEventTaskProps)props;
        }
    }
}