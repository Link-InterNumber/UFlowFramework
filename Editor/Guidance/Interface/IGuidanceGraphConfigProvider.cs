namespace PowerCellStudio
{
   public interface IGuidanceGraphConfigProvider
   {
      public void Load();

      public IGuidanceConfig Get(int id);
   }
}