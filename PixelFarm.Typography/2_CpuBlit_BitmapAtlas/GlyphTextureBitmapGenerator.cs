﻿//MIT, 2017-present, WinterDev
using System;
using System.Collections.Generic;

using Typography.OpenFont;
using Typography.OpenFont.Extensions;
using Typography.Contours;

namespace PixelFarm.CpuBlit.BitmapAtlas
{
    public class GlyphTextureBuildDetail
    {
        public ScriptLang ScriptLang;
        public char[] OnlySelectedGlyphIndices;
        public HintTechnique HintTechnique;

    }

    public struct GlyphTextureBitmapGenerator
    {

        public delegate void OnEachGlyph(BitmapAtlasItemSource glyphImage);

        /// <summary>
        /// version of msdf generator
        /// </summary>
        public ushort MsdfGenVersion { get; set; }

        OnEachGlyph _onEachGlyphDel;
        public SimpleBitmapAtlasBuilder CreateTextureFontFromBuildDetail(
            Typeface typeface, float sizeInPoint,
            TextureKind textureKind,
            GlyphTextureBuildDetail[] details,
            OnEachGlyph onEachGlyphDel = null)
        {
            _onEachGlyphDel = onEachGlyphDel;
            //-------------------------------------------------------------
            var atlasBuilder = new SimpleBitmapAtlasBuilder();

            atlasBuilder.SetAtlasInfo(textureKind, sizeInPoint);
            //-------------------------------------------------------------  
            int j = details.Length;
            for (int i = 0; i < j; ++i)
            {
                GlyphTextureBuildDetail detail = details[i];
                if (!detail.ScriptLang.IsEmpty())
                {
                    //skip those script lang=null
                    //2. find associated glyph index base on input script langs
                    List<ushort> outputGlyphIndexList = new List<ushort>();
                    typeface.CollectAllAssociateGlyphIndex(outputGlyphIndexList, detail.ScriptLang);
                    CreateTextureFontFromGlyphIndices(typeface,
                        sizeInPoint,
                        detail.HintTechnique,
                        atlasBuilder,
                        GetUniqueGlyphIndexList(outputGlyphIndexList)
                        );
                }
            }
            for (int i = 0; i < j; ++i)
            {
                GlyphTextureBuildDetail detail = details[i];
                if (detail.OnlySelectedGlyphIndices != null)
                {
                    //skip those script lang=null
                    //2. find associated glyph index base on input script langs

                    CreateTextureFontFromGlyphIndices(typeface,
                        sizeInPoint,
                        detail.HintTechnique,
                        atlasBuilder,
                        detail.OnlySelectedGlyphIndices
                        );
                }
            }

            _onEachGlyphDel = null;//reset
            return atlasBuilder;
        }

        public SimpleBitmapAtlasBuilder CreateTextureFontFromInputChars(
            Typeface typeface, float sizeInPoint,
            TextureKind textureKind,
            char[] chars,
            OnEachGlyph onEachGlyphDel = null)
        {

            _onEachGlyphDel = onEachGlyphDel;
            //convert input chars into glyphIndex
            List<ushort> glyphIndices = new List<ushort>(chars.Length);
            int i = 0;
            foreach (char ch in chars)
            {
                glyphIndices.Add(typeface.GetGlyphIndex(ch));
                i++;
            }
            //-------------------------------------------------------------
            var atlasBuilder = new SimpleBitmapAtlasBuilder();
            atlasBuilder.SetAtlasInfo(textureKind, sizeInPoint);
            //------------------------------------------------------------- 
            //we can specfic subset with special setting for each set 
            CreateTextureFontFromGlyphIndices(typeface, sizeInPoint,
                HintTechnique.TrueTypeInstruction_VerticalOnly, atlasBuilder, GetUniqueGlyphIndexList(glyphIndices));

            _onEachGlyphDel = null;//reset                
            return atlasBuilder;
        }
        void CreateTextureFontFromGlyphIndices(
              Typeface typeface,
              float sizeInPoint,
              HintTechnique hintTechnique,
              SimpleBitmapAtlasBuilder atlasBuilder,
              char[] chars)
        {
            int j = chars.Length;
            ushort[] glyphIndices = new ushort[j];
            for (int i = 0; i < j; ++i)
            {
                glyphIndices[i] = typeface.GetGlyphIndex(chars[i]);
            }

            CreateTextureFontFromGlyphIndices(typeface, sizeInPoint, hintTechnique, atlasBuilder, glyphIndices);
        }
        void CreateTextureFontFromGlyphIndices(
              Typeface typeface,
              float sizeInPoint,
              HintTechnique hintTechnique,
              SimpleBitmapAtlasBuilder atlasBuilder,
              ushort[] glyphIndices)
        {

            //sample: create sample msdf texture 
            //-------------------------------------------------------------
            var outlineBuilder = new GlyphOutlineBuilder(typeface);
            outlineBuilder.SetHintTechnique(hintTechnique);
            //
            if (atlasBuilder.TextureKind == TextureKind.Msdf)
            {

                float pxscale = typeface.CalculateScaleToPixelFromPointSize(sizeInPoint);
                var msdfGenParams = new Msdfgen.MsdfGenParams();
                int j = glyphIndices.Length;

                if (MsdfGenVersion == 3)
                {
                    Msdfgen.MsdfGen3 gen3 = new Msdfgen.MsdfGen3();

                    for (int i = 0; i < j; ++i)
                    {
                        ushort gindex = glyphIndices[i];
                        //create picture with unscaled version set scale=-1
                        //(we will create glyph contours and analyze them)

                        outlineBuilder.BuildFromGlyphIndex(gindex, -1);

                        var glyphToVxs = new GlyphTranslatorToVxs();
                        outlineBuilder.ReadShapes(glyphToVxs);
                        using (Tools.BorrowVxs(out var vxs))
                        {
                            glyphToVxs.WriteUnFlattenOutput(vxs, pxscale);
                            BitmapAtlasItemSource glyphImg = gen3.GenerateMsdfTexture(vxs);
                            glyphImg.UniqueInt16Name = gindex;
                            _onEachGlyphDel?.Invoke(glyphImg);
                            //

                            atlasBuilder.AddItemSource(glyphImg);
                        }

                    }
                }
                else
                {

                    Msdfgen.MsdfGen3 gen3 = new Msdfgen.MsdfGen3();
                    for (int i = 0; i < j; ++i)
                    {
                        ushort gindex = glyphIndices[i];
                        //create picture with unscaled version set scale=-1
                        //(we will create glyph contours and analyze them)
                        outlineBuilder.BuildFromGlyphIndex(gindex, -1);

                        var glyphToVxs = new GlyphTranslatorToVxs();
                        outlineBuilder.ReadShapes(glyphToVxs);

                        using (Tools.BorrowVxs(out var vxs))
                        {
                            glyphToVxs.WriteUnFlattenOutput(vxs, pxscale);
                            BitmapAtlasItemSource glyphImg = gen3.GenerateMsdfTexture(vxs);
                            glyphImg.UniqueInt16Name = gindex;
                            _onEachGlyphDel?.Invoke(glyphImg);

                            atlasBuilder.AddItemSource(glyphImg);
                        }
                    }
                }
            }
            else
            {
                AggGlyphTextureGen aggTextureGen = new AggGlyphTextureGen();
                aggTextureGen.TextureKind = atlasBuilder.TextureKind;
                //create reusable agg painter***

                //assume each glyph size= 2 * line height
                //TODO: review here again...
                int tmpMemBmpHeight = (int)(2 * typeface.CalculateRecommendLineSpacing() * typeface.CalculateScaleToPixelFromPointSize(sizeInPoint));
                //create glyph img    
                using (PixelFarm.CpuBlit.MemBitmap tmpMemBmp = new PixelFarm.CpuBlit.MemBitmap(tmpMemBmpHeight, tmpMemBmpHeight)) //square
                {
                    //draw a glyph into tmpMemBmp and then copy to a GlyphImage                     
                    aggTextureGen.Painter = PixelFarm.CpuBlit.AggPainter.Create(tmpMemBmp);
#if DEBUG
                    tmpMemBmp._dbugNote = "CreateGlyphImage()";
#endif

                    int j = glyphIndices.Length;
                    for (int i = 0; i < j; ++i)
                    {
                        //build glyph
                        ushort gindex = glyphIndices[i];
                        outlineBuilder.BuildFromGlyphIndex(gindex, sizeInPoint);

                        BitmapAtlasItemSource glyphImg = aggTextureGen.CreateAtlasItem(outlineBuilder, 1);

                        glyphImg.UniqueInt16Name = gindex;
                        _onEachGlyphDel?.Invoke(glyphImg);
                        atlasBuilder.AddItemSource(glyphImg);
                    }
                }
            }
        }

        static ushort[] GetUniqueGlyphIndexList(List<ushort> inputGlyphIndexList)
        {
            Dictionary<ushort, bool> uniqueGlyphIndices = new Dictionary<ushort, bool>(inputGlyphIndexList.Count);
            foreach (ushort glyphIndex in inputGlyphIndexList)
            {
                if (!uniqueGlyphIndices.ContainsKey(glyphIndex))
                {
                    uniqueGlyphIndices.Add(glyphIndex, true);
                }
            }
            //
            ushort[] uniqueGlyphIndexArray = new ushort[uniqueGlyphIndices.Count];
            int i = 0;
            foreach (ushort glyphIndex in uniqueGlyphIndices.Keys)
            {
                uniqueGlyphIndexArray[i] = glyphIndex;
                i++;
            }
            return uniqueGlyphIndexArray;
        }
    }
}