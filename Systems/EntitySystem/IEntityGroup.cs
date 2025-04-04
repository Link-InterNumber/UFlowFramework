using System.Collections.Generic;

namespace PowerCellStudio
{
    public interface IEntityGroup
    {
        internal Dictionary<long, ILinkEntity> entities { get; }
        
        public long count { get; }

        public bool IsMatch(ILinkEntity entity);

        public bool AddEntity(ILinkEntity entity);

        public bool RemoveEntity(ILinkEntity entity);
        
        public bool RemoveEntity(long index);

        public void Clear();

        public ILinkEntity GetEntity(long index);

        public ILinkEntity[] AllEntity();
        
        public void Update(float deltaTime);
    }
}