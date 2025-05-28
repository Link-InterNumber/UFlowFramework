using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    public sealed class EntityManager
    {
        private List<IEntityGroup> _entityGroups = new List<IEntityGroup>();
        private Dictionary<long, ILinkEntity> _entities = new Dictionary<long, ILinkEntity>();
        private HashSet<long> _waitToRemove = new HashSet<long>();
        private Queue<ILinkEntity> _waitToAdd = new Queue<ILinkEntity>();

        public void AddEntityGroup(IEntityGroup entityGroup)
        {
            _entityGroups.Add(entityGroup);
        }
        
        public void RemoveEntityGroup(IEntityGroup entityGroup)
        {
            _entityGroups.Remove(entityGroup);
        }
        
        public void Update(float deltaTime)
        {
            // 添加新entity
            if (_waitToAdd.Count > 0)
            {
                while (_waitToAdd.Count > 0)
                {
                    var entity = _waitToAdd.Dequeue();
                    var added = false;
                    // Use for loop instead of foreach for better performance
                    for (int i = 0; i < _entityGroups.Count; i++)
                    {
                        if (_entityGroups[i].AddEntity(entity))
                        {
                            added = true;
                            // Break early if entity can only belong to one group
                            // break;
                        }
                    }
                    if (added) _entities.Add(entity.index, entity);
                }
            }

            // 运行Group逻辑
            foreach (var entityGroup in _entityGroups)
            {
                entityGroup.Update(deltaTime);
            }

            // 移除entity
            if (_waitToRemove.Count <= 0) return;
            foreach (var index in _waitToRemove)
            {
                RemoveEntityByIndex(index);
            }
            _waitToRemove.Clear();
        }

        public T GetEntityGroup<T>() where T : class, IEntityGroup
        {
            // Optimized type checking with direct cast
            for (int i = 0; i < _entityGroups.Count; i++)
            {
                if (_entityGroups[i] is T entityGroup)
                {
                    return entityGroup;
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
            if (entity == null) return;
            _waitToAdd.Enqueue(entity);
        }
        
        public void RemoveEntity(ILinkEntity entity)
        {
            // Optimized group removal with for loop
            _waitToRemove.Add(entity.index);
        }
        
        public void RemoveEntity(long index)
        {
            _waitToRemove.Add(index);
        }

        private void RemoveEntityByIndex(long index)
        {
            for (int i = 0; i < _entityGroups.Count; i++)
            {
                _entityGroups[i].RemoveEntity(index);
            }
            
            if (_entities.TryGetValue(index, out var entity))
            {
                _entities.Remove(index);
                entity.Destroy();
            }
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
            return _entities.TryGetValue(index, out var entity) ? entity : null;
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
            if (entityGroup == null) return;
            foreach (var entity in entityGroup.AllEntity())
            {
                action(entity);
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