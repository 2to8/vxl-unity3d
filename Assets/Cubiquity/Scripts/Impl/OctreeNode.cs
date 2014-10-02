﻿using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections;
using System.Text;

namespace Cubiquity
{
	namespace Impl
	{
		public class OctreeNode : MonoBehaviour
		{
			[System.NonSerialized]
			public uint meshLastSyncronised;
			[System.NonSerialized]
			public uint lastSyncronisedWithVolumeRenderer;
			[System.NonSerialized]
			public uint lastSyncronisedWithVolumeCollider;
			[System.NonSerialized]
			public Vector3 lowerCorner;
			[System.NonSerialized]
			public GameObject[,,] children;
			
			[System.NonSerialized]
			public uint nodeHandle;

			public static GameObject CreateOctreeNode(uint nodeHandle, GameObject parentGameObject)
			{			
				int xPos, yPos, zPos;
				//Debug.Log("Getting position for node handle = " + nodeHandle);
				CubiquityDLL.GetNodePosition(nodeHandle, out xPos, out yPos, out zPos);
				
				StringBuilder name = new StringBuilder("(" + xPos + ", " + yPos + ", " + zPos + ")");
				
				GameObject newGameObject = new GameObject(name.ToString ());
				newGameObject.AddComponent<OctreeNode>();
				
				OctreeNode octreeNode = newGameObject.GetComponent<OctreeNode>();
				octreeNode.lowerCorner = new Vector3(xPos, yPos, zPos);
				octreeNode.nodeHandle = nodeHandle;
				
				if(parentGameObject)
				{
					newGameObject.layer = parentGameObject.layer;
						
					newGameObject.transform.parent = parentGameObject.transform;
					newGameObject.transform.localPosition = new Vector3();
					newGameObject.transform.localRotation = new Quaternion();
					newGameObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
					
					OctreeNode parentOctreeNode = parentGameObject.GetComponent<OctreeNode>();
					
					if(parentOctreeNode != null)
					{
						Vector3 parentLowerCorner = parentOctreeNode.lowerCorner;
						newGameObject.transform.localPosition = octreeNode.lowerCorner - parentLowerCorner;
					}
					else
					{
						newGameObject.transform.localPosition = octreeNode.lowerCorner;
					}
				}
				else
				{
					newGameObject.transform.localPosition = octreeNode.lowerCorner;
				}
				
				newGameObject.hideFlags = HideFlags.HideInHierarchy;
				
				return newGameObject;
			}
			
			public int syncNode(int availableNodeSyncs, GameObject voxelTerrainGameObject)
			{
				int nodeSyncsPerformed = 0;
				
				if(availableNodeSyncs <= 0)
				{
					return nodeSyncsPerformed;
				}
				
				uint meshLastUpdated = CubiquityDLL.GetMeshLastUpdated(nodeHandle);		
				
				if(meshLastSyncronised < meshLastUpdated)
				{			
					if(CubiquityDLL.NodeHasMesh(nodeHandle) == 1)
					{					
						// Set up the rendering mesh											
						VolumeRenderer volumeRenderer = voxelTerrainGameObject.GetComponent<VolumeRenderer>();
						if(volumeRenderer != null)
						{						
							//Mesh renderingMesh = volumeRenderer.BuildMeshFromNodeHandle(nodeHandle);
							
							Mesh renderingMesh = null;
							if(voxelTerrainGameObject.GetComponent<Volume>().GetType() == typeof(TerrainVolume))
							{
                                renderingMesh = MeshConversion.BuildMeshFromNodeHandleForTerrainVolume(nodeHandle, false);
							}
							else if(voxelTerrainGameObject.GetComponent<Volume>().GetType() == typeof(ColoredCubesVolume))
							{
								renderingMesh = MeshConversion.BuildMeshFromNodeHandleForColoredCubesVolume(nodeHandle, false);
							}
					
					        MeshFilter meshFilter = gameObject.GetOrAddComponent<MeshFilter>() as MeshFilter;
						    MeshRenderer meshRenderer = gameObject.GetOrAddComponent<MeshRenderer>() as MeshRenderer;
							
							if(meshFilter.sharedMesh != null)
							{
								DestroyImmediate(meshFilter.sharedMesh);
							}
							
					        meshFilter.sharedMesh = renderingMesh;
						
							meshRenderer.sharedMaterial = volumeRenderer.material;
						}
						
						// Set up the collision mesh
						VolumeCollider volumeCollider = voxelTerrainGameObject.GetComponent<VolumeCollider>();					
						if((volumeCollider != null) && (Application.isPlaying))
						{
							Mesh collisionMesh = volumeCollider.BuildMeshFromNodeHandle(nodeHandle);
							MeshCollider meshCollider = gameObject.GetOrAddComponent<MeshCollider>() as MeshCollider;
							meshCollider.sharedMesh = collisionMesh;
						}
					}
					// If there is no mesh in Cubiquity then we make sure there isn't on in Unity.
					else
					{
						MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>() as MeshCollider;
						if(meshCollider)
						{
							DestroyImmediate(meshCollider);
						}
						
						MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>() as MeshRenderer;
						if(meshRenderer)
						{
							DestroyImmediate(meshRenderer);
						}
						
						MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>() as MeshFilter;
						if(meshFilter)
						{
							DestroyImmediate(meshFilter);
						}
					}
					
					meshLastSyncronised = CubiquityDLL.GetCurrentTime();
					availableNodeSyncs--;
					nodeSyncsPerformed++;
					
				}
				
				VolumeRenderer vr = voxelTerrainGameObject.GetComponent<VolumeRenderer>();
				MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
				if(vr != null && mr != null)
				{
					uint renderThisNode = 0;
					// Horrible hack to choose correct funtion!
					if(voxelTerrainGameObject.GetComponent<Volume>().GetType() == typeof(TerrainVolume))
					{
						renderThisNode = CubiquityDLL.RenderThisNodeMC(nodeHandle);
					}
					else if(voxelTerrainGameObject.GetComponent<Volume>().GetType() == typeof(ColoredCubesVolume))
					{
						renderThisNode = CubiquityDLL.RenderThisNode(nodeHandle);
					}

					mr.enabled = vr.enabled && (renderThisNode != 0);
					
					if(lastSyncronisedWithVolumeRenderer < vr.lastModified)
					{
						mr.receiveShadows = vr.receiveShadows;
						mr.castShadows = vr.castShadows;

						#if UNITY_EDITOR
						EditorUtility.SetSelectedWireframeHidden(mr, !vr.showWireframe);
						#endif

						lastSyncronisedWithVolumeRenderer = Clock.timestamp;
					}
				}
				
				VolumeCollider vc = voxelTerrainGameObject.GetComponent<VolumeCollider>();
				MeshCollider mc = gameObject.GetComponent<MeshCollider>();
				if(vc != null && mc != null)
				{
					if(mc.enabled != vc.enabled) // Not sure we really need this check?
					{
						mc.enabled = vc.enabled;
					}
					
					if(lastSyncronisedWithVolumeCollider < vc.lastModified)
					{
						// Actual syncronization to be filled in in the future when we have something to syncronize.
						lastSyncronisedWithVolumeCollider = Clock.timestamp;
					}
				}
				
				//Now syncronise any children
				for(uint z = 0; z < 2; z++)
				{
					for(uint y = 0; y < 2; y++)
					{
						for(uint x = 0; x < 2; x++)
						{
							if(CubiquityDLL.HasChildNode(nodeHandle, x, y, z) == 1)
							{					
							
								uint childNodeHandle = CubiquityDLL.GetChildNode(nodeHandle, x, y, z);					
								
								GameObject childGameObject = GetChild(x,y,z);
								
								if(childGameObject == null)
								{							
									childGameObject = OctreeNode.CreateOctreeNode(childNodeHandle, gameObject);
									
									SetChild(x, y, z, childGameObject);
								}
								
								//syncNode(childNodeHandle, childGameObject);
								
								OctreeNode childOctreeNode = childGameObject.GetComponent<OctreeNode>();
								int syncs = childOctreeNode.syncNode(availableNodeSyncs, voxelTerrainGameObject);
								availableNodeSyncs -= syncs;
								nodeSyncsPerformed += syncs;
							}
						}
					}
				}
				
				return nodeSyncsPerformed;
			}
			
			public GameObject GetChild(uint x, uint y, uint z)
			{
				if(children != null)
				{
					return children[x, y, z];
				}
				else
				{
					return null;
				}
			}
			
			public void SetChild(uint x, uint y, uint z, GameObject gameObject)
			{
				if(children == null)
				{
					children = new GameObject[2, 2, 2];
				}
				
				children[x, y, z] = gameObject;
			}
		}
	}
}
