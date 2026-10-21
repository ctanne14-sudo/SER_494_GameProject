using System.Collections;
using UnityEngine;

public class UIShake : MonoBehaviour
{
    int interpolationFramesCount = 8;
    int elapsedFrames = 0;

    int maxFrameReset = 24;
    bool direction;
    float duration = .2f;
    Coroutine running;
    Vector3 start;
    Vector3 end;
    Vector3 origin;


    void Awake()
    {
        start = transform.position - new Vector3(.05f, 0f ,0f);
        end = transform.position + new Vector3(.05f, 0f ,0f);
        origin = transform.position;
    }
    

    public void shake()
    {
        if (running != null) StopCoroutine(running);
        transform.position = origin;
        running = StartCoroutine(RunShake());
    }

    IEnumerator RunShake()
    {
        float t = 0f;
        while (t < duration)
        {
            if (direction) {
                float interpolationRatio = (float)elapsedFrames / interpolationFramesCount;
                transform.position = Vector3.Lerp(start, end, interpolationRatio);
                elapsedFrames = (elapsedFrames + 1) % (maxFrameReset); 
                direction = elapsedFrames == 0 ? !direction : direction;
            } else
            {
                float interpolationRatio = (float)elapsedFrames / interpolationFramesCount;
                transform.position = Vector3.Lerp(end, start, interpolationRatio);
                elapsedFrames = (elapsedFrames + 1) % (maxFrameReset);
                direction = elapsedFrames == 0 ? !direction : direction;
            }
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = origin;
        running = null;
    }


}