using UnityEngine;

public class WaveLengthCtrl : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float length = 10f;

    private Vector3[] _posCache;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = false;
        _posCache = new Vector3[2]
        {
            Vector3.left * Mathf.Max(0, length),
            Vector3.zero
        };
        lineRenderer.SetPositions(_posCache);
    }

    // Update is called once per frame
    void Update()
    {
        _posCache[0] = Vector3.left * Mathf.Max(0, length);
        lineRenderer.SetPositions(_posCache);
    }
}
