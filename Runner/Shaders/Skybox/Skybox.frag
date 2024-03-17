#version 450

const float PI = 3.14159265359;

layout(binding = 1) uniform sampler2D u_Texture;
layout(binding = 2) uniform sampler2D u_AlbedoMap;
layout(binding = 3) uniform sampler2D u_NormalMap;
layout(binding = 4) uniform sampler2D u_MetallicMap;
layout(binding = 5) uniform sampler2D u_RoughnessMap;
layout(binding = 6) uniform samplerCube u_Skybox;
layout(binding = 7) uniform samplerCube u_SkyboxIbl;

layout(binding = 8) uniform Material {
	vec3 albedo;
	float metallic;
	float roughness;
} u_Material;

layout(binding = 9) uniform Light {
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

const vec2 invAtan = vec2(0.1591, 0.3183);
vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
    uv *= invAtan;
    uv += 0.5;
    return uv;
}

void main() {
	vec3 result = pow(texture(u_Skybox, v_pos).rgb, vec3(2.2));
	color = vec4(result, 1);
}