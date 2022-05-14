using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmFollow : MonoBehaviour {
    public Transform target;
    public float smoothTime = 0.3f;
    float velocity;

    void LateUpdate() {
        transform.position = target.position;
        Quaternion currentRotation = transform.rotation;
        Quaternion goalRotation = target.rotation;
        float smoothX = Mathf.SmoothDamp(currentRotation.x, goalRotation.x, ref velocity, smoothTime);
        float smoothY = Mathf.SmoothDamp(currentRotation.y, goalRotation.y, ref velocity, smoothTime);
        float smoothZ = Mathf.SmoothDamp(currentRotation.z, goalRotation.z, ref velocity, smoothTime);
        float smoothW = Mathf.SmoothDamp(currentRotation.w, goalRotation.w, ref velocity, smoothTime);
        Quaternion smoothQuaternion = new Quaternion(smoothX, smoothY, smoothZ, smoothW);
        transform.rotation = smoothQuaternion;
    }

}
