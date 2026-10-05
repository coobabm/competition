using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
	public class InitDreamOS : MonoBehaviour
	{
		[InitializeOnLoad]
		public class InitOnLoad
		{
			static InitOnLoad()
			{
				if (!EditorPrefs.HasKey("DreamOSv3.Installed"))
				{
					EditorPrefs.SetInt("DreamOSv3.Installed", 1);
					EditorUtility.DisplayDialog("Hello there!", "Thank you for purchasing DreamOS." +
						"\n\nIf you need help, feel free to contact us through our support channels.", "Got it");
				}
			}
		}
	}
}