using System;
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class ObjectInteractor : MonoBehaviour
    {
        protected abstract void OnDestroy();
    }
}