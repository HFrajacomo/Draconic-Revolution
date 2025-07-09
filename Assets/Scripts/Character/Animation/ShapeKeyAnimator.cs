using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = System.Random;

public class ShapeKeyAnimator : MonoBehaviour {
    private SkinnedMeshRenderer animationTarget;
    private Dictionary<string, Coroutine> runningAnimations = new Dictionary<string, Coroutine>();
    private Random rng = new Random();


    void Start(){
        this.animationTarget = GetComponent<SkinnedMeshRenderer>();
        Play("Blink", new ShapeKeyAnimationSettings(ShapeKeyAnimationType.PULSE_RETRIGGER_RANDOM, 3f, 0.15f, 3f));
    }

    public void OnDestroy(){
        foreach(string name in this.runningAnimations.Keys){
            StopCoroutine(this.runningAnimations[name]);
        }
    }

    public void Play(string name, ShapeKeyAnimationSettings settings){
        // If animation is running already
        if(this.runningAnimations.ContainsKey(name)){
            StopCoroutine(this.runningAnimations[name]);
            this.runningAnimations.Remove(name);
        }

        int index = this.animationTarget.sharedMesh.GetBlendShapeIndex(name);

        // Invalid ShapeKeys name
        if(index < 0)
            return;

        Coroutine animation;

        switch(settings.type){
            case ShapeKeyAnimationType.START:
                animation = StartCoroutine(AnimateStartShapeKey(index, name, settings.duration));
                break;
            case ShapeKeyAnimationType.STOP:
                animation = StartCoroutine(AnimateStopShapeKey(index, name, settings.duration));
                break;
            case ShapeKeyAnimationType.PULSE_RETRIGGER:
                animation = StartCoroutine(AnimatePulseRetriggerShapeKey(index, name, settings.duration, settings.pulseDuration));
                break;
            case ShapeKeyAnimationType.PULSE_RETRIGGER_RANDOM:
                animation = StartCoroutine(AnimatePulseRetriggerRandomShapeKey(index, name, settings.duration, settings.pulseDuration, settings.extraArg));
                break;
            default:
                return;
        }

        this.runningAnimations.Add(name, animation);
    }

    private IEnumerator AnimateStartShapeKey(int index, string name, float duration){
        float elapsedTime = 0f;
        float skWeight = 0f;

        while(elapsedTime < duration){
            skWeight = Mathf.Lerp(0f, 100f, elapsedTime / duration);
            this.animationTarget.SetBlendShapeWeight(index, skWeight);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.animationTarget.SetBlendShapeWeight(index, 100f);
        this.runningAnimations.Remove(name);
    }

    private IEnumerator AnimateStopShapeKey(int index, string name, float duration){
        float elapsedTime = 0f;
        float skWeight = 0f;

        while(elapsedTime < duration){
            skWeight = Mathf.Lerp(100f, 0f, elapsedTime / duration);
            this.animationTarget.SetBlendShapeWeight(index, skWeight);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.animationTarget.SetBlendShapeWeight(index, 0f);
        this.runningAnimations.Remove(name);
    }

    private IEnumerator AnimatePulseRetriggerShapeKey(int index, string name, float duration, float pulseDuration){
        float elapsedTime = 0f;

        while(true){
            while(elapsedTime < duration){
                this.animationTarget.SetBlendShapeWeight(index, 0f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            while(elapsedTime < duration + pulseDuration){
                this.animationTarget.SetBlendShapeWeight(index, 100f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            this.animationTarget.SetBlendShapeWeight(index, 0f);
            elapsedTime = 0f;
            yield return null;
        }
    }

    private IEnumerator AnimatePulseRetriggerRandomShapeKey(int index, string name, float duration, float pulseDuration, float durationRandomOffset){
        float elapsedTime = 0f;
        float randomOffset;

        while(true){
            randomOffset = (float)(this.rng.NextDouble() * durationRandomOffset);

            while(elapsedTime < duration + randomOffset){
                this.animationTarget.SetBlendShapeWeight(index, 0f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            while(elapsedTime < duration + randomOffset + pulseDuration){
                this.animationTarget.SetBlendShapeWeight(index, 100f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            this.animationTarget.SetBlendShapeWeight(index, 0f);
            elapsedTime = 0f;
            yield return null;
        }
    }
}