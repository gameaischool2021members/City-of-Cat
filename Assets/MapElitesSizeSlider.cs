using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapElitesSizeSlider : MonoBehaviour
{
    public Slider meSizeSlider;

    public CityManager cityManager;

    public void Start()
	{
		//Adds a listener to the main slider and invokes a method when the value changes.
		meSizeSlider.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
	}
	
	// Invoked when the value of the slider changes.
	public void ValueChangeCheck()
	{
		cityManager.setMapElitesGridSize((int)meSizeSlider.value);
        
	}
}
