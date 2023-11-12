#version 410 core

struct Material {
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	float shininess;
};

struct Light {
	vec3 position;

	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
};

layout(location = 0) in vec2 v_texCoord;
layout(location = 1) in vec3 v_normal;
layout(location = 2) in vec3 v_pos;

layout(location = 0) out vec4 color;

uniform vec4 u_Color = vec4(1);
uniform vec4 u_LightColor;
uniform vec3 u_LightPos;

uniform vec3 u_ViewPos;

uniform Light u_Light;
uniform Material u_Material;
uniform sampler2D u_Texture;

void main()
{
	// Ambient
	vec3 ambient = u_Light.ambient * u_Material.ambient;

	// Diffuse
	vec3 norm = normalize(v_normal);
	vec3 lightDir = normalize(u_Light.position - v_pos);
	float diff = max(dot(norm, lightDir), 0.0);
	vec3 diffuse = u_Light.diffuse * (diff * u_Material.diffuse);

	// Specular
	vec3 viewDir = normalize(u_ViewPos - v_pos);
	vec3 reflectDir = reflect(-lightDir, norm);
	float spec = pow(max(dot(viewDir, reflectDir), 0.0), u_Material.shininess);
	vec3 specular =  u_Light.specular * (spec * u_Material.specular);

	vec4 texColor = texture(u_Texture, v_texCoord);
	vec3 result = (ambient + diffuse + specular) * u_Color.xyz;
	color = vec4(result, u_Color.w);// * texColor;
}