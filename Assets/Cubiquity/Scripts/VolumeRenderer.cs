﻿using UnityEngine;
using System.Collections;

using Cubiquity.Impl;

namespace Cubiquity
{
	/// Controls some visual aspects of the volume and allows it to be rendered.
	/**
	 * The role of the VolumeRenderer component for volumes is conceptually similar to the role of Unity's MeshRenderer class for meshes.
	 * Specifically, it can be attached to a GameObject which also has a Volume component to cause that Volume component to be drawn. It 
	 * also exposes a number of properties such as whether a volume should cast and receive shadows.
	 * 
	 * Remember that Cubiquity acctually draws the volume by creating standard Mesh objects. Internally Cubiquity will copy the properties
	 * of the VolumeRenderer to the MeshRenderers which are generated.
	 * 
	 * \sa VolumeCollider
	 */
	public abstract class VolumeRenderer : MonoBehaviour
	{
        /// Material for this volume.
        public Material material
        {
            get
            {
                return mMaterial;
            }
            set
            {                
                mMaterial = value;
                mMaterialLod1 = new Material(value);
                mMaterialLod1.SetFloat("_height", 1.0f);
                mMaterialLod2 = new Material(value);
                mMaterialLod2.SetFloat("_height", 2.0f);
                lastModified = Clock.timestamp;
            }
        }
        [SerializeField]
		private Material mMaterial;

        public Material materialLod1
        {
            get
            {
                return mMaterialLod1;
            }
        }
        private Material mMaterialLod1;

        public Material materialLod2
        {
            get
            {
                return mMaterialLod2;
            }
        }
        private Material mMaterialLod2;
		
		/// Controls whether this volume casts shadows.
		public bool castShadows
		{
			get
			{
				return mCastShadows;
			}
			set
			{
				if(mCastShadows != value)
				{
					mCastShadows = value;
					lastModified = Clock.timestamp;
				}
			}
		}
		[SerializeField]
		private bool mCastShadows = true;
		
		/// Controls whether this volume receives shadows.
		public bool receiveShadows
		{
			get
			{
				return mReceiveShadows;
			}
			set
			{
				if(mReceiveShadows != value)
				{
					mReceiveShadows = value;
					lastModified = Clock.timestamp;
				}
			}
		}
		[SerializeField]
		private bool mReceiveShadows = true;

		/// Controls whether the wireframe overlay is displayed when this volume is selected in the editor.
		public bool showWireframe
		{
			get
			{
				return mShowWireframe;
			}
			set
			{
				if(mShowWireframe != value)
				{
					mShowWireframe = value;
					lastModified = Clock.timestamp;
				}
			}
		}
		[SerializeField]
		private bool mShowWireframe = false;

        /// Controls whether the wireframe overlay is displayed when this volume is selected in the editor.
        public float lodThreshold
        {
            get
            {
                return mLodThreshold;
            }
            set
            {
                mLodThreshold = value;
            }
        }
        [SerializeField]
        private float mLodThreshold = 1.0f;
		
		/// \cond
		public uint lastModified = Clock.timestamp;
		/// \endcond
		
		// Dummy start method rqured for the 'enabled' checkbox to show up in the inspector.
		void Start() { }
	}
}
