using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = System.Random;

public class FaceExpressionAnimator : MonoBehaviour {
    private Material material;
    private int currentIndex = 0;
    private Coroutine runningAnimation;
    private Random rng = new Random();

    private static readonly Dictionary<int, int> BLINKING_INDEXES = new Dictionary<int, int>(){
        {0, 1},
        {2, 3}
    };


    void Start(){
        Play(0);
    }

    public void OnDestroy(){
        if(this.runningAnimation != null)
            StopCoroutine(this.runningAnimation);
    }

    public void SetMaterial(Material mat){
        this.material = mat;
    }

    public void Play(int expressionIndex){
        if(expressionIndex < 0)
            return;

        // If animation is running already
        if(this.runningAnimation != null){
            StopCoroutine(this.runningAnimation);
            this.runningAnimation = null;
        }

        this.material.SetFloat("_ExpressionIndex", expressionIndex);
        this.currentIndex = expressionIndex;

        if(BLINKING_INDEXES.ContainsKey(expressionIndex))
            this.runningAnimation = StartCoroutine(AnimatePulseRetriggerRandomExpression(expressionIndex, BLINKING_INDEXES[expressionIndex], 3f, 0.15f, 3f));
    }

    private IEnumerator AnimatePulseRetriggerRandomExpression(int index, int pulseIndex, float duration, float pulseDuration, float durationRandomOffset){
        float elapsedTime = 0f;
        float randomOffset;

        while(true){
            if(this.material == null){
                yield return null;
            }

            randomOffset = (float)(this.rng.NextDouble() * durationRandomOffset);

            while(elapsedTime < duration + randomOffset){
                this.material.SetFloat("_ExpressionIndex", index);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            while(elapsedTime < duration + randomOffset + pulseDuration){
                this.material.SetFloat("_ExpressionIndex", pulseIndex);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            this.material.SetFloat("_ExpressionIndex", index);
            elapsedTime = 0f;
            yield return null;
        }
    }
}