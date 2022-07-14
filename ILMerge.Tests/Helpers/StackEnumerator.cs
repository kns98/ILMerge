using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ILMerging.Tests.Helpers
{
    public static class StackEnumerator
    {
        public static StackEnumerator<T> Create<T>(params T[] initial)
        {
            return new StackEnumerator<T>(initial);
        }

        public static StackEnumerator<T> Create<T>(IEnumerable<T> initial)
        {
            return new StackEnumerator<T>(initial);
        }

        public static StackEnumerator<T> Create<T>(IEnumerator<T> initial)
        {
            return new StackEnumerator<T>(initial);
        }

        public static StackEnumerator<TContext, T> Create<TContext, T>(TContext initialContext, params T[] initial)
        {
            return new StackEnumerator<TContext, T>(initialContext, initial);
        }

        public static StackEnumerator<TContext, T> Create<TContext, T>(TContext initialContext, IEnumerable<T> initial)
        {
            return new StackEnumerator<TContext, T>(initialContext, initial);
        }

        public static StackEnumerator<TContext, T> Create<TContext, T>(TContext initialContext, IEnumerator<T> initial)
        {
            return new StackEnumerator<TContext, T>(initialContext, initial);
        }
    }

    public sealed class StackEnumerator<T> : IDisposable
    {
        private readonly Stack<IEnumerator<T>> stack = new Stack<IEnumerator<T>>();
        private IEnumerator<T> current;

        public StackEnumerator(IEnumerator<T> initial)
        {
            current = initial ?? Enumerable.Empty<T>().GetEnumerator();
        }

        public StackEnumerator(IEnumerable<T> initial) : this(initial?.GetEnumerator())
        {
        }

        public StackEnumerator(params T[] initial) : this((IEnumerable<T>)initial)
        {
        }

        public T Current => current.Current;

        public void Dispose()
        {
            current.Dispose();
            foreach (var item in stack)
                item.Dispose();
            stack.Clear();
        }

        public bool MoveNext()
        {
            while (!current.MoveNext())
            {
                current.Dispose();
                if (stack.Count == 0) return false;
                current = stack.Pop();
            }

            return true;
        }

        public void Recurse(IEnumerator<T> newCurrent)
        {
            if (newCurrent == null) return;
            stack.Push(current);
            current = newCurrent;
        }

        public void Recurse(IEnumerable<T> newCurrent)
        {
            if (newCurrent == null) return;
            Recurse(newCurrent.GetEnumerator());
        }

        public void Recurse(params T[] newCurrent)
        {
            Recurse((IEnumerable<T>)newCurrent);
        }

        // Foreach support
        [EditorBrowsable(EditorBrowsableState.Never)]
        public StackEnumerator<T> GetEnumerator()
        {
            return this;
        }
    }

    public sealed class StackEnumerator<TContext, T> : IDisposable
    {
        private readonly Stack<Tuple<TContext, IEnumerator<T>>> stack = new Stack<Tuple<TContext, IEnumerator<T>>>();
        private Tuple<TContext, IEnumerator<T>> current;

        public StackEnumerator(TContext initialContext, IEnumerator<T> initial)
        {
            current = Tuple.Create(initialContext, initial ?? Enumerable.Empty<T>().GetEnumerator());
        }

        public StackEnumerator(TContext initialContext, IEnumerable<T> initial) : this(initialContext,
            initial?.GetEnumerator())
        {
        }

        public StackEnumerator(TContext initialContext, params T[] initial) : this(initialContext,
            (IEnumerable<T>)initial)
        {
        }

        public ContextCurrent Current => new ContextCurrent(current.Item1, current.Item2.Current);

        public void Dispose()
        {
            current.Item2.Dispose();
            foreach (var item in stack)
                item.Item2.Dispose();
            stack.Clear();
        }

        public bool MoveNext()
        {
            while (!current.Item2.MoveNext())
            {
                current.Item2.Dispose();
                if (stack.Count == 0) return false;
                current = stack.Pop();
            }

            return true;
        }

        public void Recurse(TContext newContext, IEnumerator<T> newCurrent)
        {
            if (newCurrent == null) return;
            stack.Push(current);
            current = Tuple.Create(newContext, newCurrent);
        }

        public void Recurse(TContext newContext, IEnumerable<T> newCurrent)
        {
            if (newCurrent == null) return;
            Recurse(newContext, newCurrent.GetEnumerator());
        }

        public void Recurse(TContext newContext, params T[] newCurrent)
        {
            Recurse(newContext, (IEnumerable<T>)newCurrent);
        }

        // Foreach support
        [EditorBrowsable(EditorBrowsableState.Never)]
        public StackEnumerator<TContext, T> GetEnumerator()
        {
            return this;
        }

        public struct ContextCurrent
        {
            public TContext Context { get; }

            public T Current { get; }

            public ContextCurrent(TContext context, T current)
            {
                Context = context;
                Current = current;
            }
        }
    }
}