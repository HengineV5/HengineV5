#version 450

layout(location = 0) in vec2 v_texCoord;
layout(location = 1) in vec3 v_pos;

layout(location = 0) out vec4 color;

layout(binding = 1) uniform sampler2D u_TextureMap;

layout(binding = 2) uniform UniformBufferObject {
    int totalStates;
    int state;
} u_Ubo;

void main() {
    vec2 scaledUV = vec2(v_texCoord.x / float(u_Ubo.totalStates) + u_Ubo.state / float(u_Ubo.totalStates), v_texCoord.y);

    color = texture(u_TextureMap, scaledUV);
    //color = vec4(vec2(round(v_texCoord.x * 10) / 10, round(v_texCoord.y * 10) / 10), 0.0, 1.0);
}