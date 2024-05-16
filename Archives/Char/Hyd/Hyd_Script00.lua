local orbiterState

function MatchInit(actor)
	orbiterState = 0
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

function CmnBWalk_Init(actor)
	actor:SetVelocity(-2.25, 0.0)
	end

function CmnBWalk_End(actor)
	actor:SetVelocity(0.0, 0.0)
	end