// หมอกจำกัดระยะมอง (port จาก patchFog ใน js/main.js)
// มืดลงตามระยะ "จากตัวละคร" บนระนาบ XZ ไม่ใช่ระยะจากกล้อง
//   _SkeldFogCenter.xz = ตำแหน่งผู้เล่น (world space)
//   _SkeldFogRange     = (inner, outer, dark, amount)
// ถ้ายังไม่มีใครเซ็ต global ค่าจะเป็น 0 → amount = 0 → ไม่มีหมอก
#ifndef SKELD_FOG_INCLUDED
#define SKELD_FOG_INCLUDED

float4 _SkeldFogCenter;
float4 _SkeldFogRange;

float SkeldFogFactor(float3 worldPos)
{
    float d = distance(worldPos.xz, _SkeldFogCenter.xz);
    float vis = 1.0 - smoothstep(_SkeldFogRange.x, _SkeldFogRange.y, d);
    return lerp(1.0, lerp(_SkeldFogRange.z, 1.0, vis), _SkeldFogRange.w);
}

#endif
