#version 450

const float PI = 3.14159265359;

layout(location = 0) in vec3 v_color;
layout(location = 1) in vec3 v_normal;
layout(location = 2) in vec3 v_pos;
layout(location = 3) in vec3 v_ViewPos;

layout(location = 0) out vec4 color;

void main() {
	vec3 lightDir = normalize(v_ViewPos - v_pos);
	float diff = max(dot(v_normal, lightDir), 0.0);

	color = vec4(vec3(diff) * v_color, 1.0);
}