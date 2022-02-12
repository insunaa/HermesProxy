﻿using Framework.GameMath;
using Framework.IO;
using HermesProxy.World.Enums.V2_5_2_39570;
using HermesProxy.World.Server.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HermesProxy.World.Objects.Version.V2_5_2_39570
{
    public class ObjectUpdateBuilder
    {
        public ObjectUpdateBuilder(ObjectUpdate updateData)
        {
            m_alreadyWritten = false;
            m_updateData = updateData;

            Enums.ObjectType objectType = updateData.Guid.GetObjectType();
            if (updateData.CreateData != null)
            {
                objectType = updateData.CreateData.ObjectType;
                if (updateData.CreateData.ThisIsYou)
                    objectType = Enums.ObjectType.ActivePlayer;
            }
            m_objectType = ObjectTypeConverter.ConvertToBCC(objectType);
            m_objectTypeMask = Enums.ObjectTypeMask.Object;

            uint size;
            switch (m_objectType)
            {
                case Enums.ObjectTypeBCC.Item:
                    size = (uint)ItemField.ITEM_END;
                    m_objectTypeMask |= Enums.ObjectTypeMask.Item;
                    break;
                case Enums.ObjectTypeBCC.Container:
                    size = (uint)ContainerField.CONTAINER_END;
                    m_objectTypeMask |= Enums.ObjectTypeMask.Item;
                    m_objectTypeMask |= Enums.ObjectTypeMask.Container;
                    break;
                case Enums.ObjectTypeBCC.Unit:
                    size = (uint)UnitField.UNIT_END;
                    m_objectTypeMask |= Enums.ObjectTypeMask.Unit;
                    break;
                case Enums.ObjectTypeBCC.Player:
                    size = (uint)PlayerField.PLAYER_END;
                    m_objectTypeMask |= Enums.ObjectTypeMask.Unit;
                    m_objectTypeMask |= Enums.ObjectTypeMask.Player;
                    break;
                case Enums.ObjectTypeBCC.ActivePlayer:
                    size = (uint)ActivePlayerField.ACTIVE_PLAYER_END;
                    m_objectTypeMask |= Enums.ObjectTypeMask.Unit;
                    m_objectTypeMask |= Enums.ObjectTypeMask.Player;
                    m_objectTypeMask |= Enums.ObjectTypeMask.ActivePlayer;
                    break;
                case Enums.ObjectTypeBCC.GameObject:
                    size = (uint)GameObjectField.GAMEOBJECT_END;
                    m_objectTypeMask |= Enums.ObjectTypeMask.GameObject;
                    break;
                case Enums.ObjectTypeBCC.DynamicObject:
                    size = (uint)DynamicObjectField.DYNAMICOBJECT_END;
                    m_objectTypeMask |= Enums.ObjectTypeMask.DynamicObject;
                    break;
                case Enums.ObjectTypeBCC.Corpse:
                    size = (uint)CorpseField.CORPSE_END;
                    m_objectTypeMask |= Enums.ObjectTypeMask.Corpse;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unsupported object type!");
            }

            m_fields = new UpdateFieldsArray(size);
        }

        protected bool m_alreadyWritten;
        protected ObjectUpdate m_updateData;
        protected UpdateFieldsArray m_fields;
        protected Enums.ObjectTypeBCC m_objectType;
        protected Enums.ObjectTypeMask m_objectTypeMask;
        protected CreateObjectBits m_createBits;

        public void WriteToPacket(WorldPacket packet)
        {
            packet.WriteUInt8((byte)m_updateData.Type);
            packet.WritePackedGuid128(m_updateData.Guid);

            if (m_updateData.Type != Enums.UpdateTypeModern.Values)
            {
                packet.WriteUInt8((byte)m_objectType);
                packet.WriteInt32((int)m_objectTypeMask); //< HeirFlags

                SetCreateObjectBits();
                BuildMovementUpdate(packet);
            }
            BuildValuesUpdate(packet);
            BuildDynamicValuesUpdate(packet);
        }

        public void SetCreateObjectBits()
        {
            m_createBits.Clear();
            m_createBits.MovementUpdate = m_updateData.CreateData != null & m_updateData.CreateData.MoveInfo != null;
            m_createBits.MovementTransport = m_updateData.CreateData != null & m_updateData.CreateData.MoveInfo != null && m_updateData.CreateData.MoveInfo.TransportGuid != null;
            m_createBits.Stationary = m_updateData.Guid.GetHighType() == Enums.HighGuidType.Transport;
            m_createBits.CombatVictim = m_updateData.CreateData != null && m_updateData.CreateData.AutoAttackVictim != null;
            m_createBits.Vehicle = m_updateData.CreateData != null & m_updateData.CreateData.MoveInfo != null && m_updateData.CreateData.MoveInfo.VehicleId != 0;
            m_createBits.Rotation = m_objectType == Enums.ObjectTypeBCC.GameObject;
            m_createBits.ThisIsYou = m_createBits.ActivePlayer = m_objectType == Enums.ObjectTypeBCC.ActivePlayer;
        }

        public void BuildValuesUpdate(WorldPacket packet)
        {
            WriteValuesToArray();
            m_fields.WriteToPacket(packet);
        }

        public void BuildDynamicValuesUpdate(WorldPacket packet)
        {
            uint valueCount = (uint)PlayerDynamicField.PLAYER_DYNAMIC_END;
            var updateMask = new UpdateMask(valueCount);
            updateMask.AppendToPacket(packet);
        }

        public void BuildMovementUpdate(WorldPacket data)
        {
            int PauseTimesCount = 0;

            data.WriteBit(m_createBits.NoBirthAnim);
            data.WriteBit(m_createBits.EnablePortals);
            data.WriteBit(m_createBits.PlayHoverAnim);
            data.WriteBit(m_createBits.MovementUpdate);
            data.WriteBit(m_createBits.MovementTransport);
            data.WriteBit(m_createBits.Stationary);
            data.WriteBit(m_createBits.CombatVictim);
            data.WriteBit(m_createBits.ServerTime);
            data.WriteBit(m_createBits.Vehicle);
            data.WriteBit(m_createBits.AnimKit);
            data.WriteBit(m_createBits.Rotation);
            data.WriteBit(m_createBits.AreaTrigger);
            data.WriteBit(m_createBits.GameObject);
            data.WriteBit(m_createBits.SmoothPhasing);
            data.WriteBit(m_createBits.ThisIsYou);
            data.WriteBit(m_createBits.SceneObject);
            data.WriteBit(m_createBits.ActivePlayer);
            data.WriteBit(m_createBits.Conversation);
            data.FlushBits();

            if (m_createBits.MovementUpdate)
            {
                MovementInfo moveInfo = m_updateData.CreateData.MoveInfo;
                bool HasFallDirection = moveInfo.Flags.HasAnyFlag(HermesProxy.World.Enums.MovementFlagModern.Falling);
                bool HasFall = HasFallDirection || moveInfo.FallTime != 0;
                bool HasSpline = m_updateData.CreateData.MoveSpline != null;

                data.WritePackedGuid128(m_updateData.Guid);                     // MoverGUID

                data.WriteUInt32(moveInfo.MoveTime);                            // MoveTime
                data.WriteFloat(moveInfo.Position.X);
                data.WriteFloat(moveInfo.Position.Y);
                data.WriteFloat(moveInfo.Position.Z);
                data.WriteFloat(moveInfo.Orientation);

                data.WriteFloat(moveInfo.SwimPitch);                            // Pitch
                data.WriteFloat(moveInfo.SplineElevation);                      // StepUpStartElevation

                data.WriteUInt32(0);                                            // RemoveForcesIDs.size()
                data.WriteUInt32(0);                                            // MoveIndex

                //for (public uint i = 0; i < RemoveForcesIDs.Count; ++i)
                //    *data << ObjectGuid(RemoveForcesIDs);

                data.WriteBits(moveInfo.Flags, 30);
                data.WriteBits(moveInfo.FlagsExtra, 18);
                data.WriteBit(moveInfo.TransportGuid != null);                 // HasTransport
                data.WriteBit(HasFall);                                        // HasFall
                data.WriteBit(HasSpline);                                      // HasSpline - marks that the unit uses spline movement
                data.WriteBit(false);                                          // HeightChangeFailed
                data.WriteBit(false);                                          // RemoteTimeValid

                if (moveInfo.TransportGuid != null)
                    WriteTransportInfo(data, moveInfo);

                if (HasFall)
                {
                    data.WriteUInt32(moveInfo.FallTime);                              // Time
                    data.WriteFloat(moveInfo.JumpVerticalSpeed);                      // JumpVelocity

                    if (data.WriteBit(HasFallDirection))
                    {
                        data.WriteFloat(moveInfo.JumpSinAngle);                       // Direction
                        data.WriteFloat(moveInfo.JumpCosAngle);
                        data.WriteFloat(moveInfo.JumpHorizontalSpeed);                // Speed
                    }
                }

                data.WriteFloat(moveInfo.WalkSpeed);
                data.WriteFloat(moveInfo.RunSpeed);
                data.WriteFloat(moveInfo.RunBackSpeed);
                data.WriteFloat(moveInfo.SwimSpeed);
                data.WriteFloat(moveInfo.SwimBackSpeed);
                data.WriteFloat(moveInfo.FlightSpeed);
                data.WriteFloat(moveInfo.FlightBackSpeed);
                data.WriteFloat(moveInfo.TurnRate);
                data.WriteFloat(moveInfo.PitchRate);

                //MovementForces movementForces = unit.GetMovementForces();
                //if (movementForces != null)
                //{
                //    data.WriteInt32(movementForces.GetForces().Count);
                //    data.WriteFloat(movementForces.GetModMagnitude());          // MovementForcesModMagnitude
                //}
                //else
                //{
                    data.WriteUInt32(0);
                    data.WriteFloat(1.0f);                                       // MovementForcesModMagnitude
                //}

                data.WriteBit(HasSpline);
                data.FlushBits();

                //if (movementForces != null)
                //    foreach (MovementForce force in movementForces.GetForces())
                //        MovementExtensions.WriteMovementForceWithDirection(force, data, unit);

                // HasMovementSpline - marks that spline data is present in packet
                if (HasSpline)
                    WriteCreateObjectSplineDataBlock(m_updateData.CreateData.MoveSpline, data);
            }

            data.WriteInt32(PauseTimesCount);

            if (m_createBits.Stationary)
            {
                data.WriteFloat(m_updateData.CreateData.MoveInfo.Position.X);
                data.WriteFloat(m_updateData.CreateData.MoveInfo.Position.Y);
                data.WriteFloat(m_updateData.CreateData.MoveInfo.Position.Z);
                data.WriteFloat(m_updateData.CreateData.MoveInfo.Orientation);
            }

            if (m_createBits.CombatVictim)
                data.WritePackedGuid128(m_updateData.CreateData.AutoAttackVictim); // CombatVictim

            if (m_createBits.ServerTime)
            {
                /** @TODO Use IsTransport() to also handle type 11 (TRANSPORT)
                    Currently grid objects are not updated if there are no nearby players,
                    this causes clients to receive different PathProgress
                    resulting in players seeing the object in a different position
                */
                if (m_updateData.CreateData.MoveInfo.TransportPathTimer != 0) // ServerTime
                    data.WriteUInt32(m_updateData.CreateData.MoveInfo.TransportPathTimer);
                else
                    data.WriteUInt32((uint)Time.UnixTime);
            }

            if (m_createBits.Vehicle)
            {
                data.WriteUInt32(m_updateData.CreateData.MoveInfo.VehicleId); // RecID
                data.WriteFloat(m_updateData.CreateData.MoveInfo.VehicleOrientation); // InitialRawFacing
            }

            if (m_createBits.AnimKit)
            {
                data.WriteUInt16(0); // AiID
                data.WriteUInt16(0); // MovementID
                data.WriteUInt16(0); // MeleeID
            }

            if (m_createBits.Rotation)
                data.WriteInt64(m_updateData.CreateData.MoveInfo.Rotation.GetPackedRotation()); // Rotation

            for (int i = 0; i < PauseTimesCount; ++i)
                data.WriteUInt32(0);

            if (m_createBits.MovementTransport)
                WriteTransportInfo(data, m_updateData.CreateData.MoveInfo);

            /*
            if (m_createBits.AreaTrigger)
            {
                AreaTrigger areaTrigger = ToAreaTrigger();
                AreaTriggerMiscTemplate areaTriggerMiscTemplate = areaTrigger.GetMiscTemplate();
                AreaTriggerTemplate areaTriggerTemplate = areaTrigger.GetTemplate();

                data.WriteUInt32(areaTrigger.GetTimeSinceCreated());

                data.WriteVector3(areaTrigger.GetRollPitchYaw());

                bool hasAbsoluteOrientation = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAbsoluteOrientation);
                bool hasDynamicShape = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasDynamicShape);
                bool hasAttached = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAttached);
                bool hasFaceMovementDir = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFaceMovementDir);
                bool hasFollowsTerrain = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFollowsTerrain);
                bool hasUnk1 = areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk1);
                bool hasTargetRollPitchYaw = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasTargetRollPitchYaw);
                bool hasScaleCurveID = areaTriggerMiscTemplate.ScaleCurveId != 0;
                bool hasMorphCurveID = areaTriggerMiscTemplate.MorphCurveId != 0;
                bool hasFacingCurveID = areaTriggerMiscTemplate.FacingCurveId != 0;
                bool hasMoveCurveID = areaTriggerMiscTemplate.MoveCurveId != 0;
                bool hasAnimation = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAnimID);
                bool hasUnk3 = areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk3);
                bool hasAnimKitID = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAnimKitID);
                bool hasAnimProgress = false;
                bool hasAreaTriggerSphere = areaTriggerTemplate.IsSphere();
                bool hasAreaTriggerBox = areaTriggerTemplate.IsBox();
                bool hasAreaTriggerPolygon = areaTriggerTemplate.IsPolygon();
                bool hasAreaTriggerCylinder = areaTriggerTemplate.IsCylinder();
                bool hasAreaTriggerSpline = areaTrigger.HasSplines();
                bool hasOrbit = areaTrigger.HasOrbit();
                bool hasMovementScript = false;

                data.WriteBit(hasAbsoluteOrientation);
                data.WriteBit(hasDynamicShape);
                data.WriteBit(hasAttached);
                data.WriteBit(hasFaceMovementDir);
                data.WriteBit(hasFollowsTerrain);
                data.WriteBit(hasUnk1);
                data.WriteBit(hasTargetRollPitchYaw);
                data.WriteBit(hasScaleCurveID);
                data.WriteBit(hasMorphCurveID);
                data.WriteBit(hasFacingCurveID);
                data.WriteBit(hasMoveCurveID);
                data.WriteBit(hasAnimation);
                data.WriteBit(hasAnimKitID);
                data.WriteBit(hasUnk3);
                data.WriteBit(hasAnimProgress);
                data.WriteBit(hasAreaTriggerSphere);
                data.WriteBit(hasAreaTriggerBox);
                data.WriteBit(hasAreaTriggerPolygon);
                data.WriteBit(hasAreaTriggerCylinder);
                data.WriteBit(hasAreaTriggerSpline);
                data.WriteBit(hasOrbit);
                data.WriteBit(hasMovementScript);

                if (hasUnk3)
                    data.WriteBit(false);

                data.FlushBits();

                if (hasAreaTriggerSpline)
                {
                    data.WriteUInt32(areaTrigger.GetTimeToTarget());
                    data.WriteUInt32(areaTrigger.GetElapsedTimeForMovement());

                    MovementExtensions.WriteCreateObjectAreaTriggerSpline(areaTrigger.GetSpline(), data);
                }

                if (hasTargetRollPitchYaw)
                    data.WriteVector3(areaTrigger.GetTargetRollPitchYaw());

                if (hasScaleCurveID)
                    data.WriteUInt32(areaTriggerMiscTemplate.ScaleCurveId);

                if (hasMorphCurveID)
                    data.WriteUInt32(areaTriggerMiscTemplate.MorphCurveId);

                if (hasFacingCurveID)
                    data.WriteUInt32(areaTriggerMiscTemplate.FacingCurveId);

                if (hasMoveCurveID)
                    data.WriteUInt32(areaTriggerMiscTemplate.MoveCurveId);

                if (hasAnimation)
                    data.WriteUInt32(areaTriggerMiscTemplate.AnimId);

                if (hasAnimKitID)
                    data.WriteUInt32(areaTriggerMiscTemplate.AnimKitId);

                if (hasAnimProgress)
                    data.WriteUInt32(0);

                if (hasAreaTriggerSphere)
                {
                    data.WriteFloat(areaTriggerTemplate.SphereDatas.Radius);
                    data.WriteFloat(areaTriggerTemplate.SphereDatas.RadiusTarget);
                }

                if (hasAreaTriggerBox)
                {
                    unsafe
                    {
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.Extents[0]);
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.Extents[1]);
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.Extents[2]);

                        data.WriteFloat(areaTriggerTemplate.BoxDatas.ExtentsTarget[0]);
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.ExtentsTarget[1]);
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.ExtentsTarget[2]);
                    }
                }

                if (hasAreaTriggerPolygon)
                {
                    data.WriteInt32(areaTriggerTemplate.PolygonVertices.Count);
                    data.WriteInt32(areaTriggerTemplate.PolygonVerticesTarget.Count);
                    data.WriteFloat(areaTriggerTemplate.PolygonDatas.Height);
                    data.WriteFloat(areaTriggerTemplate.PolygonDatas.HeightTarget);

                    foreach (var vertice in areaTriggerTemplate.PolygonVertices)
                        data.WriteVector2(vertice);

                    foreach (var vertice in areaTriggerTemplate.PolygonVerticesTarget)
                        data.WriteVector2(vertice);
                }

                if (hasAreaTriggerCylinder)
                {
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.Radius);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.RadiusTarget);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.Height);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.HeightTarget);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.LocationZOffset);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.LocationZOffsetTarget);
                }

                //if (hasMovementScript)
                //    *data << *areaTrigger->GetMovementScript(); // AreaTriggerMovementScriptInfo

                if (hasOrbit)
                    areaTrigger.GetCircularMovementInfo().Value.Write(data);
            }
            */

            if (m_createBits.GameObject)
            {
                bool bit8 = false;
                uint Int1 = 0;

                data.WriteUInt32(0); // WorldEffectID

                data.WriteBit(bit8);
                data.FlushBits();
                if (bit8)
                    data.WriteUInt32(Int1);
            }

            //if (m_createBits.SmoothPhasing)
            //{
            //    data.WriteBit(ReplaceActive);
            //    data.WriteBit(StopAnimKits);
            //    data.WriteBit(HasReplaceObjectt);
            //    data.FlushBits();
            //    if (HasReplaceObject)
            //        *data << ObjectGuid(ReplaceObject);
            //}

            //if (m_createBits.SceneObject)
            //{
            //    data.WriteBit(HasLocalScriptData);
            //    data.WriteBit(HasPetBattleFullUpdate);
            //    data.FlushBits();

            //    if (HasLocalScriptData)
            //    {
            //        data.WriteBits(Data.length(), 7);
            //        data.FlushBits();
            //        data.WriteString(Data);
            //    }

            //    if (HasPetBattleFullUpdate)
            //    {
            //        for (std::size_t i = 0; i < 2; ++i)
            //        {
            //            *data << ObjectGuid(Players[i].CharacterID);
            //            *data << int32(Players[i].TrapAbilityID);
            //            *data << int32(Players[i].TrapStatus);
            //            *data << uint16(Players[i].RoundTimeSecs);
            //            *data << int8(Players[i].FrontPet);
            //            *data << uint8(Players[i].InputFlags);

            //            data.WriteBits(Players[i].Pets.size(), 2);
            //            data.FlushBits();
            //            for (std::size_t j = 0; j < Players[i].Pets.size(); ++j)
            //            {
            //                *data << ObjectGuid(Players[i].Pets[j].BattlePetGUID);
            //                *data << int32(Players[i].Pets[j].SpeciesID);
            //                *data << int32(Players[i].Pets[j].DisplayID);
            //                *data << int32(Players[i].Pets[j].CollarID);
            //                *data << int16(Players[i].Pets[j].Level);
            //                *data << int16(Players[i].Pets[j].Xp);
            //                *data << int32(Players[i].Pets[j].CurHealth);
            //                *data << int32(Players[i].Pets[j].MaxHealth);
            //                *data << int32(Players[i].Pets[j].Power);
            //                *data << int32(Players[i].Pets[j].Speed);
            //                *data << int32(Players[i].Pets[j].NpcTeamMemberID);
            //                *data << uint16(Players[i].Pets[j].BreedQuality);
            //                *data << uint16(Players[i].Pets[j].StatusFlags);
            //                *data << int8(Players[i].Pets[j].Slot);

            //                *data << uint32(Players[i].Pets[j].Abilities.size());
            //                *data << uint32(Players[i].Pets[j].Auras.size());
            //                *data << uint32(Players[i].Pets[j].States.size());
            //                for (std::size_t k = 0; k < Players[i].Pets[j].Abilities.size(); ++k)
            //                {
            //                    *data << int32(Players[i].Pets[j].Abilities[k].AbilityID);
            //                    *data << int16(Players[i].Pets[j].Abilities[k].CooldownRemaining);
            //                    *data << int16(Players[i].Pets[j].Abilities[k].LockdownRemaining);
            //                    *data << int8(Players[i].Pets[j].Abilities[k].AbilityIndex);
            //                    *data << uint8(Players[i].Pets[j].Abilities[k].Pboid);
            //                }

            //                for (std::size_t k = 0; k < Players[i].Pets[j].Auras.size(); ++k)
            //                {
            //                    *data << int32(Players[i].Pets[j].Auras[k].AbilityID);
            //                    *data << uint32(Players[i].Pets[j].Auras[k].InstanceID);
            //                    *data << int32(Players[i].Pets[j].Auras[k].RoundsRemaining);
            //                    *data << int32(Players[i].Pets[j].Auras[k].CurrentRound);
            //                    *data << uint8(Players[i].Pets[j].Auras[k].CasterPBOID);
            //                }

            //                for (std::size_t k = 0; k < Players[i].Pets[j].States.size(); ++k)
            //                {
            //                    *data << uint32(Players[i].Pets[j].States[k].StateID);
            //                    *data << int32(Players[i].Pets[j].States[k].StateValue);
            //                }

            //                data.WriteBits(Players[i].Pets[j].CustomName.length(), 7);
            //                data.FlushBits();
            //                data.WriteString(Players[i].Pets[j].CustomName);
            //            }
            //        }

            //        for (std::size_t i = 0; i < 3; ++i)
            //        {
            //            *data << uint32(Enviros[j].Auras.size());
            //            *data << uint32(Enviros[j].States.size());
            //            for (std::size_t j = 0; j < Enviros[j].Auras.size(); ++j)
            //            {
            //                *data << int32(Enviros[j].Auras[j].AbilityID);
            //                *data << uint32(Enviros[j].Auras[j].InstanceID);
            //                *data << int32(Enviros[j].Auras[j].RoundsRemaining);
            //                *data << int32(Enviros[j].Auras[j].CurrentRound);
            //                *data << uint8(Enviros[j].Auras[j].CasterPBOID);
            //            }

            //            for (std::size_t j = 0; j < Enviros[j].States.size(); ++j)
            //            {
            //                *data << uint32(Enviros[i].States[j].StateID);
            //                *data << int32(Enviros[i].States[j].StateValue);
            //            }
            //        }

            //        *data << uint16(WaitingForFrontPetsMaxSecs);
            //        *data << uint16(PvpMaxRoundTime);
            //        *data << int32(CurRound);
            //        *data << uint32(NpcCreatureID);
            //        *data << uint32(NpcDisplayID);
            //        *data << int8(CurPetBattleState);
            //        *data << uint8(ForfeitPenalty);
            //        *data << ObjectGuid(InitialWildPetGUID);
            //        data.WriteBit(IsPVP);
            //        data.WriteBit(CanAwardXP);
            //        data.FlushBits();
            //    }
            //}

            if (m_createBits.ActivePlayer)
            {
                bool hasSceneInstanceIDs = false;
                bool hasRuneState = false;
                bool hasUnkBCC = false;

                data.WriteBit(hasSceneInstanceIDs);
                data.WriteBit(hasRuneState);
                data.WriteBit(hasUnkBCC);
                data.FlushBits();
                //if (HasSceneInstanceIDs)
                //{
                //    *data << uint32(SceneInstanceIDs.size());
                //    for (std::size_t i = 0; i < SceneInstanceIDs.size(); ++i)
                //        *data << uint32(SceneInstanceIDs[i]);
                //}
                //if (hasRuneState)
                //{
                //    *data << uint8((1 << maxRunes) - 1);
                //    *data << uint8(player->GetRunesState());
                //    *data << uint32(maxRunes);
                //    for (std::size_t i = 0; i < maxRunes; ++i)
                //        *data << uint8((baseCd - float(player->GetruneCooldown(i))) / baseCd * 255);
                //}
                //if(hasUnkBCC)
                //{
                //    for (std::size_t i = 0; i < 132; ++i)
                //        *data << int32(0);
                //}
            }

            /*
            if (m_createBits.Conversation)
            {
                Conversation self = ToConversation();
                if (data.WriteBit(self.GetTextureKitId() != 0))
                    data.WriteUInt32(self.GetTextureKitId());
                data.FlushBits();
            }
            */
        }

        public static void WriteTransportInfo(WorldPacket data, MovementInfo moveInfo)
        {
            bool hasPrevTime = false;
            bool hasVehicleId = moveInfo.VehicleId != 0;

            data.WritePackedGuid128(moveInfo.TransportGuid.To128()); // Transport Guid
            data.WriteFloat(moveInfo.TransportOffset.X);
            data.WriteFloat(moveInfo.TransportOffset.Y);
            data.WriteFloat(moveInfo.TransportOffset.Z);
            data.WriteFloat(moveInfo.TransportOffset.W);
            data.WriteInt8(moveInfo.TransportSeat);                  // VehicleSeatIndex
            data.WriteUInt32(moveInfo.TransportTime);                // MoveTime

            data.WriteBit(hasPrevTime);
            data.WriteBit(hasVehicleId);
            data.FlushBits();

            if (hasPrevTime)
                data.WriteUInt32(0);                                 // PrevMoveTime

            if (hasVehicleId)
                data.WriteUInt32(moveInfo.VehicleId);                // VehicleRecID
        }

        public static void WriteCreateObjectSplineDataBlock(ServerSideMovement moveSpline, WorldPacket data)
        {
            data.WriteUInt32(moveSpline.SplineId);                                          // ID

            //if (!moveSpline.IsCyclic())                                                     // Destination
            //    data.WriteVector3(moveSpline.FinalDestination());
            //else
                data.WriteVector3(Vector3.Zero);

            bool hasSplineMove = data.WriteBit(moveSpline.SplineCount != 0);
            data.FlushBits();

            if (hasSplineMove)
            {
                data.WriteUInt32((uint)moveSpline.SplineFlags);                             // SplineFlags
                data.WriteUInt32(moveSpline.SplineTime);                                    // Elapsed
                data.WriteUInt32(moveSpline.SplineTimeFull);                                // Duration
                data.WriteFloat(1.0f);                                                      // DurationModifier
                data.WriteFloat(1.0f);                                                      // NextDurationModifier
                data.WriteBits((byte)moveSpline.SplineType, 2);                             // Face
                bool hasFadeObjectTime = data.WriteBit(false);
                data.WriteBits(moveSpline.SplineCount, 16);
                data.WriteBit(false);                                                       // HasSplineFilter
                data.WriteBit(false);                                                       // HasSpellEffectExtraData
                data.WriteBit(false);                                                       // HasJumpExtraData
                data.WriteBit(false);                                                       // HasAnimationTierTransition
                data.FlushBits();

                //if (HasSplineFilterKey)
                //{
                //    data << uint32(FilterKeysCount);
                //    for (var i = 0; i < FilterKeysCount; ++i)
                //    {
                //        data << float(In);
                //        data << float(Out);
                //    }

                //    data.WriteBits(FilterFlags, 2);
                //    data.FlushBits();
                //}

                switch (moveSpline.SplineType)
                {
                    case Enums.SplineTypeModern.FacingSpot:
                        data.WriteVector3(moveSpline.FinalFacingSpot);  // FaceSpot
                        break;
                    case Enums.SplineTypeModern.FacingTarget:
                        data.WritePackedGuid128(moveSpline.FinalFacingGuid.To128()); // FaceGUID
                        break;
                    case Enums.SplineTypeModern.FacingAngle:
                        data.WriteFloat(moveSpline.FinalOrientation);   // FaceDirection
                        break;
                }

                if (hasFadeObjectTime)
                    data.WriteInt32(0); // FadeObjectTime

                foreach (var vec in moveSpline.SplinePoints)
                    data.WriteVector3(vec);

                /*
                if (moveSpline.spell_effect_extra.HasValue)
                {
                    data.WritePackedGuid(moveSpline.spell_effect_extra.Value.Target);
                    data.WriteUInt32(moveSpline.spell_effect_extra.Value.SpellVisualId);
                    data.WriteUInt32(moveSpline.spell_effect_extra.Value.ProgressCurveId);
                    data.WriteUInt32(moveSpline.spell_effect_extra.Value.ParabolicCurveId);
                }
                
                if (moveSpline.splineflags.HasFlag(SplineFlag.Parabolic))
                {
                    data.WriteFloat(moveSpline.vertical_acceleration);
                    data.WriteInt32(moveSpline.effect_start_time);
                    data.WriteUInt32(0);                                                  // Duration (override)
                }

                if (moveSpline.anim_tier.HasValue)
                {
                    data.WriteUInt32(moveSpline.anim_tier.Value.TierTransitionId);
                    data.WriteInt32(moveSpline.effect_start_time);
                    data.WriteUInt32(0);
                    data.WriteUInt8(moveSpline.anim_tier.Value.AnimTier);
                }*/
            }
        }

        public void WriteValuesToArray()
        {
            if (m_alreadyWritten)
                return;

            ObjectData objectData = m_updateData.ObjectData;
            if (objectData.Guid != null)
                m_fields.SetUpdateField<WowGuid128>(ObjectField.OBJECT_FIELD_GUID, objectData.Guid);
            if (objectData.EntryID != null)
                m_fields.SetUpdateField<int>(ObjectField.OBJECT_FIELD_ENTRY, (int)objectData.EntryID);
            if (objectData.DynamicFlags != null)
                m_fields.SetUpdateField<uint>(ObjectField.OBJECT_DYNAMIC_FLAGS, (uint)objectData.DynamicFlags);
            if (objectData.Scale != null)
                m_fields.SetUpdateField<float>(ObjectField.OBJECT_DYNAMIC_FLAGS, (float)objectData.Scale);

            UnitData unitData = m_updateData.UnitData;
            if (unitData.Charm != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_CHARM, unitData.Charm);
            if (unitData.Summon != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_SUMMON, unitData.Summon);
            if (unitData.Critter != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_CRITTER, unitData.Critter);
            if (unitData.CharmedBy != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_CHARMEDBY, unitData.CharmedBy);
            if (unitData.SummonedBy != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_SUMMONEDBY, unitData.SummonedBy);
            if (unitData.CreatedBy != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_CREATEDBY, unitData.CreatedBy);
            if (unitData.DemonCreator != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_DEMON_CREATOR, unitData.DemonCreator);
            if (unitData.LookAtControllerTarget != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_LOOK_AT_CONTROLLER_TARGET, unitData.LookAtControllerTarget);
            if (unitData.Target != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_TARGET, unitData.Target);
            if (unitData.BattlePetCompanionGUID != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_BATTLE_PET_COMPANION_GUID, unitData.BattlePetCompanionGUID);
            if (unitData.BattlePetDBID != null)
                m_fields.SetUpdateField<ulong>(UnitField.UNIT_FIELD_BATTLE_PET_DB_ID, (ulong)unitData.BattlePetDBID);
            if (unitData.ChannelData != null)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_CHANNEL_DATA;
                m_fields.SetUpdateField<int>(startIndex, (int)unitData.ChannelData.SpellID);
                m_fields.SetUpdateField<int>(startIndex + 1, (int)unitData.ChannelData.SpellXSpellVisualID);
            }
            if (unitData.SummonedByHomeRealm != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_SUMMONED_BY_HOME_REALM, (uint)unitData.SummonedByHomeRealm);
            if (unitData.RaceId != null || unitData.ClassId != null || unitData.PlayerClassId != null || unitData.SexId != null)
            {
                if (unitData.RaceId != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_0, (byte)unitData.RaceId, 0);
                if (unitData.ClassId != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_0, (byte)unitData.ClassId, 1);
                if (unitData.PlayerClassId != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_0, (byte)unitData.PlayerClassId, 2);
                if (unitData.SexId != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_0, (byte)unitData.SexId, 3);
            }
            if (unitData.DisplayPower != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_DISPLAY_POWER, (uint)unitData.DisplayPower);
            if (unitData.OverrideDisplayPowerID != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_OVERRIDE_DISPLAY_POWER_ID, (uint)unitData.OverrideDisplayPowerID);
            if (unitData.Health != null)
                m_fields.SetUpdateField<long>(UnitField.UNIT_FIELD_HEALTH, (long)unitData.Health);
            for (int i = 0; i < 6; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_POWER;
                if (unitData.Power[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)unitData.Power[i]);
            }
            if (unitData.MaxHealth != null)
                m_fields.SetUpdateField<long>(UnitField.UNIT_FIELD_MAXHEALTH, (long)unitData.MaxHealth);
            for (int i = 0; i < 6; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_MAXPOWER;
                if (unitData.MaxPower[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)unitData.MaxPower[i]);
            }
            for (int i = 0; i < 6; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_MOD_POWER_REGEN;
                if (unitData.ModPowerRegen[i] != null)
                    m_fields.SetUpdateField<float>(startIndex + i, (float)unitData.ModPowerRegen[i]);
            }
            if (unitData.Level != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_LEVEL, (int)unitData.Level);
            if (unitData.EffectiveLevel != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_EFFECTIVE_LEVEL, (int)unitData.EffectiveLevel);
            if (unitData.ContentTuningID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_CONTENT_TUNING_ID, (int)unitData.ContentTuningID);
            if (unitData.ScalingLevelMin != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_SCALING_LEVEL_MIN, (int)unitData.ScalingLevelMin);
            if (unitData.ScalingLevelMax != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_SCALING_LEVEL_MAX, (int)unitData.ScalingLevelMax);
            if (unitData.ScalingLevelDelta != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_SCALING_LEVEL_DELTA, (int)unitData.ScalingLevelDelta);
            if (unitData.ScalingFactionGroup != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_SCALING_FACTION_GROUP, (int)unitData.ScalingFactionGroup);
            if (unitData.ScalingHealthItemLevelCurveID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_SCALING_HEALTH_ITEM_LEVEL_CURVE_ID, (int)unitData.ScalingHealthItemLevelCurveID);
            if (unitData.ScalingDamageItemLevelCurveID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_SCALING_DAMAGE_ITEM_LEVEL_CURVE_ID, (int)unitData.ScalingDamageItemLevelCurveID);
            if (unitData.FactionTemplate != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_FACTIONTEMPLATE, (int)unitData.FactionTemplate);
            for (int i = 0; i < 3; i++)
            {
                int startIndex = (int)UnitField.UNIT_VIRTUAL_ITEM_SLOT_ID;
                int sizePerEntry = 2;
                if (unitData.VirtualItems[i] != null)
                {
                    m_fields.SetUpdateField<int>(startIndex + i * sizePerEntry, (int)unitData.VirtualItems[i].ItemID);
                    m_fields.SetUpdateField<ushort>(startIndex + i * sizePerEntry + 1, (ushort)unitData.VirtualItems[i].ItemAppearanceModID, 0);
                    m_fields.SetUpdateField<ushort>(startIndex + i * sizePerEntry + 1, (ushort)unitData.VirtualItems[i].ItemVisual, 1);
                }
            }
            if (unitData.Flags != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_FLAGS, (uint)unitData.Flags);
            if (unitData.Flags2 != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_FLAGS_2, (uint)unitData.Flags2);
            if (unitData.Flags3 != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_FLAGS_3, (uint)unitData.Flags3);
            if (unitData.AuraState != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_AURASTATE, (uint)unitData.AuraState);
            for (int i = 0; i < 2; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_BASEATTACKTIME;
                if (unitData.AttackRoundBaseTime[i] != null)
                    m_fields.SetUpdateField<uint>(startIndex + i, (uint)unitData.AttackRoundBaseTime[i]);
            }
            if (unitData.RangedAttackRoundBaseTime != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_RANGEDATTACKTIME, (uint)unitData.RangedAttackRoundBaseTime);
            if (unitData.BoundingRadius != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_BOUNDINGRADIUS, (float)unitData.BoundingRadius);
            if (unitData.CombatReach != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_COMBATREACH, (float)unitData.CombatReach);
            if (unitData.DisplayID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_DISPLAYID, (int)unitData.DisplayID);
            if (unitData.DisplayScale != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_DISPLAYSCALE, (float)unitData.DisplayScale);
            if (unitData.NativeDisplayID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_NATIVEDISPLAYID, (int)unitData.NativeDisplayID);
            if (unitData.NativeXDisplayScale != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_NATIVEXDISPLAYSCALE, (float)unitData.NativeXDisplayScale);
            if (unitData.MountDisplayID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_MOUNTDISPLAYID, (int)unitData.MountDisplayID);
            if (unitData.MinDamage != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MINDAMAGE, (float)unitData.MinDamage);
            if (unitData.MaxDamage != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MAXDAMAGE, (float)unitData.MaxDamage);
            if (unitData.MinOffHandDamage != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MINOFFHANDDAMAGE, (float)unitData.MinOffHandDamage);
            if (unitData.MaxOffHandDamage != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MAXOFFHANDDAMAGE, (float)unitData.MaxOffHandDamage);
            if (unitData.StandState != null || unitData.PetLoyaltyIndex != null || unitData.VisFlags != null || unitData.AnimTier != null)
            {
                if (unitData.StandState != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_1, (byte)unitData.StandState, 0);
                if (unitData.PetLoyaltyIndex != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_1, (byte)unitData.PetLoyaltyIndex, 1);
                if (unitData.VisFlags != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_1, (byte)unitData.VisFlags, 2);
                if (unitData.AnimTier != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_1, (byte)unitData.AnimTier, 3);
            }
            if (unitData.PetNumber != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_PETNUMBER, (uint)unitData.PetNumber);
            if (unitData.PetNameTimestamp != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_PET_NAME_TIMESTAMP, (uint)unitData.PetNameTimestamp);
            if (unitData.PetExperience != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_PETEXPERIENCE, (uint)unitData.PetExperience);
            if (unitData.PetNextLevelExperience != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_PETNEXTLEVELEXPERIENCE, (uint)unitData.PetNextLevelExperience);
            if (unitData.ModCastSpeed != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_MOD_CAST_SPEED, (float)unitData.ModCastSpeed);
            if (unitData.ModCastHaste != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_MOD_CAST_HASTE, (float)unitData.ModCastHaste);
            if (unitData.ModHaste != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MOD_HASTE, (float)unitData.ModHaste);
            if (unitData.ModRangedHaste != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MOD_RANGED_HASTE, (float)unitData.ModRangedHaste);
            if (unitData.ModHasteRegen != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MOD_HASTE_REGEN, (float)unitData.ModHasteRegen);
            if (unitData.ModTimeRate != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MOD_TIME_RATE, (float)unitData.ModTimeRate);
            if (unitData.CreatedBySpell != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_CREATED_BY_SPELL, (int)unitData.CreatedBySpell);
            for (int i = 0; i < 2; i++)
            {
                int startIndex = (int)UnitField.UNIT_NPC_FLAGS;
                if (unitData.NpcFlags[i] != null)
                    m_fields.SetUpdateField<uint>(startIndex + i, (uint)unitData.NpcFlags[i]);
            }
            if (unitData.EmoteState != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_NPC_EMOTESTATE, (int)unitData.EmoteState);
            if (unitData.TrainingPointsUsed != null && unitData.TrainingPointsTotal != null)
            {
                m_fields.SetUpdateField<ushort>(UnitField.UNIT_FIELD_TRAINING_POINTS_TOTAL, (ushort)unitData.TrainingPointsUsed, 0);
                m_fields.SetUpdateField<ushort>(UnitField.UNIT_FIELD_TRAINING_POINTS_TOTAL, (ushort)unitData.TrainingPointsTotal, 1);
            }
            for (int i = 0; i < 5; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_STAT;
                if (unitData.Stats[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)unitData.Stats[i]);
            }
            for (int i = 0; i < 5; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_POSSTAT;
                if (unitData.StatPosBuff[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)unitData.StatPosBuff[i]);
            }
            for (int i = 0; i < 5; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_NEGSTAT;
                if (unitData.StatNegBuff[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)unitData.StatNegBuff[i]);
            }
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_RESISTANCES;
                if (unitData.Resistances[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)unitData.Resistances[i]);
            }
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_RESISTANCEBUFFMODSPOSITIVE;
                if (unitData.ResistanceBuffModsPositive[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)unitData.ResistanceBuffModsPositive[i]);
            }
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_RESISTANCEBUFFMODSNEGATIVE;
                if (unitData.ResistanceBuffModsNegative[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)unitData.ResistanceBuffModsNegative[i]);
            }
            if (unitData.BaseMana != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_BASE_MANA, (int)unitData.BaseMana);
            if (unitData.BaseHealth != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_BASE_HEALTH, (int)unitData.BaseHealth);
            if (unitData.SheatheState != null || unitData.PvpFlags != null || unitData.PetFlags != null || unitData.ShapeshiftForm != null)
            {
                if (unitData.SheatheState != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_2, (byte)unitData.SheatheState, 0);
                if (unitData.PvpFlags != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_2, (byte)unitData.PvpFlags, 1);
                if (unitData.PetFlags != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_2, (byte)unitData.PetFlags, 2);
                if (unitData.ShapeshiftForm != null)
                    m_fields.SetUpdateField<byte>(UnitField.UNIT_FIELD_BYTES_2, (byte)unitData.ShapeshiftForm, 3);
            }
            if (unitData.AttackPower != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_ATTACK_POWER, (int)unitData.AttackPower);
            if (unitData.AttackPowerModPos != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_ATTACK_POWER_MOD_POS, (int)unitData.AttackPowerModPos);
            if (unitData.AttackPowerModNeg != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_ATTACK_POWER_MOD_NEG, (int)unitData.AttackPowerModNeg);
            if (unitData.AttackPowerMultiplier != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_ATTACK_POWER_MULTIPLIER, (float)unitData.AttackPowerMultiplier);
            if (unitData.RangedAttackPower != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_RANGED_ATTACK_POWER, (int)unitData.RangedAttackPower);
            if (unitData.RangedAttackPowerModPos != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_RANGED_ATTACK_POWER_MOD_POS, (int)unitData.RangedAttackPowerModPos);
            if (unitData.RangedAttackPowerModNeg != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_RANGED_ATTACK_POWER_MOD_NEG, (int)unitData.RangedAttackPowerModNeg);
            if (unitData.RangedAttackPowerMultiplier != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_RANGED_ATTACK_POWER_MULTIPLIER, (float)unitData.RangedAttackPowerMultiplier);
            if (unitData.AttackSpeedAura != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_ATTACK_SPEED_AURA, (int)unitData.AttackSpeedAura);
            if (unitData.Lifesteal != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_LIFESTEAL, (float)unitData.Lifesteal);
            if (unitData.MinRangedDamage != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MINRANGEDDAMAGE, (float)unitData.MinRangedDamage);
            if (unitData.MaxRangedDamage != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MAXRANGEDDAMAGE, (float)unitData.MaxRangedDamage);
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_POWER_COST_MODIFIER;
                if (unitData.PowerCostModifier[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)unitData.PowerCostModifier[i]);
            }
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)UnitField.UNIT_FIELD_POWER_COST_MULTIPLIER;
                if (unitData.PowerCostMultiplier[i] != null)
                    m_fields.SetUpdateField<float>(startIndex + i, (float)unitData.PowerCostMultiplier[i]);
            }
            if (unitData.MaxHealthModifier != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_MAXHEALTHMODIFIER, (float)unitData.MaxHealthModifier);
            if (unitData.HoverHeight != null)
                m_fields.SetUpdateField<float>(UnitField.UNIT_FIELD_HOVERHEIGHT, (float)unitData.HoverHeight);
            if (unitData.MinItemLevelCutoff != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_MIN_ITEM_LEVEL_CUTOFF, (int)unitData.MinItemLevelCutoff);
            if (unitData.MinItemLevel != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_MIN_ITEM_LEVEL, (int)unitData.MinItemLevel);
            if (unitData.MaxItemLevel != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_MAXITEMLEVEL, (int)unitData.MaxItemLevel);
            if (unitData.WildBattlePetLevel != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_WILD_BATTLE_PET_LEVEL, (int)unitData.WildBattlePetLevel);
            if (unitData.BattlePetCompanionNameTimestamp != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_BATTLEPET_COMPANION_NAME_TIMESTAMP, (uint)unitData.BattlePetCompanionNameTimestamp);
            if (unitData.InteractSpellID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_INTERACT_SPELL_ID, (int)unitData.InteractSpellID);
            if (unitData.StateSpellVisualID != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_STATE_SPELL_VISUAL_ID, (uint)unitData.StateSpellVisualID);
            if (unitData.StateAnimID != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_STATE_ANIM_ID, (uint)unitData.StateAnimID);
            if (unitData.StateAnimKitID != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_STATE_ANIM_KIT_ID, (uint)unitData.StateAnimKitID);
            if (unitData.StateWorldEffectsID != null)
                m_fields.SetUpdateField<uint>(UnitField.UNIT_FIELD_STATE_WORLD_EFFECT_ID, (uint)unitData.StateWorldEffectsID);
            if (unitData.ScaleDuration != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_SCALE_DURATION, (int)unitData.ScaleDuration);
            if (unitData.LooksLikeMountID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_LOOKS_LIKE_MOUNT_ID, (int)unitData.LooksLikeMountID);
            if (unitData.LooksLikeCreatureID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_LOOKS_LIKE_CREATURE_ID, (int)unitData.LooksLikeCreatureID);
            if (unitData.LookAtControllerID != null)
                m_fields.SetUpdateField<int>(UnitField.UNIT_FIELD_LOOK_AT_CONTROLLER_ID, (int)unitData.LookAtControllerID);
            if (unitData.GuildGUID != null)
                m_fields.SetUpdateField<WowGuid128>(UnitField.UNIT_FIELD_GUILD_GUID, unitData.GuildGUID);

            PlayerData playerData = m_updateData.PlayerData;
            if (playerData.DuelArbiter != null)
                m_fields.SetUpdateField<WowGuid128>(PlayerField.PLAYER_DUEL_ARBITER, playerData.DuelArbiter);
            if (playerData.WowAccount != null)
                m_fields.SetUpdateField<WowGuid128>(PlayerField.PLAYER_WOW_ACCOUNT, playerData.WowAccount);
            if (playerData.LootTargetGUID != null)
                m_fields.SetUpdateField<WowGuid128>(PlayerField.PLAYER_LOOT_TARGET_GUID, playerData.LootTargetGUID);
            if (playerData.PlayerFlags != null)
                m_fields.SetUpdateField<uint>(PlayerField.PLAYER_FLAGS, (uint)playerData.PlayerFlags);
            if (playerData.PlayerFlagsEx != null)
                m_fields.SetUpdateField<uint>(PlayerField.PLAYER_FLAGS_EX, (uint)playerData.PlayerFlagsEx);
            if (playerData.GuildRankID != null)
                m_fields.SetUpdateField<uint>(PlayerField.PLAYER_GUILDRANK, (uint)playerData.GuildRankID);
            if (playerData.GuildDeleteDate != null)
                m_fields.SetUpdateField<uint>(PlayerField.PLAYER_GUILDDELETE_DATE, (uint)playerData.GuildDeleteDate);
            if (playerData.GuildLevel != null)
                m_fields.SetUpdateField<int>(PlayerField.PLAYER_GUILDLEVEL, (int)playerData.GuildLevel);
            if (playerData.PartyType != null || playerData.NumBankSlots != null || playerData.NativeSex != null || playerData.Inebriation != null)
            {
                if (playerData.PartyType != null)
                    m_fields.SetUpdateField<byte>(PlayerField.PLAYER_BYTES, (byte)playerData.PartyType, 0);
                if (playerData.NumBankSlots != null)
                    m_fields.SetUpdateField<byte>(PlayerField.PLAYER_BYTES, (byte)playerData.NumBankSlots, 1);
                if (playerData.NativeSex != null)
                    m_fields.SetUpdateField<byte>(PlayerField.PLAYER_BYTES, (byte)playerData.NativeSex, 2);
                if (playerData.Inebriation != null)
                    m_fields.SetUpdateField<byte>(PlayerField.PLAYER_BYTES, (byte)playerData.Inebriation, 3);
            }
            if (playerData.PvpTitle != null || playerData.ArenaFaction != null || playerData.PvPRank != null)
            {
                if (playerData.PvpTitle != null)
                    m_fields.SetUpdateField<byte>(PlayerField.PLAYER_BYTES_2, (byte)playerData.PvpTitle, 0);
                if (playerData.ArenaFaction != null)
                    m_fields.SetUpdateField<byte>(PlayerField.PLAYER_BYTES_2, (byte)playerData.ArenaFaction, 1);
                if (playerData.PvPRank != null)
                    m_fields.SetUpdateField<byte>(PlayerField.PLAYER_BYTES_2, (byte)playerData.PvPRank, 2);
            }
            if (playerData.DuelTeam != null)
                m_fields.SetUpdateField<uint>(PlayerField.PLAYER_DUEL_TEAM, (uint)playerData.DuelTeam);
            if (playerData.GuildTimeStamp != null)
                m_fields.SetUpdateField<int>(PlayerField.PLAYER_GUILD_TIMESTAMP, (int)playerData.GuildTimeStamp);
            for (int i = 0; i < 25; i++)
            {
                int startIndex = (int)PlayerField.PLAYER_QUEST_LOG;
                int sizePerEntry = 16;
                if (playerData.QuestLog[i] != null)
                {
                    if (playerData.QuestLog[i].QuestID != null)
                        m_fields.SetUpdateField<int>(startIndex + i * sizePerEntry, (int)playerData.QuestLog[i].QuestID);
                    if (playerData.QuestLog[i].StateFlags != null)
                        m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 1, (uint)playerData.QuestLog[i].StateFlags);
                    if (playerData.QuestLog[i].EndTime != null)
                        m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 2, (uint)playerData.QuestLog[i].EndTime);
                    if (playerData.QuestLog[i].AcceptTime != null)
                        m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 3, (uint)playerData.QuestLog[i].AcceptTime);
                    for (int j = 0; j < 24; j++)
                    {
                        if (playerData.QuestLog[i].ObjectiveProgress[j] != null)
                            m_fields.SetUpdateField<ushort>(startIndex + i * sizePerEntry + 4 + j / 2, (ushort)playerData.QuestLog[i].ObjectiveProgress[j], (byte)(j & 1));
                    }
                }
            }
            for (int i = 0; i < 19; i++)
            {
                int startIndex = (int)PlayerField.PLAYER_VISIBLE_ITEM;
                int sizePerEntry = 2;
                if (playerData.VisibleItems[i] != null)
                {
                    m_fields.SetUpdateField<int>(startIndex + i * sizePerEntry, (int)playerData.VisibleItems[i].ItemID);
                    m_fields.SetUpdateField<ushort>(startIndex + i * sizePerEntry + 1, (ushort)playerData.VisibleItems[i].ItemAppearanceModID, 0);
                    m_fields.SetUpdateField<ushort>(startIndex + i * sizePerEntry + 1, (ushort)playerData.VisibleItems[i].ItemVisual, 1);
                }
            }
            if (playerData.ChosenTitle != null)
                m_fields.SetUpdateField<int>(PlayerField.PLAYER_CHOSEN_TITLE, (int)playerData.ChosenTitle);
            if (playerData.FakeInebriation != null)
                m_fields.SetUpdateField<int>(PlayerField.PLAYER_FAKE_INEBRIATION, (int)playerData.FakeInebriation);
            if (playerData.VirtualPlayerRealm != null)
                m_fields.SetUpdateField<uint>(PlayerField.PLAYER_FIELD_VIRTUAL_PLAYER_REALM, (uint)playerData.VirtualPlayerRealm);
            if (playerData.CurrentSpecID != null)
                m_fields.SetUpdateField<uint>(PlayerField.PLAYER_FIELD_CURRENT_SPEC_ID, (uint)playerData.CurrentSpecID);
            if (playerData.TaxiMountAnimKitID != null)
                m_fields.SetUpdateField<int>(PlayerField.PLAYER_FIELD_TAXI_MOUNT_ANIM_KIT_ID, (int)playerData.TaxiMountAnimKitID);
            for (int i = 0; i < 6; i++)
            {
                int startIndex = (int)PlayerField.PLAYER_FIELD_AVG_ITEM_LEVEL;
                if (playerData.AvgItemLevel[i] != null)
                    m_fields.SetUpdateField<float>(startIndex + i, (float)playerData.AvgItemLevel[i]);
            }
            if (playerData.CurrentBattlePetBreedQuality != null)
                m_fields.SetUpdateField<uint>(PlayerField.PLAYER_FIELD_CURRENT_BATTLE_PET_BREED_QUALITY, (uint)playerData.CurrentBattlePetBreedQuality);
            if (playerData.HonorLevel != null)
                m_fields.SetUpdateField<int>(PlayerField.PLAYER_FIELD_HONOR_LEVEL, (int)playerData.HonorLevel);
            for (int i = 0; i < 36; i++)
            {
                int startIndex = (int)PlayerField.PLAYER_FIELD_CUSTOMIZATION_CHOICES;
                int sizePerEntry = 2;
                if (playerData.Customizations[i] != null)
                {
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry, (uint)playerData.Customizations[i].ChrCustomizationOptionID);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 1, (uint)playerData.Customizations[i].ChrCustomizationChoiceID);
                }
            }

            ActivePlayerData activeData = m_updateData.ActivePlayerData;
            for (int i =0; i < 129; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_INV_SLOT_HEAD;
                int sizePerEntry = 4;
                if (activeData.InvSlots[i] != null)
                    m_fields.SetUpdateField<WowGuid128>(startIndex + i * sizePerEntry, activeData.InvSlots[i]);
            }
            if (activeData.FarsightObject != null)
                m_fields.SetUpdateField<WowGuid128>(ActivePlayerField.ACTIVE_PLAYER_FIELD_FARSIGHT, activeData.FarsightObject);
            if (activeData.ComboTarget != null)
                m_fields.SetUpdateField<WowGuid128>(ActivePlayerField.ACTIVE_PLAYER_FIELD_COMBO_TARGET, activeData.ComboTarget);
            if (activeData.SummonedBattlePetGUID != null)
                m_fields.SetUpdateField<WowGuid128>(ActivePlayerField.ACTIVE_PLAYER_FIELD_SUMMONED_BATTLE_PET_ID, activeData.SummonedBattlePetGUID);
            for (int i = 0; i < 12; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_KNOWN_TITLES;
                if (activeData.KnownTitles[i] != null)
                    m_fields.SetUpdateField<uint>(startIndex + i, (uint)activeData.KnownTitles[i]);
            }
            if (activeData.Coinage != null)
                m_fields.SetUpdateField<ulong>(ActivePlayerField.ACTIVE_PLAYER_FIELD_COINAGE, (ulong)activeData.Coinage);
            if (activeData.XP != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_XP, (int)activeData.XP);
            if (activeData.NextLevelXP != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_NEXT_LEVEL_XP, (int)activeData.NextLevelXP);
            if (activeData.TrialXP != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_TRIAL_XP, (int)activeData.TrialXP);
            for (int i = 0; i < 256; i++)
            {
                if (activeData.Skill.SkillLineID[i] != null)
                {
                    int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_SKILL_LINEID;
                    m_fields.SetUpdateField<ushort>(startIndex + i / 2, (ushort)activeData.Skill.SkillLineID[i], (byte)(i & 1));
                }
                if (activeData.Skill.SkillStep[i] != null)
                {
                    int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_SKILL_LINEID + 128;
                    m_fields.SetUpdateField<ushort>(startIndex + i / 2, (ushort)activeData.Skill.SkillStep[i], (byte)(i & 1));
                }
                if (activeData.Skill.SkillRank[i] != null)
                {
                    int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_SKILL_LINEID + 128 + 128;
                    m_fields.SetUpdateField<ushort>(startIndex + i / 2, (ushort)activeData.Skill.SkillRank[i], (byte)(i & 1));
                }
                if (activeData.Skill.SkillStartingRank[i] != null)
                {
                    int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_SKILL_LINEID + 128 + 128 + 128;
                    m_fields.SetUpdateField<ushort>(startIndex + i / 2, (ushort)activeData.Skill.SkillStartingRank[i], (byte)(i & 1));
                }
                if (activeData.Skill.SkillMaxRank[i] != null)
                {
                    int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_SKILL_LINEID + 128 + 128 + 128 + 128;
                    m_fields.SetUpdateField<ushort>(startIndex + i / 2, (ushort)activeData.Skill.SkillMaxRank[i], (byte)(i & 1));
                }
                if (activeData.Skill.SkillTempBonus[i] != null)
                {
                    int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_SKILL_LINEID + 128 + 128 + 128 + 128 + 128;
                    m_fields.SetUpdateField<ushort>(startIndex + i / 2, (ushort)activeData.Skill.SkillTempBonus[i], (byte)(i & 1));
                }
                if (activeData.Skill.SkillPermBonus[i] != null)
                {
                    int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_SKILL_LINEID + 128 + 128 + 128 + 128 + 128 + 128;
                    m_fields.SetUpdateField<ushort>(startIndex + i / 2, (ushort)activeData.Skill.SkillPermBonus[i], (byte)(i & 1));
                }
            }
            if (activeData.CharacterPoints != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_CHARACTER_POINTS, (int)activeData.CharacterPoints);
            if (activeData.MaxTalentTiers != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MAX_TALENT_TIERS, (int)activeData.MaxTalentTiers);
            if (activeData.TrackCreatureMask != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_TRACK_CREATURES, (uint)activeData.TrackCreatureMask);
            for (int i = 0; i < 2; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_TRACK_RESOURCES;
                if (activeData.TrackResourceMask[i] != null)
                    m_fields.SetUpdateField<uint>(startIndex + i, (uint)activeData.TrackResourceMask[i]);
            }
            if (activeData.MainhandExpertise != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_EXPERTISE, (float)activeData.MainhandExpertise);
            if (activeData.OffhandExpertise != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_OFFHAND_EXPERTISE, (float)activeData.OffhandExpertise);
            if (activeData.RangedExpertise != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_RANGED_EXPERTISE, (float)activeData.RangedExpertise);
            if (activeData.CombatRatingExpertise != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_COMBAT_RATING_EXPERTISE, (float)activeData.CombatRatingExpertise);
            if (activeData.BlockPercentage != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BLOCK_PERCENTAGE, (float)activeData.BlockPercentage);
            if (activeData.DodgePercentage != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_DODGE_PERCENTAGE, (float)activeData.DodgePercentage);
            if (activeData.DodgePercentageFromAttribute != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_DODGE_PERCENTAGE_FROM_ATTRIBUTE, (float)activeData.DodgePercentageFromAttribute);
            if (activeData.ParryPercentage != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_PARRY_PERCENTAGE, (float)activeData.ParryPercentage);
            if (activeData.ParryPercentageFromAttribute != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_PARRY_PERCENTAGE_FROM_ATTRIBUTE, (float)activeData.ParryPercentageFromAttribute);
            if (activeData.CritPercentage != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_CRIT_PERCENTAGE, (float)activeData.CritPercentage);
            if (activeData.RangedCritPercentage != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_RANGED_CRIT_PERCENTAGE, (float)activeData.RangedCritPercentage);
            if (activeData.OffhandCritPercentage != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_OFFHAND_CRIT_PERCENTAGE, (float)activeData.OffhandCritPercentage);
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_SPELL_CRIT_PERCENTAGE1;
                if (activeData.SpellCritPercentage[i] != null)
                    m_fields.SetUpdateField<float>(startIndex + i, (float)activeData.SpellCritPercentage[i]);
            }
            if (activeData.ShieldBlock != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_SHIELD_BLOCK, (int)activeData.ShieldBlock);
            if (activeData.Mastery != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MASTERY, (float)activeData.Mastery);
            if (activeData.Speed != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_SPEED, (float)activeData.Speed);
            if (activeData.Avoidance != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_AVOIDANCE, (float)activeData.Avoidance);
            if (activeData.Sturdiness != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_STURDINESS, (float)activeData.Sturdiness);
            if (activeData.Versatility != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_VERSATILITY, (int)activeData.Versatility);
            if (activeData.VersatilityBonus != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_VERSATILITY_BONUS, (float)activeData.VersatilityBonus);
            if (activeData.PvpPowerDamage != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_PVP_POWER_DAMAGE, (float)activeData.PvpPowerDamage);
            if (activeData.PvpPowerHealing != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_PVP_POWER_HEALING, (float)activeData.PvpPowerHealing);
            for (int i = 0; i < 240; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_EXPLORED_ZONES;
                if (activeData.ExploredZones[i] != null)
                    m_fields.SetUpdateField<ulong>(startIndex + i * 2, (ulong)activeData.ExploredZones[i]);
            }
            for (int i = 0; i < 2; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_REST_INFO;
                int sizePerEntry = 2;
                if (activeData.RestInfo[i] != null)
                {
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry, (uint)activeData.RestInfo[i].Threshold);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 1, (uint)activeData.RestInfo[i].StateID);
                }
            }
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_DAMAGE_DONE_POS;
                if (activeData.ModDamageDonePos[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)activeData.ModDamageDonePos[i]);
            }
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_DAMAGE_DONE_NEG;
                if (activeData.ModDamageDoneNeg[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)activeData.ModDamageDoneNeg[i]);
            }
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_DAMAGE_DONE_PCT;
                if (activeData.ModDamageDonePercent[i] != null)
                    m_fields.SetUpdateField<float>(startIndex + i, (float)activeData.ModDamageDonePercent[i]);
            }
            if (activeData.ModHealingDonePos != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_HEALING_DONE_POS, (int)activeData.ModHealingDonePos);
            if (activeData.ModHealingPercent != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_HEALING_PCT, (float)activeData.ModHealingPercent);
            if (activeData.ModHealingDonePercent != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_HEALING_DONE_PCT, (float)activeData.ModHealingDonePercent);
            if (activeData.ModPeriodicHealingDonePercent != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_PERIODIC_HEALING_DONE_PERCENT, (float)activeData.ModPeriodicHealingDonePercent);
            for (int i = 0; i < 3; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_WEAPON_DMG_MULTIPLIERS;
                if (activeData.WeaponDmgMultipliers[i] != null)
                    m_fields.SetUpdateField<float>(startIndex + i, (float)activeData.WeaponDmgMultipliers[i]);
            }
            for (int i = 0; i < 3; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_WEAPON_ATK_SPEED_MULTIPLIERS;
                if (activeData.WeaponAtkSpeedMultipliers[i] != null)
                    m_fields.SetUpdateField<float>(startIndex + i, (float)activeData.WeaponAtkSpeedMultipliers[i]);
            }
            if (activeData.ModSpellPowerPercent != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_SPELL_POWER_PCT, (float)activeData.ModSpellPowerPercent);
            if (activeData.ModResiliencePercent != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_RESILIENCE_PERCENT, (float)activeData.ModResiliencePercent);
            if (activeData.OverrideSpellPowerByAPPercent != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_OVERRIDE_SPELL_POWER_BY_AP_PCT, (float)activeData.OverrideSpellPowerByAPPercent);
            if (activeData.OverrideAPBySpellPowerPercent != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_OVERRIDE_AP_BY_SPELL_POWER_PERCENT, (float)activeData.OverrideAPBySpellPowerPercent);
            if (activeData.ModTargetResistance != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_TARGET_RESISTANCE, (int)activeData.ModTargetResistance);
            if (activeData.ModTargetPhysicalResistance != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_TARGET_PHYSICAL_RESISTANCE, (int)activeData.ModTargetPhysicalResistance);
            if (activeData.LocalFlags != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_LOCAL_FLAGS, (uint)activeData.LocalFlags);
            if (activeData.GrantableLevels != null || activeData.MultiActionBars != null || activeData.LifetimeMaxRank != null || activeData.NumRespecs != null)
            {
                if (activeData.GrantableLevels != null)
                    m_fields.SetUpdateField<byte>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES, (byte)activeData.GrantableLevels, 0);
                if (activeData.MultiActionBars != null)
                    m_fields.SetUpdateField<byte>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES, (byte)activeData.MultiActionBars, 1);
                if (activeData.LifetimeMaxRank != null)
                    m_fields.SetUpdateField<byte>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES, (byte)activeData.LifetimeMaxRank, 2);
                if (activeData.NumRespecs != null)
                    m_fields.SetUpdateField<byte>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES, (byte)activeData.NumRespecs, 3);
            }
            if (activeData.AmmoID != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_AMMO_ID, (uint)activeData.AmmoID);
            if (activeData.PvpMedals != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_PVP_MEDALS, (uint)activeData.PvpMedals);
            for (int i = 0; i < 12; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_BUYBACK_PRICE;
                if (activeData.BuybackPrice[i] != null)
                    m_fields.SetUpdateField<uint>(startIndex + i, (uint)activeData.BuybackPrice[i]);
            }
            for (int i = 0; i < 12; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_BUYBACK_TIMESTAMP;
                if (activeData.BuybackTimestamp[i] != null)
                    m_fields.SetUpdateField<uint>(startIndex + i, (uint)activeData.BuybackTimestamp[i]);
            }
            if (activeData.TodayHonorableKills != null && activeData.YesterdayHonorableKills != null)
            {
                m_fields.SetUpdateField<ushort>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES_2, (ushort)activeData.TodayHonorableKills, 0);
                m_fields.SetUpdateField<ushort>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES_2, (ushort)activeData.YesterdayHonorableKills, 1);
            }
            if (activeData.TodayHonorableKills != null && activeData.YesterdayHonorableKills != null)
            {
                m_fields.SetUpdateField<ushort>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES_3, (ushort)activeData.LastWeekHonorableKills, 0);
                m_fields.SetUpdateField<ushort>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES_3, (ushort)activeData.ThisWeekHonorableKills, 1);
            }
            if (activeData.ThisWeekContribution != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_THIS_WEEK_CONTRIBUTION, (uint)activeData.ThisWeekContribution);
            if (activeData.LifetimeHonorableKills != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_LIFETIME_HONORABLE_KILLS, (uint)activeData.LifetimeHonorableKills);
            if (activeData.YesterdayContribution != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_YESTERDAY_CONTRIBUTION, (uint)activeData.YesterdayContribution);
            if (activeData.LastWeekContribution != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_LAST_WEEK_CONTRIBUTION, (uint)activeData.LastWeekContribution);
            if (activeData.LastWeekRank != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_LAST_WEEK_RANK, (uint)activeData.LastWeekRank);
            if (activeData.WatchedFactionIndex != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_WATCHED_FACTION_INDEX, (int)activeData.WatchedFactionIndex);
            for (int i = 0; i < 32; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_COMBAT_RATINGS;
                if (activeData.CombatRatings[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)activeData.CombatRatings[i]);
            }
            for (int i = 0; i < 6; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_PVP_INFO;
                int sizePerEntry = 12;
                if (activeData.PvpInfo[i] != null)
                {
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry, (uint)activeData.PvpInfo[i].WeeklyPlayed);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 1, (uint)activeData.PvpInfo[i].WeeklyWon);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 2, (uint)activeData.PvpInfo[i].SeasonPlayed);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 3, (uint)activeData.PvpInfo[i].SeasonWon);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 4, (uint)activeData.PvpInfo[i].Rating);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 5, (uint)activeData.PvpInfo[i].WeeklyBestRating);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 6, (uint)activeData.PvpInfo[i].SeasonBestRating);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 7, (uint)activeData.PvpInfo[i].PvpTierID);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 8, (uint)activeData.PvpInfo[i].WeeklyBestWinPvpTierID);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 9, (uint)activeData.PvpInfo[i].Field_28);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 10, (uint)activeData.PvpInfo[i].Field_2C);
                    m_fields.SetUpdateField<uint>(startIndex + i * sizePerEntry + 11, (uint)(activeData.PvpInfo[i].Disqualified ? 1 : 0));
                }
            }
            if (activeData.MaxLevel != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MAX_LEVEL, (int)activeData.MaxLevel);
            if (activeData.ScalingPlayerLevelDelta != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_SCALING_PLAYER_LEVEL_DELTA, (int)activeData.ScalingPlayerLevelDelta);
            if (activeData.MaxCreatureScalingLevel != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MAX_CREATURE_SCALING_LEVEL, (int)activeData.MaxCreatureScalingLevel);
            for (int i = 0; i < 4; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_NO_REAGENT_COST_MASK;
                if (activeData.NoReagentCostMask[i] != null)
                    m_fields.SetUpdateField<uint>(startIndex + i, (uint)activeData.NoReagentCostMask[i]);
            }
            if (activeData.PetSpellPower != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_PET_SPELL_POWER, (int)activeData.PetSpellPower);
            for (int i = 0; i < 2; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_PROFESSION_SKILL_LINE;
                if (activeData.ProfessionSkillLine[i] != null)
                    m_fields.SetUpdateField<int>(startIndex + i, (int)activeData.ProfessionSkillLine[i]);
            }
            if (activeData.UiHitModifier != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_UI_HIT_MODIFIER, (float)activeData.UiHitModifier);
            if (activeData.UiSpellHitModifier != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_UI_SPELL_HIT_MODIFIER, (float)activeData.UiSpellHitModifier);
            if (activeData.HomeRealmTimeOffset != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_HOME_REALM_TIME_OFFSET, (int)activeData.HomeRealmTimeOffset);
            if (activeData.ModPetHaste != null)
                m_fields.SetUpdateField<float>(ActivePlayerField.ACTIVE_PLAYER_FIELD_MOD_PET_HASTE, (float)activeData.ModPetHaste);
            if (activeData.LocalRegenFlags != null || activeData.AuraVision != null || activeData.NumBackpackSlots != null)
            {
                if (activeData.LocalRegenFlags != null)
                    m_fields.SetUpdateField<byte>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES_4, (byte)activeData.LocalRegenFlags, 0);
                if (activeData.AuraVision != null)
                    m_fields.SetUpdateField<byte>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES_4, (byte)activeData.AuraVision, 1);
                if (activeData.NumBackpackSlots != null)
                    m_fields.SetUpdateField<byte>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES_4, (byte)activeData.NumBackpackSlots, 2);
            }
            if (activeData.OverrideSpellsID != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_OVERRIDE_SPELLS_ID, (int)activeData.OverrideSpellsID);
            if (activeData.LfgBonusFactionID != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_LFG_BONUS_FACTION_ID, (int)activeData.LfgBonusFactionID);
            if (activeData.LootSpecID != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_LOOT_SPEC_ID, (uint)activeData.LootSpecID);
            if (activeData.OverrideZonePVPType != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_OVERRIDE_ZONE_PVP_TYPE, (uint)activeData.OverrideZonePVPType);
            for (int i = 0; i < 4; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_BAG_SLOT_FLAGS;
                if (activeData.BagSlotFlags[i] != null)
                    m_fields.SetUpdateField<uint>(startIndex + i, (uint)activeData.BagSlotFlags[i]);
            }
            for (int i = 0; i < 7; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_BANK_BAG_SLOT_FLAGS;
                if (activeData.BankBagSlotFlags[i] != null)
                    m_fields.SetUpdateField<uint>(startIndex + i, (uint)activeData.BankBagSlotFlags[i]);
            }
            for (int i = 0; i < 875; i++)
            {
                int startIndex = (int)ActivePlayerField.ACTIVE_PLAYER_FIELD_QUEST_COMPLETED;
                if (activeData.QuestCompleted[i] != null)
                    m_fields.SetUpdateField<ulong>(startIndex + i * 2, (ulong)activeData.QuestCompleted[i]);
            }
            if (activeData.Honor != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_HONOR, (int)activeData.Honor);
            if (activeData.HonorNextLevel != null)
                m_fields.SetUpdateField<int>(ActivePlayerField.ACTIVE_PLAYER_FIELD_HONOR_NEXT_LEVEL, (int)activeData.HonorNextLevel);
            if (activeData.PvPTierMaxFromWins != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_PVP_TIER_MAX_FROM_WINS, (uint)activeData.PvPTierMaxFromWins);
            if (activeData.PvPLastWeeksTierMaxFromWins != null)
                m_fields.SetUpdateField<uint>(ActivePlayerField.ACTIVE_PLAYER_FIELD_PVP_LAST_WEEKS_TIER_MAX_FROM_WINS, (uint)activeData.PvPLastWeeksTierMaxFromWins);
            if (activeData.InsertItemsLeftToRight != null || activeData.PvPRankProgress != null)
            {
                if (activeData.InsertItemsLeftToRight != null)
                    m_fields.SetUpdateField<byte>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES_5, (byte)(activeData.InsertItemsLeftToRight == true ? 1 : 0), 0);
                if (activeData.PvPRankProgress != null)
                    m_fields.SetUpdateField<byte>(ActivePlayerField.ACTIVE_PLAYER_FIELD_BYTES_5, (byte)activeData.PvPRankProgress, 1);
            }


            m_alreadyWritten = true;
        }


    }
}