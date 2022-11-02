using Res.Scripts.Hero;
using UnityEngine;

public class RoleMoveCtlrBase : MonoBehaviour
{

    protected virtual void Start()
    {
        RegisterEvent();
    }

    protected virtual void RegisterEvent()
    {
        
    }
    
    protected virtual void UnRegisterEvent()
    {
        
    }
    

    protected virtual void OnDestroy()
    {
        UnRegisterEvent();
    }
}
