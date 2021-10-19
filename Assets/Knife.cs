using UnityEngine;
using UnityEngine.Events;
using Lean.Touch;

public class Knife : MonoBehaviour
{
    [SerializeField] private Transform knifeModel;
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Honey honey;
    [SerializeField] private LeanDragCamera leanDragCamera;
    [Space]
    [SerializeField] private Vector3 cutTranslation = new Vector3(0.025f, -0.01f, 0f);
    [SerializeField] private float cutWidth = 0.5f;
    [SerializeField] private float cutSpeed = 0.5f;
    [Space]
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float slowSensitivity;

    public UnityEvent win;

    private float seedSin = 0;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            knifeModel.Translate(cutTranslation);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            var managedToSlice = honey.SliceUp(transform.position);
            if (managedToSlice)
            {
                topPoint.position = transform.position;
            }

            knifeModel.Translate(-cutTranslation);
        }

        if (honey.IsRolling)
        {
            leanDragCamera.Sensitivity = slowSensitivity;
            var knifeModelPosition = knifeModel.transform.position;

            seedSin += Time.deltaTime;
            var frictionMove = Mathf.Sin(seedSin * cutSpeed) * cutWidth + 0.1f;

            knifeModel.transform.position = new Vector3(knifeModelPosition.x, knifeModelPosition.y, frictionMove);
        }
        else
        {
            leanDragCamera.Sensitivity = normalSensitivity;
        }

        if (transform.position.y <= bottomPoint.position.y)
        {
            honey.SliceUp(transform.position);
            win.Invoke();
            Destroy(this);
        }
    }
}