local yu
local iz
local izActive = false

function MatchInit(actor)
	actor.curHealth = 400
	actor.maxHealth = 420
	yu = actor
	iz = actor:CreateChild("Iz", 0.0, 0.0)
	end

function SetPersonaMove(state, xOffset, yOffset, xMaxDist, yMaxDist)
	if izActive == false or math.abs(yu:GetDistanceFrom(iz).X) >= xMaxDist or math.abs(yu:GetDistanceFrom(iz).Y) >= yMaxDist then
		iz.position.X = yu.position.X + xOffset
		iz.position.Y = yu.position.Y + yOffset
		end
	iz:EnterState(state)
	end

function Iz_Init(actor)
	actor.curPalNum = 1
	end

function CmnStand_Loop(actor)
	if actor.curAnimName == "CmnStandTaunt" then
		actor:SetAnimation("CmnStand")
	elseif math.random(0, 10) >= 10 then
		actor:SetAnimation("CmnStandTaunt")
    	end
    end

function CmnStandTurn_Init(actor)
	actor:Flip()
	end

function CmnFWalk_Init(actor)
	actor:SetVelocity(3.0, 0.0)
	end

function CmnFWalk_End(actor)
	actor:SetVelocity(0.0, 0.0)
	end

function CmnFDash_Init(actor)
	actor:SetVelocity(9.0, 0.0)
	actor:RemoveCancel("CmnFWalk")
	end

function CmnFDash_End(actor)
	actor:SetVelocity(0.0, 0.0)
	end

function CmnBWalk_Init(actor)
	actor:SetVelocity(-2.25, 0.0)
	end

function CmnBWalk_End(actor)
	actor:SetVelocity(0.0, 0.0)
	end

function CmnJump_Init(actor)
	local dir = actor:GetInputDir()
	if dir == 9 then
		actor:SetVelocity(3.0, 17.0)
	elseif dir == 7 then
		actor:SetVelocity(-3.0, 17.0)
	else
		actor:SetVelocity(0.0, 17.0)
		end
	actor:SetVelocityMod(0.0, -0.7)
	end

function CmnLand_Init(actor)
	actor:SetVelocity(0.0, 0.0)
	actor:SetVelocityMod(0.0, 0.0)
	end

function NmlAtk5C_Update(actor, frame)
	if frame == 1 then
		SetPersonaMove("Iz_Atk5C", 0.6, 0.0, 5.0, 0.01)
		end
	end
	
function Iz_Atk5C_Update(actor, frame)
	if frame == 1 then
		actor:SetVelocity(8.0, 0.0)
		actor:SetVelocityMod(-0.35, 0.0)
	elseif frame == 3 then
		actor:SetVelocity(0.0, 0.0)
		actor:SetVelocityMod(0.0, 0.0)
	elseif frame == 12 then
		actor.animBlending = true
		end
	end

function Iz_Atk5C_End(actor)
	actor.animBlending = false
	actor:EnterState("Iz_Init", false)
	end