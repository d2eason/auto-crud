using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Firebend.AutoCrud.Core.Abstractions.Services;
using Firebend.AutoCrud.Core.Extensions;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Core.Interfaces.Services.Entities;
using Firebend.AutoCrud.Core.Models.Searching;
using Firebend.AutoCrud.Mongo.Interfaces;

namespace Firebend.AutoCrud.Mongo.Abstractions.Entities
{
    public abstract class MongoEntitySearchService<TKey, TEntity, TSearch> : AbstractEntitySearchService<TEntity, TSearch>, IEntitySearchService<TKey, TEntity, TSearch>
        where TKey : struct
        where TEntity : class, IEntity<TKey>
        where TSearch : EntitySearchRequest
    {
        private readonly IEntityDefaultOrderByProvider<TKey, TEntity> _orderByProvider;
        private readonly IMongoReadClient<TKey, TEntity> _readClient;

        public MongoEntitySearchService(IMongoReadClient<TKey, TEntity> readClient,
            IEntityDefaultOrderByProvider<TKey, TEntity> orderByProvider)
        {
            _readClient = readClient;
            _orderByProvider = orderByProvider;
        }

        public async Task<List<TEntity>> SearchAsync(TSearch request, CancellationToken cancellationToken = default)
        {
            var results = await PageAsync(request, cancellationToken);

            return results?.Data?.ToList();
        }

        public Task<EntityPagedResponse<TEntity>> PageAsync(TSearch request, CancellationToken cancellationToken = default)
        {
            return _readClient.PageAsync(request?.Search,
                BuildSearchExpression(request),
                request?.PageNumber,
                request?.PageSize,
                request?.DoCount ?? false,
                GetOrderByGroups(request),
                cancellationToken
            );
        }

        private IEnumerable<(Expression<Func<TEntity, object>> order, bool ascending)> GetOrderByGroups(TSearch search)
        {
            var orderByGroups = search?.OrderBy?.ToOrderByGroups<TEntity>()?.ToList();

            if (!(orderByGroups?.Any() ?? false))
            {
                var orderBy = _orderByProvider.OrderBy;

                if (orderBy != default && orderBy.func != null)
                {
                    orderByGroups = new List<(Expression<Func<TEntity, object>> order, bool ascending)>
                    {
                        orderBy
                    };
                }
            }

            return orderByGroups;
        }

        protected virtual Expression<Func<TEntity, bool>> BuildSearchFilter(TSearch search)
        {
            return null;
        }

         protected virtual Expression<Func<TEntity, bool>> BuildSearchExpression(TSearch search)
        {
            return GetSearchExpression(BuildSearchFilter(search), search);
        }
    }
}