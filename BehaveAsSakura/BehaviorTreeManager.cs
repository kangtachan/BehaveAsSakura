﻿using BehaveAsSakura.Tasks;
using System;

namespace BehaveAsSakura
{
	public class BehaviorTreeManager
	{
		public BehaviorTree CreateTree(IBehaviorTreeOwner owner, string path)
		{
			return CreateTree( owner, path, null );
		}

		internal BehaviorTree CreateTree(IBehaviorTreeOwner owner, string path, Task parentTask = null)
		{
			var treeDesc = LoadTree( path );
			var tree = new BehaviorTree( this, owner, treeDesc, parentTask );

			return tree;
		}

		internal Task CreateTask(BehaviorTree tree, uint taskId, Task parentTask = null)
		{
			throw new NotImplementedException();
		}

		BehaviorTreeDesc LoadTree(string path)
		{
			throw new NotImplementedException();
		}
	}
}
