#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace SqlParser.ObjectPool
{
    /// <summary>
    /// A provider of <see cref="ObjectPool{T}"/> instances.
    /// </summary>
    public abstract class ObjectPoolProvider
    {
        /// <summary>
        /// Creates an <see cref="ObjectPool"/>.
        /// </summary>
        /// <typeparam name="T">The type to create a pool for.</typeparam>
        public ObjectPool<T> Create<T>() where T : class, new()
        {
            return Create<T>(new DefaultPooledObjectPolicy<T>());
        }

        /// <summary>
        /// Creates an <see cref="ObjectPool"/> with the given <see cref="IPooledObjectPolicy{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type to create a pool for.</typeparam>
        public abstract ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy) where T : class;
    }
    /// <summary>
    /// A pool of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    public abstract class ObjectPool<T> where T : class
    {
        /// <summary>
        /// Gets an object from the pool if one is available, otherwise creates one.
        /// </summary>
        /// <returns>A <typeparamref name="T"/>.</returns>
        public abstract T Get();

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        /// <param name="obj">The object to add to the pool.</param>
        public abstract void Return(T obj);
    }

    /// <summary>
    /// Methods for creating <see cref="ObjectPool{T}"/> instances.
    /// </summary>
    public static class ObjectPool
    {
        /// <inheritdoc />
        public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T>? policy = null) where T : class, new()
        {
            var provider = new DefaultObjectPoolProvider();
            return provider.Create(policy ?? new DefaultPooledObjectPolicy<T>());
        }
    }

    /// <summary>
    /// Default implementation for <see cref="PooledObjectPolicy{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of object which is being pooled.</typeparam>
    public class DefaultPooledObjectPolicy<T> : PooledObjectPolicy<T> where T : class, new()
    {
        /// <inheritdoc />
        public override T Create()
        {
            return new T();
        }

        /// <inheritdoc />
        public override bool Return(T obj)
        {
            // DefaultObjectPool<T> doesn't call 'Return' for the default policy.
            // So take care adding any logic to this method, as it might require changes elsewhere.
            return true;
        }
    }
    /// <summary>
    /// A base type for <see cref="IPooledObjectPolicy{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of object which is being pooled.</typeparam>
    public abstract class PooledObjectPolicy<T> : IPooledObjectPolicy<T> where T : notnull
    {
        /// <inheritdoc />
        public abstract T Create();

        /// <inheritdoc />
        public abstract bool Return(T obj);
    }

    /// <summary>
    /// Represents a policy for managing pooled objects.
    /// </summary>
    /// <typeparam name="T">The type of object which is being pooled.</typeparam>
    public interface IPooledObjectPolicy<T> where T : notnull
    {
        /// <summary>
        /// Create a <typeparamref name="T"/>.
        /// </summary>
        /// <returns>The <typeparamref name="T"/> which was created.</returns>
        T Create();

        /// <summary>
        /// Runs some processing when an object was returned to the pool. Can be used to reset the state of an object and indicate if the object should be returned to the pool.
        /// </summary>
        /// <param name="obj">The object to return to the pool.</param>
        /// <returns><see langword="true" /> if the object should be returned to the pool. <see langword="false" /> if it's not possible/desirable for the pool to keep the object.</returns>
        bool Return(T obj);
    }

    /// <summary>
    /// The default <see cref="ObjectPoolProvider"/>.
    /// </summary>
    public class DefaultObjectPoolProvider : ObjectPoolProvider
    {
        /// <summary>
        /// The maximum number of objects to retain in the pool.
        /// </summary>
        public int MaximumRetained { get; set; } = Environment.ProcessorCount * 2;

        /// <inheritdoc/>
        public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
            {
                return new DisposableObjectPool<T>(policy, MaximumRetained);
            }

            return new DefaultObjectPool<T>(policy, MaximumRetained);
        }
    }

    internal sealed class DisposableObjectPool<T> : DefaultObjectPool<T>, IDisposable where T : class
    {
        private volatile bool _isDisposed;

        public DisposableObjectPool(IPooledObjectPolicy<T> policy)
            : base(policy)
        {
        }

        public DisposableObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
            : base(policy, maximumRetained)
        {
        }

        public override T Get()
        {
            if (_isDisposed)
            {
                ThrowObjectDisposedException();
            }

            return base.Get();

            void ThrowObjectDisposedException()
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public override void Return(T obj)
        {
            // When the pool is disposed or the obj is not returned to the pool, dispose it
            if (_isDisposed || !ReturnCore(obj))
            {
                DisposeItem(obj);
            }
        }

        private bool ReturnCore(T obj)
        {
            bool returnedToPool = false;

            if (_isDefaultPolicy || (_fastPolicy?.Return(obj) ?? _policy.Return(obj)))
            {
                if (_firstItem == null && Interlocked.CompareExchange(ref _firstItem, obj, null) == null)
                {
                    returnedToPool = true;
                }
                else
                {
                    var items = _items;
                    for (var i = 0; i < items.Length && !(returnedToPool = Interlocked.CompareExchange(ref items[i].Element, obj, null) == null); i++)
                    {
                    }
                }
            }

            return returnedToPool;
        }

        public void Dispose()
        {
            _isDisposed = true;

            DisposeItem(_firstItem);
            _firstItem = null;

            ObjectWrapper[] items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                DisposeItem(items[i].Element);
                items[i].Element = null;
            }
        }

        private static void DisposeItem(T? item)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }



    /// <summary>
    /// Default implementation of <see cref="ObjectPool{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type to pool objects for.</typeparam>
    /// <remarks>This implementation keeps a cache of retained objects. This means that if objects are returned when the pool has already reached "maximumRetained" objects they will be available to be Garbage Collected.</remarks>
    public class DefaultObjectPool<T> : ObjectPool<T> where T : class
    {
        private protected readonly ObjectWrapper[] _items;
        private protected readonly IPooledObjectPolicy<T> _policy;
        private protected readonly bool _isDefaultPolicy;
        private protected T? _firstItem;

        // This class was introduced in 2.1 to avoid the interface call where possible
        private protected readonly PooledObjectPolicy<T>? _fastPolicy;

        /// <summary>
        /// Creates an instance of <see cref="DefaultObjectPool{T}"/>.
        /// </summary>
        /// <param name="policy">The pooling policy to use.</param>
        public DefaultObjectPool(IPooledObjectPolicy<T> policy)
            : this(policy, Environment.ProcessorCount * 2)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DefaultObjectPool{T}"/>.
        /// </summary>
        /// <param name="policy">The pooling policy to use.</param>
        /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
        public DefaultObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _fastPolicy = policy as PooledObjectPolicy<T>;
            _isDefaultPolicy = IsDefaultPolicy();

            // -1 due to _firstItem
            _items = new ObjectWrapper[maximumRetained - 1];

            bool IsDefaultPolicy()
            {
                var type = policy.GetType();

                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefaultPooledObjectPolicy<>);
            }
        }

        /// <inheritdoc />
        public override T Get()
        {
            var item = _firstItem;
            if (item == null || Interlocked.CompareExchange(ref _firstItem, null, item) != item)
            {
                var items = _items;
                for (var i = 0; i < items.Length; i++)
                {
                    item = items[i].Element;
                    if (item != null && Interlocked.CompareExchange(ref items[i].Element, null, item) == item)
                    {
                        return item;
                    }
                }

                item = Create();
            }

            return item;
        }

        // Non-inline to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private T Create() => _fastPolicy?.Create() ?? _policy.Create();

        /// <inheritdoc />
        public override void Return(T obj)
        {
            if (_isDefaultPolicy || (_fastPolicy?.Return(obj) ?? _policy.Return(obj)))
            {
                if (_firstItem != null || Interlocked.CompareExchange(ref _firstItem, obj, null) != null)
                {
                    var items = _items;
                    for (var i = 0; i < items.Length && Interlocked.CompareExchange(ref items[i].Element, obj, null) != null; ++i)
                    {
                    }
                }
            }
        }

        // PERF: the struct wrapper avoids array-covariance-checks from the runtime when assigning to elements of the array.
        [DebuggerDisplay("{Element}")]
        private protected struct ObjectWrapper
        {
            public T? Element;
        }
    }


    /// <summary>
    /// Extension methods for <see cref="ObjectPoolProvider"/>.
    /// </summary>
    public static class ObjectPoolProviderExtensions
    {
        /// <summary>
        /// Creates an <see cref="ObjectPool{T}"/> that pools <see cref="StringBuilder"/> instances.
        /// </summary>
        /// <param name="provider">The <see cref="ObjectPoolProvider"/>.</param>
        /// <returns>The <see cref="ObjectPool{T}"/>.</returns>
        public static ObjectPool<StringBuilder> CreateStringBuilderPool(this ObjectPoolProvider provider)
        {
            return provider.Create<StringBuilder>(new StringBuilderPooledObjectPolicy());
        }

        /// <summary>
        /// Creates an <see cref="ObjectPool{T}"/> that pools <see cref="StringBuilder"/> instances.
        /// </summary>
        /// <param name="provider">The <see cref="ObjectPoolProvider"/>.</param>
        /// <param name="initialCapacity">The initial capacity to initialize <see cref="StringBuilder"/> instances with.</param>
        /// <param name="maximumRetainedCapacity">The maximum value for <see cref="StringBuilder.Capacity"/> that is allowed to be
        /// retained, when an instance is returned.</param>
        /// <returns>The <see cref="ObjectPool{T}"/>.</returns>
        public static ObjectPool<StringBuilder> CreateStringBuilderPool(
            this ObjectPoolProvider provider,
            int initialCapacity,
            int maximumRetainedCapacity)
        {
            var policy = new StringBuilderPooledObjectPolicy()
            {
                InitialCapacity = initialCapacity,
                MaximumRetainedCapacity = maximumRetainedCapacity,
            };

            return provider.Create<StringBuilder>(policy);
        }
    }


    /// <summary>
    /// A policy for pooling <see cref="StringBuilder"/> instances.
    /// </summary>
    public class StringBuilderPooledObjectPolicy : PooledObjectPolicy<StringBuilder>
    {
        /// <summary>
        /// Gets or sets the initial capacity of pooled <see cref="StringBuilder"/> instances.
        /// </summary>
        /// <value>Defaults to <c>100</c>.</value>
        public int InitialCapacity { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum value for <see cref="StringBuilder.Capacity"/> that is allowed to be
        /// retained, when <see cref="Return(StringBuilder)"/> is invoked.
        /// </summary>
        /// <value>Defaults to <c>4096</c>.</value>
        public int MaximumRetainedCapacity { get; set; } = 4 * 1024;

        /// <inheritdoc />
        public override StringBuilder Create()
        {
            return new StringBuilder(InitialCapacity);
        }

        /// <inheritdoc />
        public override bool Return(StringBuilder obj)
        {
            if (obj.Capacity > MaximumRetainedCapacity)
            {
                // Too big. Discard this one.
                return false;
            }

            obj.Clear();
            return true;
        }
    }
}
