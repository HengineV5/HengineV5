#version 450

layout(location = 0) in vec2 v_texCoord;
layout(location = 1) in vec3 v_pos;
layout(location = 2) flat in uint v_inverted;

layout(location = 0) out vec4 color;

layout(binding = 1) uniform sampler2D u_TextureMap;

layout(binding = 2) uniform GuiStateBufferObject {
    int totalStates;
    int state;
    bool bezier;
} u_Ubo;

void main() {
    vec2 scaledUV = vec2(v_texCoord.x / float(u_Ubo.totalStates) + u_Ubo.state / float(u_Ubo.totalStates), v_texCoord.y);
    vec3 col = vec3(texture(u_TextureMap, scaledUV));

    color = (u_Ubo.bezier && (v_inverted == 1 ? v_texCoord.y > (v_texCoord.x * v_texCoord.x) : v_texCoord.y < (v_texCoord.x * v_texCoord.x))) ? vec4(0) : vec4(col, 1);
    //color = (u_Ubo.bezier && v_texCoord.y < (v_texCoord.x * v_texCoord.x)) ? vec4(0) : vec4(vec2(round(v_texCoord.x * 10) / 10, round(v_texCoord.y * 10) / 10), 0.0, 1.0);
    //color = (u_Ubo.bezier && v_texCoord.y < (v_texCoord.x * v_texCoord.x)) ? vec4(0) : texture(u_TextureMap, scaledUV);
    //color = texture(u_TextureMap, scaledUV);
    //color = vec4(vec2(round(v_texCoord.x * 10) / 10, round(v_texCoord.y * 10) / 10), 0.0, 1.0);
}