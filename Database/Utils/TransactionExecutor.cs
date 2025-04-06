using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoreKit.Database.Utils
{
    public interface ITransactionExecutor<TContext>
    {
        Task<T> ExecuteTransaction<T>(Func<T> method);
        Task<T> ExecuteAsyncTransaction<T>(Func<Task<T>> method);
        Task<List<T>> ExecuteAsyncTransaction<T>(params Func<Task<T>>[] methods);
        Task<List<T>> ExecuteTransaction<T>(params Func<T>[] methods);
    }
/*
    public class TransactionExecutor<TContext> : ITransactionExecutor<TContext> where TContext : DbContext
    {
        private readonly TContext _context;

        public TransactionExecutor(TContext context)
        {
            _context = context;
        }

        public async Task<T> ExecuteAsyncTransaction<T>(Func<Task<T>> method)
        {
            T result = default(T);

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                // Calling original function
                result = await method.Invoke();
                await transaction.CommitAsync();
            });

            return result;
        }
        public async Task<List<T>> ExecuteAsyncTransaction<T>(params Func<Task<T>>[] methods)
        {
            var results = new List<T>();

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                foreach (var method in methods)
                {
                    var result = await method.Invoke();
                    results.Add(result);
                }
                await transaction.CommitAsync();
            });

            return results;
        }

        public async Task<T> ExecuteTransaction<T>(Func<T> method)
        {
            T result = default(T);

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                // Calling original function
                result = method.Invoke();
                await transaction.CommitAsync();
            });

            return result;
        }

        public async Task<List<T>> ExecuteTransaction<T>(params Func<T>[] methods)
        {
            var results = new List<T>();

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                foreach (var method in methods)
                {
                    var result = method.Invoke();
                    results.Add(result);
                }
                await transaction.CommitAsync();
            });

            return results;
        }
    }*/
}
