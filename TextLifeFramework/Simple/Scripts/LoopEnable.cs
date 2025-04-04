using EnjoyGameClub.TextLifeFramework.Core;
using UnityEngine;

namespace EnjoyGameClub.TextLifeFramework
{
    [ExecuteAlways]
    public class LoopEnable : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Next();
            }
        }

        private void Next()
        {
            int openIndex = -1;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.activeSelf && openIndex == -1)
                {
                    openIndex = i + 1 == transform.childCount ? 0 : i + 1;
                }
                child.SetActive(false);
            }
        
            if(openIndex==-1)
                return;
            var obj =transform.GetChild(openIndex).gameObject;
            obj.transform.GetComponentInChildren<TextLife>().ResetAnimation();
            obj.SetActive(true);
        }
    }
}