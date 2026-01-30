using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Warehouse.Domain.Domain;
using Warehouse.Repository.Interface;

namespace Warehouse.Repository.Implementation;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _entities;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _entities = _context.Set<T>();
    }

    public T Insert(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _entities.Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public List<T> InsertMany(List<T> entities)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (entities.Count == 0) return entities;

        _entities.AddRange(entities);
        _context.SaveChanges();
        return entities;
    }

    public T Update(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _entities.Update(entity);
        _context.SaveChanges();
        return entity;
    }

    public T Delete(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _entities.Remove(entity);
        _context.SaveChanges();
        return entity;
    }

    public E? Get<E>(
        Expression<Func<T, E>> selector,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        IQueryable<T> query = _entities;

        if (include != null) query = include(query);
        if (predicate != null) query = query.Where(predicate);
        if (orderBy != null) query = orderBy(query);

        return query.Select(selector).FirstOrDefault();
    }

    public IEnumerable<E> GetAll<E>(
        Expression<Func<T, E>> selector,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        IQueryable<T> query = _entities;

        if (include != null) query = include(query);
        if (predicate != null) query = query.Where(predicate);
        if (orderBy != null) query = orderBy(query);

        // materialize here so callers don’t keep a live query around
        return query.Select(selector).ToList();
    }
}

