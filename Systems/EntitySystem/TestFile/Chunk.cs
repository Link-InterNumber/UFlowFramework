namespace TestEntity
{
   public class Chunk
   {
      public Archetype archetype;
      public int entityCount;

      public Entity[] entities;

      public Dictionary<ulong, Component[]> componantMap;

      public Chunk(Archetype archetype, int entityCount)
      {
         this.archetype = archetype;
         this.entities = new Entity[entityCount];
         this.entityCount = 0;
         this.componantMap = new Dictionary<ulong, Component[]>();
         // 初始化this.componantMap
         foreach (var componentType in archetype.ComponentTypes)
         {
            componantMap[componentType.TypeId] = new Component[entityCount];
         }
         
      }
   }
}