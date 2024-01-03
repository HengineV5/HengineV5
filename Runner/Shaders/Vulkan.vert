#version 450

#extension GL_KHR_vulkan_glsl : enable

layout(push_constant) uniform constants
{
  mat4 model;
} pushConstant;

layout(binding = 0) uniform UniformBufferObject {
    mat4 translation;
    mat4 rotation;
    mat4 scale;
    mat4 view;
    mat4 proj;
} u_Ubo;

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec2 inTexCoord;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec2 fragTexCoord;

void main() {
	fragColor = vec3(1.0f, 1.0f, 1.0f);
  fragTexCoord = inTexCoord;

  //mat4 model = ubo.translation * ubo.rotation * ubo.scale;
  mat4 model = pushConstant.model;
	gl_Position = u_Ubo.proj * u_Ubo.view * model * vec4(inPosition, 1.0);
}