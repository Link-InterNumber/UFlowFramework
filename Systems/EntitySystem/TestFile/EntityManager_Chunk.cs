namespace TestEntity
{
   public partial class EntityManager
   {
      private Dictionary<Type, ulong> _componentMaskMap;
      private Dictionary<ulong, Chunk> _chunks;

      private void InitComponentMaskMap()
      {
         _componentMaskMap = new Dictionary<Type, ulong>();
         _chunks = new Dictionary<long, Chunk>();
         // 反射获取所有Component的子类，并赋值唯一id
         var componentType = typeof(Component);
         var allComponentTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => componentType.IsAssignableFrom(t) && t != componentType && !t.IsAbstract);

         ulong id = 1;
         foreach (var type in allComponentTypes)
         {
            // 确保id在64位无符号整数范围内
            if (id == 0) throw new InvalidOperationException("Component mask overflow, too many components defined.");
            if (_componentMaskMap.ContainsKey(type))
            {
               throw new InvalidOperationException($"Component type {type.Name} is already registered.");
            }
            _componentMaskMap[type] = id;
            id <<= 1;
         }
      }

      private Archetype GetArchetype(ulong inputMask, params Component[] components)
      {
         ulong mask = inputMask;
         foreach (var component in components)
         {
            if (_componentMaskMap.TryGetValue(component.GetType(), out ulong componentMask))
            {
               mask |= componentMask;
            }
            else
            {
               throw new InvalidOperationException($"Component type {component.GetType().Name} is not registered.");
            }
         }
         return new Archetype(mask);
      }

      private Archetype GetArchetype(ulong inputMask, params Type[] components)
      {
         ulong mask = inputMask;
         foreach (var component in components)
         {
            if (_componentMaskMap.TryGetValue(component, out ulong componentMask))
            {
               mask |= componentMask;
            }
            else
            {
               throw new InvalidOperationException($"Component type {component.GetType().Name} is not registered.");
            }
         }
         return new Archetype(mask);
      }

      private Chunk GetOrCreateChunk(Archetype archetype, int entityCount)
      {
         long chunkKey = archetype.componentMask; // 使用componentMask作为唯一标识
         if (!_chunks.TryGetValue(chunkKey, out Chunk chunk))
         {
            chunk = new Chunk(archetype, entityCount);
            _chunks[chunkKey] = chunk;
         }
         return chunk;
      }

      public (Entity[] entities, Span<T> components) Query<T>()
      // Where T : Component
      {
         var archetypeMask = _componentMaskMap[typeof(T)];
         if (_chunks.TryGetValue(archetypeMask, out var chunk))
         {
            var components = chunk.componantMap[archetypeMask].asSpan<T>().Slice(0, chunk.entityCount);
            return (chunk.entities, components);
         }
         foreach (var kvp in _chunks)
         {
            var value = kvp.Value;
            if (!value.archetype.IsSupersetTo(archetypeMask)) continue;
            var entities = value.entities;
            var components = value.componantMap[archetypeMask].asSpan<T>().Slice(0, value.entityCount);
            return (entities, components);
         }
         return (Array.Empty<Entity>(), Span<T>.Empty);
      }

      public Entity[] Query(Archetype archetype)
      {
         if (_chunks.TryGetValue(archetype.componentMask, out var chunk))
         {
            return chunk.entities;
         }
         foreach (var kvp in _chunks)
         {
            var value = kvp.Value;
            if (!value.archetype.IsSupersetTo(archetype)) continue;
            return value.entities;
         }
         return Array.Empty<Entity>();
      }
   }
}