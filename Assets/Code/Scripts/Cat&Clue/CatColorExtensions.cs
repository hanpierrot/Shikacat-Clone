using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CatColorExtensions
{
    public static string ToSkinName(this CatColor color)
    {
        switch (color)
        {
            case CatColor.Orange: return "cam";
            case CatColor.Red: return "do";
            case CatColor.Pink: return "hong";
            case CatColor.Purple: return "timxanh";
            case CatColor.White: return "trang";
            case CatColor.Yellow: return "vang";
            case CatColor.Blue: return "xanhduong";
            case CatColor.Green: return "xanhla";
            case CatColor.Teal: return "xanhngoc";
            case CatColor.Neon: return "xanhnhat";
            default: return "default";
        }
    }
}
