﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	public class MapOverlay : MonoBehaviour
	{
		CelestialBody _body;
		MeshRenderer meshRenderer;
		Mesh mesh;

		public CelestialBody Body
		{
			set
			{
				_body = value;

				var target = ScaledSpace.Instance.scaledSpaceTransforms.FirstOrDefault(t => t.name == _body.name);
				gameObject.layer = target.gameObject.layer;
				gameObject.transform.parent = target;
				gameObject.transform.localScale = Vector3d.one * 1.01 * _body.Radius * ScaledSpace.InverseScaleFactor * 10;
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;

			}
			get
			{
				return _body;
			}
		}

		bool _Visible = true;
		public bool Visible
		{
			get
			{
				return _Visible;
			}
			set
			{
				_Visible = value;
				OnMapVisibility();
			}
		}

		protected void Awake()
		{
			var meshFilter = gameObject.AddComponent<MeshFilter>();
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
			mesh = meshFilter.mesh;

			meshRenderer.castShadows = false;
			meshRenderer.receiveShadows = false;

			var material = new Material(new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ProceduralCities.Resources.AlphaUnlitVertexColored.txt")).ReadToEnd()); // TODO

			var color = Color.white;
			color.a = 0.4f;
			material.color = color;

			renderer.material = material;

			GameEvents.OnMapEntered.Add(OnMapEntered);
			GameEvents.OnMapExited.Add(OnMapExited);
			GameEvents.onLevelWasLoaded.Add(OnLevelWasLoaded);

			OnMapVisibility();
		}

		void OnLevelWasLoaded(GameScenes scene)
		{
			OnMapVisibility();
		}

		void OnMapEntered()
		{
			try
			{
				meshRenderer.enabled = _Visible;
			}
			catch
			{
			}
		}

		void OnMapExited()
		{
			try
			{
				meshRenderer.enabled = false;
			}
			catch
			{
			}
		}

		void OnMapVisibility()
		{
			try
			{
				meshRenderer.enabled = Visible && HighLogic.LoadedSceneHasPlanetarium && MapView.MapIsEnabled;
			}
			catch
			{
			}
		}

		protected void UpdateMesh(IEnumerable<Vector3> vertices, IEnumerable<int> triangles, IEnumerable<Color32> colors)
		{
			Debug.Log("[ProceduralCities] Updating map mesh");
			UpdateMesh(vertices.ToArray(), triangles.ToArray(), colors.ToArray());
		}

		protected void UpdateMesh(Vector3[] vertices, int[] triangles, Color32[] colors)
		{
			mesh.Clear();

			mesh.vertices = vertices;
			mesh.colors32 = colors;
			mesh.triangles = triangles;

			mesh.RecalculateBounds();
			mesh.Optimize();
			OnMapVisibility();
		}
	}
}
