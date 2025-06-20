namespace PowerCellStudio
{
    public class ShakeUtils
    {
        [System.Flags]
        public enum ShakeType
        {
            None = 0,
            Position = 1 << 0, // 位移震动
            Rotation = 2 << 0,  // 旋转震动
        }

        public static ShakeHandle Shake(ShakeType shakeType, Transform target, float duration, float frequency, Vector3 magnitude,
             bool isUnscaleTime = true, AnimationCurve curve = null)
        {
            var request = new ShakeRequest(shakeType, target, duration, frequency, magnitude, curve, isUnscaleTime);
            var handle = new ShakeHandle(request);
            ApplicationManager.instance.StartCoroutine(ProcessedHandle(handle));
            return handle;
        }

        private static IEnumerator ProcessedHandle(ShakeHandle handle)
        {
            while (!handle.isDone)
            {
                handle.Process(handle.isUnscaleTime? Time.unscaledDeltaTime : Time.deltaTime);
                yield return null;
            }
            handle.Cancel();
        }
    }
}