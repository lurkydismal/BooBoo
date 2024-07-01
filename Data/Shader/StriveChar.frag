#version 330

in vec3 fragPos;
in vec2 fragTexCoord;
in vec3 fragNormal;
in vec4 fragColor;

out vec4 finalColor;

void main()
{
	finalColor = vec4(fragNormal, 1.0f);
}