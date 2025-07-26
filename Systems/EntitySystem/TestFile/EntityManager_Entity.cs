namespace TestEntity
{
   public partial class EntityManager
   {
      public void RequestCreateEntity(params Component[] components)
      {
         var request = new EntityCreateRequest()
         {
            components = components,
         };
         _entityCreateBuffer.Add(request);
      }

      private Entity CreateEntity(EntityCreateRequest request)
      {

      }

      public bool HasComponent<T>(Entity entity) where T : Component
      {
         if (entity.isDestroyed) return false;
         var archetypeMask = _componentMaskMap[typeof(T)];
         if (!entity.archetype.IsSupersetTo(archetypeMask))
         {
            return false;
         }
         return true;
      }

      public T ReadComponent<T>(Entity entity) where T : Component
      {
         if (entity.isDestroyed) return default;
         var archetypeMask = _componentMaskMap[typeof(T)];
         if (!entity.archetype.IsSupersetTo(archetypeMask))
         {

            return default;
         }
         if (_chunks.TryGetValue(entity.chunkIndex, out var chunk))
         {
            return chunk.componantMap.TryGetValue(archetypeMask, out var components)
                ? components[entity.indexInChunk] as T
                : default;
         }
         return default;
      }

      public void WriteComponent(Entity entity, params Component[] components)
      {
         if (entity.isDestroyed || components == null || components.Length == 0) return;

         if (!_chunks.TryGetValue(entity.chunkIndex, out var chunk)) return;
         foreach (var component in components)
         {
            var mask = _componentMaskMap[component.GetType()];
            chunk.componantMap[archetypeMask][entity.indexInChunk] = components;
         }
      }
   }
}