using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    public abstract class EntityGroupBase : IEntityGroup
    {
        public EntityGroupBase()
        {
            _entities = new Dictionary<long, ILinkEntity>();
        }

        private Dictionary<long, ILinkEntity> _entities;
        Dictionary<long, ILinkEntity> IEntityGroup.entities => _entities;
        public long count => _entities.Count;

        public abstract bool IsMatch(ILinkEntity entity);

        public virtual bool AddEntity(ILinkEntity entity)
        {
            if(entity == null) return false;
            if(!IsMatch(entity)) return false;
            _entities[entity.index] = entity;
            return true;
        }

        public virtual bool RemoveEntity(ILinkEntity entity)
        {
            if (entity == null) return false;
            return RemoveEntity(entity.index);
        }
        
        public virtual bool RemoveEntity(long index)
        {
            if (!_entities.Remove(index))
            {
                return false;
            }
            return true;
        }

        public virtual void Clear()
        {
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

        public ILinkEntity[] AllEntity()
        {
            return _entities.Values.ToArray();
        }

        public abstract void Update(float deltaTime);
    }
}