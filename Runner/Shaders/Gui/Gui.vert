#version 450

#extension GL_KHR_vulkan_glsl : enable

layout(location = 0) in vec4 position;
layout(location = 1) in vec2 texCoord;

layout(location = 0) out vec2 v_texCoord;
layout(location = 1) out vec3 v_pos;

layout(binding = 0) uniform UniformBufferObject {
    mat4 proj;
    vec2 screenSize;

    vec4 position;
    vec4 size;
} u_Ubo;

vec2 size = vec2(1024, 1024);

void main() {
    vec2 relativePosition = vec2(position.x * u_Ubo.size.x + u_Ubo.position.x + position.y, position.z * u_Ubo.size.z + u_Ubo.position.z + position.w);

    vec2 guiPos = relativePosition / u_Ubo.screenSize + vec2(position.x * u_Ubo.size.y, position.z * u_Ubo.size.w);

    vec2 screenPos = guiPos * 2 - 1; // Convert from [0-1] -> [-1, 1]
    vec3 worldPos = vec3(screenPos, 1.0);
    //worldPos.y = -worldPos.y;
    worldPos.z = -worldPos.z;

    vec4 pos = u_Ubo.proj * vec4(worldPos, 1.0);
    gl_Position = pos;

    v_pos = vec3(0.0, 0.0, 0.0);

    vec2 screenTexCoor = guiPos * u_Ubo.screenSize;
    v_texCoord = vec2(texCoord);
}