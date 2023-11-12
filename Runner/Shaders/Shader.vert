#version 410 core
layout(location = 0) in vec4 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec2 texCoord;

layout(location = 0) out vec2 v_texCoord;
layout(location = 1) out vec3 v_normal;
layout(location = 2) out vec3 v_pos;

uniform mat4 u_Translation;
uniform mat4 u_Rotation;
uniform mat4 u_Scale;

uniform mat4 u_View;
uniform mat4 u_Projection;

void main()
{
	mat4 model = u_Translation * u_Rotation * u_Scale;
	gl_Position = (u_Projection * u_View * model) * position;

	v_pos = vec3(model * position);
	v_texCoord = texCoord;
	v_normal = mat3(transpose(inverse(model))) * normal;
}