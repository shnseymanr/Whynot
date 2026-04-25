using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PourDetector : MonoBehaviour
{
public int pourThreshold = 45;
public Transform origin = null;
public GameObject streamPrefab = null;

private bool isPouring = false;
private Stream currentStream = null;

private void Update()
    {
        float currentAngle = CalculatePourAngle();
       bool pourCheck = CalculatePourAngle() > pourThreshold; 
       if(isPouring != pourCheck)
        {
            isPouring = pourCheck;

            if(isPouring)
            {
                StartPour();
            }
            else
            {
                EndPour();
            }
        }
    }

private void StartPour()
    {
        print("Start");
        currentStream = CreateStream();
        currentStream.Begin();
    }

private void EndPour()
    {
        print("End");
    }

private float CalculatePourAngle()
{
    // Kapsülün yukarı bakan ucu (up) ile dünyanın yukarısı (Vector3.up) arasındaki açıyı ölçer.
    // Kapsül dik dururken bu değer 0'dır. Eğildikçe 180'e doğru gider.
    return Vector3.Angle(transform.up, Vector3.up);
}

private Stream CreateStream()
    {
        GameObject streamObject = Instantiate(streamPrefab, origin.position, Quaternion.identity, transform);
        return streamObject.GetComponent<Stream>();
    }

}
