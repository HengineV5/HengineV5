#version 450

layout(location = 0) in vec2 v_texCoord;
layout(location = 1) in vec3 v_pos;

layout(location = 0) out vec4 color;

void main() {
    color = vec4(v_texCoord.xy, 0.0, 1.0);
    //color.x = round(color.x);
    //color.y = round(color.y);

    //color.x = int(color.x * 10) % 2 == 0 ? 0 : 1;
    //color.y = int(color.y * 10) % 2 == 0 ? 0 : 1;

    /*
    if (abs(color.x) > 0.5)
        color.x = 0;

    if (abs(color.y) > 0.5)
        color.y = 0;
    */
}