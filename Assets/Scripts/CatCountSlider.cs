using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatCountSlider : MonoBehaviour
{
    public Slider catCountSlider;

    public CityManager cityManager;

    public void Start()
	{
		//Adds a listener to the main slider and invokes a method when the value changes.
		catCountSlider.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
	}
	
	// Invoked when the value of the slider changes.
	public void ValueChangeCheck()
	{
		cityManager.setCatCount((int)catCountSlider.value);
        
	}
}
