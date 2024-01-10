#version 450

const float PI = 3.14159265359;

layout(binding = 1) uniform samplerCube u_Texture;
layout(binding = 2) uniform sampler2D u_AlbedoMap;
layout(binding = 3) uniform sampler2D u_NormalMap;
layout(binding = 4) uniform sampler2D u_MetallicMap;
layout(binding = 5) uniform sampler2D u_RoughnessMap;
//layout(binding = 5) uniform sampler2D u_AoMap;

layout(binding = 6) uniform Material {
	vec3 albedo;
	float metallic;
	float roughness;
} u_Material;

layout(binding = 7) uniform Light {
	vec3 position;
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
} u_Light[4];

layout(location = 0) in vec2 v_texCoord;
layout(location = 1) in vec3 v_normal;
layout(location = 2) in vec3 v_pos;
layout(location = 3) in vec3 v_ViewPos;

layout(location = 0) out vec4 color;

void main() {
	color = texture(u_Texture, v_pos);
	//color = vec4(v_ViewPos, 1.0);
}