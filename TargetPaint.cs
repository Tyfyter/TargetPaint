using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace TargetPaint {
	public class TargetPaint : Mod {
		public static Texture2D pixel;
		public override void Load() {
			if (Main.dedServ) return;
			pixel = ModContent.Request<Texture2D>("TargetPaint/NPCs/Pixel", AssetRequestMode.ImmediateLoad).Value;
		}
		public override void Unload() {
			pixel = null;
		}
	}
}