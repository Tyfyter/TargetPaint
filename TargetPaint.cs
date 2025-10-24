using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Threading;
using System;
using System.ComponentModel;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

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
	public class Hitbox_Viewer_Toggle : BuilderToggle {
		public override string HoverTexture => Texture;
		public override bool Active() => !PaintConfig.Instance.HideToggle;
		public override int NumberOfStates => 3;
		public override string DisplayValue() => Language.GetTextValue("Mods.TargetPaint.BuilderToggle",
			Language.GetTextValue("Mods.TargetPaint.BuilderToggle" + CurrentState)
		);
		public override bool Draw(SpriteBatch spriteBatch, ref BuilderToggleDrawParams drawParams) {
			drawParams.Frame.X = (CurrentState - 1) * 20;
			drawParams.Frame.Y = 0;
			drawParams.Frame.Height = 18;
			drawParams.Frame.Width = 18;
			switch (CurrentState) {
				case 0:
				drawParams.Frame.X = 0;
				drawParams.Frame.Y = 20;
				break;
			}
			return true;
		}
		public override bool DrawHover(SpriteBatch spriteBatch, ref BuilderToggleDrawParams drawParams) {
			drawParams.Frame.Y = 20;
			drawParams.Frame.Height = 18;
			drawParams.Frame.Width = 18;
			return true;
		}
	}
	public class PaintSystem : ModSystem {
		public override void Load() {
			On_Projectile.Damage += (orig, self) => {
				orig(self);
				if (!PaintConfig.Instance.PerfectTemporalPrecision) return;
				if (Main.LocalPlayer.builderAccStatus[ModContent.GetInstance<Hitbox_Viewer_Toggle>().Type] == 0) return;
				if (self.friendly || self.hostile) ProcessProjectile(self);
			};
		}

		Color[] data;
		Texture2D texture;
		int lastWidth;
		int lastHeight;
		public override void PostDrawTiles() {
			int state = Main.LocalPlayer.builderAccStatus[ModContent.GetInstance<Hitbox_Viewer_Toggle>().Type];
			if (state == 0) {
				return;
			}
			if (lastWidth != Main.screenWidth || lastHeight != Main.screenHeight) {
				lastWidth = Main.screenWidth;
				lastHeight = Main.screenHeight;
				texture?.Dispose();
				texture = null;
				data = new Color[Main.screenWidth * Main.screenHeight];
			}
			texture ??= new(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
			if (!PaintConfig.Instance.PerfectTemporalPrecision) {
				foreach (Projectile proj in Main.ActiveProjectiles) {
					if (proj.friendly || proj.hostile) ProcessProjectile(proj);
				}
			}
			texture.SetData(data);
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			Main.spriteBatch.Draw(texture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
			Main.spriteBatch.End();
			switch (state) {
				default:
				case 1:
				Array.Clear(data);
				break;

				case 2:
				byte fadeSpeed = PaintConfig.Instance.FadeSpeed;
				for (int i = 0; i < data.Length; i++) {
					if (data[i].R > 0) data[i].R -= Math.Min(data[i].R, fadeSpeed);
					if (data[i].G > 0) data[i].G -= Math.Min(data[i].G, fadeSpeed);
					if (data[i].B > 0) data[i].B -= Math.Min(data[i].B, fadeSpeed);
					if (data[i].A > 0) data[i].A -= Math.Min(data[i].A, fadeSpeed);
				}
				break;
			}
		}
		public void ProcessProjectile(Projectile proj) {
			Point screenPoint = Main.screenPosition.ToPoint();
			int precision = PaintConfig.Instance.Precision;
			int fluff = 32;
			int fluffRatio = fluff / precision;
			int num = ProjectileID.Sets.DrawScreenCheckFluff[proj.type];
			if (new Rectangle((int)Main.Camera.ScaledPosition.X - num, (int)Main.Camera.ScaledPosition.Y - num, (int)Main.Camera.ScaledSize.X + num * 2, (int)Main.Camera.ScaledSize.Y + num * 2).Intersects(proj.Hitbox)) {
				Rectangle hitbox = proj.Hitbox;
				ProjectileLoader.ModifyDamageHitbox(proj, ref hitbox);
				if (ProjectileID.Sets.IsAWhip[proj.type]) {
					proj.WhipPointsForCollision.Clear();
					Projectile.FillWhipControlPoints(proj, proj.WhipPointsForCollision);
				}
				FastParallel.For(0, Main.screenWidth / fluff, (minX, maxX, _) => {
					try {
						for (int baseY = 0; baseY < Main.screenHeight / fluff; baseY++) {
							for (int baseX = minX; baseX < maxX; baseX++) {
								if (IsProjColliding(
									proj,
									hitbox,
									new Rectangle(
										screenPoint.X + baseX * fluff,
										screenPoint.Y + baseY * fluff, fluff, fluff)
									)) {
									for (int y = 0; y < fluffRatio; y++) {
										if (baseY * fluffRatio + y >= Main.screenHeight) break;
										for (int x = 0; x < fluffRatio; x++) {
											if (baseX * fluffRatio + x >= Main.screenWidth) break;
											int index = baseX * fluff + x * precision + (baseY * fluff + y * precision) * Main.screenWidth;
											if (data[index].A <= 255) {
												if (IsProjColliding(
													proj,
													hitbox,
													new Rectangle(
														screenPoint.X + baseX * fluff + x * precision,
														screenPoint.Y + baseY * fluff + y * precision, precision, precision)
													)) {
													SetData(index, proj.hostile ? (proj.friendly ? 0 : 2) : 1);
												}
											}
										}
									}
								}
							}
						}
					} catch { }
				});
			}
			void SetData(int index, int colorIndex = 0) {
				uint value = 0xff000000u | (0xffu << (8 << (colorIndex - 1)));
				for (int i = 0; i < precision; i++) {
					for (int j = 0; j < precision; j++) {
						data[index + i + j * Main.screenWidth].PackedValue |= value;
					}
				}
			}
		}
		public static bool IsProjColliding(Projectile proj, Rectangle projHitbox, Rectangle hitbox) {
			if (ProjectileID.Sets.IsAWhip[proj.type]) {
				for (int m = 0; m < proj.WhipPointsForCollision.Count; m++) {
					Point point = proj.WhipPointsForCollision[m].ToPoint();
					projHitbox.Location = new Point(point.X - projHitbox.Width / 2, point.Y - projHitbox.Height / 2);
					if (projHitbox.Intersects(hitbox)) {
						return true;
					}
				}
				return false;
			}
			return proj.Colliding(projHitbox, hitbox);
		}
	}
	public class PaintConfig : ModConfig {
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public static PaintConfig Instance;
		[Range(1, 32), DefaultValue(2)]
		public int Precision = 2;
		[DefaultValue(true)]
		public bool PerfectTemporalPrecision = true;
		[Range((byte)0, (byte)255), DefaultValue(16)]
		public byte FadeSpeed = 16;
		[DefaultValue(false)]
		public bool HideToggle = false;
	}
}