#version 450

const float PI = 3.14159265359;

layout(binding = 1) uniform sampler2D u_Texture;
layout(binding = 2) uniform sampler2D u_AlbedoMap;
layout(binding = 3) uniform sampler2D u_NormalMap;
layout(binding = 4) uniform sampler2D u_MetallicMap;
layout(binding = 5) uniform sampler2D u_RoughnessMap;
layout(binding = 6) uniform samplerCube u_Skybox;
layout(binding = 7) uniform samplerCube u_IrradianceMap;
layout(binding = 8) uniform samplerCube u_SpecularMap;
//layout(binding = 6) uniform sampler2D u_AoMap;

/*
layout(binding = 2) uniform Material {
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	float shininess;
} u_Material;
*/

layout(binding = 9) uniform Material {
	vec3 albedo;
	float metallic;
	float roughness;
} u_Material;

layout(binding = 10) uniform Light {
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

float ao = 1;

vec3 getNormalFromMap()
{
    vec3 tangentNormal = texture(u_NormalMap, v_texCoord).xyz;

    vec3 Q1  = dFdx(v_pos);
    vec3 Q2  = dFdy(v_pos);
    vec2 st1 = dFdx(v_texCoord);
    vec2 st2 = dFdy(v_texCoord);

    vec3 N   = normalize(v_normal);
    vec3 T  = normalize(Q1*st2.t - Q2*st1.t);
    vec3 B  = -normalize(cross(N, T));
    mat3 TBN = mat3(T, B, N);

    return normalize(TBN * tangentNormal);
}

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
		//vec3 albedo = u_Material.albedo;
		vec3 albedo = pow(texture(u_AlbedoMap, v_texCoord).rgb, vec3(2.2)) * u_Material.albedo;

		//float metallic = 1 - texture(u_MetallicMap, v_texCoord).r;
		float metallic = 1 - (pow(texture(u_MetallicMap, v_texCoord).b, 2.2) * u_Material.metallic);
		//float metallic = 1 - (pow(texture(u_MetallicMap, v_texCoord).b, 2.2));

		//float roughness = 1 - texture(u_RoughnessMap, v_texCoord).r;
		float roughness = 1 - (pow(texture(u_RoughnessMap, v_texCoord).r, 2.2) * u_Material.roughness);
		//float roughness = 1 - (pow(texture(u_RoughnessMap, v_texCoord).r, 2.2));
		
		vec3 N = normalize(v_normal);
		//vec3 N = getNormalFromMap();
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
			vec3  radiance = u_Light[i].ambient * attenuation * 50;

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
		//color = vec4(v_texCoord, 0.0, 1.0);
		//color = vec4(v_texCoord, 0.0, 1.0);
		//color = vec4(albedo, 1.0);
		//color = vec4(mod(1, roughness), 0.0, 0.0, 1.0);
		//color = vec4(u_Material.roughness, 0.0, 0.0, 1.0);

		//vec3 I1 = normalize(v_pos - v_ViewPos);
		//vec3 R1 = reflect(I1, normalize(v_normal));
		//color = vec4(texture(u_Texture, R1).rgb, 1.0);
}