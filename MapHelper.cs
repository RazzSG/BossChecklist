﻿using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace BossChecklist
{
	public class MapHelper : ModMapLayer {
		public override void Draw(ref MapOverlayDrawContext context, ref string text) {
			if (BossChecklist.FeatureConfig.ItemMapDrawingEnabled is false || Main.item is null)
				return; // loop through items only if at least one of the configs is enabled

			foreach (Item item in Main.ActiveItems) {
				if (!IsWhitelistedItem(item.type))
					continue; // do not draw items that are inacive or not whitelisted

				Main.instance.LoadItem(item.type); // Items SHOULD already be loaded, but in case it isn't have a backup

				if (context.Draw(TextureAssets.Item[item.type].Value, item.VisualPosition / 16, Color.White, new SpriteFrame(1, 1, 0, 0), 1f, 1.2f, Alignment.Center).IsMouseOver)
					text = item.HoverName; // Display the item's hover name when hovering over the icon
			}
		}

		public static bool IsWhitelistedItem(int type) {
			if (ItemID.Sets.BossBag[type]) {
				return BossChecklist.FeatureConfig.TreasureBagsOnMap;
			}
			else if (type == ItemID.ShadowScale || type == ItemID.TissueSample) {
				return BossChecklist.FeatureConfig.ScalesOnMap;
			}
			else if (RecipeGroup.recipeGroups[RecipeGroupID.Fragment].ValidItems.Contains(type)) {
				return BossChecklist.FeatureConfig.FragmentsOnMap;
			}
			return false;
		}
	}
}
