using UnityEngine;

namespace SDG.Unturned
{
	public class LookAtLocalPlayer : MonoBehaviour
	{
#if GAME && !DEDICATED_SERVER
		private void LateUpdate()
		{
			if (Dedicator.IsDedicatedServer)
			{
				return;
			}

			if (Player.LocalPlayer != null)
			{
				transform.LookAt(Player.LocalPlayer.look.aim);
			}
		}
#endif // GAME && !DEDICATED_SERVER
	}
}
