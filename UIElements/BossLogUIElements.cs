﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System.Collections.Generic;
using System.Linq;
using ReLogic.Content;
using ReLogic.OS;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.UI;
using Terraria.UI.Chat;
using System;

namespace BossChecklist.UIElements
{
	internal static class BossLogUIElements
	{
		/// <summary>
		/// Hides certain mouse over interactions from appearing such as tile icons or NPC names.
		/// </summary>
		static void HideMouseOverInteractions() 
		{
			Main.mouseText = true;
			Main.LocalPlayer.cursorItemIconEnabled = false;
			Main.LocalPlayer.cursorItemIconID = -1;
			Main.ItemIconCacheUpdate(0);
		}

		internal class OpenLogButton : UIImageButton
		{
			internal Asset<Texture2D> texture;
			private Vector2 offset;
			internal bool dragging;

			public OpenLogButton(Asset<Texture2D> texture) : base(texture) {
				this.texture = texture;
			}

			private void DragStart(UIMouseEvent evt) {
				var dimensions = GetDimensions().ToRectangle();
				offset = new Vector2(evt.MousePosition.X - dimensions.Left, evt.MousePosition.Y - dimensions.Top); // aligns the element's center with the mouse position
				dragging = true;
			}

			private void DragEnd(UIMouseEvent evt) {
				// Set the new position
				Left.Set(evt.MousePosition.X - Main.screenWidth - offset.X, 1f);
				Top.Set(evt.MousePosition.Y - Main.screenHeight - offset.Y, 1f);
				Recalculate();

				// Update and save the new button position
				BossChecklist.BossLogConfig.BossLogPos = new Vector2(Left.Pixels, Top.Pixels);
				BossChecklist.SaveConfig(BossChecklist.BossLogConfig);

				dragging = false;
			}

			public override void RightMouseDown(UIMouseEvent evt) {
				base.RightMouseDown(evt);
				DragStart(evt);
			}

			public override void RightMouseUp(UIMouseEvent evt) {
				base.RightMouseUp(evt);
				DragEnd(evt);
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);

				if (dragging) {
					Left.Set(Main.mouseX - Main.screenWidth - offset.X, 1f);
					Top.Set(Main.mouseY - Main.screenHeight - offset.Y, 1f);
					//Recalculate();
				}
				else {
					Vector2 configVec = BossChecklist.BossLogConfig.BossLogPos;
					Left.Set(configVec.X, 1f);
					Top.Set(configVec.Y, 1f);
				}

				var parentSpace = Parent.GetDimensions().ToRectangle();
				if (!GetDimensions().ToRectangle().Contains(parentSpace)) {
					Left.Pixels = Utils.Clamp(Left.Pixels, -parentSpace.Right, -Width.Pixels);
					Top.Pixels = Utils.Clamp(Top.Pixels, -parentSpace.Bottom, -Height.Pixels);
					Recalculate();
					BossChecklist.BossLogConfig.BossLogPos = new Vector2(Left.Pixels, Top.Pixels);
				}
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				base.DrawSelf(spriteBatch);

				if ((ContainsPoint(Main.MouseScreen) || dragging) && !PlayerInput.IgnoreMouseInterface) {
					Main.LocalPlayer.mouseInterface = true;
					HideMouseOverInteractions();
				}

				// When hovering over the button, draw a 'Boss Log' text over the button
				CalculatedStyle bookArea = GetInnerDimensions();
				string hoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.BossLog");
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(hoverText);
				Vector2 pos = new Vector2(bookArea.X - (stringAdjust.X / 3), bookArea.Y - 24);
				if (IsMouseHovering && !dragging) {
					spriteBatch.DrawString(FontAssets.MouseText.Value, hoverText, pos, Color.White); // text shouldn't appear if dragging the element
				}

				Asset<Texture2D> cover = BossLogUI.colorTexture;
				Color coverColor = BossChecklist.BossLogConfig.BossLogColor;
				if (!IsMouseHovering && !dragging) {
					cover = BossLogUI.fadedTexture; // make the color match the faded UIImageButton
					coverColor = new Color(coverColor.R, coverColor.G, coverColor.B, 128);
				}
				spriteBatch.Draw(cover.Value, bookArea.ToRectangle(), coverColor);

				// UIImageButtons are normally faded, so if dragging and not draw the button fully opaque
				// This is most likely to occur when the mouse travels off screen while dragging
				if (dragging) {
					spriteBatch.Draw(texture.Value, bookArea.ToRectangle(), Color.White);
				}

				// Determine a border color for the button
				PlayerAssist player = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				Color? borderColor = null;

				if (IsMouseHovering || dragging) {
					borderColor = Color.Goldenrod; // If hovering over or dragging the button, the book will be highlighted in a gold border
				}
				else if (BossChecklist.DebugConfig.NewRecordsDisabled || BossChecklist.DebugConfig.RecordTrackingDisabled || BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
					borderColor = Color.Firebrick; // If Records are disabled in any way, the book will be highlighted with a red border
				}
				else if (!player.hasOpenedTheBossLog || player.hasNewRecord.Any(x => x == true)) {
					float modifier = Main.masterColor / 200f; // If the player has not opened the log or has not viewed a new record page, the book will be hightlighted with a flashing log-colored border
					borderColor = new Color(coverColor.R * modifier, coverColor.G * modifier, coverColor.B * modifier);
				}

				if (borderColor.HasValue) {
					spriteBatch.Draw(BossLogUI.borderTexture.Value, bookArea.ToRectangle(), borderColor.Value); // Draw a colored border if one was set
				}
			}
		}

		internal class NavigationalButton : UIImageButton
		{
			public string Id { get; init; } = "";
			internal Asset<Texture2D> texture;
			internal string hoverText;

			public NavigationalButton(Asset<Texture2D> texture, string hoverText = null) : base(texture) {
				this.texture = texture;
				this.hoverText = hoverText;
			}			

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)
					Main.LocalPlayer.mouseInterface = true;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				base.DrawSelf(spriteBatch);
				if (IsMouseHovering && !string.IsNullOrEmpty(hoverText)) {
					BossUISystem.Instance.UIHoverText = Language.GetTextValue(hoverText); // Display the hover text in a tooltip-like box
				}
			}
		}

		internal class LogItemSlot : UIElement
		{
			public string Id { get; init; } = "";
			internal string hoverText;
			internal Item item;
			private readonly int context;
			private readonly float scale;
			internal bool hasItem;

			public LogItemSlot(Item item, bool hasItem, string hoverText = "", int context = ItemSlot.Context.TrashItem, float scale = 1f) {
				this.context = context;
				this.scale = scale;
				this.item = item;
				this.hoverText = hoverText;
				this.hasItem = hasItem;

				Width.Set(TextureAssets.InventoryBack9.Width() * scale, 0f);
				Height.Set(TextureAssets.InventoryBack9.Height() * scale, 0f);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				Rectangle inner = GetInnerDimensions().ToRectangle();
				float oldScale = Main.inventoryScale;
				Main.inventoryScale = scale;

				bool isDemonAltar = hoverText == Language.GetTextValue("MapObject.DemonAltar");
				bool isCrimsonAltar = hoverText == Language.GetTextValue("MapObject.CrimsonAltar");
				bool byHand = hoverText == Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.ByHand");

				if (item.type == ItemID.None && !isDemonAltar && !isCrimsonAltar)
					return; // blank item slots should not be drawn

				if (!Id.StartsWith("loot_")) {
					ItemSlot.Draw(spriteBatch, ref item, context, inner.TopLeft());
					Main.inventoryScale = oldScale;

					// Draws the evil altars in the designated slots if needed
					if (isCrimsonAltar || isDemonAltar) {
						Main.instance.LoadTiles(TileID.DemonAltar);
						int offsetX = 0;
						int offsetY = 0;
						int offsetSrc = isCrimsonAltar ? 3 : 0;
						for (int i = 0; i < 6; i++) {
							float scale = 0.64f;
							Rectangle src = new Rectangle((offsetX + offsetSrc) * 18, offsetY * 18, 16, 16 + (offsetY * 2));
							// Determine the position of EACH tile of the selected altar (multi-tile, 3x2)
							float posX = inner.X + (inner.Width / 2) - (src.Width * scale / 2) + (src.Width * scale * (offsetX - 1));
							float posY = inner.Y + (inner.Height / 2) - (src.Height * scale / 2) + (src.Height * scale / 2 * (offsetY == 0 ? -1 : 1));
							Vector2 pos2 = new Vector2(posX, posY);
							spriteBatch.Draw(TextureAssets.Tile[TileID.DemonAltar].Value, pos2, src, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

							offsetX++;
							if (offsetX == 3) {
								offsetX = 0;
								offsetY++;
							}
						}
					}

					// Hover text
					if (IsMouseHovering) {
						if (isCrimsonAltar || isDemonAltar || byHand || item.type == ItemID.None) {
							BossUISystem.Instance.UIHoverText = hoverText; // Empty item, default to hoverText if applicable
						}
						else {
							Main.HoverItem = item;
							Main.hoverItemName = item.HoverName;
						}
					}

					return; // This should cover everything for item slots in the Spawn subpage (spawn item slot, recipe slots, and empty tile slots)
				}

				/// Everything below is being set up for loot related itemslots ///
				BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[BossUISystem.Instance.BossLog.PageNum];
				bool hardModeMasked = BossChecklist.BossLogConfig.MaskHardMode && !Main.hardMode && selectedBoss.progression > BossTracker.WallOfFlesh;
				bool progressRestricted = !selectedBoss.IsDownedOrForced && (BossChecklist.BossLogConfig.MaskBossLoot || hardModeMasked);
				bool expertRestricted = item.expert && !Main.expertMode;
				bool masterRestricted = item.master && !Main.masterMode;
				bool OWmusicRestricted = BossTracker.otherWorldMusicBoxTypes.Contains(item.type) && !BossLogUI.OtherworldUnlocked;

				// Make a backups of the original itemslot texture and alter the texture to display the color needed
				// If the config 'Hide boss drops' is enabled and the boss hasn't been defeated yet, the itemslot should appear red, even if the item was already obtained
				// Otherwise, the itemslot will always appear green if obtained
				// If not obtained and the item is mode or seed restricted, itemslot background is red
				// Any other case should leave the itemslot color as is
				var backup = TextureAssets.InventoryBack7;
				Color oldColor = item.color;
				if (progressRestricted) {
					TextureAssets.InventoryBack7 = TextureAssets.InventoryBack11;
					item.color = Color.Black; // item should be masked in a black silhouette
				}
				else if (hasItem) {
					TextureAssets.InventoryBack7 = TextureAssets.InventoryBack3;
				}
				else if (expertRestricted || masterRestricted || OWmusicRestricted) {
					TextureAssets.InventoryBack7 = TextureAssets.InventoryBack11;
				}
				
				// Draw the item slot and reset the fields to their original value
				ItemSlot.Draw(spriteBatch, ref item, context, inner.TopLeft());
				Main.inventoryScale = oldScale;
				TextureAssets.InventoryBack7 = backup;
				item.color = oldColor; // if the item was masked

				// Draw golden border around items that are considered collectibles
				if (selectedBoss.collection.Contains(item.type)) {
					string texturePath = "BossChecklist/Resources/Extra_HighlightedCollectible";
					Asset<Texture2D> border = ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad);
					spriteBatch.Draw(border.Value, inner.TopLeft(), Color.White);
				}

				// Similar to the logic of deciding the itemslot color, decide what should be drawn and what text should show when hovering over
				// Masked item takes priority, displaying 'Defeat this entry to show items'
				// If the item has not been obtained, check for item restrictions and apply those icons and texts
				// If no item restrictions exist, display normal item tooltips, and draw a checkmark for obtained items
				Vector2 pos = new Vector2(inner.X + inner.Width / 2, inner.Y + inner.Height / 2);
				if (progressRestricted) {
					if (IsMouseHovering) {
						BossUISystem.Instance.UIHoverText = "Mods.BossChecklist.BossLog.HoverText.MaskedItems";
						BossUISystem.Instance.UIHoverTextColor = Color.IndianRed;
					}
				}
				else if (!hasItem) {
					if (expertRestricted) {
						spriteBatch.Draw(Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconDifficultyExpert").Value, pos, Color.White);
						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = "Mods.BossChecklist.BossLog.HoverText.ItemIsExpertOnly";
							BossUISystem.Instance.UIHoverTextColor = Main.DiscoColor;
						}
					}
					else if (masterRestricted) {
						spriteBatch.Draw(Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconDifficultyMaster").Value, pos, Color.White);
						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = "Mods.BossChecklist.BossLog.HoverText.ItemIsMasterOnly";
							BossUISystem.Instance.UIHoverTextColor = new Color(255, (byte)(Main.masterColor * 200f), 0, Main.mouseTextColor); // mimics Master Mode color
						}
					}
					else if (OWmusicRestricted) {
						spriteBatch.Draw(Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconRandomSeed").Value, pos, Color.White);
						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = "Mods.BossChecklist.BossLog.HoverText.ItemIsLocked";
							BossUISystem.Instance.UIHoverTextColor = Color.Goldenrod; // mimics Master Mode color
						}
					}
					else {
						if (IsMouseHovering) {
							Main.HoverItem = item;
							Main.hoverItemName = item.HoverName;
						}
					}
				}
				else {
					spriteBatch.Draw(BossLogUI.checkMarkTexture.Value, pos, Color.White);
					if (IsMouseHovering) {
						Main.HoverItem = item;
						Main.hoverItemName = item.HoverName;
					}
				}

				// Finally, if the 'Show collectible type' config is enabled, draw their respective icons and texts where needed
				if (BossChecklist.DebugConfig.ShowCollectionType && selectedBoss.collection.Contains(item.type)) {
					string showType = "Generic";
					Asset<Texture2D> showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Generic");
					if (selectedBoss.collectType.TryGetValue(item.type, out CollectionType type) && type != CollectionType.Generic) {
						if (type == CollectionType.Trophy) {
							showType = "Trophy";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Trophy");
						}
						else if (type == CollectionType.MusicBox) {
							showType = "Music";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Music");
						}
						else if (type == CollectionType.Mask) {
							showType = "Mask";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Mask");
						}
						else if (type == CollectionType.Pet) {
							showType = "Pet";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Pet");
						}
						else if (type == CollectionType.Mount) {
							showType = "Mount";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Pet");
						}
						else if (type == CollectionType.Relic) {
							showType = "Relic";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Trophy");
						}
					}

					Vector2 iconPos = new Vector2((int)inner.BottomLeft().X - 4, (int)inner.BottomLeft().Y - 15);
					spriteBatch.Draw(showIcon.Value, iconPos, Color.White);
					if (IsMouseHovering) {
						Utils.DrawBorderString(spriteBatch, showType, inner.TopLeft(), Colors.RarityAmber, 0.8f);
					}
				}
			}
		}

		internal class LootRow : UIElement
		{
			readonly int order; // Had to put the itemslots in a row in order to be put in a UIList with scroll functionality

			public LootRow(int order) {
				this.order = order;
				Height.Pixels = 50;
				Width.Pixels = 800;
			}

			public override int CompareTo(object obj) {
				LootRow other = obj as LootRow;
				return order.CompareTo(other.order);
			}
		}

		/// <summary>
		/// Creates an image of a mod's icon when with hovertext of the mod's display name.
		/// </summary>
		internal class ModIcon : UIImage {
			readonly Asset<Texture2D> icon;
			readonly string modName;

			public ModIcon (Asset<Texture2D> icon, string modName) : base(icon) {
				this.icon = icon;
				this.modName = modName;
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (IsMouseHovering)
					PlayerInput.LockVanillaMouseScroll("BossChecklist/BossLogUIElement");
			}

			public override void Draw(SpriteBatch spriteBatch) {
				if (icon.Size() == new Vector2(80, 80)) {
					base.Draw(spriteBatch);
				}
				else {
					spriteBatch.Draw(icon.Value, GetInnerDimensions().ToRectangle(), Color.White); // If the icon size is not 80x80, overwrite the drawing to the proper dimensions
				}

				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
					BossUISystem.Instance.UIHoverText = ModLoader.GetMod(modName).DisplayName;
				}
			}
		}

		internal class BossLogPanel : UIElement
		{
			public string Id { get; init; } = "";

			public override void Draw(SpriteBatch spriteBatch) {
				base.Draw(spriteBatch);
				Rectangle pageRect = GetInnerDimensions().ToRectangle();
				int selectedLogPage = BossUISystem.Instance.BossLog.PageNum;

				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
					Main.player[Main.myPlayer].mouseInterface = true;
					HideMouseOverInteractions();
				}

				if (selectedLogPage == BossLogUI.Page_Prompt) {
					if (Id == "PageOne") {
						Vector2 pos = new Vector2(GetInnerDimensions().X + 10, GetInnerDimensions().Y + 15);
						string message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.BeforeYouBegin");
						Utils.DrawBorderString(spriteBatch, message, pos, Color.White, 0.8f);

						float textScale = 1f;
						message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.EnableProgressionMode");
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(message) * textScale;
						pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 40);
						Utils.DrawBorderString(spriteBatch, message, pos, Colors.RarityAmber, textScale);
					}
					else if (Id == "PageTwo") {
						float textScale = 1f;
						string message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.SelectAnOption");
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(message) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 40);
						Utils.DrawBorderString(spriteBatch, message, pos, Colors.RarityAmber, textScale);
					}
				}
				else if (selectedLogPage == BossLogUI.Page_TableOfContents) {
					if (Id == "PageOne") {
						float textScale = 0.6f;
						string message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.PreHardmode");
						Vector2 stringSize = FontAssets.DeathText.Value.MeasureString(message) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 15);
						Utils.DrawBorderStringBig(spriteBatch, message, pos, Colors.RarityAmber, textScale);
					}
					else if (Id == "PageTwo") {
						float textScale = 0.6f;
						string message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Hardmode");
						Vector2 stringSize = FontAssets.DeathText.Value.MeasureString(message) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 15);
						Utils.DrawBorderStringBig(spriteBatch, message, pos, Colors.RarityAmber, textScale);
					}
				}
				else if (selectedLogPage == BossLogUI.Page_Credits) {
					if (Id == "PageOne") {
						// Mod Developers Credits
						string specialThanks = Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.ThanksDevs");
						float textScale = 1.15f;
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(specialThanks) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 10);
						Utils.DrawBorderString(spriteBatch, specialThanks, pos, Main.DiscoColor, textScale);

						Asset<Texture2D> users = BossChecklist.instance.Assets.Request<Texture2D>("Resources/Extra_CreditUsers");
						string[] usernames = { "Jopojelly", "SheepishShepherd", "direwolf420", "riveren", "Orian", "Panini" };
						string[] titles = { "Mod Owner", "Mod Co-Owner", "Code Contributor", "Spriter", "Beta Tester", "Beta Tester" };
						Color[] colors = { Color.CornflowerBlue, Color.Goldenrod, Color.Tomato, Color.MediumPurple, new Color(49, 210, 162), Color.HotPink };
						const float nameScaling = 0.85f;
						const float titleScaling = 0.75f;

						int row = 0;
						for (int i = 0; i < usernames.Length; i++) {
							bool left = i % 2 == 0;
							bool panini = usernames[i] == "Panini";

							Vector2 userpos = new Vector2(pageRect.X + (pageRect.Width / 2) - 30 + (left ? -85 : 85) - (panini ? 10 : 0), pageRect.Y + 75 + (125 * row));
							Rectangle userselected = new Rectangle(0 + (60 * i), 0, 60 + (panini ? 10 : 0), 58);
							spriteBatch.Draw(users.Value, userpos, userselected, Color.White);
							
							Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(usernames[i]);
							Vector2 stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * nameScaling) / 2) + (panini ? 5 : 0), userpos.Y - 25);
							Utils.DrawBorderString(spriteBatch, usernames[i], stringPos, colors[i], nameScaling);

							stringAdjust = FontAssets.MouseText.Value.MeasureString(titles[i]);
							stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * titleScaling) / 2) + (panini ? 5 : 0), userpos.Y + userselected.Height + 10);
							Utils.DrawBorderString(spriteBatch, titles[i], stringPos, colors[i], titleScaling);

							if (!left) {
								row++;
							}
						}
					}
					else if (Id == "PageTwo" && BossUISystem.Instance.RegisteredMods.Count > 0) {
						// Supported Mod Credits Page
						string thanksMods = Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.ThanksMods");
						float textScale = 1.15f;
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(thanksMods) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 10);
						Utils.DrawBorderString(spriteBatch, thanksMods, pos, Color.LightSkyBlue, textScale);

						string notice = Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.Notice");
						textScale = 0.9f;
						Vector2 stringSize2 = FontAssets.MouseText.Value.MeasureString(notice) * textScale;
						Vector2 pos2 = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize2.X / 2), pos.Y + stringSize2.Y + 5);
						Utils.DrawBorderString(spriteBatch, notice, pos2, Color.LightBlue, textScale);
					}
				}
				else if (selectedLogPage >= 0) {
					// Boss Pages
					BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[selectedLogPage];
					bool masked = BossLogUI.MaskBoss(selectedBoss) == Color.Black;
					if (Id == "PageOne") {
						if (selectedBoss.customDrawing != null) {
							// If a custom drawing is active, full drawing control is given to the modder within the boss portrait
							// Nothing else will be drawn, including any base texture. Modders must supply that if they wish.
							selectedBoss.customDrawing(spriteBatch, pageRect, BossLogUI.MaskBoss(selectedBoss));
						}
						else {
							Asset<Texture2D> bossTexture = null;
							Rectangle bossSourceRectangle = new Rectangle();
							if (selectedBoss.portraitTexture != null) {
								bossTexture = selectedBoss.portraitTexture;
								bossSourceRectangle = new Rectangle(0, 0, bossTexture.Value.Width, bossTexture.Value.Height);
							}
							else if (selectedBoss.npcIDs.Count > 0) {
								Main.instance.LoadNPC(selectedBoss.npcIDs[0]);
								bossTexture = TextureAssets.Npc[selectedBoss.npcIDs[0]];
								bossSourceRectangle = new Rectangle(0, 0, bossTexture.Width(), bossTexture.Height() / Main.npcFrameCount[selectedBoss.npcIDs[0]]);
							}
							if (bossTexture != null) {
								float drawScale = 1f;
								float xScale = (float)pageRect.Width / bossSourceRectangle.Width;
								// TODO: pageRect.Height might be too much, we might want to trim off the top a bit (May need adjusting, but changed to -150)
								float yScale = (float)(pageRect.Height - 150) / bossSourceRectangle.Height;
								if (xScale < 1 || yScale < 1) {
									drawScale = xScale < yScale ? xScale : yScale;
								}
								spriteBatch.Draw(bossTexture.Value, pageRect.Center(), bossSourceRectangle, BossLogUI.MaskBoss(selectedBoss), 0, bossSourceRectangle.Center(), drawScale, SpriteEffects.None, 0f);
							}
						}

						// Everything below this point is outside of the boss portrait (Boss head icons, boss names, etc)

						Rectangle firstHeadPos = new Rectangle();
						bool countedFirstHead = false;
						int offset = 0;
						int totalWidth = 0;
						int lastX = 0;
						foreach (Asset<Texture2D> headTexture in selectedBoss.headIconTextures.Reverse<Asset<Texture2D>>()) {
							Texture2D head = headTexture.Value;
							Rectangle src = new Rectangle(0, 0, head.Width, head.Height);
							// Weird special case for Deerclops. Its head icon has a significant amount of whitespace.
							if (selectedBoss.Key == "Terraria Deerclops") {
								src = new Rectangle(2, 0, 48, 40);
							}
							int xHeadOffset = pageRect.X + pageRect.Width - src.Width - 10 - ((src.Width + 2) * offset);
							Rectangle headPos = new Rectangle(xHeadOffset, pageRect.Y + 5, src.Width, src.Height);
							if (!countedFirstHead) {
								firstHeadPos = headPos;
								countedFirstHead = true;
							}
							spriteBatch.Draw(head, headPos, src, BossLogUI.MaskBoss(selectedBoss));
							offset++;
							totalWidth += headPos.Width;
							lastX = xHeadOffset;
						}

						string isDefeated = $"{Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Defeated", Main.worldName)}";
						string notDefeated = $"{Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Undefeated", Main.worldName)}";

						if (selectedBoss.ForceDowned) {
							isDefeated = $"''{Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Defeated", Main.worldName)}''";
						}

						Asset<Texture2D> texture = selectedBoss.IsDownedOrForced ? BossLogUI.checkMarkTexture : BossLogUI.xTexture;
						Vector2 defeatpos = new Vector2(firstHeadPos.X + (firstHeadPos.Width / 2), firstHeadPos.Y + firstHeadPos.Height - (texture.Height() / 2));
						spriteBatch.Draw(texture.Value, defeatpos, Color.White);

						// Hovering over the head icon will display the defeated text
						Rectangle hoverRect = new Rectangle(lastX, firstHeadPos.Y, totalWidth, firstHeadPos.Height);
						if (Main.MouseScreen.Between(hoverRect.TopLeft(), hoverRect.BottomRight())) {
							BossUISystem.Instance.UIHoverText = selectedBoss.IsDownedOrForced ? isDefeated : notDefeated;
							BossUISystem.Instance.UIHoverTextColor = selectedBoss.IsDownedOrForced ? Colors.RarityGreen : Colors.RarityRed;
						}

						bool enabledCopyButtons = BossChecklist.DebugConfig.AccessInternalNames && selectedBoss.modSource != "Unknown";
						Vector2 pos = new Vector2(pageRect.X + 5 + (enabledCopyButtons ? 25 : 0), pageRect.Y + 5);
						string progression = BossChecklist.DebugConfig.ShowProgressionValue ? $"[{selectedBoss.progression}f] " : "";
						Utils.DrawBorderString(spriteBatch, progression + selectedBoss.DisplayName, pos, Color.Goldenrod);

						if (enabledCopyButtons) {
							Texture2D clipboard = ModContent.Request<Texture2D>("Terraria/Images/UI/CharCreation/Copy", AssetRequestMode.ImmediateLoad).Value;
							Vector2 vec2 = new Vector2(pageRect.X + 5, pos.Y);
							spriteBatch.Draw(clipboard, vec2, Color.Goldenrod);
						}

						pos = new Vector2(pageRect.X + 5 + (enabledCopyButtons ? 25 : 0), pageRect.Y + 30);
						Utils.DrawBorderString(spriteBatch, selectedBoss.SourceDisplayName, pos, new Color(150, 150, 255));

						if (enabledCopyButtons) {
							Texture2D clipboard = ModContent.Request<Texture2D>("Terraria/Images/UI/CharCreation/Copy", AssetRequestMode.ImmediateLoad).Value;
							Rectangle clipRect = new Rectangle(pageRect.X + 5, pageRect.Y + 5, clipboard.Width, clipboard.Height);

							Color copied = (Platform.Get<IClipboard>().Value == selectedBoss.Key) ? Color.Gold : Color.White;
							spriteBatch.Draw(clipboard, clipRect, copied);

							// Hovering and rightclick will copy to clipboard
							if (Main.MouseScreen.Between(clipRect.TopLeft(), clipRect.BottomRight())) {
								string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.CopyKey");
								BossUISystem.Instance.UIHoverText = $"{translated}:\n{selectedBoss.Key}";
								if (Main.mouseLeft && Main.mouseLeftRelease) {
									Platform.Get<IClipboard>().Value = selectedBoss.Key;
								}
							}

							clipRect.Y += 25;
							copied = (Platform.Get<IClipboard>().Value == selectedBoss.modSource) ? Color.Gold : Color.White;
							spriteBatch.Draw(clipboard, clipRect, copied);

							if (Main.MouseScreen.Between(clipRect.TopLeft(), clipRect.BottomRight())) {
								string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.CopySource");
								BossUISystem.Instance.UIHoverText = $"{translated}:\n{selectedBoss.modSource}";
								if (Main.mouseLeft && Main.mouseLeftRelease) {
									Platform.Get<IClipboard>().Value = selectedBoss.modSource;
								}
							}
						}
					}
					else if (Id == "PageTwo" && selectedBoss.modSource != "Unknown") {
						if (BossLogUI.SelectedSubPage == SubPage.Records) {
							if (selectedBoss.type == EntryType.Boss) {
								// Boss Records Subpage
							}
							else if (selectedBoss.type == EntryType.MiniBoss) {
								foreach (BossInfo entry in BossChecklist.bossTracker.SortedBosses) {
									if (entry.type != EntryType.Event)
										continue;

									if (entry.npcIDs.Contains(selectedBoss.npcIDs[0])) {
										Texture2D icon = entry.headIconTextures[0].Value;
										Rectangle headPos = new Rectangle(pageRect.X + 15, pageRect.Y + 100, icon.Width, icon.Height);
										Color faded = entry.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;
										spriteBatch.Draw(icon, headPos, faded);
										if (Main.MouseScreen.Between(headPos.TopLeft(), headPos.BottomRight())) {
											string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewPage");
											BossUISystem.Instance.UIHoverText = entry.DisplayName + "\n" + translated;
											if (Main.mouseLeft && Main.mouseLeftRelease) {
												BossUISystem.Instance.BossLog.PendingPageNum = entry.GetIndex; // Reset UI positions when changing the page
											}
										}
									}
								}
							}
							else if (selectedBoss.type == EntryType.Event) {
								var bosses = BossChecklist.bossTracker.SortedBosses;
								int offset = 0;
								int offsetY = 0;

								int headTextureOffsetX = 0;
								int headTextureOffsetY = 0;

								foreach (int npcID in selectedBoss.npcIDs) {
									int npcIndex = bosses.FindIndex(x => x.npcIDs.Contains(npcID) && x.type != EntryType.Event);
									if (npcIndex == -1)
										continue;

									BossInfo addedNPC = bosses[npcIndex];
									Texture2D head = addedNPC.headIconTextures[0].Value;
									Rectangle headPos = new Rectangle(pageRect.X + headTextureOffsetX + 15, pageRect.Y + 100, head.Width, head.Height);
									Color headColor = addedNPC.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;

									spriteBatch.Draw(head, headPos, headColor);
									headTextureOffsetX += head.Width + 5;
									if (Main.MouseScreen.Between(headPos.TopLeft(), headPos.BottomRight())) {
										string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewPage");
										BossUISystem.Instance.UIHoverText = addedNPC.DisplayName + "\n" + translated;
										if (Main.mouseLeft && Main.mouseLeftRelease) {
											BossUISystem.Instance.BossLog.PendingPageNum = addedNPC.GetIndex; // Reset UI positions when changing the page
										}
									}
									if (head.Height > headTextureOffsetY) {
										headTextureOffsetY = head.Height;
									}
								}

								offset = 0;
								if (headTextureOffsetY != 0) {
									offsetY = headTextureOffsetY + 5;
								}

								foreach (int npcID in selectedBoss.npcIDs) {
									if (offset == 0 && offsetY == 4)
										break; // For now, we stop drawing any banners that exceed the books limit (TODO: might have to reimplement as a UIList for scrolling purposes)

									if (npcID < NPCID.Count) {
										int init = Item.NPCtoBanner(npcID) + 21;
										if (init <= 21)
											continue;

										Main.instance.LoadNPC(npcID);
										Main.instance.LoadTiles(TileID.Banners);
										Asset<Texture2D> banner = TextureAssets.Tile[TileID.Banners];

										int jump = 0;
										if (init >= 222) {
											jump = 6;
											init -= 222;
										}
										else if (init >= 111) {
											jump = 3;
											init -= 111;
										}

										int bannerID = Item.NPCtoBanner(npcID);
										int bannerItem = Item.BannerToItem(bannerID);
										bool reachedKillCount = NPC.killCount[bannerID] >= ItemID.Sets.KillsToBanner[bannerItem];
										Color bannerColor = reachedKillCount ? Color.White : masked ? Color.Black : BossLogUI.faded;
										if (bannerID <= 0 || NPCID.Sets.PositiveNPCTypesExcludedFromDeathTally[NPCID.FromNetId(npcID)])
											continue;

										for (int j = 0; j < 3; j++) {
											Rectangle bannerPos = new Rectangle(pageRect.X + offset + 15, pageRect.Y + 100 + (16 * j) + offsetY, 16, 16);
											Rectangle rect = new Rectangle(init * 18, (jump * 18) + (j * 18), 16, 16);
											spriteBatch.Draw(banner.Value, bannerPos, rect, bannerColor);

											if (Main.MouseScreen.Between(bannerPos.TopLeft(), bannerPos.BottomRight())) {
												string npcName = masked ? "???" : Lang.GetNPCNameValue(npcID);
												string killcount = $"\n{NPC.killCount[Item.NPCtoBanner(npcID)]}";
												if (!reachedKillCount) {
													killcount += $" / {ItemID.Sets.KillsToBanner[Item.BannerToItem(bannerID)]}";
												}
												BossUISystem.Instance.UIHoverText = npcName + killcount;
											}
										}
										offset += 25;
										if (offset == 14 * 25) {
											offset = 0;
											offsetY += 64;
										}
									}
									else { // Its a modded NPC
										Main.instance.LoadNPC(npcID);

										int bannerItemID = NPCLoader.GetNPC(npcID).BannerItem;
										if (bannerItemID <= 0 || !ContentSamples.ItemsByType.TryGetValue(bannerItemID, out Item item))
											continue;

										if (item.createTile <= -1)
											continue;

										Main.instance.LoadTiles(item.createTile);
										Asset<Texture2D> banner = TextureAssets.Tile[item.createTile];

										// Code adapted from TileObject.DrawPreview
										var tileData = TileObjectData.GetTileData(item.createTile, item.placeStyle);
										int styleColumn = tileData.CalculatePlacementStyle(item.placeStyle, 0, 0); // adjust for StyleMultiplier
										int styleRow = 0;
										//int num3 = tileData.DrawYOffset;
										if (tileData.StyleWrapLimit > 0) {
											styleRow = styleColumn / tileData.StyleWrapLimit * tileData.StyleLineSkip; // row quotient
											styleColumn %= tileData.StyleWrapLimit; // remainder
										}

										int x;
										int y;
										if (tileData.StyleHorizontal) {
											x = tileData.CoordinateFullWidth * styleColumn;
											y = tileData.CoordinateFullHeight * styleRow;
										}
										else {
											x = tileData.CoordinateFullWidth * styleRow;
											y = tileData.CoordinateFullHeight * styleColumn;
										}

										int bannerID = NPCLoader.GetNPC(npcID).Banner;
										int bannerItem = NPCLoader.GetNPC(npcID).BannerItem;
										string source = NPCLoader.GetNPC(npcID).Mod.DisplayName;
										bool reachedKillCount = NPC.killCount[bannerID] >= ItemID.Sets.KillsToBanner[bannerItem];

										Color bannerColor = NPC.killCount[bannerID] >= 50 ? Color.White : masked ? Color.Black : BossLogUI.faded;

										int[] heights = tileData.CoordinateHeights;
										int heightOffSet = 0;
										int heightOffSetTexture = 0;
										for (int j = 0; j < heights.Length; j++) { // could adjust for non 1x3 here and below if we need to.
											Rectangle bannerPos = new Rectangle(pageRect.X + offset + 15, pageRect.Y + 100 + heightOffSet + offsetY, 16, 16);
											Rectangle rect = new Rectangle(x, y + heightOffSetTexture, tileData.CoordinateWidth, tileData.CoordinateHeights[j]);
											Main.spriteBatch.Draw(banner.Value, bannerPos, rect, bannerColor);
											heightOffSet += heights[j];
											heightOffSetTexture += heights[j] + tileData.CoordinatePadding;

											if (Main.MouseScreen.Between(bannerPos.TopLeft(), bannerPos.BottomRight())) {
												string npcName = masked ? "???" : Lang.GetNPCNameValue(npcID);
												string killcount = $"\n{NPC.killCount[Item.NPCtoBanner(npcID)]}";
												if (!reachedKillCount) {
													killcount += $" / {ItemID.Sets.KillsToBanner[Item.BannerToItem(bannerID)]}";
												}
												BossUISystem.Instance.UIHoverText = npcName + killcount;
											}
										}
										offset += 25;
										if (offset == 14 * 25) {
											offset = 0;
											offsetY += 64;
										}
									}
								}
							}
						}
						else if (BossLogUI.SelectedSubPage == SubPage.SpawnInfo) {
							// Spawn Item Subpage
						}
						else if (BossLogUI.SelectedSubPage == SubPage.LootAndCollectibles) {
							// Loot Table Subpage
							Asset<Texture2D> bagTexture;
							if (selectedBoss.treasureBag != 0) {
								Main.instance.LoadItem(selectedBoss.treasureBag);
								bagTexture = TextureAssets.Item[selectedBoss.treasureBag];
							}
							else {
								bagTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_TreasureBag", AssetRequestMode.ImmediateLoad);
							}
							DrawAnimation drawAnim = Main.itemAnimations[selectedBoss.treasureBag]; // 0 is null
							Rectangle srcRect = drawAnim != null ? srcRect = drawAnim.GetFrame(bagTexture.Value) : bagTexture.Value.Bounds;
							Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - 5 - (bagTexture.Width() / 2), pageRect.Y + 88, srcRect.Width, srcRect.Height);
							spriteBatch.Draw(bagTexture.Value, posRect, srcRect, Color.White);
						}
					}
				}
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (IsMouseHovering)
					PlayerInput.LockVanillaMouseScroll("BossChecklist/BossLogUIElement");
			}
		}

		internal class RecordDisplaySlot : UIImage
		{
			BossInfo entry;
			PersonalStats stats;
			WorldStats worldStats;

			int slotID = -1;
			string title;
			string value;
			int achX;
			int achY;
			string tooltip;

			public RecordDisplaySlot(Asset<Texture2D> texture, SubCategory subCategory, int slot) : base(texture) {
				this.entry = BossChecklist.bossTracker.SortedBosses[BossUISystem.Instance.BossLog.PageNum];
				this.stats = Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld[entry.GetRecordIndex].stats;
				this.worldStats = WorldAssist.worldRecords[entry.GetRecordIndex].stats;

				slotID = slot;
				title = GetTitle(subCategory)[slot];
				value = GetValue(subCategory)[slot];
				tooltip = GetTooltip(subCategory)[slot];
				achX = (int)GetAchCoords(subCategory)[slot].X;
				achY = (int)GetAchCoords(subCategory)[slot].Y;
			}

			private string[] GetTitle(SubCategory sub) {
				string path = "Mods.BossChecklist.BossLog.RecordSlot.Title";
				return new string[] {
					Language.GetTextValue($"Mods.BossChecklist.BossLog.Terms.{sub}"),
					Language.GetTextValue($"{path}.{sub}"),
					Language.GetTextValue($"{path}.Duration{(sub == SubCategory.WorldRecord ? "World" : "")}"),
					Language.GetTextValue($"{path}.HitsTaken{(sub == SubCategory.WorldRecord ? "World" : "")}")
				};
			}

			private string[] GetValue(SubCategory sub) {
				// Defaults to Previous Attempt, the subcategory users will first see
				string unique = stats.attempts == 0 ? Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Unchallenged") : stats.attempts.ToString();
				string duration = PersonalStats.TimeConversion(stats.durationPrev);
				string hitsTaken = PersonalStats.HitCount(stats.hitsTakenPrev);

				if (sub == SubCategory.PersonalBest) {
					unique = stats.GetKDR();
					duration = PersonalStats.TimeConversion(stats.durationBest);
					hitsTaken = PersonalStats.HitCount(stats.hitsTakenBest);
				}
				else if (sub == SubCategory.FirstVictory) {
					unique = stats.PlayTimeToString();
					duration = PersonalStats.TimeConversion(stats.durationFirst);
					hitsTaken = PersonalStats.HitCount(stats.hitsTakenFirst);
				}
				else if (sub == SubCategory.WorldRecord) {
					unique = worldStats.GetGlobalKDR();
					duration = PersonalStats.TimeConversion(worldStats.durationWorld);
					hitsTaken = PersonalStats.HitCount(worldStats.hitsTakenWorld);
				}

				return new string[] {
					Language.GetTextValue(sub == SubCategory.WorldRecord ? Main.worldName : Main.LocalPlayer.name),
					unique,
					duration,
					hitsTaken
				};
			}

			private string[] GetTooltip(SubCategory sub) {
				string path = "Mods.BossChecklist.BossLog.RecordSlot.Tooltip";
				return new string[] {
					"",
					Language.GetTextValue($"{path}.{sub}"),
					Language.GetTextValue($"{path}.Duration"),
					Language.GetTextValue($"{path}.HitsTaken")
				};
			}

			private Vector2[] GetAchCoords(SubCategory sub) {
				Vector2 uniqueAch = new Vector2(0, 9);

				if (sub == SubCategory.PersonalBest) {
					uniqueAch = new Vector2(0, 3);
				}
				else if (sub == SubCategory.FirstVictory) {
					uniqueAch = new Vector2(7, 10);
				}
				else if (sub == SubCategory.WorldRecord) {
					uniqueAch = worldStats.totalKills >= worldStats.totalDeaths ? new Vector2(4, 10) : new Vector2(4, 8);
				}

				return new Vector2[] {
					new Vector2(-1, -1),
					uniqueAch,
					sub == SubCategory.WorldRecord ? new Vector2(2, 12) : new Vector2(4, 9),
					sub == SubCategory.WorldRecord ? new Vector2(0, 7) : new Vector2(3, 0)
				};
			}

			public override void Draw(SpriteBatch spriteBatch) {
				base.Draw(spriteBatch);
				Rectangle inner = GetInnerDimensions().ToRectangle();

				// Draw an achievement icon that represents the record type
				if (achX >= 0 && achY >= 0) {
					Texture2D achievements = ModContent.Request<Texture2D>("Terraria/Images/UI/Achievements").Value;
					Rectangle achSlot = new Rectangle(66 * achX, 66 * achY, 64, 64);
					spriteBatch.Draw(achievements, inner.TopLeft(), achSlot, Color.White);

					if (Main.MouseScreen.Between(inner.TopLeft(), new Vector2(inner.X + 64, inner.Y + 64))) {
						BossUISystem.Instance.UIHoverText = tooltip;
					}
				}

				// TODO: Experimental feature notice, eventually will need to be removed
				if (slotID == 0) {
					Texture2D bnnuy = ModContent.Request<Texture2D>("Terraria/Images/UI/Creative/Journey_Toggle", AssetRequestMode.ImmediateLoad).Value;
					Rectangle bnnuyRect = new Rectangle(inner.X + inner.Width - bnnuy.Width - 15, inner.Y + inner.Height / 2 - bnnuy.Height / 2, bnnuy.Width, bnnuy.Height);
					spriteBatch.Draw(bnnuy, bnnuyRect, Color.White);

					if (Main.MouseScreen.Between(bnnuyRect.TopLeft(), bnnuyRect.BottomRight())) {
						string noticeText;
						if (BossLogUI.RecordSubCategory == SubCategory.WorldRecord) {
							noticeText = $"World Records are currently {(BossChecklist.DebugConfig.DisableWorldRecords ? $"[c/{Color.Red.Hex3()}:disabled]" : $"[c/{Color.LightGreen.Hex3()}:enabled]")}" +
								"\nThe World Records feature is still under construction." +
								"\nThis feature is known to not work and cause issues, so enable at your own risk." +
								$"\nWorld Records can be {(BossChecklist.DebugConfig.DisableWorldRecords ? "enabled" : "disabled")} under the Feature Testing configs.";
						}
						else {
							noticeText = $"Boss Records are currently {(BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE ? $"[c/{Color.Red.Hex3()}:disabled]" : $"[c/{Color.LightGreen.Hex3()}:enabled]")}" +
								"\nThis section of the Boss Log is still under construction." +
								"\nAny features or configs related to this page may not work or cause issues." +
								$"\nBoss Records can be toggled under the Feature Testing configs.";
						}
						BossUISystem.Instance.UIHoverText = noticeText;
						BossUISystem.Instance.UIHoverTextColor = Color.Gold;
					}

					foreach (BossInfo eventEntry in BossChecklist.bossTracker.SortedBosses) {
						if (eventEntry.type == EntryType.Event && eventEntry.npcIDs.Contains(entry.npcIDs[0])) {
							Texture2D icon = eventEntry.headIconTextures[0].Value;
							Rectangle headRect = new Rectangle(inner.X + 15, inner.Y + inner.Height / 2 - icon.Height / 2, icon.Width, icon.Height);
							Color faded = eventEntry.IsDownedOrForced ? Color.White : BossLogUI.MaskBoss(entry) == Color.Black ? Color.Black : BossLogUI.faded;
							spriteBatch.Draw(icon, headRect, faded);
							if (Main.MouseScreen.Between(headRect.TopLeft(), headRect.BottomRight())) {
								string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewPage");
								BossUISystem.Instance.UIHoverText = eventEntry.DisplayName + "\n" + translated;
								if (Main.mouseLeft && Main.mouseLeftRelease) {
									BossUISystem.Instance.BossLog.PendingPageNum = eventEntry.GetIndex;
								}
							}
							break; // Stop at the first event
						}
					}
				}

				// Draw the title and record value texts
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(title);
				Color col = slotID == 0 ? Color.Goldenrod : Color.Gold;
				Vector2 pos = new Vector2(inner.X + (inner.Width / 2) - (int)(stringAdjust.X / 2) + 2, inner.Y + (int)(stringAdjust.Y / 3));
				Utils.DrawBorderString(spriteBatch, title, pos, col);

				stringAdjust = FontAssets.MouseText.Value.MeasureString(value);
				col = slotID == 0 ? Color.LightYellow : Color.White;
				pos = new Vector2(inner.X + (inner.Width / 2) - (int)(stringAdjust.X / 2) + 2, inner.Y + inner.Height - (int)stringAdjust.Y);
				Utils.DrawBorderString(spriteBatch, value, pos, col);
			}
		}

		internal class FixedUIScrollbar : UIScrollbar
		{
			protected override void DrawSelf(SpriteBatch spriteBatch) {
				UserInterface temp = UserInterface.ActiveInstance;
				UserInterface.ActiveInstance = BossUISystem.Instance.BossLogInterface;
				base.DrawSelf(spriteBatch);
				UserInterface.ActiveInstance = temp;
			}

			public override void MouseDown(UIMouseEvent evt) {
				UserInterface temp = UserInterface.ActiveInstance;
				UserInterface.ActiveInstance = BossUISystem.Instance.BossLogInterface;
				base.MouseDown(evt);
				UserInterface.ActiveInstance = temp;
			}

			public override void Click(UIMouseEvent evt) {
				UserInterface temp = UserInterface.ActiveInstance;
				UserInterface.ActiveInstance = BossUISystem.Instance.BossLogInterface;
				base.Click(evt);
				UserInterface.ActiveInstance = temp;
			}

			public override void ScrollWheel(UIScrollWheelEvent evt) {
				//Main.NewText(evt.ScrollWheelValue);
				base.ScrollWheel(evt);
				//if (BossLogUI.PageNum < 0 || BossLogUI.SubPageNum != 1) return;
				if (this.Parent != null && this.Parent.IsMouseHovering) {
					//Main.NewText(evt.ScrollWheelValue);
					this.ViewPosition -= (float)evt.ScrollWheelValue / 1000;
				}
				else if (this.Parent != null && this.Parent.IsMouseHovering) {
					//Main.NewText(evt.ScrollWheelValue);
					this.ViewPosition -= (float)evt.ScrollWheelValue / 1000;
				}
			}
		}

		internal class BookUI : UIImage
		{
			public string Id { get; init; } = "";
			readonly Asset<Texture2D> book;
			readonly BossLogUI log;
			public static bool isDrawn;

			public BookUI(Asset<Texture2D> texture) : base(texture) {
				book = texture;
				log = BossUISystem.Instance.BossLog;
			}

			internal static bool DrawTab(string Id) {
				int page = BossUISystem.Instance.BossLog.PageNum;
				bool MatchesCreditsTab = page == -2 && Id == "Credits_Tab";
				bool MatchesBossTab = page == BossLogUI.FindNext(EntryType.Boss) && Id == "Boss_Tab";
				bool MatchesMinibossTab = (page == BossLogUI.FindNext(EntryType.MiniBoss) || BossChecklist.BossLogConfig.OnlyShowBossContent) && Id == "Miniboss_Tab";
				bool MatchesEventTab = (page == BossLogUI.FindNext(EntryType.Event) || BossChecklist.BossLogConfig.OnlyShowBossContent) && Id == "Event_Tab";
				return !(MatchesCreditsTab || MatchesBossTab || MatchesMinibossTab || MatchesEventTab);
			}

			internal string DetermineHintText() {
				int selectedLogPage = log.PageNum;
				string hintText = "";
				if (selectedLogPage == -1) {
					hintText += Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.MarkEntry");
					hintText += "\n" + Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.HideEntry");
					if (BossChecklist.DebugConfig.ResetForcedDowns) {
						hintText += "\n" + Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearMarked");
					}
					if (BossChecklist.DebugConfig.ResetHiddenEntries) {
						hintText += "\n" + Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearHidden");
					}
				}
				else if (selectedLogPage >= 0) {
					if (BossLogUI.SelectedSubPage == SubPage.Records && BossChecklist.DebugConfig.ResetRecordsBool && BossLogUI.RecordSubCategory != SubCategory.WorldRecord) {
						//hintText += Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearRecord"); // TODO: Make this function. Clear a singular record
						hintText += Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearAllRecords");
					}
					if (BossLogUI.SelectedSubPage == SubPage.LootAndCollectibles && BossChecklist.DebugConfig.ResetLootItems) {
						hintText += Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.RemoveItem");
						hintText += "\n" + Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearItems");
					}
				}

				return hintText;
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (IsMouseHovering)
					PlayerInput.LockVanillaMouseScroll("BossChecklist/BossLogUIElement");
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				int selectedLogPage = log.PageNum;

				if (Id == "Info_Tab") {
					if (BossChecklist.BossLogConfig.AnyProgressionModeConfigUsed) {
						Rectangle rect = GetDimensions().ToRectangle();
						spriteBatch.Draw(book.Value, rect, Color.Firebrick);

						Texture2D texture = Main.Assets.Request<Texture2D>($"Images/Item_{ItemID.Blindfold}", AssetRequestMode.ImmediateLoad).Value;
						float scale = 0.85f;
						Vector2 pos = new Vector2(rect.X + rect.Width / 2 - texture.Width * scale / 2, rect.Y + rect.Height / 2 - texture.Height * scale / 2);
						spriteBatch.Draw(texture, pos, texture.Bounds, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = "Mods.BossChecklist.BossLog.HoverText.ProgressionModeIsEnabled";
							BossUISystem.Instance.UIHoverTextColor = Color.Wheat;
						}
					}
					return;
				}
				else if (Id == "Shortcut_Tab") {
					if (!string.IsNullOrEmpty(DetermineHintText())) {
						Rectangle rect = GetDimensions().ToRectangle();
						spriteBatch.Draw(book.Value, rect, Color.Tan);

						Texture2D texture = BossLogUI.mouseTexture.Value;
						Vector2 pos = new Vector2(rect.X + rect.Width / 2 - texture.Width / 2, rect.Y + rect.Height / 2 - texture.Height / 2);
						spriteBatch.Draw(texture, pos, texture.Bounds, Color.White);

						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = DetermineHintText();
							BossUISystem.Instance.UIHoverTextColor = Color.Wheat;
						}
					}
					return;
				}
				else if (Id == "ToCFilter_Tab") {
					// The hardback part of the UIPanel should be layered under all of the tabs, so it is drawn here
					Asset<Texture2D> pages = BossChecklist.instance.Assets.Request<Texture2D>("Resources/LogUI_Back");
					BossLogPanel panel = BossUISystem.Instance.BossLog.BookArea;
					Vector2 pagePos = new Vector2(panel.Left.Pixels, panel.Top.Pixels);
					spriteBatch.Draw(pages.Value, pagePos, BossChecklist.BossLogConfig.BossLogColor);
				}

				if (!Id.EndsWith("_Tab")) {
					base.DrawSelf(spriteBatch);
				}
				else {
					// Tab drawing
					SpriteEffects effect = SpriteEffects.FlipHorizontally;
					if (Id == "Boss_Tab" && (selectedLogPage >= BossLogUI.FindNext(EntryType.Boss) || selectedLogPage == -2)) {
						effect = SpriteEffects.None;
					}
					else if (Id == "Miniboss_Tab" && (selectedLogPage >= BossLogUI.FindNext(EntryType.MiniBoss) || selectedLogPage == -2)) {
						effect = SpriteEffects.None;
					}
					else if (Id == "Event_Tab" && (selectedLogPage >= BossLogUI.FindNext(EntryType.Event) || selectedLogPage == -2)) {
						effect = SpriteEffects.None;
					}
					else if (Id == "ToCFilter_Tab") {
						effect = SpriteEffects.None;
					}

					if (DrawTab(Id) && selectedLogPage != -3) {
						spriteBatch.Draw(book.Value, GetDimensions().ToRectangle(), new Rectangle(0, 0, book.Width(), book.Height()), Color.Tan, 0f, Vector2.Zero, effect, 0f);
					}
				}

				if (Id == "Event_Tab") {
					// Paper Drawing
					// The paper part of the UIPanel should be layered on top of all tabs, so it is drawn here
					Asset<Texture2D> pages = BossChecklist.instance.Assets.Request<Texture2D>("Resources/LogUI_Paper");
					BossLogPanel panel = BossUISystem.Instance.BossLog.BookArea;
					Vector2 pagePos = new Vector2(panel.Left.Pixels, panel.Top.Pixels);
					spriteBatch.Draw(pages.Value, pagePos, Color.White);
				}

				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
					// Needed to remove mousetext from outside sources when using the Boss Log
					Main.player[Main.myPlayer].mouseInterface = true;
					HideMouseOverInteractions();
				}

				if (Id.EndsWith("_Tab") && selectedLogPage != -3) {
					// Tab Icon
					Asset<Texture2D> texture = BossLogUI.tocTexture;

					if (Id == "Boss_Tab") {
						texture = BossLogUI.bossNavTexture;
					}
					else if (Id == "Miniboss_Tab") {
						texture = BossLogUI.minibossNavTexture;
					}
					else if (Id == "Event_Tab") {
						texture = BossLogUI.eventNavTexture;
					}
					else if (Id == "Credits_Tab") {
						texture = BossLogUI.credTexture;
					}
					else if (Id == "ToCFilter_Tab" && selectedLogPage == -1) {
						texture = BossLogUI.filterTexture;
					}
					else if (Id == "ToCFilter_Tab" && selectedLogPage != -1) {
						texture = BossLogUI.tocTexture;
					}

					Rectangle inner = GetInnerDimensions().ToRectangle();
					int offsetX = inner.X < Main.screenWidth / 2 ? 2 : -2;
					Vector2 pos = new Vector2(inner.X + (inner.Width / 2) - (texture.Value.Width / 2) + offsetX, inner.Y + (inner.Height / 2) - (texture.Value.Height / 2));

					if (DrawTab(Id))
						spriteBatch.Draw(texture.Value, pos, Color.White);
					else
						return;

					if (IsMouseHovering) {
						List<BossInfo> bossList = BossChecklist.bossTracker.SortedBosses;
						string tabMessage = "";
						if (Id == "Boss_Tab" && BossLogUI.FindNext(EntryType.Boss) != -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpBoss", bossList[BossLogUI.FindNext(EntryType.Boss)].DisplayName);
						}
						else if (Id == "Miniboss_Tab" && BossLogUI.FindNext(EntryType.MiniBoss) != -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpMini", bossList[BossLogUI.FindNext(EntryType.MiniBoss)].DisplayName);
						}
						else if (Id == "Event_Tab" && BossLogUI.FindNext(EntryType.Event) != -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpEvent", bossList[BossLogUI.FindNext(EntryType.Event)].DisplayName);
						}
						else if (Id == "Credits_Tab") {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpCred");
						}
						else if (Id == "ToCFilter_Tab" && selectedLogPage == -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ToggleFilters");
						}
						else if (Id == "ToCFilter_Tab" && selectedLogPage != -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpTOC");
						}
						if (tabMessage != "") {
							BossUISystem.Instance.UIHoverText = tabMessage;
						}
					}
				}

				if (Id.Contains("F_") && IsMouseHovering) {
					string termPrefix = "Mods.BossChecklist.BossLog.Terms.";
					string termLang = $"{termPrefix}hide";
					string termLang2 = $"";
					if (Id == "F_0") {
						termLang = $"{termPrefix}{BossChecklist.BossLogConfig.FilterBosses.ToLower().Replace(" ", "")}";
						termLang2 = $"{termPrefix}Bosses";
						BossUISystem.Instance.UIHoverText = $"{Language.GetTextValue(termLang)} {Language.GetTextValue(termLang2)}";
					}
					if (Id == "F_1") {
						if (!BossChecklist.BossLogConfig.OnlyShowBossContent) {
							termLang = $"{termPrefix}{BossChecklist.BossLogConfig.FilterMiniBosses.ToLower().Replace(" ", "")}";
							termLang2 = $"{termPrefix}MiniBosses";
						}
						BossUISystem.Instance.UIHoverText = $"{Language.GetTextValue(termLang)} {Language.GetTextValue(termLang2)}";
					}
					if (Id == "F_2") {
						if (!BossChecklist.BossLogConfig.OnlyShowBossContent) {
							termLang = $"{termPrefix}{BossChecklist.BossLogConfig.FilterEvents.ToLower().Replace(" ", "")}";
							termLang2 = $"{termPrefix}Events";
						}
						BossUISystem.Instance.UIHoverText = $"{Language.GetTextValue(termLang)} {Language.GetTextValue(termLang2)}";
					}
					if (Id == "F_3") {
						BossUISystem.Instance.UIHoverText = "Mods.BossChecklist.BossLog.HoverText.ToggleVisibility";
					}
				}
			}
		}

		internal class TableOfContents : UIText
		{
			public int Index { get; init; }
			readonly float order = 0;
			readonly bool isNext;
			readonly bool downed;
			readonly string displayName;
			readonly bool allLoot;
			readonly bool allCollectibles;

			public TableOfContents(int index, string displayName, bool nextCheck, bool loot, bool collect, float textScale = 1, bool large = false) : base(displayName, textScale, large) {
				this.Index = index;
				this.displayName = displayName;
				this.isNext = nextCheck;
				this.order = BossChecklist.bossTracker.SortedBosses[Index].progression;
				this.downed = BossChecklist.bossTracker.SortedBosses[Index].IsDownedOrForced;
				this.allLoot = loot;
				this.allCollectibles = collect;
			}

			public override void MouseOver(UIMouseEvent evt) {
				BossLogUI.headNum = Index;
				if (BossChecklist.DebugConfig.ShowProgressionValue) {
					SetText($"[{order}f] {displayName}");
				}
				base.MouseOver(evt);
			}

			public override void MouseOut(UIMouseEvent evt) {
				BossLogUI.headNum = -1; // MouseOut will occur even if the element is removed when changing pages!
				SetText(displayName);
				base.MouseOut(evt);
			}

			public override void Draw(SpriteBatch spriteBatch) {
				Rectangle inner = GetInnerDimensions().ToRectangle();
				Vector2 pos = new Vector2(inner.X - 20, inner.Y - 5);
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[Index];

				if (order != -1) {
					// Use the appropriate text color for conditions
					if ((!selectedBoss.available() && !downed) || selectedBoss.hidden) {
						TextColor = Color.DimGray; // Hidden or Unavailable entry text color takes priority over all other text color alterations
					}
					else if (BossChecklist.BossLogConfig.ColoredBossText) {
						int recordIndex = BossChecklist.bossTracker.SortedBosses[Index].GetRecordIndex;
						if (recordIndex != -1 && modPlayer.hasNewRecord[recordIndex]) {
							TextColor = Main.DiscoColor;
						}
						else if (IsMouseHovering) {
							TextColor = TextColor = Color.SkyBlue;
						}
						else if (isNext) {
							TextColor = new Color(248, 235, 91);
						}
						else {
							TextColor = downed ? Colors.RarityGreen : Colors.RarityRed;
						}
					}
					else {
						TextColor = IsMouseHovering ? Color.Silver : Color.White; // Disabled colored text
					}
				}

				// base drawing comes after colors so they do not flicker when updating check list
				base.Draw(spriteBatch);

				Rectangle parent = this.Parent.GetInnerDimensions().ToRectangle();
				int hardModeOffset = selectedBoss.progression > BossTracker.WallOfFlesh ? 10 : 0;
				string looted = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.AllLoot");
				string collected = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.AllCollectibles");
				Texture2D texture = null;
				string hoverText = "";

				if (allLoot && allCollectibles) {
					texture = BossLogUI.goldChestTexture.Value;
					hoverText = $"{looted}\n{collected}";
				}
				else if (allLoot || allCollectibles) {
					texture = BossLogUI.chestTexture.Value;
					if (allLoot) {
						looted = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.AllDropLoot");
					}
					hoverText = allLoot ? Language.GetTextValue(looted) : Language.GetTextValue(collected);
				}

				if (texture != null) {
					Rectangle chestPos = new Rectangle(parent.X + parent.Width - texture.Width - hardModeOffset, inner.Y - 2, texture.Width, texture.Height);
					spriteBatch.Draw(texture, chestPos, Color.White);
					if (Main.MouseScreen.Between(chestPos.TopLeft(), chestPos.BottomRight())) {
						BossUISystem.Instance.UIHoverText = hoverText;
					}
				}

				if (order != -1f) {
					Asset<Texture2D> checkGrid = BossLogUI.checkboxTexture;
					string checkType = BossChecklist.BossLogConfig.SelectedCheckmarkType;

					if (downed) {
						if (checkType == "X and  ☐") {
							checkGrid = BossLogUI.xTexture;
						}
						else if (checkType != "Strike-through") {
							checkGrid = BossLogUI.checkMarkTexture;
						}
						else {
							Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(displayName);
							Asset<Texture2D> strike = BossChecklist.instance.Assets.Request<Texture2D>("Resources/Checks_Strike");
							int w = strike.Value.Width / 3;
							int h = strike.Value.Height;

							Color hoverColor = IsMouseHovering ? BossLogUI.faded : Color.White;
							int offsetY = (int)(inner.Y + (stringAdjust.Y / 3) - h / 2);

							Rectangle strikePos = new Rectangle(inner.X - w, offsetY, w, h);
							Rectangle strikeSrc = new Rectangle(0, 0, w, h);
							spriteBatch.Draw(strike.Value, strikePos, strikeSrc, hoverColor);

							strikePos = new Rectangle(inner.X, offsetY, (int)stringAdjust.X, h);
							strikeSrc = new Rectangle(w, 0, w, h);
							spriteBatch.Draw(strike.Value, strikePos, strikeSrc, IsMouseHovering ? Color.Transparent : Color.White);

							strikePos = new Rectangle(inner.X + (int)stringAdjust.X, offsetY, w, h);
							strikeSrc = new Rectangle(w * 2, 0, w, h);
							spriteBatch.Draw(strike.Value, strikePos, strikeSrc, hoverColor);
						}
					}
					else {
						checkGrid = checkType == "✓ and  X" ? BossLogUI.xTexture : BossLogUI.checkboxTexture;
						if (isNext && BossChecklist.BossLogConfig.DrawNextMark) {
							checkGrid = checkType == "Strike-through" ? BossLogUI.strikeNTexture : BossLogUI.circleTexture;
						}
					}

					if ((checkType != "Strike-through" || checkGrid == BossLogUI.strikeNTexture) && !selectedBoss.hidden) {
						if (checkGrid != BossLogUI.strikeNTexture) {
							spriteBatch.Draw(BossLogUI.checkboxTexture.Value, pos, Color.White);
						}
						spriteBatch.Draw(checkGrid.Value, pos, Color.White);
					}
				}
			}

			public override int CompareTo(object obj) {
				TableOfContents other = obj as TableOfContents;
				return order.CompareTo(other.order);
			}
		}

		internal class ProgressBar : UIElement
		{
			internal readonly Asset<Texture2D> fullBar = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_ProgressBar", AssetRequestMode.ImmediateLoad);
			internal int[] downedEntries;
			internal int[] totalEntries;
			internal Dictionary<string, int[]> modAllEntries;
			internal bool ModSourceMode = false;

			public ProgressBar() {
				downedEntries = totalEntries = new int[] { 0, 0, 0 };
			}

			public override void Click(UIMouseEvent evt) => ModSourceMode = !ModSourceMode;

			public override void Draw(SpriteBatch spriteBatch) {
				Rectangle inner = GetInnerDimensions().ToRectangle();
				int w = fullBar.Value.Width;
				int h = fullBar.Value.Height;
				int barWidth = inner.Width - 8;

				// Beginning of bar
				Rectangle src = new Rectangle(0, 0, w / 3, h);
				Rectangle pos = new Rectangle(inner.X, inner.Y, w / 3, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				// Center of bar
				src = new Rectangle(w / 3, 0, w / 3, h);
				pos = new Rectangle(inner.X + 6, inner.Y, barWidth, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				// End of bar
				src = new Rectangle(2 * (w / 3), 0, w / 3, h);
				pos = new Rectangle(inner.X + inner.Width - 4, inner.Y, w / 3, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				BossLogConfiguration configs = BossChecklist.BossLogConfig;
				int allDownedEntries = downedEntries[0];
				int allAccountedEntries = totalEntries[0];

				// If OnlyBosses config is disabled, we'll count the MiniBosses and Events to the total count as well
				if (!configs.OnlyShowBossContent) {
					allDownedEntries += downedEntries[1] + downedEntries[2];
					allAccountedEntries += totalEntries[1] + totalEntries[2];
				}

				float percentage = allAccountedEntries == 0 ? 1f : (float)allDownedEntries / (float)allAccountedEntries;
				int meterWidth = (int)(barWidth * percentage); 

				Rectangle meterPos = new Rectangle(inner.X + 4, inner.Y + 4, meterWidth + 2, (int)inner.Height - 8);
				Color bookColor = BossChecklist.BossLogConfig.BossLogColor;
				bookColor.A = 180;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, meterPos, Color.White);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, meterPos, bookColor);

				string percentDisplay = $"{((float)percentage * 100).ToString("#0.0")}%";
				float scale = 0.85f;
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(percentDisplay) * scale;
				Vector2 percentPos = new Vector2(inner.X + (inner.Width / 2) - (stringAdjust.X / 2), inner.Y - stringAdjust.Y);
				Utils.DrawBorderString(spriteBatch, percentDisplay, percentPos, Colors.RarityAmber, scale);

				if (IsMouseHovering) {
					if (ModSourceMode) {
						int[] value = modAllEntries["Terraria"];
						float entryPercentage = (float)((float)value[0] / (float)value[1]) * 100;
						BossUISystem.Instance.UIHoverText = $"Terraria: {value[0]}/{value[1]} ({entryPercentage.ToString("#0.0")}%)";
						foreach (KeyValuePair<string, int[]> entry in modAllEntries) {
							if (entry.Key == "Unknown" || entry.Key == "Terraria")
								continue;

							entryPercentage = (float)((float)entry.Value[0] / (float)entry.Value[1]) * 100;
							BossUISystem.Instance.UIHoverText += $"\n{entry.Key}: {entry.Value[0]}/{entry.Value[1]} ({entryPercentage.ToString("#0.0")}%)";
						}
						if (modAllEntries.ContainsKey("Unknown")) {
							value = modAllEntries["Unknown"];
							entryPercentage = (float)((float)value[0] / (float)value[1]) * 100;
							BossUISystem.Instance.UIHoverText = $"\nUnknown: {value[0]}/{value[1]} ({entryPercentage.ToString("#0.0")}%)";
						}
						BossUISystem.Instance.UIHoverText += $"\n[{Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.EntryCompletion")}]";
					}
					else {
						string total = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Total");
						string bosses = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Bosses");
						string miniBosses = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.MiniBosses");
						string events = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Events");
						BossUISystem.Instance.UIHoverText = $"{total}: {allDownedEntries}/{allAccountedEntries}";
						if (!configs.OnlyShowBossContent) {
							if (configs.FilterMiniBosses != "Hide" || configs.FilterEvents != "Hide") {
								BossUISystem.Instance.UIHoverText += $"\n{bosses}: {downedEntries[0]}/{totalEntries[0]}";
							}
							if (configs.FilterMiniBosses != "Hide") {
								BossUISystem.Instance.UIHoverText += $"\n{miniBosses}: {downedEntries[1]}/{totalEntries[1]}";
							}
							if (configs.FilterEvents != "Hide") {
								BossUISystem.Instance.UIHoverText += $"\n{events}: {downedEntries[2]}/{totalEntries[2]}";
							}
						}
						BossUISystem.Instance.UIHoverText += $"\n[{Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ModCompletion")}]";
					}
				}
			}
		}

		internal class FittedTextPanel : UITextPanel<string>
		{
			readonly string text;
			public FittedTextPanel(string text, float textScale = 1, bool large = false) : base(text, textScale, large) {
				this.text = text;
			}

			const float infoScaleX = 1f;
			const float infoScaleY = 1f;
			public override void Draw(SpriteBatch spriteBatch) {
				Rectangle hitbox = new Rectangle((int)GetInnerDimensions().X, (int)GetInnerDimensions().Y, (int)Width.Pixels, 100);
;
				TextSnippet[] textSnippets = ChatManager.ParseMessage(Language.GetTextValue(text), Color.White).ToArray();
				ChatManager.ConvertNormalSnippets(textSnippets);

				foreach (Vector2 direction in ChatManager.ShadowDirections) {
					ChatManager.DrawColorCodedStringShadow(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft() + direction * 1,
						Color.Black, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), hitbox.Width - (7 * 2), 1);
				}

				ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft(),
					Color.White, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), out _, hitbox.Width - (7 * 2), false);
			}
		}

		internal class SubPageButton : UIImage
		{
			Asset<Texture2D> texture;
			string buttonText;
			readonly SubPage subpageNum;
			readonly SubCategory altPageNum;
			bool isSubPage;

			public SubPageButton(Asset<Texture2D> texture, SubPage type) : base(texture) {
				this.texture = texture;
				buttonText = Language.GetTextValue($"Mods.BossChecklist.BossLog.Terms.{type}");
				subpageNum = type;
				isSubPage = true;
			}

			public SubPageButton(Asset<Texture2D> texture, SubCategory type) : base(texture) {
				this.texture = texture;
				buttonText = Language.GetTextValue($"Mods.BossChecklist.BossLog.Terms.{type}");
				altPageNum = type;
				isSubPage = false;
			}

			public override void Draw(SpriteBatch spriteBatch) {
				int selectedLogPage = BossUISystem.Instance.BossLog.PageNum;
				if (selectedLogPage < 0)
					return;

				base.DrawSelf(spriteBatch);

				Rectangle inner = GetInnerDimensions().ToRectangle();
				if (isSubPage && subpageNum == BossLogUI.SelectedSubPage) {
					Asset<Texture2D> border = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_SubpageSelected", AssetRequestMode.ImmediateLoad);
					spriteBatch.Draw(border.Value, inner, Color.White); // draw a border around the selected subpage
				}

				bool isEvent = subpageNum == SubPage.Records && BossChecklist.bossTracker.SortedBosses[selectedLogPage].type == EntryType.Event; // Event entries should display 'Kill Count' instead of 'Records'
				string translated = Language.GetTextValue(isEvent ? "LegacyInterface.101" : buttonText);
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(translated);
				Vector2 pos = new Vector2(inner.X + ((Width.Pixels - stringAdjust.X) / 2), inner.Y + 5);
				if (isSubPage) {
					spriteBatch.DrawString(FontAssets.MouseText.Value, translated, pos, Color.Gold);
				}

				if (!isSubPage) {
					if (BossLogUI.SelectedSubPage == SubPage.Records) {
						if (altPageNum == BossLogUI.RecordSubCategory) {
							spriteBatch.Draw(texture.Value, inner, Color.LemonChiffon);
							Texture2D pointer = TextureAssets.Cursors[4].Value;
							Vector2 iconPos = new Vector2(inner.Right - pointer.Width / 2 - 2, inner.Bottom - pointer.Height / 2 - 2);
							spriteBatch.Draw(pointer, iconPos, Color.White);
						}
						else if (altPageNum == BossLogUI.CompareState) {
							spriteBatch.Draw(texture.Value, inner, Color.LightCyan);
							Texture2D glass = TextureAssets.Cursors[2].Value;
							Vector2 iconPos = new Vector2(inner.Right - glass.Width / 2 - 2, inner.Bottom - glass.Height / 2 - 2);
							spriteBatch.Draw(glass, iconPos, Color.White);
						}

						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = Language.GetTextValue(buttonText);
							BossUISystem.Instance.UIHoverTextColor = Colors.RarityGreen;
						}
					}
					// No other subpage uses subcategories currently
				}
			}
		}
	}
}
