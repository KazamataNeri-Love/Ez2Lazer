#ifndef SINEWAVE_FS
#define SINEWAVE_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

// 复用与透视着色器相同的 uniform 块结构，以便直接替换 private perspectiveShader
layout(std140, set = 0, binding = 0) uniform m_PerspectiveParameters
{
    mediump vec2 g_Scale;           // x = amplitude, y = frequency
    mediump float g_VerticalOffset; // phase
    mediump float _pad;
};

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    highp float t_x = (v_TexCoord.x - v_TexRect.x) / v_TexRect.z;
    highp float t_y = (v_TexCoord.y - v_TexRect.y) / v_TexRect.w;

    // Y_normalized: 0 = 底部(判定线), 1 = 顶部
    highp float y_mapped = t_y;

    // 正弦波 X 偏移
    // g_Scale.x = 归一化振幅 (0~1, 占列宽的比例)
    // g_Scale.y = 频率
    // g_VerticalOffset = 相位
    highp float offset = g_Scale.x
        * sin(6.2831853 * g_Scale.y * y_mapped + g_VerticalOffset);

    // clamp 而非 clip — 保留图像不裁边
    highp float localX_src = clamp(t_x + offset, 0.0, 1.0);

    highp vec2 sampleCoord = vec2(
        v_TexRect.x + localX_src * v_TexRect.z,
        v_TexRect.y + t_y * v_TexRect.w);

    vec2 wrappedCoord = wrap(sampleCoord, v_TexRect);
    o_Colour = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9), wrappedCoord);
}

#endif
