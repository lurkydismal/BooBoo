import("System.Numerics")
import("BooBoo", "BooBoo.Battle")
import("BooBoo", "BooBoo.Util")

local oneSixteth <const> = 1/60

function Sign(x)
	if x > 0 then
		return 1
	elseif x < 0 then
		return -1
	else
		return 0
		end
	end

function HitVector(x, y)
	return Vector2(-x * oneSixteth, y * oneSixteth)
	end