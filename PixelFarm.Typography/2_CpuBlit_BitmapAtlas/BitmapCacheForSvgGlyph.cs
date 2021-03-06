﻿//MIT, 2019-present, WinterDev 

using System;
using System.Collections.Generic;
using Typography.OpenFont;

namespace PixelFarm.CpuBlit.BitmapAtlas
{
    public delegate MemBitmap SvgBmpBuilderFunc(System.Text.StringBuilder stbuilder);

    class BitmapCacheForSvgGlyph
    {
        Typeface _currentTypeface;
        GlyphBitmapList _bitmapList;
        Dictionary<Typeface, GlyphBitmapList> _cacheGlyphBmpLists = new Dictionary<Typeface, GlyphBitmapList>();

        SvgBmpBuilderFunc _svgBmpBuilderFunc;
        public void SetSvgBmpBuilderFunc(SvgBmpBuilderFunc svgBmpBuilder)
        {
            _svgBmpBuilderFunc = svgBmpBuilder;

        }
        public void SetCurrentTypeface(Typeface typeface)
        {
            _currentTypeface = typeface;
            if (_cacheGlyphBmpLists.TryGetValue(typeface, out _bitmapList))
            {
                return;
            }
            //TODO: you can scale down to proper img size
            //or create a single texture atlas. 
            //if not create a new one
            _bitmapList = new GlyphBitmapList();
            _cacheGlyphBmpLists.Add(typeface, _bitmapList);
            int glyphCount = typeface.GlyphCount;

            System.Text.StringBuilder stbuilder = new System.Text.StringBuilder();
            for (ushort i = 0; i < glyphCount; ++i)
            {
                stbuilder.Length = 0;//reset
                Glyph glyph = typeface.GetGlyph(i);
                typeface.ReadSvgContent(glyph, stbuilder);
                //create bitmap from svg  

                GlyphBitmap glyphBitmap = new GlyphBitmap();
                glyphBitmap.Width = glyph.MaxX - glyph.MinX;
                glyphBitmap.Height = glyph.MaxY - glyph.MinY;

                if (glyphBitmap.Width == 0 || glyphBitmap.Height == 0)
                {
                    continue;
                }

                glyphBitmap.Bitmap = _svgBmpBuilderFunc(stbuilder);// ParseAndRenderSvg(stbuilder, vgDocHost);
                if (glyphBitmap.Bitmap == null)
                {
                    continue;
                }

                //
                //MemBitmapExtensions.SaveImage(glyphBitmap.Bitmap, "testGlyphBmp_" + i + ".png");
                _bitmapList.RegisterBitmap(glyph.GlyphIndex, glyphBitmap);
            }

        }
        public GlyphBitmap GetGlyphBitmap(ushort glyphIndex)
        {
            _bitmapList.TryGetBitmap(glyphIndex, out GlyphBitmap found);
            return found;
        }
    }

}