using UnityEngine;
using Lean.Common;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace Lean.Touch
{
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanDragCamera")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Drag Camera")]
	public class LeanDragCamera : MonoBehaviour
	{
		public LeanFingerFilter Use = new LeanFingerFilter(true);

		public LeanScreenDepth ScreenDepth = new LeanScreenDepth(LeanScreenDepth.ConversionType.DepthIntercept);
		public float Sensitivity { set { sensitivity = value; } get { return sensitivity; } } [FSA("Sensitivity")] [SerializeField] private float sensitivity = 1.0f;

		public float Damping { set { damping = value; } get { return damping; } } [FSA("Damping")] [FSA("Dampening")] [SerializeField] private float damping = -1.0f;
		public float Inertia { set { inertia = value; } get { return inertia; } } [FSA("Inertia")] [SerializeField] [Range(0.0f, 1.0f)] private float inertia;

		[SerializeField]
		private Vector3 remainingDelta;


		protected virtual void Awake()
		{
			Use.UpdateRequiredSelectable(gameObject);
		}

		protected virtual void Update()
		{
			var fingers = Use.UpdateAndGetFingers();


			// Get the last and current screen point of all fingers
			var lastScreenPoint = LeanGesture.GetLastScreenCenter(fingers);
			var screenPoint     = LeanGesture.GetScreenCenter(fingers);

			

			// Get the world delta of them after conversion
			var worldDelta = ScreenDepth.ConvertDelta(lastScreenPoint, screenPoint);
			if (worldDelta.y > 0)
				return;

			if(worldDelta.magnitude > 1f)
				transform.Translate(worldDelta);

            // Store the current position
            var oldPosition = transform.localPosition;

            // Pan the camera based on the world delta
            transform.position -= worldDelta * sensitivity;

            // Add to remainingDelta
            remainingDelta += transform.localPosition - oldPosition;

            // Get t value
            var factor = LeanHelper.GetDampenFactor(damping, Time.deltaTime);

            // Dampen remainingDelta
            var newRemainingDelta = Vector3.Lerp(remainingDelta, Vector3.zero, factor);

            // Shift this position by the change in delta
            transform.localPosition = oldPosition + remainingDelta - newRemainingDelta;

            if (fingers.Count == 0 && inertia > 0.0f && damping > 0.0f)
            {
                newRemainingDelta = Vector3.Lerp(newRemainingDelta, remainingDelta, inertia);
            }

            // Update remainingDelta with the dampened value
            remainingDelta = newRemainingDelta;
        }
	}
}