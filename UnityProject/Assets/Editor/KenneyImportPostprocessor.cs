using UnityEditor;
using UnityEngine;

namespace SkeldMap.EditorTools
{
    // ตั้งค่านำเข้าของชุด Kenney อัตโนมัติ
    //  • Characters / MedicalAids: เท็กซ์เจอร์เป็น "พาเลตต์สี" (ช่องสีเล็ก ๆ) → Point filter ไม่มี mipmap กันสีไหลข้ามช่อง
    //  • SpaceKit: เมชพื้นเอา "ลายกระเบื้อง" จากพาเลตต์ขนาด ~120px มาแปะ → ต้องมี mipmap + Trilinear + Anisotropic
    //    ไม่งั้นพอกระเบื้องบนจอเล็กลง เส้นร่องจะกะพริบ/คืบเวลากล้องขยับ (Point filter ไม่มี mip = aliasing)
    //    ส่วนสีพาเลตต์ของชิ้นอื่นใช้พื้นที่ UV เล็กมาก ระดับ mip ที่เลือกจึงต่ำ ไม่ทำให้สีเลอะ
    //  • โมเดล SpaceKit ต้องเปิด Read/Write เพราะโค้ดรวมเมชหลายพันชิ้นเป็นก้อนเดียวตอนเริ่มเกม
    public sealed class KenneyImportPostprocessor : AssetPostprocessor
    {
        static bool IsPaletteTexture(string p) =>
            p.Contains("Resources/Characters/Textures/") || p.Contains("Resources/MedicalAids/Textures/");

        void OnPreprocessTexture()
        {
            var imp = (TextureImporter)assetImporter;
            if (IsPaletteTexture(assetPath))
            {
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Point;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.npotScale = TextureImporterNPOTScale.None;
            }
            else if (assetPath.Contains("Resources/SpaceKit/Textures/"))
            {
                imp.mipmapEnabled = true;
                imp.filterMode = FilterMode.Trilinear;
                imp.anisoLevel = 8;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.npotScale = TextureImporterNPOTScale.None;
            }
        }

        void OnPreprocessModel()
        {
            if (!assetPath.Contains("Resources/SpaceKit/")) return;
            ((ModelImporter)assetImporter).isReadable = true;
        }
    }
}
