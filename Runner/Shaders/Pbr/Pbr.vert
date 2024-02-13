#version 450

#extension GL_KHR_vulkan_glsl : enable

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec2 texCoord;
layout(location = 3) in vec3 tangent;

layout(location = 0) out vec2 v_texCoord;
layout(location = 1) out vec3 v_normal;
layout(location = 2) out vec3 v_pos;
layout(location = 3) out vec3 v_viewPos;

layout(binding = 0) uniform UniformBufferObject {
    mat4 translation;
    mat4 rotation;
    mat4 scale;
    mat4 view;
    mat4 proj;
    vec3 viewPos;
} u_Ubo;

void main() {
    mat4 model = u_Ubo.translation * u_Ubo.rotation * u_Ubo.scale;
    gl_Position = u_Ubo.proj * u_Ubo.view * model * vec4(position, 1.0);

    mat3 normalMatrix = transpose(inverse(mat3(model)));
    vec3 T = normalize(normalMatrix * tangent);
    vec3 N = normalize(normalMatrix * normal);
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);

    v_pos = vec3(model * vec4(position, 1));
    v_viewPos = u_Ubo.viewPos;
    v_texCoord = texCoord;
    v_normal = mat3(transpose(inverse(model))) * normal;
}