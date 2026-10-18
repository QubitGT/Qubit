/*
 * Qubit Menu  Libraries/Entities/Critters.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Qubit Software  /Derivative work of Seralyth Software, Goldentrophy Software
 * https://github.com/QubitGT/Qubit
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Overpowered = Qubit.Modules.Categories.Overpowered;
using Photon.Pun;
using Qubit.Core;
using Qubit.Libraries.Core;
using Qubit.Modules.Classes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Categories.Projectiles;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.GameObj;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Qubit.Libraries.Entities
{
    public static class Critters
    {

        public static void EffectSpam(CrittersManager.CritterEvent critterEvent)
        {
            if (rightGrab)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    CrittersPawn[] critters = ManagerRegistry.CrittersManager.crittersPawns.ToArray();
                    if (critters.Length > 0)
                    {
                        CrittersPawn critter = critters[0];
                        critter.transform.position = References.RightHand.position;
                        int actorId = critter.actorId;
                        ManagerRegistry.CrittersManager.TriggerEvent(critterEvent, actorId, critter.transform.position, Quaternion.LookRotation(critter.transform.up));
                    }
                }
                else
                {
                    CrittersActor.CrittersActorType type = CrittersActor.CrittersActorType.StickyTrap;
                    Vector3 velocity = Vector3.down * 20f;
                    switch (critterEvent)
                    {
                        case CrittersManager.CritterEvent.StunExplosion:
                            type = CrittersActor.CrittersActorType.StunBomb;
                            break;
                        case CrittersManager.CritterEvent.StickyDeployed:
                        case CrittersManager.CritterEvent.StickyTriggered:
                            type = CrittersActor.CrittersActorType.StickyTrap;
                            break;
                        case CrittersManager.CritterEvent.NoiseMakerTriggered:
                            type = CrittersActor.CrittersActorType.NoiseMaker;
                            break;
                    }

                    CrittersGrabber localGrabber = GetAllType<CrittersGrabber>().Where(grabber => grabber.rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber && grabber.isLeft).FirstOrDefault();
                    List<CrittersActor> critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 3f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    if (critters.Count <= 0)
                        critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    if (critters.Count <= 0)
                        critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    CrittersActor critter = critters[Random.Range(0, critters.Count)];

                    if (Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 25f)
                    {
                        References.LocalRig.enabled = false;
                        References.LocalRig.transform.position = critter.transform.position - Vector3.one * 5f;

                        if (Critters.CritterCoroutine != null)
                            Coroutines.instance.StopCoroutine(Critters.CritterCoroutine);

                        Critters.CritterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
                    }

                    if (Vector3.Distance(critter.transform.position, ServerPos) < 25f && Time.time > Critters.critterGrabDelay)
                    {
                        Critters.critterGrabDelay = Time.time + 0.1f;

                        critter.transform.position = References.RightHand.position;
                        critter.transform.rotation = References.RightHand.rotation;

                        if (critter)
                            critter.SetImpulseVelocity(velocity, Vector3.zero);

                        if (localGrabber != null)
                            ManagerRegistry.CrittersManager.SendRPC("RemoteCrittersActorGrabbedby",
                                ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, localGrabber.actorId,
                                Quaternion.identity, Vector3.zero, false);
                        ManagerRegistry.CrittersManager.SendRPC("RemoteCritterActorReleased", ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, false, critter.transform.rotation, critter.transform.position, velocity, Vector3.zero);
                    }
                }
            }
        }

        public static void EffectGun(CrittersManager.CritterEvent critterEvent)
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true))
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        CrittersPawn[] critters = ManagerRegistry.CrittersManager.crittersPawns.ToArray();
                        if (critters.Length > 0)
                        {
                            CrittersPawn critter = critters[0];
                            critter.transform.position = NewPointer.transform.position;
                            int actorId = critter.actorId;
                            ManagerRegistry.CrittersManager.TriggerEvent(critterEvent, actorId, critter.transform.position, Quaternion.LookRotation(critter.transform.up));
                        }
                    }
                    else
                    {
                        CrittersActor.CrittersActorType type = CrittersActor.CrittersActorType.StickyTrap;
                        Vector3 velocity = -Ray.normal * 50f;
                        switch (critterEvent)
                        {
                            case CrittersManager.CritterEvent.StunExplosion:
                                type = CrittersActor.CrittersActorType.StunBomb;
                                break;
                            case CrittersManager.CritterEvent.StickyDeployed:
                            case CrittersManager.CritterEvent.StickyTriggered:
                                type = CrittersActor.CrittersActorType.StickyTrap;
                                break;
                            case CrittersManager.CritterEvent.NoiseMakerTriggered:
                                type = CrittersActor.CrittersActorType.NoiseMaker;
                                break;
                        }

                        CrittersGrabber localGrabber = GetAllType<CrittersGrabber>().Where(grabber => grabber.rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber && grabber.isLeft).FirstOrDefault();
                        List<CrittersActor> critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 3f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                        if (critters.Count <= 0)
                            critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                        if (critters.Count <= 0)
                            critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                        CrittersActor critter = critters[Random.Range(0, critters.Count)];

                        if (Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 25f)
                        {
                            References.LocalRig.enabled = false;
                            References.LocalRig.transform.position = critter.transform.position - Vector3.one * 5f;

                            if (Critters.CritterCoroutine != null)
                                Coroutines.instance.StopCoroutine(Critters.CritterCoroutine);

                            Critters.CritterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
                        }

                        if (Vector3.Distance(critter.transform.position, ServerPos) < 25f && Time.time > Critters.critterGrabDelay)
                        {
                            Critters.critterGrabDelay = Time.time + 0.05f;

                            critter.transform.position = NewPointer.transform.position + Ray.normal;
                            critter.transform.rotation = RandomQuaternion();

                            if (critter)
                                critter.SetImpulseVelocity(velocity, Vector3.zero);

                            if (localGrabber != null)
                                ManagerRegistry.CrittersManager.SendRPC("RemoteCrittersActorGrabbedby",
                                    ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, localGrabber.actorId,
                                    Quaternion.identity, Vector3.zero, false);
                            ManagerRegistry.CrittersManager.SendRPC("RemoteCritterActorReleased", ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, false, critter.transform.rotation, critter.transform.position, velocity, Vector3.zero);
                        }
                    }
                }
            });
        }

        public static void CritterSpam()
        {
            if (rightGrab)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    List<CrittersPawn> critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null).ToList();

                    CrittersPawn targetCritter = critters[Random.Range(0, critters.Count)];
                    targetCritter.transform.position = References.RightHand.position;
                    targetCritter.transform.rotation = RandomQuaternion();
                }
                else
                {
                    CrittersGrabber localGrabber = GetAllType<CrittersGrabber>().Where(grabber => grabber.rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber && grabber.isLeft).FirstOrDefault();
                    List<CrittersPawn> critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 3f).ToList();

                    if (critters.Count <= 0)
                        critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f).ToList();

                    if (critters.Count <= 0)
                        critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null).ToList();

                    if (critters.Count <= 0)
                        return;

                    CrittersPawn critter = critters[Random.Range(0, critters.Count)];

                    if (Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 25f)
                    {
                        References.LocalRig.enabled = false;
                        References.LocalRig.transform.position = critter.transform.position - Vector3.one * 5f;

                        if (Critters.CritterCoroutine != null)
                            Coroutines.instance.StopCoroutine(Critters.CritterCoroutine);

                        Critters.CritterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
                    }

                    if (Vector3.Distance(critter.transform.position, ServerPos) < 25f && critter.currentState != CrittersPawn.CreatureState.Grabbed && Time.time > Critters.critterGrabDelay)
                    {
                        Critters.critterGrabDelay = Time.time + 0.05f;

                        critter.transform.position = References.RightHand.position;
                        critter.transform.rotation = RandomQuaternion();

                        if (localGrabber != null)
                            ManagerRegistry.CrittersManager.SendRPC("RemoteCrittersActorGrabbedby",
                                ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, localGrabber.actorId,
                                Quaternion.identity, Vector3.zero, false);
                        ManagerRegistry.CrittersManager.SendRPC("RemoteCritterActorReleased", ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, false, critter.transform.rotation, critter.transform.position, Vector3.zero, Vector3.zero);
                    }
                }
            }
        }

        public static void CritterMinigun()
        {
            if (rightGrab)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    List<CrittersPawn> critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null).ToList();

                    CrittersPawn targetCritter = critters[Random.Range(0, critters.Count)];
                    targetCritter.transform.position = References.RightHand.position;
                    targetCritter.transform.rotation = RandomQuaternion();

                    if (targetCritter.usesRB)
                        targetCritter.SetImpulseVelocity(GetGunDirection(References.RightHand) * ShootStrength, RandomVector3(100f));
                }
                else
                {
                    CrittersGrabber localGrabber = GetAllType<CrittersGrabber>().Where(grabber => grabber.rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber && grabber.isLeft).FirstOrDefault();
                    List<CrittersPawn> critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 3f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    if (critters.Count <= 0)
                        critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    if (critters.Count <= 0)
                        critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    CrittersPawn critter = critters[Random.Range(0, critters.Count)];

                    if (Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 25f)
                    {
                        References.LocalRig.enabled = false;
                        References.LocalRig.transform.position = critter.transform.position - Vector3.one * 5f;

                        if (Critters.CritterCoroutine != null)
                            Coroutines.instance.StopCoroutine(Critters.CritterCoroutine);

                        Critters.CritterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
                    }

                    if (Vector3.Distance(critter.transform.position, ServerPos) < 25f && Time.time > Critters.critterGrabDelay)
                    {
                        Critters.critterGrabDelay = Time.time + 0.05f;

                        critter.transform.position = References.RightHand.position;
                        critter.transform.rotation = RandomQuaternion();

                        if (localGrabber != null)
                            ManagerRegistry.CrittersManager.SendRPC("RemoteCrittersActorGrabbedby",
                                ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, localGrabber.actorId,
                                Quaternion.identity, Vector3.zero, false);
                        ManagerRegistry.CrittersManager.SendRPC("RemoteCritterActorReleased", ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, false, critter.transform.rotation, critter.transform.position, GetGunDirection(References.RightHand) * ShootStrength, Vector3.zero);
                    }
                }
            }
        }
        public static void CritterGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    List<CrittersPawn> critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null).ToList();

                    CrittersPawn targetCritter = critters[Random.Range(0, critters.Count)];
                    targetCritter.transform.position = NewPointer.transform.position;
                    targetCritter.transform.rotation = RandomQuaternion();
                }
                else
                {
                    CrittersGrabber localGrabber = GetAllType<CrittersGrabber>().Where(grabber => grabber.rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber && grabber.isLeft).FirstOrDefault();
                    List<CrittersPawn> critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 3f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    if (critters.Count <= 0)
                        critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    if (critters.Count <= 0)
                        critters = ManagerRegistry.CrittersManager.crittersPawns.Where(critter => critter != null).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    CrittersPawn critter = critters[Random.Range(0, critters.Count)];

                    if (Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 25f)
                    {
                        References.LocalRig.enabled = false;
                        References.LocalRig.transform.position = critter.transform.position - Vector3.one * 5f;

                        if (Critters.CritterCoroutine != null)
                            Coroutines.instance.StopCoroutine(Critters.CritterCoroutine);

                        Critters.CritterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
                    }

                    if (Vector3.Distance(critter.transform.position, ServerPos) < 25f && Time.time > Critters.critterGrabDelay)
                    {
                        Critters.critterGrabDelay = Time.time + 0.05f;

                        critter.transform.position = NewPointer.transform.position + Vector3.up;
                        critter.transform.rotation = RandomQuaternion();

                        if (localGrabber != null)
                            ManagerRegistry.CrittersManager.SendRPC("RemoteCrittersActorGrabbedby",
                                ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, localGrabber.actorId,
                                Quaternion.identity, Vector3.zero, false);
                        ManagerRegistry.CrittersManager.SendRPC("RemoteCritterActorReleased", ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, false, critter.transform.rotation, critter.transform.position, Vector3.zero, Vector3.zero);
                    }
                }
            });
        }

        public static void ObjectSpam(CrittersActor.CrittersActorType type)
        {
            if (rightGrab)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    CrittersActor Object = ManagerRegistry.CrittersManager.SpawnActor(type);
                    Object.MoveActor(References.RightHand.position, References.RightHand.rotation);

                    if (Object.usesRB)
                        Object.SetImpulseVelocity(GetGunDirection(References.RightHand) * ShootStrength, RandomVector3(100f));
                }
                else
                {
                    Vector3 velocity = GetGunDirection(References.RightHand) * ShootStrength;
                    switch (type)
                    {
                        case CrittersActor.CrittersActorType.LoudNoise:
                            type = CrittersActor.CrittersActorType.NoiseMaker;
                            velocity = Vector3.down * 50f;
                            break;
                        case CrittersActor.CrittersActorType.StickyGoo:
                            type = CrittersActor.CrittersActorType.StickyTrap;
                            velocity = Vector3.down * 50f;
                            break;
                    }

                    CrittersGrabber localGrabber = GetAllType<CrittersGrabber>().Where(grabber => grabber.rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber && grabber.isLeft).FirstOrDefault();
                    List<CrittersActor> critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 3f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    if (critters.Count <= 0)
                        critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    if (critters.Count <= 0)
                        critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                    CrittersActor critter = critters[Random.Range(0, critters.Count)];

                    if (Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 25f)
                    {
                        References.LocalRig.enabled = false;
                        References.LocalRig.transform.position = critter.transform.position - Vector3.one * 5f;

                        if (Critters.CritterCoroutine != null)
                            Coroutines.instance.StopCoroutine(Critters.CritterCoroutine);

                        Critters.CritterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
                    }

                    if (Vector3.Distance(critter.transform.position, ServerPos) < 25f && Time.time > Critters.critterGrabDelay)
                    {
                        Critters.critterGrabDelay = Time.time + 0.05f;

                        critter.transform.position = References.RightHand.position;
                        critter.transform.rotation = References.RightHand.rotation;

                        if (critter)
                            critter.SetImpulseVelocity(velocity, Vector3.zero);

                        if (localGrabber != null)
                            ManagerRegistry.CrittersManager.SendRPC("RemoteCrittersActorGrabbedby",
                                ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, localGrabber.actorId,
                                Quaternion.identity, Vector3.zero, false);
                        ManagerRegistry.CrittersManager.SendRPC("RemoteCritterActorReleased", ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, false, critter.transform.rotation, critter.transform.position, velocity, Vector3.zero);
                    }
                }
            }
        }

        public static void ObjectGun(CrittersActor.CrittersActorType type)
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true))
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        CrittersActor Object = ManagerRegistry.CrittersManager.SpawnActor(type);
                        Object.MoveActor(NewPointer.transform.position + Vector3.up, RandomQuaternion());
                    }
                    else
                    {
                        Vector3 velocity = Vector3.zero;
                        Vector3 position = NewPointer.transform.position + Vector3.up;

                        switch (type)
                        {
                            case CrittersActor.CrittersActorType.LoudNoise:
                                type = CrittersActor.CrittersActorType.NoiseMaker;
                                velocity = Ray.normal * -20f;
                                position = NewPointer.transform.position + Ray.normal;
                                break;
                            case CrittersActor.CrittersActorType.StickyGoo:
                                type = CrittersActor.CrittersActorType.StickyTrap;
                                velocity = Ray.normal * -20f;
                                position = NewPointer.transform.position + Ray.normal;
                                break;
                        }

                        CrittersGrabber localGrabber = GetAllType<CrittersGrabber>().Where(grabber => grabber.rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber && grabber.isLeft).FirstOrDefault();
                        List<CrittersActor> critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 3f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                        if (critters.Count <= 0)
                            critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type && Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) < 25f).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                        if (critters.Count <= 0)
                            critters = GetAllType<CrittersActor>().Where(critter => critter != null && critter.crittersActorType == type).OrderByDescending(critter => Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position)).ToList();

                        CrittersActor critter = critters[Random.Range(0, critters.Count)];

                        if (Vector3.Distance(critter.transform.position, References.BodyCollider.transform.position) > 25f)
                        {
                            References.LocalRig.enabled = false;
                            References.LocalRig.transform.position = critter.transform.position - Vector3.one * 5f;

                            if (Critters.CritterCoroutine != null)
                                Coroutines.instance.StopCoroutine(Critters.CritterCoroutine);

                            Critters.CritterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
                        }

                        if (Vector3.Distance(critter.transform.position, ServerPos) < 25f && Time.time > Critters.critterGrabDelay)
                        {
                            Critters.critterGrabDelay = Time.time + 0.1f;

                            critter.transform.position = position;
                            critter.transform.rotation = RandomQuaternion();

                            if (critter)
                                critter.SetImpulseVelocity(velocity, Vector3.zero);

                            if (localGrabber != null)
                                ManagerRegistry.CrittersManager.SendRPC("RemoteCrittersActorGrabbedby",
                                    ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, localGrabber.actorId,
                                    Quaternion.identity, Vector3.zero, false);
                            ManagerRegistry.CrittersManager.SendRPC("RemoteCritterActorReleased", ManagerRegistry.CrittersManager.guard.currentOwner, critter.actorId, false, critter.transform.rotation, critter.transform.position, velocity, Vector3.zero);
                        }
                    }
                }
            });
        }

        public static Coroutine CritterCoroutine;
        public static float critterGrabDelay;
    }
}
