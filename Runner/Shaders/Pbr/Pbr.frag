#version 450

const float PI = 3.14159265359;

layout(binding = 1) uniform sampler2D u_Texture;
layout(binding = 2) uniform sampler2D u_AlbedoMap;
layout(binding = 3) uniform sampler2D u_NormalMap;
layout(binding = 4) uniform sampler2D u_MetallicMap;
layout(binding = 5) uniform sampler2D u_RoughnessMap;
layout(binding = 6) uniform sampler2D u_DepthMap;
layout(binding = 7) uniform samplerCube u_Skybox;
layout(binding = 8) uniform samplerCube u_IrradianceMap;
layout(binding = 9) uniform samplerCube u_SpecularMap;
//layout(binding = 6) uniform sampler2D u_AoMap;

/*
layout(binding = 2) uniform Material {
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	float shininess;
} u_Material;
*/

layout(binding = 10) uniform Material {
	vec3 albedo;
	float metallic;
	float roughness;
} u_Material;

layout(binding = 11) uniform Light {
	vec3 position;
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
} u_Light[4];

layout(location = 0) in vec2 v_texCoord;
layout(location = 1) in vec3 v_normal;
layout(location = 2) in vec3 v_pos;
layout(location = 3) in vec3 v_ViewPos;
layout(location = 4) in mat3 v_TBN;

layout(location = 0) out vec4 color;

float ao = 1;
float height_scale = 0.1 / 4;

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 fresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
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

vec2 ParallaxMapping(vec2 texCoords, vec3 viewDir)
{
	float height = -texture(u_DepthMap, texCoords).r;
	vec2 p = viewDir.xy / viewDir.z * (height * height_scale);
    return texCoords - p;
}

void main() {
		vec3 viewDir = normalize(v_TBN * v_ViewPos - v_TBN * v_pos);
		vec2 texCoords = ParallaxMapping(v_texCoord, viewDir);

		vec3 albedo = texture(u_AlbedoMap, texCoords).rgb;
		float metallic = texture(u_MetallicMap, texCoords).r;
		float roughness = texture(u_RoughnessMap, texCoords).r;
		
		vec3 N = texture(u_NormalMap, texCoords).rgb;
		//N = N * 2.0 - 1.0;
		N = normalize(v_TBN * N);


		vec3 V = normalize(v_ViewPos - v_pos);
		vec3 R = reflect(-V, N); 

		vec3 F0 = vec3(0.04);
		F0 = mix(F0, albedo, metallic);

		vec3 Lo = vec3(0.0);
		for (int i = 0; i < 4; i++)
		{
			vec3 L = normalize(u_Light[i].position - v_pos);
			vec3 H = normalize(V + L);

			float dist = length(u_Light[i].position - v_pos);
			float attenuation = 1.0 / (dist * dist);
			vec3  radiance = u_Light[i].ambient * attenuation * 25;

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

		vec3 F = fresnelSchlickRoughness(max(dot(N, V), 0.0), F0, roughness);
    
		vec3 kS = F;
		vec3 kD = 1.0 - kS;
		kD *= 1.0 - metallic;	  
    
		vec3 irradiance = pow(texture(u_IrradianceMap, N).rgb, vec3(2.2));
		vec3 diffuse      = irradiance * albedo;
    
		// sample both the pre-filter map and the BRDF lut and combine them together as per the Split-Sum approximation to get the IBL specular part.
		const float MAX_REFLECTION_LOD = 5.0;
		vec3 prefilteredColor = pow(textureLod(u_SpecularMap, R,  roughness * MAX_REFLECTION_LOD).rgb, vec3(2.2));
		vec2 brdf  = texture(u_Texture, vec2(max(dot(N, V), 0.0), roughness)).rg;
		vec3 specular = prefilteredColor * (F * brdf.x + brdf.y);

		vec3 ambient = (kD * diffuse + specular) * ao;
		vec3 result = ambient + Lo;

		result = result / (result + vec3(1.0));
		result = pow(result, vec3(1.0/2.2)); 

		color = vec4(result, 1.0);

		/*
		float height = texture(u_DepthMap, v_texCoord).r;
		height = height - 0.1;
		height *= 1.5;
		color = vec4(vec3(height), 1.0);
		*/
}