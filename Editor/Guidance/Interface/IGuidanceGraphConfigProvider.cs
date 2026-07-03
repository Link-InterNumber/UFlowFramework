namespace PowerCellStudio.Editor
{
   public interface IGuidanceGraphConfigProvider
   {
      public void Load();

      public IGuidanceConfig Get(int id);
   }
}