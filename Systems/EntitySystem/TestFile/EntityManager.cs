namespace TestEntity
{
   public partial class EntityManager
   {
      private SparseSet<IEntity> _entitys;

      private List<IEntitySystem> _systems;

      private List<ComponentChangeRequest> _componentAddBuffer;

      private List<ComponentChangeRequest> _componentRemoveBuffer;

      private List<EntityCreateRequest> _entityCreateBuffer;

      public EntityManager()
      {
         InitComponentMaskMap();
         _systems = new List<IEntitySystem>();
      }

      public void AddSystem(IEntitySystem system)
      {
         if (system == null) return;
         _systems.Add(system);
      }

      public void Update(float dt)
      {
         AddNewEntity();
         AddComponent();
         UpdateSystems(dt);
         RemoveComponent();
         RemoveEntity();
      }

      public void AddNewEntity()
      {

      }

      public void AddComponent()
      {

      }

      public void UpdateSystems(float dt)
      {
         foreach (var system in _systems)
         {
            system.Update(dt, this);
         }
      }

      public void RemoveComponent()
      {

      }

      public void RemoveEntity()
      {

      }

   }
}