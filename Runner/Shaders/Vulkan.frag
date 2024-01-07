#version 450

layout(binding = 1) uniform sampler2D u_Texture;

layout(binding = 2) uniform Material {
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	float shininess;
} u_Material;

layout(binding = 3) uniform Light {
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

vec4 u_Color = vec4(1);

void main() {
		vec3 totalResult = vec3(0);

		for(int i = 0; i < 4; ++i) 
		{
			// Ambient
			vec3 ambient = u_Light[i].ambient * u_Material.ambient;

			// Diffuse
			vec3 norm = normalize(v_normal);
			vec3 lightDir = normalize(u_Light[i].position - v_pos);
			float diff = max(dot(norm, lightDir), 0.0);
			vec3 diffuse = u_Light[i].diffuse * (diff * u_Material.diffuse);

			// Specular
			vec3 viewDir = normalize(v_ViewPos - v_pos);
			vec3 reflectDir = reflect(-lightDir, norm);
			float spec = pow(max(dot(viewDir, reflectDir), 0.0), u_Material.shininess);
			vec3 specular =  u_Light[i].specular * (spec * u_Material.specular);

			//vec3 result = (ambient + diffuse + specular) * u_Color.xyz;
			vec3 result = (ambient + diffuse + specular) * u_Color.xyz;
			totalResult = totalResult + result / 4;
			//color = vec4(result, u_Color.w) * texColor;
		}

		vec4 texColor = texture(u_Texture, v_texCoord);
		color = vec4(totalResult, u_Color.w) * texColor;
		//color = vec4(u_Light[2].specular, u_Color.w) * texColor;
}