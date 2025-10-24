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
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Graphics.Effects;

namespace TargetPaint.NPCs {
	public class TargetScreen : ModNPC {
		public override string Texture => "TargetPaint/NPCs/Pixel";
		public override void SetStaticDefaults() {
			NPCID.Sets.NPCBestiaryDrawOffset[Type] = new() { Hide = true };
		}
		const int precision = 4;
		internal static bool[,] data;
		public override void SetDefaults() {
			NPC.life = 99;
			NPC.lifeMax = 99;
			NPC.defense = 99;
			NPC.width = 96;
			NPC.height = 96;
			NPC.chaseable = false;
			data = new bool[Main.screenWidth / precision, Main.screenHeight / precision];
		}
		public override bool? CanBeHitByProjectile(Projectile projectile) {
			return false;
		}
		public override bool CanChat() => true;
		public override string GetChat() {
			NPC.life = 0;
			NPC.checkDead();
			return "";
		}
		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D tex = TargetPaint.pixel;
			spriteBatch.Draw(tex, NPC.position - Main.screenPosition, null, Color.White, 0, Vector2.Zero, 96, 0, 0);
			if (screenPos != Main.screenPosition) return false;
			const int fluff = 32;
			const int fluffRatio = fluff / precision;
			Projectile proj;
			Point screenPoint = Main.screenPosition.ToPoint();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				proj = Main.projectile[i];
				if (proj.active && proj.friendly) {
					int num = ProjectileID.Sets.DrawScreenCheckFluff[Main.projectile[i].type];
					if (new Rectangle((int)Main.Camera.ScaledPosition.X - num, (int)Main.Camera.ScaledPosition.Y - num, (int)Main.Camera.ScaledSize.X + num * 2, (int)Main.Camera.ScaledSize.Y + num * 2).Intersects(proj.Hitbox)) {
						for (int baseY = 0; baseY < Main.screenHeight / fluff; baseY++) {
							for (int baseX = 0; baseX < Main.screenWidth / fluff; baseX++) {
								if (proj.Colliding(
									proj.Hitbox,
									new Rectangle(
										screenPoint.X + baseX * fluff,
										screenPoint.Y + baseY * fluff, fluff, fluff)
									)) {
									for (int y = 0; y < fluffRatio; y++) {
										if (baseY * fluffRatio + y >= data.GetLength(1)) break;
										for (int x = 0; x < fluffRatio; x++) {
											if (baseX * fluffRatio + x >= data.GetLength(0)) break;
											if (!data[baseX * fluffRatio + x, baseY * fluffRatio + y]) {
												if (proj.Colliding(
													proj.Hitbox,
													new Rectangle(
														screenPoint.X + baseX * fluff + x * precision,
														screenPoint.Y + baseY * fluff + y * precision, precision, precision)
													)) {
													data[baseX * fluffRatio + x, baseY * fluffRatio + y] = true;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			Color color = Color.Red * 0.6f;
			for (int y = 0; y < data.GetLength(1); y++) {
				for (int x = 0; x < data.GetLength(0); x++) {
					if (data[x, y]) {
						spriteBatch.Draw(tex,
							new Rectangle(x * precision, y * precision, precision, precision),
							null,
							color,
							0,
							Vector2.Zero,
							SpriteEffects.None,
						0);
						data[x, y] = false;
					}
				}
			}
			return false;
		}
	}
}
