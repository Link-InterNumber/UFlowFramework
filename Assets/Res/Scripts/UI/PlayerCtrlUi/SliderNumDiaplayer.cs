using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderNumDiaplayer : MonoBehaviour
{
    public Slider sliderNode;

    private Text displayer;
    // Start is called before the first frame update
    void Awake()
    {
        displayer = GetComponent<Text>();
        if(displayer)
            sliderNode.onValueChanged.AddListener(OnValueChange);
    }

    private void OnValueChange(float arg0)
    {
        displayer.text = arg0.ToString();
    }
}
