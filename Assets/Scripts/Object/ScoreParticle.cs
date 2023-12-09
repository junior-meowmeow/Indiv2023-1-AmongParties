using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreParticle : SyncObject
{
    void Start()
    {
        var main = GetComponent<ParticleSystem>().main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    void OnParticleSystemStopped()
    {
        gameObject.SetActive(false);
    }
}
