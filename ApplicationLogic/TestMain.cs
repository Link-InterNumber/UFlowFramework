namespace PowerCellStudio
{
    public class TestMain: SceneMainBase
    {
        protected override void ReadyForStart()
        {
            
        }

        protected override ILocalizationProvider GetLocalizationProvider()
        {
            return new UnityLocalizationProvider();
        }
    }
}