#version 450

const float PI = 3.14159265359;

layout(binding = 1) uniform sampler2D u_Texture;
layout(binding = 2) uniform sampler2D u_AlbedoMap;
layout(binding = 3) uniform sampler2D u_NormalMap;
layout(binding = 4) uniform sampler2D u_MetallicMap;
layout(binding = 5) uniform sampler2D u_RoughnessMap;
//layout(binding = 5) uniform sampler2D u_AoMap;

/*
layout(binding = 2) uniform Material {
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	float shininess;
} u_Material;
*/

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

float ao = 0;

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}  

float distributionGGX(vec3 N, vec3 H, float roughness)
{
    float a      = roughness*roughness;
    float a2     = a*a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;
	
    float num   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return num / denom;
}

float geometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num   = NdotV;
    float denom = NdotV * (1.0 - k) + k;
	
    return num / denom;
}

float geometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2  = geometrySchlickGGX(NdotV, roughness);
    float ggx1  = geometrySchlickGGX(NdotL, roughness);
	
    return ggx1 * ggx2;
}

vec3 cookTerranceBRDF(vec3 N, vec3 V, vec3 L, vec3 H, vec3 albedo, float metallic, float roughness)
{
	vec3 F0 = vec3(0.04);
	F0 = mix(F0, albedo, metallic);
	vec3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);

	float NDF = distributionGGX(N, H, roughness);       
	float G   = geometrySmith(N, V, L, roughness);  

	vec3 numerator    = NDF * G * F;
	float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0)  + 0.0001;
	return numerator / denominator;  
}

void main() {
		/*
		vec3 albedo = u_Material.albedo;
		float metallic = u_Material.metallic;
		//float roughness = u_Material.roughness;
		*/

		vec3 albedo = texture(u_AlbedoMap, v_texCoord).rgb;
		albedo = vec3(pow(albedo.x, 2.2), pow(albedo.y, 2.2), pow(albedo.z, 2.2));

		float metallic = texture(u_MetallicMap, v_texCoord).r;
		float roughness = texture(u_RoughnessMap, v_texCoord).r;
		roughness *= 8;
		/*
		*/
		
		vec3 N = normalize(v_normal);
		vec3 V = normalize(v_ViewPos - v_pos);

		vec3 Lo = vec3(0.0);
		for (int i = 0; i < 4; i++)
		{
			vec3 L = normalize(u_Light[i].position - v_pos);
			vec3 H = normalize(V + L);

			float dist = length(u_Light[i].position - v_pos);
			float attenuation = 1.0 / (dist * dist);
			vec3  radiance = u_Light[i].ambient * attenuation * 50;

			vec3 F0 = vec3(0.04);
			F0 = mix(F0, albedo, metallic);
			vec3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);

			float NDF = distributionGGX(N, H, roughness);       
			float G = geometrySmith(N, V, L, roughness);  

			vec3 numerator = NDF * G * F;
			float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0)  + 0.0001;
			vec3 specular = numerator / denominator;

			vec3 kS = F;
			vec3 kD = vec3(1.0) - kS;
			kD *= 1.0 - metallic;

			float NdotL = max(dot(N, L), 0.0);
			Lo += (kD * albedo / PI + specular) * radiance * NdotL;
		}

		vec3 ambient = vec3(0.03) * albedo * ao;
		vec3 result = ambient + Lo;

		result = result / (result + vec3(1.0));
		result = pow(result, vec3(1.0/2.2)); 

		color = vec4(result, 1.0);
}