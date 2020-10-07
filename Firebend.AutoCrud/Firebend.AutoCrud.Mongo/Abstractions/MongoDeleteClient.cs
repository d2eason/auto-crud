using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Internal;
using Firebend.AutoCrud.Core.Interfaces;
using Firebend.AutoCrud.Mongo.Interfaces;
using MongoDB.Driver;

namespace Firebend.AutoCrud.Mongo.Abstractions
{
    public abstract class MongoDeleteClient<TEntity, TKey> : MongoClientBaseEntity<TEntity, TKey>, IMongoDeleteClient<TEntity, TKey>
        where TKey : struct
        where TEntity : IEntity<TKey>
    {
        protected MongoDeleteClient(IMongoClient client, ILogger logger, IMongoEntityConfiguration entityConfiguration) : base(client, logger, entityConfiguration)
        {
        }

        public async Task<TEntity> DeleteAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
        {
            filter = BuildFilters(filter);
            
            var mongoCollection = GetCollection();

            var result = await RetryErrorAsync(() => mongoCollection.FindOneAndDeleteAsync(filter, null, cancellationToken));

            if (result != null)
            {
                //todo: domain events
                //await PublishDeleteAsync(result, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
    }
}