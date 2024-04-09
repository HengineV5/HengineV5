#version 450

#extension GL_KHR_vulkan_glsl : enable

layout(location = 0) in vec4 position;
layout(location = 1) in vec2 texCoord;

layout(location = 0) out vec2 v_texCoord;
layout(location = 1) out vec3 v_pos;

layout(binding = 0) uniform UniformBufferObject {
    mat4 proj;
    vec2 screenSize;
} u_Ubo;

vec2 size = vec2(1024, 1024);

void main() {
    vec2 sizeToPercent = vec2(size.x / u_Ubo.screenSize.x, size.y / u_Ubo.screenSize.y);
    
    vec2 guiPos = vec2((position.x / size.x) * sizeToPercent.x + position.y, (position.z / size.y) * sizeToPercent.y + position.w);

    vec2 screenPos = guiPos * 2 - 1;
    vec3 worldPos = vec3(screenPos, 1.0);
    //worldPos.y = -worldPos.y;
    worldPos.z = -worldPos.z;

    vec4 pos = u_Ubo.proj * vec4(worldPos, 1.0);
    gl_Position = pos;

    v_pos = vec3(0.0, 0.0, 0.0);
    v_texCoord = vec2(pos.xy);
}