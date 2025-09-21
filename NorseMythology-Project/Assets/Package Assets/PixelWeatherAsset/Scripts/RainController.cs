using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RainController : MonoBehaviour
{
    [Range(0, 1f)]
    public float masterIntensity = 1f;
    [Range(0, 1f)]
    public float rainIntensity = 1f;
    [Range(0, 1f)]
    public float windIntensity = 1f;
    [Range(0, 1f)]
    public bool autoUpdate;

    public ParticleSystem rainPart;
    private ParticleSystem.EmissionModule rainEmission;
    private ParticleSystem.ForceOverLifetimeModule rainForce;

    [SerializeField] private Player player;

    void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();

        rainEmission = rainPart.emission;
        rainForce = rainPart.forceOverLifetime;
        UpdateAll();
    }

    void Update()
    {
        transform.position = player.transform.position;
        if (autoUpdate)
            UpdateAll();

        transform.position = player.transform.position;
    }

    void UpdateAll(){
        rainEmission.rateOverTime = 200f * masterIntensity * rainIntensity;
        rainForce.x = new ParticleSystem.MinMaxCurve(-25f * windIntensity * masterIntensity, (-3-30f * windIntensity) * masterIntensity);
    }

    public void OnMasterChanged(float value)
    {
        masterIntensity = value;
        UpdateAll();
    }
    public void OnRainChanged(float value)
    {
        rainIntensity = value;
        UpdateAll();
    }
    public void OnWindChanged(float value)
    {
        windIntensity = value;
        UpdateAll();
    }
}
