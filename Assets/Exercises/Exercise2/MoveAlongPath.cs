using AfGD;
using UnityEngine;

public class MoveAlongPath : MonoBehaviour
{
    public bool rotate;
    public bool useArcLength;
    public bool logActualSpeed;
    
    public DebugCurve debugCurve;

    public float speed = 0.5f;

    private float _l;

    private Vector3 previousPosition;

    private void Start()
    {
        previousPosition = transform.position;
    }

    private void Update()
    {
        _l = Mathf.Repeat(_l + speed * Time.deltaTime, debugCurve.MaxLength);

        float u = useArcLength ? debugCurve.ArcLength(_l) : (_l * speed) / debugCurve.MaxLength;
        
        transform.position = debugCurve.Evaluate(u);
        if(rotate)
            transform.rotation = Quaternion.LookRotation(debugCurve.EvaluateDv(u));

        float actualSpeed = Vector3.Distance(previousPosition, transform.position) / Time.deltaTime;

        if(logActualSpeed)
            Debug.Log(actualSpeed);
        
        previousPosition = transform.position;
    }
}
