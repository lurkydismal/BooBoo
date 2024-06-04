local yu
local iz
local izActive = false

function MatchInit(actor)
	actor.curHealth = 420
	actor.maxHealth = 420
	yu = actor
	iz = actor:CreateChild("Iz", 0.0, 0.0)
	end

function SetPersonaMove(state, xOffset, yOffset, xMaxDist, yMaxDist)
	if izActive == false or math.abs(yu:GetDistanceFrom(iz).X) >= xMaxDist or math.abs(yu:GetDistanceFrom(iz).Y) >= yMaxDist then
		iz.position = Vector3(yu.position.X + xOffset, yu.position.Y + yOffset, 0.0)
		end
	iz:FaceActor(yu.opponent)
	iz:EnterState(state)
	izActive = true
	end

function Iz_Init(actor)
	actor.renderPriority = -1
	actor.curPalNum = 1
	actor:EnterState("Iz_Wait")
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

function NmlAtk5A1st_Init(actor)
	actor:AttackMacroWeak();
	actor:FaceActor(actor.opponent)
	end

function NmlAtk5A2nd_Init(actor)
	actor:AttackMacroMedium();
	actor:FaceActor(actor.opponent)
	end

function NmlAtk5A3rd_Init(actor)
	actor:AttackMacroHeavy();
	actor:FaceActor(actor.opponent)
	end

function NmlAtk5A3rd_Update(actor, frame)
	if frame == 3 then
		actor:SpawnEffect("bc202_eff", 0.0, 0.0)
		end
	end

function NmlAtk5C_Init(actor)
	actor:FaceActor(actor.opponent)
	end

function NmlAtk5C_Update(actor, frame)
	if frame == 1 then
		SetPersonaMove("Iz_Atk5C", -0.4, 0.0, 5.0, 0.01)
		end
	end

function Iz_Atk5C_Init(actor)
	actor:AttackMacroHeavy();
	end

function Iz_Atk5C_Update(actor, frame)
	if frame == 1 then
		actor:SetVelocity(10.0, 0.0)
		actor:SetVelocityMod(-0.35, 0.0)
	elseif frame == 3 then
		actor:SetVelocity(0.0, 0.0)
		actor:SetVelocityMod(0.0, 0.0)
	elseif frame == 12 then
		actor.animBlending = true
		end
	end

function Iz_Atk5C_Hit(actor, hit)
	yu:AddHitOrBlockCancels()
	end

function Iz_Atk5C_End(actor)
	actor.animBlending = false
	actor:EnterState("Iz_Wait", false)
	izActive = false
	end

local crossSlashHitTarget = nil

function CrossSlashC_Init(actor)
	actor:AttackMacroHeavy()
	actor.hitstopOnHit = 0
	actor.hitstunOnHit = 9999
	actor.hitStateStanding = luanet.enum(HitstunStates, "CmnHurtGutHeavy")
	actor.hitStateCrouching = luanet.enum(HitstunStates, "CmnHurtGutHeavy")
	actor.hitStateAerial = luanet.enum(HitstunStates, "CmnHurtGutHeavy")
	actor.dirOnHitGround = HitVector(16.0, 0.0)
	actor.dirOnHitAir = actor.dirOnHitGround
	actor.dirModOnHitAir = actor.dirModOnHitGround
	actor.dirMinOnHitAir = actor.dirMinOnHitGround
	end

function CrossSlashC_Update(actor, frame)
	if frame == 2 then
		actor:SpawnEffect("bc430_eff", 0.0, 0.0)
		BeginSuperFreeze(80, actor, 1.7, 2.6)
	elseif frame == 26 then
		actor:SpawnEffect("CrossSlash", 0.0, 0.0)
	elseif frame == 27 or frame == 28 then
		actor:AddPosition(0.6, 0.0)
	elseif frame == 56 or frame == 57 or frame == 58 then
		actor:AddPosition(-0.4, 0.0)
	elseif frame == 32 and crossSlashHitTarget ~= nil then
		SetPersonaMove("Iz_CrossSlashC", 0.0, 0.0, 0.0, 0.0)
		end
	end

function CrossSlashC_Hit(actor, hit)
	actor:SetInvincibility(true)
	hit.position = Vector3(actor.position.X + (0.3 * Sign(actor:GetDistanceFrom(actor.opponent).X)), 0.0, 0.0)
	crossSlashHitTarget = hit
	end

function CrossSlashC_End(actor)
	crossSlashHitTarget = nil
	end

function Iz_CrossSlashC_Init(actor)
	actor.position = crossSlashHitTarget.position
	actor:AddPosition(0.0, 10.0)
	actor:AttackMacroHeavy()
	actor.hitStateStanding = luanet.enum(HitstunStates, "CmnHurtStagger")
	actor.dirOnHitGround = Vector3(0.0, 0.0, 0.0)
	actor.dirModOnHitGround = Vector3(0.0, 0.0, 0.0)
	actor.dirMinOnHitGround = Vector3(0.0, 0.0, 0.0)
	actor.crumpleOnHit = true
	actor.HKDOnHit = 50
	end

function Iz_CrossSlashC_Update(actor, frame)
	if frame == 11 then
		actor:SetVelocity(0.0, -50.0)
		end
	end

function Iz_CrossSlashC_Land(actor)
	actor.frameActiveTime = 10000
	end

function Iz_CrossSlashC_End(actor)
	actor:EnterState("Iz_Wait", false)
	izActive = false
	end