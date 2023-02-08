using System;
using System.Collections.Generic;
using System.Linq;
using VL.Lib.Collections;

namespace VL.Lib.Collections
{
    public static class Trees
    {
        //public delegate void IReadOnlyTree_to_NodeCommand(IReadOnlyTreeNode node, out ITraverseCommand command);


        public static class ReadOnlyTreeNode
        {
            public static bool HasChildren(IReadOnlyTreeNode input) => input?.Children.Count() > 0;
            public static IEnumerable<IReadOnlyTreeNode> Flatten(IReadOnlyTreeNode input)
                => input.YieldReturn().Concat(input.Children.SelectMany(c => Flatten(c)));

            public static void Traverse<T>(T input, Func<IReadOnlyTreeNode, ITraverseCommand> update, out T output, out ITraverseCommand lastCommand)
                where T : IReadOnlyTreeNode
            {
                lastCommand = input.TraverseMessageToChildsInternal(null, (IReadOnlyTreeNode n, object m) => update(n));
                output = input;
            }

            public static void TraverseMessageToChilds<T, TMessage>(T input, TMessage message, Func<IReadOnlyTreeNode, TMessage, ITraverseCommand> update, out T output, out ITraverseCommand lastCommand)
                where T : IReadOnlyTreeNode
            {
                lastCommand = input.TraverseMessageToChildsInternal(message, (IReadOnlyTreeNode n, TMessage m) => update(n, m));
                output = input;
            }

            public static void TraverseBreadthFirst<T>(T input, Func<IReadOnlyTreeNode, ITraverseCommand> update, out T output)
                where T : IReadOnlyTreeNode
            {
                var queue = new Queue<IReadOnlyTreeNode>();
                TraverseBreadthFirstInternal(input, (object)null, (n, m) => update(n), queue);
                output = input;
            }

            public static void TraverseBreadthFirstMessageToChilds<T, TMessage>(T input, TMessage message, Func<IReadOnlyTreeNode, TMessage, ITraverseCommand> update, out T output)
                 where T : IReadOnlyTreeNode
            {
                var queue = new Queue<IReadOnlyTreeNode>();
                TraverseBreadthFirstInternal(input, message, update, queue);
                output = input;
            }
        }

        public static class ReadOnlyTree
        {
            public static IEnumerable<T> Flatten<T>(T input)
                where T : IReadOnlyTree<T>
                 => (input.YieldReturn()).Concat(input.Children.SelectMany(c => Flatten(c)));

            public static void Traverse<T>(T input, Func<T, ITraverseCommand> update, out T output, out ITraverseCommand lastCommand)
                where T : IReadOnlyTree<T>
            {
                lastCommand = input.TraverseMessageToChildsInternal(null, (T n, object m) => update(n));
                output = input;
            }

            public static void TraverseMessageToChilds<T, TMessage>(T input, TMessage message, Func<T, TMessage, ITraverseCommand> update, out T output, out ITraverseCommand lastCommand)
                where T : IReadOnlyTree<T>
            {
                lastCommand = input.TraverseMessageToChildsInternal(message, update);
                output = input;
            }

            public static void TraverseBreadthFirst<T>(T input, Func<T, ITraverseCommand> update, out T output)
               where T : IReadOnlyTree<T>
            {
                var queue = new Queue<T>();
                TraverseBreadthFirstInternal(input, null, (T n, object m) => update(n), queue);
                output = input;
            }

            public static void TraverseBreadthFirstMessageToChilds<T, TMessage>(T input, TMessage message, Func<T, TMessage, ITraverseCommand> update, out T output)
                where T : IReadOnlyTree<T>
            {
                var queue = new Queue<T>();
                TraverseBreadthFirstInternal(input, message, (T n, TMessage m) => update(n, m), queue);
                output = input;
            }
        }

        /// <summary>
        /// Gives you an untyped view on a node in a tree.
        /// Implement this interface if you want to be able to be part of a tree that you can traverse.
        /// Note that different implementations of this interface may contribute to one tree structure.
        /// </summary>
        public interface IReadOnlyTreeNode
        {
            IReadOnlyList<IReadOnlyTreeNode> Children { get; }
        }

        /// <summary>
        /// A typed tree that guarantees that all nodes in all child branches are of the same type.
        /// Implement this interface if you want a guarantee that all nodes are of a particular type.
        /// Note that a typed tree can be a part of an untyped tree structure, but not the other way around.
        /// </summary>
        /// <typeparam name="T">The type that all nodes in the whole tree share. This can be an interface again, which would again allow to mix different types in one tree. </typeparam>
        public interface IReadOnlyTree<out T> : IReadOnlyTreeNode
            where T : IReadOnlyTree<T>
        {
            new IReadOnlyList<T> Children { get; }
        }


        class TestLeave : IReadOnlyTree<TestLeave>
        {
            public IReadOnlyList<TestLeave> Children { get; set; }
            IReadOnlyList<IReadOnlyTreeNode> IReadOnlyTreeNode.Children => Children;
        }

        class TestTree<T> : IReadOnlyTree<T>
            where T : class, IReadOnlyTree<T> // class constraint so that T is a Liskov-subtype of IReadOnlyTree<T> and by that of IReadOnlyTreeNode (no boxing necessary)
        {
            public List<T> Children { get; set; }
            IReadOnlyList<T> IReadOnlyTree<T>.Children => Children;
            IReadOnlyList<IReadOnlyTreeNode> IReadOnlyTreeNode.Children => Children;
        }

        static class Tests
        {
            static void Test1()
            {
                TestTree<TestLeave> t0 = new TestTree<TestLeave>();
                IReadOnlyTree<TestLeave> t1 = new TestTree<TestLeave>();

                // wow: a TestTree<TestLeave> can be seen as a tree structure that is "deeper".
                // this is unrelated to the fact that his particular instance doesn't have descandents at all. 
                // it is like saying: we can't promise anything about our children, but if you go deeper n levels all descandents that you will then stumble upon will be of type TestLeave
                IReadOnlyTree<IReadOnlyTree<TestLeave>> t2 = new TestTree<TestLeave>();
                IReadOnlyTree<IReadOnlyTree<IReadOnlyTree<TestLeave>>> t3 = new TestTree<TestLeave>();

                // but the deeper you traverse into - in a generic way - you'll always stumble upon the same promise. you're not making progress.
                var flat = ReadOnlyTree.Flatten(t3);

                // only when doing it manually.
                var t3Leave = t3.Children.First().Children.First().Children.First();

                // but again this leave can be seen as this less promising type
                IReadOnlyTree<IReadOnlyTree<IReadOnlyTree<TestLeave>>> t3Descendent = t3Leave;


                TestTree<IReadOnlyTree<TestLeave>> t4 = new TestTree<IReadOnlyTree<TestLeave>>();
                t4.Children.Add(t1);

                TestTree<IReadOnlyTree<IReadOnlyTree<TestLeave>>> t5 = new TestTree<IReadOnlyTree<IReadOnlyTree<TestLeave>>>();
                t5.Children.Add(t4);
                IReadOnlyTree<IReadOnlyTree<IReadOnlyTree<TestLeave>>> t5_ = t5;

                //TestTree<TestTree<TestLeave>> t6; // we can't declare this. TestTree<TestLeave> is no IReadOnlyTree<TestTree<TestLeave>>, it is a IReadOnlyTree<TestLeave>

                // take-away: the interfaces allow the user to trick himself and get mad.
                // if he would have just defined his own recursive type he would have stumbled upon these issues earlier.

                //var flat = ReadOnlyTree.Flatten(t5); // type inference is stuck. maybe we should accept an IReadOnlyTree<T> instead of T ? 
            }
        }

        /// <summary>
        /// Used in traverse algorithms of IReadOnlyTree
        /// </summary>
        public interface ITraverseCommand
        {
        }



        internal interface IMessage<out TMessage>
        {
            TMessage Message { get; }
        }
        public static void TryGetMessage<TMessage>(this ITraverseCommand command, out TMessage message, out bool success)
        {
            var x = command as IMessage<TMessage>;
            success = x != null;
            if (success) message = x.Message; else message = default(TMessage);
        }


        internal interface IReferingToNode<out T>
            where T : IReadOnlyTreeNode
        {
            T Node { get; }
        }
        public static void TryGetNode<T>(this ITraverseCommand command, out T node, out bool success)
            where T : IReadOnlyTreeNode
        {
            var r = command as IReferingToNode<T>;
            success = r != null;
            if (success) node = r.Node; else node = default(T);
        }

        //continue traversing
        internal interface IContinueCommand : ITraverseCommand
        {
        }
        public static bool IsContinue(this ITraverseCommand command) => command is IContinueCommand;


        internal class TraverseAllChildsCommand : IContinueCommand
        {
            public bool TraverseChilds { get; set; }
        }
        public static ITraverseCommand TraverseAllChilds = new TraverseAllChildsCommand() { TraverseChilds = true };
        public static ITraverseCommand SkipAllChilds = new TraverseAllChildsCommand() { TraverseChilds = false };
        public static bool IsTraverseAllChilds(this ITraverseCommand command) => (command as TraverseAllChildsCommand)?.TraverseChilds ?? false;
        public static bool IsSkipAllChilds(this ITraverseCommand command) => (!(command as TraverseAllChildsCommand)?.TraverseChilds) ?? false;


        internal class TraverseAllChildsCommand<TMessage> : TraverseAllChildsCommand, IMessage<TMessage>
        {
            public TMessage Message { get; set; }
        }
        public static ITraverseCommand TraverseAllChildsWithMessage<TMessage>(TMessage message) => new TraverseAllChildsCommand<TMessage>() { TraverseChilds = true, Message = message };


        internal class TraverseNodeCommand<T> : IContinueCommand
            where T : IReadOnlyTree<T>
        {
            public T Node { get; set; }
        }
        public static ITraverseCommand TraverseNode<T>(T node)
             where T : IReadOnlyTree<T>
            => new TraverseNodeCommand<T>() { Node = node };

        internal class TraverseNodeCommand<T, TMessage> : TraverseNodeCommand<T>, IMessage<TMessage>
            where T : IReadOnlyTree<T>
        {
            public TMessage Message { get; set; }
        }
        public static ITraverseCommand TraverseNodeWithMessage<T, TMessage>(T node, TMessage message)
            where T : IReadOnlyTree<T>
            => new TraverseNodeCommand<T, TMessage>() { Node = node, Message = message };


        internal class OneUpCommand : IContinueCommand
        {
        }
        /// <summary>
        /// Don't traverse Children and also don't traverse the rest of the siblings. Not supported by BreadthFirst traversing.
        /// </summary>
        public static ITraverseCommand OneUp = new OneUpCommand();
        public static bool IsOneUp(this ITraverseCommand command) => command is OneUpCommand;


        internal class GoOnCommand : IContinueCommand
        {
        }
        internal static ITraverseCommand GoOn = new GoOnCommand();
        public static bool IsGoOn(this ITraverseCommand command) => command is GoOnCommand;




        //stop traversing
        internal interface IStopCommand : ITraverseCommand
        {
        }
        public static bool IsStop(this ITraverseCommand command) => command is IStopCommand;


        internal class StopCommand : IStopCommand
        {
            public bool Success { get; set; }
        }
        internal class StopCommand<TMessage> : StopCommand, IMessage<TMessage>
        {
            public TMessage Message { get; set; }
        }
        public static ITraverseCommand Return = new StopCommand() { Success = true };
        public static ITraverseCommand Fail = new StopCommand();
        public static ITraverseCommand ReturnWithMessage<TMessage>(TMessage message) => new StopCommand<TMessage>() { Message = message, Success = true };
        public static ITraverseCommand FailWithMessage<TMessage>(TMessage message) => new StopCommand<TMessage>() { Message = message };
        public static bool IsFail(this ITraverseCommand command) => !command.IsSuccess();
        public static bool IsSuccess(this ITraverseCommand command) => (!(command is StopCommand)) || (command as StopCommand).Success;


        //traverse algorithms

        // ungeric version works with reference types (the interfaces)
        internal static ITraverseCommand TraverseMessageToChildsInternal<TMessage>(this IReadOnlyTreeNode node, TMessage message, Func<IReadOnlyTreeNode, TMessage, ITraverseCommand> update)
        {
            var command = update(node, message);

            if (command.IsContinue())
            {
                if (command.IsOneUp())
                    return command;

                command.TryGetMessage(out message, out var success);

                if (command.IsTraverseAllChilds())
                {
                    foreach (var c in node.Children)
                    {
                        if (c == null) return Fail;
                        command = c.TraverseMessageToChildsInternal(message, update);
                        if (command.IsStop())
                            return command;
                        if (command.IsOneUp())
                            return GoOn;
                    }
                    return command;
                }
                else
                if (command.IsSkipAllChilds())
                {
                    return command;
                }
                else
                {
                    command.TryGetNode<IReadOnlyTreeNode>(out var nextNode, out success);
                    if (!success) return Fail;
                    return nextNode.TraverseMessageToChildsInternal(message, update);
                }
            }
            else
            if (command.IsStop())
            {
                return command;
            }
            throw new NotImplementedException();
        }

        // generic version may work with structs as well. interface works as a constraint, we pass T around
        internal static ITraverseCommand TraverseMessageToChildsInternal<T, TMessage>(this T node, TMessage message, Func<T, TMessage, ITraverseCommand> update)
            where T : IReadOnlyTree<T>
        {
            var command = update(node, message);

            if (command.IsContinue())
            {
                if (command.IsOneUp())
                    return command;

                command.TryGetMessage(out message, out var success);

                if (command.IsTraverseAllChilds())
                {
                    foreach (var c in node.Children)
                    {
                        if (c == null) return Fail;
                        command = c.TraverseMessageToChildsInternal(message, update);
                        if (command.IsStop())
                            return command;
                        if (command.IsOneUp())
                            return GoOn;
                    }
                    return command;
                }
                else
                if (command.IsSkipAllChilds())
                {
                    return command;
                }
                else
                {
                    command.TryGetNode<T>(out var nextNode, out success);
                    if (!success) return Fail;
                    return nextNode.TraverseMessageToChildsInternal(message, update);
                }
            }
            else
            if (command.IsStop())
            {
                return command;
            }
            throw new NotImplementedException();
        }


        internal static void TraverseBreadthFirstInternal<TMessage>(IReadOnlyTreeNode node, TMessage message, Func<IReadOnlyTreeNode, TMessage, ITraverseCommand> update, Queue<IReadOnlyTreeNode> queue)
        {
            queue.Enqueue(node);
            for (;;)
            {
                if (queue.Count == 0)
                    return;

                var x = queue.Dequeue();

                var command = update(x, message);

                if (command.IsContinue())
                {
                    if (command.IsOneUp())
                        return;

                    command.TryGetMessage(out message, out var success);
                    if (command.IsTraverseAllChilds())
                    {
                        foreach (var c in x.Children)
                        {
                            queue.Enqueue(c);
                        }
                    }
                    else
                    if (command.IsSkipAllChilds())
                    {

                    }
                    else
                    {
                        command.TryGetNode<IReadOnlyTreeNode>(out var nextNode, out success);
                        if (!success)
                            throw new Exception($"Couldn't retrieve node to traverse next. Have been in {node}. Tried to retrieve a node of type {typeof(IReadOnlyTreeNode)}.");
                        queue.Enqueue(nextNode);
                    }
                }
                else
                if (command.IsStop())
                {
                    return;
                }
                else
                    throw new NotImplementedException();
            }
        }

        internal static void TraverseBreadthFirstInternal<T, TMessage>(T node, TMessage message, Func<T, TMessage, ITraverseCommand> update, Queue<T> queue)
            where T : IReadOnlyTree<T>
        {
            queue.Enqueue(node);
            for (;;)
            {
                if (queue.Count == 0)
                    return;

                var x = queue.Dequeue();

                var command = update(x, message);

                if (command.IsContinue())
                {
                    if (command.IsOneUp())
                        return;

                    command.TryGetMessage(out message, out var success);
                    if (command.IsTraverseAllChilds())
                    {
                        foreach (var c in x.Children)
                        {
                            queue.Enqueue(c);
                        }
                    }
                    else
                    if (command.IsSkipAllChilds())
                    {

                    }
                    else
                    {
                        command.TryGetNode<T>(out var nextNode, out success);
                        if (!success)
                            throw new Exception($"Couldn't retrieve node to traverse next. Have been in {node}. Tried to retrieve a node of type {typeof(T)}.");
                        queue.Enqueue(nextNode);
                    }
                }
                else
                if (command.IsStop())
                {
                    return;
                }
                else
                    throw new NotImplementedException();
            }
        }

    }
}
