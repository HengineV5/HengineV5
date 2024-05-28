#version 450

const float PI = 3.14159265359;

layout(location = 0) in vec2 v_texCoord;
layout(location = 1) in vec3 v_normal;
layout(location = 2) in vec3 v_pos;
layout(location = 3) in vec3 v_ViewPos;

layout(location = 0) out vec4 color;

void main() {
	color = vec4(1.0, 1.0, 1.0, 1.0);
}