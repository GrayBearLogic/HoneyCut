using Obi;
using UnityEngine;

public class ObiFluidController : MonoBehaviour
{
    [SerializeField] private ObiEmitter obiEmitter;
    [SerializeField] private Honey honey;
    [SerializeField] private float honeyAmount = 70;

    private void Update()
    {
        obiEmitter.speed = honey.IsRolling ? honey.wholeDistanceDiff* honeyAmount : 0f;
    }
}
