#version 450

#extension GL_KHR_vulkan_glsl : enable

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec2 texCoord;

layout(location = 0) out vec2 v_texCoord;
layout(location = 1) out vec3 v_normal;
layout(location = 2) out vec3 v_pos;

layout(binding = 0) uniform UniformBufferObject {
    mat4 translation;
    mat4 rotation;
    mat4 scale;
    mat4 view;
    mat4 proj;
} u_Ubo;

void main() {
  mat4 model = u_Ubo.translation * u_Ubo.rotation * u_Ubo.scale;
	gl_Position = u_Ubo.proj * u_Ubo.view * model * vec4(position, 1.0);

  v_pos = vec3(model * vec4(position, 1));
	v_texCoord = texCoord;
	v_normal = mat3(transpose(inverse(model))) * normal;
}