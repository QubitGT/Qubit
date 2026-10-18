/*
 * Qubit Menu  Libraries/Entities/Blocks.cs
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

using Settings = Qubit.Modules.Categories.Settings;
using Fun = Qubit.Modules.Categories.Fun;
using Overpowered = Qubit.Modules.Categories.Overpowered;
using Manager = Qubit.Modules.Manager;
using Module = Qubit.Modules.Module;
using GorillaTagScripts.VirtualStumpCustomMaps;
using static Qubit.Core.Security;
using static Qubit.Modules.Manager;
using static Qubit.Modules.Categories.Master;
using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Swimming;
using GorillaNetworking;
using GorillaTag.Cosmetics;
using GorillaTag.Rendering;
using GorillaTagScripts;
using GorillaTagScripts.Builder;
using Ionic.Zlib;
using POpusCodec.Enums;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using PlayFab;
using PlayFab.ClientModels;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Menu;
using Qubit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Windows.Speech;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Transcription;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Categories.Fun;
using static Qubit.Modules.Categories.Projectiles;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.GameModes;
using static Qubit.Utilities.GameObj;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Security = Qubit.Core.Security;

namespace Qubit.Libraries.Entities
{
    public static class Blocks
    {

        public static int pieceIdSet = -566818631;
        private static float blockDelay;

        private static Dictionary<int, string> blocks;

        public static float startTimeBuilding;

        public static int blockDebounceIndex = 2;
        public static float blockDebounce = 0.1f;

        private static float noclipBuildingDelay;

        public static int pieceId = -1;

        private static readonly List<BuilderPiece> potentialgrabbedpieces = new List<BuilderPiece>();

        public static void GiveBuilderWatch()
        {
            References.LocalRig.EnableBuilderResizeWatch(true);
            Security.RPCProtection();
        }

        public static void RemoveBuilderWatch()
        {
            References.LocalRig.EnableBuilderResizeWatch(false);
            Security.RPCProtection();
        }
        public static void BlocksGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                CreatePiece(pieceIdSet, NewPointer.transform.position + Vector3.up * 0.1f, Quaternion.identity, 0);
                Security.RPCProtection();
            });
        }
        public static void SelectBlockGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    BuilderPiece gunTarget = Ray.collider.GetComponentInParent<BuilderPiece>();
                    if (gunTarget && Time.time > gbgd)
                    {
                        gbgd = Time.time + 0.1f;
                        pieceIdSet = gunTarget.pieceType;
                        Notifications.SendFormat("green", "SUCCESS", "Successfully selected piece " + gunTarget.displayName + ".");
                    }
                }
            });
        }

        public static void CopyBlockInfoGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                                if (GetGunInput(true))
                                {
                                    BuilderPiece gunTarget = Ray.collider.GetComponentInParent<BuilderPiece>();
                                    if (gunTarget && Time.time > gbgd)
                                    {
                                        gbgd = Time.time + 0.1f;
                                        GUIUtility.systemCopyBuffer = @$"{gunTarget.displayName}
                Piece Type: {gunTarget.pieceType}
                Piece Name: {gunTarget.name}";
                                        Notifications.SendFormat("green", "SUCCESS", "Successfully copied piece data of " + gunTarget.displayName + ".");
                                    }
                                }
            });
        }

        public static void SelectBlock(int type, string name)
        {
            pieceIdSet = type;
            Notifications.SendFormat("green", "SUCCESS", "Successfully selected piece " + name.Replace("(Clone)", "") + ".");
        }
        public static Dictionary<int, string> GetAllBlockData()
        {
            if (blocks == null)
            {
                if (ManagerRegistry.BuilderTable == null)
                    return new Dictionary<int, string>();

                blocks = new Dictionary<int, string>();
                foreach (var piece in ManagerRegistry.BuilderTable.builderPool.piecePools.SelectMany(list => list))
                {
                    try
                    {
                        blocks.Add(piece.name.Replace("(Clone)", "").GetStaticHash(), piece.displayName ?? piece.displayName);
                    }
                    catch { }
                }
            }

            return blocks;
        }

        public static int[] GetAllBlockTypes() =>
            GetAllBlockData().Keys.ToArray();

        public static int GetRandomBlockType()
        {
            int[] blockTypes = GetAllBlockTypes();
            return blockTypes[Random.Range(0, blockTypes.Length)];
        }

        public static void BlockBrowser()
        {
            rememberdirectory = pageNumber;

            Dictionary<int, string> allBlockData = GetAllBlockData();
            List<Module> blockButtons = new List<Module> { new Module { buttonText = "Exit Building Block Browser", method = RemoveCosmeticBrowser, isTogglable = false, toolTip = "Returns you back to the fun mods." } };

            int i = 0;
            foreach (KeyValuePair<int, string> block in allBlockData)
            {
                blockButtons.Add
                (
                    new Module
                    {
                        buttonText = $"SelectBlock{i}",
                        overlapText = block.Value,
                        method = () => SelectBlock(block.Key, block.Value),
                        isTogglable = false,
                        toolTip = $"Selects the block \"{block.Value}\" to be used for the building mods."
                    }
                );
                i++;
            }

            Manager.modules["Temporary Category"] = blockButtons.ToArray();
            Manager.CurrentCategoryName = "Temporary Category";
        }

        public static void SpamGrabBlocks()
        {
            if (rightGrab)
            {
                CreatePiece(pieceIdSet, References.RightHand.position, References.RightHand.rotation, 0);
                Security.RPCProtection();
            }
        }

        public static void BuildingBlockMinigun()
        {
            if (rightGrab)
            {
                CreatePiece(pieceIdSet, References.RightHand.position, References.RightHand.rotation, 0, null, false, false, GetGunDirection(References.RightHand) * ShootStrength);
                Security.RPCProtection();
            }
        }
        public static void DestroyBlocks()
        {
            if (Time.time > delayer)
            {
                delayer = Time.time + 0.3f;

                BuilderPiece[] totalPieces = PhotonNetwork.IsMasterClient ? GetAllType<BuilderPiece>() :
                    GetAllType<BuilderPiece>()
                        .Where(piece => piece.gameObject.activeInHierarchy)
                        .Where(piece => Vector3.Distance(piece.transform.position, References.LeftHand.position) < 2.5f).ToArray();

                for (int i = 0; i < 100; i++)
                {
                    BuilderPiece piece = totalPieces[Random.Range(0, totalPieces.Length)];
                    if (piece.gameObject.activeSelf)
                        RecyclePiece(piece, true, 2);
                }
            }
        }

        public static void SaveBuilderTableData()
        {
            string fileName = $"{PluginInfo.BaseDirectory}/BuilderTableData.json";

            File.WriteAllText(fileName, ManagerRegistry.BuilderTable.WriteTableToJson());

            string filePath = Files.GetGamePath() + "/" + fileName;
            Files.Start(filePath);
        }

        public static void LoadBuilderTableData()
        {
            string fileName = $"{PluginInfo.BaseDirectory}/BuilderTableData.json";

            if (!File.Exists(fileName))
                return;

            string tableData = File.ReadAllText(fileName);
            BuilderTable table = ManagerRegistry.BuilderTable;

            table.tableData ??= new BuilderTableData();

            table.SetIsDirty(false);
            table.tableData.numEdits++;

            tableData = Convert.ToBase64String(GZipStream.CompressString(tableData));
            ManagerRegistry.SharedBlocksManager.OnSavePrivateScanSuccess += table.OnSaveScanSuccess;
            ManagerRegistry.SharedBlocksManager.OnSavePrivateScanFailed += table.OnSaveScanFailure;
            ManagerRegistry.SharedBlocksManager.RequestSavePrivateScan(table.currentSaveSlot, tableData);
        }

        public static void PlaceBlockGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    BuilderPiece gunTarget = Ray.collider.GetComponentInParent<BuilderPiece>();
                    if (gunTarget)
                    {
                        PlacePiece(PlacePatch._piece, gunTarget, PlacePatch._bumpOffsetX, PlacePatch._bumpOffsetZ, PlacePatch._twist, PlacePatch._parentPiece, PlacePatch._attachIndex, PlacePatch._parentAttachIndex);
                        Security.RPCProtection();
                    }
                }
            });
        }

        public static void DestroyBlockGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    BuilderPiece gunTarget = Ray.collider.GetComponentInParent<BuilderPiece>();
                    if (gunTarget)
                    {
                        RecyclePiece(gunTarget, true, 2);
                        Security.RPCProtection();
                    }
                }
            });
        }
        public static void ApplyBlockDelay(int index)
        {
            blockDebounceIndex = index;
            blockDebounce = index / 20f;
        }

        public static void CreatePiece(int pieceType, Vector3 position, Quaternion rotation, int materialType, object target = null, bool overrideFreeze = false, bool forceGravity = false, Vector3? velocity = null, Vector3? angVelocity = null)
        {
            if (Manager.GetIndex("Random Block Type").enabled)
                pieceType = GetRandomBlockType();

            BuilderTable table = ManagerRegistry.BuilderTable;
            BuilderTableNetworking Networking = table.builderNetworking;
            if (Qubit.Core.Networking.IsMasterClient)
            {
                if (Time.time > blockDelay)
                {
                    int pieceId = table.CreatePieceId();

                    object[] args = {
                        pieceType,
                        pieceId,
                        BitPackUtils.PackWorldPosForNetwork(position),
                        BitPackUtils.PackQuaternionForNetwork(rotation),
                        materialType,
                        (byte)4,
                        1,
                        PhotonNetwork.LocalPlayer
                    };

                    switch (target)
                    {
                        case RpcTarget rpcTargetCreate:
                            Networking.photonView.RPC("PieceCreatedByShelfRPC", rpcTargetCreate, args);
                            break;
                        case Player playerCreate:
                            Networking.photonView.RPC("PieceCreatedByShelfRPC", playerCreate, args);
                            break;
                        default:
                            Networking.photonView.RPC("PieceCreatedByShelfRPC", RpcTarget.All, args);
                            break;
                    }

                    if ((!overrideFreeze && !Manager.GetIndex("Zero Gravity Blocks").enabled) || forceGravity)
                    {
                        blockDelay = Time.time + 0.02f;

                        args = new object[]
                        {
                            Networking.CreateLocalCommandId(),
                            pieceId,
                            true,
                            BitPackUtils.PackHandPosRotForNetwork(Vector3.zero, Quaternion.identity),
                            PhotonNetwork.LocalPlayer
                        };

                        switch (target)
                        {
                            case RpcTarget rpcTargetGrab:
                                Networking.photonView.RPC("PieceGrabbedRPC", rpcTargetGrab, args);
                                break;
                            case Player playerGrab:
                                Networking.photonView.RPC("PieceGrabbedRPC", playerGrab, args);
                                break;
                            default:
                                Networking.photonView.RPC("PieceGrabbedRPC", RpcTarget.All, args);
                                break;
                        }

                        args = new object[]
                        {
                            Networking.CreateLocalCommandId(),
                            pieceId,
                            position,
                            rotation,
                            velocity ?? Vector3.zero,
                            angVelocity ?? Vector3.zero,
                            PhotonNetwork.LocalPlayer
                        };

                        switch (target)
                        {
                            case RpcTarget rpcTargetDrop:
                                Networking.photonView.RPC("PieceDroppedRPC", rpcTargetDrop, args);
                                break;
                            case Player playerDrop:
                                Networking.photonView.RPC("PieceDroppedRPC", playerDrop, args);
                                break;
                            default:
                                Networking.photonView.RPC("PieceDroppedRPC", RpcTarget.All, args);
                                break;
                        }
                    }
                }
            }
            else
            {
                if (Time.time > blockDelay)
                {
                    blockDelay = Time.time + blockDebounce;
                    BuilderPiece piece = (GetAllType<BuilderPiece>()
                        .Where(piece => piece.gameObject.activeInHierarchy)
                        .Where(piece => piece.pieceType == pieceType)
                        .Where(piece => !piece.isBuiltIntoTable)
                        .Where(piece => piece.CanPlayerGrabPiece(PhotonNetwork.LocalPlayer.ActorNumber, piece.transform.position))
                        .Where(piece => Vector3.Distance(piece.transform.position, ServerLeftHandPos) < 2.5f)
                        .OrderBy(piece => Vector3.Distance(piece.transform.position, ServerLeftHandPos))
                        .FirstOrDefault()
                        ?? null) ?? GetAllType<BuilderPiece>()
                            .Where(piece => piece.gameObject.activeInHierarchy)
                            .Where(piece => !piece.isBuiltIntoTable)
                            .Where(piece => piece.CanPlayerGrabPiece(PhotonNetwork.LocalPlayer.ActorNumber, piece.transform.position))
                            .Where(piece => Vector3.Distance(piece.transform.position, ServerLeftHandPos) < 2.5f)
                            .OrderBy(piece => Vector3.Distance(piece.transform.position, ServerLeftHandPos))
                            .FirstOrDefault()
                            ?? null;

                    if (piece == null)
                        return;

                    if (Vector3.Distance(ServerLeftHandPos, position) > 2.5f)
                        position = ServerLeftHandPos + (position - ServerLeftHandPos).normalized * 2.5f;

                    pieceId = piece.pieceId;

                    Networking.RequestGrabPiece(piece, true, Vector3.zero, Quaternion.identity);
                    Networking.RequestDropPiece(piece, position, rotation, velocity ?? Vector3.zero, angVelocity ?? Vector3.zero);
                }
            }
        }

        public static void GrabPiece(BuilderPiece piece, bool isLefHand, Vector3 localPosition, Quaternion localRotation)
        {
            BuilderTableNetworking Networking = ManagerRegistry.BuilderTable.builderNetworking;
            if (Qubit.Core.Networking.IsMasterClient)
            {
                Networking.photonView.RPC("PieceGrabbedRPC", RpcTarget.All, Networking.CreateLocalCommandId(), piece.pieceId, isLefHand, BitPackUtils.PackHandPosRotForNetwork(localPosition, localRotation), PhotonNetwork.LocalPlayer);
            }
            else
                Networking.RequestGrabPiece(piece, isLefHand, localPosition, localRotation);
        }

        public static void PlacePiece(BuilderPiece piece, BuilderPiece attachPiece, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, BuilderPiece parentPiece, int attachIndex, int parentAttachIndex)
        {
            if (attachPiece.isBuiltIntoTable)
                return;

            BuilderTableNetworking Networking = ManagerRegistry.BuilderTable.builderNetworking;
            if (Qubit.Core.Networking.IsMasterClient)
            {
                Networking.photonView.RPC("PiecePlacedRPC", RpcTarget.All, Networking.CreateLocalCommandId(), piece.pieceId, attachPiece != null ? attachPiece.pieceId : -1, BuilderTable.PackPiecePlacement(twist, bumpOffsetX, bumpOffsetZ), parentPiece != null ? parentPiece.pieceId : -1, attachIndex, parentAttachIndex, PhotonNetwork.LocalPlayer, PhotonNetwork.ServerTimestamp);
            }
            else
            {
                Networking.RequestGrabPiece(piece, true, Vector3.zero, Quaternion.identity);
                Networking.RequestPlacePiece(piece, attachPiece, bumpOffsetX, bumpOffsetZ, twist, parentPiece, attachIndex, parentAttachIndex);
            }
        }

        public static void DropPiece(BuilderPiece piece, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity)
        {
            BuilderTableNetworking Networking = ManagerRegistry.BuilderTable.builderNetworking;
            if (Qubit.Core.Networking.IsMasterClient)
            {
                Networking.photonView.RPC("PieceDroppedRPC", RpcTarget.All, Networking.CreateLocalCommandId(), piece.pieceId, position, rotation, velocity, angVelocity, PhotonNetwork.LocalPlayer);
            }
            else
                Networking.RequestDropPiece(piece, position, rotation, velocity, angVelocity);
        }

        public static void RecyclePiece(BuilderPiece piece, bool playFX, int recyclerID)
        {
            if (piece.isBuiltIntoTable)
                return;

            BuilderTable table = ManagerRegistry.BuilderTable;
            BuilderTableNetworking Networking = table.builderNetworking;

            if (Qubit.Core.Networking.IsMasterClient)
            {
                Networking.photonView.RPC("PieceDestroyedRPC", RpcTarget.All, piece.pieceId, BitPackUtils.PackWorldPosForNetwork(piece.transform.position), BitPackUtils.PackQuaternionForNetwork(piece.transform.rotation), playFX, (short)recyclerID);
            }
            else
            {
                if (Time.time > blockDelay)
                {
                    blockDelay = Time.time + blockDebounce;
                    if (piece.CanPlayerGrabPiece(PhotonNetwork.LocalPlayer.ActorNumber, piece.transform.position) && Vector3.Distance(piece.transform.position, ServerLeftHandPos) < 2.5f)
                    {
                        BuilderDropZone dropZone = table.dropZones
                            .Where(zone => (int)zone.dropType >= 1)
                            .Where(zone => Vector3.Distance(zone.transform.position, ServerLeftHandPos) < 2.5f)
                            .OrderBy(zone => Vector3.Distance(zone.transform.position, ServerLeftHandPos))
                            .FirstOrDefault() ?? null;

                        Vector3 dropPosition = dropZone != null ? dropZone.transform.position : ServerLeftHandPos + Vector3.down * 2f;

                        GrabPiece(piece, true, Vector3.zero, Quaternion.identity);
                        DropPiece(piece, dropPosition, RandomQuaternion(), Vector3.down * 20f, Vector3.zero);
                    }
                }
            }
        }

        public static void BuildingBlockAura()
        {
            CreatePiece(pieceIdSet, References.LocalRig.transform.position + RandomVector3().normalized * 2f, Quaternion.identity, 0);
            Security.RPCProtection();
        }

        public static void BuildingBlockTextGun()
        {
            TextGun.Render(Fun.textToRender, positions =>
            {
                foreach (Vector3 position in positions)
                    CreatePiece(pieceIdSet, position, Quaternion.identity, 0);

                Security.RPCProtection();
            });
        }

        public static void RainBuildingBlocks()
        {
            CreatePiece(pieceIdSet, References.LocalRig.transform.position + new Vector3(Random.Range(-3f, 3f), 4f, Random.Range(-3f, 3f)), Quaternion.identity, 0);
            Security.RPCProtection();
        }

        public static void BuildingBlockFountain()
        {
            CreatePiece(pieceIdSet, References.LocalRig.transform.position + Vector3.up * 3f, Quaternion.identity, 0, velocity: RandomVector3(15f));
            Security.RPCProtection();
        }

        public static void OrbitBlocks()
        {
            CreatePiece(pieceIdSet, References.HeadCollider.transform.position + new Vector3(MathF.Cos((float)Time.frameCount / 30), 0f, MathF.Sin((float)Time.frameCount / 30)), Quaternion.identity, 0);
            Security.RPCProtection();
        }
        public static void NoclipBuilding()
        {
            if (Time.time > noclipBuildingDelay)
                noclipBuildingDelay = Time.time + 5f;
            else
                return;

            foreach (BuilderPiece piece in GetAllType<BuilderPiece>().Where(block => block.gameObject.activeInHierarchy && !block.isBuiltIntoTable))
                piece.SetColliderLayers(piece.colliders, BuilderTable.heldLayerLocal);
        }

        public static void DisableNoclipBuilding()
        {
            foreach (BuilderPiece piece in GetAllType<BuilderPiece>().Where(block => block.gameObject.activeInHierarchy && !block.isBuiltIntoTable))
                piece.SetColliderLayers(piece.colliders, piece.state == BuilderPiece.State.AttachedAndPlaced ? BuilderTable.placedLayer : BuilderTable.droppedLayer);
        }

        public static void MultiGrab()
        {
            ManagerRegistry.BuilderPieceInteractor.handState[1] = BuilderPieceInteractor.HandState.Empty;
            ManagerRegistry.BuilderPieceInteractor.heldPiece[1] = null;
        }
        public static IEnumerator CreateGetPiece(int pieceType, Action<BuilderPiece> onComplete)
        {
            CreatePatch.enabled = true;
            CreatePatch.pieceTypeSearch = pieceType;

            yield return null;

            CreatePiece(pieceType, References.LocalRig.transform.position + Vector3.up, Quaternion.identity, 0, null, true);
            Security.RPCProtection();

            while (pieceId < 0)
                yield return null;

            yield return null;

            pieceId = -1;
            CreatePatch.enabled = false;
            CreatePatch.pieceTypeSearch = 0;

            onComplete?.Invoke(ManagerRegistry.BuilderTable.GetPiece(pieceId)); // so bad
        }

        public static IEnumerator CreateShotgun()
        {
            if (!Qubit.Core.Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                yield break;
            }

            BuilderPiece basea = null;

            yield return CreateGetPiece(-1927069002, piece => basea = piece);
            while (basea == null)
                yield return null;

            GrabPiece(basea, false, Vector3.zero, Quaternion.identity);
            yield return null;

            ManagerRegistry.BuilderPieceInteractor.handState[1] = BuilderPieceInteractor.HandState.Empty;
            ManagerRegistry.BuilderPieceInteractor.heldPiece[1] = null;
            yield return null;

            BuilderPiece base2a = null;

            yield return CreateGetPiece(-1621444201, piece => base2a = piece);
            while (base2a == null)
                yield return null;

            GrabPiece(base2a, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(base2a, base2a, 0, 0, 0, basea, 1, 0);
            yield return null;

            BuilderPiece slopea = null;

            yield return CreateGetPiece(-993249117, piece => slopea = piece);
            while (slopea == null)
                yield return null;

            GrabPiece(slopea, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(slopea, slopea, 0, 0, 2, base2a, 1, 0);
            yield return null;

            BuilderPiece trigger = null;

            yield return CreateGetPiece(251444537, piece => trigger = piece);
            while (trigger == null)
                yield return null;

            GrabPiece(trigger, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(trigger, trigger, -1, -2, 3, slopea, 1, 0);
            yield return null;

            BuilderPiece slopeb = null;

            yield return CreateGetPiece(-993249117, piece => slopeb = piece);
            while (slopeb == null)
                yield return null;

            GrabPiece(slopeb, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(basea, trigger, 0, -2, 3, slopeb, 1, 0);
            yield return null;

            BuilderPiece base2b = null;

            yield return CreateGetPiece(-1621444201, piece => base2b = piece);
            while (base2b == null)
                yield return null;

            GrabPiece(base2b, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(slopeb, slopeb, 0, 0, 2, base2b, 1, 0);
            yield return null;

            BuilderPiece baseb = null;

            yield return CreateGetPiece(-1927069002, piece => baseb = piece);
            while (baseb == null)
                yield return null;

            GrabPiece(baseb, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(base2b, base2b, 0, 0, 0, baseb, 1, 0);
            yield return null;

            BuilderPiece minislopeb = null;

            yield return CreateGetPiece(1700655257, piece => minislopeb = piece);
            while (minislopeb == null)
                yield return null;

            GrabPiece(minislopeb, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(baseb, slopea, 0, -3, 2, minislopeb, 2, 0);
            yield return null;

            BuilderPiece minislopea = null;

            yield return CreateGetPiece(1700655257, piece => minislopea = piece);
            while (minislopea == null)
                yield return null;

            GrabPiece(minislopea, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(minislopeb, slopeb, 0, -3, 2, minislopea, 2, 0);
            yield return null;

            BuilderPiece minislope2a = null;

            yield return CreateGetPiece(1700655257, piece => minislope2a = piece);
            while (minislope2a == null)
                yield return null;

            GrabPiece(minislope2a, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(minislopea, minislopeb, 0, 0, 2, minislope2a, 1, 0);
            yield return null;

            BuilderPiece minislope2b = null;

            yield return CreateGetPiece(1700655257, piece => minislope2b = piece);
            while (minislope2b == null)
                yield return null;

            GrabPiece(minislope2b, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(minislope2a, minislopea, 0, 0, 2, minislope2b, 1, 0);
            yield return null;

            BuilderPiece flatthinga = null;

            yield return CreateGetPiece(477262573, piece => flatthinga = piece);
            while (flatthinga == null)
                yield return null;

            GrabPiece(flatthinga, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(minislope2b, minislope2b, 0, -1, 2, flatthinga, 2, 0);
            yield return null;

            BuilderPiece flatthingb = null;

            yield return CreateGetPiece(477262573, piece => flatthingb = piece);
            while (flatthingb == null)
                yield return null;

            GrabPiece(flatthingb, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(flatthinga, minislope2a, 0, -1, 2, flatthingb, 2, 0);
            yield return null;

            BuilderPiece connectorthinga = null;

            yield return CreateGetPiece(251444537, piece => connectorthinga = piece);
            while (connectorthinga == null)
                yield return null;

            GrabPiece(connectorthinga, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(flatthingb, flatthinga, -1, 1, 3, connectorthinga, 1, 0);
            yield return null;

            BuilderPiece connectorthingb = null;

            yield return CreateGetPiece(661312857, piece => connectorthingb = piece);
            while (connectorthingb == null)
                yield return null;

            GrabPiece(connectorthingb, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(connectorthinga, connectorthinga, -1, 0, 1, connectorthingb, 1, 0);
            yield return null;

            BuilderPiece connectorthingc = null;

            yield return CreateGetPiece(661312857, piece => connectorthingc = piece);
            while (connectorthingc == null)
                yield return null;

            GrabPiece(connectorthingc, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(connectorthingb, connectorthinga, 0, 0, 1, connectorthingc, 1, 0);
            yield return null;

            BuilderPiece barrela = null;

            yield return CreateGetPiece(661312857, piece => barrela = piece);
            while (barrela == null)
                yield return null;

            GrabPiece(barrela, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(connectorthingc, connectorthingb, 0, 0, 1, barrela, 1, 0);
            yield return null;

            BuilderPiece barrelb = null;

            yield return CreateGetPiece(661312857, piece => barrelb = piece);
            while (barrelb == null)
                yield return null;

            GrabPiece(barrelb, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(barrela, barrela, 0, 0, 2, barrelb, 1, 0);
            yield return null;

            BuilderPiece scope = null;

            yield return CreateGetPiece(-648273975, piece => scope = piece);
            while (scope == null)
                yield return null;

            GrabPiece(scope, false, Vector3.zero, Quaternion.identity);
            yield return null;
            PlacePiece(barrelb, minislope2a, -2, 1, 3, scope, 1, 0);
            yield return null;
            DropPiece(scope, References.RightHand.position, Quaternion.identity, Vector3.zero, Vector3.zero);
            yield return null;
            GrabPiece(basea, false, new Vector3(-0.2f, 0.01f, -0.3f), new Quaternion(0f, 0.1f, 0.75f, -0.6f));
            yield return null;
        }

        public static IEnumerator CreateMassiveBlock()
        {
            References.LocalRig.sizeManager.currentSizeLayerMaskValue = 2;
            yield return new WaitForSeconds(0.6f);

            BuilderPiece stupid = null;

            yield return CreateGetPiece(pieceIdSet, piece => stupid = piece);
            while (stupid == null)
                yield return null;

            GrabPiece(stupid, false, Vector3.zero, Quaternion.identity);

            yield return new WaitForSeconds(0.2f);

            References.LocalRig.sizeManager.currentSizeLayerMaskValue = 13;
            yield return null;
        }

        public static void MassiveBlock()
        {
            if (rightGrab && !previousGripDown)
                Coroutines.instance.StartCoroutine(CreateMassiveBlock());

            previousGripDown = rightGrab;
        }
        public static void GrabAllBlocksNearby()
        {
            if (rightGrab && Time.time > blockDelay)
            {
                blockDelay = Time.time + blockDebounce;
                BuilderPiece piece = GetAllType<BuilderPiece>()
                    .Where(piece => piece.gameObject.activeInHierarchy)
                    .Where(piece => !piece.isBuiltIntoTable)
                    .Where(piece => piece.heldByPlayerActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                    .Where(piece => piece.CanPlayerGrabPiece(PhotonNetwork.LocalPlayer.ActorNumber, piece.transform.position))
                    .Where(piece => Vector3.Distance(piece.transform.position, ServerLeftHandPos) < 2.5f)
                    .OrderBy(piece => Vector3.Distance(piece.transform.position, ServerLeftHandPos))
                    .FirstOrDefault();

                if (piece == null)
                    return;

                GrabPiece(piece, false, Manager.GetIndex("No Random Position Grab").enabled ? Vector3.zero : RandomVector3(0.5f), Manager.GetIndex("No Random Rotation Grab").enabled ? Quaternion.identity : RandomQuaternion());
                potentialgrabbedpieces.Add(piece);

                Security.RPCProtection();
            }

            if (rightTrigger > 0.5f && Time.time > blockDelay)
            {
                int amount = 0;
                blockDelay = Time.time + 0.3f;
                foreach (BuilderPiece piece in GetAllType<BuilderPiece>().Where(piece => piece.gameObject.activeInHierarchy).Where(piece => piece.heldByPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    if (amount > 100)
                        break;

                    amount++;

                    DropPiece(piece, References.RightHand.position, References.RightHand.rotation, RandomVector3(19f), RandomVector3(19f));
                }

                potentialgrabbedpieces.Clear();
                Security.RPCProtection();
            }
        }

        public static void GrabAllSelectedNearby()
        {
            if (rightGrab && Time.time > blockDelay)
            {
                blockDelay = Time.time + blockDebounce;
                BuilderPiece piece = GetAllType<BuilderPiece>()
                    .Where(piece => piece.gameObject.activeInHierarchy)
                    .Where(piece => piece.pieceType == pieceIdSet)
                    .Where(piece => !piece.isBuiltIntoTable)
                    .Where(piece => piece.heldByPlayerActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                    .Where(piece => piece.CanPlayerGrabPiece(PhotonNetwork.LocalPlayer.ActorNumber, piece.transform.position))
                    .Where(piece => Vector3.Distance(piece.transform.position, ServerLeftHandPos) < 2.5f)
                    .OrderBy(piece => Vector3.Distance(piece.transform.position, ServerLeftHandPos))
                    .FirstOrDefault();

                if (piece == null)
                    return;

                GrabPiece(piece, false, Manager.GetIndex("No Random Position Grab").enabled ? Vector3.zero : RandomVector3(0.5f), Manager.GetIndex("No Random Rotation Grab").enabled ? Quaternion.identity : RandomQuaternion());
                potentialgrabbedpieces.Add(piece);

                Security.RPCProtection();
            }

            if (rightTrigger > 0.5f && Time.time > blockDelay)
            {
                int amount = 0;
                blockDelay = Time.time + 0.3f;
                foreach (BuilderPiece piece in GetAllType<BuilderPiece>().Where(piece => piece.gameObject.activeInHierarchy).Where(piece => piece.heldByPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    if (amount > 100)
                        break;

                    amount++;

                    DropPiece(piece, References.RightHand.position, References.RightHand.rotation, RandomVector3(19f), RandomVector3(19f));
                }

                potentialgrabbedpieces.Clear();
                Security.RPCProtection();
            }
        }

        public static void UnlimitedBuilding()
        {
            ManagerRegistry.BuilderPieceInteractor.maxHoldablePieceStackCount = int.MaxValue;
            UnlimitPatches.enabled = true;
        }

        public static void DisableUnlimitedBuilding()
        {
            ManagerRegistry.BuilderPieceInteractor.maxHoldablePieceStackCount = 50;
            UnlimitPatches.enabled = false;
        }

        public static void BlockCrashGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!Qubit.Core.Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                    Blocks.CreatePiece(1934114066, new Vector3(-127.6248f, 16.99441f, -217.2094f), Quaternion.identity, 0, NetPlayerToPlayer(lockTarget.GetPlayer()), false, true);
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                } });
        }

        public static void BlockCrashAll()
        {
            if (rightTrigger > 0.5f)
            {
                if (!Qubit.Core.Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                Blocks.CreatePiece(1934114066, new Vector3(-127.6248f, 16.99441f, -217.2094f), Quaternion.identity, 0, RpcTarget.Others, false, true);
            }
        }
    }
}
