// This function maps a 2D equirectangular texture to a cubemap asset and returns the cubemap
// Parameters:
//   tex: the 2D equirectangular texture to map
//   mip: the mipmap level to sample from (default 0)
//   roughness: the roughness value to use for the cubemap (default 0)
// Returns:
//   the cubemap asset
void GetCubemapFromEquirectangular(Texture2D tex, out Cube cubemap)
{
    // Create a temporary render texture to render the cubemap faces to
    RenderTexture cubemapRT = RenderTexture.GetTemporary(tex.width, tex.width, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
    cubemapRT.dimension = TextureDimension.Cube;

    // Create a material to render the cubemap faces with
    Material cubemapMat = new Material(Shader.Find("Hidden/BlitCubemap"));

    // Set the cubemap material's texture and roughness values
    cubemapMat.SetTexture("_MainTex", tex);
    cubemapMat.SetFloat("_Roughness", roughness);

    // Render each cubemap face to the temporary render texture
    for (int i = 0; i < 6; i++)
    {
        cubemapMat.SetInt("_Face", i);
        Graphics.SetRenderTarget(cubemapRT, 0, (CubemapFace)i);
        GL.Clear(true, true, Color.clear);
        Graphics.Blit(null, cubemapMat);
    }

    // Create a new cubemap asset from the temporary render texture
    Cube cubemap = new Cube(tex.width, TextureFormat.RGBA32, true);
    cubemap.SetPixels(cubemapRT.GetPixels(CubemapFace.PositiveX), CubemapFace.PositiveX);
    cubemap.SetPixels(cubemapRT.GetPixels(CubemapFace.NegativeX), CubemapFace.NegativeX);
    cubemap.SetPixels(cubemapRT.GetPixels(CubemapFace.PositiveY), CubemapFace.PositiveY);
    cubemap.SetPixels(cubemapRT.GetPixels(CubemapFace.NegativeY), CubemapFace.NegativeY);
    cubemap.SetPixels(cubemapRT.GetPixels(CubemapFace.PositiveZ), CubemapFace.PositiveZ);
    cubemap.SetPixels(cubemapRT.GetPixels(CubemapFace.NegativeZ), CubemapFace.NegativeZ);
    cubemap.Apply(false);

    // Release the temporary render texture and material
    RenderTexture.ReleaseTemporary(cubemapRT);
    UnityEngine.Object.DestroyImmediate(cubemapMat);

}
