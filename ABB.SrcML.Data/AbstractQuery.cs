/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *  Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Queries perform read-only operations on <see cref="AbstractWorkingSet"/> objects
    /// </summary>
    /// <typeparam name="TResult">The return type from the query</typeparam>
    public abstract class AbstractQuery<TResult> {
        /// <summary>
        /// The amount of time to wait for the <see cref="AbstractWorkingSet.TryObtainReadLock">read lock</see>
        /// </summary>
        public int LockTimeout { get; private set; }

        /// <summary>
        /// The working set for this query
        /// </summary>
        public AbstractWorkingSet WorkingSet { get; private set; }

        /// <summary>
        /// The task factory for asynchronous operations
        /// </summary>
        protected TaskFactory Factory { get; private set; }

        private AbstractQuery() { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="factory">The task factory for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, TaskFactory factory)
            : this(workingSet, Timeout.Infinite, factory) { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The timeout to use for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout)
            : this(workingSet, lockTimeout, Task.Factory) { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        /// <param name="factory">The task factory for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory) {
            this.WorkingSet = workingSet;
            this.LockTimeout = lockTimeout;
            this.Factory = factory;
        }

        /// <summary>
        /// <see cref="Execute(Statement)">Executes the query</see> asynchronously on the <see cref="WorkingSet"/>
        /// </summary>
        /// <param name="cancellationToken">the cancellation token</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(CancellationToken cancellationToken) {
            if(null == WorkingSet) {
                throw new InvalidOperationException("Query has no working set");
            }
            return Factory.StartNew<TResult>(() => {
                NamespaceDefinition globalScope;
                cancellationToken.ThrowIfCancellationRequested();
                if(WorkingSet.TryObtainReadLock(LockTimeout, out globalScope)) {
                    try {
                        cancellationToken.ThrowIfCancellationRequested();
                        return Execute(globalScope);
                    } finally {
                        WorkingSet.ReleaseReadLock();
                    }
                }
                throw new TimeoutException();
            });
        }

        /// <summary>
        /// <see cref="Execute(Statement)">Executes the query</see> asynchronously on the <see cref="WorkingSet"/>
        /// </summary>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync() {
            if(null == WorkingSet) {
                throw new InvalidOperationException("Query has no working set");
            }
            return Factory.StartNew<TResult>(Execute);
        }

        /// <summary>
        /// <see cref="Execute(Statement)">Executes the query</see> on the <see cref="WorkingSet"/>
        /// </summary>
        /// <returns>The query result</returns>
        public TResult Execute() {
            if(null == WorkingSet) {
                throw new InvalidOperationException("Query has no working set");
            }
            NamespaceDefinition globalScope;
            if(WorkingSet.TryObtainReadLock(LockTimeout, out globalScope)) {
                try {
                    return Execute(globalScope);
                } finally {
                    WorkingSet.ReleaseReadLock();
                }
            }
            throw new TimeoutException();
        }

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <returns>The query result</returns>
        public abstract TResult Execute(Statement root);
    }

    /// <summary>
    /// This class is used to support query objects that have multiple parameters
    /// </summary>
    /// <typeparam name="TTuple">A tuple of the query parameters</typeparam>
    /// <typeparam name="TResult">The query result type</typeparam>
    public abstract class AbstractQueryBase<TTuple, TResult> {
        private AbstractQueryBase() { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        /// <param name="factory">The task factory for this query</param>
        internal AbstractQueryBase(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory) {
            this.WorkingSet = workingSet;
            this.LockTimeout = lockTimeout;
            this.Factory = factory;
        }

        /// <summary>
        /// The amount of time to wait for the <see cref="AbstractWorkingSet.TryObtainReadLock">read lock</see>
        /// </summary>
        public int LockTimeout { get; private set; }

        /// <summary>
        /// The working set for this query
        /// </summary>
        public AbstractWorkingSet WorkingSet { get; private set; }

        /// <summary>
        /// The task factory for asynchronous operations
        /// </summary>
        protected TaskFactory Factory { get; private set; }

        /// <summary>
        /// <see cref="ExecuteImpl">Executes the query</see> asynchronously on <see cref="WorkingSet"/>
        /// </summary>
        /// <param name="parameterTuple">A tuple with the query parameters</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task with the query result</returns>
        protected Task<TResult> ExecuteAsync(TTuple parameterTuple, CancellationToken cancellationToken) {
            if(null == WorkingSet) {
                throw new InvalidOperationException("Query has no working set");
            }
            Func<TResult> action = () => {
                NamespaceDefinition globalScope;
                cancellationToken.ThrowIfCancellationRequested();
                if(WorkingSet.TryObtainReadLock(LockTimeout, out globalScope)) {
                    try {
                        cancellationToken.ThrowIfCancellationRequested();
                        return ExecuteImpl(globalScope, parameterTuple);
                    } finally {
                        WorkingSet.ReleaseReadLock();
                    }
                }
                throw new TimeoutException();
            };

            return Factory.StartNew<TResult>(action, cancellationToken);
        }

        /// <summary>
        /// <see cref="ExecuteImpl">Executes the query</see> asynchronously on <see cref="WorkingSet"/>
        /// </summary>
        /// <param name="parameterTuple">A tuple with the query parameters</param>
        /// <returns>A task with the query result</returns>
        protected TResult Execute(TTuple parameterTuple) {
            if(null == WorkingSet) {
                throw new InvalidOperationException("Query has no working set");
            }
            NamespaceDefinition globalScope;
            if(WorkingSet.TryObtainReadLock(LockTimeout, out globalScope)) {
                try {
                    return ExecuteImpl(globalScope, parameterTuple);
                } finally {
                    WorkingSet.ReleaseReadLock();
                }
            }
            throw new TimeoutException();
        }

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameterTuple">A tuple with the query parameters</param>
        /// <returns>The query result</returns>
        protected abstract TResult ExecuteImpl(Statement root, TTuple parameterTuple);
    }

    /// <summary>
    /// An abstract query with one parameter
    /// </summary>
    /// <typeparam name="TParam">The query parameter type</typeparam>
    /// <typeparam name="TResult">The query result type</typeparam>
    public abstract class AbstractQuery<TParam, TResult> : AbstractQueryBase<Tuple<TParam>, TResult> {
        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout)
            : base(workingSet, lockTimeout, Task.Factory) { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        /// <param name="factory">The task factory for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory)
            : base(workingSet, lockTimeout, factory) { }

        /// <summary>
        /// <see cref="Execute(Statement,TParam)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter">The first query parameter</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam parameter) {
            return base.ExecuteAsync(Tuple.Create(parameter), new CancellationToken(false));
        }


        /// <summary>
        /// <see cref="Execute(Statement,TParam)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter">The query parameter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam parameter, CancellationToken cancellationToken) {
            return base.ExecuteAsync(Tuple.Create(parameter), cancellationToken);
        }

        /// <summary>
        /// <see cref="Execute(Statement,TParam)">Executes the query</see> on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter">The query parameter</param>
        /// <returns>A task with the query result</returns>
        public TResult Execute(TParam parameter) {
            return base.Execute(Tuple.Create(parameter));
        }

        /// <summary>
        /// Executes the query on the given <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to start the query at</param>
        /// <param name="parameter">The query parameter</param>
        /// <returns>The query result</returns>
        public abstract TResult Execute(Statement root, TParam parameter);

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameterTuple">A tuple with the query parameters</param>
        /// <returns>The query result</returns>
        protected sealed override TResult ExecuteImpl(Statement root, Tuple<TParam> parameterTuple) {
            return Execute(root, parameterTuple.Item1);
        }
    }

    /// <summary>
    /// An abstract query with two parameters
    /// </summary>
    /// <typeparam name="TParam1">The type for the first query parameter</typeparam>
    /// <typeparam name="TParam2">The type for the second query parameter</typeparam>
    /// <typeparam name="TResult">The query result type</typeparam>
    public abstract class AbstractQuery<TParam1, TParam2, TResult>
        : AbstractQueryBase<Tuple<TParam1, TParam2>, TResult> {
        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout)
            : base(workingSet, lockTimeout, Task.Factory) { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        /// <param name="factory">The task factory for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory)
            : base(workingSet, lockTimeout, factory) { }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, CancellationToken cancellationToken) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2), cancellationToken);
        }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2), new CancellationToken(false));
        }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2)">Executes the query</see> on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <returns>The query result</returns>
        public TResult Execute(TParam1 parameter1, TParam2 parameter2) {
            return Execute(Tuple.Create(parameter1, parameter2));
        }

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <returns>The query result</returns>
        public abstract TResult Execute(Statement root, TParam1 parameter1, TParam2 parameter2);

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter">A tuple with the query parameters</param>
        /// <returns>The query result</returns>
        protected sealed override TResult ExecuteImpl(Statement root, Tuple<TParam1, TParam2> parameter) {
            return Execute(root, parameter.Item1, parameter.Item2);
        }
    }

    /// <summary>
    /// An abstract query with three parameters
    /// </summary>
    /// <typeparam name="TParam1">The type for the first query parameter</typeparam>
    /// <typeparam name="TParam2">The type for the second query parameter</typeparam>
    /// <typeparam name="TParam3">The type for the 3rd query parameter</typeparam>
    /// <typeparam name="TResult">The query result type</typeparam>
    public abstract class AbstractQuery<TParam1, TParam2, TParam3, TResult>
        : AbstractQueryBase<Tuple<TParam1, TParam2, TParam3>, TResult> {
        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout)
            : base(workingSet, lockTimeout, Task.Factory) { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        /// <param name="factory">The task factory for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory)
            : base(workingSet, lockTimeout, factory) { }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2,TParam3)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, CancellationToken cancellationToken) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3), cancellationToken);
        }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2,TParam3)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3), new CancellationToken(false));
        }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2,TParam3)">Executes the query</see> on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <returns>The query result</returns>
        public TResult Execute(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3) {
            return Execute(Tuple.Create(parameter1, parameter2, parameter3));
        }

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <returns>The query result</returns>
        public abstract TResult Execute(Statement root, TParam1 parameter1, TParam2 parameter2, TParam3 parameter3);

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter">A tuple with the query parameters</param>
        /// <returns>The query result</returns>
        protected sealed override TResult ExecuteImpl(Statement root, Tuple<TParam1, TParam2, TParam3> parameter) {
            return Execute(root, parameter.Item1, parameter.Item2, parameter.Item3);
        }
    }

    /// <summary>
    /// An abstract query with four parameters
    /// </summary>
    /// <typeparam name="TParam1">The type for the first query parameter</typeparam>
    /// <typeparam name="TParam2">The type for the second query parameter</typeparam>
    /// <typeparam name="TParam3">The type for the third query parameter</typeparam>
    /// <typeparam name="TParam4">The type for the fourth query parameter</typeparam>
    /// <typeparam name="TResult">The query result type</typeparam>
    public abstract class AbstractQuery<TParam1, TParam2, TParam3, TParam4, TResult>
        : AbstractQueryBase<Tuple<TParam1, TParam2, TParam3, TParam4>, TResult> {
        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout)
            : base(workingSet, lockTimeout, Task.Factory) { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        /// <param name="factory">The task factory for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory)
            : base(workingSet, lockTimeout, factory) { }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2,TParam3,TParam4)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <param name="parameter4">The fourth query parameter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, CancellationToken cancellationToken) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3, parameter4), cancellationToken);
        }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2,TParam3,TParam4)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <param name="parameter4">The fourth query parameter</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3, parameter4), new CancellationToken(false));
        }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2,TParam3,TParam4)">Executes the query</see> on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <param name="parameter4">The fourth query parameter</param>
        /// <returns>The query result</returns>
        public TResult Execute(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4) {
            return Execute(Tuple.Create(parameter1, parameter2, parameter3, parameter4));
        }

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <param name="parameter4">The fourth query parameter</param>
        /// <returns>The query result</returns>
        public abstract TResult Execute(Statement root, TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4);

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter">A tuple with the query parameters</param>
        /// <returns>The query result</returns>
        protected sealed override TResult ExecuteImpl(Statement root, Tuple<TParam1, TParam2, TParam3, TParam4> parameter) {
            return Execute(root, parameter.Item1, parameter.Item2, parameter.Item3, parameter.Item4);
        }
    }

    /// <summary>
    /// An abstract query with five parameters
    /// </summary>
    /// <typeparam name="TParam1">The type for the first query parameter</typeparam>
    /// <typeparam name="TParam2">The type for the second query parameter</typeparam>
    /// <typeparam name="TParam3">The type for the third query parameter</typeparam>
    /// <typeparam name="TParam4">The type for the fourth query parameter</typeparam>
    /// <typeparam name="TParam5">The type for the fifth query parameter</typeparam>
    /// <typeparam name="TResult">The query result type</typeparam>
    public abstract class AbstractQuery<TParam1, TParam2, TParam3, TParam4, TParam5, TResult>
        : AbstractQueryBase<Tuple<TParam1, TParam2, TParam3, TParam4, TParam5>, TResult> {
        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout)
            : base(workingSet, lockTimeout, Task.Factory) { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set for this query</param>
        /// <param name="lockTimeout">The read lock timeout to use for this query</param>
        /// <param name="factory">The task factory for this query</param>
        protected AbstractQuery(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory)
            : base(workingSet, lockTimeout, factory) { }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2,TParam3,TParam4,TParam5)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <param name="parameter4">The fourth query parameter</param>
        /// <param name="parameter5">The fifth query parameter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, TParam5 parameter5, CancellationToken cancellationToken) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3, parameter4, parameter5), cancellationToken);
        }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2,TParam3,TParam4,TParam5)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <param name="parameter4">The fourth query parameter</param>
        /// <param name="parameter5">The fifth query parameter</param>
        /// <returns>A task with the query result</returns>
        public Task<TResult> ExecuteAsync(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, TParam5 parameter5) {
            return ExecuteAsync(Tuple.Create(parameter1, parameter2, parameter3, parameter4, parameter5), new CancellationToken(false));
        }

        /// <summary>
        /// <see cref="Execute(Statement,TParam1,TParam2,TParam3,TParam4,TParam5)">Executes the query</see> asynchronously on the <see cref="AbstractQueryBase{TTuple,TResult}.WorkingSet"/>
        /// </summary>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <param name="parameter4">The fourth query parameter</param>
        /// <param name="parameter5">The fifth query parameter</param>
        /// <returns>The query result</returns>
        public TResult Execute(TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, TParam5 parameter5) {
            return Execute(Tuple.Create(parameter1, parameter2, parameter3, parameter4, parameter5));
        }

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter1">The first query parameter</param>
        /// <param name="parameter2">The second query parameter</param>
        /// <param name="parameter3">The third query parameter</param>
        /// <param name="parameter4">The fourth query parameter</param>
        /// <param name="parameter5">The fifth query parameter</param>
        /// <returns>The query result</returns>
        public abstract TResult Execute(Statement root, TParam1 parameter1, TParam2 parameter2, TParam3 parameter3, TParam4 parameter4, TParam5 parameter5);

        /// <summary>
        /// Executes the query on <paramref name="root"/>
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter">A tuple with the query parameters</param>
        /// <returns>The query result</returns>
        protected sealed override TResult ExecuteImpl(Statement root, Tuple<TParam1, TParam2, TParam3, TParam4, TParam5> parameter) {
            return Execute(root, parameter.Item1, parameter.Item2, parameter.Item3, parameter.Item4, parameter.Item5);
        }
    }
}
