// UNITY_SHADER_NO_UPGRADE
#ifndef UNITY_SHADER_GRAPH_INCLUDED
#define UNITY_SHADER_GRAPH_INCLUDED

bool IsGammaSpace()
{
    #ifdef UNITY_COLORSPACE_GAMMA
        return true;
    #else
        return false;
    #endif
}

struct Gradient
{
    int type;
    int colorsLength;
    int alphasLength;
    float4 colors[8];
    float2 alphas[8];
};

#endif // UNITY_SHADER_GRAPH_INCLUDED
