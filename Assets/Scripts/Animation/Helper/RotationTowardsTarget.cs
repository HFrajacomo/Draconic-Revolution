using UnityEngine;

public class RotationTowardsTarget : MonoBehaviour
{
    [SerializeField] private Transform main;
    [SerializeField] private Transform cam;

    private float angle;
    private float lastAngle = 0;

    void Start(){
        this.main = this.gameObject.transform;
    }

    void Update(){
        if(this.main == null || this.cam == null)
            return;

        this.angle = this.cam.localEulerAngles.x - this.lastAngle;

        this.main.RotateAround(this.cam.position, this.cam.right, this.angle);

        this.lastAngle = this.cam.localEulerAngles.x;
    }

    public void Setup(Transform cam){
        this.cam = cam;
    }
}
