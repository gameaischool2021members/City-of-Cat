using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GenerateCityButton : MonoBehaviour
{
    public CityManager cityManager;

    public Button genereateCityButton;

    private AudioSource audioSource;

    void Start(){
        genereateCityButton.onClick.AddListener(GenerateCity);
        audioSource = GetComponent<AudioSource>();
    }

    void GenerateCity(){
        cityManager.GenerateCity();
        audioSource.Play();
    }
}
