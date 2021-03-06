using System;

namespace Firebend.AutoCrud.Core.Interfaces.Models
{
    public interface IEntity<TKey>
        where TKey : struct
    {
        TKey Id { get; set; }
    }

    public class FooEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }
}