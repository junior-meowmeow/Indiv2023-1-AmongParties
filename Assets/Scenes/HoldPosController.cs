using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldPosController : MonoBehaviour
{
    [SerializeField] private Animation anim;

    void Start()
    {
        anim = GetComponent<Animation>();
    }

    public void PlayAnimation(AnimationClip clip)
    {
        anim.Play(clip.name);
    }
}
