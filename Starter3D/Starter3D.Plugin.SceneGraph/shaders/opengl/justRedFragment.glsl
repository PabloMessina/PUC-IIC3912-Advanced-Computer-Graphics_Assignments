#version 330

precision highp float;

in vec3 fragPosition;
in vec3 fragNormal;
in vec3 fragTextureCoords;

out vec4 outFragColor;

void main(void)
{
  outFragColor = vec4(1.0,0.0,0.0,1.0);
}