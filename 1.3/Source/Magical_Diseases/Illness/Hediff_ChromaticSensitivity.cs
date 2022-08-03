using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Magical_Diseases.Illness
{
	public class Hediff_ChromaticSensitivity : HediffWithComps
	{
		private float maxChangePerIngredient;

		#region Properties

		public HediffDef_ChromaticSensitivity Def => def as HediffDef_ChromaticSensitivity;

		#endregion Properties

		#region Overrides

		public override void PostMake()
		{
			base.PostMake();
			maxChangePerIngredient = 0.1f;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref maxChangePerIngredient, "maxChangePerIngredient");
		}

		#endregion Overrides

		#region Helpers

		private Color MoveColorsCloser(Color currentColor, Color targetColor)
		{
			Log.Warning($"R {currentColor.r}->{targetColor.r} Max diff: {maxChangePerIngredient}");
			Log.Warning($"G {currentColor.g}->{targetColor.g} Max diff: {maxChangePerIngredient}");
			Log.Warning($"B {currentColor.b}->{targetColor.b} Max diff: {maxChangePerIngredient}");
			return new Color(
				Mathf.MoveTowards(currentColor.r, targetColor.r, maxChangePerIngredient),
				Mathf.MoveTowards(currentColor.g, targetColor.g, maxChangePerIngredient),
				Mathf.MoveTowards(currentColor.b, targetColor.b, maxChangePerIngredient));
		}

		private Color? MoveTowardsColorFromFood(Thing food, Color startingColor)
		{
			var comp = food.TryGetComp<CompIngredients>();
			Log.Warning("Comp has " + (comp?.ingredients?.Count ?? 0).ToString() + " ingredients");
			if (comp == null || (comp.ingredients?.Count ?? 0) <= 0)
			{
				return food.Stuff?.stuffProps?.color is Color stuffColor
					? MoveColorsCloser(startingColor, stuffColor)
					: MoveColorsCloser(startingColor,
						TextureAtlasHelper.MakeReadableTextureInstance((Texture2D)food.Graphic.MatSingle.mainTexture)
							.GetDominantColor());
			}

			var newCol = comp.ingredients.Aggregate(startingColor, MoveColourTowardsIngredientColour);
			Log.Warning($"New col {newCol.r} {newCol.g} {newCol.b}");
			return newCol;
		}

		/**
 * Forced Color
 * StuffProps
 * GraphicData.ColorTwo
 * GraphicData.Color
 * DominantColor
 */
		private void testcol(Texture2D tex)
		{
			Log.Warning($"Tex size: {tex.height} x {tex.width}");
			Log.Warning($"Tex Dom: {tex.GetDominantColor()}");
			Log.Warning($"Tex Dom without outline: {tex.GetDominantColor(minBrightness: 0.21f)}");
			foreach (var color32 in tex.GetPixels32().GroupBy(p => $"{p.r},{p.g},{p.b}"))
			{
				if (color32.Count() >= 99)
					Log.Message($"color32 {color32.Key}: {color32.Count()}");
			}

			Log.Warning($"common col: {tex.GetPixels32().GroupBy(p => $"{p.r},{p.g},{p.b}").MaxBy(g => g.Count())}");
			foreach (var color in tex.GetPixels().GroupBy(p => $"{p.r},{p.g},{p.b}"))
			{
				if (color.Count() >= 99)
					Log.Message($"color {color.Key}: {color.Count()}");
			}
		}

		/**
		 * List of colors to exclude from possible consideration
		 * Used to avoid the box counting as the dominant color for things like Rice, Corn etc.
		 */
		private static readonly HashSet<int> ExcludedColors = new HashSet<int>()
		{
			140 | 101 << 8 | 49 << 16, // Raw Food Boxes
			0 // Outline; pure black
		};

		private Color? ExtractBestColor(Texture2D texture2D)
		{
			Color32? bestColor = null;
			int bestKey = 0;
			int commonality = 0;
			foreach (var countedColor in texture2D.GetPixels32()
				         .Where(p => p.a > 5) // Ignore anything that's basically transparent 
				         .GroupBy(p => p.r | p.g << 8 | p.b << 16))
			{
				var newCommonality = countedColor.Count();
				if (newCommonality <= commonality && !ExcludedColors.Contains(bestKey)) continue;
				bestColor = countedColor.First();
				bestKey = countedColor.Key;
				commonality = newCommonality;
#if DEBUG
				Log.Message($"Found {commonality} pixels of color: {bestColor}");
#endif
			}

			return bestColor is Color32 chosen ? new Color32(chosen.r, chosen.g, chosen.b, byte.MaxValue) : (Color?)null;
		}

		private Color MoveColourTowardsIngredientColour(Color color, ThingDef ingredient)
		{
#if DEBUG
			Log.Warning($"Looking at {ingredient.defName}");
			var debugPng = "D:\\Modding\\" + ingredient.defName + ".png";
			if (!File.Exists(debugPng))
			{
				TextureAtlasHelper.WriteDebugPNG((Texture2D)ingredient.graphic.MatSingle.mainTexture, debugPng);
			}
#endif

			return ingredient.stuffProps?.color is Color stuffColor
				? MoveColorsCloser(color, stuffColor)
				: ExtractBestColor(TextureAtlasHelper.MakeReadableTextureInstance((Texture2D)ingredient.graphic.MatSingle.mainTexture)) is Color newColor
					? MoveColorsCloser(color, newColor)
					: color;
		}

		private Color? GetFromTexture(Texture2D texture)
		{
			return texture == null
				? (Color?)null
				: Texture2D
					.CreateExternalTexture(texture.width, texture.height, texture.format, true, true,
						texture.GetNativeTexturePtr())
					.GetDominantColor();
		}

		private Color MoveColourTowardsIngredientColourOld(Color color, ThingDef ingredient)
		{
			// var stopwatch = Stopwatch.StartNew();
			// var elapsed = stopwatch.ElapsedMilliseconds;
			//
			//
			//
			//
			//
			// // Log.Warning($"Buildable {ThingDefOf.Wall.GetColorForStuff(ingredient)} {stopwatch.ElapsedMilliseconds - elapsed}");
			// Log.Warning($"Stuff {ingredient.stuffProps?.color} {stopwatch.ElapsedMilliseconds - elapsed}");
			// Log.Warning($"Icon {ingredient.uiIconColor}");
			// Log.Warning($"Mat {ingredient.graphic.MatSingle.color}");
			// Log.Warning($"Mat2 {ingredient.graphic.MatSingle.GetColorTwo()}");
			// elapsed = stopwatch.ElapsedMilliseconds;
			// // var texture2D = ingredient.graphic.MatSingle.GetMaskTexture();
			// Log.Warning(ingredient.graphicData.texPath);
			// var texture2D = ContentFinder<Texture2D>.Get(ingredient.graphic.path);
			//
			// Log.Warning($"Content finder {stopwatch.ElapsedMilliseconds - elapsed}");
			// elapsed = stopwatch.ElapsedMilliseconds;
			//
			//
			// var t = ingredient.graphic.MatSingle.mainTexture;
			// var tex = t as Texture2D;
			// try { Log.Warning($"cast {tex.GetDominantColor()} {stopwatch.ElapsedMilliseconds - elapsed}"); } catch {};
			// try{Log.Warning($"cast {(Texture2D.CreateExternalTexture(tex.width, tex.height, tex.format, true, true, tex.GetNativeTexturePtr())).GetDominantColor()} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			//
			// var thing = ThingMaker.MakeThing(ingredient);
			// try{Log.Warning($"found a{thing.Graphic.Color} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			// try{Log.Warning($"found a2{thing.Graphic.ColorTwo} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			// try{Log.Warning($"found a3{thing.DrawColor} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			// try{Log.Warning($"found a4{thing.DrawColorTwo} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			// try{Log.Warning($"found a5{thing.DefaultGraphic.color} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			// try{Log.Warning($"found a6{thing.DefaultGraphic.colorTwo} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			// try{Log.Warning($"found a7{thing.Graphic.MatSingle.color} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			// try{Log.Warning($"found a8{thing.Graphic.MatSingle.GetMaskTexture().GetDominantColor()} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			//
			//
			// try{Log.Warning($"found ddd{ingredient} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			// try{Log.Warning($"found {texture2D?.GetDominantColor()} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			//
			// elapsed = stopwatch.ElapsedMilliseconds;
			// try{Log.Warning($"middle pixel {texture2D?.GetPixel(texture2D.width / 2, texture2D.height / 2)} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			// elapsed = stopwatch.ElapsedMilliseconds;
			// try{Log.Warning($"Mat 3{ingredient.graphic.MatSingle.GetMaskTexture()?.GetDominantColor()} {stopwatch.ElapsedMilliseconds - elapsed}");} catch {};
			//
			var g = ingredient.graphic;
			GlobalTextureAtlasManager.DumpStaticAtlases("D:\\Modding");
			TextureAtlasHelper.WriteDebugPNG((Texture2D)g.MatSingle.mainTexture,
				"D:\\Modding\\" + ingredient.defName + ".png");
			Log.Warning(
				$" Atlas {TextureAtlasHelper.MakeReadableTextureInstance((Texture2D)g.MatSingle.mainTexture).GetDominantColor()}");

			StaticTextureAtlasTile tile;
			if (GlobalTextureAtlasManager.TryGetStaticTile(TextureAtlasGroup.Item, (Texture2D)g.MatSingle.mainTexture,
				    out tile))
			{
				Log.Warning($"Atlas Tex {tile.atlas.ColorTexture.GetDominantColor()}");


				MaterialRequest request;
				if (MaterialPool.TryGetRequestForMat(g.MatSingle, out request))
				{
					Log.Error(
						"Tried getting texture atlas replacement info for a material that was not created by MaterialPool!");
					return ingredient.graphicData.colorTwo == Color.white
						? MoveColorsCloser(color, GetFromTexture(ingredient.graphic.MatSingle.GetMaskTexture()) ?? color)
						: MoveColorsCloser(color, ingredient.graphicData.colorTwo);
				}

				var uvs = new Vector2[4];
				Printer_Plane.GetUVs(tile.uvRect, out uvs[0], out uvs[1], out uvs[2], out uvs[3], false);
				Log.Warning($"UVs {uvs}");
			}

			return ingredient.graphicData.colorTwo == Color.white
				? MoveColorsCloser(color, GetFromTexture(ingredient.graphic.MatSingle.GetMaskTexture()) ?? color)
				: MoveColorsCloser(color, ingredient.graphicData.colorTwo);
		}

		#endregion Helpers

		public void FoodIngested(Thing food, Color? forcedColor)
		{
			Log.Warning("Food Ingested");
			var startingColor = pawn.story.SkinColor;
			pawn.story.skinColorOverride = forcedColor.HasValue
				? MoveColorsCloser(startingColor, forcedColor.Value)
				: MoveTowardsColorFromFood(food, startingColor) ?? startingColor;

			Log.Warning("Colour changed from (" + startingColor.r + ", " + startingColor.b + ", " + startingColor.g +
			            ") to (" + pawn.story.skinColorOverride.Value.r + ", " + pawn.story.skinColorOverride.Value.b + ", " +
			            pawn.story.skinColorOverride.Value.g);
			pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
			PortraitsCache.SetDirty(pawn);

			if (pawn.Awake() && pawn.story.SkinColor.b > 0.9 && startingColor.b < pawn.story.SkinColor.b)
				MoteMaker.ThrowText(pawn.DrawPos, pawn.Map,
					"TextMote_ChromaticSensitivity_FeelingBlue".Translate(), 6.5f);
		}
	}
}