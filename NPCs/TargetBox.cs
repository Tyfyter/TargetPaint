using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace TargetPaint.NPCs {
	public class TargetBox : ModNPC {
		public override string Texture => "TargetPaint/NPCs/Pixel";
		internal static two_eighty_eight_bytes data = new two_eighty_eight_bytes();
		public override void SetDefaults() {
			NPC.life = 9999;
			NPC.lifeMax = 9999;
			NPC.defense = 9999;
			NPC.width = 96;
			NPC.height = 96;
		}
		/*public override bool? CanBeHitByProjectile(Projectile projectile) {
            if(/*!projectile.Hitbox.Intersects(npc.Hitbox) || * /projectile.modProjectile == null) return false;
            ModProjectile modProjectile = projectile.modProjectile;
            Rectangle projHitbox = projectile.Hitbox;
            int x = (int)npc.position.X;
            int y = (int)npc.position.Y;
            for(int i = 0; ++i<576;) {
                //data[i] = modProjectile.Colliding(projHitbox, new Rectangle(x+(i%24)*4,y+(i/24)*4,4,4))??false;
                if(modProjectile.Colliding(projHitbox, new Rectangle(x+(i%24)*4, y+(i/24)*4, 4, 4))==true)data[i] = true;
            }
            return true;
        }*/
		public override bool CanChat() {
			return true;
		}
		public override string GetChat() {
			data = new two_eighty_eight_bytes();
			Main.LocalPlayer.SetTalkNPC(-1);
			return "reset target";
		}
		public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) {
			modifiers.Knockback *= 0;
			modifiers.FinalDamage *= 0;
			Rectangle projHitbox = projectile.Hitbox;
			int x = (int)NPC.position.X;
			int y = (int)NPC.position.Y;
			for (int i = 0; ++i < 2304;) {
				//data[i] = modProjectile.Colliding(projHitbox, new Rectangle(x+(i%24)*4,y+(i/24)*4,4,4))??false;
				if (projectile.Colliding(projHitbox, new Rectangle(x + (i % 48) * 2, y + (i / 48) * 2, 2, 2))) data[i] = true;
			}
			if (projectile.penetrate > 1) {
				projectile.penetrate++;
			}
		}
		public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone) {
			projectile.localNPCImmunity[NPC.whoAmI] = 0;
			for (int i = 0; ++i < NPC.immune.Length;) {
				NPC.immune[i] = 0;
			}
		}
		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			NPC.life = 9999;
			int x = (int)(NPC.position.X - Main.screenPosition.X);
			int y = (int)(NPC.position.Y - Main.screenPosition.Y);
			Mod.RequestAssetIfExists("NPCs/Pixel", out Asset<Texture2D> texture);
			for (int i = 0; ++i < 2304;) {
				spriteBatch.Draw(texture.Value, new Rectangle(x + (i % 48) * 2, y + (i / 48) * 2, 2, 2), null, data[i] ? Color.Red : Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
			}
			return false;
		}
	}
	public class ClearCommand : ModCommand {
		public override CommandType Type => CommandType.Chat;

		public override string Command => "cleartarget";

		public override string Usage => "/cleartarget";

		public override string Description => "clears the paintable target";

		public override void Action(CommandCaller player, string input, string[] args) {
			TargetBox.data = new two_eighty_eight_bytes();
		}
	}
	internal class two_eighty_eight_bytes {
		byte[] bytes = new byte[288];
		public bool this[int index] {
			get {
				return ((bytes[index / 8]) & (1 << index % 8)) != 0;
			}
			set {
				//if(value) {
				(bytes[index / 8]) |= (byte)((value ? 1 : 0) << index % 8);
				//(bytes[index/8])|=(byte)(Convert.ToByte(value)<<index%8);
				//} else {
				//(bytes[index/8])&=(byte)~(byte)(1<<index%8);
				//}
			}
		}
	}
}
