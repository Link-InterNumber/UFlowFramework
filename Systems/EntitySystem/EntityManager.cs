using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    public sealed class EntityManager
    {
        private LinkedList<IEntityGroup> _entityGroups = new LinkedList<IEntityGroup>();
        private Dictionary<long, ILinkEntity> _entities = new Dictionary<long, ILinkEntity>();
        private HashSet<long> _waitToRemove = new HashSet<long>();

        public void AddEntityGroup(IEntityGroup entityGroup)
        {
            _entityGroups.AddLast(entityGroup);
        }
        
        public void RemoveEntityGroup(IEntityGroup entityGroup)
        {
            _entityGroups.Remove(entityGroup);
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entityGroup in _entityGroups)
            {
                entityGroup.Update(deltaTime);
            }
            if (_waitToRemove.Count > 0)
            {
                foreach (var index in _waitToRemove)
                {
                    RemoveEntityByIndex(index);
                }
                _waitToRemove.Clear();
            }
        }

        public T GetEntityGroup<T>() where T : class, IEntityGroup
        {
            foreach (var entityGroup in _entityGroups)
            {
                if (entityGroup is T)
                {
                    return entityGroup as T;
                }
            }
            return null;
        }
        
        public IEntityGroup[] AllEntityGroup()
        {
            return _entityGroups.ToArray();
        }
        
        public void RemoveEntityGroup<T>() where T : class, IEntityGroup
        {
            var entityGroup = GetEntityGroup<T>();
            if (entityGroup != null)
            {
                _entityGroups.Remove(entityGroup);
            }
        }
        
        public void AddEntity(ILinkEntity entity)
        {
            var added = false;
            foreach (var entityGroup in _entityGroups)
            {
                if(entityGroup.AddEntity(entity) && !added)
                {
                    added = true;
                }
            }
            if(added) _entities.Add(entity.index, entity);
        }
        
        private void RemoveEntity(ILinkEntity entity)
        {
            if(entity == null) return;
            foreach (var entityGroup in _entityGroups)
            {
                entityGroup.RemoveEntity(entity);
            }
            _entities.Remove(entity.index);
            entity.Destroy();
        }
        
        private void RemoveEntityByIndex(long index)
        {
            foreach (var entityGroup in _entityGroups)
            {
                entityGroup.RemoveEntity(index);
            }
            if(_entities.TryGetValue(index, out var entity))
            {
                _entities.Remove(index);
                entity.Destroy();
            }
        }
        
        public void RemoveEntity(long index)
        {
            _waitToRemove.Add(index);
        }
        
        public void ClearEntity()
        {
            foreach (var entityGroup in _entityGroups)
            {
                entityGroup.Clear();
            }
            _entities.Clear();
        }
        
        public void Clear()
        {
            _entityGroups.Clear();
            _entities.Clear();
        }
        
        public ILinkEntity GetEntity(long index)
        {
            if (_entities.TryGetValue(index, out var entity))
            {
                return entity;
            }
            return null;
        }
        
        public ILinkEntity[] GetEntityByGroup<T>() where T : class, IEntityGroup
        {
            var entityGroup = GetEntityGroup<T>();
            if (entityGroup != null)
            {
                return entityGroup.AllEntity();
            }
            return Array.Empty<ILinkEntity>();
        }
        
        public void ForEachEntity<T>(Action<ILinkEntity> action) where T : class, IEntityGroup
        {
            var entityGroup = GetEntityGroup<T>();
            if (entityGroup != null)
            {
                foreach (var entity in entityGroup.AllEntity())
                {
                    action(entity);
                }
            }
        }
        
        public void AllEntity(Action<ILinkEntity> action)
        {
            foreach (var entity in _entities.Values)
            {
                action(entity);
            }
        }
    }
}