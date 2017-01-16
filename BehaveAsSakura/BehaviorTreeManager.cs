﻿using BehaveAsSakura.Tasks;
using BehaveAsSakura.Variables;
using System;

namespace BehaveAsSakura
{
    public interface IBehaviorTreeLoader
    {
        BehaviorTreeDesc LoadTree(string path);
    }

    public interface IBehaviorTreeManagerOwner : IBehaviorTreeLoader, IVariableContainer
    {
    }

    public sealed class BehaviorTreeManager
    {
        private IBehaviorTreeManagerOwner owner;

        public BehaviorTreeManager(IBehaviorTreeManagerOwner owner)
        {
            this.owner = owner;
        }

        public BehaviorTree CreateTree(IBehaviorTreeOwner owner, string path, Task parentTask = null)
        {
            var treeDesc = this.owner.LoadTree(path);
            var tree = new BehaviorTree(this, owner, treeDesc, parentTask);

            return tree;
        }

        internal Task CreateTask(BehaviorTree tree, BehaviorTreeDesc treeDesc, Task parentTask, uint taskId)
        {
            var descWrapper = treeDesc.FindTaskDesc(taskId);
            var desc = (ITaskDesc)descWrapper.CustomDesc;
            var task = desc.CreateTask(tree, parentTask, descWrapper.Id);

            if (!(task is LeafTask))
            {
                if (task is DecoratorTask)
                {
                    var decoratorTask = (DecoratorTask)task;
                    var childTaskId = ((DecoratorTaskDescWrapper)descWrapper).ChildTask;
                    var childTask = CreateTask(tree, treeDesc, task, childTaskId);
                    decoratorTask.InitializeChild(childTask);
                }
                else
                {
                    var compositeTask = (CompositeTask)task;
                    var childTaskIds = ((CompositeTaskDescWrapper)descWrapper).ChildTasks;
                    var childTasks = Array.ConvertAll(childTaskIds, i => CreateTask(tree, treeDesc, task, i));
                    compositeTask.InitializeChildren(childTasks);
                }
            }

            tree.InitializeTask(task);

            return task;
        }

        internal IBehaviorTreeManagerOwner Owner
        {
            get { return owner; }
        }
    }
}
