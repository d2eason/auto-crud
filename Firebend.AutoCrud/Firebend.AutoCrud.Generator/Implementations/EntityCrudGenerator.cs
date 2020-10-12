using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Firebend.AutoCrud.Core.Abstractions;
using Firebend.AutoCrud.Core.Extensions;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Core.Interfaces.Services;
using Firebend.AutoCrud.Core.Interfaces.Services.ClassGeneration;
using Microsoft.Extensions.DependencyInjection;

namespace Firebend.AutoCrud.Generator.Implementations
{
    public abstract class EntityCrudGenerator<TBuilder> : IEntityCrudGenerator
        where TBuilder : EntityBuilder, new ()
    {
        private readonly IDynamicClassGenerator _classGenerator;

        protected EntityCrudGenerator(IDynamicClassGenerator classGenerator)
        {
            _classGenerator = classGenerator;
        }

        protected EntityCrudGenerator() : this(new DynamicClassGenerator())
        {
            
        }

        public List<EntityBuilder> Builders { get; set; }
        
        public virtual void Generate(IServiceCollection collection)
        {
            foreach (var builder in Builders)
            {
                Generate(collection, builder);
            }
        }

        protected virtual void Generate(IServiceCollection collection, EntityBuilder builder)
        {
            var implementedTypes = new List<Type>();
            
            builder.Build();
            
            foreach (var (key, value) in OrderByDependencies(builder.Registrations))
            {
                var typeToImplement = value;
                var signature = $"{builder.EntityType.Name}_{typeToImplement.Name}";

                var interfaceImplementations = new List<Type>
                {
                    key
                };

                if (key.IsInterface)
                {
                    interfaceImplementations.Add(_classGenerator.GenerateInterface(key, $"I{signature}"));
                }

                interfaceImplementations = interfaceImplementations.Distinct().ToList();


                var generatedImplementation = _classGenerator.GenerateDynamicClass(typeToImplement,
                    signature,
                    implementedTypes,
                    interfaceImplementations.ToArray(),
                    null
                );

                interfaceImplementations.ForEach(iFace => collection.AddScoped(iFace, generatedImplementation));

                if (interfaceImplementations.Count == 0)
                {
                    collection.AddScoped(generatedImplementation);
                }

                implementedTypes = implementedTypes.Union(interfaceImplementations).Distinct().ToList();
            }

            foreach (var (key, value) in builder.InstanceRegistrations)
            {
                collection.AddSingleton(key, value);
            }
        }
        
        private static IEnumerable<KeyValuePair<Type, Type>> OrderByDependencies(IDictionary<Type, Type> source)
        {
            var orderedTypes = new List<KeyValuePair<Type, Type>>();

            if (source != null)
            {
                var maxVisits = source.Count;
                
                var typesToAdd = source.ToDictionary(x => x.Key, x => x.Value);
                
                while (typesToAdd.Count > 0)
                {
                    foreach (var type in typesToAdd.ToArray())
                    {
                        if (CanAddType(type, typesToAdd))
                        {
                            orderedTypes.Add(type);
                            typesToAdd.Remove(type.Key);
                        }
                    }

                    maxVisits--;
                    
                    if (maxVisits < 0)
                    {
                        throw new ApplicationException("Cannot resolve dependencies for DefaultCrud (do you have a circular reference?)");
                    }
                }
            }
            return orderedTypes;
        }
        
        private static bool CanAddType(KeyValuePair<Type, Type> type, IDictionary<Type, Type> typesToAdd)
        {
            return type.Value.GetConstructors(
                    BindingFlags.Public | 
                    BindingFlags.NonPublic | 
                    BindingFlags.Instance)
                .All(
                    info => info.GetParameters().All(parameterInfo => 
                        !typesToAdd.ContainsKey(parameterInfo.ParameterType) &&
                        typesToAdd
                            .All(types => !parameterInfo.ParameterType.IsAssignableFrom(types.Key))
                    )
                );
        }
        
        private static List<Type> GetCustomImplementations(IDictionary<Type, Type> configureRegistrations,
            IDictionary<Type, Type> typesToImplement)
        {
            var extraInterfaces = new List<Type>();

            if (configureRegistrations != null)
            {
                foreach (var (key, value) in configureRegistrations)
                {
                    if (!key.IsAssignableFrom(value))
                    {
                        throw new InvalidCastException(
                            $"Cannot use custom configuration {value.Name} to implement {key.Name}");
                    }

                    var implementedInterfaces = value.GetInterfaces();
                    var matchingInterface =
                        implementedInterfaces.FirstOrDefault(x => x.Name == $"I{value.Name}");

                    if (matchingInterface != null)
                    {
                        extraInterfaces.Add(matchingInterface);
                    }

                    if (typesToImplement.ContainsKey(key))
                    {
                        typesToImplement[key] = value;
                    }
                    else
                    {
                        typesToImplement.Add(key, value);
                    }
                }
            }

            return extraInterfaces;
        }
        
        public EntityCrudGenerator<TBuilder> AddBuilder(TBuilder builder, Func<TBuilder, TBuilder> configure = null)
        {
            Builders ??= new List<EntityBuilder>();

            if (configure != null)
            {
                builder = configure(builder);
            }
            
            Builders.Add(builder);

            return this;
        }
        
        public EntityCrudGenerator<TBuilder> AddBuilder<T>(Func<TBuilder, TBuilder> configure)
            
        {
            var builder = configure(new TBuilder());
            
             return AddBuilder(builder, configure);
        }
        
        public EntityCrudGenerator<TBuilder> AddBuilder<TEntity, TEntityKey>(Func<TBuilder, TBuilder> configure)
            where TEntity : IEntity<TEntityKey>
            where TEntityKey : struct
        {
            var builder = configure(new TBuilder().ForEntity<TBuilder, TEntity, TEntityKey>());
            
            return AddBuilder(builder, configure);
        }
    }
}