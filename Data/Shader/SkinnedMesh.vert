#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec2 vertexTexCoord;
layout(location = 2) in vec3 vertexNormal;
layout(location = 3) in vec4 vertexColor;
layout(location = 4) in ivec4 vertexBones;
layout(location = 5) in vec4 vertexWeights;

uniform mat4 mvp;

const int MaxBones = 100;
const int MaxBoneInfluence = 4;
uniform mat4 boneMatricies[MaxBones];

out vec3 fragPos;
out vec2 fragTexCoord;
out vec3 fragNormal;
out vec4 fragColor;

void main()
{
	vec4 finalPos = vec4(vertexPosition, 1.0f);
	vec3 finalNormal = vertexNormal;
	for(int i = 0; i < MaxBoneInfluence; i++)
	{
		if(vertexBones[i] == -1 || vertexBones[i] >= MaxBones)
			continue;
		vec4 localPos = boneMatricies[vertexBones[i]] * vec4(vertexPosition, 1.0f);
		finalPos += localPos * vertexWeights[i];
		vec3 localNormal = mat3(boneMatricies[vertexBones[i]]) * vertexNormal;
		finalNormal += localNormal * vertexWeights[i];
	}

	gl_Position = mvp * finalPos;
	fragPos = gl_Position.xyz;
	fragTexCoord = vertexTexCoord;
	fragNormal = finalNormal;
	fragColor = vertexColor;
}