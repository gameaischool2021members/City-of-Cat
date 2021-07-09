using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GenerateCityButton : MonoBehaviour
{
    public CityManager cityManager;

    public Button genereateCityButton;

    void Start(){
        genereateCityButton.onClick.AddListener(GenerateCity);
    }

    void GenerateCity(){
        cityManager.GenerateCity();
    }
}
